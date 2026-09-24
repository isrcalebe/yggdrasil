using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using yggdrasil.Integration.Tests.Infrastructure;

namespace yggdrasil.Integration.Tests;

public sealed class MalformedRequestBodyTests(IntegrationFactory factory) : IntegrationTest(factory)
{
    private static readonly Uri accounts = new("/api/v1/identity/accounts", UriKind.Relative);

    [Theory]
    [InlineData("""{"email":""")]
    [InlineData("""{"email":1,"password":2}""")]
    public async Task MalformedBodyIsBadRequestProblem(string body)
    {
        using var response = await postAsync(Client, body);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StatusCodes.Status400BadRequest, problem!.Status);
        Assert.NotNull(problem.Detail);
    }

    [Fact]
    public async Task ProductionDoesNotExposeTheBindingError()
    {
        using var production = Factory.WithWebHostBuilder(static builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();

        using var response = await postAsync(client, """{"email":""");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(problem!.Detail);
    }

    private static Task<HttpResponseMessage> postAsync(HttpClient client, string body)
        => client.PostAsync(accounts, new StringContent(body, Encoding.UTF8, "application/json"), CancellationToken);
}
