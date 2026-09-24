using System.Net;
using System.Net.Http.Json;
using yggdrasil.Integration.Tests.Infrastructure;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Modules.Identity.Contracts.v1.Antiforgery;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class SessionTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string email = "player@yggdrasil.cc";

    private const string password = "correct-horse-battery-9A";

    private const string antiforgery_header = "X-XSRF-TOKEN";

    private static readonly Uri accounts = new("/api/v1/identity/accounts", UriKind.Relative);

    private static readonly Uri antiforgery = new("/api/v1/identity/antiforgery", UriKind.Relative);

    private static readonly Uri sessions = new("/api/v1/identity/sessions", UriKind.Relative);

    private static readonly Uri current_session = new("/api/v1/identity/sessions/current", UriKind.Relative);

    [Fact]
    public async Task SignInStartsASessionWithASecureCookie()
    {
        await registerAsync();

        using var response = await signInAsync(email, password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/", (await response.Content.ReadFromJsonAsync<SignInResponse>(CancellationToken))!.RedirectTo);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), static header => header.StartsWith("yggdrasil.session=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);

        var session = await Client.GetFromJsonAsync<CurrentSessionResponse>(current_session, CancellationToken);
        Assert.Equal(email, session!.Email);
    }

    [Fact]
    public async Task UnknownEmailAndWrongPasswordAreIndistinguishable()
    {
        await registerAsync();

        using var wrongPassword = await signInAsync(email, "wrong-password-9A");
        using var unknownEmail = await signInAsync("nobody@yggdrasil.cc", password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await errorCodeAsync(wrongPassword), await errorCodeAsync(unknownEmail));
    }

    [Fact]
    public async Task RepeatedFailuresLockTheAccountEvenForTheRightPassword()
    {
        await registerAsync();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await signInAsync(email, "wrong-password-9A");
            Assert.NotEqual(HttpStatusCode.OK, failed.StatusCode);
        }

        using var response = await signInAsync(email, password);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("identity.locked_out", await errorCodeAsync(response));
    }

    [Fact]
    public async Task SignInWithoutAntiforgeryTokenIsRejected()
    {
        await registerAsync();

        using var response = await Client.PostAsJsonAsync(sessions, new SignInCommand(email, password, null), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("antiforgery.invalid_token", await errorCodeAsync(response));
    }

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("/\t/evil.example")]
    [InlineData("/\n/evil.example")]
    public async Task NonLocalReturnUrlIsRejected(string returnUrl)
    {
        await registerAsync();

        using var response = await signInAsync(email, password, returnUrl);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LocalReturnUrlIsWhereTheSiteGoesNext()
    {
        await registerAsync();

        using var response = await signInAsync(email, password, "/connect/authorize?client_id=game");

        Assert.Equal("/connect/authorize?client_id=game", (await response.Content.ReadFromJsonAsync<SignInResponse>(CancellationToken))!.RedirectTo);
    }

    [Fact]
    public async Task CurrentSessionWithoutSignInIsUnauthorized()
    {
        using var response = await Client.GetAsync(current_session, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignOutEndsTheSession()
    {
        await registerAsync();
        using var signIn = await signInAsync(email, password);

        using var request = new HttpRequestMessage(HttpMethod.Delete, current_session);
        request.Headers.Add(antiforgery_header, await antiforgeryTokenAsync());
        using var signOut = await Client.SendAsync(request, CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);

        using var session = await Client.GetAsync(current_session, CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
    }

    [Fact]
    public async Task SignOutWithoutAntiforgeryTokenIsRejected()
    {
        await registerAsync();
        using var signIn = await signInAsync(email, password);

        using var response = await Client.DeleteAsync(current_session, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task registerAsync()
    {
        using var response = await Client.PostAsJsonAsync(accounts, new RegisterAccountCommand(email, password), CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> antiforgeryTokenAsync()
        => (await Client.GetFromJsonAsync<AntiforgeryTokenResponse>(antiforgery, CancellationToken))!.Token;

    private async Task<HttpResponseMessage> signInAsync(string signInEmail, string signInPassword, string? returnUrl = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, sessions)
        {
            Content = JsonContent.Create(new SignInCommand(signInEmail, signInPassword, returnUrl)),
        };
        request.Headers.Add(antiforgery_header, await antiforgeryTokenAsync());

        return await Client.SendAsync(request, CancellationToken);
    }

    private static async Task<string?> errorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object?>>(CancellationToken);

        return problem?.GetValueOrDefault("code")?.ToString();
    }
}
