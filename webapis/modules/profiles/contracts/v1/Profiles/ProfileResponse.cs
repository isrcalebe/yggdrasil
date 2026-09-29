using System.Text.Json;

namespace yggdrasil.Modules.Profiles.Contracts.v1.Profiles;

/// <summary>
/// A game profile; <c>Data</c> is the game's own JSON, embedded as is.
/// </summary>
public sealed record ProfileResponse(
    Guid ProfileId,
    Guid GameId,
    string DisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastPlayedAt,
    JsonElement Data,
    int DataVersion);
