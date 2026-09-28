using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using yggdrasil.Modules.Identity.Administration;
using yggdrasil.Modules.Identity.Contracts.v1.Accounts;
using yggdrasil.Modules.Identity.Contracts.v1.Antiforgery;
using yggdrasil.Modules.Identity.Contracts.v1.Sessions;

namespace yggdrasil.Integration.Tests.Modules.Identity;

/// <summary>A registered account with its own session cookie, able to call endpoints protected by antiforgery.</summary>
internal sealed class SignedInAccount : IDisposable
{
    public const string PASSWORD = "correct-horse-battery-9A";

    private const string antiforgery_header = "X-XSRF-TOKEN";

    private readonly string antiforgeryToken;

    private SignedInAccount(Guid id, HttpClient http, string antiforgeryToken)
    {
        Id = id;
        Http = http;
        this.antiforgeryToken = antiforgeryToken;
    }

    public Guid Id { get; }

    public HttpClient Http { get; }

    public static async Task<Guid> RegisterAsync(WebApplicationFactory<Program> factory, string email, CancellationToken cancellationToken)
    {
        using var http = factory.CreateClient();
        using var response = await http.PostAsJsonAsync(new Uri("/api/v1/identity/accounts", UriKind.Relative), new RegisterAccountCommand(email, PASSWORD), cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RegisterAccountResponse>(cancellationToken))!.AccountId;
    }

    public static async Task<SignedInAccount> SignInAsync(WebApplicationFactory<Program> factory, Guid id, string email, CancellationToken cancellationToken)
    {
        var http = factory.CreateClient();

        using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/identity/sessions", UriKind.Relative)))
        {
            request.Content = JsonContent.Create(new SignInCommand(email, PASSWORD, null));
            request.Headers.Add(antiforgery_header, await antiforgeryTokenAsync(http, cancellationToken));

            using var response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        // Antiforgery tokens are bound to the signed-in user: the one used to sign in is no longer valid.
        return new SignedInAccount(id, http, await antiforgeryTokenAsync(http, cancellationToken));
    }

    /// <summary>
    /// The first administrator, created the way a deployment does it: listed for the seeder.
    /// </summary>
    public static async Task<SignedInAccount> SignInAsAdministratorAsync(WebApplicationFactory<Program> factory, string email, CancellationToken cancellationToken)
    {
        var id = await RegisterAsync(factory, email, cancellationToken);
        await factory.Services.GetRequiredService<AdministratorSeeder>().SeedAsync([id], cancellationToken);

        return await SignInAsync(factory, id, email, cancellationToken);
    }

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    => SendAsync(method, path, null, cancellationToken);

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative)) { Content = content };
        request.Headers.Add(antiforgery_header, antiforgeryToken);

        return await Http.SendAsync(request, cancellationToken);
    }

    public void Dispose() => Http.Dispose();

    private static async Task<string> antiforgeryTokenAsync(HttpClient http, CancellationToken cancellationToken)
        => (await http.GetFromJsonAsync<AntiforgeryTokenResponse>(new Uri("/api/v1/identity/antiforgery", UriKind.Relative), cancellationToken))!.Token;
}
