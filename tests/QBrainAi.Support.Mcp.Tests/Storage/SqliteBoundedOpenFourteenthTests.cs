using System.Data;
using System.Data.Common;
using System.Diagnostics;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-018: Fourteenth-review coverage for bounded SQLite connection opening before
/// and after the provider exposes a native handle.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-018; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 3 Category=Integration cases (OpenAsync_PreflightHeldAtBoundary_TimesOutWithoutBlockingTheCaller, OpenAsync_NativeLockAtOpen_TimesOutAndJoinsWorker and OpenAsync_RepeatedPoolingModes_PreserveStateAndInterruptLifetime) and the helpers only they use (SharedMemoryConnectionString) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class SqliteBoundedOpenFourteenthTests
{
    /// <summary>
    /// Confirms the provider inherits DbConnection.OpenAsync and therefore executes Open
    /// synchronously unless the caller supplies an explicit bounded worker.
    /// </summary>
    [Fact]
    public void SqliteOpenAsync_IsInheritedSynchronousImplementation()
    {
        var method = typeof(SqliteConnection).GetMethod(
            nameof(DbConnection.OpenAsync),
            [typeof(CancellationToken)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(DbConnection), method.DeclaringType);
    }

    /// <summary>
    /// Cancellation before SQLite has a handle must return without permitting the isolated worker
    /// to mutate the caller, while truthful accounting remains until that worker actually exits.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (as appended 2026-11-03 16:37:35: a hang guard is at least 100 times the longest real or scripted delay of the awaited step): the bound on the cancelled open rises from 5 s to 25 s. After the caller cancels a worker the hook still holds, the opener spends up to its 250 ms native cleanup deadline (SqliteBoundedConnectionOpener s_nativeCleanupTimeout) before it throws, so the awaited step has a real 250 ms delay and 5 s was 20 times it; the second holistic read before hostile validation round 12 found its class (finding C1). On a passing run the open ends about 250 ms after the cancel.</description></item>
    /// <item><term>2026-11-04</term><description>TR-MCP-USECASE-018 AC-006 (as appended 2026-11-04 12:20:22: the longest delay of the awaited step is the sum of every real or scripted delay on its path, read from the sources): the bound on the cancelled open rises from 25 s to 30 s. On the cancelled open's path the opener waits up to its 250 ms native cleanup deadline in a 10 ms loop, and its worker tracker polls the thread's exit every 5 ms (SqliteBoundedConnectionOpener.cs:20, :208-217 and :278-279), 265 ms in all, so 25 s was 94 times it and 30 s is 113 times; the third holistic read before hostile validation round 12 raised the measure (finding d3; operator 2026-11-04: "Fix it"). Assertions unchanged.</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task OpenAsync_PreHandleNativeWait_CancelsWithoutMutatingCaller()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        var originalConnectionString = connection.ConnectionString;
        connection.DefaultTimeout = 23;
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        Task<SqliteConnection>? operation = null;

        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait();
                }));
            operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromSeconds(10),
                cancellation.Token);
            Assert.True(entered.Wait(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken));

            cancellation.Cancel();
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await operation.WaitAsync(
                            TimeSpan.FromSeconds(30),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true))
                .ConfigureAwait(true);
            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.Equal(ConnectionState.Closed, connection.State);
            Assert.Null(connection.Handle);
            Assert.Equal(originalConnectionString, connection.ConnectionString);
            Assert.Equal(23, connection.DefaultTimeout);
        }
        finally
        {
            release.Set();
            hookField.SetValue(null, null);
        }

        Assert.NotNull(operation);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => operation.WaitAsync(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        Assert.True(SpinWait.SpinUntil(
            () => SqliteBoundedConnectionOpener.ActiveWorkerCount == 0,
            TimeSpan.FromSeconds(2)));
    }

    /// <summary>
    /// Close and disposal may race cancellation without invalid-handle access or a leaked worker.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (as appended 2026-11-03 16:37:35: a hang guard is at least 100 times the longest real or scripted delay of the awaited step): the bound on each cancelled open rises from 5 s to 25 s. After the cancel and the disposal, the opener spends up to its 250 ms native cleanup deadline (SqliteBoundedConnectionOpener s_nativeCleanupTimeout) before it throws, so the awaited step has a real 250 ms delay and 5 s was 20 times it; the second holistic read before hostile validation round 12 found its class (finding C1). On a passing run each of the eight opens ends about 250 ms after its cancel.</description></item>
    /// <item><term>2026-11-04</term><description>TR-MCP-USECASE-018 AC-006 (as appended 2026-11-04 12:20:22: the longest delay of the awaited step is the sum of every real or scripted delay on its path, read from the sources): the bound on each cancelled open rises from 25 s to 30 s. On each cancelled open's path the opener waits up to its 250 ms native cleanup deadline in a 10 ms loop, and its worker tracker polls the thread's exit every 5 ms (SqliteBoundedConnectionOpener.cs:20, :208-217 and :278-279), 265 ms in all, so 25 s was 94 times it and 30 s is 113 times; the third holistic read before hostile validation round 12 raised the measure (finding d3; operator 2026-11-04: "Fix it"). Assertions unchanged.</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task OpenAsync_CloseDisposeCancellationRaces_DoNotLeakWorkersOrHandles()
    {
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));

        for (var iteration = 0; iteration < 8; iteration++)
        {
            var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
            Task<SqliteConnection>? operation = null;
            try
            {
                hookField.SetValue(
                    null,
                    (Action)(() =>
                    {
                        entered.Set();
                        release.Wait();
                    }));
                operation = SqliteBoundedConnectionOpener.OpenAsync(
                    connection,
                    TimeSpan.FromSeconds(10),
                    cancellation.Token);
                Assert.True(entered.Wait(
                    TimeSpan.FromSeconds(5),
                    TestContext.Current.CancellationToken));

                if ((iteration & 1) == 0)
                {
                    cancellation.Cancel();
                    connection.Dispose();
                }
                else
                {
                    connection.Dispose();
                    cancellation.Cancel();
                }

                var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                        () => operation.WaitAsync(
                            TimeSpan.FromSeconds(30),
                            TestContext.Current.CancellationToken))
                    .ConfigureAwait(true);
                Assert.Equal(cancellation.Token, exception.CancellationToken);
                Assert.Equal(ConnectionState.Closed, connection.State);
                Assert.Null(connection.Handle);
            }
            finally
            {
                release.Set();
                hookField.SetValue(null, null);
                connection.Dispose();
            }

            Assert.NotNull(operation);
            await Assert.ThrowsAsync<OperationCanceledException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.True(SpinWait.SpinUntil(
                () => SqliteBoundedConnectionOpener.ActiveWorkerCount == 0,
                TimeSpan.FromSeconds(2)));
        }
    }

}
