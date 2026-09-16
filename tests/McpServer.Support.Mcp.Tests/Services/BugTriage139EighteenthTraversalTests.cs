using System.Diagnostics;
using System.Reflection;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Tests.Storage;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-020-AC001: Eighteenth-review consumer coverage for bounded, physically
/// contained requirements snapshot traversal.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139EighteenthTraversalTests
{
    /// <summary>
    /// Proves a metadata stall cannot hold the public caller indefinitely and that the root remains
    /// physically pinned until the isolated traversal worker actually exits.
    /// </summary>
    [Fact]
    public async Task EnumerateFilesBoundedAsync_BlockingMetadata_TimesOutWithTruthfulWorkerAccounting()
    {
        using var fixture = new TraversalFixture();
        var method = ResolveBoundedEnumeration();
        var directoryHook = ResolveHook("s_beforeTraversalDirectoryRead");
        var activeWorkers = ResolveProperty("ActiveTraversalWorkerCount");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task? operation = null;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            directoryHook.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait();
                }));
            operation = InvokeEnumeration(
                method,
                fixture.Root,
                fixture.LiveDirectory,
                TimeSpan.FromMilliseconds(100),
                CancellationToken.None);
            Assert.True(
                entered.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
                "Traversal did not reach the deterministic metadata boundary.");

            var exception = await Assert.ThrowsAsync<TimeoutException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            stopwatch.Stop();

            Assert.Contains("traversal", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Timeout took {stopwatch.Elapsed}.");
            Assert.Equal(1, ReadCounter(activeWorkers));
            Assert.ThrowsAny<IOException>(() => Directory.Move(fixture.Root, fixture.RenamedRoot));
        }
        finally
        {
            release.Set();
            directoryHook.SetValue(null, null);
            Assert.True(
                SpinWait.SpinUntil(() => ReadCounter(activeWorkers) == 0, TimeSpan.FromSeconds(2)),
                "Traversal worker remained counted after its actual exit.");
        }
    }

    /// <summary>
    /// Proves exact caller cancellation is observed while a metadata operation is blocked, without
    /// treating a detached worker as completed.
    /// </summary>
    [Fact]
    public async Task EnumerateFilesBoundedAsync_BlockingMetadata_ObservesExactCallerCancellation()
    {
        using var fixture = new TraversalFixture();
        var method = ResolveBoundedEnumeration();
        var directoryHook = ResolveHook("s_beforeTraversalDirectoryRead");
        var activeWorkers = ResolveProperty("ActiveTraversalWorkerCount");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        Task? operation = null;

        try
        {
            directoryHook.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait();
                }));
            operation = InvokeEnumeration(
                method,
                fixture.Root,
                fixture.LiveDirectory,
                TimeSpan.FromSeconds(10),
                cancellation.Token);
            Assert.True(
                entered.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken),
                "Traversal did not reach the deterministic metadata boundary.");

            cancellation.Cancel();
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);

            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.Equal(1, ReadCounter(activeWorkers));
        }
        finally
        {
            release.Set();
            directoryHook.SetValue(null, null);
            Assert.True(
                SpinWait.SpinUntil(() => ReadCounter(activeWorkers) == 0, TimeSpan.FromSeconds(2)),
                "Traversal worker remained counted after cancellation cleanup.");
        }
    }

    /// <summary>
    /// Proves a directory replaced by an external junction after the root is pinned is rejected
    /// before any external file is opened.
    /// </summary>
    [Fact]
    public async Task EnumerateFilesBoundedAsync_DirectorySwap_DoesNotTraverseExternalJunction()
    {
        using var fixture = new TraversalFixture();
        var method = ResolveBoundedEnumeration();
        var rootPinnedHook = ResolveHook("s_afterTraversalRootPinned");
        var beforeFileOpenHook = ResolveHook("s_beforeTraversalFileOpen");
        var externalOpenCount = 0;

        rootPinnedHook.SetValue(null, (Action)fixture.SwapLiveDirectoryForExternalAlias);
        beforeFileOpenHook.SetValue(
            null,
            (Action<string>)(path =>
            {
                if (path.Contains("external-only.md", StringComparison.OrdinalIgnoreCase))
                    Interlocked.Increment(ref externalOpenCount);
            }));

        try
        {
            var operation = InvokeEnumeration(
                method,
                fixture.Root,
                fixture.LiveDirectory,
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<IOException>(() => operation).ConfigureAwait(true);
            Assert.Equal(0, Volatile.Read(ref externalOpenCount));
            Assert.Equal("do-not-read", File.ReadAllText(fixture.ExternalFile));
        }
        finally
        {
            beforeFileOpenHook.SetValue(null, null);
            rootPinnedHook.SetValue(null, null);
            fixture.RestoreLiveDirectory();
        }
    }

    private static MethodInfo ResolveBoundedEnumeration()
    {
        var method = typeof(WorkspaceContainedFileSystem).GetMethod(
            "EnumerateFilesBoundedAsync",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return method!;
    }

    private static FieldInfo ResolveHook(string name)
    {
        var field = typeof(WorkspaceContainedFileSystem).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Null(field!.GetValue(null));
        return field;
    }

    private static PropertyInfo ResolveProperty(string name)
    {
        var property = typeof(WorkspaceContainedFileSystem).GetProperty(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(property);
        return property!;
    }

    private static int ReadCounter(PropertyInfo property) => (int)property.GetValue(null)!;

    private static Task InvokeEnumeration(
        MethodInfo method,
        string root,
        string directory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var result = method.Invoke(
            null,
            [
                root,
                directory,
                new WorkspaceTraversalLimits(),
                timeout,
                cancellationToken,
            ]);
        return Assert.IsAssignableFrom<Task>(result);
    }

    private sealed class TraversalFixture : IDisposable
    {
        private readonly string _holdingDirectory;
        private readonly string _parkedAlias;
        private bool _swapped;

        internal TraversalFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), $"bug-triage-139-traversal-{Guid.NewGuid():N}");
            RenamedRoot = Root + "-renamed";
            ExternalRoot = Path.Combine(Path.GetTempPath(), $"bug-triage-139-external-{Guid.NewGuid():N}");
            LiveDirectory = Path.Combine(Root, "wiki");
            _holdingDirectory = Path.Combine(Root, "wiki-safe");
            _parkedAlias = Path.Combine(Root, "wiki-external-alias");
            ExternalFile = Path.Combine(ExternalRoot, "external-only.md");

            Directory.CreateDirectory(LiveDirectory);
            Directory.CreateDirectory(ExternalRoot);
            File.WriteAllText(Path.Combine(LiveDirectory, "inside.md"), "inside");
            File.WriteAllText(ExternalFile, "do-not-read");
            CreateDirectoryAlias(_parkedAlias, ExternalRoot);
        }

        internal string Root { get; }

        internal string RenamedRoot { get; }

        internal string ExternalRoot { get; }

        internal string LiveDirectory { get; }

        internal string ExternalFile { get; }

        internal void SwapLiveDirectoryForExternalAlias()
        {
            Directory.Move(LiveDirectory, _holdingDirectory);
            Directory.Move(_parkedAlias, LiveDirectory);
            _swapped = true;
        }

        internal void RestoreLiveDirectory()
        {
            if (!_swapped)
                return;

            if (Directory.Exists(LiveDirectory))
                Directory.Move(LiveDirectory, _parkedAlias);
            if (Directory.Exists(_holdingDirectory))
                Directory.Move(_holdingDirectory, LiveDirectory);
            _swapped = false;
        }

        public void Dispose()
        {
            RestoreLiveDirectory();
            DeleteAlias(_parkedAlias);
            if (Directory.Exists(RenamedRoot))
                Directory.Move(RenamedRoot, Root);
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
            if (Directory.Exists(ExternalRoot))
                Directory.Delete(ExternalRoot, recursive: true);
        }

        private static void CreateDirectoryAlias(string aliasPath, string targetPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                Directory.CreateSymbolicLink(aliasPath, targetPath);
                return;
            }

            var start = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            start.ArgumentList.Add("/d");
            start.ArgumentList.Add("/c");
            start.ArgumentList.Add("mklink");
            start.ArgumentList.Add("/J");
            start.ArgumentList.Add(aliasPath);
            start.ArgumentList.Add(targetPath);
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("Unable to start mklink.");
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Unable to create test junction: {process.StandardError.ReadToEnd()}");
            }
        }

        private static void DeleteAlias(string path)
        {
            if (!Directory.Exists(path))
                return;
            Directory.Delete(path);
        }
    }
}
