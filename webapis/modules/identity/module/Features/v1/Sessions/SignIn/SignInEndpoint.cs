using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;
using yggdrasil.Web.Results;
using yggdrasil.Web.Security;

namespace yggdrasil.Modules.Identity.Features.v1.Sessions.SignIn;

public static class SignInEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapSignInEndpoint()
            => self
                .MapPost("/sessions", static async (SignInCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                    (await mediator.Send(command, cancellationToken)).ToHttpResult())
                .RequireAntiforgeryToken()
                .WithName("identity.sessions.sign-in")
                .WithSummary("Sign in with Yggdrasil ID")
                .Produces<SignInResponse>()
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
