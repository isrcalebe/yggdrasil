using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Server.AspNetCore;

namespace yggdrasil.Modules.Identity.OpenId;

public static class TokenEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapTokenEndpoint()
            => self
                .MapPost("/connect/token", (Delegate)exchangeAsync)
                .ExcludeFromDescription();
    }

    private static async Task<IResult> exchangeAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        return TypedResults.SignIn(result.Principal!, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
