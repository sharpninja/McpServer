using System.Diagnostics;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-001: session-log persist is idempotent on identical actions,
/// session tags persist, replace missing turn is not-found, canceled statuses round-trip.
/// </summary>
public sealed class SessionLogTriageStoreTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\sessionlog-triage-store";
    private readonly McpDbContext _db;
    private readonly SessionLogService _sut;

    /// <summary>Builds an in-memory session-log service.</summary>
    public SessionLogTriageStoreTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTriageStore_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);
        _sut = new SessionLogService(
            _db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath });
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>TEST-MCP-TRIAGESTORE-001: identical actions[] resubmit does not duplicate rows.</summary>
    [Fact]
    public async Task SubmitAsync_IdenticalActions_DoesNotDuplicate()
    {
        var sessionId = "Cursor-20260818T200000Z-identical-actions";
        var dto = CreateSession(sessionId);
        dto.Turns!.First().Actions =
        [
            new UnifiedActionDto
            {
                Order = 1,
                Type = "edit",
                Description = "Edit Program.cs",
                Status = "completed",
                FilePath = "Program.cs",
            },
        ];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        dto.Turns!.First().Actions!.First().Status = "failed";
        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogActions
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(1, stored);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-001: session-level tags persist and return on query.</summary>
    [Fact]
    public async Task SubmitAsync_SessionTags_RoundTrip()
    {
        var sessionId = "Cursor-20260818T200000Z-session-tags";
        var dto = CreateSession(sessionId);
        dto.Tags = ["triage", "cluster"];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var queried = await _sut.QueryAsync(
            new SessionLogQueryRequest { Agent = "Cursor" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var session = Assert.Single(queried.Items, item => item.SessionId == sessionId);
        Assert.NotNull(session.Tags);
        Assert.Contains("triage", session.Tags!);
        Assert.Contains("cluster", session.Tags!);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-001: replace of a missing requestId is not-found, not upsert.</summary>
    [Fact]
    public async Task ReplaceTurnAsync_MissingRequestId_ThrowsNotFound()
    {
        var sessionId = "Cursor-20260818T200000Z-replace-missing";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var missing = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260818T200000Z-missing-turn",
            Status = "completed",
            PlanFile = SessionLogTurnContextValidator.NoneSentinel,
            TodoId = SessionLogTurnContextValidator.NoneSentinel,
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ReplaceTurnAsync("Cursor", sessionId, missing, TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-001: canceled and cancelled persist and re-query.</summary>
    [Theory]
    [InlineData("canceled")]
    [InlineData("cancelled")]
    public async Task SubmitAsync_CanceledStatus_RoundTrips(string status)
    {
        var sessionId = $"Cursor-20260818T200000Z-{status}-status";
        var dto = CreateSession(sessionId);
        dto.Turns!.First().Status = status;

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync("Cursor", sessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(fetched);
        Assert.Equal(status, fetched!.Turns!.First().Status);
    }

    /// <summary>
    /// TEST-MCP-TRIAGESTORE-007: SessionLogService SaveChanges is wrapped in the 5s
    /// storage budget and surfaces backend_unavailable when the save cannot finish.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_HungSaveChanges_FailsFastWithStorageUnavailable()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogHungSave_{Guid.NewGuid():N}")
            .AddInterceptors(new HungSaveChangesInterceptor())
            .Options;
        await using var db = new McpDbContext(options);
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(WorkspacePath);
        var sut = new SessionLogService(
            db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = WorkspacePath });

        var clock = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() =>
            sut.SubmitAsync(CreateSession("Cursor-20260818T220000Z-hung-save"), cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        clock.Stop();

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(8), $"SaveChanges budget took {clock.Elapsed}.");
        var classified = McpErrorClassifier.Classify(ex);
        Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
        Assert.True(classified.Retryable);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-001: superseded persist with omitted context writes None sentinels and canceled.</summary>
    [Fact]
    public async Task UpsertTurnAsync_OmittedPlanFileTodoId_WritesNoneAndCanceled()
    {
        var sessionId = "Cursor-20260818T200000Z-supersede-none";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var hookTurn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260818T200000Z-hook-open",
            Status = "canceled",
            Response = "Superseded by req-20260818T200001Z-new",
        };

        await _sut.UpsertTurnAsync("Cursor", sessionId, hookTurn, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync("Cursor", sessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = Assert.Single(fetched!.Turns!, item => item.RequestId == hookTurn.RequestId);
        Assert.Equal("canceled", turn.Status);
        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, turn.PlanFile);
        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, turn.TodoId);
    }

    /// <summary>Delays SaveChanges until the storage budget cancels the token.</summary>
    private sealed class HungSaveChangesInterceptor : SaveChangesInterceptor
    {
        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = sessionId,
            Title = "Triage store",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260818T200000Z-entry-001",
                    Timestamp = "2026-08-18T20:00:00Z",
                    QueryText = "triage store",
                    Status = "in_progress",
                    PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                    TodoId = SessionLogTurnContextValidator.NoneSentinel,
                },
            ],
        };
    }
}
