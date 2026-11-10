using System.Data;
using System.Data.Common;
using System.Diagnostics;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-018: Fourteenth-review coverage for bounded SQLite connection opening before
/// and after the provider exposes a native handle.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-018; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/SqliteBoundedOpenFourteenthTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 3 Category=Integration cases and the helpers only they use (SharedMemoryConnectionString) moved unchanged with the class attributes; only the namespace changed.</remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class SqliteBoundedOpenFourteenthTests
{
    /// <summary>
    /// BUG-BASELINE-009 (C2): the caller-side data-source preflight runs under the open's finite timeout. A preflight
    /// step that does not return (held at the pre-resolution boundary) must not block the caller: OpenAsync hands back
    /// its task at once and fails with TimeoutException at the bound, no open worker starts, and the held preflight
    /// stays counted until it actually returns. Fixture: a file data source in a temporary directory.
    /// </summary>
    /// <remarks>
    /// Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006).
    /// Proves TR-MCP-USECASE-019 AC-002 (the caller-side data-source preflight runs off the caller's thread under the open's single finite deadline).
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-06</term><description>TR-MCP-USECASE-019 AC-002: cites the criterion it proves, so the criterion text no longer names the test (operator: "Must be fixed.").</description></item>
    /// </list>
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact(Timeout = 60_000)]
    public async Task OpenAsync_PreflightHeldAtBoundary_TimesOutWithoutBlockingTheCaller()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcp-sqlite-preflight-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(root, "preflight.db"),
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,
            }.ToString());
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforeDataSourcePhysicalResolution",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var invocationsBefore = SqliteBoundedConnectionOpener.WorkerInvocationCount;
        Exception? publicFailure = null;
        var returnedAfter = TimeSpan.MaxValue;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            hookField.SetValue(
                null,
                (Action)(() =>
                {
                    entered.Set();
                    release.Wait(TimeSpan.FromSeconds(20));
                }));
            var operation = SqliteBoundedConnectionOpener.OpenAsync(
                connection,
                TimeSpan.FromMilliseconds(250),
                TestContext.Current.CancellationToken);
            returnedAfter = stopwatch.Elapsed;
            try
            {
                using var opened = await operation
                    .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)
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
            release.Set();
            hookField.SetValue(null, null);
        }

        try
        {
            Assert.True(entered.IsSet, "The preflight never reached the pre-resolution boundary.");
            Assert.True(
                returnedAfter < TimeSpan.FromMilliseconds(500),
                $"OpenAsync blocked its caller for {returnedAfter} before returning a task.");
            Assert.IsType<TimeoutException>(publicFailure);
            Assert.True(
                stopwatch.Elapsed < TimeSpan.FromSeconds(1),
                $"The held preflight was not bounded by the open's timeout: {stopwatch.Elapsed}.");
            Assert.Equal(invocationsBefore, SqliteBoundedConnectionOpener.WorkerInvocationCount);
            Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
            var preflightField = typeof(SqliteBoundedConnectionOpener).GetField(
                "s_activePreflightCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(preflightField);
            Assert.True(
                SpinWait.SpinUntil(() => (int)preflightField!.GetValue(null)! == 0, TimeSpan.FromSeconds(5)),
                "The released preflight was not counted until it actually returned.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// A provider-native shared-cache lock encountered during Open must honor the finite deadline,
    /// close the partial handle, and leave no worker behind.
    /// </summary>
    /// <remarks>Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task OpenAsync_NativeLockAtOpen_TimesOutAndJoinsWorker()
    {
        var connectionString = SharedMemoryConnectionString();
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using (var create = anchor.CreateCommand())
        {
            create.CommandText = "CREATE TABLE OpenProbe (Id INTEGER PRIMARY KEY);";
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        await using var blocker = new SqliteConnection(connectionString);
        await blocker.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using (var begin = blocker.CreateCommand())
        {
            begin.CommandText = "PRAGMA locking_mode=EXCLUSIVE; BEGIN EXCLUSIVE;";
            await begin.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        await using var candidate = new SqliteConnection(connectionString);
        candidate.DefaultTimeout = 23;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Assert.ThrowsAsync<TimeoutException>(
                    async () => await SqliteBoundedConnectionOpener.OpenAsync(
                            candidate,
                            TimeSpan.FromMilliseconds(750),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true))
                .ConfigureAwait(true);
        }
        finally
        {
            await using var rollback = blocker.CreateCommand();
            rollback.CommandText = "ROLLBACK;";
            await rollback.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        Assert.True(
            SpinWait.SpinUntil(
                () => SqliteBoundedConnectionOpener.ActiveWorkerCount == 0,
                TimeSpan.FromSeconds(2)),
            "The SQLite open worker did not clear after the blocking lock was released.");
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
        Assert.Equal(ConnectionState.Closed, candidate.State);
        Assert.Null(candidate.Handle);
        Assert.Equal(connectionString, candidate.ConnectionString);
        Assert.Equal(23, candidate.DefaultTimeout);
    }

    /// <summary>
    /// Repeated pooled and non-pooled opens preserve the exact connection string, release the outer
    /// handle on close, and dispose cancellation registration before the connection is reused.
    /// </summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OpenAsync_RepeatedPoolingModes_PreserveStateAndInterruptLifetime(bool pooling)
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-fourteenth-open-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = pooling,
            DefaultTimeout = 17,
            ForeignKeys = true,
        }.ToString();
        var connection = new SqliteConnection(connectionString);
        connection.DefaultTimeout = 23;

        try
        {
            for (var iteration = 0; iteration < 5; iteration++)
            {
                using var completedOperationToken =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        TestContext.Current.CancellationToken);
                var openedConnection = await SqliteBoundedConnectionOpener.OpenAsync(
                        connection,
                        TimeSpan.FromSeconds(5),
                        completedOperationToken.Token)
                    .ConfigureAwait(true);
                if (!ReferenceEquals(openedConnection, connection))
                {
                    await connection.DisposeAsync().ConfigureAwait(true);
                    connection = openedConnection;
                }

                Assert.Equal(ConnectionState.Open, connection.State);
                Assert.Equal(connectionString, connection.ConnectionString);
                Assert.Equal(23, connection.DefaultTimeout);

                completedOperationToken.Cancel();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                Assert.Equal(
                    1L,
                    Convert.ToInt64(
                        await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)
                            .ConfigureAwait(true)));

                connection.Close();
                Assert.Equal(ConnectionState.Closed, connection.State);
                Assert.Null(connection.Handle);
                Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);
            }
        }
        finally
        {
            SqliteConnection.ClearPool(connection);
            await connection.DisposeAsync().ConfigureAwait(true);
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static string SharedMemoryConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = $"mcp-fourteenth-open-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            Pooling = false,
            DefaultTimeout = 30,
            ForeignKeys = true,
        }.ToString();

}
