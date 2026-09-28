using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Web.Modules;

namespace yggdrasil.Modules.Games;

/// <summary>The game catalog: every game that can use "Login with Yggdrasil ID", with its OAuth clients.</summary>
public sealed class GamesModule : IModule
{
    public const string NAME = "games";

    public string Name => NAME;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
