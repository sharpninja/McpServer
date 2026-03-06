using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Default no-op clipboard implementation.
/// </summary>
public sealed class NoOpClipboardService : IClipboardService
{
    /// <inheritdoc />
    public Task SetTextAsync(string text) => Task.CompletedTask;
}

/// <summary>
/// Default no-op app log source implementation.
/// </summary>
public sealed class NoOpAppLogService : IAppLogService
{
    private static readonly IReadOnlyList<ILogEntry> EmptyEntries = Array.Empty<ILogEntry>();

    /// <inheritdoc />
    public event Action<ILogEntry>? EntryAdded
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public IReadOnlyList<ILogEntry> Entries => EmptyEntries;

    /// <inheritdoc />
    public void Clear()
    {
    }
}
