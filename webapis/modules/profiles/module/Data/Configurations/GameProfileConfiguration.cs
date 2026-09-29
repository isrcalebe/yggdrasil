using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using yggdrasil.Modules.Profiles.Domain;

namespace yggdrasil.Modules.Profiles.Data.Configurations;

public sealed class GameProfileConfiguration : IEntityTypeConfiguration<GameProfile>
{
    public void Configure(EntityTypeBuilder<GameProfile> builder)
    {
        builder.ToTable("profiles");

        builder.HasIndex(static profile => new
        {
            profile.AccountId,
            profile.GameId
        }).IsUnique();

        builder
            .Property(static profile => profile.DisplayName)
            .HasMaxLength(GameProfile.DISPLAY_NAME_MAX_LENGTH);

        builder
            .Property(static profile => profile.Data)
            .HasColumnType("jsonb");

        builder
            .Property(static profile => profile.DataVersion)
            .IsConcurrencyToken();
    }
}
