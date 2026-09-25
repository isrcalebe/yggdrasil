using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Server.AspNetCore;
using yggdrasil.Modules.Identity.Data;

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
                        .SetAuthorizationEndpointUris("connect/authorize")
                        .SetTokenEndpointUris("connect/token");

                    options
                        .AddEphemeralEncryptionKey()
                        .AddEphemeralSigningKey();

                    options.UseAspNetCore();
                });

            self.AddOptions<OpenIddictServerAspNetCoreOptions>()
                .Configure<IHostEnvironment>(static (options, environment)
                    => options.DisableTransportSecurityRequirement = environment.IsDevelopment());

            return self;
        }
    }
}
