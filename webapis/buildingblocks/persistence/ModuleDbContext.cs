using Microsoft.EntityFrameworkCore;

namespace yggdrasil.Persistence;

/// <summary>A module <see cref="DbContext"/> that owns one database schema.</summary>
public interface IModuleDbContext
{
    /// <summary>Database schema of the module, also holding its migrations history table.</summary>
    abstract static string Schema { get; }
}

/// <summary>
/// Base <see cref="DbContext"/> of a module: every table lives in <typeparamref name="TSelf"/>'s schema and entity
/// configurations are picked up from the module assembly.
/// </summary>
public abstract class ModuleDbContext<TSelf>(DbContextOptions options) : DbContext(options)
    where TSelf : ModuleDbContext<TSelf>, IModuleDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(TSelf.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
