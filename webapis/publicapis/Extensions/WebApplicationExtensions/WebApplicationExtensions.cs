namespace yggdrasil.PublicApis.Extensions.WebApplicationExtensions;

public static class WebApplicationExtensions
{
    public delegate void ConfigureWebApplication(
        IApplicationBuilder applicationBuilder,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment
    );

    public delegate void ConfigureWebApplicationBuilder(
        IWebHostBuilder webHost,
        IConfigurationManager configurationManager,
        IServiceCollection serviceCollection,
        IHostEnvironment environment,
        ILoggingBuilder loggingBuilder
    );

    extension(WebApplicationBuilder self)
    {
        public WebApplication UseComponents(ConfigureWebApplicationBuilder configure)
        {
            var configurationManager = self.Configuration;
            var serviceCollection = self.Services;
            var environment = self.Environment;
            var loggingBuilder = self.Logging;
            var webHost = self.WebHost;

            configure(webHost, configurationManager, serviceCollection, environment, loggingBuilder);

            return self.Build();
        }
    }

    extension(WebApplication self)
    {
        public WebApplication UsePipelines(ConfigureWebApplication configure)
        {
            var configuration = self.Configuration;
            var serviceProvider = self.Services;
            var environment = self.Environment;

            configure(self, configuration, serviceProvider, environment);

            return self;
        }
    }
}
