using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Identity.Contracts.v1.Accounts;

public sealed record RegisterAccountCommand(string Email, string Password)
    : ICommand<Result<RegisterAccountResponse>>;
