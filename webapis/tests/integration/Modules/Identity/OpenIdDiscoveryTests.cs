using System.Net;
using System.Text.Json;
using yggdrasil.Integration.Tests.Infrastructure;

namespace yggdrasil.Integration.Tests.Modules.Identity;

public sealed class OpenIdDiscoveryTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private static readonly Uri discovery = new("/.well-known/openid-configuration", UriKind.Relative);

    [Fact]
    public async Task DiscoveryDocumentDescribesTheServer()
    {
        using var document = await getJsonAsync(discovery);
        var server = document.RootElement;

        Assert.Equal("http://localhost/", server.GetProperty("issuer").GetString());
        Assert.Equal("http://localhost/connect/authorize", server.GetProperty("authorization_endpoint").GetString());
        Assert.Equal("http://localhost/connect/token", server.GetProperty("token_endpoint").GetString());
        Assert.Contains("authorization_code", strings(server.GetProperty("grant_types_supported")));
        Assert.Contains("S256", strings(server.GetProperty("code_challenge_methods_supported")));
    }

    [Fact]
    public async Task JwksPublishesTheSigningKey()
    {
        using var document = await getJsonAsync(discovery);
        var jwksUri = new Uri(document.RootElement.GetProperty("jwks_uri").GetString()!);

        using var jwks = await getJsonAsync(new Uri(jwksUri.PathAndQuery, UriKind.Relative));
        var key = Assert.Single(jwks.RootElement.GetProperty("keys").EnumerateArray());

        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("sig", key.GetProperty("use").GetString());
    }

    private async Task<JsonDocument> getJsonAsync(Uri uri)
    {
        using var response = await Client.GetAsync(uri, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(CancellationToken), cancellationToken: CancellationToken);
    }

    private static IEnumerable<string?> strings(JsonElement array) => array.EnumerateArray().Select(static item => item.GetString());
}
