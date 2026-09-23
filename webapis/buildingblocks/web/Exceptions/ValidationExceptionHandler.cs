using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace yggdrasil.Web.Exceptions;

/// <summary>Turns a FluentValidation <see cref="ValidationException"/> into a <c>400 ValidationProblem</c>.</summary>
public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        var errors = validationException.Errors
            .GroupBy(static failure => string.Join('.', failure.PropertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName)))
            .ToDictionary(static group => group.Key, static group => group.Select(static failure => failure.ErrorMessage).ToArray());

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new HttpValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest },
        });
    }
}
