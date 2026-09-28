using System.Buffers.Text;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Modules.Identity.Contracts.v1.Antiforgery;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class AuthorizationCodeFlowTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string client_id = "test-game";

    private const string email = "player@yggdrasil.cc";

    private const string password = "correct-horse-battery-9A";

    // Registered without a port: a native client may then use any loopback port (RFC 8252 §7.3), since a game
    // listens on whichever port is free.
    private static readonly Uri registered_redirect_uri = new("http://127.0.0.1/callback");

    private const string game_redirect_uri = "http://127.0.0.1:54321/callback";

    // Games must be able to follow redirects themselves, so the test client does not.
    private readonly HttpClient game = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task SignedInPlayerGetsACodeTheGameRedeemsForTokens()
    {
        await registerGameAsync();
        await signInAsync();
        var (verifier, challenge) = pkce();

        using var authorize = await game.GetAsync(authorizeUri(challenge), CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, authorize.StatusCode);
        var callback = authorize.Headers.Location!;
        Assert.StartsWith(game_redirect_uri, callback.ToString(), StringComparison.Ordinal);

        var query = QueryHelpers.ParseQuery(callback.Query);
        Assert.Equal("xyz", query["state"].ToString());

        using var token = await game.PostAsync(new Uri("/connect/token", UriKind.Relative), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.AuthorizationCode,
            [Parameters.ClientId] = client_id,
            [Parameters.Code] = query["code"].ToString(),
            [Parameters.RedirectUri] = game_redirect_uri,
            [Parameters.CodeVerifier] = verifier,
        }), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, token.StatusCode);

        using var tokens = JsonDocument.Parse(await token.Content.ReadAsStringAsync(CancellationToken));
        Assert.Equal("Bearer", tokens.RootElement.GetProperty("token_type").GetString());

        var identity = payloadOf(tokens.RootElement.GetProperty("id_token").GetString()!);
        Assert.Equal(email, identity.GetProperty("email").GetString());
        Assert.True(Guid.TryParse(identity.GetProperty("sub").GetString(), out _));

        var access = payloadOf(tokens.RootElement.GetProperty("access_token").GetString()!);
        Assert.Equal(identity.GetProperty("sub").GetString(), access.GetProperty("sub").GetString());
    }

    [Fact]
    public async Task AnonymousPlayerIsSentToTheLoginPage()
    {
        await registerGameAsync();
        var (_, challenge) = pkce();

        using var response = await game.GetAsync(authorizeUri(challenge), CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("/login?returnUrl=", location, StringComparison.Ordinal);
        Assert.StartsWith("/connect/authorize?", Uri.UnescapeDataString(location["/login?returnUrl=".Length..]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthorizationWithoutPkceIsRejected()
    {
        await registerGameAsync();
        await signInAsync();

        using var response = await game.GetAsync(new Uri(
            $"/connect/authorize?client_id={client_id}&redirect_uri={Uri.EscapeDataString(game_redirect_uri)}&response_type=code&scope=openid&state=xyz",
            UriKind.Relative), CancellationToken);

        // Rejected before the redirect_uri is trusted, so the error is rendered here instead of sent to the game.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.Contains("'code_challenge' parameter is missing", await response.Content.ReadAsStringAsync(CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnregisteredRedirectUriIsRejected()
    {
        await registerGameAsync();
        var (_, challenge) = pkce();

        using var response = await game.GetAsync(new Uri(
            $"/connect/authorize?client_id={client_id}&redirect_uri={Uri.EscapeDataString("https://evil.example/callback")}&response_type=code&scope=openid&code_challenge={challenge}&code_challenge_method=S256",
            UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Uri authorizeUri(string challenge)
        => new(
            $"/connect/authorize?client_id={client_id}&redirect_uri={Uri.EscapeDataString(game_redirect_uri)}&response_type=code"
            + $"&scope={Uri.EscapeDataString("openid email")}&code_challenge={challenge}&code_challenge_method=S256&state=xyz",
            UriKind.Relative);

    private async Task registerGameAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = client_id,
            ApplicationType = ApplicationTypes.Native,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { registered_redirect_uri },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        }, CancellationToken);
    }

    private async Task signInAsync()
    {
        using var registered = await game.PostAsJsonAsync(new Uri("/api/v1/identity/accounts", UriKind.Relative), new RegisterAccountCommand(email, password), CancellationToken);
        registered.EnsureSuccessStatusCode();

        var antiforgery = await game.GetFromJsonAsync<AntiforgeryTokenResponse>(new Uri("/api/v1/identity/antiforgery", UriKind.Relative), CancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/identity/sessions", UriKind.Relative))
        {
            Content = JsonContent.Create(new SignInCommand(email, password, null)),
        };
        request.Headers.Add("X-XSRF-TOKEN", antiforgery!.Token);

        using var signedIn = await game.SendAsync(request, CancellationToken);
        signedIn.EnsureSuccessStatusCode();
    }

    /// <summary>A PKCE verifier and its S256 challenge, as a game would generate them.</summary>
    private static (string Verifier, string Challenge) pkce()
    {
        var verifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        return (verifier, Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    private static JsonElement payloadOf(string jwt)
        => JsonDocument.Parse(Base64Url.DecodeFromChars(jwt.Split('.')[1])).RootElement;
}
