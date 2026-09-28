using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Modules.Identity.Administration;

public sealed partial class AdministratorSeeder(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AdministratorSeeder> logger
) : IHostedService
{
    public const string CONFIGURATION_SECTION = "Identity:Administrators";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var accountsIds = configuration.GetSection(CONFIGURATION_SECTION).Get<Guid[]>() ?? [];

        return accountsIds.Length == 0
            ? Task.CompletedTask
            : SeedAsync(accountsIds, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task SeedAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountIds);

        await using var scope = scopeFactory.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var accounts = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();

        if (!await roles.RoleExistsAsync(Roles.ADMIN))
            ensure(await roles.CreateAsync(new IdentityRole<Guid>(Roles.ADMIN)));

        foreach (var accountId in accountIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var account = await accounts.FindByIdAsync(accountId.ToString());

            if (account is null)
            {
                LogUnknownAccount(accountId);

                continue;
            }

            if (await accounts.IsInRoleAsync(account, Roles.ADMIN))
                continue;

            ensure(await accounts.AddToRoleAsync(account, Roles.ADMIN));
            LogPromoted(accountId);
        }
    }

    private static void ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(static error => error.Description)));
    }

    [LoggerMessage(LogLevel.Warning, "Account {AccountId} listed in Identity:Administrators does not exist")]
    private partial void LogUnknownAccount(Guid accountId);

    [LoggerMessage(LogLevel.Information, "Account {AccountId} promoted to administrator")]
    private partial void LogPromoted(Guid accountId);
}
