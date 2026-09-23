using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Core.HealthChecks;

namespace yggdrasil.Persistence;

public static class PersistenceExtensions
{
    public const string CONNECTION_STRING_NAME = "Database";

    private const string migrations_history_table = "__ef_migrations_history";

    extension(IServiceCollection self)
    {
        /// <summary>
        /// Registers the module <typeparamref name="TContext"/> on the shared database, with its own schema and
        /// migrations history, plus a readiness health check and (opt-in) migrations on startup.
        /// </summary>
        public IServiceCollection AddModuleDbContext<TContext>()
            where TContext : ModuleDbContext<TContext>, IModuleDbContext
        {
            // Resolved lazily so configuration overrides applied after registration (e.g. tests) are honored.
            self.AddDbContext<TContext>(static (services, options) =>
            {
                var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString(CONNECTION_STRING_NAME)
                    ?? throw new InvalidOperationException($"Connection string '{CONNECTION_STRING_NAME}' is not configured.");

                options
                    .UseNpgsql(connectionString, static npgsql => npgsql.MigrationsHistoryTable(migrations_history_table, TContext.Schema))
                    .UseSnakeCaseNamingConvention();
            });

            self.AddHealthChecks()
                .AddDbContextCheck<TContext>($"{TContext.Schema}-database", tags: [HealthCheckTags.READY]);

            self.AddHostedService<DatabaseMigrator<TContext>>();

            return self;
        }
    }
}
