using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Identity.Contracts.v1.Sessions;

public sealed record SignInCommand(string Email, string Password, string? ReturnUrl)
    : ICommand<Result<SignInResponse>>;
