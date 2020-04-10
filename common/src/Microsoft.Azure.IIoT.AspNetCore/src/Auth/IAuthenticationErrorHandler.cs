﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.AspNetCore.Http;
    using System.Security.Authentication;

    /// <summary>
    /// Handle error
    /// </summary>
    public interface IAuthenticationErrorHandler {

        /// <summary>
        /// Aquire token non-silent if silent fails before handling.
        /// </summary>
        bool AcquireTokenIfSilentFails { get; }

        /// <summary>
        /// Handle authentication error
        /// </summary>
        void Handle(HttpContext context, AuthenticationException ex);

        /// <summary>
        /// Handle invalidate
        /// </summary>
        /// <param name="context"></param>
        void Invalidate(HttpContext context);
    }
}
