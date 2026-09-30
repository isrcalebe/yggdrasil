using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Modules.Profiles.Data;
using yggdrasil.Modules.Profiles.Features.v1.Profiles.GetCurrentProfile;
using yggdrasil.Persistence;
using yggdrasil.Web.Modules;

namespace yggdrasil.Modules.Profiles;

public sealed class ProfilesModule : IModule
{
    public const string NAME = "profiles";

    public string Name => NAME;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddModuleDbContext<ProfilesModuleDbContext>();

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGetCurrentProfileEndpoint();
    }
}
