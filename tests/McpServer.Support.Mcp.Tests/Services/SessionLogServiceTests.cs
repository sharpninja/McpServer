using System.Linq;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013: Unit tests for SessionLogService submit and query (MVP-SUPPORT-011).</summary>
public sealed class SessionLogServiceTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\sessionlog-service";

    private readonly McpDbContext _db;
    private readonly IChangeEventBus _eventBus;
    private readonly SessionLogService _sut;

    public SessionLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTests_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);
        _eventBus = Substitute.For<IChangeEventBus>();
        // TR-MCP-MT-003A: default fixture mirrors the production wiring with a
        // WorkspaceContext so SubmitAsync stamps WorkspaceId on every row; the
        // global query filter on read then matches and existing tests keep working.
        _sut = new SessionLogService(
            _db,
            NullLogger<SessionLogService>.Instance,
            _eventBus,
            new WorkspaceContext { WorkspacePath = WorkspacePath });
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task WhenSubmittingNewSessionThenSessionIsCreated()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "session-1"));

        var id = await _sut.SubmitAsync(dto).ConfigureAwait(true);

        Assert.True(id > 0);
        var stored = await _db.SessionLogs.Include(s => s.Turns).FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal("Cursor", stored.SourceType);
        Assert.Equal(BuildSessionId("Cursor", "session-1"), stored.SessionId);
        Assert.Equal("Test Session", stored.Title);
        Assert.Single(stored.Turns);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.SessionLog
                                     && e.Action == ChangeEventActions.Created
                                     && e.EntityId == $"Cursor/{BuildSessionId("Cursor", "session-1")}"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenSubmittingSameSessionTwiceThenSessionIsUpdated()
    {
        var dto1 = CreateTestDto("Cursor", BuildSessionId("Cursor", "session-dup"), title: "Original");
        await _sut.SubmitAsync(dto1).ConfigureAwait(true);

        var dto2 = CreateTestDto("Cursor", BuildSessionId("Cursor", "session-dup"), title: "Updated");
        dto2.Turns![0].QueryText = "Updated query";
        var id = await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        var stored = await _db.SessionLogs.Include(s => s.Turns).FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal("Updated", stored.Title);
        Assert.Single(stored.Turns);
        Assert.Equal("Updated query", stored.Turns.First().QueryText);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.SessionLog
                                     && e.Action == ChangeEventActions.Updated
                                     && e.EntityId == $"Cursor/{BuildSessionId("Cursor", "session-dup")}"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// BUG-SESSIONLOG-RESTORE-001: Whole-session submit on a relational provider
    /// upserts turns by request id instead of attempting duplicate inserts.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_ExistingRelationalSession_UpsertsTurnsByRequestIdWithoutDuplicateInsert()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var (sut, db) = BuildSqliteSut(connection);
        using (db)
        {
            var sessionId = BuildSessionId("ClaudeCode", "relational-upsert-turns");
            var initial = CreateTestDto("ClaudeCode", sessionId, title: "Original");
            initial.Turns =
            [
                CreateRelationalTurn("req-20260610T151235Z-bootstrap-bbcrawler-workspace", "initial bootstrap", "bootstrap"),
                CreateRelationalTurn("req-20260610T151236Z-plan-bbcrawler-workspace", "initial plan", "plan")
            ];
            await sut.SubmitAsync(initial).ConfigureAwait(true);

            var restored = CreateTestDto("ClaudeCode", sessionId, title: "Restored");
            restored.Turns =
            [
                CreateRelationalTurn("req-20260610T151235Z-bootstrap-bbcrawler-workspace", "restored bootstrap", "restore"),
                CreateRelationalTurn("req-20260610T151236Z-plan-bbcrawler-workspace", "restored plan", "restore")
            ];

            await sut.SubmitAsync(restored).ConfigureAwait(true);

            var stored = await db.SessionLogs
                .IgnoreQueryFilters()
                .Include(s => s.Turns)
                    .ThenInclude(t => t.Tags)
                .Include(s => s.Turns)
                    .ThenInclude(t => t.Actions)
                .SingleAsync(s => s.SessionId == sessionId)
                .ConfigureAwait(true);
            Assert.Equal("Restored", stored.Title);
            Assert.Equal(2, stored.Turns.Count);
            Assert.Equal(
                2,
                await db.SessionLogTurns
                    .IgnoreQueryFilters()
                    .CountAsync(t => t.SessionLogId == stored.Id)
                    .ConfigureAwait(true));
            Assert.All(stored.Turns, turn => Assert.Contains(turn.Tags, tag => tag.Tag == "restore"));
        }
    }

    /// <summary>
    /// TR-MCP-DB-003: Whole-session submit preserves durable turns that are absent
    /// from the incoming document instead of hard-deleting them or their child rows.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_ExistingRelationalSession_PreservesAbsentDurableTurnsAndChildren()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var (sut, db) = BuildSqliteSut(connection);
        using (db)
        {
            var sessionId = BuildSessionId("ClaudeCode", "relational-remove-stale-tagged-turn");
            var initial = CreateTestDto("ClaudeCode", sessionId, title: "Original");
            initial.Turns =
            [
                CreateRelationalTurn("req-20260610T151235Z-bootstrap-bbcrawler-workspace", "kept turn", "keep"),
                CreateRelationalTurn("req-20260610T151236Z-stale-bbcrawler-workspace", "stale turn", "stale")
            ];
            await sut.SubmitAsync(initial).ConfigureAwait(true);

            var restored = CreateTestDto("ClaudeCode", sessionId, title: "Restored");
            restored.Turns =
            [
                CreateRelationalTurn("req-20260610T151235Z-bootstrap-bbcrawler-workspace", "restored kept turn", "restore")
            ];

            await sut.SubmitAsync(restored).ConfigureAwait(true);

            var stored = await db.SessionLogs
                .IgnoreQueryFilters()
                .Include(s => s.Turns)
                    .ThenInclude(t => t.Tags)
                .SingleAsync(s => s.SessionId == sessionId)
                .ConfigureAwait(true);
            Assert.Equal(2, stored.Turns.Count);
            var kept = Assert.Single(stored.Turns, turn => turn.RequestId == "req-20260610T151235Z-bootstrap-bbcrawler-workspace");
            Assert.Equal("restored kept turn", kept.Response);
            var preserved = Assert.Single(stored.Turns, turn => turn.RequestId == "req-20260610T151236Z-stale-bbcrawler-workspace");
            Assert.Equal("stale turn", preserved.Response);
            Assert.Contains(preserved.Tags, tag => tag.Tag == "stale");
            Assert.Contains(preserved.Actions, action => action.Description == "stale turn action");
        }
    }

    [Fact]
    public async Task WhenSubmittingWithCopilotStatisticsThenStatisticsArePersisted()
    {
        var dto = CreateTestDto("Copilot", BuildSessionId("Copilot", "stats-session"));
        dto.CopilotStatistics = new CopilotStatisticsDto
        {
            AverageSuccessScore = 0.85,
            TotalNetTokens = 5000,
            TotalNetPremiumRequests = 3,
            CompletedCount = 10,
            InProgressCount = 2
        };

        var id = await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var stored = await _db.SessionLogs.FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal(0.85, stored.CopilotAvgSuccessScore);
        Assert.Equal(5000, stored.CopilotTotalNetTokens);
        Assert.Equal(3, stored.CopilotTotalNetPremiumRequests);
        Assert.Equal(10, stored.CopilotCompletedCount);
        Assert.Equal(2, stored.CopilotInProgressCount);
    }

    [Fact]
    public async Task WhenSubmittingWithWorkspaceThenWorkspaceIsPersisted()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "ws-session"));
        dto.Workspace = new WorkspaceInfoDto
        {
            Project = "FunWasHad",
            TargetFramework = ".NET 9",
            Repository = "sharpninja/FunWasHad",
            Branch = "develop"
        };

        var id = await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var stored = await _db.SessionLogs.FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal("FunWasHad", stored.Project);
        Assert.Equal(".NET 9", stored.TargetFramework);
        Assert.Equal("sharpninja/FunWasHad", stored.Repository);
        Assert.Equal("develop", stored.Branch);
    }

    [Fact]
    public async Task WhenSubmittingWithTagsAndContextThenMultiValuedEntitiesArePersisted()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "multi-valued"));
        dto.Turns![0].Tags = ["csharp", "ef-core"];
        dto.Turns[0].ContextList = ["src/Program.cs", "docs/README.md"];

        var id = await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var entry = await _db.SessionLogTurns
            .Include(e => e.Tags)
            .Include(e => e.ContextItems)
            .FirstAsync(e => e.SessionLogId == id)
            .ConfigureAwait(true);
        Assert.Equal(2, entry.Tags.Count);
        Assert.Contains(entry.Tags, t => t.Tag == "csharp");
        Assert.Equal(2, entry.ContextItems.Count);
    }

    [Fact]
    public async Task WhenQueryingWithNoFiltersThenAllSessionsAreReturned()
    {
        await _sut.SubmitAsync(CreateTestDto("Cursor", BuildSessionId("Cursor", "q1"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("Copilot", BuildSessionId("Copilot", "q2"))).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest()).ConfigureAwait(true);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task QueryAsync_WhenSummaryFieldsAreMissing_DerivesStartedAndTurnCountFromTurns()
    {
        var sessionId = BuildSessionId("ClaudeCode", "summary-derived");
        var dto = CreateTestDto("ClaudeCode", sessionId);
        dto.Started = null;
        dto.TurnCount = 0;
        dto.Turns![0].Timestamp = "2026-06-10T15:40:39Z";

        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "ClaudeCode" }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.TurnCount);
        Assert.StartsWith("2026-06-10T15:40:39", item.Started, StringComparison.Ordinal);
        Assert.Single(item.Turns!);
    }

    [Fact]
    public async Task WhenQueryingByAgentThenOnlyMatchingSessionsAreReturned()
    {
        await _sut.SubmitAsync(CreateTestDto("Cursor", BuildSessionId("Cursor", "agent-1"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("Copilot", BuildSessionId("Copilot", "agent-2"))).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "Cursor" }).ConfigureAwait(true);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Cursor", item.SourceType));
    }

    /// <summary>
    /// Regression: a missing agent filter must not restrict history to marker-defined
    /// source types; mixed Codex, Cursor, Cline, and McpAgent sessions are all in scope.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithoutAgent_ReturnsMixedSourceSessionsForWorkspace()
    {
        await _sut.SubmitAsync(CreateTestDto("Codex", BuildSessionId("Codex", "mixed-codex"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("Cursor", BuildSessionId("Cursor", "mixed-cursor"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("Cline", BuildSessionId("Cline", "mixed-cline"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("McpAgent", BuildSessionId("McpAgent", "mixed-mcpagent"))).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Limit = 10 }).ConfigureAwait(true);

        Assert.Equal(4, result.TotalCount);
        Assert.Contains(result.Items, item => item.SourceType == "Codex");
        Assert.Contains(result.Items, item => item.SourceType == "Cursor");
        Assert.Contains(result.Items, item => item.SourceType == "Cline");
        Assert.Contains(result.Items, item => item.SourceType == "McpAgent");
    }

    /// <summary>
    /// Regression: an explicit agent filter remains active after enabling unfiltered
    /// mixed-source history queries.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WithAgent_ReturnsOnlyMatchingSourceType()
    {
        await _sut.SubmitAsync(CreateTestDto("Codex", BuildSessionId("Codex", "agent-codex"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("Cursor", BuildSessionId("Cursor", "agent-cursor"))).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("McpAgent", BuildSessionId("McpAgent", "agent-mcpagent"))).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "McpAgent" }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal("McpAgent", item.SourceType);
    }

    /// <summary>
    /// Regression: workspace query filters must prevent session history from
    /// leaking rows that belong to another registered workspace.
    /// </summary>
    [Fact]
    public async Task QueryAsync_DoesNotReturnSessionsFromOtherWorkspaces()
    {
        var primary = BuildSutWithWorkspaceContext(WorkspacePath);
        await primary.SubmitAsync(CreateTestDto("Codex", BuildSessionId("Codex", "primary-workspace"))).ConfigureAwait(true);

        var otherWorkspacePath = @"E:\tests\sessionlog-service-other";
        var other = BuildSutWithWorkspaceContext(otherWorkspacePath);
        await other.SubmitAsync(CreateTestDto("Cursor", BuildSessionId("Cursor", "other-workspace"))).ConfigureAwait(true);

        var querySut = BuildSutWithWorkspaceContext(WorkspacePath);
        var result = await querySut.QueryAsync(new SessionLogQueryRequest { Limit = 10 }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal("Codex", item.SourceType);
        Assert.DoesNotContain(result.Items, session => session.SessionId == BuildSessionId("Cursor", "other-workspace"));
    }

    /// <summary>
    /// Regression: recent history should be ordered by last update time, not only
    /// by when the session was originally started.
    /// </summary>
    [Fact]
    public async Task QueryAsync_OrdersByLastUpdatedForRecentActivity()
    {
        var olderStartedRecent = CreateTestDto("Cursor", BuildSessionId("Cursor", "older-start-recent-update"));
        olderStartedRecent.Started = "2026-03-01T00:00:00Z";
        olderStartedRecent.LastUpdated = "2026-05-24T23:16:08Z";
        await _sut.SubmitAsync(olderStartedRecent).ConfigureAwait(true);

        var newerStartedStale = CreateTestDto("Codex", BuildSessionId("Codex", "newer-start-stale-update"));
        newerStartedStale.Started = "2026-05-01T00:00:00Z";
        newerStartedStale.LastUpdated = "2026-05-01T00:10:00Z";
        await _sut.SubmitAsync(newerStartedStale).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Limit = 1 }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal(BuildSessionId("Cursor", "older-start-recent-update"), item.SessionId);
    }

    [Fact]
    public async Task WhenQueryingByDateRangeThenOnlyMatchingSessionsAreReturned()
    {
        var early = CreateTestDto("Cursor", BuildSessionId("Cursor", "early"));
        early.Started = "2026-01-01T00:00:00Z";
        early.LastUpdated = "2026-01-01T12:00:00Z";
        await _sut.SubmitAsync(early).ConfigureAwait(true);

        var late = CreateTestDto("Cursor", BuildSessionId("Cursor", "late"));
        late.Started = "2026-02-01T00:00:00Z";
        late.LastUpdated = "2026-02-01T12:00:00Z";
        await _sut.SubmitAsync(late).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest
        {
            From = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)
        }).ConfigureAwait(true);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(BuildSessionId("Cursor", "late"), result.Items[0].SessionId);
    }

    [Fact]
    public async Task WhenQueryingWithLimitAndOffsetThenPaginationIsApplied()
    {
        for (var i = 0; i < 5; i++)
        {
            var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", $"page-{i}"));
            dto.Started = $"2026-01-{(i + 1):D2}T00:00:00Z";
            await _sut.SubmitAsync(dto).ConfigureAwait(true);
        }

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Limit = 2, Offset = 1 }).ConfigureAwait(true);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Limit);
        Assert.Equal(1, result.Offset);
    }

    [Fact]
    public async Task WhenQueryingWithLimitExceedingMaxThenLimitIsClamped()
    {
        await _sut.SubmitAsync(CreateTestDto("Cursor", BuildSessionId("Cursor", "clamp"))).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Limit = 9999 }).ConfigureAwait(true);

        Assert.Equal(1000, result.Limit);
    }

    [Fact]
    public async Task WhenQueryingByBooleanTextThenTermsCanMatchAcrossTurnFields()
    {
        var match = CreateTestDto("Cursor", BuildSessionId("Cursor", "bool-match"));
        match.Turns![0].QueryTitle = "Alpha kickoff";
        match.Turns[0].Response = "Completed beta rollout";
        await _sut.SubmitAsync(match).ConfigureAwait(true);

        var miss = CreateTestDto("Cursor", BuildSessionId("Cursor", "bool-miss"));
        miss.Turns![0].QueryTitle = "Alpha kickoff";
        miss.Turns[0].Response = "Completed gamma rollout";
        await _sut.SubmitAsync(miss).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest
        {
            Text = "alpha && beta",
        }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal(BuildSessionId("Cursor", "bool-match"), item.SessionId);
    }

    [Fact]
    public async Task WhenSubmittingWithMissingSourceTypeThenArgumentExceptionIsThrown()
    {
        var dto = new UnifiedSessionLogDto { SourceType = null, SessionId = "test" };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitAsync(dto)).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenSubmittingWithMissingSessionIdThenArgumentExceptionIsThrown()
    {
        var dto = new UnifiedSessionLogDto { SourceType = "Cursor", SessionId = null };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitAsync(dto)).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenSubmittingWithNonCanonicalSessionIdThenArgumentExceptionIsThrown()
    {
        var dto = CreateTestDto("Cursor", "cursor-invalid");

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitAsync(dto)).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenSubmittingWithInvalidRequestIdFormatThenArgumentExceptionIsThrown()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "bad-request-id"));
        dto.Turns![0].RequestId = "bad";
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SubmitAsync(dto)).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenQueryResultMappedThenDtoIncludesWorkspaceAndStatistics()
    {
        var dto = CreateTestDto("Copilot", BuildSessionId("Copilot", "round-trip"));
        dto.Workspace = new WorkspaceInfoDto { Project = "TestProject", Branch = "main" };
        dto.CopilotStatistics = new CopilotStatisticsDto { CompletedCount = 5 };
        dto.TotalTokens = 1234;
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "Copilot" }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.NotNull(item.Workspace);
        Assert.Equal("TestProject", item.Workspace!.Project);
        Assert.NotNull(item.CopilotStatistics);
        Assert.Equal(5, item.CopilotStatistics!.CompletedCount);
        Assert.Equal(1234, item.TotalTokens);
    }

    [Fact]
    public async Task WhenSubmittingOffsetTimestampsThenRoundTripUpdateSucceeds()
    {
        var sessionId = BuildSessionId("Cursor", "offset-roundtrip");
        var dto = CreateTestDto("Cursor", sessionId);
        dto.Started = "2026-03-03T12:49:58.717102-06:00";
        dto.LastUpdated = "2026-03-03T12:50:58.717102-06:00";
        dto.Turns![0].Timestamp = "2026-03-03T12:50:12.717102-06:00";
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var queried = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "Cursor" }).ConfigureAwait(true);
        var session = queried.Items.Single(i => i.SessionId == sessionId);

        // Regression coverage: previously, pushing a queried session containing
        // offset timestamps could fail with a server-side 500.
        var ex = await Record.ExceptionAsync(() => _sut.SubmitAsync(session)).ConfigureAwait(true);
        Assert.Null(ex);

        var stored = await _db.SessionLogs
            .Include(s => s.Turns)
            .SingleAsync(s => s.SessionId == sessionId)
            .ConfigureAwait(true);
        Assert.Equal(TimeSpan.Zero, stored.Started?.Offset);
        Assert.Equal(TimeSpan.Zero, stored.LastUpdated?.Offset);
        Assert.Equal(TimeSpan.Zero, stored.Turns.Single().Timestamp?.Offset);
    }

    [Fact]
    public async Task WhenUpsertingWithNewEntryThenEntryIsAddedWithoutRemovingExisting()
    {
        var dto1 = CreateTestDto("Cursor", BuildSessionId("Cursor", "keyed-add"));
        await _sut.SubmitAsync(dto1).ConfigureAwait(true);

        // Submit again with original entry plus a new one
        var dto2 = CreateTestDto("Cursor", BuildSessionId("Cursor", "keyed-add"));
        dto2.Turns!.Add(new UnifiedRequestEntryDto
        {
            RequestId = "req-20260211T100200Z-entry-002",
            QueryText = "New entry",
            Status = "completed"
        });
        dto2.TurnCount = 2;
        var id = await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        var stored = await _db.SessionLogs.Include(s => s.Turns).FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal(2, stored.Turns.Count);
    }

    [Fact]
    public async Task WhenUpsertingExistingEntryThenEntryIsUpdatedInPlace()
    {
        var dto1 = CreateTestDto("Cursor", BuildSessionId("Cursor", "keyed-update"));
        var id = await _sut.SubmitAsync(dto1).ConfigureAwait(true);

        var originalEntryId = (await _db.SessionLogTurns.FirstAsync(e => e.SessionLogId == id).ConfigureAwait(true)).Id;

        // Submit with same RequestId but different content
        var dto2 = CreateTestDto("Cursor", BuildSessionId("Cursor", "keyed-update"));
        dto2.Turns![0].QueryText = "Updated query text";
        dto2.Turns[0].Response = "Updated response";
        await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        var updatedEntry = await _db.SessionLogTurns.FirstAsync(e => e.SessionLogId == id).ConfigureAwait(true);
        Assert.Equal(originalEntryId, updatedEntry.Id); // Same row, updated in place
        Assert.Equal("Updated query text", updatedEntry.QueryText);
        Assert.Equal("Updated response", updatedEntry.Response);
    }

    [Fact]
    public async Task WhenUpsertingWithRemovedEntryThenAbsentDurableEntryIsPreserved()
    {
        var dto1 = CreateTestDto("Cursor", BuildSessionId("Cursor", "keyed-remove"));
        dto1.Turns!.Add(new UnifiedRequestEntryDto
        {
            RequestId = "req-20260211T100200Z-entry-002",
            QueryText = "Will be removed",
            Status = "completed"
        });
        dto1.TurnCount = 2;
        var id = await _sut.SubmitAsync(dto1).ConfigureAwait(true);
        Assert.Equal(2, await _db.SessionLogTurns.CountAsync(e => e.SessionLogId == id).ConfigureAwait(true));

        // TR-MCP-DB-003: Submit with only the first entry must not hard-delete
        // the absent durable turn.
        var dto2 = CreateTestDto("Cursor", BuildSessionId("Cursor", "keyed-remove"));
        dto2.TurnCount = 1;
        await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        Assert.Equal(2, await _db.SessionLogTurns.CountAsync(e => e.SessionLogId == id).ConfigureAwait(true));
        Assert.Contains(
            await _db.SessionLogTurns.Where(e => e.SessionLogId == id).ToListAsync().ConfigureAwait(true),
            turn => turn.RequestId == "req-20260211T100200Z-entry-002");
    }

    [Fact]
    public async Task WhenAppendingDialogItemsThenItemsAreAdded()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "dialog-append"));
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var items = new List<ProcessingDialogItemDto>
        {
            new() { Timestamp = "2026-02-12T10:00:00Z", Role = "model", Content = "Analyzing request", Category = "reasoning" },
            new() { Timestamp = "2026-02-12T10:00:01Z", Role = "tool", Content = "get_file(Program.cs)", Category = "tool_call" }
        };

        var count = await _sut.AppendProcessingDialogAsync("Cursor", BuildSessionId("Cursor", "dialog-append"), "req-20260211T100100Z-entry-001", items).ConfigureAwait(true);

        Assert.Equal(2, count);
        var entry = await _db.SessionLogTurns
            .Include(e => e.ProcessingDialog)
            .FirstAsync(e => e.RequestId == "req-20260211T100100Z-entry-001")
            .ConfigureAwait(true);
        Assert.Equal(2, entry.ProcessingDialog.Count);
        var first = entry.ProcessingDialog.OrderBy(p => p.Ordinal).First();
        Assert.Equal("model", first.Role);
        Assert.Equal("Analyzing request", first.Content);
        Assert.Equal("reasoning", first.Category);
        await _eventBus.Received().PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.SessionLog
                                     && e.Action == ChangeEventActions.Updated
                                     && e.EntityId == $"Cursor/{BuildSessionId("Cursor", "dialog-append")}"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenAppendingDialogMultipleTimesThenOrdinalsAreContinuous()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "dialog-multi"));
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        await _sut.AppendProcessingDialogAsync("Cursor", BuildSessionId("Cursor", "dialog-multi"), "req-20260211T100100Z-entry-001",
            [new ProcessingDialogItemDto { Role = "model", Content = "First batch" }]).ConfigureAwait(true);

        var count = await _sut.AppendProcessingDialogAsync("Cursor", BuildSessionId("Cursor", "dialog-multi"), "req-20260211T100100Z-entry-001",
            [new ProcessingDialogItemDto { Role = "model", Content = "Second batch" }]).ConfigureAwait(true);

        Assert.Equal(2, count);
        var entry = await _db.SessionLogTurns
            .Include(e => e.ProcessingDialog)
            .FirstAsync(e => e.RequestId == "req-20260211T100100Z-entry-001")
            .ConfigureAwait(true);
        var ordinals = entry.ProcessingDialog.OrderBy(p => p.Ordinal).Select(p => p.Ordinal).ToList();
        Assert.Equal([0, 1], ordinals);
    }

    [Fact]
    public async Task WhenAppendingDialogToNonexistentEntryThenThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AppendProcessingDialogAsync("Cursor", BuildSessionId("Cursor", "nonexistent"), "req-20260211T100100Z-entry-001",
                [new ProcessingDialogItemDto { Role = "model", Content = "test" }])).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenQueryingSessionWithDialogThenDialogIsIncludedInDto()
    {
        var dto = CreateTestDto("Copilot", BuildSessionId("Copilot", "dialog-query"));
        dto.Turns![0].ProcessingDialog =
        [
            new ProcessingDialogItemDto { Timestamp = "2026-02-12T10:00:00Z", Role = "model", Content = "Thinking...", Category = "reasoning" }
        ];
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "Copilot" }).ConfigureAwait(true);

        var entry = result.Items.First(i => i.SessionId == BuildSessionId("Copilot", "dialog-query")).Turns!.First();
        Assert.NotNull(entry.ProcessingDialog);
        Assert.Single(entry.ProcessingDialog!);
        Assert.Equal("model", entry.ProcessingDialog![0].Role);
        Assert.Equal("Thinking...", entry.ProcessingDialog[0].Content);
    }

    /// <summary>
    /// TR-MCP-MT-003A: SubmitAsync must stamp the resolved workspace context onto the
    /// persisted SessionLogEntity so subsequent reads under the same workspace context
    /// are not hidden by the global query filter.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_StampsWorkspaceIdOnSessionEntity()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);

        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "ws-stamp"));
        var id = await sut.SubmitAsync(dto).ConfigureAwait(true);

        var stored = await _db.SessionLogs
            .IgnoreQueryFilters()
            .FirstAsync(s => s.Id == id)
            .ConfigureAwait(true);
        Assert.Equal(WorkspacePath, stored.WorkspaceId);
    }

    /// <summary>
    /// TR-MCP-MT-003A: Child entities (turns, actions, tags, context items, dialog,
    /// commits, string-list items) must also carry the parent's WorkspaceId so they
    /// pass the per-entity query filters at <see cref="McpDbContext.OnModelCreating"/>.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_StampsWorkspaceIdOnEveryChildEntity()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);

        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "ws-children"));
        dto.Turns![0].Actions =
        [
            new UnifiedActionDto { Order = 0, Description = "Edit Program.cs", Type = "edit", Status = "completed", FilePath = "Program.cs" }
        ];
        dto.Turns[0].Tags = ["csharp", "ef-core"];
        dto.Turns[0].ContextList = ["docs/README.md"];
        dto.Turns[0].ProcessingDialog =
        [
            new ProcessingDialogItemDto { Role = "model", Content = "thinking", Category = "reasoning" }
        ];
        dto.Turns[0].Commits =
        [
            new SessionLogCommitDto { Sha = "abc123", Branch = "main", Message = "commit msg", Author = "x", FilesChanged = ["a.cs"] }
        ];
        dto.Turns[0].FilesModified = ["a.cs"];

        var id = await sut.SubmitAsync(dto).ConfigureAwait(true);

        var turn = await _db.SessionLogTurns.IgnoreQueryFilters().FirstAsync(t => t.SessionLogId == id).ConfigureAwait(true);
        Assert.Equal(WorkspacePath, turn.WorkspaceId);

        var action = await _db.SessionLogActions.IgnoreQueryFilters().FirstAsync(a => a.SessionLogTurnId == turn.Id).ConfigureAwait(true);
        Assert.Equal(WorkspacePath, action.WorkspaceId);

        var tag = await _db.SessionLogTurnTags.IgnoreQueryFilters().FirstAsync(t => t.SessionLogTurnId == turn.Id).ConfigureAwait(true);
        Assert.Equal(WorkspacePath, tag.WorkspaceId);

        var context = await _db.SessionLogTurnContexts.IgnoreQueryFilters().FirstAsync(c => c.SessionLogTurnId == turn.Id).ConfigureAwait(true);
        Assert.Equal(WorkspacePath, context.WorkspaceId);

        var dialog = await _db.SessionLogProcessingDialogs.IgnoreQueryFilters().FirstAsync(d => d.SessionLogTurnId == turn.Id).ConfigureAwait(true);
        Assert.Equal(WorkspacePath, dialog.WorkspaceId);

        var commit = await _db.SessionLogCommits.IgnoreQueryFilters().FirstAsync(c => c.SessionLogTurnId == turn.Id).ConfigureAwait(true);
        Assert.Equal(WorkspacePath, commit.WorkspaceId);

        var stringList = await _db.SessionLogTurnStringLists.IgnoreQueryFilters().FirstAsync(s => s.SessionLogTurnId == turn.Id).ConfigureAwait(true);
        Assert.Equal(WorkspacePath, stringList.WorkspaceId);
    }

    /// <summary>
    /// TR-MCP-MT-003A: When no workspace context is injected (ingestion / batch import
    /// path) and the DbContext has no workspace override either, WorkspaceId must
    /// default to empty string and not crash. Uses a dedicated DbContext so the
    /// fixture-level <see cref="McpDbContext.OverrideWorkspaceId"/> does not auto-fill
    /// the field via <see cref="McpDbContext.SaveChangesAsync"/>'s built-in stamping.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_WithNullWorkspaceContext_KeepsWorkspaceIdEmpty()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTests_NullCtx_{Guid.NewGuid()}")
            .Options;
        using var db = new McpDbContext(options);
        db.Database.EnsureCreated();

        var sut = new SessionLogService(db, NullLogger<SessionLogService>.Instance, _eventBus, workspaceContext: null);

        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "no-ws"));
        var id = await sut.SubmitAsync(dto).ConfigureAwait(true);

        var stored = await db.SessionLogs
            .IgnoreQueryFilters()
            .FirstAsync(s => s.Id == id)
            .ConfigureAwait(true);
        Assert.Equal(string.Empty, stored.WorkspaceId);
    }

    /// <summary>
    /// BUG-APPVISIBILITY-001: Session-log reads must honor the request workspace
    /// even when <see cref="McpDbContext"/> was constructed before the scoped
    /// <see cref="WorkspaceContext"/> was populated by middleware or stdio wiring.
    /// </summary>
    [Fact]
    public async Task QueryGetAndHashCheckAsync_UseWorkspaceContextWhenDbContextWasConstructedBeforeWorkspaceResolved()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTests_StaleCtx_Reads_{Guid.NewGuid()}")
            .Options;
        var workspaceContext = new WorkspaceContext();
        using var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(db, NullLogger<SessionLogService>.Instance, _eventBus, workspaceContext);

        workspaceContext.WorkspacePath = WorkspacePath;
        var sessionId = BuildSessionId("Codex", "stale-query");
        await sut.SubmitAsync(CreateTestDto("Codex", sessionId), contentHash: "hash-stale-query").ConfigureAwait(true);

        db.OverrideWorkspaceId(string.Empty);
        var query = await sut.QueryAsync(new SessionLogQueryRequest { Agent = "Codex" }).ConfigureAwait(true);

        var item = Assert.Single(query.Items);
        Assert.Equal(sessionId, item.SessionId);
        Assert.Equal(WorkspacePath, db.CurrentWorkspaceId);

        db.OverrideWorkspaceId(string.Empty);
        var fetched = await sut.GetAsync("Codex", sessionId).ConfigureAwait(true);

        Assert.NotNull(fetched);
        Assert.Equal(sessionId, fetched!.SessionId);
        Assert.Equal(WorkspacePath, db.CurrentWorkspaceId);

        db.OverrideWorkspaceId(string.Empty);
        var unchanged = await sut.IsUnchangedAsync("Codex", sessionId, "hash-stale-query").ConfigureAwait(true);

        Assert.True(unchanged);
        Assert.Equal(WorkspacePath, db.CurrentWorkspaceId);
    }

    /// <summary>
    /// BUG-APPVISIBILITY-001: Incremental turn and dialog mutation paths must
    /// re-synchronize the DbContext discriminator before locating existing rows.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAndAppendDialogAsync_UseWorkspaceContextWhenDbContextWasConstructedBeforeWorkspaceResolved()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTests_StaleCtx_Mutations_{Guid.NewGuid()}")
            .Options;
        var workspaceContext = new WorkspaceContext();
        using var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(db, NullLogger<SessionLogService>.Instance, _eventBus, workspaceContext);

        workspaceContext.WorkspacePath = WorkspacePath;
        var sessionId = BuildSessionId("Cursor", "stale-turn");
        await sut.SubmitAsync(CreateTestDto("Cursor", sessionId)).ConfigureAwait(true);

        db.OverrideWorkspaceId(string.Empty);
        var turn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260527T014000Z-stale-turn",
            Timestamp = "2026-05-27T01:40:00Z",
            QueryText = "append through stale db context",
            Status = "completed",
            Actions =
            [
                new UnifiedActionDto
                {
                    Description = "Recorded stale context turn append regression",
                    Type = "session_turn",
                    Status = "completed",
                    FilePath = "tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs"
                }
            ]
        };

        var turnId = await sut.UpsertTurnAsync("Cursor", sessionId, turn).ConfigureAwait(true);

        Assert.True(turnId > 0);
        Assert.Equal(WorkspacePath, db.CurrentWorkspaceId);

        db.OverrideWorkspaceId(string.Empty);
        var dialogCount = await sut.AppendProcessingDialogAsync(
            "Cursor",
            sessionId,
            "req-20260527T014000Z-stale-turn",
            [new ProcessingDialogItemDto { Role = "model", Content = "visible after workspace sync", Category = "reasoning" }])
            .ConfigureAwait(true);

        Assert.Equal(1, dialogCount);
        Assert.Equal(WorkspacePath, db.CurrentWorkspaceId);
    }

    /// <summary>
    /// BUG-APPVISIBILITY-001: A long-lived service instance must return records
    /// for the current workspace after the scoped workspace context changes, and
    /// must not leak records from a different workspace.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WhenWorkspaceContextChanges_ReturnsOnlyCurrentWorkspaceRows()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTests_TwoWorkspace_{Guid.NewGuid()}")
            .Options;
        var workspaceContext = new WorkspaceContext();
        using var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(db, NullLogger<SessionLogService>.Instance, _eventBus, workspaceContext);

        var primarySessionId = BuildSessionId("Codex", "primary-visible");
        workspaceContext.WorkspacePath = WorkspacePath;
        await sut.SubmitAsync(CreateTestDto("Codex", primarySessionId)).ConfigureAwait(true);

        var otherWorkspacePath = @"E:\tests\sessionlog-service-other";
        var otherSessionId = BuildSessionId("Cursor", "other-visible");
        workspaceContext.WorkspacePath = otherWorkspacePath;
        await sut.SubmitAsync(CreateTestDto("Cursor", otherSessionId)).ConfigureAwait(true);

        workspaceContext.WorkspacePath = WorkspacePath;
        var primaryResult = await sut.QueryAsync(new SessionLogQueryRequest { Limit = 10 }).ConfigureAwait(true);

        var primaryItem = Assert.Single(primaryResult.Items);
        Assert.Equal(primarySessionId, primaryItem.SessionId);
        Assert.DoesNotContain(primaryResult.Items, item => item.SessionId == otherSessionId);

        workspaceContext.WorkspacePath = otherWorkspacePath;
        var otherResult = await sut.QueryAsync(new SessionLogQueryRequest { Limit = 10 }).ConfigureAwait(true);

        var otherItem = Assert.Single(otherResult.Items);
        Assert.Equal(otherSessionId, otherItem.SessionId);
        Assert.DoesNotContain(otherResult.Items, item => item.SessionId == primarySessionId);
    }

    /// <summary>
    /// FR-SUPPORT-010C: <c>UpsertTurnAsync</c> creates a new turn on an existing
    /// session without deleting any sibling turns. Guards the per-turn helper
    /// against the delete-stale behavior of the full-session upsert.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurn_AppendsWithoutDeletingSiblings()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        var sessionId = BuildSessionId("Cursor", "turn-append");
        var initial = CreateTestDto("Cursor", sessionId);
        await sut.SubmitAsync(initial).ConfigureAwait(true);

        var newTurn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260516T120000Z-second",
            Timestamp = "2026-05-16T12:00:00Z",
            QueryText = "second turn",
            Status = "completed",
            Actions =
            [
                new UnifiedActionDto
                {
                    Description = "Recorded service turn append",
                    Type = "session_turn",
                    Status = "completed",
                    FilePath = "tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTests.cs"
                }
            ]
        };

        var turnId = await sut.UpsertTurnAsync("Cursor", sessionId, newTurn).ConfigureAwait(true);

        Assert.True(turnId > 0);
        var turns = await _db.SessionLogTurns
            .IgnoreQueryFilters()
            .Where(t => t.SessionLog!.SessionId == sessionId)
            .ToListAsync()
            .ConfigureAwait(true);
        Assert.Equal(2, turns.Count);
        Assert.Contains(turns, t => t.RequestId == "req-20260211T100100Z-entry-001");
        Assert.Contains(turns, t => t.RequestId == "req-20260516T120000Z-second");
    }

    /// <summary>
    /// FR-SUPPORT-010C: <c>UpsertTurnAsync</c> persists structured turn fields so
    /// agents do not have to fold audit data into the response text.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurn_PersistsStructuredFields()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        var sessionId = BuildSessionId("ClaudeCode", "structured-turn-append");
        await sut.SubmitAsync(CreateTestDto("ClaudeCode", sessionId)).ConfigureAwait(true);

        var newTurn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260610T154039Z-structured-turn",
            Timestamp = "2026-06-10T15:40:39Z",
            QueryTitle = "Repro turn",
            QueryText = "Structured fields repro",
            Response = "testing structured fields",
            Interpretation = "structured DTO sub-field repro",
            Status = "in_progress",
            TokenCount = 123,
            Tags = ["repro"],
            ContextList = ["src/McpServer.Repl.Core/GenericClientPassthrough.cs"],
            Actions =
            [
                new UnifiedActionDto
                {
                    Order = 1,
                    Description = "repro action",
                    Type = "edit",
                    Status = "completed",
                    FilePath = "src/test.cs"
                }
            ]
        };

        await sut.UpsertTurnAsync("ClaudeCode", sessionId, newTurn).ConfigureAwait(true);

        var fetched = await sut.GetAsync("ClaudeCode", sessionId).ConfigureAwait(true);

        Assert.NotNull(fetched);
        var appended = Assert.Single(fetched!.Turns!, turn => turn.RequestId == "req-20260610T154039Z-structured-turn");
        Assert.Equal("structured DTO sub-field repro", appended.Interpretation);
        Assert.Equal(123, appended.TokenCount);
        Assert.Equal("repro", Assert.Single(appended.Tags!));
        Assert.Equal("src/McpServer.Repl.Core/GenericClientPassthrough.cs", Assert.Single(appended.ContextList!));
        var action = Assert.Single(appended.Actions!);
        Assert.Equal(1, action.Order);
        Assert.Equal("repro action", action.Description);
        Assert.Equal("src/test.cs", action.FilePath);
    }

    /// <summary>
    /// FR-SUPPORT-010C: <c>UpsertTurnAsync</c> on an existing requestId updates the
    /// row in place rather than inserting a duplicate.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_ExistingTurn_UpdatesInPlace()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        var sessionId = BuildSessionId("Cursor", "turn-update");
        var initial = CreateTestDto("Cursor", sessionId);
        await sut.SubmitAsync(initial).ConfigureAwait(true);

        var updatedTurn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260211T100100Z-entry-001",
            Timestamp = "2026-02-11T10:01:00Z",
            QueryText = "updated query",
            Response = "updated response",
            Status = "completed",
            DesignDecisions =
            [
                "Keep UpsertTurnAsync focused on updating the addressed turn in place."
            ]
        };

        await sut.UpsertTurnAsync("Cursor", sessionId, updatedTurn).ConfigureAwait(true);

        var turn = await _db.SessionLogTurns
            .IgnoreQueryFilters()
            .SingleAsync(t => t.RequestId == "req-20260211T100100Z-entry-001")
            .ConfigureAwait(true);
        Assert.Equal("updated query", turn.QueryText);
        Assert.Equal("updated response", turn.Response);
    }

    /// <summary>
    /// FR-SUPPORT-010C: Incremental per-turn updates preserve existing structured
    /// child rows when the later DTO omits those collections.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_ExistingTurn_PreservesStructuredCollectionsWhenOmitted()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        var sessionId = BuildSessionId("ClaudeCode", "turn-merge-structured");
        await sut.SubmitAsync(CreateTestDto("ClaudeCode", sessionId)).ConfigureAwait(true);

        await sut.UpsertTurnAsync("ClaudeCode", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = "req-20260211T100100Z-entry-001",
            Timestamp = "2026-02-11T10:01:00Z",
            QueryText = "initial structured query",
            Response = "initial response",
            Status = "in_progress",
            Tags = ["repro"],
            ContextList = ["src/McpServer.Services/Services/SessionLogService.cs"],
            Actions =
            [
                new UnifiedActionDto
                {
                    Order = 1,
                    Description = "preserve action",
                    Type = "edit",
                    Status = "completed",
                    FilePath = "src/file.cs"
                }
            ],
            FilesModified = ["src/file.cs"],
            Blockers = ["initial blocker"]
        }).ConfigureAwait(true);

        await sut.UpsertTurnAsync("ClaudeCode", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = "req-20260211T100100Z-entry-001",
            Response = "final response",
            Status = "completed",
            DesignDecisions = ["Per-turn updates are merged so omitted structured collections are preserved."]
        }).ConfigureAwait(true);

        var fetched = await sut.GetAsync("ClaudeCode", sessionId).ConfigureAwait(true);

        Assert.NotNull(fetched);
        var turn = Assert.Single(fetched!.Turns!, item => item.RequestId == "req-20260211T100100Z-entry-001");
        Assert.Equal("final response", turn.Response);
        Assert.Equal("repro", Assert.Single(turn.Tags!));
        Assert.Equal("src/McpServer.Services/Services/SessionLogService.cs", Assert.Single(turn.ContextList!));
        Assert.Equal("preserve action", Assert.Single(turn.Actions!).Description);
        Assert.Equal("src/file.cs", Assert.Single(turn.FilesModified!));
        Assert.Equal("initial blocker", Assert.Single(turn.Blockers!));
        Assert.Equal(
            "Per-turn updates are merged so omitted structured collections are preserved.",
            Assert.Single(turn.DesignDecisions!));
    }

    /// <summary>
    /// FR-SUPPORT-010C: <c>UpsertTurnAsync</c> rejects terminal turn states without
    /// decision, action, or commit evidence ONLY for Quad-Brain ACID agent sessions
    /// (SourceType <c>QBAgent</c> = <c>McpHostedAgentDefaults.QBAgentSourceType</c>).
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_AcidAgentClosingTurnWithoutEvidence_ThrowsArgumentException()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        const string qbAgentSourceType = "QBAgent";
        var sessionId = BuildSessionId(qbAgentSourceType, "turn-close-validation");
        await sut.SubmitAsync(CreateTestDto(qbAgentSourceType, sessionId)).ConfigureAwait(true);

        var turn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260516T120100Z-no-evidence",
            Timestamp = "2026-05-16T12:01:00Z",
            QueryText = "terminal turn without audit evidence",
            Status = "completed"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => sut.UpsertTurnAsync(qbAgentSourceType, sessionId, turn))
            .ConfigureAwait(true);

        Assert.Contains("no decision, action, or commit items", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Compliance with Session Logging Requirements is not optional.", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-SUPPORT-010C: <c>UpsertTurnAsync</c> allows a standard (non-Quad-Brain) agent
    /// session to close a terminal turn without decision, action, or commit evidence.
    /// The ACID compliance gate must not leak into the standard session-log endpoints.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_StandardAgentClosingTurnWithoutEvidence_Succeeds()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        var sessionId = BuildSessionId("Cursor", "turn-close-standard");
        await sut.SubmitAsync(CreateTestDto("Cursor", sessionId)).ConfigureAwait(true);

        var turn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260516T120200Z-no-evidence-ok",
            Timestamp = "2026-05-16T12:02:00Z",
            QueryText = "standard terminal turn without audit evidence",
            Status = "completed"
        };

        var id = await sut.UpsertTurnAsync("Cursor", sessionId, turn).ConfigureAwait(true);

        Assert.True(id > 0);
    }

    /// <summary>
    /// FR-SUPPORT-010C: <c>UpsertTurnAsync</c> throws when the parent session does
    /// not exist so the controller can map to 404.
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_SessionMissing_ThrowsInvalidOperationException()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        var turn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260516T120000Z-x",
            QueryText = "x",
            Status = "completed"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.UpsertTurnAsync("Cursor", BuildSessionId("Cursor", "missing"), turn))
            .ConfigureAwait(true);
    }

    /// <summary>
    /// FR-SUPPORT-010A: After submit, <c>GetAsync</c> by (sourceType, sessionId)
    /// returns the same record under the same workspace context.
    /// </summary>
    [Fact]
    public async Task GetAsync_AfterSubmit_ReturnsRecord()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);
        var sessionId = BuildSessionId("Cursor", "get-by-id");
        await sut.SubmitAsync(CreateTestDto("Cursor", sessionId)).ConfigureAwait(true);

        var fetched = await sut.GetAsync("Cursor", sessionId).ConfigureAwait(true);

        Assert.NotNull(fetched);
        Assert.Equal(sessionId, fetched!.SessionId);
        Assert.Equal("Cursor", fetched.SourceType);
    }

    /// <summary>
    /// FR-SUPPORT-010A: <c>GetAsync</c> returns null for a session that does not
    /// exist (controller maps to 404).
    /// </summary>
    [Fact]
    public async Task GetAsync_Missing_ReturnsNull()
    {
        var sut = BuildSutWithWorkspaceContext(WorkspacePath);

        var fetched = await sut.GetAsync("Cursor", BuildSessionId("Cursor", "absent")).ConfigureAwait(true);

        Assert.Null(fetched);
    }

    #region Phase 0 - workspace stamping and child-filter bugs (BUG-SESSIONLOG-WS-001..004, repro 2026-06-12)

    private const string DriftedWorkspacePath = @"E:\tests\sessionlog-service-DRIFTED";

    /// <summary>
    /// BUG-SESSIONLOG-WS-001 (Bug A): a turn row whose WorkspaceId drifted away from
    /// its parent session must still be matched by request id on upsert. Today the
    /// workspace query filter hides the row, the service INSERTs a duplicate, and the
    /// unique index (SessionLogId, RequestId) throws (HTTP 500 in production).
    /// </summary>
    [Fact]
    public async Task UpsertTurnAsync_DriftedTurnStamp_UpdatesExistingTurnInsteadOfDuplicateInsert()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "ws-drift-upsert");

        var (sut1, db1) = BuildSqliteSut(connection);
        using (db1)
        {
            await sut1.SubmitAsync(CreateTestDto("ClaudeCode", sessionId)).ConfigureAwait(true);
        }

        DriftTurnRows(connection, sessionId);

        var (sut2, db2) = BuildSqliteSut(connection);
        using (db2)
        {
            var update = new UnifiedRequestEntryDto
            {
                RequestId = "req-20260211T100100Z-entry-001",
                Timestamp = "2026-02-11T10:05:00Z",
                QueryText = "How do I configure EF Core?",
                Response = "Updated response after drift",
                Status = "in_progress"
            };

            var turnId = await sut2.UpsertTurnAsync("ClaudeCode", sessionId, update).ConfigureAwait(true);

            Assert.True(turnId > 0);
            Assert.Equal(1, CountTurnRows(connection, "req-20260211T100100Z-entry-001"));
        }
    }

    /// <summary>
    /// BUG-SESSIONLOG-WS-002 (Bug A invariant): every child row written by submit or
    /// turn upsert carries the parent session's WorkspaceId, for both the explicit
    /// workspace-context path and the ambient auto-stamp path.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SubmitAndUpsert_ChildRowsAlwaysMatchParentSessionWorkspace(bool useServiceWorkspaceContext)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "ws-stamp-invariant");

        var (sut, db) = BuildSqliteSut(connection, useServiceWorkspaceContext);
        using (db)
        {
            var dto = CreateTestDto("ClaudeCode", sessionId);
            dto.Turns![0].Actions = [new UnifiedActionDto { Order = 1, Description = "edit", Type = "edit", Status = "completed", FilePath = "src/a.cs" }];
            dto.Turns[0].Tags = ["phase0"];
            dto.Turns[0].Commits = [new SessionLogCommitDto { Sha = "abc123", Branch = "main", Message = "m" }];
            dto.Turns[0].DesignDecisions = ["Decision: stamp children from parent."];
            await sut.SubmitAsync(dto).ConfigureAwait(true);

            var richTurn = new UnifiedRequestEntryDto
            {
                RequestId = "req-20260211T110000Z-entry-002",
                Timestamp = "2026-02-11T11:00:00Z",
                QueryText = "second turn",
                Status = "completed",
                DesignDecisions = ["Decision: second turn keeps invariant."],
                Commits = [new SessionLogCommitDto { Sha = "def456", Branch = "main", Message = "m2" }]
            };
            await sut.UpsertTurnAsync("ClaudeCode", sessionId, richTurn).ConfigureAwait(true);

            var session = await db.SessionLogs.IgnoreQueryFilters()
                .Include(s => s.Turns).ThenInclude(t => t.Actions)
                .Include(s => s.Turns).ThenInclude(t => t.Tags)
                .Include(s => s.Turns).ThenInclude(t => t.Commits)
                .Include(s => s.Turns).ThenInclude(t => t.StringListItems)
                .FirstAsync(s => s.SessionId == sessionId).ConfigureAwait(true);

            Assert.False(string.IsNullOrEmpty(session.WorkspaceId));
            foreach (var turn in session.Turns)
            {
                Assert.Equal(session.WorkspaceId, turn.WorkspaceId);
                foreach (var a in turn.Actions) Assert.Equal(session.WorkspaceId, a.WorkspaceId);
                foreach (var t in turn.Tags) Assert.Equal(session.WorkspaceId, t.WorkspaceId);
                foreach (var c in turn.Commits) Assert.Equal(session.WorkspaceId, c.WorkspaceId);
                foreach (var s in turn.StringListItems) Assert.Equal(session.WorkspaceId, s.WorkspaceId);
            }
        }
    }

    /// <summary>
    /// BUG-SESSIONLOG-WS-003 (Bug B): a bare status-only submit (no turns) on a session
    /// whose turn carries commits must not sever the required Turn-Commit association
    /// and must not corrupt the persisted turn count - including when the child rows'
    /// stamps drifted (the 2026-06-12 production close failure).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmitAsync_BareStatusUpdate_PreservesTurnsAndCommits(bool driftChildStamps)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "ws-bare-close");

        var (sut1, db1) = BuildSqliteSut(connection);
        using (db1)
        {
            await sut1.SubmitAsync(CreateTestDto("ClaudeCode", sessionId, title: "Before close")).ConfigureAwait(true);
            var turnWithCommit = new UnifiedRequestEntryDto
            {
                RequestId = "req-20260211T120000Z-entry-002",
                Timestamp = "2026-02-11T12:00:00Z",
                QueryText = "work",
                Status = "completed",
                DesignDecisions = ["Decision: ship it."],
                Commits = [new SessionLogCommitDto { Sha = "554ab3d", Branch = "main", Message = "fix(plugin): drop .mcp.json" }]
            };
            await sut1.UpsertTurnAsync("ClaudeCode", sessionId, turnWithCommit).ConfigureAwait(true);
        }

        if (driftChildStamps)
            DriftTurnRows(connection, sessionId);

        var (sut2, db2) = BuildSqliteSut(connection);
        using (db2)
        {
            var bareClose = new UnifiedSessionLogDto
            {
                SourceType = "ClaudeCode",
                SessionId = sessionId,
                Title = "Before close",
                Status = "completed"
            };

            await sut2.SubmitAsync(bareClose).ConfigureAwait(true);
        }

        var (_, verifyDb) = BuildSqliteSut(connection);
        using (verifyDb)
        {
            var session = await verifyDb.SessionLogs.IgnoreQueryFilters()
                .Include(s => s.Turns).ThenInclude(t => t.Commits)
                .FirstAsync(s => s.SessionId == sessionId).ConfigureAwait(true);

            Assert.Equal("completed", session.Status);
            Assert.Equal(2, session.Turns.Count);
            Assert.Equal(2, session.TurnCount);
            Assert.Single(session.Turns.SelectMany(t => t.Commits));
        }
    }

    /// <summary>
    /// BUG-SESSIONLOG-WS-004 (Bug C): query-history and per-id get report the real turn
    /// count even when child stamps drifted. Today the filtered Turns collection yields
    /// turnCount 0 while the rows still exist.
    /// </summary>
    [Fact]
    public async Task QueryAsync_DriftedTurnStamps_ReportsRealTurnCount()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "ws-drift-count");

        var (sut1, db1) = BuildSqliteSut(connection);
        using (db1)
        {
            await sut1.SubmitAsync(CreateTestDto("ClaudeCode", sessionId)).ConfigureAwait(true);
        }

        DriftTurnRows(connection, sessionId);

        var (sut2, db2) = BuildSqliteSut(connection);
        using (db2)
        {
            var page = await sut2.QueryAsync(new SessionLogQueryRequest { Agent = "ClaudeCode", Limit = 50 }).ConfigureAwait(true);
            var listed = Assert.Single(page.Items, s => s.SessionId == sessionId);
            Assert.Equal(1, listed.TurnCount);

            var fetched = await sut2.GetAsync("ClaudeCode", sessionId).ConfigureAwait(true);
            Assert.NotNull(fetched);
            Assert.Single(fetched!.Turns!);
        }
    }

    /// <summary>
    /// FR-SUPPORT-010D isolation guard: with child query filters removed, dialog append
    /// keyed by (sourceType, sessionId, requestId) must still resolve the turn through
    /// the workspace-filtered parent session and never touch another workspace's turn
    /// that shares the same identifiers.
    /// </summary>
    [Fact]
    public async Task AppendProcessingDialogAsync_SameIdsInTwoWorkspaces_OnlyTouchesCurrentWorkspace()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "ws-isolation-dialog");

        var (sutW1, dbW1) = BuildSqliteSut(connection, workspacePath: WorkspacePath);
        using (dbW1)
        {
            await sutW1.SubmitAsync(CreateTestDto("ClaudeCode", sessionId)).ConfigureAwait(true);
        }

        var (sutW2, dbW2) = BuildSqliteSut(connection, workspacePath: DriftedWorkspacePath);
        using (dbW2)
        {
            await sutW2.SubmitAsync(CreateTestDto("ClaudeCode", sessionId)).ConfigureAwait(true);
        }

        var (sutW1b, dbW1b) = BuildSqliteSut(connection, workspacePath: WorkspacePath);
        using (dbW1b)
        {
            var added = await sutW1b.AppendProcessingDialogAsync(
                "ClaudeCode",
                sessionId,
                "req-20260211T100100Z-entry-001",
                [new ProcessingDialogItemDto { Timestamp = "2026-02-11T10:02:00Z", Role = "model", Content = "w1 only", Category = "reasoning" }])
                .ConfigureAwait(true);
            Assert.Equal(1, added);
        }

        var (_, verifyDb) = BuildSqliteSut(connection);
        using (verifyDb)
        {
            var dialogOwners = await verifyDb.SessionLogProcessingDialogs.IgnoreQueryFilters()
                .Where(d => d.Content == "w1 only")
                .Select(d => d.SessionLogTurn!.SessionLog!.WorkspaceId)
                .ToListAsync().ConfigureAwait(true);
            var owner = Assert.Single(dialogOwners);
            Assert.Equal(WorkspacePath, owner);
        }
    }

    /// <summary>
    /// BUG-SESSIONLOG-WS-005: the data-repair routine re-stamps drifted child rows to
    /// their parent session's WorkspaceId and is idempotent.
    /// </summary>
    [Fact]
    public async Task RepairWorkspaceStampsAsync_RestampsDriftedChildrenToParent_Idempotent()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "ws-repair");

        var (sut1, db1) = BuildSqliteSut(connection);
        using (db1)
        {
            var dto = CreateTestDto("ClaudeCode", sessionId);
            dto.Turns![0].Commits = [new SessionLogCommitDto { Sha = "abc", Branch = "main", Message = "m" }];
            dto.Turns[0].DesignDecisions = ["Decision: repair."];
            await sut1.SubmitAsync(dto).ConfigureAwait(true);
        }

        DriftTurnRows(connection, sessionId, driftGrandchildren: true);

        var (sut2, db2) = BuildSqliteSut(connection);
        using (db2)
        {
            var dryRunCount = await sut2.RepairWorkspaceStampsAsync(dryRun: true).ConfigureAwait(true);
            Assert.True(dryRunCount > 0);
            var dryRunRepeat = await sut2.RepairWorkspaceStampsAsync(dryRun: true).ConfigureAwait(true);
            Assert.Equal(dryRunCount, dryRunRepeat);

            var firstPass = await sut2.RepairWorkspaceStampsAsync().ConfigureAwait(true);
            Assert.Equal(dryRunCount, firstPass);
            var secondPass = await sut2.RepairWorkspaceStampsAsync().ConfigureAwait(true);
            Assert.Equal(0, secondPass);

            var session = await db2.SessionLogs.IgnoreQueryFilters()
                .Include(s => s.Turns).ThenInclude(t => t.Commits)
                .Include(s => s.Turns).ThenInclude(t => t.StringListItems)
                .FirstAsync(s => s.SessionId == sessionId).ConfigureAwait(true);
            foreach (var turn in session.Turns)
            {
                Assert.Equal(session.WorkspaceId, turn.WorkspaceId);
                foreach (var c in turn.Commits) Assert.Equal(session.WorkspaceId, c.WorkspaceId);
                foreach (var s in turn.StringListItems) Assert.Equal(session.WorkspaceId, s.WorkspaceId);
            }
        }
    }

    /// <summary>
    /// FR-SUPPORT-010F: whole-session submit is ADDITIVE. Omitted session scalars
    /// (title, model) survive a partial submit; supplied scalars update.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_PartialSessionPayload_PreservesOmittedSessionScalars()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "additive-session");

        var (sut1, db1) = BuildSqliteSut(connection);
        using (db1)
        {
            var full = CreateTestDto("ClaudeCode", sessionId, title: "Original title");
            full.Model = "claude-fable-5";
            await sut1.SubmitAsync(full).ConfigureAwait(true);
        }

        var (sut2, db2) = BuildSqliteSut(connection);
        using (db2)
        {
            await sut2.SubmitAsync(new UnifiedSessionLogDto
            {
                SourceType = "ClaudeCode",
                SessionId = sessionId,
                Status = "completed"
            }).ConfigureAwait(true);

            var stored = await db2.SessionLogs.IgnoreQueryFilters()
                .FirstAsync(s => s.SessionId == sessionId).ConfigureAwait(true);
            Assert.Equal("completed", stored.Status);
            Assert.Equal("Original title", stored.Title);
            Assert.Equal("claude-fable-5", stored.Model);
        }
    }

    /// <summary>
    /// FR-SUPPORT-010F: re-submitting an existing turn through whole-session submit
    /// merges instead of clobbering - omitted turn scalars (response, queryText)
    /// survive; previously appended collections survive.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_SparseTurnInSessionPayload_PreservesOmittedTurnFields()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var sessionId = BuildSessionId("ClaudeCode", "additive-turn");
        const string requestId = "req-20260211T100100Z-entry-001";

        var (sut1, db1) = BuildSqliteSut(connection);
        using (db1)
        {
            var full = CreateTestDto("ClaudeCode", sessionId);
            full.Turns![0].Response = "Original response";
            full.Turns[0].Interpretation = "Original interpretation";
            full.Turns[0].DesignDecisions = ["Decision: keep data."];
            await sut1.SubmitAsync(full).ConfigureAwait(true);
        }

        var (sut2, db2) = BuildSqliteSut(connection);
        using (db2)
        {
            await sut2.SubmitAsync(new UnifiedSessionLogDto
            {
                SourceType = "ClaudeCode",
                SessionId = sessionId,
                Turns =
                [
                    new UnifiedRequestEntryDto
                    {
                        RequestId = requestId,
                        Status = "completed",
                        Tags = ["wrap-up"]
                    }
                ]
            }).ConfigureAwait(true);

            var turn = await db2.SessionLogTurns.IgnoreQueryFilters()
                .Include(t => t.Tags)
                .Include(t => t.StringListItems)
                .FirstAsync(t => t.RequestId == requestId).ConfigureAwait(true);
            Assert.Equal("completed", turn.Status);
            Assert.Equal("Original response", turn.Response);
            Assert.Equal("Original interpretation", turn.Interpretation);
            Assert.Equal("How do I configure EF Core?", turn.QueryText);
            Assert.Contains(turn.StringListItems, s => s.ListType == "DesignDecision" && s.Value == "Decision: keep data.");
            Assert.Contains(turn.Tags, t => t.Tag == "wrap-up");
        }
    }

    private static void DriftTurnRows(SqliteConnection connection, string sessionId, bool driftGrandchildren = false)
    {
        EnsureWorkspaceRow(connection, DriftedWorkspacePath);
        ExecuteSql(connection,
            $"UPDATE SessionLogTurns SET WorkspaceId = @ws WHERE SessionLogId IN (SELECT Id FROM SessionLogs WHERE SessionId = @sid)",
            ("@ws", DriftedWorkspacePath), ("@sid", sessionId));
        if (driftGrandchildren)
        {
            foreach (var table in new[] { "SessionLogCommits", "SessionLogTurnStringLists", "SessionLogActions", "SessionLogTurnTags" })
            {
                ExecuteSql(connection,
                    $"UPDATE {table} SET WorkspaceId = @ws WHERE SessionLogTurnId IN (SELECT t.Id FROM SessionLogTurns t JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = @sid)",
                    ("@ws", DriftedWorkspacePath), ("@sid", sessionId));
            }
        }
    }

    private static void EnsureWorkspaceRow(SqliteConnection connection, string workspaceId)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        using var db = new McpDbContext(options);
        if (db.Workspaces.Any(w => w.WorkspaceId == workspaceId))
            return;
        db.Workspaces.Add(new McpServer.Support.Mcp.Storage.Entities.WorkspaceEntity
        {
            WorkspaceId = workspaceId,
            WorkspacePath = workspaceId,
            Name = "drift-workspace"
        });
        db.SaveChanges();
    }

    private static int CountTurnRows(SqliteConnection connection, string requestId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM SessionLogTurns WHERE RequestId = @rid";
        cmd.Parameters.AddWithValue("@rid", requestId);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ExecuteSql(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value);
        cmd.ExecuteNonQuery();
    }

    private static (SessionLogService Sut, McpDbContext Db) BuildSqliteSut(
        SqliteConnection connection,
        bool useServiceWorkspaceContext)
        => BuildSqliteSut(connection, WorkspacePath, useServiceWorkspaceContext);

    private static (SessionLogService Sut, McpDbContext Db) BuildSqliteSut(
        SqliteConnection connection,
        string workspacePath,
        bool useServiceWorkspaceContext = true)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = workspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            useServiceWorkspaceContext ? workspaceContext : null);
        return (sut, db);
    }

    #endregion

    private SessionLogService BuildSutWithWorkspaceContext(string workspacePath)
    {
        _db.OverrideWorkspaceId(workspacePath);
        var ctx = new WorkspaceContext { WorkspacePath = workspacePath };
        return new SessionLogService(_db, NullLogger<SessionLogService>.Instance, _eventBus, ctx);
    }

    private static (SessionLogService Sut, McpDbContext Db) BuildSqliteSut(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            workspaceContext);
        return (sut, db);
    }

    private static UnifiedSessionLogDto CreateTestDto(string sourceType, string sessionId, string title = "Test Session")
    {
        return new UnifiedSessionLogDto
        {
            SourceType = sourceType,
            SessionId = sessionId,
            Title = title,
            Model = "gpt-4",
            Started = "2026-02-11T10:00:00Z",
            LastUpdated = "2026-02-11T12:00:00Z",
            Status = "completed",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260211T100100Z-entry-001",
                    Timestamp = "2026-02-11T10:01:00Z",
                    QueryText = "How do I configure EF Core?",
                    QueryTitle = "EF Core Config",
                    Response = "Use AddDbContext in Program.cs",
                    Status = "completed"
                }
            ]
        };
    }

    private static UnifiedRequestEntryDto CreateRelationalTurn(string requestId, string response, string tag)
    {
        return new UnifiedRequestEntryDto
        {
            RequestId = requestId,
            Timestamp = "2026-06-10T15:12:35Z",
            QueryTitle = response,
            QueryText = response,
            Response = response,
            Status = "completed",
            Tags = [tag],
            ContextList = [$"docs/{tag}.md"],
            Actions =
            [
                new UnifiedActionDto
                {
                    Description = $"{response} action",
                    Type = "test",
                    Status = "completed",
                    FilePath = $"src/{tag}.cs"
                }
            ],
            DesignDecisions = [$"{response} decision"],
            FilesModified = [$"src/{tag}.cs"],
            Blockers = [$"{response} blocker"]
        };
    }

    private static string BuildSessionId(string agent, string suffix)
    {
        var normalized = new string((suffix ?? string.Empty)
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "session";
        return $"{agent}-20260304T113901Z-{normalized}";
    }
}

