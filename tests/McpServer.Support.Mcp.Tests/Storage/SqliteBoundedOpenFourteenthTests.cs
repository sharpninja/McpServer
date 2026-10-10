using System.Data;
using System.Data.Common;
using System.Diagnostics;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-018: Fourteenth-review coverage for bounded SQLite connection opening before
/// and after the provider exposes a native handle.
/// </summary>
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
                            TimeSpan.FromSeconds(5),
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
    /// A provider-native shared-cache lock encountered during Open must honor the finite deadline,
    /// close the partial handle, and leave no worker behind.
    /// </summary>
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
    /// Close and disposal may race cancellation without invalid-handle access or a leaked worker.
    /// </summary>
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
                            TimeSpan.FromSeconds(5),
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

    /// <summary>
    /// Repeated pooled and non-pooled opens preserve the exact connection string, release the outer
    /// handle on close, and dispose cancellation registration before the connection is reused.
    /// </summary>
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
