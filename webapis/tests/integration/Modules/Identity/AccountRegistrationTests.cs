using System.Net;
using System.Net.Http.Json;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class AccountRegistrationTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string password = "correct-horse-battery-9A";

    private static readonly Uri accounts = new("/api/v1/identity/accounts", UriKind.Relative);

    [Fact]
    public async Task DuplicateEmailIgnoringCaseIsConflict()
    {
        using var first = await Client.PostAsJsonAsync(accounts, new RegisterAccountCommand("player@yggdrasil.cc", password), CancellationToken);
        using var second = await Client.PostAsJsonAsync(accounts, new RegisterAccountCommand("Player@Yggdrasil.cc", password), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        Assert.Contains("identity.email_taken", await second.Content.ReadAsStringAsync(CancellationToken), StringComparison.Ordinal);
    }
}
