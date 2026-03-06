using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Clipboard abstraction for UI.Core view models.
/// </summary>
public interface IClipboardService
{
    /// <summary>Copies text to clipboard.</summary>
    Task SetTextAsync(string text);
}

/// <summary>
/// Read-only log entry contract for log viewers.
/// </summary>
public interface ILogEntry
{
    /// <summary>Entry timestamp.</summary>
    DateTime Timestamp { get; }

    /// <summary>Entry level.</summary>
    LogLevel Level { get; }

    /// <summary>Category/source name.</summary>
    string Source { get; }

    /// <summary>Entry message text.</summary>
    string Message { get; }

    /// <summary>Formatted display text.</summary>
    string Display { get; }
}

/// <summary>
/// Log stream abstraction consumed by log view models.
/// </summary>
public interface IAppLogService
{
    /// <summary>Raised when a new entry is appended.</summary>
    event Action<ILogEntry>? EntryAdded;

    /// <summary>Current log entry snapshot.</summary>
    IReadOnlyList<ILogEntry> Entries { get; }

    /// <summary>Clears all log entries.</summary>
    void Clear();
}
