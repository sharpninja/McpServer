using Microsoft.Extensions.Logging;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-003: A captured log entry from a <see cref="CallContext"/>.
/// Stores the timestamp, level, message, and optional exception for each log call
/// made through the context's <see cref="ILogger"/> implementation.
/// </summary>
/// <param name="Timestamp">When the log entry was created.</param>
/// <param name="Level">The log level.</param>
/// <param name="Message">The formatted log message.</param>
/// <param name="Exception">The exception associated with the log entry, if any.</param>
public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Message,
    Exception? Exception);
