using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Integration.Tests.Modules.Identity;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Games.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Integration.Tests.Modules.Games;

public sealed class GameRegistrationTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string admin_email = "admin@yggdrasil.cc";

    private const string player_email = "player@yggdrasil.cc";

    private const string games = "/api/v1/games";

    [Fact]
    public async Task AdministratorRegistersAGameWithItsClients()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, admin_email, CancellationToken);

        using var response = await registerAsync(admin, "my-game", "My Game");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var registered = (await response.Content.ReadFromJsonAsync<RegisterGameResponse>(CancellationToken))!;
        Assert.Equal("my-game", registered.ClientId);
        Assert.Equal("my-game.server", registered.ServerClientId);

        await using var scope = Factory.Services.CreateAsyncScope();
        var game = await scope.ServiceProvider.GetRequiredService<GamesModuleDbContext>().Games.SingleAsync(CancellationToken);
        Assert.Equal(registered.GameId, game.Id);
        Assert.Equal(registered.ClientId, game.ClientId);

        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var client = (await applications.FindByClientIdAsync(registered.ClientId, CancellationToken))!;
        Assert.Equal(ClientTypes.Public, await applications.GetClientTypeAsync(client, CancellationToken));
        Assert.Equal(ApplicationTypes.Native, await applications.GetApplicationTypeAsync(client, CancellationToken));
        Assert.True(await applications.HasRequirementAsync(client, Requirements.Features.ProofKeyForCodeExchange, CancellationToken));

        var server = (await applications.FindByClientIdAsync(registered.ServerClientId, CancellationToken))!;
        Assert.Equal(ClientTypes.Confidential, await applications.GetClientTypeAsync(server, CancellationToken));
    }

    [Fact]
    public async Task ServerSecretWorksAndIsOnlyStoredHashed()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, admin_email, CancellationToken);
        using var response = await registerAsync(admin, "my-game", "My Game");
        var registered = (await response.Content.ReadFromJsonAsync<RegisterGameResponse>(CancellationToken))!;

        using var token = await Client.PostAsync(new Uri("/connect/token", UriKind.Relative), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.ClientCredentials,
            [Parameters.ClientId] = registered.ServerClientId,
            [Parameters.ClientSecret] = registered.ServerClientSecret,
        }), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, token.StatusCode);

        // Nothing can show the secret again: the database only has a hash of it.
        await using var scope = Factory.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var server = (await applications.FindByClientIdAsync(registered.ServerClientId, CancellationToken))!;
        var stored = ((OpenIddictEntityFrameworkCoreApplication<Guid>)server).ClientSecret;

        Assert.False(string.IsNullOrEmpty(stored));
        Assert.DoesNotContain(registered.ServerClientSecret, stored, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SlugIsTaken()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, admin_email, CancellationToken);
        using var first = await registerAsync(admin, "my-game", "My Game");

        using var second = await registerAsync(admin, "my-game", "Another Game");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("my--game")]
    [InlineData("-my-game")]
    [InlineData("my_game")]
    public async Task InvalidSlugIsRejected(string slug)
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, admin_email, CancellationToken);

        using var response = await registerAsync(admin, slug, "My Game");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlayerIsForbidden()
    {
        var playerId = await SignedInAccount.RegisterAsync(Factory, player_email, CancellationToken);
        using var player = await SignedInAccount.SignInAsync(Factory, playerId, player_email, CancellationToken);

        using var response = await registerAsync(player, "my-game", "My Game");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static Task<HttpResponseMessage> registerAsync(SignedInAccount account, string slug, string name)
        => account.SendAsync(HttpMethod.Post, games, JsonContent.Create(new RegisterGameCommand(slug, name)), CancellationToken);
}
