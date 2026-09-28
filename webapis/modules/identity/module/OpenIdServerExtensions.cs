using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Server.AspNetCore;
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
                        .SetAuthorizationEndpointUris("connect/authorize")
                        .SetTokenEndpointUris("connect/token");

                    options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.OfflineAccess);

                    // Refresh tokens rotate on every use (OpenIddict's default). By default a redeemed one still works for
                    // 30 more seconds, to forgive a retried request; with no leeway, a replay is rejected right away and
                    // revokes the tokens issued from it, since it means the token leaked.
                    options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);

                    options
                        .AddEphemeralEncryptionKey()
                        .AddEphemeralSigningKey();

                    // Game servers validate access tokens locally against the published keys (JWKS): signed, not encrypted.
                    options.DisableAccessTokenEncryption();

                    // /connect/authorize is handled by AuthorizeEndpoint; the token endpoint needs no code of ours for
                    // the authorization code grant, OpenIddict issues the tokens from the principal stored in the code.
                    options
                        .UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableTokenEndpointPassthrough();
                });

            self.AddOptions<OpenIddictServerAspNetCoreOptions>()
                .Configure<IHostEnvironment>(static (options, environment)
                    => options.DisableTransportSecurityRequirement = environment.IsDevelopment());

            return self;
        }
    }
}
