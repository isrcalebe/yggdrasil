using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace yggdrasil.Web.Versioning;

public static class VersioningExtensions
{
    extension(IServiceCollection self)
    {
        /// <summary>
        /// URL segment versioning (<c>/api/v1/...</c>). The API explorer groups endpoints as <c>v1</c>, <c>v2</c>, ...,
        /// which is the name of the matching OpenAPI document (<c>/openapi/v1.json</c>).
        /// </summary>
        public IServiceCollection AddApiVersioningDefaults()
        {
            self
                .AddApiVersioning(static options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1);
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                })
                .AddApiExplorer(static options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            return self;
        }
    }
}
