using System.Data;
using System.Diagnostics;
using System.Reflection;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC003: SQLite caller-state ownership at bounded public completion.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139EighteenthSqliteTests
{
    /// <summary>
    /// Proves timeout returns without leaving a worker able to mutate the caller connection: the
    /// caller is inspected, reused, and disposed before the deliberately blocked worker is released.
    /// </summary>
    [Fact]
    public async Task OpenAsync_Timeout_CallerIsRestoredReusableAndDisposableBeforeWorkerRelease()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        connection.DefaultTimeout = 37;
        var originalConnectionString = connection.ConnectionString;
        var originalDefaultTimeout = connection.DefaultTimeout;
        var hookField = typeof(SqliteBoundedConnectionOpener).GetField(
            "s_beforePinnedDataSourceOpen",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(hookField);
        Assert.Null(hookField!.GetValue(null));
        Assert.Equal(0, SqliteBoundedConnectionOpener.ActiveWorkerCount);

        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task? operation = null;
        Exception? publicFailure = null;
        Exception? reuseFailure = null;
        string? observedConnectionString = null;
        int? observedDefaultTimeout = null;
        ConnectionState? observedState = null;
        var activeAtPublicCompletion = 0;
        var disposedBeforeRelease = false;
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

            operation = SqliteBoundedConnectionOpener.OpenIsolatedAsync(
                connection,
                TimeSpan.FromMilliseconds(100),
                CancellationToken.None);
            Assert.True(
                entered.Wait(
                    TimeSpan.FromSeconds(2),
                    TestContext.Current.CancellationToken),
                "The SQLite worker did not reach the deterministic blocking boundary.");

            var winner = await Task.WhenAny(
                    operation,
                    Task.Delay(
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            stopwatch.Stop();
            if (ReferenceEquals(winner, operation))
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

            activeAtPublicCompletion = SqliteBoundedConnectionOpener.ActiveWorkerCount;
            observedConnectionString = connection.ConnectionString;
            observedDefaultTimeout = connection.DefaultTimeout;
            observedState = connection.State;
            try
            {
                connection.Open();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                var scalar = await command.ExecuteScalarAsync(
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
                Assert.Equal(1L, scalar);
                connection.Close();
                connection.Dispose();
                disposedBeforeRelease = true;
            }
            catch (Exception exception)
            {
                reuseFailure = exception;
            }
        }
        finally
        {
            release.Set();
            hookField.SetValue(null, null);
            connection.Dispose();
            Assert.True(
                SpinWait.SpinUntil(
                    () => SqliteBoundedConnectionOpener.ActiveWorkerCount == 0,
                    TimeSpan.FromSeconds(2)),
                "The SQLite worker remained counted after its actual exit.");
        }

        Assert.True(
            operation?.IsCompleted == true,
            $"SQLite public timeout exceeded one second; elapsed {stopwatch.Elapsed}.");
        Assert.IsType<TimeoutException>(publicFailure);
        Assert.Equal(1, activeAtPublicCompletion);
        Assert.Equal(originalConnectionString, observedConnectionString);
        Assert.Equal(originalDefaultTimeout, observedDefaultTimeout);
        Assert.Equal(ConnectionState.Closed, observedState);
        Assert.Null(reuseFailure);
        Assert.True(disposedBeforeRelease);
    }
}
