using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Identity.Contracts;
using yggdrasil.Modules.Identity.Contracts.v1;
using yggdrasil.Modules.Profiles.Contracts.v1.Profiles;
using yggdrasil.Modules.Profiles.Http;
using yggdrasil.Web.Results;

namespace yggdrasil.Modules.Profiles.Features.v1.Profiles.GetCurrentProfile;

public static class GetCurrentProfileEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapGetCurrentProfileEndpoint()
            => self
                .MapGet("/me", static async (HttpContext context, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    // A token without a player (client credentials) has no "current" profile.
                    if (user.AccountId is not { } accountId || user.ClientId is not { } clientId)
                        return Results.Forbid();

                    var result = await mediator.Send(new GetCurrentProfileQuery(accountId, clientId), cancellationToken);

                    return result.ToHttpResult(profile =>
                    {
                        context.Response.Headers.ETag = DataVersionETag.Format(profile.DataVersion);

                        return TypedResults.Ok(profile);
                    });
                })
                .RequireAuthorization(IdentityPolicies.PROFILES_READ)
                .WithName("profiles.current")
                .WithSummary("Get the signed-in player's profile in this game")
                .Produces<ProfileResponse>()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
