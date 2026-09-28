using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using yggdrasil.Modules.Games.Domain;

namespace yggdrasil.Modules.Games.Data.Configurations;

public sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");

        builder.Property(static game => game.Slug).HasMaxLength(Game.SLUG_MAX_LENGTH);
        builder.HasIndex(static game => game.Slug).IsUnique();

        builder.Property(static game => game.Name).HasMaxLength(Game.NAME_MAX_LENGTH);
    }
}
