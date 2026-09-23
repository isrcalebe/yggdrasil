using Microsoft.EntityFrameworkCore;

namespace yggdrasil.Persistence;

/// <summary>A module <see cref="DbContext"/> that owns one database schema.</summary>
public interface IModuleDbContext
{
    /// <summary>Database schema of the module, also holding its migrations history table.</summary>
    abstract static string Schema { get; }
}
