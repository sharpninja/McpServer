using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013: Unit tests for SessionLogService submit and query (MVP-SUPPORT-011).</summary>
public sealed class SessionLogServiceTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly SessionLogService _sut;

    public SessionLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTests_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _sut = new SessionLogService(_db, NullLogger<SessionLogService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task WhenSubmittingNewSessionThenSessionIsCreated()
    {
        var dto = CreateTestDto("Cursor", "session-1");

        var id = await _sut.SubmitAsync(dto).ConfigureAwait(true);

        Assert.True(id > 0);
        var stored = await _db.SessionLogs.Include(s => s.Entries).FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal("Cursor", stored.SourceType);
        Assert.Equal("session-1", stored.SessionId);
        Assert.Equal("Test Session", stored.Title);
        Assert.Single(stored.Entries);
    }

    [Fact]
    public async Task WhenSubmittingSameSessionTwiceThenSessionIsUpdated()
    {
        var dto1 = CreateTestDto("Cursor", "session-dup", title: "Original");
        await _sut.SubmitAsync(dto1).ConfigureAwait(true);

        var dto2 = CreateTestDto("Cursor", "session-dup", title: "Updated");
        dto2.Entries![0].QueryText = "Updated query";
        var id = await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        var stored = await _db.SessionLogs.Include(s => s.Entries).FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal("Updated", stored.Title);
        Assert.Single(stored.Entries);
        Assert.Equal("Updated query", stored.Entries.First().QueryText);
    }

    [Fact]
    public async Task WhenSubmittingWithCopilotStatisticsThenStatisticsArePersisted()
    {
        var dto = CreateTestDto("Copilot", "stats-session");
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
        var dto = CreateTestDto("Cursor", "ws-session");
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
        var dto = CreateTestDto("Cursor", "multi-valued");
        dto.Entries![0].Tags = ["csharp", "ef-core"];
        dto.Entries[0].ContextList = ["src/Program.cs", "docs/README.md"];

        var id = await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var entry = await _db.SessionLogEntries
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
        await _sut.SubmitAsync(CreateTestDto("Cursor", "q1")).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("Copilot", "q2")).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest()).ConfigureAwait(true);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task WhenQueryingByAgentThenOnlyMatchingSessionsAreReturned()
    {
        await _sut.SubmitAsync(CreateTestDto("Cursor", "agent-1")).ConfigureAwait(true);
        await _sut.SubmitAsync(CreateTestDto("Copilot", "agent-2")).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "Cursor" }).ConfigureAwait(true);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Cursor", item.SourceType));
    }

    [Fact]
    public async Task WhenQueryingByDateRangeThenOnlyMatchingSessionsAreReturned()
    {
        var early = CreateTestDto("Cursor", "early");
        early.Started = "2026-01-01T00:00:00Z";
        early.LastUpdated = "2026-01-01T12:00:00Z";
        await _sut.SubmitAsync(early).ConfigureAwait(true);

        var late = CreateTestDto("Cursor", "late");
        late.Started = "2026-02-01T00:00:00Z";
        late.LastUpdated = "2026-02-01T12:00:00Z";
        await _sut.SubmitAsync(late).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest
        {
            From = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero)
        }).ConfigureAwait(true);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("late", result.Items[0].SessionId);
    }

    [Fact]
    public async Task WhenQueryingWithLimitAndOffsetThenPaginationIsApplied()
    {
        for (var i = 0; i < 5; i++)
        {
            var dto = CreateTestDto("Cursor", $"page-{i}");
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
        await _sut.SubmitAsync(CreateTestDto("Cursor", "clamp")).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Limit = 9999 }).ConfigureAwait(true);

        Assert.Equal(1000, result.Limit);
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
    public async Task WhenQueryResultMappedThenDtoIncludesWorkspaceAndStatistics()
    {
        var dto = CreateTestDto("Copilot", "round-trip");
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
    public async Task WhenUpsertingWithNewEntryThenEntryIsAddedWithoutRemovingExisting()
    {
        var dto1 = CreateTestDto("Cursor", "keyed-add");
        await _sut.SubmitAsync(dto1).ConfigureAwait(true);

        // Submit again with original entry plus a new one
        var dto2 = CreateTestDto("Cursor", "keyed-add");
        dto2.Entries!.Add(new UnifiedRequestEntryDto
        {
            RequestId = "req-keyed-add-2",
            QueryText = "New entry",
            Status = "completed"
        });
        dto2.EntryCount = 2;
        var id = await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        var stored = await _db.SessionLogs.Include(s => s.Entries).FirstAsync(s => s.Id == id).ConfigureAwait(true);
        Assert.Equal(2, stored.Entries.Count);
    }

    [Fact]
    public async Task WhenUpsertingExistingEntryThenEntryIsUpdatedInPlace()
    {
        var dto1 = CreateTestDto("Cursor", "keyed-update");
        var id = await _sut.SubmitAsync(dto1).ConfigureAwait(true);

        var originalEntryId = (await _db.SessionLogEntries.FirstAsync(e => e.SessionLogId == id).ConfigureAwait(true)).Id;

        // Submit with same RequestId but different content
        var dto2 = CreateTestDto("Cursor", "keyed-update");
        dto2.Entries![0].QueryText = "Updated query text";
        dto2.Entries[0].Response = "Updated response";
        await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        var updatedEntry = await _db.SessionLogEntries.FirstAsync(e => e.SessionLogId == id).ConfigureAwait(true);
        Assert.Equal(originalEntryId, updatedEntry.Id); // Same row, updated in place
        Assert.Equal("Updated query text", updatedEntry.QueryText);
        Assert.Equal("Updated response", updatedEntry.Response);
    }

    [Fact]
    public async Task WhenUpsertingWithRemovedEntryThenStaleEntryIsDeleted()
    {
        var dto1 = CreateTestDto("Cursor", "keyed-remove");
        dto1.Entries!.Add(new UnifiedRequestEntryDto
        {
            RequestId = "req-keyed-remove-2",
            QueryText = "Will be removed",
            Status = "completed"
        });
        dto1.EntryCount = 2;
        var id = await _sut.SubmitAsync(dto1).ConfigureAwait(true);
        Assert.Equal(2, await _db.SessionLogEntries.CountAsync(e => e.SessionLogId == id).ConfigureAwait(true));

        // Submit with only the first entry — second should be removed
        var dto2 = CreateTestDto("Cursor", "keyed-remove");
        dto2.EntryCount = 1;
        await _sut.SubmitAsync(dto2).ConfigureAwait(true);

        Assert.Equal(1, await _db.SessionLogEntries.CountAsync(e => e.SessionLogId == id).ConfigureAwait(true));
        var remaining = await _db.SessionLogEntries.FirstAsync(e => e.SessionLogId == id).ConfigureAwait(true);
        Assert.Equal("req-keyed-remove-1", remaining.RequestId);
    }

    [Fact]
    public async Task WhenAppendingDialogItemsThenItemsAreAdded()
    {
        var dto = CreateTestDto("Cursor", "dialog-append");
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var items = new List<ProcessingDialogItemDto>
        {
            new() { Timestamp = "2026-02-12T10:00:00Z", Role = "model", Content = "Analyzing request", Category = "reasoning" },
            new() { Timestamp = "2026-02-12T10:00:01Z", Role = "tool", Content = "get_file(Program.cs)", Category = "tool_call" }
        };

        var count = await _sut.AppendProcessingDialogAsync("Cursor", "dialog-append", "req-dialog-append-1", items).ConfigureAwait(true);

        Assert.Equal(2, count);
        var entry = await _db.SessionLogEntries
            .Include(e => e.ProcessingDialog)
            .FirstAsync(e => e.RequestId == "req-dialog-append-1")
            .ConfigureAwait(true);
        Assert.Equal(2, entry.ProcessingDialog.Count);
        var first = entry.ProcessingDialog.OrderBy(p => p.Ordinal).First();
        Assert.Equal("model", first.Role);
        Assert.Equal("Analyzing request", first.Content);
        Assert.Equal("reasoning", first.Category);
    }

    [Fact]
    public async Task WhenAppendingDialogMultipleTimesThenOrdinalsAreContinuous()
    {
        var dto = CreateTestDto("Cursor", "dialog-multi");
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        await _sut.AppendProcessingDialogAsync("Cursor", "dialog-multi", "req-dialog-multi-1",
            [new ProcessingDialogItemDto { Role = "model", Content = "First batch" }]).ConfigureAwait(true);

        var count = await _sut.AppendProcessingDialogAsync("Cursor", "dialog-multi", "req-dialog-multi-1",
            [new ProcessingDialogItemDto { Role = "model", Content = "Second batch" }]).ConfigureAwait(true);

        Assert.Equal(2, count);
        var entry = await _db.SessionLogEntries
            .Include(e => e.ProcessingDialog)
            .FirstAsync(e => e.RequestId == "req-dialog-multi-1")
            .ConfigureAwait(true);
        var ordinals = entry.ProcessingDialog.OrderBy(p => p.Ordinal).Select(p => p.Ordinal).ToList();
        Assert.Equal([0, 1], ordinals);
    }

    [Fact]
    public async Task WhenAppendingDialogToNonexistentEntryThenThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AppendProcessingDialogAsync("Cursor", "nonexistent", "req-1",
                [new ProcessingDialogItemDto { Role = "model", Content = "test" }])).ConfigureAwait(true);
    }

    [Fact]
    public async Task WhenQueryingSessionWithDialogThenDialogIsIncludedInDto()
    {
        var dto = CreateTestDto("Copilot", "dialog-query");
        dto.Entries![0].ProcessingDialog =
        [
            new ProcessingDialogItemDto { Timestamp = "2026-02-12T10:00:00Z", Role = "model", Content = "Thinking...", Category = "reasoning" }
        ];
        await _sut.SubmitAsync(dto).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Agent = "Copilot" }).ConfigureAwait(true);

        var entry = result.Items.First(i => i.SessionId == "dialog-query").Entries!.First();
        Assert.NotNull(entry.ProcessingDialog);
        Assert.Single(entry.ProcessingDialog!);
        Assert.Equal("model", entry.ProcessingDialog![0].Role);
        Assert.Equal("Thinking...", entry.ProcessingDialog[0].Content);
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
            EntryCount = 1,
            Entries =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = $"req-{sessionId}-1",
                    Timestamp = "2026-02-11T10:01:00Z",
                    QueryText = "How do I configure EF Core?",
                    QueryTitle = "EF Core Config",
                    Response = "Use AddDbContext in Program.cs",
                    Status = "completed"
                }
            ]
        };
    }
}
