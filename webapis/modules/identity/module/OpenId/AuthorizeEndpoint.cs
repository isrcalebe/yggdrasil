using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using yggdrasil.Modules.Identity.Domain;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Modules.Identity.OpenId;

/// <summary>
/// The authorization endpoint of "Login with Yggdrasil ID". OpenIddict has already validated the request (client,
/// redirect URI, PKCE) when this runs; it only decides who the player is.
/// </summary>
public static class AuthorizeEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapAuthorizeEndpoint()
            => self
                .MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], authorizeAsync)
                .ExcludeFromDescription();
    }

    private static async Task<IResult> authorizeAsync(HttpContext context, UserManager<Account> userManager)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var session = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var account = session.Succeeded
            ? await userManager.GetUserAsync(session.Principal)
            : null;

        if (account is null)
        {
            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;

            return TypedResults.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role)
            .SetClaim(Claims.Subject, account.Id.ToString())
            .SetClaim(Claims.Email, account.Email);

        identity.SetScopes(request.GetScopes());
        identity.SetDestinations(destinationsOf);

        return TypedResults.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<string> destinationsOf(Claim claim)
        => claim.Type switch
        {
            Claims.Email when claim.Subject?.HasScope(Scopes.Email) == true => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Email => [],
            _ => [Destinations.AccessToken]
        };
}
