using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using yggdrasil.Modules.Identity.Administration;
using yggdrasil.Modules.Identity.Contracts;
using yggdrasil.Modules.Identity.Data;
using yggdrasil.Modules.Identity.Domain;
using yggdrasil.Modules.Identity.Features.v1.Accounts.RegisterAccount;
using yggdrasil.Modules.Identity.Features.v1.Administrators;
using yggdrasil.Modules.Identity.Features.v1.Antiforgery.GetAntiforgeryToken;
using yggdrasil.Modules.Identity.Features.v1.Sessions.GetCurrentSession;
using yggdrasil.Modules.Identity.Features.v1.Sessions.SignIn;
using yggdrasil.Modules.Identity.Features.v1.Sessions.SignOut;
using yggdrasil.Modules.Identity.OpenId;
using yggdrasil.Persistence;
using yggdrasil.Web.Modules;

namespace yggdrasil.Modules.Identity;

public sealed class IdentityModule : IModule
{
    public const string NAME = "identity";

    public string Name => NAME;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityModuleDbContext>();

        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddIdentityCore<Account>(static options =>
        {
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = string.Empty;
            options.Password.RequiredLength = 8;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityModuleDbContext>()
            .AddSignInManager();

        services
            .AddAuthorizationBuilder()
            .AddPolicy(IdentityPolicies.ADMIN, static policy => policy.RequireRole(Roles.ADMIN))
            .AddPolicy(IdentityPolicies.PROFILES_READ, static policy => policy
                .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(static context => context.User.HasScope(IdentityScopes.PROFILES_READ))
            );

        services.AddOpenIdServer();

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IHostEnvironment>(static (options, environment) =>
            {
                options.Cookie.Name = "yggdrasil.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;

                options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

                options.Events.OnRedirectToLogin = static context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = static context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;

                    return Task.CompletedTask;
                };
            });

        services.AddSingleton<AdministratorSeeder>();
        services.AddHostedService(static services => services.GetRequiredService<AdministratorSeeder>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetAntiforgeryTokenEndpoint();
        endpoints.MapRegisterAccountEndpoint();
        endpoints.MapSignInEndpoint();
        endpoints.MapGetCurrentSessionEndpoint();
        endpoints.MapSignOutEndpoint();
        endpoints.MapGrantAdministratorEndpoint();
    }

    public void MapRootEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthorizeEndpoint();
        endpoints.MapTokenEndpoint();
    }
}
