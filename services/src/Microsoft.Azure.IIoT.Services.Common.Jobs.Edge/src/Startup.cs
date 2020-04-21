﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge {
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.Runtime;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Auth.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Cors;
    using Microsoft.Azure.IIoT.AspNetCore.Correlation;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using Autofac;
    using Prometheus;
    using Autofac.Extensions.DependencyInjection;
    using System;
    using ILogger = Serilog.ILogger;
    using Microsoft.Azure.IIoT.Http.Ssl;

    /// <summary>
    /// Webservice startup
    /// </summary>
    public class Startup {

        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Service info - Initialized in constructor
        /// </summary>
        public ServiceInfo ServiceInfo { get; } = new ServiceInfo();

        /// <summary>
        /// Current hosting environment - Initialized in constructor
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration) :
            this(env, new Config(new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build())) {
        }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, Config configuration) {
            Environment = env;
            Config = configuration;
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services) {

            // services.AddLogging(o => o.AddConsole().AddDebug());

            services.AddHeaderForwarding();
            services.AddCors();
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();
            services.AddHttpsRedirect();
            // services.AddJwtBearerAuthentication(); // TODO
            services.AddAuthorizationPolicies();

            // TODO: Remove http client factory and use
            // services.AddHttpClient();

            services.AddHttpContextAccessor();
            services.AddAuthentication("DeviceTokenAuth")
                .AddScheme<AuthenticationSchemeOptions, IdentityTokenAuthHandler>(
                    "DeviceTokenAuth", null);

            // Add controllers as services so they'll be resolved.
            services.AddControllers().AddSerializers();
            services.AddSwagger(Config, ServiceInfo.Name, ServiceInfo.Description);
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime) {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();
            var log = applicationContainer.Resolve<ILogger>();

            app.UsePathBase();
            app.UseHeaderForwarding();

            app.UseRouting();
            app.EnableCors();

            // app.UseJwtBearerAuthentication(); // TODO
            app.UseAuthorization();
            app.UseHttpsRedirect();

            app.UseCorrelation();
            app.UseSwagger();
            app.UseMetricServer();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/healthz");
            });

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);

            // Print some useful information at bootstrap time
            log.Information("{service} web service started with id {id}",
                ServiceInfo.Name, ServiceInfo.Id);
        }

        /// <summary>
        /// Configure Autofac container
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder) {
            // Register service info and configuration interfaces
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces();
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces();

            // Add diagnostics
            builder.AddDiagnostics(Config);

            // Register http client module
            builder.RegisterModule<HttpClientModule>();
#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif
            // Add serializers
            builder.RegisterModule<MessagePackModule>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Add service to service authentication
            builder.RegisterModule<WebApiAuthentication>();

            // CORS setup
            builder.RegisterType<CorsSetup>()
                .AsImplementedInterfaces().SingleInstance();

            // TODO: Use job database service api
            builder.RegisterType<JobDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WorkerDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DefaultJobOrchestrator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DefaultDemandMatcher>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<IdentityTokenValidator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinIdentityTokenStore>()
                .AsImplementedInterfaces().SingleInstance();

            // Activate all hosts
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();
        }
    }
}