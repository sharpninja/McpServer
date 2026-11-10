using System.Data;
using System.Diagnostics;
using System.Reflection;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC002: Consumer-facing SQLite pre-handle timeout and lifecycle coverage.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-020-AC002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139SixteenthSqliteConsumerTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
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
}
