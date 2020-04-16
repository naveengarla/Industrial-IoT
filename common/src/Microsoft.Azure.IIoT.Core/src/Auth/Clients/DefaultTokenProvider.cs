﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System;
    using System.Linq;

    /// <summary>
    /// Caching token provider
    /// </summary>
    public class DefaultTokenProvider : ITokenProvider {

        /// <inheritdoc/>
        public DefaultTokenProvider(IEnumerable<ITokenSource> tokenSources) {
            _tokenSources = tokenSources?.Where(s => s.IsEnabled).ToList() ??
                throw new ArgumentNullException(nameof(tokenSources));
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            return _tokenSources.Any(p => p.Resource == resource);
        }

        /// <inheritdoc/>
        public virtual async Task<TokenResultModel> GetTokenForAsync(
            string resource, IEnumerable<string> scopes = null) {
            foreach (var source in _tokenSources.Where(p => p.Resource == resource)) {
                var token = await source.GetTokenAsync(scopes);
                if (token != null) {
                    return token;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public virtual async Task InvalidateAsync(string resource) {
            await Task.WhenAll(_tokenSources
                .Where(p => p.Resource == resource)
                .Select(p => p.InvalidateAsync()));
        }

        private readonly List<ITokenSource> _tokenSources;
    }
}
