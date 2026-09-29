using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Integration.Tests.Modules.Identity;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Profiles.Contracts.v1.Profiles;
using yggdrasil.Modules.Profiles.Data;

namespace yggdrasil.Integration.Tests.Modules.Profiles;

/// <summary>A game reads the signed-in player's profile with the access token from "Login with Yggdrasil ID".</summary>
public sealed class CurrentProfileTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string current_profile = "/api/v1/profiles/me";

    [Fact]
    public async Task FirstAccessCreatesTheProfile()
    {
        var gameId = await registerGameAsync();
        using var game = new GameClient(Factory);
        var accessToken = await logInAsync(game, "openid profiles.read");

        using var response = await getProfileAsync(game, accessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<ProfileResponse>(CancellationToken))!;
        Assert.Equal(gameId, profile.GameId);
        Assert.StartsWith("Player-", profile.DisplayName, StringComparison.Ordinal);
        Assert.Equal("{}", profile.Data.GetRawText());
        Assert.Equal(1, profile.DataVersion);
    }

    [Fact]
    public async Task LaterAccessesReturnTheSameProfile()
    {
        await registerGameAsync();
        using var game = new GameClient(Factory);
        var accessToken = await logInAsync(game, "openid profiles.read");

        using var first = await getProfileAsync(game, accessToken);
        using var second = await getProfileAsync(game, accessToken);

        Assert.Equal(
            (await first.Content.ReadFromJsonAsync<ProfileResponse>(CancellationToken))!.ProfileId,
            (await second.Content.ReadFromJsonAsync<ProfileResponse>(CancellationToken))!.ProfileId);

        await using var scope = Factory.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ProfilesModuleDbContext>().Profiles.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task TokenWithoutTheScopeIsForbidden()
    {
        await registerGameAsync();
        using var game = new GameClient(Factory);
        var accessToken = await logInAsync(game, "openid");

        using var response = await getProfileAsync(game, accessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequestWithoutTokenIsUnauthorized()
    {
        using var response = await Client.GetAsync(new Uri(current_profile, UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SessionCookieIsNotAnAccessToken()
    {
        await registerGameAsync();
        using var game = new GameClient(Factory);
        await game.SignInAsync(CancellationToken);

        // Signed in on the website, but no bearer token: the browser session never grants API scopes.
        using var response = await game.Http.GetAsync(new Uri(current_profile, UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Registers the game the way an administrator does, so its client is <see cref="GameClient.CLIENT_ID"/>.</summary>
    private async Task<Guid> registerGameAsync()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, "admin@yggdrasil.cc", CancellationToken);
        using var response = await admin.SendAsync(HttpMethod.Post, "/api/v1/games", JsonContent.Create(new RegisterGameCommand(GameClient.CLIENT_ID, "Test Game")), CancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RegisterGameResponse>(CancellationToken))!.GameId;
    }

    private static async Task<string> logInAsync(GameClient game, string scope)
    {
        await game.SignInAsync(CancellationToken);
        var tokens = await game.LogInAsync(scope, CancellationToken);

        return tokens.GetProperty("access_token").GetString()!;
    }

    private static Task<HttpResponseMessage> getProfileAsync(GameClient game, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(current_profile, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return game.Http.SendAsync(request, CancellationToken);
    }
}
