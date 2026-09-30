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
/// Game servers write progression data; players (game clients) can only read it.
/// </summary>
public sealed class ProfileDataTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string current_profile = "/api/v1/profiles/me";

    [Fact]
    public async Task ServerReplacesTheDataAndGetsTheNextETag()
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;

        using var response = await replaceDataAsync(arranged.ServerToken, arranged.ProfileId, """{"level": 5}""", "\"1\"");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("\"2\"", response.Headers.ETag!.Tag);

        // The player sees the new data, at the new version.
        using var read = await getProfileAsync(game, arranged.PlayerToken);
        var profile = (await read.Content.ReadFromJsonAsync<ProfileResponse>(CancellationToken))!;
        Assert.Equal(5, profile.Data.GetProperty("level").GetInt32());
        Assert.Equal(2, profile.DataVersion);
        Assert.Equal("\"2\"", read.Headers.ETag!.Tag);
    }

    [Fact]
    public async Task WriteBasedOnAnOldETagIsPreconditionFailed()
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;

        using var first = await replaceDataAsync(arranged.ServerToken, arranged.ProfileId, """{"level": 5}""", "\"1\"");
        using var second = await replaceDataAsync(arranged.ServerToken, arranged.ProfileId, """{"level": 4}""", "\"1\"");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed, second.StatusCode);
    }

    [Fact]
    public async Task WriteWithoutIfMatchIsPreconditionRequired()
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;

        using var response = await replaceDataAsync(arranged.ServerToken, arranged.ProfileId, """{"level": 5}""", ifMatch: null);

        Assert.Equal((HttpStatusCode)428, response.StatusCode);
    }

    [Theory]
    [InlineData("""["not", "an", "object"]""")]
    [InlineData("\"just a string\"")]
    public async Task DataMustBeAJsonObject(string data)
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;

        using var response = await replaceDataAsync(arranged.ServerToken, arranged.ProfileId, data, "\"1\"");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DataOverTheSizeLimitIsRejected()
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;
        var tooLarge = $$"""{"blob": "{{new string('x', 65 * 1024)}}"}""";

        using var response = await replaceDataAsync(arranged.ServerToken, arranged.ProfileId, tooLarge, "\"1\"");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlayersCannotWriteTheirOwnProgress()
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;

        // The game client's token only carries profiles.read.
        using var response = await replaceDataAsync(arranged.PlayerToken, arranged.ProfileId, """{"level": 99}""", "\"1\"");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnknownProfileIsNotFound()
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;

        using var response = await replaceDataAsync(arranged.ServerToken, Guid.CreateVersion7(), """{"level": 5}""", "\"1\"");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GameClientCannotAskForTheWriteScope()
    {
        var arranged = await arrangeAsync();
        using var game = arranged.Game;
        var (_, challenge) = GameClient.Pkce();

        using var response = await game.Http.GetAsync(GameClient.AuthorizeUri(challenge, "openid profiles.write"), CancellationToken);

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
    }

    /// <summary>
    /// A registered game, a player who logged in through it (their profile exists, at version 1) and a token of the game's
    /// server.
    /// </summary>
    private async Task<Arranged> arrangeAsync()
    {
        using var admin = await SignedInAccount.SignInAsAdministratorAsync(Factory, "admin@yggdrasil.cc", CancellationToken);
        using var registration = await admin.SendAsync(HttpMethod.Post, "/api/v1/games", JsonContent.Create(new RegisterGameCommand(GameClient.CLIENT_ID, "Test Game")), CancellationToken);
        var registered = (await registration.Content.ReadFromJsonAsync<RegisterGameResponse>(CancellationToken))!;

        var game = new GameClient(Factory);
        await game.SignInAsync(CancellationToken);
        var playerToken = (await game.LogInAsync("openid profiles.read", CancellationToken)).GetProperty("access_token").GetString()!;

        using var read = await getProfileAsync(game, playerToken);
        var profile = (await read.Content.ReadFromJsonAsync<ProfileResponse>(CancellationToken))!;

        using var token = await Client.PostAsync(new Uri("/connect/token", UriKind.Relative), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.ClientCredentials,
            [Parameters.ClientId] = registered.ServerClientId,
            [Parameters.ClientSecret] = registered.ServerClientSecret,
            [Parameters.Scope] = "profiles.write",
        }), CancellationToken);
        var serverToken = (await GameClient.ReadJsonAsync(token, CancellationToken)).GetProperty("access_token").GetString()!;

        return new Arranged(game, playerToken, profile.ProfileId, serverToken);
    }

    private static Task<HttpResponseMessage> getProfileAsync(GameClient game, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(current_profile, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return game.Http.SendAsync(request, CancellationToken);
    }

    private Task<HttpResponseMessage> replaceDataAsync(string accessToken, Guid profileId, string data, string? ifMatch)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, new Uri($"/api/v1/profiles/{profileId}/data", UriKind.Relative))
        {
            Content = new StringContent(data, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (ifMatch is not null)
            request.Headers.IfMatch.Add(EntityTagHeaderValue.Parse(ifMatch));

        return Client.SendAsync(request, CancellationToken);
    }

    private sealed record Arranged(GameClient Game, string PlayerToken, Guid ProfileId, string ServerToken);
}
