using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Identity.Contracts.v1.Administrators;

public sealed record GrantAdministratorCommand(Guid AccountId) : ICommand<Result>;
