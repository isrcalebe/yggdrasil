using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Modules.Games.Data;
using yggdrasil.Modules.Games.Features.v1.Games.RegisterGame;
using yggdrasil.Persistence;
using yggdrasil.Web.Modules;

namespace yggdrasil.Modules.Games;

/// <summary>The game catalog: every game that can use "Login with Yggdrasil ID", with its OAuth clients.</summary>
public sealed class GamesModule : IModule
{
    public const string NAME = "games";

    public string Name => NAME;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddModuleDbContext<GamesModuleDbContext>();

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRegisterGameEndpoint();
    }
}
