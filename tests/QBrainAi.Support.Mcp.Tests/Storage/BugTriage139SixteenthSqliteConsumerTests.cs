using System.Data;
using System.Diagnostics;
using System.Reflection;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC002: Consumer-facing SQLite pre-handle timeout and lifecycle coverage.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139SixteenthSqliteConsumerTests
{
    /// <summary>
    /// Verifies that a synchronous open which ignores managed interruption cannot extend public
    /// timeout completion and remains counted until the actual worker exits.
    /// </summary>
    /// <remarks>Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task OpenAsync_UnstoppablePreHandleWorker_IsBoundedAndTruthfullyCounted()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        var originalDefaultTimeout = connection.DefaultTimeout;
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task? operation = null;
        Exception? publicFailure = null;
        var completedWithinBound = false;
        var activeAtPublicCompletion = 0;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    while (!release.IsSet)
                    {
                        try
                        {
                            release.Wait();
                        }
                        catch (ThreadInterruptedException)
                        {
                            // Simulate native Linux I/O, which Thread.Interrupt cannot stop.
                        }
                    }
                }));
            operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromMilliseconds(100),
                CancellationToken.None);
            Assert.True(
                entered.Wait(
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
            activeAtPublicCompletion = SqliteBoundedConnectionOpener.ActiveWorkerCount;
            if (completedWithinBound)
            {
                try
                {
                    await operation.ConfigureAwait(true);
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
                    await operation
                        .WaitAsync(
                            TimeSpan.FromSeconds(2),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true);
                }
                catch (Exception)
                {
                    // Cleanup observes the already-classified public timeout after releasing the worker.
                }
            }
        }

        Assert.True(
            completedWithinBound,
            $"SQLite public timeout exceeded one second; elapsed {stopwatch.Elapsed}.");
        Assert.IsType<TimeoutException>(publicFailure);
        Assert.Equal(1, activeAtPublicCompletion);
        Assert.True(
            SpinWait.SpinUntil(
                () => SqliteBoundedConnectionOpener.ActiveWorkerCount == 0,
                TimeSpan.FromSeconds(2)),
            "SQLite worker accounting cleared before or remained after the worker's actual exit.");
        Assert.Equal(originalDefaultTimeout, connection.DefaultTimeout);
        Assert.Equal(ConnectionState.Closed, connection.State);
    }
    /// <summary>
    /// Verifies successful public completion is withheld until the real opener thread exits,
    /// while active-worker accounting remains asserted at the final thread boundary.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (as appended 2026-11-03 16:37:35: a hang guard is at least 100 times the longest real or scripted delay of the awaited step): the open's timeout argument rises from 5 s to 10 s. It is a hang guard: it fires only when the test stalls before it releases the held worker, and the case's 100 ms scripted wait (the open must still be withheld 100 ms after entry) comes before that release, so 5 s was 50 times that delay; the second holistic read before hostile validation round 12 found its class (finding C1). On a passing run the test releases the worker after 100 ms and the timeout never fires.</description></item>
    /// <item><term>2026-11-04</term><description>TR-MCP-USECASE-018 AC-006 (as appended 2026-11-04 12:20:22: the longest delay of the awaited step is the sum of every real or scripted delay on its path, read from the sources): the open's timeout argument rises from 10 s to 15 s. Before the open completes, the case waits 100 ms and releases the worker, and the opener's worker tracker then polls the thread's exit every 5 ms (SqliteBoundedConnectionOpener.cs:278-279), 105 ms in all, so 10 s was 95 times it and 15 s is 143 times; the third holistic read before hostile validation round 12 raised the measure (finding d3; operator 2026-11-04: "Fix it"). Assertions unchanged.</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task OpenAsync_ResultWithheldUntilThreadExit_WorkerRemainsObservable()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        var exitHookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforeWorkerExit",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(exitHookField);
        Assert.Null(exitHookField!.GetValue(null));
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<SqliteConnection>? operation = null;
        try
        {
            exitHookField.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait();
                }));

            operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromSeconds(15),
                TestContext.Current.CancellationToken);
            Assert.True(entered.Wait(
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken));
            await Task.Delay(
                    TimeSpan.FromMilliseconds(100),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        }
        finally
        {
            release.Set();
            exitHookField.SetValue(null, null);
        }

        Assert.NotNull(operation);
        await using var openedConnection = await operation.WaitAsync(
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotSame(connection, openedConnection);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(ConnectionState.Open, openedConnection.State);
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        openedConnection.Close();
    }

    /// <summary>
    /// Verifies descriptor-pinned opening restores both the visible connection string and the
    /// provider pool identity, so a later direct reopen targets the original data source.
    /// </summary>
    [Fact]
    public void PinnedDataSourceLifecycle_DirectReopenUsesOriginalPoolIdentity()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "mcp-sqlite-pool-identity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var originalPath = Path.Combine(root, "original.db");
        var pinnedPath = Path.Combine(root, "pinned.db");
        var originalConnectionString = CreateConnectionString(originalPath);
        var pinnedConnectionString = CreateConnectionString(pinnedPath);

        try
        {
            SeedDatabase(originalConnectionString, "original");
            SeedDatabase(pinnedConnectionString, "pinned");

            using var connection = new SqliteConnection(pinnedConnectionString);
            connection.Open();
            var leaseType = typeof(SqliteBoundedConnectionOpener).GetNestedType(
                "PinnedSqliteDataSourceLease",
                BindingFlags.NonPublic);
            Assert.NotNull(leaseType);
            var lease = Activator.CreateInstance(
                leaseType!,
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: [connection, originalConnectionString, new MemoryStream()],
                culture: null);
            Assert.NotNull(lease);
            var completeOpen = leaseType!.GetMethod(
                "CompleteOpen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(completeOpen);

            completeOpen!.Invoke(lease, null);
            Assert.Equal(originalConnectionString, connection.ConnectionString);
            connection.Close();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM marker;";
            Assert.Equal("original", Assert.IsType<string>(command.ExecuteScalar()));
            ((IDisposable)lease!).Dispose();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        static string CreateConnectionString(string path) =>
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            }.ToString();

        static void SeedDatabase(string connectionString, string value)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "CREATE TABLE marker(value TEXT NOT NULL); INSERT INTO marker(value) VALUES ($value);";
            command.Parameters.AddWithValue("$value", value);
            _ = command.ExecuteNonQuery();
        }
    }
}
