using Mediator;
using Microsoft.AspNetCore.Identity;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts.v1.Administrators;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Features.v1.Administrators;


public sealed class GrantAdministratorCommandHandler(UserManager<Account> userManager) : ICommandHandler<GrantAdministratorCommand, Result>
{
    private static readonly Error account_not_found = Error.NotFound("identity.account_not_found", "The account does not exists.");

    public async ValueTask<Result> Handle(GrantAdministratorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var account = await userManager.FindByIdAsync(command.AccountId.ToString());

        if (account is null)
            return account_not_found;

        if (await userManager.IsInRoleAsync(account, Roles.ADMIN))
            return Result.Success();

        var result = await userManager.AddToRoleAsync(account, Roles.ADMIN);

        return result.Succeeded
            ? Result.Success()
            : throw new InvalidOperationException(string.Join(" ", result.Errors.Select(static error => error.Description)));
    }
}
