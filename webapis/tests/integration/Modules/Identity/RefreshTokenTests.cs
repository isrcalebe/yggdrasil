using System.Net;
using yggdrasil.Integration.Tests.Infrastructure;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class RefreshTokenTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task OfflineAccessIssuesARefreshToken()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        await game.SignInAsync(CancellationToken);

        var tokens = await game.LogInAsync("openid offline_access", CancellationToken);

        Assert.True(tokens.TryGetProperty("refresh_token", out _));
    }

    [Fact]
    public async Task WithoutOfflineAccessThereIsNoRefreshToken()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        await game.SignInAsync(CancellationToken);

        var tokens = await game.LogInAsync("openid", CancellationToken);

        Assert.False(tokens.TryGetProperty("refresh_token", out _));
    }

    [Fact]
    public async Task RefreshTokenRotatesOnUse()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        await game.SignInAsync(CancellationToken);
        var first = await game.LogInAsync("openid offline_access", CancellationToken);

        using var response = await game.RefreshAsync(first.GetProperty("refresh_token").GetString()!, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var second = await GameClient.ReadJsonAsync(response, CancellationToken);

        Assert.NotEqual(first.GetProperty("refresh_token").GetString(), second.GetProperty("refresh_token").GetString());
        Assert.Equal(
            GameClient.PayloadOf(first.GetProperty("access_token").GetString()!).GetProperty("sub").GetString(),
            GameClient.PayloadOf(second.GetProperty("access_token").GetString()!).GetProperty("sub").GetString());
    }

    [Fact]
    public async Task RedeemedRefreshTokenIsRejected()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        await game.SignInAsync(CancellationToken);
        var first = await game.LogInAsync("openid offline_access", CancellationToken);
        var refreshToken = first.GetProperty("refresh_token").GetString()!;

        using var rotated = await game.RefreshAsync(refreshToken, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);

        using var replayed = await game.RefreshAsync(refreshToken, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, replayed.StatusCode);
        Assert.Equal(Errors.InvalidGrant, (await GameClient.ReadJsonAsync(replayed, CancellationToken)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task ReplayRevokesTheRotatedRefreshTokenToo()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        await game.SignInAsync(CancellationToken);
        var first = await game.LogInAsync("openid offline_access", CancellationToken);
        var refreshToken = first.GetProperty("refresh_token").GetString()!;

        using var rotated = await game.RefreshAsync(refreshToken, CancellationToken);
        var second = await GameClient.ReadJsonAsync(rotated, CancellationToken);

        using var replayed = await game.RefreshAsync(refreshToken, CancellationToken);
        using var afterReplay = await game.RefreshAsync(second.GetProperty("refresh_token").GetString()!, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, afterReplay.StatusCode);
    }
}
