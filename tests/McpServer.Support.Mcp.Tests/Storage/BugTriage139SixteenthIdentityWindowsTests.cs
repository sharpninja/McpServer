using System.Diagnostics;
using System.Reflection;
using McpServer.Client;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC001: Real Windows worker-lifecycle coverage for physical identity.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139SixteenthIdentityWindowsTests
{
    /// <summary>
    /// Verifies that a cancellation attempt which cannot stop the resolver thread still yields a
    /// bounded public timeout while the blocked worker remains observable until it really exits.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_UnstoppableWindowsWorker_CompletesPubliclyAndRemainsObservable()
    {
        Assert.True(OperatingSystem.IsWindows());

        var hookField = typeof(WorkspaceIdentityPath).GetField(
            "s_beforeWindowsNativeOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        var activeField = typeof(WorkspaceIdentityPath).GetField(
            "s_activeNativeResolverWorkers",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.NotNull(activeField);
        Assert.Null(hookField!.GetValue(null));
        Assert.Equal(0, ReadCounter(activeField!));

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<(string NormalizedPath, string StorageKey)>? operation = null;
        Exception? publicFailure = null;
        var completedWithinBound = false;
        var activeWhileBlocked = 0;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait();
                }));

            operation = WorkspaceIdentityPath.ResolveAsync(
                Path.GetTempPath(),
                WorkspacePathPlatform.Windows,
                TimeSpan.FromMilliseconds(100),
                CancellationToken.None);
            Assert.True(entered.Wait(
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken));

            var winner = await Task.WhenAny(
                    operation,
                    Task.Delay(
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            stopwatch.Stop();
            completedWithinBound = ReferenceEquals(winner, operation);
            activeWhileBlocked = ReadCounter(activeField);
            if (completedWithinBound)
            {
                try
                {
                    _ = await operation.ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    publicFailure = exception;
                }
            }
        }
        finally
        {
            release.Set();
            hookField.SetValue(null, null);
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
                    // Cleanup occurs after the bounded/public and active-worker observations.
                }
            }
        }

        Assert.True(
            completedWithinBound,
            $"Public identity resolution did not complete within one second; elapsed {stopwatch.Elapsed}.");
        Assert.Equal(1, activeWhileBlocked);
        Assert.IsType<TimeoutException>(publicFailure);
        Assert.True(
            SpinWait.SpinUntil(() => ReadCounter(activeField) == 0, TimeSpan.FromSeconds(2)),
            "The resolver worker remained active after its blocking operation was released.");
    }
    /// <summary>
    /// Verifies successful public completion is withheld until the real resolver thread exits,
    /// while active-worker accounting remains asserted at the final thread boundary.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ResultWithheldUntilThreadExit_WorkerRemainsObservable()
    {
        Assert.True(OperatingSystem.IsWindows());

        var exitHookField = typeof(WorkspaceIdentityPath).GetField(
            "s_beforeWindowsWorkerExit",
            BindingFlags.NonPublic | BindingFlags.Static);
        var activeField = typeof(WorkspaceIdentityPath).GetField(
            "s_activeNativeResolverWorkers",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(exitHookField);
        Assert.NotNull(activeField);
        Assert.Null(exitHookField!.GetValue(null));
        Assert.Equal(0, ReadCounter(activeField!));

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<(string NormalizedPath, string StorageKey)>? operation = null;
        try
        {
            exitHookField.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait();
                }));

            operation = WorkspaceIdentityPath.ResolveAsync(
                Path.GetTempPath(),
                WorkspacePathPlatform.Windows,
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);
            Assert.True(entered.Wait(
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken));
            await Task.Delay(
                    TimeSpan.FromMilliseconds(100),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, ReadCounter(activeField));
        }
        finally
        {
            release.Set();
            exitHookField.SetValue(null, null);
        }

        Assert.NotNull(operation);
        var result = await operation.WaitAsync(
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.False(string.IsNullOrWhiteSpace(result.NormalizedPath));
        Assert.Equal(0, ReadCounter(activeField));
    }

    /// <summary>
    /// Verifies that a remote UNC path is rejected before the Windows resolver can enter a native
    /// open that depends on cancellation succeeding.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_RemoteUnc_IsRejectedBeforeNativeOpen()
    {
        Assert.True(OperatingSystem.IsWindows());

        var hookField = typeof(WorkspaceIdentityPath).GetField(
            "s_beforeWindowsNativeOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        var invocationField = typeof(WorkspaceIdentityPath).GetField(
            "s_nativeResolverInvocationCount",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.NotNull(invocationField);
        Assert.Null(hookField!.GetValue(null));

        var hookCalls = 0;
        var invocationsBefore = ReadCounter(invocationField!);
        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    Interlocked.Increment(ref hookCalls);
                    throw new InvalidOperationException("Remote UNC reached the native-open boundary.");
                }));

            await Assert.ThrowsAsync<NotSupportedException>(
                    () => WorkspaceIdentityPath.ResolveAsync(
                        @"\\192.0.2.1\bug139-sixteenth\child",
                        WorkspacePathPlatform.Windows,
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
        }
        finally
        {
            hookField.SetValue(null, null);
        }

        Assert.Equal(0, hookCalls);
        Assert.Equal(invocationsBefore, ReadCounter(invocationField));
    }

    private static int ReadCounter(FieldInfo field) =>
        Assert.IsType<int>(field.GetValue(null));
}
