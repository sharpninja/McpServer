using System.Collections;
using System.Text;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-001: Result monad for CQRS handler return values.
/// All handlers return <see cref="Result{T}"/> to represent success or failure
/// without throwing exceptions for expected error conditions.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    /// <summary>The success value, or <c>default</c> if the result is a failure.</summary>
    public T? Value { get; }

    /// <summary>The error message, or <c>null</c> if the result is a success.</summary>
    public string? Error { get; }

    /// <summary>The exception that caused the failure, or <c>null</c> if none.</summary>
    public Exception? Exception { get; }

    /// <summary>Whether this result represents a successful outcome.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether this result represents a failed outcome.</summary>
    public bool IsFailure => !IsSuccess;

    private Result(T? value, string? error, Exception? exception, bool isSuccess)
    {
        Value = value;
        Error = error;
        Exception = exception;
        IsSuccess = isSuccess;
    }

    /// <summary>Creates a successful result with the specified value.</summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value) => new(value, null, null, true);

    /// <summary>Creates a failed result with the specified error message.</summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(string error) => new(default, error, null, false);

    /// <summary>Creates a failed result with the specified exception.</summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(Exception exception)
        => new(default, exception?.Message, exception, false);

    /// <summary>Creates a failed result with both an error message and exception.</summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(string error, Exception exception)
        => new(default, error, exception, false);

    /// <summary>
    /// Monadic bind: if this result is successful, applies <paramref name="f"/> to the value
    /// and returns the resulting <see cref="Result{TNext}"/>. If this result is a failure,
    /// propagates the failure.
    /// </summary>
    /// <typeparam name="TNext">The type of the next result's value.</typeparam>
    /// <param name="f">The function to apply to the success value.</param>
    /// <returns>The bound result.</returns>
    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> f)
        => IsSuccess ? f(Value!) : Result<TNext>.Failure(Error!, Exception!);

    /// <summary>
    /// Functor map: if this result is successful, applies <paramref name="f"/> to the value
    /// and wraps the output in a new success result. If this result is a failure, propagates the failure.
    /// </summary>
    /// <typeparam name="TNext">The type of the mapped value.</typeparam>
    /// <param name="f">The mapping function.</param>
    /// <returns>The mapped result.</returns>
    public Result<TNext> Map<TNext>(Func<T, TNext> f)
        => IsSuccess ? Result<TNext>.Success(f(Value!)) : Result<TNext>.Failure(Error!, Exception!);

    /// <summary>Returns the success value, or <paramref name="fallback"/> if the result is a failure.</summary>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The value or fallback.</returns>
    public T GetValueOrDefault(T fallback) => IsSuccess ? Value! : fallback;

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(IsSuccess ? "Success" : "Failure");

        if (Error is not null)
            sb.AppendLine($"Error: {Error}");

        if (Value is not null)
        {
            sb.AppendLine("Value:");
            foreach (var line in Value.ToYaml().Split('\n'))
                sb.AppendLine($"  {line}");
        }

        if (Exception is not null)
        {
            sb.AppendLine($"Exception: {Exception}");
            if (Exception.Data.Count > 0)
            {
                sb.AppendLine("ExceptionData:");
                foreach (DictionaryEntry entry in Exception.Data)
                    sb.AppendLine($"  {entry.Key}: {entry.Value}");
            }
        }

        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// TR-MCP-CQRS-001: Non-generic Result for void-like commands that don't return a value.
/// </summary>
public readonly struct Result
{
    /// <summary>The error message, or <c>null</c> if the result is a success.</summary>
    public string? Error { get; }

    /// <summary>The exception that caused the failure, or <c>null</c> if none.</summary>
    public Exception? Exception { get; }

    /// <summary>Whether this result represents a successful outcome.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether this result represents a failed outcome.</summary>
    public bool IsFailure => !IsSuccess;

    private Result(string? error, Exception? exception, bool isSuccess)
    {
        Error = error;
        Exception = exception;
        IsSuccess = isSuccess;
    }

    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(null, null, true);

    /// <summary>Creates a failed result with the specified error message.</summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(string error) => new(error, null, false);

    /// <summary>Creates a failed result with the specified exception.</summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(Exception exception) => new(exception?.Message, exception, false);

    /// <summary>Creates a failed result with both an error message and exception.</summary>
    /// <param name="error">The error message.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(string error, Exception exception) => new(error, exception, false);

    /// <inheritdoc />
    public override string ToString()
        => IsSuccess ? "Success" : $"Failure({Error})";
}
