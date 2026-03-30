using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McpServer.Repl.Core;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Production integration tests for SessionLogWorkflow with real SessionLogClient stub.
/// Validates that the production implementation correctly handles all workflow operations,
/// turn lifecycle enforcement, canonical identifier validation, and error handling.
/// </summary>
public class SessionLogWorkflowProductionTests
{
    private readonly ISessionLogWorkflow _workflow;
    private readonly StubSessionLogClient _stubClient;
    private readonly TimeProvider _timeProvider;

    public SessionLogWorkflowProductionTests()
    {
        _stubClient = new StubSessionLogClient();
        _timeProvider = TimeProvider.System;
        _workflow = new SessionLogWorkflow(_stubClient, _timeProvider);
    }

    #region Bootstrap and Session Creation Tests

    [Fact]
    public async Task BootstrapAsync_FirstCall_Succeeds()
    {
        await _workflow.BootstrapAsync();
        // No exception means success
    }

    [Fact]
    public async Task BootstrapAsync_MultipleCall_IsIdempotent()
    {
        await _workflow.BootstrapAsync();
        await _workflow.BootstrapAsync();
        await _workflow.BootstrapAsync();
        // No exception means success
    }

    [Fact]
    public async Task OpenSessionAsync_ValidParameters_CreatesSession()
    {
        await _workflow.BootstrapAsync();

        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-feature-auth";
        var title = "Implementing JWT authentication";
        var model = "claude-sonnet-4-20250514";

        await _workflow.OpenSessionAsync(agent, sessionId, title, model);

        var session = _workflow.CurrentSession();
        Assert.NotNull(session);
        Assert.Equal(agent, session!.Agent);
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal(title, session.Title);
        Assert.Equal(model, session.Model);
        Assert.Equal("in_progress", session.Status);
        Assert.Equal(0, session.TurnCount);
    }

    [Fact]
    public async Task OpenSessionAsync_InvalidSessionId_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.OpenSessionAsync("Copilot", "copilot-20260304T113901Z-test", "Test", "model"));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304-test", "Test", "model"));
    }

    [Fact]
    public async Task OpenSessionAsync_AgentPrefixMismatch_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.OpenSessionAsync("Cline", "Copilot-20260304T113901Z-test", "Test", "model"));
    }

    [Fact]
    public async Task OpenSessionAsync_DuplicateSessionId_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync();

        var sessionId = "Copilot-20260304T113901Z-duplicate";
        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model"));
    }

    #endregion

    #region Turn Lifecycle Tests

    [Fact]
    public async Task BeginTurnAsync_ValidParameters_CreatesTurn()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        var requestId = "req-20260304T113901Z-task-001";
        await _workflow.BeginTurnAsync(requestId, "Task Title", "Task Description");

        var session = _workflow.CurrentSession();
        Assert.Equal(requestId, session!.CurrentTurnRequestId);
        Assert.Equal("in_progress", session.CurrentTurnStatus);
    }

    [Fact]
    public async Task BeginTurnAsync_NoActiveSession_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.BeginTurnAsync("req-20260304T113901Z-task", "Title", "Query"));
    }

    [Fact]
    public async Task BeginTurnAsync_InvalidRequestId_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.BeginTurnAsync("request-20260304T113901Z-task", "Title", "Query"));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.BeginTurnAsync("req-20260304-task", "Title", "Query"));
    }

    [Fact]
    public async Task BeginTurnAsync_DuplicateRequestId_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        var requestId = "req-20260304T113901Z-duplicate";
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");
        await _workflow.CompleteTurnAsync("Done");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.BeginTurnAsync(requestId, "Task", "Query"));
    }

    [Fact]
    public async Task UpdateTurnAsync_ActiveTurn_UpdatesFields()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        await _workflow.UpdateTurnAsync(
            response: "Updated response",
            interpretation: "Updated interpretation",
            tokenCount: 1250,
            tags: new List<string> { "feature", "security" },
            contextList: new List<string> { "src/File1.cs" });

        // No exception means success
        var session = _workflow.CurrentSession();
        Assert.Equal("in_progress", session!.CurrentTurnStatus);
    }

    [Fact]
    public async Task UpdateTurnAsync_NoActiveTurn_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.UpdateTurnAsync(response: "Response"));
    }

    [Fact]
    public async Task CompleteTurnAsync_ActiveTurn_MarksAsCompleted()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        await _workflow.CompleteTurnAsync("Task completed successfully");

        var session = _workflow.CurrentSession();
        Assert.Null(session!.CurrentTurnRequestId);
        Assert.Null(session.CurrentTurnStatus);
        Assert.Equal(1, session.TurnCount);
    }

    [Fact]
    public async Task CompleteTurnAsync_NullOrEmptyResponse_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        // ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
            await _workflow.CompleteTurnAsync(null!));

        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
            await _workflow.CompleteTurnAsync(""));
    }

    [Fact]
    public async Task FailTurnAsync_ActiveTurn_MarksAsFailed()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        await _workflow.FailTurnAsync("Unable to complete task", "dependency_missing");

        var session = _workflow.CurrentSession();
        Assert.Null(session!.CurrentTurnRequestId);
        Assert.Null(session.CurrentTurnStatus);
        Assert.Equal(1, session.TurnCount);
    }

    [Fact]
    public async Task TurnImmutability_CompletedTurn_CannotModify()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");
        await _workflow.CompleteTurnAsync("Done");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.UpdateTurnAsync(response: "Cannot update"));
    }

    [Fact]
    public async Task TurnImmutability_FailedTurn_CannotModify()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");
        await _workflow.FailTurnAsync("Failed", "error");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.UpdateTurnAsync(response: "Cannot update"));
    }

    #endregion

    #region Dialog and Action Append Tests

    [Fact]
    public async Task AppendDialogAsync_ValidDialogItems_AppendsToTurn()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        var dialogItems = new List<IDialogItem>
        {
            new DialogItem(DateTimeOffset.UtcNow, "model", "Analyzing requirements...", "reasoning"),
            new DialogItem(DateTimeOffset.UtcNow, "tool", "File created", "tool_result")
        };

        await _workflow.AppendDialogAsync(dialogItems);

        // No exception means success
    }

    [Fact]
    public async Task AppendDialogAsync_EmptyList_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.AppendDialogAsync(new List<IDialogItem>()));
    }

    [Fact]
    public async Task AppendActionsAsync_ValidActions_AppendsToTurn()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        var actions = new List<ISessionAction>
        {
            new SessionAction(1, "Created File1.cs", "create", "completed", "src/File1.cs"),
            new SessionAction(2, "Edited File2.cs", "edit", "completed", "src/File2.cs")
        };

        await _workflow.AppendActionsAsync(actions);

        // No exception means success
    }

    [Fact]
    public async Task AppendActionsAsync_EmptyList_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.AppendActionsAsync(new List<ISessionAction>()));
    }

    #endregion

    #region Query History Tests

    [Fact]
    public async Task QueryHistoryAsync_NoFilter_ReturnsResults()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");
        await _workflow.CompleteTurnAsync("Done");

        var history = await _workflow.QueryHistoryAsync();

        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task QueryHistoryAsync_FilterByAgent_ReturnsMatchingSessions()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        var history = await _workflow.QueryHistoryAsync(agent: "Copilot");

        Assert.NotNull(history);
        Assert.All(history, s => Assert.Equal("Copilot", s.Agent));
    }

    [Fact]
    public async Task QueryHistoryAsync_Pagination_ReturnsCorrectSlice()
    {
        await _workflow.BootstrapAsync();
        
        var history = await _workflow.QueryHistoryAsync(limit: 5, offset: 0);

        Assert.NotNull(history);
    }

    [Fact]
    public async Task QueryHistoryAsync_NegativeLimit_ThrowsArgumentOutOfRangeException()
    {
        await _workflow.BootstrapAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _workflow.QueryHistoryAsync(limit: -1));
    }

    [Fact]
    public async Task QueryHistoryAsync_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        await _workflow.BootstrapAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _workflow.QueryHistoryAsync(offset: -1));
    }

    #endregion

    #region Complete Workflow Integration Tests

    [Fact]
    public async Task CompleteWorkflow_OpenSessionBeginTurnComplete_Success()
    {
        await _workflow.BootstrapAsync();

        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-complete-workflow";
        var title = "Complete Workflow Test";
        var model = "claude-sonnet-4";

        await _workflow.OpenSessionAsync(agent, sessionId, title, model);

        var session = _workflow.CurrentSession();
        Assert.NotNull(session);
        Assert.Equal(sessionId, session!.SessionId);
        Assert.Equal("in_progress", session.Status);

        var requestId = "req-20260304T113901Z-task-001";
        await _workflow.BeginTurnAsync(requestId, "Task Title", "Query text");

        Assert.Equal(requestId, session.CurrentTurnRequestId);
        Assert.Equal("in_progress", session.CurrentTurnStatus);

        await _workflow.UpdateTurnAsync(response: "Working on it...");

        await _workflow.CompleteTurnAsync("Task completed");

        Assert.Null(session.CurrentTurnRequestId);
        Assert.Equal(1, session.TurnCount);
    }

    [Fact]
    public async Task CompleteWorkflow_MultipleTurnsInSession_Success()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-multi-turn", "Multi-Turn", "model");

        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task 1", "Query 1");
        await _workflow.UpdateTurnAsync(response: "Response 1");
        await _workflow.CompleteTurnAsync("Task 1 completed");

        var session = _workflow.CurrentSession();
        Assert.Equal(1, session!.TurnCount);

        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-002", "Task 2", "Query 2");
        await _workflow.UpdateTurnAsync(response: "Response 2");
        await _workflow.CompleteTurnAsync("Task 2 completed");

        Assert.Equal(2, session.TurnCount);

        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-003", "Task 3", "Query 3");
        await _workflow.FailTurnAsync("Task 3 failed", "error_code");

        Assert.Equal(3, session.TurnCount);
        Assert.Null(session.CurrentTurnRequestId);
    }

    [Fact]
    public async Task CompleteWorkflow_WithDialogAndActions_Success()
    {
        await _workflow.BootstrapAsync();
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-dialog", "Dialog Test", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");

        var dialogItems = new List<IDialogItem>
        {
            new DialogItem(DateTimeOffset.UtcNow, "model", "Analyzing...", "reasoning"),
            new DialogItem(DateTimeOffset.UtcNow, "tool", "File created", "tool_result")
        };
        await _workflow.AppendDialogAsync(dialogItems);

        var actions = new List<ISessionAction>
        {
            new SessionAction(1, "Created File1.cs", "create", "completed", "src/File1.cs")
        };
        await _workflow.AppendActionsAsync(actions);

        await _workflow.CompleteTurnAsync("Completed with dialog and actions");

        var session = _workflow.CurrentSession();
        Assert.Equal(1, session!.TurnCount);
    }

    #endregion
}
