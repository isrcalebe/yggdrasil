using Mediator;
using Microsoft.AspNetCore.Identity;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Features.v1.Sessions.SignIn;

public sealed class SignInCommandHandler(UserManager<Account> userManager, SignInManager<Account> signInManager)
    : ICommandHandler<SignInCommand, Result<SignInResponse>>
{
    private static readonly Error invalid_credentials = Error.Unauthorized("identity.invalid_credentials", "Invalid email or password.");

    private static readonly Error locked_out = Error.Forbidden("identity.locked_out", "Too many failed attempts. Try again later.");

    public async ValueTask<Result<SignInResponse>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var account = await userManager.FindByEmailAsync(command.Email.Trim());

        if (account is null)
        {
            // Hash anyway, so unknown emails take as long as wrong passwords and cannot be told apart by timing.
            userManager.PasswordHasher.HashPassword(new Account(), command.Password);

            return invalid_credentials;
        }

        var result = await signInManager.PasswordSignInAsync(account, command.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
            return new SignInResponse(command.ReturnUrl ?? "/");

        return result.IsLockedOut ? locked_out : invalid_credentials;
    }
}
