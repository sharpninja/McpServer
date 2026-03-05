using Microsoft.Extensions.Logging;

namespace McpServer.Cqrs;

/// <summary>
/// Snapshot of a completed CQRS dispatch captured by <see cref="Dispatcher"/> for diagnostics/UI display.
/// </summary>
/// <param name="StartedAt">When the dispatch call context started.</param>
/// <param name="FinishedAt">When the dispatch completed.</param>
/// <param name="OperationName">CQRS request type name (command/query).</param>
/// <param name="CorrelationId">Final correlation ID string for the dispatch.</param>
/// <param name="Outcome">Outcome label (e.g. Success, Failure, Timeout, Cancelled).</param>
/// <param name="ElapsedMilliseconds">Execution time in milliseconds.</param>
/// <param name="Error">Top-level error text, if any.</param>
/// <param name="UserId">User ID from the context, if populated.</param>
/// <param name="UserName">User name from the context, if populated.</param>
/// <param name="Roles">User roles from the context, if populated.</param>
/// <param name="Entries">Captured log entries for the dispatch.</param>
/// <param name="RequestData">Serialized request parameters, if captured.</param>
/// <param name="ResultData">Serialized result value from the Result monad, if captured.</param>
public sealed record DispatchLogRecord(
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string OperationName,
    string CorrelationId,
    string Outcome,
    long ElapsedMilliseconds,
    string? Error,
    string? UserId,
    string? UserName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<DispatchLogRecordEntry> Entries,
    string? RequestData = null,
    string? ResultData = null);

/// <summary>
/// Lightweight serialized form of a <see cref="LogEntry"/> retained in dispatch history.
/// </summary>
/// <param name="Timestamp">Entry timestamp.</param>
/// <param name="Level">Log level.</param>
/// <param name="Message">Formatted log message.</param>
/// <param name="ExceptionType">Exception CLR type name, if present.</param>
/// <param name="ExceptionMessage">Exception message, if present.</param>
public sealed record DispatchLogRecordEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Message,
    string? ExceptionType,
    string? ExceptionMessage);
