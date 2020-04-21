﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.AspNetCore.Components.Authorization;
    using Autofac;
    using Serilog;
    using System.Collections.Generic;

    /// <summary>
    /// Default web app authentication
    /// </summary>
    public class WebAppAuthentication : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<HttpHandlerFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientAuthAggregateConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<DefaultTokenProvider>()
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .IfNotRegistered(typeof(ITokenProvider));
            // Cache tokens in memory
            builder.RegisterType<MemoryCache>()
                .AsImplementedInterfaces().SingleInstance()
                .IfNotRegistered(typeof(ICache));

            builder.RegisterModule<DefaultServiceAuthProviders>();

            // 1) Use auth service open id token client
            builder.RegisterType<OpenIdUserTokenClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            // 2) Use msal user token as fallback
            builder.RegisterType<MsalUserTokenClient>()
                .AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            // Use service to service token source
            builder.RegisterType<UserTokenSource>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            base.Load(builder);
        }

        /// <summary>
        /// First try passthrough, then try service client credentials
        /// </summary>
        internal class UserTokenSource : TokenClientAggregateSource, ITokenSource {

            /// <inheritdoc/>
            public UserTokenSource(OpenIdUserTokenClient oi, MsalUserTokenClient uc,
                IEnumerable<ITokenClient> providers, ILogger logger)
                : base(providers, Http.Resource.Platform, logger, oi, uc) {
            }
        }
    }
}
