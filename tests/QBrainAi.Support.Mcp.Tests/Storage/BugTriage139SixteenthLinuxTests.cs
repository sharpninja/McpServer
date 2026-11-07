using System.Diagnostics;
using System.Reflection;
using QBrainAi.Client;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC001: Native Linux alias, replacement, and worker-lifecycle coverage.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class BugTriage139SixteenthLinuxTests
{
    /// <summary>
    /// Verifies that a lexical path on a safe local filesystem which physically enters a delayed
    /// FUSE mount through a symbolic link is rejected by the atomic no-cross-device lookup before
    /// the target mount is traversed.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact(Timeout = 60_000)]
    public async Task WorkspaceIdentity_SafeSymlinkIntoFuse_IsRejectedByAtomicNoCrossDeviceResolver()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);

        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var safeRoot = Path.Combine(fixture.RootPath, "safe-alias-root");
        var alias = Path.Combine(safeRoot, "alias");
        Directory.CreateDirectory(safeRoot);
        Directory.CreateSymbolicLink(alias, fixture.MountPath);

        var invocationField = ResolveField("s_nativeResolverInvocationCount");
        var activeField = ResolveField("s_activeNativeResolverWorkers");
        var invocationsBefore = ReadCounter(invocationField);
        var stopwatch = Stopwatch.StartNew();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => WorkspaceIdentityPath.ResolveAsync(
                    Path.Combine(alias, "blocked"),
                    WorkspacePathPlatform.CaseSensitive,
                    TimeSpan.FromMilliseconds(250),
                    TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        stopwatch.Stop();
        Assert.Contains("fuse", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"Physical-alias rejection took {stopwatch.Elapsed}.");
        Assert.Equal(invocationsBefore + 1, ReadCounter(invocationField));
        Assert.Equal(0, ReadCounter(activeField));
    }

    /// <summary>
    /// Verifies that replacing a previously safe alias with a delayed FUSE target between lexical
    /// classification and physical lookup is rejected by the atomic no-cross-device resolver.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact(Timeout = 60_000)]
    public async Task WorkspaceIdentity_AliasReplacement_IsRejectedByAtomicNoCrossDeviceResolver()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);

        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var safeRoot = Path.Combine(fixture.RootPath, "replacement-root");
        var safeTarget = Path.Combine(fixture.RootPath, "replacement-target");
        var alias = Path.Combine(safeRoot, "alias");
        Directory.CreateDirectory(safeRoot);
        Directory.CreateDirectory(safeTarget);
        Directory.CreateSymbolicLink(alias, safeTarget);

        var boundaryField = ResolveField("s_beforeUnixPhysicalResolution");
        var invocationField = ResolveField("s_nativeResolverInvocationCount");
        var invocationsBefore = ReadCounter(invocationField);
        var swapped = 0;
        boundaryField.SetValue(
            null,
            (Action)(() =>
            {
                if (Interlocked.Exchange(ref swapped, 1) != 0)
                    return;
                Directory.Delete(alias);
                Directory.CreateSymbolicLink(alias, fixture.MountPath);
            }));

        try
        {
            var exception = await Assert.ThrowsAsync<NotSupportedException>(
                    () => WorkspaceIdentityPath.ResolveAsync(
                        Path.Combine(alias, "blocked"),
                        WorkspacePathPlatform.CaseSensitive,
                        TimeSpan.FromMilliseconds(250),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Contains("fuse", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, Volatile.Read(ref swapped));
            Assert.Equal(invocationsBefore + 1, ReadCounter(invocationField));
        }
        finally
        {
            boundaryField.SetValue(null, null);
        }
    }

    /// <summary>
    /// Verifies that a local symbolic link into a real delayed FUSE filesystem is rejected before
    /// any SQLite worker or native handle is created.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact(Timeout = 60_000)]
    public async Task SqliteOpen_SafeSymlinkIntoFuse_IsRejectedBeforeWorker()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);

        var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var safeRoot = Path.Combine(fixture.RootPath, "sqlite-safe-root");
        var alias = Path.Combine(safeRoot, "alias");
        Directory.CreateDirectory(safeRoot);
        Directory.CreateSymbolicLink(alias, fixture.MountPath);

        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(alias, "blocked.db"),
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            }.ToString());
        var invocationsBefore = SqliteBoundedConnectionOpener.WorkerInvocationCount;
        Task? operation = null;
        Exception? publicFailure = null;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromMilliseconds(250),
                TestContext.Current.CancellationToken);
            try
            {
                await operation
                    .WaitAsync(
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }
            catch (Exception operationFailure)
            {
                publicFailure = operationFailure;
            }

            stopwatch.Stop();
        }
        finally
        {
            await fixture.DisposeAsync().ConfigureAwait(true);
            if (operation is not null)
            {
                try
                {
                    await operation
                        .WaitAsync(
                            TimeSpan.FromSeconds(2),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true);
                }
                catch (Exception)
                {
                    // The behavioral failure was captured before releasing the delayed mount.
                }
            }
        }

        var exception = Assert.IsType<NotSupportedException>(publicFailure);
        Assert.Contains("fuse", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"SQLite FUSE rejection exceeded one second: {stopwatch.Elapsed}.");
        Assert.Equal(invocationsBefore, SqliteBoundedConnectionOpener.WorkerInvocationCount);
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
    }

    /// <summary>
    /// BUG-BASELINE-009 (C1): a local symbolic link to a path below the root of a delayed FUSE mount is rejected
    /// by classifying the link target in the mount table, without the preflight entering the mount. Following the
    /// link with stat would send the helper a lookup it never answers; only its root is answered, which is why the
    /// root-target cases above cannot catch this.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact(Timeout = 60_000)]
    public async Task SqliteOpen_SafeSymlinkBelowFuseRoot_IsRejectedWithoutEnteringTheMount()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);

        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var safeRoot = Path.Combine(fixture.RootPath, "sqlite-below-root");
        var alias = Path.Combine(safeRoot, "alias");
        Directory.CreateDirectory(safeRoot);
        Directory.CreateSymbolicLink(alias, Path.Combine(fixture.MountPath, "inside"));

        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(alias, "blocked.db"),
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            }.ToString());
        var invocationsBefore = SqliteBoundedConnectionOpener.WorkerInvocationCount;
        Exception? publicFailure = null;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var opened = await SqliteBoundedConnectionOpener.OpenAsync(
                    connection,
                    TimeSpan.FromMilliseconds(250),
                    TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }
        catch (Exception operationFailure)
        {
            publicFailure = operationFailure;
        }

        stopwatch.Stop();
        var exception = Assert.IsType<NotSupportedException>(publicFailure);
        Assert.Contains("fuse", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"Rejection of a link below the FUSE root took {stopwatch.Elapsed}.");
        Assert.Equal(invocationsBefore, SqliteBoundedConnectionOpener.WorkerInvocationCount);
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
    }

    /// <summary>
    /// Verifies replacement of a previously safe SQLite alias at the validation boundary cannot
    /// reach the synchronous open worker.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact(Timeout = 60_000)]
    public async Task SqliteOpen_AliasReplacementBeforePhysicalResolution_IsRejected()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);

        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var safeRoot = Path.Combine(fixture.RootPath, "sqlite-replacement-root");
        var safeTarget = Path.Combine(fixture.RootPath, "sqlite-replacement-target");
        var alias = Path.Combine(safeRoot, "alias");
        Directory.CreateDirectory(safeRoot);
        Directory.CreateDirectory(safeTarget);
        Directory.CreateSymbolicLink(alias, safeTarget);

        var boundaryField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforeDataSourcePhysicalResolution",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(boundaryField);
        Assert.Null(boundaryField!.GetValue(null));

        var swapped = 0;
        boundaryField.SetValue(
            null,
            (Action)(() =>
            {
                if (Interlocked.Exchange(ref swapped, 1) != 0)
                    return;
                Directory.Delete(alias);
                Directory.CreateSymbolicLink(alias, fixture.MountPath);
            }));
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(alias, "replacement.db"),
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            }.ToString());
        var invocationsBefore = SqliteBoundedConnectionOpener.WorkerInvocationCount;
        try
        {
            var exception = await Assert.ThrowsAsync<NotSupportedException>(
                    () => SqliteBoundedConnectionOpener.OpenAsync(
                        connection,
                        TimeSpan.FromMilliseconds(250),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Contains("fuse", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            boundaryField.SetValue(null, null);
        }

        Assert.Equal(1, Volatile.Read(ref swapped));
        Assert.Equal(invocationsBefore, SqliteBoundedConnectionOpener.WorkerInvocationCount);
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
    }

    /// <summary>
    /// Verifies that replacing a safe alias after the worker's last validation cannot reach native
    /// SQLite opening: the actual file descriptor is acquired by one atomic no-cross-device open.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact(Timeout = 60_000)]
    public async Task SqliteOpen_AliasReplacementAtNativeOpen_IsAtomicallyRejected()
    {
        Assert.True(OperatingSystem.IsLinux());
        Assert.True(LinuxDelayedFuseFixture.IsSupported);

        await using var fixture = await LinuxDelayedFuseFixture.CreateAsync().ConfigureAwait(true);
        var safeRoot = Path.Combine(fixture.RootPath, "sqlite-native-open-root");
        var safeTarget = Path.Combine(fixture.RootPath, "sqlite-native-open-target");
        var holdingTarget = safeTarget + "-holding";
        var alias = Path.Combine(safeRoot, "alias");
        Directory.CreateDirectory(safeRoot);
        Directory.CreateDirectory(safeTarget);
        Directory.CreateSymbolicLink(alias, safeTarget);

        var boundaryField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(boundaryField);
        Assert.Null(boundaryField!.GetValue(null));

        var swapped = 0;
        boundaryField.SetValue(
            null,
            (Action)(() =>
            {
                if (Interlocked.Exchange(ref swapped, 1) != 0)
                    return;
                Directory.Move(safeTarget, holdingTarget);
                Directory.CreateSymbolicLink(safeTarget, fixture.MountPath);
            }));
        var originalConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(alias, "native-open.db"),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString();
        using var connection = new SqliteConnection(originalConnectionString);
        var invocationsBefore = SqliteBoundedConnectionOpener.WorkerInvocationCount;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var exception = await Assert.ThrowsAsync<NotSupportedException>(
                    () => SqliteBoundedConnectionOpener.OpenAsync(
                        connection,
                        TimeSpan.FromMilliseconds(250),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Contains("FUSE", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            stopwatch.Stop();
            boundaryField.SetValue(null, null);
            try
            {
                File.Delete(safeTarget);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (Directory.Exists(safeTarget))
                    Directory.Delete(safeTarget);
            }

            if (Directory.Exists(holdingTarget))
                Directory.Move(holdingTarget, safeTarget);
        }
        Assert.Equal(1, Volatile.Read(ref swapped));
        Assert.Equal(invocationsBefore + 1, SqliteBoundedConnectionOpener.WorkerInvocationCount);
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(System.Data.ConnectionState.Closed, connection.State);
        Assert.Equal(originalConnectionString, connection.ConnectionString);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"Atomic SQLite rejection exceeded one second: {stopwatch.Elapsed}.");
    }

    /// <summary>
    /// Verifies that an intentionally unstoppable Unix helper cannot extend public completion and
    /// remains counted until the actual helper process exits.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    public async Task WorkspaceIdentity_UnstoppableUnixHelper_RemainsObservableAfterPublicTimeout()
    {
        Assert.True(OperatingSystem.IsLinux());

        var processFactoryField = ResolveField("s_unixResolverProcessFactory");
        var terminationField = ResolveField("s_unixResolverTerminationOverride");
        var activeField = ResolveField("s_activeNativeResolverWorkers");
        Process? helper = null;
        processFactoryField.SetValue(
            null,
            (Func<ProcessStartInfo, Process>)(_ =>
            {
                var startInfo = new ProcessStartInfo("/bin/sh")
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add("while :; do sleep 1; done");
                helper = Process.Start(startInfo)
                    ?? throw new InvalidOperationException("Unable to start the Linux lifecycle helper.");
                return helper;
            }));
        terminationField.SetValue(null, (Func<Process, bool>)(_ => false));

        Task<(string NormalizedPath, string StorageKey)>? operation = null;
        Exception? publicFailure = null;
        var activeAfterPublicCompletion = 0;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            operation = WorkspaceIdentityPath.ResolveAsync(
                Path.GetTempPath(),
                WorkspacePathPlatform.CaseSensitive,
                TimeSpan.FromMilliseconds(100),
                TestContext.Current.CancellationToken);
            try
            {
                _ = await operation.WaitAsync(
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }
            catch (Exception operationFailure)
            {
                publicFailure = operationFailure;
            }

            stopwatch.Stop();
            activeAfterPublicCompletion = ReadCounter(activeField);
        }
        finally
        {
            processFactoryField.SetValue(null, null);
            terminationField.SetValue(null, null);
            if (helper is not null)
            {
                try
                {
                    if (!helper.HasExited)
                        helper.Kill(entireProcessTree: true);
                    await helper.WaitForExitAsync(TestContext.Current.CancellationToken)
                        .WaitAsync(
                            TimeSpan.FromSeconds(2),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true);
                }
                catch (InvalidOperationException)
                {
                    // The helper exited between observation and cleanup.
                }
                finally
                {
                    helper.Dispose();
                }
            }

            if (operation is not null)
            {
                try
                {
                    _ = await operation.WaitAsync(
                            TimeSpan.FromSeconds(2),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true);
                }
                catch (Exception)
                {
                    // The public result was captured before releasing the simulated leaked worker.
                }
            }
        }

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"Public resolution exceeded its one-second bound: {stopwatch.Elapsed}.");
        Assert.IsType<TimeoutException>(publicFailure);
        Assert.Equal(1, activeAfterPublicCompletion);
        Assert.True(
            SpinWait.SpinUntil(() => ReadCounter(activeField) == 0, TimeSpan.FromSeconds(2)),
            "The Unix resolver count did not clear after the helper actually exited.");
    }


    /// <summary>
    /// Proves the bounded opener can transact through a pinned Linux descriptor, release the pin,
    /// restore the original connection string on close, and reopen the same connection.
    /// </summary>
    /// <remarks>Integration: starts a real Unix resolver helper and measures its lifecycle after the public timeout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    public async Task SqlitePinnedDataSource_SupportsTransactionsAndReopen()
    {
        Assert.True(OperatingSystem.IsLinux());

        var root = Path.Combine(
            Path.GetTempPath(),
            "mcp-sqlite-descriptor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var databasePath = Path.Combine(root, "pinned.db");
        var originalConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false,
        }.ToString();
        var invocationsBefore = SqliteBoundedConnectionOpener.WorkerInvocationCount;
        SqliteConnection? connection = null;
        try
        {
            var template = new SqliteConnection(originalConnectionString);
            var originalDefaultTimeout = template.DefaultTimeout;
            var openedConnection = await SqliteBoundedConnectionOpener.OpenAsync(
                    template,
                    TimeSpan.FromSeconds(2),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            if (!ReferenceEquals(openedConnection, template))
                await template.DisposeAsync().ConfigureAwait(true);
            connection = openedConnection;
            Assert.Equal(originalDefaultTimeout, connection.DefaultTimeout);

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE sample(value INTEGER NOT NULL);";
                _ = await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }

            await using (var transaction = await connection
                             .BeginTransactionAsync(TestContext.Current.CancellationToken)
                             .ConfigureAwait(true))
            {
                await using var command = connection.CreateCommand();
                command.Transaction = (SqliteTransaction)transaction;
                command.CommandText = "INSERT INTO sample(value) VALUES (42);";
                Assert.Equal(
                    1,
                    await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)
                        .ConfigureAwait(true));
                await transaction.CommitAsync(TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }

            await connection.CloseAsync().ConfigureAwait(true);
            Assert.Equal(originalConnectionString, connection.ConnectionString);

            connection.Open();
            await using (var directVerifyCommand = connection.CreateCommand())
            {
                directVerifyCommand.CommandText =
                    "SELECT COUNT(*) FROM sample WHERE value = 42;";
                Assert.Equal(
                    1L,
                    Assert.IsType<long>(directVerifyCommand.ExecuteScalar()));
            }

            connection.Close();
            Assert.Equal(originalConnectionString, connection.ConnectionString);

            var replacement = await SqliteBoundedConnectionOpener.OpenAsync(
                    connection,
                    TimeSpan.FromSeconds(2),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            if (!ReferenceEquals(replacement, connection))
            {
                await connection.DisposeAsync().ConfigureAwait(true);
                connection = replacement;
            }

            await using (var verifyCommand = connection.CreateCommand())
            {
                verifyCommand.CommandText = "SELECT COUNT(*) FROM sample WHERE value = 42;";
                Assert.Equal(
                    1L,
                    Assert.IsType<long>(
                        await verifyCommand.ExecuteScalarAsync(TestContext.Current.CancellationToken)
                            .ConfigureAwait(true)));
            }

            await connection.CloseAsync().ConfigureAwait(true);
            Assert.Equal(originalConnectionString, connection.ConnectionString);
            Assert.Equal(originalDefaultTimeout, connection.DefaultTimeout);
        }
        finally
        {
            if (connection is not null)
                await connection.DisposeAsync().ConfigureAwait(true);
            Assert.Equal(
                invocationsBefore + 2,
                SqliteBoundedConnectionOpener.WorkerInvocationCount);
            Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static FieldInfo ResolveField(string fieldName)
    {
        var field = typeof(WorkspaceIdentityPath).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return field!;
    }

    private static int ReadCounter(FieldInfo field) =>
        Assert.IsType<int>(field.GetValue(null));
}

/// <summary>
/// Real FUSE filesystem whose non-root metadata lookup blocks until the helper is terminated.
/// </summary>
internal sealed class LinuxDelayedFuseFixture : IAsyncDisposable
{
    private const string Source = """
#define FUSE_USE_VERSION 31
#include <fuse3/fuse.h>
#include <signal.h>
#include <string.h>
#include <sys/stat.h>
#include <unistd.h>

static int delayed_getattr(const char *path, struct stat *metadata, struct fuse_file_info *info)
{
    (void)info;
    memset(metadata, 0, sizeof(*metadata));
    if (strcmp(path, "/") == 0)
    {
        metadata->st_mode = S_IFDIR | 0700;
        metadata->st_nlink = 2;
        return 0;
    }

    for (;;)
        pause();
}

static int delayed_readdir(
    const char *path,
    void *buffer,
    fuse_fill_dir_t filler,
    off_t offset,
    struct fuse_file_info *info,
    enum fuse_readdir_flags flags)
{
    (void)path;
    (void)buffer;
    (void)filler;
    (void)offset;
    (void)info;
    (void)flags;
    for (;;)
        pause();
}

static const struct fuse_operations operations = {
    .getattr = delayed_getattr,
    .readdir = delayed_readdir,
};

int main(int argc, char **argv)
{
    return fuse_main(argc, argv, &operations, NULL);
}
""";

    /// <summary>
    /// BUG-BASELINE-009: hard cap on a fixture's life. Its helpers are killed when it elapses or when the test's own
    /// token is cancelled (the per-test timeout), which fails every pending FUSE request, so a thread blocked in one is
    /// released instead of hanging the run.
    /// </summary>
    private static readonly TimeSpan s_watchdogLimit = TimeSpan.FromSeconds(30);

    private readonly object _gate = new();
    private readonly string _executablePath;
    private readonly Process _process;
    private readonly Task<string> _standardError;
    private readonly List<(string MountPath, Process Process, Task<string> StandardError)> _replacementMounts = [];
    private readonly CancellationTokenSource _watchdog;
    private readonly CancellationTokenRegistration _watchdogRegistration;
    private bool _stopped;

    private LinuxDelayedFuseFixture(
        string rootPath,
        string mountPath,
        string executablePath,
        Process process,
        Task<string> standardError)
    {
        RootPath = rootPath;
        MountPath = mountPath;
        _executablePath = executablePath;
        _process = process;
        _standardError = standardError;
        _watchdog = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        _watchdog.CancelAfter(s_watchdogLimit);
        _watchdogRegistration = _watchdog.Token.Register(KillHelpers);
    }

    internal static bool IsSupported =>
        OperatingSystem.IsLinux() &&
        File.Exists("/dev/fuse") &&
        File.Exists("/usr/bin/gcc") &&
        File.Exists("/usr/include/fuse3/fuse.h") &&
        File.Exists("/usr/bin/fusermount3");

    internal string RootPath { get; }

    internal string MountPath { get; }

    internal static async Task<LinuxDelayedFuseFixture> CreateAsync()
    {
        Assert.True(IsSupported, "Linux FUSE native-test prerequisites are required.");
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            "mcp-sixteenth-fuse-" + Guid.NewGuid().ToString("N"));
        var mountPath = Path.Combine(rootPath, "mount");
        var sourcePath = Path.Combine(rootPath, "delayed-fuse.c");
        var executablePath = Path.Combine(rootPath, "delayed-fuse");
        Directory.CreateDirectory(mountPath);
        await File.WriteAllTextAsync(
                sourcePath,
                Source,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await RunProcessAsync(
                "/usr/bin/gcc",
                [sourcePath, "-o", executablePath, "-lfuse3", "-pthread"],
                TimeSpan.FromSeconds(20))
            .ConfigureAwait(true);

        var (process, standardError) = StartHelper(executablePath, mountPath);
        var fixture = new LinuxDelayedFuseFixture(rootPath, mountPath, executablePath, process, standardError);
        try
        {
            await WaitForMountAsync(mountPath, process, standardError).ConfigureAwait(true);
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync().ConfigureAwait(true);
            throw;
        }
    }

    /// <summary>
    /// Mounts a second instance of this delayed FUSE filesystem at an existing replacement-root path. The helper mounts
    /// through the unprivileged fusermount3, so a non-root test user can replace a root with a real delayed FUSE mount
    /// (BUG-BASELINE-009, operator decision 2026-10-11; the root-only mount --bind it replaces exits 32 for non-root users).
    /// </summary>
    /// <param name="destination">Empty directory that becomes the replacement root mount.</param>
    internal async Task MountDelayedFuseAtAsync(string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        Directory.CreateDirectory(destination);
        var (process, standardError) = StartHelper(_executablePath, destination);
        lock (_gate)
            _replacementMounts.Add((destination, process, standardError));
        if (_watchdog.IsCancellationRequested)
            KillHelpers();
        await WaitForMountAsync(destination, process, standardError).ConfigureAwait(true);
    }

    /// <summary>
    /// Starts one helper instance mounted at <paramref name="mountPath"/>. It runs multi-threaded (no -s), so a request
    /// another process leaves blocked on a non-root path cannot wedge the root lookups the tests rely on.
    /// </summary>
    private static (Process Process, Task<string> StandardError) StartHelper(string executablePath, string mountPath)
    {
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("fsname=bug139-sixteenth");
        startInfo.ArgumentList.Add(mountPath);
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start the delayed FUSE helper.");
        var standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);
        _ = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        return (process, standardError);
    }

    /// <summary>Kills every helper process (watchdog and teardown); each kill aborts that helper's FUSE connection.</summary>
    private void KillHelpers()
    {
        List<Process> processes;
        lock (_gate)
            processes = [_process, .. _replacementMounts.Select(mount => mount.Process)];
        foreach (var process in processes)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The helper already exited or was disposed.
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The helper exited while it was being killed.
            }
        }
    }

    /// <summary>
    /// Detaches every replacement mount and then the original one, each with fusermount3 and a helper kill. Teardown
    /// ignores the test's token (bounded by its own timeouts instead), so a timed-out test still detaches its mounts.
    /// </summary>
    internal async Task StopAsync()
    {
        if (_stopped)
            return;
        _stopped = true;

        List<(string MountPath, Process Process, Task<string> StandardError)> replacements;
        lock (_gate)
            replacements = [.. _replacementMounts];
        foreach (var replacement in replacements.AsEnumerable().Reverse())
        {
            await DetachAsync(replacement.MountPath, replacement.Process, replacement.StandardError)
                .ConfigureAwait(true);
        }

        await DetachAsync(MountPath, _process, _standardError).ConfigureAwait(true);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(true);
        _watchdogRegistration.Dispose();
        _watchdog.Dispose();
        _process.Dispose();
        lock (_gate)
        {
            foreach (var replacement in _replacementMounts)
                replacement.Process.Dispose();
        }

        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, recursive: true);
    }

    private static async Task DetachAsync(string mountPath, Process process, Task<string> standardError)
    {
        try
        {
            await RunProcessAsync(
                    "/usr/bin/fusermount3",
                    ["-u", "-z", mountPath],
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None)
                .ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException)
        {
            // An unconditional helper kill follows a failed or concurrent detach.
        }

        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5))
                .ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException)
        {
            // The helper exited between observation and cleanup, or did not exit in time.
        }

        try
        {
            _ = await standardError.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(true);
        }
        catch (TimeoutException)
        {
            // The helper's standard error stayed open; its process was killed above.
        }
    }

    private static async Task WaitForMountAsync(string mountPath, Process process, Task<string> standardError)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            var mountInfo = await File.ReadAllTextAsync(
                    "/proc/self/mountinfo",
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            if (mountInfo.Contains(
                    " " + mountPath + " ",
                    StringComparison.Ordinal))
            {
                return;
            }

            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    "FUSE helper exited before mounting: " +
                    await standardError.ConfigureAwait(true));
            }

            await Task.Delay(
                    TimeSpan.FromMilliseconds(25),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        throw new TimeoutException($"The FUSE helper did not mount {mountPath} within five seconds.");
    }

    private static Task RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout) =>
        RunProcessAsync(fileName, arguments, timeout, TestContext.Current.CancellationToken);

    private static async Task RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start {fileName}.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken)
                .WaitAsync(timeout, cancellationToken)
                .ConfigureAwait(true);
        }
        catch
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            throw;
        }

        var standardOutput = await output.ConfigureAwait(true);
        var standardError = await error.ConfigureAwait(true);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{fileName} exited {process.ExitCode}: {standardError}{standardOutput}");
        }
    }
}
