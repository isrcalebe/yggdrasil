using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;
using yggdrasil.Web.Results;
using yggdrasil.Web.Security;

namespace yggdrasil.Modules.Identity.Features.v1.Sessions.SignOut;

public static class SignOutEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapSignOutEndpoint()
            => self
                .MapDelete("/sessions/current", static async (IMediator mediator, CancellationToken cancellationToken) =>
                    (await mediator.Send(new SignOutCommand(), cancellationToken)).ToHttpResult())
                .RequireAntiforgeryToken()
                .WithName("identity.sessions.sign-out")
                .WithSummary("Sign out")
                .Produces(StatusCodes.Status204NoContent);
    }
}
