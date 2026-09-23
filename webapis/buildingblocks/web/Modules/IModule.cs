using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace yggdrasil.Web.Modules;

/// <summary>
/// Entry point of a module. The host lists its modules once, in <see cref="ModuleExtensions"/>'s <c>AddModules</c>;
/// endpoints and middlewares are wired from that same list.
/// </summary>
public interface IModule
{
    /// <summary>Lowercase module name, used as route segment (<c>/api/v{version}/{Name}</c>) and OpenAPI tag.</summary>
    string Name { get; }

    /// <summary>API versions the module endpoints are available in.</summary>
    IReadOnlyList<ApiVersion> ApiVersions => [new ApiVersion(1)];

    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Maps the module endpoints into a versioned group already prefixed with <c>/api/v{version}/{Name}</c>.</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>Optional middleware, executed after routing and before endpoints.</summary>
    void ConfigureMiddleware(IApplicationBuilder app) { }
}
