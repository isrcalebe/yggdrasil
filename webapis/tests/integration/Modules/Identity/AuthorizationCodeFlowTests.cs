using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using yggdrasil.Integration.Tests.Infrastructure;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class AuthorizationCodeFlowTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task SignedInPlayerGetsACodeTheGameRedeemsForTokens()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        await game.SignInAsync(CancellationToken);
        var (verifier, challenge) = GameClient.Pkce();

        using var authorize = await game.Http.GetAsync(GameClient.AuthorizeUri(challenge), CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, authorize.StatusCode);
        var callback = authorize.Headers.Location!;
        Assert.StartsWith(GameClient.REDIRECT_URI, callback.ToString(), StringComparison.Ordinal);

        var query = QueryHelpers.ParseQuery(callback.Query);
        Assert.Equal("xyz", query["state"].ToString());

        using var token = await game.PostTokenAsync(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.AuthorizationCode,
            [Parameters.Code] = query["code"].ToString(),
            [Parameters.RedirectUri] = GameClient.REDIRECT_URI,
            [Parameters.CodeVerifier] = verifier,
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, token.StatusCode);

        var tokens = await GameClient.ReadJsonAsync(token, CancellationToken);
        Assert.Equal("Bearer", tokens.GetProperty("token_type").GetString());

        var identity = GameClient.PayloadOf(tokens.GetProperty("id_token").GetString()!);
        Assert.Equal(GameClient.EMAIL, identity.GetProperty("email").GetString());
        Assert.True(Guid.TryParse(identity.GetProperty("sub").GetString(), out _));

        var access = GameClient.PayloadOf(tokens.GetProperty("access_token").GetString()!);
        Assert.Equal(identity.GetProperty("sub").GetString(), access.GetProperty("sub").GetString());
    }

    [Fact]
    public async Task AnonymousPlayerIsSentToTheLoginPage()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        var (_, challenge) = GameClient.Pkce();

        using var response = await game.Http.GetAsync(GameClient.AuthorizeUri(challenge), CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("/login?returnUrl=", location, StringComparison.Ordinal);
        Assert.StartsWith("/connect/authorize?", Uri.UnescapeDataString(location["/login?returnUrl=".Length..]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizationWithoutPkceIsRejected()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        await game.SignInAsync(CancellationToken);

        using var response = await game.Http.GetAsync(new Uri(
            $"/connect/authorize?client_id={GameClient.CLIENT_ID}&redirect_uri={Uri.EscapeDataString(GameClient.REDIRECT_URI)}&response_type=code&scope=openid&state=xyz",
            UriKind.Relative), CancellationToken);

        // Rejected before the redirect_uri is trusted, so the error is rendered here instead of sent to the game.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.Contains("'code_challenge' parameter is missing", await response.Content.ReadAsStringAsync(CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnregisteredRedirectUriIsRejected()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);
        var (_, challenge) = GameClient.Pkce();

        using var response = await game.Http.GetAsync(new Uri(
            $"/connect/authorize?client_id={GameClient.CLIENT_ID}&redirect_uri={Uri.EscapeDataString("https://evil.example/callback")}&response_type=code&scope=openid&code_challenge={challenge}&code_challenge_method=S256",
            UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
