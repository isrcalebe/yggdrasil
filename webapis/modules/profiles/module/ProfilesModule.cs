using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Persistence;
using yggdrasil.Web.Modules;

namespace yggdrasil.Modules.Profiles;

public sealed class ProfilesModule : IModule
{
    public const string NAME = "profiles";

    public string Name => NAME;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
