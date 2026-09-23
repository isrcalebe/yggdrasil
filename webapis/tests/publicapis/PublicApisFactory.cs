using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace yggdrasil.PublicApis.Tests;

/// <summary>
/// Host without a database: nothing is migrated and the connection string points to a closed port,
/// so tests never depend on the local compose environment.
/// </summary>
public sealed class PublicApisFactory : WebApplicationFactory<Program>
{
    public const string UNREACHABLE_DATABASE = "Host=127.0.0.1;Port=1;Database=yggdrasil;Username=yggdrasil;Password=yggdrasil;Timeout=1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .UseSetting("ConnectionStrings:Database", UNREACHABLE_DATABASE)
            .UseSetting("Database:MigrateOnStartup", "false");
    }
}
