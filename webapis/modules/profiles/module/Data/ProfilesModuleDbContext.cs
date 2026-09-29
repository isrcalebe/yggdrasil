using Microsoft.EntityFrameworkCore;
using yggdrasil.Modules.Profiles.Domain;
using yggdrasil.Persistence;

namespace yggdrasil.Modules.Profiles.Data;

public sealed class ProfilesModuleDbContext(DbContextOptions<ProfilesModuleDbContext> options)
    : DbContext(options), IModuleDbContext
{
    public static string Schema => ProfilesModule.NAME;

    public DbSet<GameProfile> Profiles => Set<GameProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyModuleConventions<ProfilesModuleDbContext>();
}
