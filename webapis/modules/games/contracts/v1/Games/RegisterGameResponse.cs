namespace yggdrasil.Modules.Games.Contracts.v1.Games;

/// <summary>
/// The server client secret is shown only in this response: store it in the game server's configuration now.
/// </summary>
public sealed record RegisterGameResponse(Guid GameId, string Slug, string ClientId, string ServerClientId, string ServerClientSecret);
