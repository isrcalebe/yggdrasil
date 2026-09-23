using Microsoft.AspNetCore.Http;

using yggdrasil.Core.Results;

namespace yggdrasil.Web.Results;

public static class ResultHttpExtensions
{
    extension(Result self)
    {
        /// <summary>Maps to <c>204 No Content</c> on success, or to a ProblemDetails response.</summary>
        public IResult ToHttpResult()
            => self.IsSuccess ? TypedResults.NoContent() : self.Error.ToProblem();
    }

    extension<TValue>(Result<TValue> self)
    {
        /// <summary>
        /// Maps to <paramref name="onSuccess"/> (default <c>200 OK</c>) on success, or to a ProblemDetails response.
        /// </summary>
        public IResult ToHttpResult(Func<TValue, IResult>? onSuccess = null)
            => self.IsSuccess
                ? onSuccess?.Invoke(self.Value) ?? TypedResults.Ok(self.Value)
                : self.Error.ToProblem();
    }

    extension(Error self)
    {
        public IResult ToProblem()
            => TypedResults.Problem(
                detail: self.Description,
                statusCode: self.Type switch
                {
                    ErrorType.Validation => StatusCodes.Status400BadRequest,
                    ErrorType.NotFound => StatusCodes.Status404NotFound,
                    ErrorType.Conflict => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status422UnprocessableEntity,
                },
                extensions: new Dictionary<string, object?> { ["code"] = self.Code });
    }
}
