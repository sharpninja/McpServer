using System.Data;
using System.Diagnostics;
using System.Reflection;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC003: SQLite caller-state ownership at bounded public completion.
/// </summary>
/// <remarks>
/// <para>2026-11-07, TEST-MCP-USECASE-020-AC003; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139EighteenthSqliteTests.cs) into the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests, so the unit-owned project holds only unit cases (slice S5, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); it stays in the serialized WorkspaceIdentityNativeProcessStateCollection, whose definition is linked into this assembly, and QBrainAi.Storage grants this assembly the internals it asserts on; only the namespace changed.</para>
/// <para>2026-11-07, TEST-MCP-USECASE-020-AC003; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: QBrainAi.Support.Mcp.Environment.IntegrationTests, the project the entry above names, is now QBrainAi.Support.Mcp.EnvironmentIntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); QBrainAi.Storage grants the renamed assembly the same internals; the class and its cases are unchanged.</para>
/// </remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139EighteenthSqliteTests
{
    /// <summary>
    /// Proves timeout returns without leaving a worker able to mutate the caller connection: the
    /// caller is inspected, reused, and disposed before the deliberately blocked worker is released.
    /// </summary>
    /// <remarks>Integration: asserts an elapsed wall-clock bound on actual scheduling and timer execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
