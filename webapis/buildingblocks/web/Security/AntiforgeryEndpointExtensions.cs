using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace yggdrasil.Web.Security;

public static class AntiforgeryEndpointExtensions
{
    extension(RouteHandlerBuilder self)
    {
        public RouteHandlerBuilder RequireAntiforgeryToken()
            => self.AddEndpointFilter(static async (context, next) =>
            {
                var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

                return await antiforgery.IsRequestValidAsync(context.HttpContext)
                    ? await next(context)
                    : TypedResults.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        detail: "Missing or invalid antiforgery token.",
                        extensions: new Dictionary<string, object?>
                        {
                            ["code"] = "antiforgery.invalid_token"
                        }
                    );
            });
    }
}
