using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using yggdrasil.PublicApis.HealthChecks;

namespace yggdrasil.PublicApis.Extensions.EndpointRouteBuilderExtensions;

public static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder self)
    {
        public IEndpointRouteBuilder MapHealthCheckRoutes()
        {
            self.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = static registration => registration.Tags.Contains(HealthCheckTags.LIVE),
            });

            self.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = static registration => registration.Tags.Contains(HealthCheckTags.READY),
            });

            return self;
        }

        public IEndpointRouteBuilder MapOpenApi(IWebHostEnvironment environment)
        {
            if (environment.IsProduction())
                return self;

            self.MapOpenApi();
            self.MapScalarApiReference(options =>
                options
                    .WithTitle("yggdrasil")
                    .WithTheme(ScalarTheme.Kepler)
                    .DisableAgent()
                    .DisableMcp()
                    .DisableTelemetry()
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .HideDeveloperTools());

            return self;
        }
    }
}
