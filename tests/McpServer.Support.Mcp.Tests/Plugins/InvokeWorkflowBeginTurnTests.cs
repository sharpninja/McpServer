using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Plugins;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-006 / TEST-MCP-SESSIONLOG-006:
/// Slice 8 persist-path tests for workflow.sessionlog.beginTurn planFile/todoId.
/// These call the shipped <see cref="SessionLogService"/> persist/merge path.
/// </summary>
public sealed class InvokeWorkflowBeginTurnTests : IDisposable
{
    private const string Agent = "Cursor";
    private const string WorkspacePath = @"E:\tests\plugin-begin-turn";
    private readonly McpDbContext _db;
    private readonly SessionLogService _sut;

    /// <summary>Creates an isolated in-memory persist fixture.</summary>
    public InvokeWorkflowBeginTurnTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase("plugin-begin-" + Guid.NewGuid().ToString("N"))
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

    /// <summary>Missing first persist of a new requestId is rejected and inserts no turn.</summary>
    [Fact]
    public async Task Invoke_WorkflowBeginTurn_MissingFields_FailsValidation()
    {
        var sessionId = "Cursor-20260304T113901Z-plugin-miss";
        await _sut.OpenSessionAsync(Agent, sessionId, "t", "m", TestContext.Current.CancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertTurnAsync(
            Agent,
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113901Z-plugin-miss",
                Status = "in_progress",
            },
            TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(0, await _db.SessionLogTurns.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>First persist with no plan-map values stores the None sentinel pair.</summary>
    [Fact]
    public async Task Invoke_WorkflowBeginTurn_FirstTurn_SendsNoneWhenNoPlanMap()
    {
        var sessionId = "Cursor-20260304T113901Z-plugin-none";
        await _sut.OpenSessionAsync(Agent, sessionId, "t", "m", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync(
            Agent,
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113901Z-plugin-none",
                Status = "in_progress",
                PlanFile = "None",
                TodoId = "None",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("None", stored.PlanFile);
        Assert.Equal("None", stored.TodoId);
    }

    /// <summary>First persist with mapped plan/todo stores those exact values.</summary>
    [Fact]
    public async Task Invoke_WorkflowBeginTurn_FirstTurn_SendsMappedPlanAndTodo()
    {
        var sessionId = "Cursor-20260304T113901Z-plugin-map";
        await _sut.OpenSessionAsync(Agent, sessionId, "t", "m", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync(
            Agent,
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = "req-20260304T113901Z-plugin-map",
                Status = "in_progress",
                PlanFile = "docs/plans/foo.md",
                TodoId = "MCP-SESSIONLOG-002",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("docs/plans/foo.md", stored.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", stored.TodoId);
    }

    /// <summary>Reopen omit does not overwrite stored planFile/todoId.</summary>
    [Fact]
    public async Task Invoke_WorkflowBeginTurn_Reopen_OmitsFieldsAndDoesNotOverwrite()
    {
        var sessionId = "Cursor-20260304T113901Z-plugin-reopen";
        var requestId = "req-20260304T113901Z-plugin-reopen";
        await _sut.OpenSessionAsync(Agent, sessionId, "t", "m", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpsertTurnAsync(
            Agent,
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = requestId,
                Status = "in_progress",
                PlanFile = "docs/plans/foo.md",
                TodoId = "MCP-SESSIONLOG-002",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sut.UpsertTurnAsync(
            Agent,
            sessionId,
            new UnifiedRequestEntryDto
            {
                RequestId = requestId,
                Status = "in_progress",
                Response = "reopened",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("docs/plans/foo.md", stored.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", stored.TodoId);
        Assert.Equal("reopened", stored.Response);
    }
}
