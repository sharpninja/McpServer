using System.Linq;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
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
    public async Task WhenUpsertingWithRemovedEntryThenStaleEntryIsDeleted()
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

        // Submit with only the first entry — second should be removed
        var dto2 = CreateTestDto("Cursor", BuildSessionId("Cursor", "keyed-remove"));
        dto2.TurnCount = 1;
        await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        Assert.Equal(1, await _db.SessionLogTurns.CountAsync(e => e.SessionLogId == id).ConfigureAwait(true));
        var remaining = await _db.SessionLogTurns.FirstAsync(e => e.SessionLogId == id).ConfigureAwait(true);
        Assert.Equal("req-20260211T100100Z-entry-001", remaining.RequestId);
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
            Status = "completed"
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
            Status = "completed"
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

    private SessionLogService BuildSutWithWorkspaceContext(string workspacePath)
    {
        _db.OverrideWorkspaceId(workspacePath);
        var ctx = new WorkspaceContext { WorkspacePath = workspacePath };
        return new SessionLogService(_db, NullLogger<SessionLogService>.Instance, _eventBus, ctx);
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

