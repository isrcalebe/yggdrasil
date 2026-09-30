using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Integration.Tests.Modules.Identity;
using yggdrasil.Modules.Games.Contracts.v1.Games;
using yggdrasil.Modules.Profiles.Contracts.v1.Profiles;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Integration.Tests.Modules.Profiles;

/// <summary>
/// Two games on the same Yggdrasil: neither one's tokens reach the other's profiles.
/// </summary>
public sealed class CrossGameAccessTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string my_game = GameClient.CLIENT_ID;

    private const string other_game = "other-game";

    [Fact]
    public async Task ServerCannotWriteAnotherGamesProfile()
    {
        var (_, other) = await registerTwoGamesAsync();
        using var player = new GameClient(Factory, my_game);
        await player.SignInAsync(CancellationToken);
        var playerToken = await accessTokenAsync(player);
        var profile = await getProfileAsync(player, playerToken);

        // The other game's server holds a valid profiles.write token, just not for this game.
        using var response = await replaceDataAsync(await serverTokenAsync(other), profile.ProfileId, """{"level": 99}""");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var unchanged = await getProfileAsync(player, playerToken);
        Assert.Equal("{}", unchanged.Data.GetRawText());
        Assert.Equal(1, unchanged.DataVersion);
    }

    [Fact]
    public async Task ServerStillWritesItsOwnGamesProfile()
    {
        var (mine, _) = await registerTwoGamesAsync();
        using var player = new GameClient(Factory, my_game);
        await player.SignInAsync(CancellationToken);
        var profile = await getProfileAsync(player, await accessTokenAsync(player));

        using var response = await replaceDataAsync(await serverTokenAsync(mine), profile.ProfileId, """{"level": 5}""");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task EachGameReadsOnlyItsOwnProfileOfThePlayer()
    {
        var (mine, other) = await registerTwoGamesAsync();

        using var inMyGame = new GameClient(Factory, my_game);
        await inMyGame.SignInAsync(CancellationToken);
        var myProfile = await getProfileAsync(inMyGame, await accessTokenAsync(inMyGame));

        // Same player, second game.
        using var inOtherGame = new GameClient(Factory, other_game);
        await inOtherGame.SignInExistingAsync(CancellationToken);
        var otherProfile = await getProfileAsync(inOtherGame, await accessTokenAsync(inOtherGame));

        Assert.Equal(mine.GameId, myProfile.GameId);
        Assert.Equal(other.GameId, otherProfile.GameId);
        Assert.NotEqual(myProfile.ProfileId, otherProfile.ProfileId);
    }

    private async Task<(RegisterGameResponse Mine, RegisterGameResponse Other)> registerTwoGamesAsync()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, "admin@yggdrasil.cc", CancellationToken);

        return (await registerAsync(admin, my_game), await registerAsync(admin, other_game));
    }

    private static async Task<RegisterGameResponse> registerAsync(SignedInAccount admin, string slug)
    {
        using var response = await admin.SendAsync(HttpMethod.Post, "/api/v1/games", JsonContent.Create(new RegisterGameCommand(slug, slug)), CancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RegisterGameResponse>(CancellationToken))!;
    }

    private static async Task<string> accessTokenAsync(GameClient game)
        => (await game.LogInAsync("openid profiles.read", CancellationToken)).GetProperty("access_token").GetString()!;

    private async Task<string> serverTokenAsync(RegisterGameResponse game)
    {
        using var response = await Client.PostAsync(new Uri("/connect/token", UriKind.Relative), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.ClientCredentials,
            [Parameters.ClientId] = game.ServerClientId,
            [Parameters.ClientSecret] = game.ServerClientSecret,
            [Parameters.Scope] = "profiles.write",
        }), CancellationToken);

        return (await GameClient.ReadJsonAsync(response, CancellationToken)).GetProperty("access_token").GetString()!;
    }

    private static async Task<ProfileResponse> getProfileAsync(GameClient game, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/profiles/me", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await game.Http.SendAsync(request, CancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(CancellationToken))!;
    }

    private Task<HttpResponseMessage> replaceDataAsync(string accessToken, Guid profileId, string data)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, new Uri($"/api/v1/profiles/{profileId}/data", UriKind.Relative))
        {
            Content = new StringContent(data, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.IfMatch.Add(EntityTagHeaderValue.Parse("\"1\""));

        return Client.SendAsync(request, CancellationToken);
    }
}
