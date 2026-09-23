using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using yggdrasil.Core.HealthChecks;

namespace yggdrasil.PublicApis.Tests.HealthChecks;

public sealed class HealthCheckEndpointsTests(PublicApisFactory factory) : IClassFixture<PublicApisFactory>
{
    // Readiness depends on module databases: its healthy path needs tests against a real database (Testcontainers).
    [Fact]
    public async Task LivenessIsHealthy()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }

    [Theory]
    [InlineData("/health/live", HttpStatusCode.OK)]
    [InlineData("/health/ready", HttpStatusCode.ServiceUnavailable)]
    public async Task FailingReadinessCheckOnlyAffectsReadiness(string path, HttpStatusCode expected)
    {
        using var withFailingDependency = factory.WithWebHostBuilder(static builder =>
            builder.ConfigureTestServices(static services =>
                services.AddHealthChecks()
                    .AddCheck("dependency", static () => HealthCheckResult.Unhealthy(), tags: [HealthCheckTags.READY])));
        using var client = withFailingDependency.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }
}
