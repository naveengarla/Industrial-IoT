// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Autofac;
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;

    /// <summary>
    /// Injected iot hub edge hosting context
    /// </summary>
    public sealed class IoTEdgeHosted : Module {

        /// <summary>
        /// Load the module
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder) {

            // Register sdk, edgelet client and token generators
            builder.RegisterType<IoTSdkFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<EventSourceBroker>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EdgeletClient>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SasTokenGenerator>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<MemoryCache>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // .... and module host
            builder.RegisterType<IoTEdgeModuleHost>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            base.Load(builder);
        }
    }
}
