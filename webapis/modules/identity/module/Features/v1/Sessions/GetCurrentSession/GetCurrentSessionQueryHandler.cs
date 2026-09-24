using Mediator;
using Microsoft.AspNetCore.Identity;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Features.v1.Sessions.GetCurrentSession;

public sealed class GetCurrentSessionQueryHandler(UserManager<Account> userManager) : IQueryHandler<GetCurrentSessionQuery, Result<CurrentSessionResponse>>
{
    public async ValueTask<Result<CurrentSessionResponse>> Handle(GetCurrentSessionQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var account = await userManager.FindByIdAsync(query.AccountId.ToString());

        return account is null
            ? Error.Unauthorized("identity.session_invalid", "The session no longer matches an account.")
            : new CurrentSessionResponse(account.Id, account.Email!);
    }
}
