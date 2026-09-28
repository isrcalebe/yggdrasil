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

/// <summary>
/// Plays the part of a game doing "Login with Yggdrasil ID": a public native client with PKCE and a loopback redirect.
/// </summary>
internal sealed class GameClient(IntegrationFactory factory) : IDisposable
{
    public const string CLIENT_ID = "test-game";

    public const string EMAIL = "player@yggdrasil.cc";

    public const string PASSWORD = "correct-horse-battery-9A";

    public const string REDIRECT_URI = "http://127.0.0.1:54321/callback";

    // Registered without a port: a native client may then use any loopback port (RFC 8252 §7.3), since a game
    // listens on whichever port is free.
    private static readonly Uri registered_redirect_uri = new("http://127.0.0.1/callback");

    /// <summary>Does not follow redirects, so tests read the <c>Location</c> header like a game would.</summary>
    public HttpClient Http { get; } = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task RegisterAsync(CancellationToken cancellationToken)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = CLIENT_ID,
            ApplicationType = ApplicationTypes.Native,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { registered_redirect_uri },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        }, cancellationToken);
    }

    public async Task SignInAsync(CancellationToken cancellationToken)
    {
        using var registered = await Http.PostAsJsonAsync(new Uri("/api/v1/identity/accounts", UriKind.Relative), new RegisterAccountCommand(EMAIL, PASSWORD), cancellationToken);
        registered.EnsureSuccessStatusCode();

        var antiforgery = await Http.GetFromJsonAsync<AntiforgeryTokenResponse>(new Uri("/api/v1/identity/antiforgery", UriKind.Relative), cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/identity/sessions", UriKind.Relative))
        {
            Content = JsonContent.Create(new SignInCommand(EMAIL, PASSWORD, null)),
        };
        request.Headers.Add("X-XSRF-TOKEN", antiforgery!.Token);

        using var signedIn = await Http.SendAsync(request, cancellationToken);
        signedIn.EnsureSuccessStatusCode();
    }

    /// <summary>The whole flow for a signed-in player: authorize, then redeem the code. Returns the token response.</summary>
    public async Task<JsonElement> LogInAsync(string scope, CancellationToken cancellationToken)
    {
        var (verifier, challenge) = Pkce();

        using var authorize = await Http.GetAsync(AuthorizeUri(challenge, scope), cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, authorize.StatusCode);

        using var token = await PostTokenAsync(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.AuthorizationCode,
            [Parameters.Code] = QueryHelpers.ParseQuery(authorize.Headers.Location!.Query)["code"].ToString(),
            [Parameters.RedirectUri] = REDIRECT_URI,
            [Parameters.CodeVerifier] = verifier,
        }, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);

        return await ReadJsonAsync(token, cancellationToken);
    }

    public Task<HttpResponseMessage> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        => PostTokenAsync(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.RefreshToken,
            [Parameters.RefreshToken] = refreshToken,
        }, cancellationToken);

    public Task<HttpResponseMessage> PostTokenAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        parameters[Parameters.ClientId] = CLIENT_ID;

        return Http.PostAsync(new Uri("/connect/token", UriKind.Relative), new FormUrlEncodedContent(parameters), cancellationToken);
    }

    public static Uri AuthorizeUri(string challenge, string scope = "openid email")
        => new(
            $"/connect/authorize?client_id={CLIENT_ID}&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}&response_type=code"
            + $"&scope={Uri.EscapeDataString(scope)}&code_challenge={challenge}&code_challenge_method=S256&state=xyz",
            UriKind.Relative);

    /// <summary>A PKCE verifier and its S256 challenge, as a game would generate them.</summary>
    public static (string Verifier, string Challenge) Pkce()
    {
        var verifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        return (verifier, Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return document.RootElement.Clone();
    }

    public static JsonElement PayloadOf(string jwt)
    {
        using var document = JsonDocument.Parse(Base64Url.DecodeFromChars(jwt.Split('.')[1]));

        return document.RootElement.Clone();
    }

    public void Dispose() => Http.Dispose();
}
