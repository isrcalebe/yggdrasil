using Mediator;
using Microsoft.AspNetCore.Identity;
using yggdrasil.Core.Results;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Features.v1.Sessions.SignOut;

public sealed class SignOutCommandHandler(SignInManager<Account> signInManager)
    : ICommandHandler<SignOutCommand, Result>
{
    public async ValueTask<Result> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();

        return Result.Success();
    }
}
