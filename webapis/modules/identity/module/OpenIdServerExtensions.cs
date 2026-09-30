using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Server.AspNetCore;
using yggdrasil.Modules.Identity.Contracts;
using yggdrasil.Modules.Identity.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Modules.Identity;

internal static class OpenIdServerExtensions
{
    extension(IServiceCollection self)
    {
        public IServiceCollection AddOpenIdServer()
        {
            self.AddOpenIddict()
                .AddCore(static options => options
                    .UseEntityFrameworkCore()
                    .UseDbContext<IdentityModuleDbContext>()
                    .ReplaceDefaultEntities<Guid>())
                .AddServer(static options =>
                {
                    options
                        .AllowAuthorizationCodeFlow()
                        .RequireProofKeyForCodeExchange()
                        .AllowRefreshTokenFlow()
                        .AllowClientCredentialsFlow()
                        .SetAuthorizationEndpointUris("connect/authorize")
                        .SetTokenEndpointUris("connect/token");

                    options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.OfflineAccess, IdentityScopes.PROFILES_READ, IdentityScopes.PROFILES_WRITE);

                    // Refresh tokens rotate on every use (OpenIddict's default). By default a redeemed one still works for
                    // 30 more seconds, to forgive a retried request; with no leeway, a replay is rejected right away and
                    // revokes the tokens issued from it, since it means the token leaked.
                    options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);

                    options
                        .AddEphemeralEncryptionKey()
                        .AddEphemeralSigningKey();

                    // Game servers validate access tokens locally against the published keys (JWKS): signed, not encrypted.
                    options.DisableAccessTokenEncryption();

                    // After validating a request, OpenIddict hands it to our endpoints (OpenId/AuthorizeEndpoint and
                    // OpenId/TokenEndpoint), which decide who the tokens are about.
                    options
                        .UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableTokenEndpointPassthrough();
                })
                .AddValidation(static options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });

            self.AddOptions<OpenIddictServerAspNetCoreOptions>()
                .Configure<IHostEnvironment>(static (options, environment)
                    => options.DisableTransportSecurityRequirement = environment.IsDevelopment());

            return self;
        }
    }
}
