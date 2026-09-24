namespace yggdrasil.Core.Results;

public enum ErrorType
{
    /// <summary>A business rule was violated.</summary>
    Failure,

    /// <summary>The input is invalid.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state of the resource.</summary>
    Conflict,

    /// <summary>
    /// The caller could not be authenticated.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// The caller is authenticated but not allowed to perform the operation.
    /// </summary>
    Forbidden,
}

/// <summary>An expected error. <see cref="Code"/> is stable and meant for clients, e.g. <c>notes.not_found</c>.</summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
}
