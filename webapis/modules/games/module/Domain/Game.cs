namespace yggdrasil.Modules.Games.Domain;

public sealed class Game
{
    public const int SLUG_MAX_LENGTH = 64;
    public const int NAME_MAX_LENGTH = 64;

    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>Stable, URL-friendly identifier (e.g. <c>my-game</c>), unique across the catalog.</summary>
    public string Slug { get; private set; }

    /// <summary>Display name shown to players.</summary>
    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public Game(string slug, string name, DateTimeOffset createdAt)
    {
        Slug = slug;
        Name = name;
        CreatedAt = createdAt;
    }
}
