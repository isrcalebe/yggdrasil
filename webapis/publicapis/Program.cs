using System.Globalization;
using Serilog;
using yggdrasil.PublicApis.Extensions.EndpointRouteBuilderExtensions;
using yggdrasil.PublicApis.Extensions.ServiceCollectionExtensions;
using yggdrasil.PublicApis.Extensions.WebApplicationExtensions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    await WebApplication
        .CreateSlimBuilder(args)
        .UseComponents((webHost, configuration, services, environment, logging) =>
        {
            webHost.UseKestrelHttpsConfiguration();

            services
                .UseRouting()
                .UseCommon()
                .UseOpenApi()
                .UseHealthChecks()
                .UseLogging();
        })
        .UsePipelines((application, configuration, services, environment) =>
        {
            application.UseExceptionHandler();

            if (!environment.IsDevelopment())
                application.UseHsts();

            application
                .UseSerilogRequestLogging()
                .UseResponseCompression()
                .UseRouting()
                .UseOutputCache()
                .UseEndpoints(endpoints =>
                {
                    endpoints
                        .MapHealthCheckRoutes()
                        .MapOpenApi(environment);
                });
        })
        .RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
