namespace yggdrasil.Modules.Profiles.Domain;

/// <summary>
/// A player's profile in one game (the SDKs expose it as <c>YggdrasilGameProfile</c>). The account and the game live in
/// other modules: they are referenced by id only, without foreign keys across schemas.
/// </summary>
public sealed class GameProfile
{
    public const int DISPLAY_NAME_MAX_LENGTH = 32;

    /// <summary>
    /// Data of a profile the game has not written to yet.
    /// </summary>
    public const string EMPTY_DATA = "{}";

    public const int DATA_MAX_BYTES = 64 * 1024;

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public Guid AccountId { get; private init; }

    public Guid GameId { get; private init; }

    /// <summary>
    /// Name shown to other players of this game.
    /// </summary>
    public string DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset LastPlayedAt { get; private set; }

    /// <summary>
    /// JSON the game defines and owns (progress, settings...), stored as <c>jsonb</c>.
    /// </summary>
    public string Data { get; private set; } = EMPTY_DATA;

    /// <summary>
    /// Incremented on every change of <see cref="Data"/>, and only then: the concurrency token behind the <c>ETag</c>, so a
    /// write based on stale data fails instead of overwriting newer progress.
    /// </summary>
    public int DataVersion { get; private set; } = 1;

    public GameProfile(Guid accountId, Guid gameId, string displayName, DateTimeOffset createdAt)
    {
        AccountId = accountId;
        GameId = gameId;
        DisplayName = displayName;
        CreatedAt = createdAt;
        LastPlayedAt = createdAt;
    }

    public void ReplaceData(string data)
    {
        Data = data;
        DataVersion++;
    }
}
