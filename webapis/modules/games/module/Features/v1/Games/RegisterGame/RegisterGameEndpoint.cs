using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Identity.Contracts;
using yggdrasil.Web.Results;
using yggdrasil.Web.Security;

namespace yggdrasil.Modules.Games.Features.v1.Games.RegisterGame;

public static class RegisterGameEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapRegisterGameEndpoint()
            => self
                .MapPost("/", static async (RegisterGameCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                    (await mediator.Send(command, cancellationToken)).ToHttpResult(static response => TypedResults.Created((string?)null, response)))
                .RequireAuthorization(IdentityPolicies.ADMIN)
                .RequireAntiforgeryToken()
                .WithName("games.register")
                .WithSummary("Register a game and create its OAuth clients")
                .Produces<RegisterGameResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
