using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Identity.Contracts.v1.Antiforgery;

namespace yggdrasil.Modules.Identity.Features.v1.Antiforgery.GetAntiforgeryToken;

public static class GetAntiforgeryTokenEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapGetAntiforgeryTokenEndpoint()
            => self
                .MapGet("/antiforgery", static (HttpContext httpContext, IAntiforgery antiforgery) =>
                {
                    var token = antiforgery.GetAndStoreTokens(httpContext);

                    return TypedResults.Ok(new AntiforgeryTokenResponse(token.RequestToken!));
                })
                .WithName("identity.antiforgery.get")
                .WithSummary("Get an antiforgery token for state-changing requests");
    }
}
