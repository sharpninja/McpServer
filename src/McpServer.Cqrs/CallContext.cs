using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-003: Per-dispatch call context carrying correlation ID, auth claims,
/// timing, and an <see cref="ILogger"/> that captures log entries to an internal list.
/// Created by the <see cref="Dispatcher"/> for each <c>SendAsync</c> / <c>QueryAsync</c> call.
/// </summary>
public sealed class CallContext : ILogger, IDisposable
{
    private readonly ConcurrentBag<LogEntry> _entries = [];
    private readonly DateTimeOffset _started = DateTimeOffset.UtcNow;
    private bool _disposed;

    /// <summary>Initializes a new <see cref="CallContext"/> with a fresh correlation ID.</summary>
    public CallContext()
    {
        Correlation = new CorrelationId();
    }

    /// <summary>Initializes a new <see cref="CallContext"/> with an existing correlation ID (e.g. from an HTTP header).</summary>
    /// <param name="correlation">The correlation ID to use.</param>
    public CallContext(CorrelationId correlation)
    {
        ArgumentNullException.ThrowIfNull(correlation);
        Correlation = correlation;
    }

    /// <summary>The correlation ID for this call tree.</summary>
    public CorrelationId Correlation { get; }

    /// <summary>When this call context was created.</summary>
    public DateTimeOffset Started => _started;

    /// <summary>Elapsed time since the context was created.</summary>
    public TimeSpan Elapsed => DateTimeOffset.UtcNow - _started;

    /// <summary>The operation name (typically the command/query type name).</summary>
    public string OperationName { get; set; } = "";

    /// <summary>User ID from JWT <c>sub</c> claim, if authenticated.</summary>
    public string? UserId { get; set; }

    /// <summary>User display name from JWT <c>preferred_username</c> claim.</summary>
    public string? UserName { get; set; }

    /// <summary>Roles from JWT <c>realm_roles</c> claim.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>All log entries captured during this call.</summary>
    public IReadOnlyCollection<LogEntry> Entries => _entries;

    /// <summary>Arbitrary properties bag for pipeline behaviors to share state.</summary>
    public ConcurrentDictionary<string, object?> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cancellation token for this call.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (_disposed || !IsEnabled(logLevel)) return;
        var message = formatter(state, exception);

        // Include the full exception text (type + message + stack) in error messages so
        // dispatcher log views remain useful when only the formatted message is displayed.
        if (exception is not null && logLevel >= LogLevel.Error)
        {
            var exceptionText = exception.ToString();
            if (!string.IsNullOrWhiteSpace(exceptionText) &&
                (string.IsNullOrEmpty(message) || !message.Contains(exceptionText, StringComparison.Ordinal)))
            {
                message = string.IsNullOrWhiteSpace(message)
                    ? exceptionText
                    : $"{message}{Environment.NewLine}{exceptionText}";
            }
        }

        _entries.Add(new LogEntry(DateTime.UtcNow, logLevel, message, exception));
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
