using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Modules.Identity.Domain;
using yggdrasil.Persistence;

namespace yggdrasil.Modules.Identity.Features.v1.Accounts.RegisterAccount;

public sealed class RegisterAccountCommandHandler(UserManager<Account> userManager)
    : ICommandHandler<RegisterAccountCommand, Result<RegisterAccountResponse>>
{
    private static readonly Error email_taken = Error.Conflict("identity.email_taken", "An account with this email already exists.");

    public async ValueTask<Result<RegisterAccountResponse>> Handle(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email = command.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
            return email_taken;

        var account = new Account { UserName = email, Email = email };
        IdentityResult result;

        try
        {
            result = await userManager.CreateAsync(account, command.Password);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            return email_taken;
        }

        if (result.Succeeded)
            return new RegisterAccountResponse(account.Id);

        if (result.Errors.Any(static error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            return email_taken;

        return Error.Failure("identity.registration_failed", string.Join(" ", result.Errors.Select(static error => error.Description)));
    }
}
