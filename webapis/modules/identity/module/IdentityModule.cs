using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Modules.Identity.Data;
using yggdrasil.Modules.Identity.Domain;
using yggdrasil.Modules.Identity.Features.v1.Accounts.RegisterAccount;
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

        services.AddIdentityCore<Account>(static options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityModuleDbContext>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
        => endpoints.MapRegisterAccountEndpoint();
}
