using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using yggdrasil.Integration.Tests.Infrastructure;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class ClientCredentialsTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private const string client_id = "test-server";

    private const string client_secret = "test-server-secret";

    // Stand-ins for the API scopes the profiles module will define: one the server may ask for, one it may not.
    private const string granted_scope = "test.granted";

    private const string other_scope = "test.other";

    [Fact]
    public async Task ServerGetsAnAccessTokenCarryingItsScopes()
    {
        await registerServerAsync();

        using var response = await requestTokenAsync(client_secret, granted_scope);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await GameClient.ReadJsonAsync(response, CancellationToken);

        Assert.False(tokens.TryGetProperty("refresh_token", out _));
        Assert.False(tokens.TryGetProperty("id_token", out _));

        var access = GameClient.PayloadOf(tokens.GetProperty("access_token").GetString()!);
        Assert.Equal(client_id, access.GetProperty("sub").GetString());
        Assert.Equal(granted_scope, access.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task WrongSecretIsRejected()
    {
        await registerServerAsync();

        using var response = await requestTokenAsync("not-the-secret", granted_scope);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(Errors.InvalidClient, (await GameClient.ReadJsonAsync(response, CancellationToken)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task ScopeNotGrantedToTheServerIsRejected()
    {
        await registerServerAsync();

        using var response = await requestTokenAsync(client_secret, other_scope);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(Errors.InvalidRequest, (await GameClient.ReadJsonAsync(response, CancellationToken)).GetProperty("error").GetString());
    }

    [Fact]
    public async Task GameClientCannotUseClientCredentials()
    {
        using var game = new GameClient(Factory);
        await game.RegisterAsync(CancellationToken);

        using var response = await game.PostTokenAsync(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.ClientCredentials,
        }, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task registerServerAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var scopes = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        await scopes.CreateAsync(new OpenIddictScopeDescriptor { Name = granted_scope }, CancellationToken);
        await scopes.CreateAsync(new OpenIddictScopeDescriptor { Name = other_scope }, CancellationToken);

        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = client_id,
            ClientSecret = client_secret,
            ClientType = ClientTypes.Confidential,
            DisplayName = "Test game server",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + granted_scope,
            },
        }, CancellationToken);
    }

    private Task<HttpResponseMessage> requestTokenAsync(string secret, string scope)
        => Client.PostAsync(new Uri("/connect/token", UriKind.Relative), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [Parameters.GrantType] = GrantTypes.ClientCredentials,
            [Parameters.ClientId] = client_id,
            [Parameters.ClientSecret] = secret,
            [Parameters.Scope] = scope,
        }), CancellationToken);
}
