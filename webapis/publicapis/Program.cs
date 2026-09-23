using System.Globalization;
using Serilog;
using yggdrasil.Modules.Identity;
using yggdrasil.PublicApis.Extensions.EndpointRouteBuilderExtensions;
using yggdrasil.PublicApis.Extensions.ServiceCollectionExtensions;
using yggdrasil.PublicApis.Extensions.WebApplicationExtensions;
using yggdrasil.Web.Modules;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    await WebApplication
        .CreateBuilder(args)
        .UseComponents((webHost, configuration, services, environment, logging) =>
        {
            services
                .UseRouting()
                .UseCommon()
                .UseMediator()
                .UseOpenApi()
                .UseHealthChecks()
                .UseLogging();

            services.AddModules(configuration,
                new IdentityModule());
        })
        .UsePipelines((application, configuration, services, environment) =>
        {
            application
                .UseSerilogRequestLogging(options => options.Logger = services.GetRequiredService<Serilog.ILogger>())
                .UseExceptionHandler();

            if (!environment.IsDevelopment())
                application.UseHsts();

            application
                .UseResponseCompression()
                .UseRouting()
                .UseModules()
                .UseOutputCache()
                .UseEndpoints(endpoints =>
                {
                    endpoints
                        .MapHealthCheckRoutes()
                        .MapOpenApi(environment)
                        .MapModules();
                });
        })
        .RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Application terminated unexpectedly");

    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
