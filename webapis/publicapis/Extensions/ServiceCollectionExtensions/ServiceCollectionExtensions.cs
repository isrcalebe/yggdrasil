using Mediator;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Debugging;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Templates;
using Serilog.Templates.Themes;
using yggdrasil.Core.HealthChecks;
using yggdrasil.Web.Behaviors;
using yggdrasil.Web.Exceptions;
using yggdrasil.Web.Versioning;

namespace yggdrasil.PublicApis.Extensions.ServiceCollectionExtensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection self)
    {
        public IServiceCollection UseCommon()
            => self
                .AddSingleton(TimeProvider.System)
                .AddExceptionHandler<ValidationExceptionHandler>()
                .AddOutputCache()
                .AddResponseCompression();

        public IServiceCollection UseMediator()
            => self.AddMediator(static options =>
            {
                options.ServiceLifetime = ServiceLifetime.Scoped;
                options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
            });

        public IServiceCollection UseOpenApi()
            => self.AddOpenApi();

        public IServiceCollection UseHealthChecks()
        {
            self.AddHealthChecks()
                .AddCheck("self", static () => HealthCheckResult.Healthy(), tags: [HealthCheckTags.LIVE]);

            return self;
        }

        public IServiceCollection UseRouting()
            => self
                .AddRouting(static options => options.LowercaseUrls = true)
                .AddApiVersioningDefaults()
                .AddProblemDetails(static options => options.CustomizeProblemDetails = static context =>
                {
                    var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

                    if (environment.IsDevelopment() && context.Exception is not null)
                        context.ProblemDetails.Detail = context.Exception.Message;
                })
                .AddHttpContextAccessor();

        public IServiceCollection UseLogging()
        {
            const string template = "[{@t:HH:mm:ss} {@l:u3} ({Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<none>')})] {@m}\n{@x}";

            self.AddSerilog(static (services, configuration) =>
            {
                var environment = services.GetRequiredService<IHostEnvironment>();

                configuration
                    .ReadFrom.Configuration(services.GetRequiredService<IConfiguration>())
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails();

                if (environment.IsDevelopment())
                {
                    SelfLog.Enable(Console.Error);
                    configuration.WriteTo.Console(new ExpressionTemplate(template, theme: TemplateTheme.Code));
                }
                else
                {
                    configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
                }
            },
            // Each host owns its logger; the static Log.Logger only covers startup failures.
            // Sharing a frozen ReloadableLogger breaks multiple hosts per process (e.g. WebApplicationFactory).
            preserveStaticLogger: true);

            return self;
        }
    }
}
