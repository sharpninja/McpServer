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

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-007 / FR-MCP-TRIAGESTORE-002: dense beginTurn/completeTurn loads
/// must not cartesian-join sibling session collections, and a failed graph load must not
/// report success or treat the session as missing.
/// Fixture: Sqlite session with two turns that each carry actions, tags, context, dialog,
/// commits, and string lists, plus session tags.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-007; FR-MCP-TRIAGESTORE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (UpsertTurnAsync_SlowGraphLoad_FailsRetryableWithoutPersisting) and the helpers only it uses (DelayedReaderInterceptor) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (WorkspacePath, Agent, SessionId, ExistingRequestId, SecondRequestId, NewRequestId, SeedDenseSessionAsync, BuildSut, OpenConnection, Count, NewTurn, CreateDenseSession and DenseTurn) moved once into Services/SessionLogDenseGraphTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogDenseGraphTests
{
    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: UpsertTurn (beginTurn/completeTurn) loads sibling collections
    /// as separate queries. One SELECT must not join SessionLogActions and SessionLogTurnTags.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_DenseGraph_UsesSplitQueriesNotCartesianJoin()
    {
        await using var connection = OpenConnection();
        await SeedDenseSessionAsync(connection).ConfigureAwait(true);

        var capture = new CommandCaptureInterceptor();
        var (sut, db) = BuildSut(connection, capture);
        using (db)
        {
            var actionsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogActions");
            var tagsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogTurnTags");

            await sut.UpsertTurnAsync(Agent, SessionId, NewTurn(), TestContext.Current.CancellationToken).ConfigureAwait(true);

            var selects = capture.Commands.Where(static sql => sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.Contains(selects, static sql => sql.Contains("SessionLogActions", StringComparison.Ordinal));
            Assert.Contains(selects, static sql => sql.Contains("SessionLogTurnTags", StringComparison.Ordinal));
            Assert.DoesNotContain(
                selects,
                static sql => sql.Contains("SessionLogActions", StringComparison.Ordinal)
                    && sql.Contains("SessionLogTurnTags", StringComparison.Ordinal));
            Assert.Equal(actionsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogActions"));
            Assert.Equal(tagsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogTurnTags"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns t JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE t.RequestId = 'req-20260928T220002Z-dense-new'"));
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: SQL 1205 during graph load is retryable backend_unavailable.
    /// The session is not reported missing and the new turn is not persisted.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_SqlDeadlock1205_DoesNotReportMissingSession()
    {
        await using var connection = OpenConnection();
        await SeedDenseSessionAsync(connection).ConfigureAwait(true);

        var (sut, db) = BuildSut(connection, new DeadlockReaderInterceptor());
        using (db)
        {
            var turnsBefore = Count(connection, "SELECT COUNT(*) FROM SessionLogTurns");
            var ex = await Assert.ThrowsAsync<StorageGraphMaterializationException>(() =>
                sut.UpsertTurnAsync(Agent, SessionId, NewTurn(), TestContext.Current.CancellationToken)).ConfigureAwait(true);

            Assert.Contains("not treated as missing", ex.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
            var classified = McpErrorClassifier.Classify(ex);
            Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
            Assert.True(classified.Retryable);
            Assert.Equal(503, classified.StatusCode);
            Assert.Equal(turnsBefore, Count(connection, "SELECT COUNT(*) FROM SessionLogTurns"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM SessionLogs WHERE SessionId = 'Cursor-20260928T220000Z-dense-graph'"));
        }
    }

    /// <summary>Records reader command text issued while a session graph is loaded.</summary>
    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        /// <summary>Command texts observed for this context.</summary>
        public List<string> Commands { get; } = [];

        /// <inheritdoc />
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    /// <summary>Fails the first reader with SQL Server deadlock 1205.</summary>
    private sealed class DeadlockReaderInterceptor : DbCommandInterceptor
    {
        /// <inheritdoc />
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
            => throw SqlExceptionFactory.Create(1205, "Transaction was deadlocked on lock resources and chosen as the deadlock victim.");

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => throw SqlExceptionFactory.Create(1205, "Transaction was deadlocked on lock resources and chosen as the deadlock victim.");
    }
}
