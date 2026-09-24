using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Identity.Contracts.v1.Sessions;

public sealed record GetCurrentSessionQuery(Guid AccountId)
    : IQuery<Result<CurrentSessionResponse>>;
