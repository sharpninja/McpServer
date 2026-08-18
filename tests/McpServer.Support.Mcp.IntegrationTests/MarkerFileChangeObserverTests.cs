using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// TEST-HANDOFF / suite-load: marker observation must fail the prior Changed-only race
/// and pass when Renamed plus poll is enabled.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MarkerFileChangeObserverTests
{
    /// <summary>
    /// Prior race: when FileSystemWatcher drops events (EnableRaisingEvents false),
    /// Changed/Created-only never completes.
    /// </summary>
    [Fact]
    public async Task ChangedCreatedOnly_DroppedEvents_TimesOut()
    {
        var root = CreateRoot();
        try
        {
            var markerPath = Path.Combine(root, MarkerFileService.MarkerFileName);
            await File.WriteAllTextAsync(markerPath, "before", TestContext.Current.CancellationToken).ConfigureAwait(true);
            using var watcher = CreateWatcher(root, raiseEvents: false);
            var watch = MarkerFileChangeObserver.WatchAsync(
                markerPath,
                watcher,
                MarkerFileChangeObserver.Mode.ChangedCreatedOnly,
                TimeSpan.FromMilliseconds(400),
                TestContext.Current.CancellationToken);

            await AtomicReplaceAsync(markerPath, "after").ConfigureAwait(true);

            var ex = await Assert.ThrowsAsync<TimeoutException>(() => watch).ConfigureAwait(true);
            Assert.Contains(markerPath, ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Current contract: timestamp/length poll observes the rewrite even when the watcher is silent.
    /// </summary>
    [Fact]
    public async Task RenamedAndPoll_DroppedEvents_ObservesAtomicReplace()
    {
        var root = CreateRoot();
        try
        {
            var markerPath = Path.Combine(root, MarkerFileService.MarkerFileName);
            await File.WriteAllTextAsync(markerPath, "before", TestContext.Current.CancellationToken).ConfigureAwait(true);
            using var watcher = CreateWatcher(root, raiseEvents: false);
            var watch = MarkerFileChangeObserver.WatchAsync(
                markerPath,
                watcher,
                MarkerFileChangeObserver.Mode.RenamedAndPoll,
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken);

            await AtomicReplaceAsync(markerPath, "after-poll").ConfigureAwait(true);
            await watch.ConfigureAwait(true);

            Assert.Equal("after-poll", await File.ReadAllTextAsync(markerPath, TestContext.Current.CancellationToken).ConfigureAwait(true));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "marker-observe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static FileSystemWatcher CreateWatcher(string root, bool raiseEvents)
        => new(root, MarkerFileService.MarkerFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName,
            InternalBufferSize = 64 * 1024,
            EnableRaisingEvents = raiseEvents,
        };

    private static async Task AtomicReplaceAsync(string markerPath, string content)
    {
        var temp = markerPath + ".tmp";
        await File.WriteAllTextAsync(temp, content, TestContext.Current.CancellationToken).ConfigureAwait(true);
        File.Replace(temp, markerPath, destinationBackupFileName: null);
    }
}
