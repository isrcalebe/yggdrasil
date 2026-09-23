using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace yggdrasil.Integration.Tests.Infrastructure;

/// <summary>PostgreSQL container shared by every integration test, with a fast data reset between tests.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string migrations_history_table = "__ef_migrations_history";

    private static readonly string[] excluded_schemas = ["public", "pg_catalog", "information_schema"];

    // Same image as compose.yaml.
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    private Respawner? respawner;

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    /// <summary>
    /// Deletes the data of every module table, keeping the schema and the migrations history. Must run after a host
    /// started (and applied the migrations), since the tables to clean are discovered on the first call.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        if (respawner is null)
        {
            // Respawn refuses a database without tables, which is the case while no module has migrations.
            if (!await hasModuleTablesAsync(connection))
                return;

            respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToExclude = [.. excluded_schemas],
                TablesToIgnore = [new Table(migrations_history_table)],
            });
        }

        await respawner.ResetAsync(connection);
    }

    private static async Task<bool> hasModuleTablesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_type = 'BASE TABLE' AND table_schema <> ALL(@excluded) AND table_name <> @history)
            """,
            connection);

        command.Parameters.AddWithValue("excluded", excluded_schemas);
        command.Parameters.AddWithValue("history", migrations_history_table);

        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
