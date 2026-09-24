using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;
using yggdrasil.Web.Results;

namespace yggdrasil.Modules.Identity.Features.v1.Sessions.GetCurrentSession;

public static class GetCurrentSessionEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapGetCurrentSessionEndpoint()
            => self
                .MapGet("/sessions/current", static async (ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var accountId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

                    return (await mediator.Send(new GetCurrentSessionQuery(accountId), cancellationToken)).ToHttpResult();
                })
                .RequireAuthorization()
                .WithName("identity.sessions.current")
                .WithSummary("Get the signed-in account")
                .Produces<CurrentSessionResponse>()
                .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
