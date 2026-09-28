using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Modules.Identity.Administration;
using yggdrasil.Modules.Identity.Domain;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class AdministratorTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string admin_email = "admin@yggdrasil.cc";

    private const string player_email = "player@yggdrasil.cc";

    [Fact]
    public async Task AnonymousCallerIsUnauthorized()
    {
        var playerId = await SignedInAccount.RegisterAsync(Factory, player_email, CancellationToken);

        using var response = await Client.PutAsync(new Uri($"/api/v1/identity/administrators/{playerId}", UriKind.Relative), null, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlayerIsForbidden()
    {
        var playerId = await SignedInAccount.RegisterAsync(Factory, player_email, CancellationToken);
        using var player = await SignedInAccount.SignInAsync(Factory, playerId, player_email, CancellationToken);

        using var response = await player.SendAsync(HttpMethod.Put, $"/api/v1/identity/administrators/{playerId}", CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorGrantsTheRole()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, admin_email, CancellationToken);
        var playerId = await SignedInAccount.RegisterAsync(Factory, player_email, CancellationToken);

        using var granted = await admin.SendAsync(HttpMethod.Put, $"/api/v1/identity/administrators/{playerId}", CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, granted.StatusCode);

        // The role reaches the cookie on the next sign-in: the promoted player now passes the policy too.
        using var promoted = await SignedInAccount.SignInAsync(Factory, playerId, player_email, CancellationToken);
        using var again = await promoted.SendAsync(HttpMethod.Put, $"/api/v1/identity/administrators/{playerId}", CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
    }

    [Fact]
    public async Task GrantingAnUnknownAccountIsNotFound()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, admin_email, CancellationToken);

        using var response = await admin.SendAsync(HttpMethod.Put, $"/api/v1/identity/administrators/{Guid.CreateVersion7()}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AccountsListedInConfigurationArePromotedOnStartup()
    {
        var adminId = await SignedInAccount.RegisterAsync(Factory, admin_email, CancellationToken);

        // A second host on the same database, started with the account listed, as a deployment would be.
        await using var configured = Factory.WithWebHostBuilder(builder => builder.UseSetting($"{AdministratorSeeder.CONFIGURATION_SECTION}:0", adminId.ToString()));
        using var admin = await SignedInAccount.SignInAsync(configured, adminId, admin_email, CancellationToken);

        using var response = await admin.SendAsync(HttpMethod.Put, $"/api/v1/identity/administrators/{adminId}", CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SeederPromotesListedAccountsAndSkipsUnknownOnes()
    {
        var adminId = await SignedInAccount.RegisterAsync(Factory, admin_email, CancellationToken);

        await Factory.Services.GetRequiredService<AdministratorSeeder>().SeedAsync([adminId, Guid.CreateVersion7()], CancellationToken);

        await using var scope = Factory.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();

        Assert.True(await accounts.IsInRoleAsync((await accounts.FindByIdAsync(adminId.ToString()))!, "admin"));
    }
}
