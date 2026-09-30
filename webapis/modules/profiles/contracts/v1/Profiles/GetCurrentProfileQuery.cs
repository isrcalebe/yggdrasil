using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Profiles.Contracts.v1.Profiles;

public sealed record GetCurrentProfileQuery(Guid AccountId, string ClientId)
    : IQuery<Result<ProfileResponse>>;
