// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Diagnostics configuration
    /// </summary>
    public class AppInsightsConfig : DiagnosticsConfig, IAppInsightsConfig {

        /// <summary>
        /// Configuration keys
        /// </summary>
        private const string kInstrumentationKeyKey = "Diagnostics:InstrumentationKey";

        /// <inheritdoc/>
        public string InstrumentationKey =>
            GetStringOrDefault(kInstrumentationKeyKey,
                () => GetStringOrDefault(PcsVariable.PCS_APPINSIGHTS_INSTRUMENTATIONKEY,
                () => null));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public AppInsightsConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
