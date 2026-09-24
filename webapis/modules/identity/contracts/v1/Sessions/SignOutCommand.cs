using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Identity.Contracts.v1.Sessions;

public sealed record SignOutCommand : ICommand<Result>;
