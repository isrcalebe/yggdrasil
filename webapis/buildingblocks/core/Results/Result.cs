using System.Diagnostics.CodeAnalysis;

namespace yggdrasil.Core.Results;

/// <summary>Outcome of an operation that returns no value.</summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that returns <typeparamref name="TValue"/> on success.</summary>
public sealed class Result<TValue> : Result
{
    private Result(TValue value) : base(null) => Value = value;

    private Result(Error error) : base(error) => Value = default!;

    /// <summary>The value. Only valid when <see cref="Result.IsSuccess"/> is <see langword="true"/>.</summary>
    public TValue Value
    {
        get => IsSuccess ? field : throw new InvalidOperationException("The value of a failed result cannot be accessed.");
        private init;
    }

    public static Result<TValue> Success(TValue value) => new(value);

    public new static Result<TValue> Failure(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
