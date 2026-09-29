namespace yggdrasil.Modules.Games.Contracts.v1.Games;

public sealed record GameResponse(Guid GameId, string Slug, string Name);
