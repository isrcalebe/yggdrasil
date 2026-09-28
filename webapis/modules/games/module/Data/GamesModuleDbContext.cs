using Microsoft.EntityFrameworkCore;
using yggdrasil.Modules.Games.Domain;
using yggdrasil.Persistence;

namespace yggdrasil.Modules.Games.Data;

public sealed class GamesModuleDbContext(DbContextOptions<GamesModuleDbContext> options)
    : DbContext(options), IModuleDbContext
{
    public static string Schema => GamesModule.NAME;

    public DbSet<Game> Games => Set<Game>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyModuleConventions<GamesModuleDbContext>();
}
