using Mediator;
using Microsoft.AspNetCore.Identity;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Features.v1.Accounts.RegisterAccount;

public sealed class RegisterAccountCommandHandler(UserManager<Account> userManager)
    : ICommandHandler<RegisterAccountCommand, Result<RegisterAccountResponse>>
{
    private static readonly Error email_taken = Error.Conflict("identity.email_taken", "An account with this email already exists.");

    public async ValueTask<Result<RegisterAccountResponse>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await userManager.FindByEmailAsync(command.Email) is not null)
            return email_taken;

        var account = new Account { UserName = command.Email, Email = command.Email };
        var result = await userManager.CreateAsync(account, command.Password);

        if (result.Succeeded)
            return new RegisterAccountResponse(account.Id);

        if (result.Errors.Any(static error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            return email_taken;

        return Error.Failure("identity.registration_failed", string.Join(" ", result.Errors.Select(static error => error.Description)));
    }
}
