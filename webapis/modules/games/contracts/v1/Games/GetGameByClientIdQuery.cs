using Mediator;
using yggdrasil.Core.Results;

namespace yggdrasil.Modules.Games.Contracts.v1.Games;

/// <summary>
/// The game an OAuth client belongs to: either the game's own client or its server's. Sent by other modules; not exposed
/// over HTTP.
/// </summary>
public sealed record GetGameByClientIdQuery(string ClientId)
    : IQuery<Result<GameResponse>>;
