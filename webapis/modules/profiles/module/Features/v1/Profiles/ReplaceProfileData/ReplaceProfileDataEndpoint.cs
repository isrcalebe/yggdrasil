using System.Security.Claims;
using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts;
using yggdrasil.Modules.Profiles.Contracts.v1.Profiles;
using yggdrasil.Modules.Profiles.Http;
using yggdrasil.Web.Results;

namespace yggdrasil.Modules.Profiles.Features.v1.Profiles.ReplaceProfileData;

public static class ReplaceProfileDataEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapReplaceProfileDataEndpoint()
            => self
                .MapPut("/{profileId:guid}/data", static async (Guid profileId, JsonElement data, HttpContext httpContext, IMediator mediator, ClaimsPrincipal user, CancellationToken cancellationToken) =>
                {
                    var ifMatch = httpContext.Request.GetTypedHeaders().IfMatch;

                    if (user.ClientId is not { } clientId)
                        return Results.Forbid();

                    // No If-Match, no write: a blind replace could erase progress another write just saved.
                    if (ifMatch.Count == 0)
                        return TypedResults.Problem(
                            detail: "Send the profile's ETag in If-Match: writes must be based on the data they change.",
                            statusCode: StatusCodes.Status428PreconditionRequired,
                            extensions: new Dictionary<string, object?> { ["code"] = "profiles.if_match_required" });

                    if (!DataVersionETag.TryParse(ifMatch, out var expectedDataVersion))
                        return Error.PreconditionFailed("profiles.invalid_if_match", "If-Match must be the ETag of the profile.").ToProblem();

                    var result = await mediator.Send(new ReplaceProfileDataCommand(profileId, clientId, expectedDataVersion, data), cancellationToken);

                    return result.ToHttpResult(response =>
                    {
                        httpContext.Response.Headers.ETag = DataVersionETag.Format(response.DataVersion);

                        return TypedResults.NoContent();
                    });
                })
                .RequireAuthorization(IdentityPolicies.PROFILES_WRITE)
                .WithName("profiles.data.replace")
                .WithSummary("Replace a profile's data (game servers)")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status412PreconditionFailed)
                .ProducesProblem(StatusCodes.Status428PreconditionRequired);
    }
}
