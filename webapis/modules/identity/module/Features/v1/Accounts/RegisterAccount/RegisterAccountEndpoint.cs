using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Web.Results;

namespace yggdrasil.Modules.Identity.Features.v1.Accounts.RegisterAccount;

public static class RegisterAccountEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapRegisterAccountEndpoint()
            => self
                .MapPost("/accounts", static async (RegisterAccountCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                    (await mediator.Send(command, cancellationToken)).ToHttpResult(static response => TypedResults.Created((string?)null, response)))
                .WithName("identity.accounts.register")
                .WithSummary("Register a Yggdrasil ID")
                .Produces<RegisterAccountResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
