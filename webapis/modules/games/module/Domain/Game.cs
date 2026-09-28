namespace yggdrasil.Modules.Games.Domain;

public sealed class Game
{
    public const int SLUG_MAX_LENGTH = 64;
    public const int NAME_MAX_LENGTH = 64;

    public const int CLIENT_ID_MAX_LENGTH = 100;

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>Stable, URL-friendly identifier (e.g. <c>my-game</c>), unique across the catalog.</summary>
    public string Slug { get; private set; }

    /// <summary>Display name shown to players.</summary>
    public string Name { get; private set; }

    public string ClientId { get; private init; }

    public string ServerClientId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public Game(string slug, string name, string clientId, string serverClientId, DateTimeOffset createdAt)
    {
        Slug = slug;
        Name = name;
        ClientId = clientId;
        ServerClientId = serverClientId;
        CreatedAt = createdAt;
    }
}
