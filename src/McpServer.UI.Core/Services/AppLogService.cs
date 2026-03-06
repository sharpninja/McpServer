using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Application-wide logger that captures all log entries in memory.
/// Implements <see cref="ILoggerFactory"/> and <see cref="ILoggerProvider"/>
/// so it can be registered as the standard logging infrastructure.
/// </summary>
public sealed class AppLogService : ILoggerFactory, ILoggerProvider, McpServer.UI.Core.Services.IAppLogService
{
    private static readonly Lazy<AppLogService> _instance = new(() => new AppLogService());
    public static AppLogService Instance => _instance.Value;

    private readonly List<LogEntry> _entries = new();
    private readonly object _lock = new();
    private event Action<McpServer.UI.Core.Services.ILogEntry>? _entryAddedBridge;

    public event Action<LogEntry>? EntryAdded;

    event Action<McpServer.UI.Core.Services.ILogEntry>? McpServer.UI.Core.Services.IAppLogService.EntryAdded
    {
        add => _entryAddedBridge += value;
        remove => _entryAddedBridge -= value;
    }

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_lock) return _entries.ToArray(); }
    }

    IReadOnlyList<McpServer.UI.Core.Services.ILogEntry> McpServer.UI.Core.Services.IAppLogService.Entries => Entries;

    public void AddEntry(Microsoft.Extensions.Logging.LogLevel level, string source, string message)
    {
        var entry = new LogEntry(DateTime.Now, level, source, message);
        lock (_lock) _entries.Add(entry);
        EntryAdded?.Invoke(entry);
        _entryAddedBridge?.Invoke(entry);
    }

    public void Clear()
    {
        lock (_lock) _entries.Clear();
    }

    // ILoggerFactory
    public ILogger CreateLogger(string categoryName) => new AppLogger(this, categoryName);
    public void AddProvider(ILoggerProvider provider) { }

    // ILoggerProvider
    ILogger ILoggerProvider.CreateLogger(string categoryName) => new AppLogger(this, categoryName);

    public void Dispose() { }

    private AppLogService() { }
}

/// <summary>
/// Per-category logger that forwards to <see cref="AppLogService"/>.
/// Also implements <see cref="ILogger{T}"/> via the non-generic <see cref="ILogger"/>.
/// </summary>
internal sealed class AppLogger : ILogger
{
    private readonly AppLogService _service;
    private readonly string _category;

    public AppLogger(AppLogService service, string category)
    {
        _service = service;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (exception != null)
            message += Environment.NewLine + exception;
        _service.AddEntry(logLevel, _category, message);
    }
}

/// <summary>
/// Generic ILogger&lt;T&gt; implementation backed by <see cref="AppLogService"/>.
/// </summary>
public sealed class AppLogger<T> : ILogger<T>
{
    private readonly ILogger _inner;

    public AppLogger(AppLogService service)
    {
        _inner = service.CreateLogger(typeof(T).Name);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _inner.Log(logLevel, eventId, state, exception, formatter);
}

public sealed class LogEntry : McpServer.UI.Core.Services.ILogEntry
{
    public DateTime Timestamp { get; }
    public Microsoft.Extensions.Logging.LogLevel Level { get; }
    public string Source { get; }
    public string Message { get; }
    public string Display => $"[{Timestamp:HH:mm:ss.fff}] [{Level}] {(string.IsNullOrEmpty(Source) ? "" : $"[{Source}] ")}{Message}";

    public LogEntry(DateTime timestamp, Microsoft.Extensions.Logging.LogLevel level, string source, string message)
    {
        Timestamp = timestamp;
        Level = level;
        Source = source;
        Message = message;
    }
}

