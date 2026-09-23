using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace yggdrasil.Persistence;

/// <summary>
/// Applies pending migrations of <typeparamref name="TContext"/> on startup when <c>Database:MigrateOnStartup</c>
/// is enabled. Meant for local development; production applies migrations through a migration bundle.
/// </summary>
internal sealed partial class DatabaseMigrator<TContext>(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DatabaseMigrator<TContext>> logger
) : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Database:MigrateOnStartup"))
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        await context.Database.MigrateAsync(cancellationToken);

        LogMigrated(typeof(TContext).Name);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(LogLevel.Information, "Applied pending migrations of {Context}")]
    private partial void LogMigrated(string context);
}
