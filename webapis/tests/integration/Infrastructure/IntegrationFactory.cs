using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace yggdrasil.Integration.Tests.Infrastructure;

/// <summary>The real host, pointed at the Testcontainers database and applying every module migration on startup.</summary>
public sealed class IntegrationFactory(PostgresFixture database) : WebApplicationFactory<Program>
{
    public PostgresFixture Database { get; } = database;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .UseSetting("ConnectionStrings:Database", Database.ConnectionString)
            .UseSetting("Database:MigrateOnStartup", "true");
    }
}
