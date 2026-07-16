namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-DB-006: serializes async work per string key while allowing different keys to run
/// concurrently. Used to bound concurrent session-log turn deletes on a single session so a burst
/// cannot exhaust/poison the SQL Server connection pool (BUG-TRIAGE-079). Per-key semaphores are
/// reference-counted and removed once no waiter or holder remains.
/// </summary>
public sealed class KeyedAsyncLock
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    /// <summary>Acquires the lock for <paramref name="key"/>; dispose the result to release.</summary>
    /// <param name="key">Serialization key (for example sourceType/sessionId).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable releaser held for the duration of the critical section.</returns>
    public async Task<IAsyncDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        Entry entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out entry!))
            {
                entry = new Entry();
                _entries[key] = entry;
            }
            else
            {
                entry.RefCount++;
            }
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            ReleaseRef(key, entry, semaphoreWasEntered: false);
            throw;
        }

        return new Releaser(this, key, entry);
    }

    private void ReleaseRef(string key, Entry entry, bool semaphoreWasEntered)
    {
        lock (_gate)
        {
            if (semaphoreWasEntered)
                entry.Semaphore.Release();

            entry.RefCount--;
            if (entry.RefCount == 0)
            {
                _entries.Remove(key);
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class Entry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount = 1;
    }

    private sealed class Releaser(KeyedAsyncLock owner, string key, Entry entry) : IAsyncDisposable
    {
        private int _disposed;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.ReleaseRef(key, entry, semaphoreWasEntered: true);
            return ValueTask.CompletedTask;
        }
    }
}
