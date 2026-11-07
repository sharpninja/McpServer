using System.Data.Common;
using System.Diagnostics;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.SessionLogDenseGraphTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-007 / FR-MCP-TRIAGESTORE-002: dense beginTurn/completeTurn loads
/// must not cartesian-join sibling session collections, and a failed graph load must not
/// report success or treat the session as missing.
/// Fixture: Sqlite session with two turns that each carry actions, tags, context, dialog,
/// commits, and string lists, plus session tags.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-007; FR-MCP-TRIAGESTORE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogDenseGraphTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (DelayedReaderInterceptor) moved unchanged with the class attributes; the helpers both halves use come from Services/SessionLogDenseGraphTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogDenseGraphTests
{
    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: a graph load that exceeds the command budget fails retryable
    /// and does not insert the turn or clear the existing session.
    /// </summary>
    /// <remarks>
    /// Integration: its outcome depends on a real storage-command budget timer firing while the delayed step runs, a wall-clock wait under TR-MCP-USECASE-018 AC-006; its elapsed check is a hang guard only.
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-05</term><description>TEST-MCP-TRIAGESTORE-007; TR-MCP-USECASE-018 AC-006 (a wall-clock bound makes a case an integration case): which budget elapsed is read from the exception message (the configured 1 second storage command budget; the delayed reader would finish after 4 seconds and throw nothing), and the elapsed time keeps a 20 second hang guard instead of the 3 second bound, because the sibling sweep of the Windows integration lane failure at b79fd51f (slice Z5) found this case at up to 6.37 seconds of run time across nine Windows lanes (ug-r13z5-win-lane-elapsed-bound-durations-1.txt): its bound also timed the host. The classification line above was restated after the eighth holistic read before hostile validation round 13 (finding b2), because it still named the elapsed bound this entry removed.</description></item>
    /// </list>
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task UpsertTurnAsync_SlowGraphLoad_FailsRetryableWithoutPersisting()
    {
        await using var connection = OpenConnection();
        await SeedDenseSessionAsync(connection).ConfigureAwait(true);

        var (sut, db) = BuildSut(connection, new DelayedReaderInterceptor(TimeSpan.FromSeconds(4)), budgetSeconds: 1);
        using (db)
        {
            var turnsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogTurns");
            var clock = Stopwatch.StartNew();
            var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() =>
                sut.UpsertTurnAsync(Agent, SessionId, NewTurn(), TestContext.Current.CancellationToken)).ConfigureAwait(true);
            clock.Stop();

            Assert.Contains("within the 1 second storage command budget", ex.Message, StringComparison.Ordinal);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(20), $"The graph load was not cut short by its budget: {clock.Elapsed}.");
            var classified = McpErrorClassifier.Classify(ex);
            Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
            Assert.True(classified.Retryable);
            Assert.NotEqual(McpErrorClassifier.NotFound, classified.Code);
            Assert.Equal(turnsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns"));
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns t WHERE t.RequestId = 'req-20260928T220002Z-dense-new'"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns t WHERE t.RequestId = 'req-20260928T220000Z-dense-existing'"));
        }
    }

    /// <summary>Delays each reader command so an unbudgeted graph load outlasts the storage budget.</summary>
    private sealed class DelayedReaderInterceptor : DbCommandInterceptor
    {
        private readonly TimeSpan _delay;

        /// <summary>Initializes the interceptor.</summary>
        /// <param name="delay">How long each reader waits.</param>
        public DelayedReaderInterceptor(TimeSpan delay) => _delay = delay;

        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
        }
    }
}
