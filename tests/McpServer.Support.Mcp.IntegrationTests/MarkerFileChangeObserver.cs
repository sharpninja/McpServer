namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// Observes marker-file rewrites. Changed/Created-only watchers miss atomic replace
/// and drop events under suite load; Renamed plus a timestamp/length poll does not.
/// </summary>
public static class MarkerFileChangeObserver
{
    /// <summary>How the observer waits for a marker rewrite.</summary>
    public enum Mode
    {
        /// <summary>Prior race: Changed and Created only. No Renamed. No poll.</summary>
        ChangedCreatedOnly = 0,

        /// <summary>Current contract: Renamed plus timestamp/length poll.</summary>
        RenamedAndPoll = 1,
    }

    /// <summary>
    /// Completes when <paramref name="markerPath"/> is rewritten according to <paramref name="mode"/>.
    /// </summary>
    public static async Task WatchAsync(
        string markerPath,
        FileSystemWatcher? watcher,
        Mode mode,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markerPath);

        var beforeExists = File.Exists(markerPath);
        var beforeWrite = beforeExists ? File.GetLastWriteTimeUtc(markerPath) : DateTime.MinValue;
        var beforeLength = beforeExists ? new FileInfo(markerPath).Length : -1L;
        string? beforeContent = null;
        if (beforeExists)
            beforeContent = TryReadShared(markerPath);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        FileSystemEventHandler? changedHandler = null;
        FileSystemEventHandler? createdHandler = null;
        RenamedEventHandler? renamedHandler = null;

        void Complete()
        {
            if (watcher is not null)
            {
                if (changedHandler is not null)
                    watcher.Changed -= changedHandler;
                if (createdHandler is not null)
                    watcher.Created -= createdHandler;
                if (renamedHandler is not null)
                    watcher.Renamed -= renamedHandler;
            }

            tcs.TrySetResult();
        }

        if (watcher is not null)
        {
            changedHandler = (_, _) => Complete();
            createdHandler = (_, _) => Complete();
            watcher.Changed += changedHandler;
            watcher.Created += createdHandler;
            if (mode == Mode.RenamedAndPoll)
            {
                renamedHandler = (_, _) => Complete();
                watcher.Renamed += renamedHandler;
            }
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);
        try
        {
            while (!tcs.Task.IsCompleted)
            {
                linked.Token.ThrowIfCancellationRequested();
                if (mode == Mode.RenamedAndPoll && File.Exists(markerPath))
                {
                    var info = new FileInfo(markerPath);
                    info.Refresh();
                    if (info.LastWriteTimeUtc > beforeWrite || info.Length != beforeLength)
                    {
                        Complete();
                        break;
                    }

                    if (beforeContent is not null)
                    {
                        var current = TryReadShared(markerPath);
                        if (current is not null && current != beforeContent)
                        {
                            Complete();
                            break;
                        }
                    }
                }

                await Task.WhenAny(tcs.Task, Task.Delay(50, linked.Token)).ConfigureAwait(false);
            }

            await tcs.Task.WaitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Complete();
            throw new TimeoutException($"Marker file was not observed within {timeout.TotalSeconds:0} s at {markerPath}");
        }
    }

    private static string? TryReadShared(string path)
    {
        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
