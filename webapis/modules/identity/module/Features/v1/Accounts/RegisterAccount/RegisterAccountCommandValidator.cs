using FluentValidation;
using Microsoft.AspNetCore.Identity;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Features.v1.Accounts.RegisterAccount;

public sealed class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator(UserManager<Account> userManager)
    {
        ArgumentNullException.ThrowIfNull(userManager);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .CustomAsync(async (password, context, _) =>
            {
                foreach (var validator in userManager.PasswordValidators)
                {
                    var result = await validator.ValidateAsync(userManager, new Account(), password);

                    foreach (var error in result.Errors)
                        context.AddFailure(error.Description);
                }
            });
    }
}
