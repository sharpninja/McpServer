using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// AC-FR-MCP-SESSIONLOGCTX-001-001 / 002 / 003 / 005 and AC-TR-MCP-SESSIONLOG-006-002:
/// persist, merge, replace, and query of planFile/todoId through SessionLogService.
/// </summary>
public sealed class SessionLogServiceTurnContextTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\sessionlog-turn-context";
    private readonly McpDbContext _db;
    private readonly SessionLogService _sut;

    /// <summary>Creates an isolated in-memory service fixture.</summary>
    public SessionLogServiceTurnContextTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogTurnCtx_{Guid.NewGuid()}")
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

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: missing planFile inserts no turn.</summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurnWithoutPlanFile_ThrowsAndDoesNotInsert()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-miss";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertTurnAsync(
            "Cursor",
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113901Z-new",
                TodoId = "None",
            },
            TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(1, await _db.SessionLogTurns.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-003: import with omitted fields extracts then persists.</summary>
    [Fact]
    public async Task SubmitAsync_ImportMissingFields_ExtractsFromTurnText()
    {
        var dto = CreateSession("Cursor-20260304T113901Z-ctx-imp");
        dto.Turns!.First().PlanFile = null;
        dto.Turns!.First().TodoId = null;
        dto.Turns!.First().QueryText = "working MCP-IMPORT-001 on docs/plans/imported.md";
        await _sut.SubmitAsync(dto, sourceFilePath: @"E:\imports\session.json", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("docs/plans/imported.md", stored.PlanFile);
        Assert.Equal("MCP-IMPORT-001", stored.TodoId);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: interactive submit without fields throws.</summary>
    [Fact]
    public async Task SubmitAsync_NewTurnMissingFields_Throws()
    {
        var dto = CreateSession("Cursor-20260304T113901Z-ctx-sub");
        dto.Turns!.First().PlanFile = null;
        dto.Turns!.First().TodoId = null;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: empty-string planFile inserts no turn row.</summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurnEmptyPlanFile_ThrowsAndDoesNotInsert()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-emptyplan";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertTurnAsync(
            "Cursor",
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113904Z-emptyplan",
                PlanFile = string.Empty,
                TodoId = "MCP-SESSIONLOG-002",
            },
            TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(1, await _db.SessionLogTurns.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: omitted todoId with a valid planFile inserts no turn row.</summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurnNullTodoId_ThrowsAndDoesNotInsert()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-nulltodo";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertTurnAsync(
            "Cursor",
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113905Z-nulltodo",
                PlanFile = "docs/plans/foo.md",
                TodoId = null,
            },
            TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(1, await _db.SessionLogTurns.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: empty-string todoId with a valid planFile inserts no turn row.</summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurnEmptyTodoId_ThrowsAndDoesNotInsert()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-emptytodo";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertTurnAsync(
            "Cursor",
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113906Z-emptytodo",
                PlanFile = "docs/plans/foo.md",
                TodoId = string.Empty,
            },
            TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(1, await _db.SessionLogTurns.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: whitespace todoId with a valid planFile inserts no turn row.</summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurnWhitespaceTodoId_ThrowsAndDoesNotInsert()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-wstodo";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertTurnAsync(
            "Cursor",
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113907Z-wstodo",
                PlanFile = "docs/plans/foo.md",
                TodoId = "  ",
            },
            TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(1, await _db.SessionLogTurns.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: canceled supersession may omit the pair and stores exact None.</summary>
    [Fact]
    public async Task UpsertTurnAsync_CanceledOmission_StoresExactNone()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-canceled";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync("Cursor", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = "req-20260304T113908Z-canceled",
            Status = "canceled",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(t => t.RequestId == "req-20260304T113908Z-canceled", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("None", stored.PlanFile);
        Assert.Equal("None", stored.TodoId);
        Assert.Equal("canceled", stored.Status);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: cancelled supersession may omit the pair and stores exact None.</summary>
    [Fact]
    public async Task UpsertTurnAsync_CancelledOmission_StoresExactNone()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-cancelled";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync("Cursor", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = "req-20260304T113909Z-cancelled",
            Status = "cancelled",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(t => t.RequestId == "req-20260304T113909Z-cancelled", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("None", stored.PlanFile);
        Assert.Equal("None", stored.TodoId);
        Assert.Equal("cancelled", stored.Status);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: interactive submit with an empty planFile and a valid todoId inserts no turn.</summary>
    [Fact]
    public async Task SubmitAsync_EmptyPlanFileWithValidTodo_ThrowsAndInsertsNoTurn()
    {
        var dto = CreateSession("Cursor-20260304T113901Z-ctx-subempty");
        dto.Turns!.First().PlanFile = string.Empty;
        dto.Turns!.First().TodoId = "MCP-SESSIONLOG-002";
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Equal(0, await _db.SessionLogTurns.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-003: interactive submit with a valid planFile and omitted todoId inserts no turn.</summary>
    [Fact]
    public async Task SubmitAsync_ValidPlanOmittedTodo_ThrowsAndInsertsNoTurn()
    {
        var dto = CreateSession("Cursor-20260304T113901Z-ctx-subtodo");
        dto.Turns!.First().PlanFile = "docs/plans/foo.md";
        dto.Turns!.First().TodoId = null;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Equal(0, await _db.SessionLogTurns.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-002: None/None persists as None.</summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurnWithNoneNone_PersistsNone()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-none";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync("Cursor", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = "req-20260304T113902Z-none",
            PlanFile = "None",
            TodoId = "None",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(t => t.RequestId == "req-20260304T113902Z-none", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("None", stored.PlanFile);
        Assert.Equal("None", stored.TodoId);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-001: valid pair round-trips on get.</summary>
    [Fact]
    public async Task UpsertTurnAsync_NewTurnWithValidValues_RoundTripsOnGet()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-rt";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync("Cursor", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = "req-20260304T113903Z-rt",
            PlanFile = "docs/plans/foo.md",
            TodoId = "MCP-SESSIONLOG-002",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var got = await _sut.GetAsync("Cursor", sessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = got!.Turns!.Single(t => t.RequestId == "req-20260304T113903Z-rt");
        Assert.Equal("docs/plans/foo.md", turn.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", turn.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-002: additive omit preserves stored values.</summary>
    [Fact]
    public async Task UpsertTurnAsync_ExistingTurnOmittingFields_PreservesStoredValues()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-omit";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var requestId = "req-20260304T113901Z-seed";
        await _sut.UpsertTurnAsync("Cursor", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = requestId,
            Response = "later",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(t => t.RequestId == requestId, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("docs/plans/seed.md", stored.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", stored.TodoId);
        Assert.Equal("later", stored.Response);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-002: supplied todoId updates.</summary>
    [Fact]
    public async Task UpsertTurnAsync_ExistingTurnSupplyingNewTodo_UpdatesTodoId()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-upd";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync("Cursor", sessionId, new UnifiedRequestEntryDto
        {
            RequestId = "req-20260304T113901Z-seed",
            TodoId = "PLAN-OTHER-001",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(t => t.RequestId == "req-20260304T113901Z-seed", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("PLAN-OTHER-001", stored.TodoId);
        Assert.Equal("docs/plans/seed.md", stored.PlanFile);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-002: replace omit throws.</summary>
    [Fact]
    public async Task ReplaceTurnAsync_OmittingFields_Throws()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-rep";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ReplaceTurnAsync(
            "Cursor",
            sessionId,
            new UnifiedRequestEntryDto { RequestId = "req-20260304T113901Z-seed", Response = "x" },
            TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-005: text search hits planFile.</summary>
    [Fact]
    public async Task QueryAsync_TextMatchesPlanFileOnly_ReturnsSession()
    {
        await _sut.SubmitAsync(CreateSession("Cursor-20260304T113901Z-ctx-txt"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = await _sut.QueryAsync(new SessionLogQueryRequest { Text = "docs/plans/seed.md" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, result.TotalCount);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-005: todoId filter is exact.</summary>
    [Fact]
    public async Task QueryAsync_FilterByTodoId_ReturnsOnlyMatches()
    {
        await _sut.SubmitAsync(CreateSession("Cursor-20260304T113901Z-ctx-f1"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = await _sut.QueryAsync(new SessionLogQueryRequest { TodoId = "MCP-SESSIONLOG-002" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, result.TotalCount);
        var miss = await _sut.QueryAsync(new SessionLogQueryRequest { TodoId = "PLAN-MISS-001" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(0, miss.TotalCount);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-005: exact planFile filter.</summary>
    [Fact]
    public async Task QueryAsync_FilterByExactPlanFile_ReturnsOnlyMatches()
    {
        await _sut.SubmitAsync(CreateSession("Cursor-20260304T113901Z-ctx-pf"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = await _sut.QueryAsync(new SessionLogQueryRequest { PlanFile = "docs/plans/seed.md" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, result.TotalCount);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-005: ~/ query expands then exact-matches.</summary>
    [Fact]
    public async Task QueryAsync_HomeRelativeFilter_ExpandsThenExactMatches()
    {
        var sessionId = "Cursor-20260304T113901Z-ctx-home";
        var dto = CreateSession(sessionId);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        dto.Turns!.First().PlanFile = Path.Combine(home, "plans", "live.md").Replace('\\', '/');
        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = await _sut.QueryAsync(new SessionLogQueryRequest { PlanFile = "~/plans/live.md" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, result.TotalCount);
    }

    private static UnifiedSessionLogDto CreateSession(string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Cursor",
            SessionId = sessionId,
            Title = "ctx",
            Model = "gpt-4",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260304T113901Z-seed",
                    QueryText = "seed",
                    Status = "in_progress",
                    PlanFile = "docs/plans/seed.md",
                    TodoId = "MCP-SESSIONLOG-002",
                }
            ]
        };
    }
}
