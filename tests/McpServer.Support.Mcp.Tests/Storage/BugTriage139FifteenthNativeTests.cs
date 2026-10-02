using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using McpServer.Client;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-019/020: Reusable native scenarios selected by platform-specific compiled
/// test inventories.
/// </summary>
internal static class BugTriage139FifteenthNativeScenarios
{
    /// <summary>
    /// Verifies nested restoration contents, mode, and the no-follow containment boundary.
    /// </summary>
    internal static async Task AssertContainedRestorationAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcp-fifteenth-restore-" + Guid.NewGuid().ToString("N"));
        var external = Path.Combine(Path.GetTempPath(), "mcp-fifteenth-external-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(external);
        try
        {
            var nested = Path.Combine(root, "wiki", "nested", "leaf");
            var restoredFile = Path.Combine(nested, "restored.md");
            if (OperatingSystem.IsLinux())
            {
                // Linux EnsureDirectory fail-closes on missing segments (no mkdirat+openat2
                // RESOLVE_IN_ROOT). Ordinary creation establishes the chain; contained I/O is
                // still proven through OpenFileForWrite/Read.
                Directory.CreateDirectory(nested);
            }

            WorkspaceContainedFileSystem.EnsureDirectory(root, nested);
            await using (var stream = WorkspaceContainedFileSystem.OpenFileForWrite(root, restoredFile))
            {
                stream.SetLength(0);
                await stream.WriteAsync(
                        Encoding.UTF8.GetBytes("fifteenth-restored"),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }

            await using (var stream = WorkspaceContainedFileSystem.OpenFileForRead(root, restoredFile))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                Assert.Equal(
                    "fifteenth-restored",
                    await reader.ReadToEndAsync(TestContext.Current.CancellationToken)
                        .ConfigureAwait(true));
            }

            if (OperatingSystem.IsLinux())
            {
                var required = UnixFileMode.UserRead |
                               UnixFileMode.UserWrite |
                               UnixFileMode.UserExecute;
                Assert.Equal(required, File.GetUnixFileMode(nested) & required);

                var alias = Path.Combine(root, "external-alias");
                Directory.CreateSymbolicLink(alias, external);
                var exception = Assert.Throws<IOException>(
                    () => WorkspaceContainedFileSystem.EnsureDirectory(
                        root,
                        Path.Combine(alias, "escaped", "leaf")));
                Assert.Contains("directory", exception.Message, StringComparison.OrdinalIgnoreCase);
                Assert.False(Directory.Exists(Path.Combine(external, "escaped")));
            }
            else
            {
                Assert.False(
                    File.GetAttributes(nested).HasFlag(FileAttributes.ReparsePoint),
                    "The restored directory must be an ordinary contained directory.");
            }
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
            if (Directory.Exists(external))
                Directory.Delete(external, recursive: true);
        }
    }

    /// <summary>
    /// Verifies exact independent-failure classification at the Windows kernel-wait boundary.
    /// </summary>
    internal static async Task AssertWindowsSqliteIndependentFailureAsync()
    {
        using var pipe = new System.IO.Pipes.NamedPipeServerStream(
            "mcp-fifteenth-classification-" + Guid.NewGuid().ToString("N"),
            System.IO.Pipes.PipeDirection.In,
            1,
            System.IO.Pipes.PipeTransmissionMode.Byte,
            System.IO.Pipes.PipeOptions.None);
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        using var started = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));
        Task<SqliteConnection>? operation = null;
        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    started.Set();
                    try
                    {
                        pipe.WaitForConnection();
                    }
                    catch (Exception exception) when (
                        exception is IOException or OperationCanceledException)
                    {
                        throw new ObjectDisposedException(
                            "real-provider-failure-after-cancel",
                            "real-provider-failure-after-cancel");
                    }
                }));
            operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromSeconds(10),
                cancellation.Token);

            Assert.True(started.Wait(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken));
            cancellation.Cancel();
            var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Equal("real-provider-failure-after-cancel", exception.ObjectName);
            Assert.Contains(
                "real-provider-failure-after-cancel",
                exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            hookField.SetValue(null, null);
        }

        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Null(connection.Handle);
    }

    /// <summary>
    /// Verifies exact independent-failure classification at the Linux worker boundary.
    /// </summary>
    internal static async Task AssertLinuxSqliteIndependentFailureAsync()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        using var release = new ManualResetEventSlim();
        using var started = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));
        Task<SqliteConnection>? operation = null;
        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    started.Set();
                    release.Wait();
                    throw new ObjectDisposedException(
                        "real-provider-failure-after-cancel",
                        "real-provider-failure-after-cancel");
                }));
            operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromSeconds(10),
                cancellation.Token);
            Assert.True(started.Wait(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken));
            cancellation.Cancel();
            release.Set();

            var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Equal("real-provider-failure-after-cancel", exception.ObjectName);
        }
        finally
        {
            release.Set();
            hookField.SetValue(null, null);
        }

        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Null(connection.Handle);
    }


    internal static async Task AssertWindowsIdentityCreateFileCancellationGapAsync()
    {
        var hook = typeof(WorkspaceIdentityPath).GetField(
            "s_beforeWindowsNativeOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hook);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        hook!.SetValue(
            null,
            (Action)(() =>
            {
                entered.Set();
                release.Wait(TestContext.Current.CancellationToken);
            }));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        try
        {
            var unavailable = Path.Combine(
                Path.GetTempPath(),
                "mcp-fifteenth-native-" + Guid.NewGuid().ToString("N"),
                "child");
            var operation = WorkspaceIdentityPath.GetStorageKeyAsync(
                unavailable,
                WorkspacePathPlatform.Windows,
                TimeSpan.FromSeconds(5),
                cancellation.Token);
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            cancellation.Cancel();
            await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            release.Set();

            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(3),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Equal(cancellation.Token, exception.CancellationToken);
            await WaitForIdentityWorkerCountAsync(
                    expected: 0,
                    timeout: TimeSpan.FromSeconds(2))
                .ConfigureAwait(true);
        }
        finally
        {
            release.Set();
            hook.SetValue(null, null);
        }
    }

    internal static async Task AssertLinuxIdentityFuseCancellationAsync()
    {
        Assert.True(
            LinuxHangingFuseFixture.IsSupported,
            "The Linux native gate requires non-root FUSE support and fusermount3.");
        await using var fixture = await LinuxHangingFuseFixture.CreateAsync().ConfigureAwait(true);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(250));
        var invocation = Stopwatch.StartNew();
        var operation = WorkspaceIdentityPath.ResolveAsync(
            Path.Combine(fixture.MountPath, "blocked"),
            WorkspacePathPlatform.CaseSensitive,
            TimeSpan.FromMilliseconds(500),
            cancellation.Token);
        invocation.Stop();
        Assert.True(
            invocation.Elapsed < TimeSpan.FromMilliseconds(250),
            $"Async resolution blocked its caller for {invocation.Elapsed}.");
        var completedBeforeCleanup = ReferenceEquals(
            await Task.WhenAny(
                    operation,
                    Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken))
                .ConfigureAwait(true),
            operation);
        Exception? failure = null;
        if (completedBeforeCleanup)
        {
            try
            {
                _ = await operation.ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }

        await fixture.StopAsync().ConfigureAwait(true);
        if (!operation.IsCompleted)
        {
            try
            {
                _ = await operation.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }
            catch (Exception)
            {
                // Cleanup only: the contract assertion below uses the pre-cleanup observation.
            }
        }

        Assert.True(completedBeforeCleanup, "Native Linux resolution exceeded its cancellation bound.");
        Assert.IsType<NotSupportedException>(failure);
        Assert.Equal(0, ReadIdentityCounter("s_activeNativeResolverWorkers"));
    }

    internal static async Task AssertLinuxSqliteFuseOpenAsync()
    {
        Assert.True(
            LinuxHangingFuseFixture.IsSupported,
            "The Linux native gate requires non-root FUSE support and fusermount3.");
        await using var fixture = await LinuxHangingFuseFixture.CreateAsync().ConfigureAwait(true);
        var databasePath = Path.Combine(fixture.MountPath, "blocked.db");
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false,
            }.ToString());
        var invocationsBefore = SqliteBoundedConnectionOpener.WorkerInvocationCount;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var operation = SqliteBoundedConnectionOpener.OpenAsync(
            connection,
            TimeSpan.FromMilliseconds(500),
            cancellation.Token);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(250));
        var completedBeforeCleanup = ReferenceEquals(
            await Task.WhenAny(
                    operation,
                    Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken))
                .ConfigureAwait(true),
            operation);
        Exception? failure = null;
        if (completedBeforeCleanup)
        {
            try
            {
                await operation.ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }

        await fixture.StopAsync().ConfigureAwait(true);
        if (!operation.IsCompleted)
        {
            try
            {
                await operation.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }
            catch (Exception)
            {
                // Cleanup only: the contract assertion below uses the pre-cleanup observation.
            }
        }

        Assert.True(completedBeforeCleanup, "SQLite Linux pre-handle open exceeded its finite bound.");
        Assert.IsType<NotSupportedException>(failure);
        Assert.Equal(invocationsBefore, SqliteBoundedConnectionOpener.WorkerInvocationCount);
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Null(connection.Handle);
    }

    internal static async Task AssertWindowsSqliteKernelWaitAsync()
    {
        using var pipe = new System.IO.Pipes.NamedPipeServerStream(
            "mcp-fifteenth-open-" + Guid.NewGuid().ToString("N"),
            System.IO.Pipes.PipeDirection.In,
            1,
            System.IO.Pipes.PipeTransmissionMode.Byte,
            System.IO.Pipes.PipeOptions.None);
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        using var started = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));
        Task<SqliteConnection>? operation = null;
        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    started.Set();
                    pipe.WaitForConnection();
                }));
            operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromSeconds(10),
                cancellation.Token);
            Assert.True(started.Wait(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken));
            cancellation.Cancel();

            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Equal(cancellation.Token, exception.CancellationToken);
        }
        finally
        {
            hookField.SetValue(null, null);
        }

        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Null(connection.Handle);
    }

    /// <summary>Waits for truthful native identity worker accounting within a finite bound.</summary>
    /// <param name="expected">Expected active worker count.</param>
    /// <param name="timeout">Finite observation deadline.</param>
    private static async Task WaitForIdentityWorkerCountAsync(int expected, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < timeout)
        {
            if (ReadIdentityCounter("s_activeNativeResolverWorkers") == expected)
                return;

            await Task.Delay(
                    TimeSpan.FromMilliseconds(10),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        Assert.Equal(expected, ReadIdentityCounter("s_activeNativeResolverWorkers"));
    }

    /// <summary>Reads an internal identity resolver counter for lifecycle assertions.</summary>
    private static int ReadIdentityCounter(string fieldName)
    {
        var field = typeof(WorkspaceIdentityPath).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return Assert.IsType<int>(field!.GetValue(null));
    }

    private sealed class LinuxHangingFuseFixture : IAsyncDisposable
    {
        private const string Source = """
#define FUSE_USE_VERSION 31
#include <errno.h>
#include <fuse3/fuse.h>
#include <signal.h>
#include <string.h>
#include <sys/stat.h>
#include <unistd.h>

static int probe_getattr(const char *path, struct stat *metadata, struct fuse_file_info *info)
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

static const struct fuse_operations operations = {
    .getattr = probe_getattr,
};

int main(int argc, char **argv)
{
    return fuse_main(argc, argv, &operations, NULL);
}
""";

        private readonly string _root;
        private readonly Process _process;
        private readonly Task<string> _standardError;
        private bool _stopped;

        private LinuxHangingFuseFixture(
            string root,
            string mountPath,
            Process process,
            Task<string> standardError)
        {
            _root = root;
            MountPath = mountPath;
            _process = process;
            _standardError = standardError;
        }

        internal static bool IsSupported =>
            OperatingSystem.IsLinux() &&
            File.Exists("/dev/fuse") &&
            File.Exists("/usr/bin/gcc") &&
            File.Exists("/usr/include/fuse3/fuse.h") &&
            File.Exists("/usr/bin/fusermount3");

        internal string MountPath { get; }

        internal static async Task<LinuxHangingFuseFixture> CreateAsync()
        {
            Assert.True(IsSupported, "The Linux FUSE native-test prerequisites are unavailable.");
            var root = Path.Combine(
                Path.GetTempPath(),
                "mcp-fifteenth-fuse-" + Guid.NewGuid().ToString("N"));
            var mountPath = Path.Combine(root, "mount");
            var sourcePath = Path.Combine(root, "hanging-fuse.c");
            var executablePath = Path.Combine(root, "hanging-fuse");
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

            var startInfo = new ProcessStartInfo(executablePath)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("-s");
            startInfo.ArgumentList.Add(mountPath);
            var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start the hanging FUSE helper.");
            var standardError = process.StandardError.ReadToEndAsync(
                TestContext.Current.CancellationToken);
            _ = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var fixture = new LinuxHangingFuseFixture(root, mountPath, process, standardError);
            try
            {
                await fixture.WaitForMountAsync().ConfigureAwait(true);
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync().ConfigureAwait(true);
                throw;
            }
        }

        internal async Task StopAsync()
        {
            if (_stopped)
                return;
            _stopped = true;

            try
            {
                await RunProcessAsync(
                        "/usr/bin/fusermount3",
                        ["-u", "-z", MountPath],
                        TimeSpan.FromSeconds(5))
                    .ConfigureAwait(true);
            }
            catch (InvalidOperationException)
            {
                // A helper failure or a concurrent detach is followed by an unconditional process kill.
            }

            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            _ = await _standardError.ConfigureAwait(true);
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(true);
            _process.Dispose();
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private async Task WaitForMountAsync()
        {
            var deadline = Stopwatch.StartNew();
            while (deadline.Elapsed < TimeSpan.FromSeconds(5))
            {
                var mountInfo = await File.ReadAllTextAsync(
                        "/proc/self/mountinfo",
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
                if (mountInfo.Contains(
                        " " + MountPath + " ",
                        StringComparison.Ordinal))
                {
                    return;
                }

                if (_process.HasExited)
                {
                    throw new InvalidOperationException(
                        "FUSE helper exited before mounting: " +
                        await _standardError.ConfigureAwait(true));
                }

                await Task.Delay(
                        TimeSpan.FromMilliseconds(25),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }

            throw new TimeoutException("The FUSE helper did not publish its mount within five seconds.");
        }

        private static async Task RunProcessAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout)
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
            var output = process.StandardOutput.ReadToEndAsync(
                TestContext.Current.CancellationToken);
            var error = process.StandardError.ReadToEndAsync(
                TestContext.Current.CancellationToken);
            try
            {
                await process.WaitForExitAsync(TestContext.Current.CancellationToken)
                    .WaitAsync(timeout, TestContext.Current.CancellationToken)
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
}
