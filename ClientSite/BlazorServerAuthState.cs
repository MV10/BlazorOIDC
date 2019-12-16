using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ClientSite
{
    public class BlazorServerAuthState 
        : RevalidatingServerAuthenticationStateProvider
    {
        private readonly BlazorServerAuthStateCache Cache;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IDiscoveryCache OidcDiscoveryCache;

        public BlazorServerAuthState(
            ILoggerFactory loggerFactory,
            BlazorServerAuthStateCache cache,
            IHttpClientFactory httpClientFactory,
            IDiscoveryCache discoveryCache)
            : base(loggerFactory)
        {
            Cache = cache;
            this.httpClientFactory = httpClientFactory;
            OidcDiscoveryCache = discoveryCache;
        }

        protected override TimeSpan RevalidationInterval
            => TimeSpan.FromSeconds(10); // TODO read from config

        protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var sid =
                authenticationState.User.Claims
                .Where(c => c.Type.Equals("sid"))
                .Select(c => c.Value)
                .FirstOrDefault();

            var name =
                authenticationState.User.Claims
                .Where(c => c.Type.Equals("name"))
                .Select(c => c.Value)
                .FirstOrDefault() ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"\nValidate: {name} / {sid}");

            if (sid != null && Cache.HasSubjectId(sid))
            {
                var data = Cache.Get(sid);

                if(DateTimeOffset.UtcNow >= data.Expiration)
                {
                    System.Diagnostics.Debug.WriteLine($"*** EXPIRED ***");
                    Cache.Remove(sid);
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"Now UTC: {DateTimeOffset.UtcNow.ToString("o")}");
                System.Diagnostics.Debug.WriteLine($"Refresh: {data.RefreshAt.ToString("o")}");

                // part 3
                if (data.RefreshAt < DateTimeOffset.UtcNow) await RefreshAccessToken(data);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"(not in cache)");
            }

            return true;
        }

        // part 3
        private async Task RefreshAccessToken(BlazorServerAuthData data)
        {
            System.Diagnostics.Debug.WriteLine("Refreshing API access token:");

            var client = httpClientFactory.CreateClient();
            var disco = await OidcDiscoveryCache.GetAsync();
            if (disco.IsError) return;
            System.Diagnostics.Debug.WriteLine("...discovery complete");

            var tokenResponse = await client.RequestRefreshTokenAsync(
                new RefreshTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = "interactive.confidential.short",
                    ClientSecret = "secret",
                    RefreshToken = data.RefreshToken
                });
            if (tokenResponse.IsError) return;
            System.Diagnostics.Debug.WriteLine("...refresh complete");

            data.AccessToken = tokenResponse.AccessToken;
            data.RefreshToken = tokenResponse.RefreshToken;
            data.RefreshAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(tokenResponse.ExpiresIn / 2);
            System.Diagnostics.Debug.WriteLine("...auth cache updated.");
        }
    }
}
