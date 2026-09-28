using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

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

    private static async Task<IResult> exchangeAsync(HttpContext context, IOpenIddictApplicationManager applications)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            // A game server acting on its own behalf: the tokens are about the client itself.
            var application = await applications.FindByClientIdAsync(request.ClientId!)
                ?? throw new InvalidOperationException("The client application cannot be found.");

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role)
                .SetClaim(Claims.Subject, await applications.GetClientIdAsync(application))
                .SetClaim(Claims.Name, await applications.GetDisplayNameAsync(application));

            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(static _ => [Destinations.AccessToken]);

            return TypedResults.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Authorization code and refresh token: issue new tokens from the principal stored in the code or refresh token.
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        return TypedResults.SignIn(result.Principal!, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
