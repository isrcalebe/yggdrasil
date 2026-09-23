using Asp.Versioning.Builder;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace yggdrasil.Web.Modules;

public static class ModuleExtensions
{
    extension(IServiceCollection self)
    {
        /// <summary>
        /// Registers the modules: their services, their FluentValidation validators, and the list later used by
        /// <c>UseModules</c> and <c>MapModules</c>. This is the only place a module needs to be listed.
        /// </summary>
        public IServiceCollection AddModules(IConfiguration configuration, params IModule[] modules)
        {
            self.AddSingleton(new ModuleRegistry(modules));
            self.AddValidatorsFromAssemblies(modules.Select(static module => module.GetType().Assembly).Distinct());

            foreach (var module in modules)
                module.ConfigureServices(self, configuration);

            return self;
        }
    }

    extension(IApplicationBuilder self)
    {
        public IApplicationBuilder UseModules()
        {
            foreach (var module in self.ApplicationServices.GetRequiredService<ModuleRegistry>().Modules)
                module.ConfigureMiddleware(self);

            return self;
        }
    }

    extension(IEndpointRouteBuilder self)
    {
        public IEndpointRouteBuilder MapModules()
        {
            foreach (var module in self.ServiceProvider.GetRequiredService<ModuleRegistry>().Modules)
            {
                var versionSet = module.ApiVersions
                    .Aggregate(self.NewApiVersionSet(module.Name), static (builder, version) => builder.HasApiVersion(version))
                    .ReportApiVersions()
                    .Build();

                var group = self
                    .MapGroup($"/api/v{{version:apiVersion}}/{module.Name}")
                    .WithTags(module.Name)
                    .WithApiVersionSet(versionSet);

                module.MapEndpoints(group);
            }

            return self;
        }
    }

    private sealed record ModuleRegistry(IReadOnlyList<IModule> Modules);
}
