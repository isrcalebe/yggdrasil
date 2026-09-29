using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Games.Contracts.v1.Games;

public sealed record GetGameByClientIdQuery(string ClientId)
    : IQuery<Result<GameResponse>>;
