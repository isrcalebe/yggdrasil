using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using yggdrasil.Modules.Identity.Contracts;
using yggdrasil.Modules.Identity.Contracts.v1.Administrators;
using yggdrasil.Web.Results;
using yggdrasil.Web.Security;

namespace yggdrasil.Modules.Identity.Features.v1.Administrators;

public static class GrantAdministratorEndpoint
{
    extension(IEndpointRouteBuilder self)
    {
        internal RouteHandlerBuilder MapGrantAdministratorEndpoint()
            => self.MapPut("/administrators/{accountId:guid}", static async (Guid accountId, IMediator mediator, CancellationToken cancellationToken) =>
                (await mediator.Send(new GrantAdministratorCommand(accountId), cancellationToken)).ToHttpResult())
                .RequireAuthorization(IdentityPolicies.ADMIN)
                .RequireAntiforgeryToken()
                .WithName("identity.administrators.grant")
                .WithSummary("Grant administrator to an account")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
