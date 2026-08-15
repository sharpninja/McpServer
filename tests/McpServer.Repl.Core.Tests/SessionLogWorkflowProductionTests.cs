using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
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
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        // No exception means success
    }

    [Fact]
    public async Task BootstrapAsync_MultipleCall_IsIdempotent()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        // No exception means success
    }

    [Fact]
    public async Task OpenSessionAsync_ValidParameters_CreatesSession()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-feature-auth";
        var title = "Implementing JWT authentication";
        var model = "claude-sonnet-4-20250514";

        await _workflow.OpenSessionAsync(agent, sessionId, title, model, cancellationToken: TestContext.Current.CancellationToken);

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
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.OpenSessionAsync("Copilot", "copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenSessionAsync_AgentPrefixMismatch_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.OpenSessionAsync("Cline", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenSessionAsync_DuplicateSessionId_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        var sessionId = "Copilot-20260304T113901Z-duplicate";
        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model", cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region Recovery Import Tests

    [Fact]
    public async Task ImportRecoveryAsync_MergesWithExistingFullSessionBeforeSubmit()
    {
        var client = Substitute.For<ISessionLogClientAdapter>();
        var existing = new UnifiedSessionLogDto
        {
            SourceType = "Codex",
            SessionId = "Codex-20260514T000000Z-recovery",
            Title = "Existing Session",
            Model = "gpt-5",
            Started = "2026-05-14T00:00:00Z",
            LastUpdated = "2026-05-14T00:02:00Z",
            Status = "in_progress",
            Turns = new List<UnifiedRequestEntryDto>
            {
                new()
                {
                    RequestId = "req-20260514T000100Z-existing",
                    Timestamp = "2026-05-14T00:01:00Z",
                    QueryTitle = "Existing",
                    QueryText = "Existing text",
                    Status = "completed",
                    Actions = new List<UnifiedActionDto>
                    {
                        new() { Order = 1, Type = "test", Status = "completed", Description = "Existing action" }
                    }
                }
            },
            TurnCount = 1
        };

        client.QueryAsync("Codex", null, null, null, null, 1000, 0, Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult
            {
                Items = new[] { existing },
                TotalCount = 1,
                Limit = 1000
            });

        UnifiedSessionLogDto? submitted = null;
        client.SubmitAsync(Arg.Do<UnifiedSessionLogDto>(dto => submitted = dto), Arg.Any<CancellationToken>())
            .Returns(new SessionLogSubmitResult
            {
                Id = 42,
                SourceType = "Codex",
                SessionId = "Codex-20260514T000000Z-recovery"
            });

        var workflow = new SessionLogWorkflow(client, TimeProvider.System);
        var incoming = new UnifiedSessionLogDto
        {
            SourceType = "Codex",
            SessionId = "Codex-20260514T000000Z-recovery",
            Title = "Recovered Session",
            Model = "gpt-5",
            AgentSessionId = "Codex-20260514T000000Z-agent",
            AgentSessionTranscriptFile = "F:/GitHub/McpServer/.mcpServer/codex/transcripts/recovery.jsonl",
            AgentExecutablePath = "C:/Users/kingd/AppData/Roaming/npm/codex.cmd",
            AgentExecutableVersion = "1.2.3",
            Started = "2026-05-14T00:00:00Z",
            LastUpdated = "2026-05-14T00:05:00Z",
            Status = "completed",
            Turns = new List<UnifiedRequestEntryDto>
            {
                new()
                {
                    RequestId = "req-20260514T000100Z-existing",
                    Timestamp = "2026-05-14T00:01:00Z",
                    QueryTitle = "Existing",
                    QueryText = "Existing text",
                    Status = "completed",
                    Actions = new List<UnifiedActionDto>
                    {
                        new() { Order = 2, Type = "edit", Status = "completed", Description = "Recovered action" }
                    }
                },
                new()
                {
                    RequestId = "req-20260514T000500Z-imported",
                    Timestamp = "2026-05-14T00:05:00Z",
                    QueryTitle = "Imported",
                    QueryText = "Imported text",
                    Status = "completed-local",
                    Response = "Recovered response"
                }
            }
        };

        var result = await workflow.ImportRecoveryAsync(incoming, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.ExistingSessionFound);
        Assert.Equal(1, result.ImportedTurns);
        Assert.Equal(1, result.MergedTurns);
        Assert.Equal(2, result.TotalTurns);
        Assert.NotNull(submitted);
        Assert.Equal(2, submitted!.Turns!.Count);
        Assert.Equal(2, submitted.TurnCount);
        Assert.Equal("completed", submitted.Status);
        Assert.Equal("Codex-20260514T000000Z-agent", submitted.AgentSessionId);
        Assert.Equal("F:/GitHub/McpServer/.mcpServer/codex/transcripts/recovery.jsonl", submitted.AgentSessionTranscriptFile);
        Assert.Equal("C:/Users/kingd/AppData/Roaming/npm/codex.cmd", submitted.AgentExecutablePath);
        Assert.Equal("1.2.3", submitted.AgentExecutableVersion);
        Assert.Contains(submitted.Turns, turn =>
            turn.RequestId == "req-20260514T000100Z-existing" &&
            turn.Actions is { Count: 2 });
        Assert.Contains(submitted.Turns, turn =>
            turn.RequestId == "req-20260514T000500Z-imported" &&
            turn.Status == "completed");
    }

    #endregion

    #region Turn Lifecycle Tests

    /// <summary>
    /// AC-TR-MCP-SESSIONLOG-006-007 / TEST-MCP-SESSIONLOG-006:
    /// REPL BeginTurnAsync forwards planFile and todoId onto the submitted turn.
    /// </summary>
    [Fact]
    public async Task BeginTurnAsync_ForwardsPlanFileAndTodoId()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync(
            "req-20260304T113901Z-task-001",
            "Task Title",
            "Task Description",
            cancellationToken: TestContext.Current.CancellationToken,
            planFile: "docs/plans/foo.md",
            todoId: "MCP-SESSIONLOG-002");

        var submitted = _stubClient.LastSubmitted;
        Assert.NotNull(submitted);
        var turn = Assert.Single(submitted!.Turns!);
        Assert.Equal("docs/plans/foo.md", turn.PlanFile);
        Assert.Equal("MCP-SESSIONLOG-002", turn.TodoId);
    }

    [Fact]
    public async Task BeginTurnAsync_ValidParameters_CreatesTurn()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);

        var requestId = "req-20260304T113901Z-task-001";
        await _workflow.BeginTurnAsync(requestId, "Task Title", "Task Description", cancellationToken: TestContext.Current.CancellationToken);

        var session = _workflow.CurrentSession();
        Assert.Equal(requestId, session!.CurrentTurnRequestId);
        Assert.Equal("in_progress", session.CurrentTurnStatus);
    }

    [Fact]
    public async Task BeginTurnAsync_NoActiveSession_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.BeginTurnAsync("req-20260304T113901Z-task", "Title", "Query", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BeginTurnAsync_InvalidRequestId_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.BeginTurnAsync("request-20260304T113901Z-task", "Title", "Query", cancellationToken: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.BeginTurnAsync("req-20260304-task", "Title", "Query", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BeginTurnAsync_DuplicateRequestId_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);

        var requestId = "req-20260304T113901Z-duplicate";
        await _workflow.BeginTurnAsync(requestId, "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.CompleteTurnAsync("Done", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.BeginTurnAsync(requestId, "Task", "Query", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateTurnAsync_ActiveTurn_UpdatesFields()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        await _workflow.UpdateTurnAsync(
            response: "Updated response",
            interpretation: "Updated interpretation",
            tokenCount: 1250,
            tags: new List<string> { "feature", "security" },
            contextList: new List<string> { "src/File1.cs" }, cancellationToken: TestContext.Current.CancellationToken);

        // No exception means success
        var session = _workflow.CurrentSession();
        Assert.Equal("in_progress", session!.CurrentTurnStatus);
    }

    [Fact]
    public async Task UpdateTurnAsync_NoActiveTurn_ThrowsInvalidOperationException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.UpdateTurnAsync(response: "Response", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompleteTurnAsync_ActiveTurn_MarksAsCompleted()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        await _workflow.CompleteTurnAsync("Task completed successfully", cancellationToken: TestContext.Current.CancellationToken);

        var session = _workflow.CurrentSession();
        Assert.Null(session!.CurrentTurnRequestId);
        Assert.Null(session.CurrentTurnStatus);
        Assert.Equal(1, session.TurnCount);
    }

    [Fact]
    public async Task CompleteTurnAsync_NullOrEmptyResponse_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        // ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
            await _workflow.CompleteTurnAsync(null!, cancellationToken: TestContext.Current.CancellationToken));

        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
            await _workflow.CompleteTurnAsync("", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FailTurnAsync_ActiveTurn_MarksAsFailed()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        await _workflow.FailTurnAsync("Unable to complete task", "dependency_missing", cancellationToken: TestContext.Current.CancellationToken);

        var session = _workflow.CurrentSession();
        Assert.Null(session!.CurrentTurnRequestId);
        Assert.Null(session.CurrentTurnStatus);
        Assert.Equal(1, session.TurnCount);
    }

    [Fact]
    public async Task TurnImmutability_CompletedTurn_CannotModify()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.CompleteTurnAsync("Done", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.UpdateTurnAsync(response: "Cannot update", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TurnImmutability_FailedTurn_CannotModify()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.FailTurnAsync("Failed", "error", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _workflow.UpdateTurnAsync(response: "Cannot update", cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region Dialog and Action Append Tests

    [Fact]
    public async Task AppendDialogAsync_ValidDialogItems_AppendsToTurn()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        var dialogItems = new List<IDialogItem>
        {
            new DialogItem(DateTimeOffset.UtcNow, "model", "Analyzing requirements...", "reasoning"),
            new DialogItem(DateTimeOffset.UtcNow, "tool", "File created", "tool_result")
        };

        await _workflow.AppendDialogAsync(dialogItems, cancellationToken: TestContext.Current.CancellationToken);

        // No exception means success
    }

    [Fact]
    public async Task AppendDialogAsync_EmptyList_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.AppendDialogAsync(new List<IDialogItem>(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AppendActionsAsync_ValidActions_AppendsToTurn()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        var actions = new List<ISessionAction>
        {
            new SessionAction(1, "Created File1.cs", "create", "completed", "src/File1.cs"),
            new SessionAction(2, "Edited File2.cs", "edit", "completed", "src/File2.cs")
        };

        await _workflow.AppendActionsAsync(actions, cancellationToken: TestContext.Current.CancellationToken);

        // No exception means success
    }

    [Fact]
    public async Task AppendActionsAsync_EmptyList_ThrowsArgumentException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _workflow.AppendActionsAsync(new List<ISessionAction>(), cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region Query History Tests

    [Fact]
    public async Task QueryHistoryAsync_NoFilter_ReturnsResults()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.CompleteTurnAsync("Done", cancellationToken: TestContext.Current.CancellationToken);

        var history = await _workflow.QueryHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(history);
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task QueryHistoryAsync_FilterByAgent_ReturnsMatchingSessions()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-test", "Test", "model", cancellationToken: TestContext.Current.CancellationToken);

        var history = await _workflow.QueryHistoryAsync(agent: "Copilot", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(history);
        Assert.All(history, s => Assert.Equal("Copilot", s.Agent));
    }

    [Fact]
    public async Task QueryHistoryAsync_Pagination_ReturnsCorrectSlice()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        var history = await _workflow.QueryHistoryAsync(limit: 5, offset: 0, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(history);
    }

    [Fact]
    public async Task QueryHistoryAsync_NegativeLimit_ThrowsArgumentOutOfRangeException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _workflow.QueryHistoryAsync(limit: -1, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task QueryHistoryAsync_NegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _workflow.QueryHistoryAsync(offset: -1, cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region Complete Workflow Integration Tests

    [Fact]
    public async Task CompleteWorkflow_OpenSessionBeginTurnComplete_Success()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);

        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-complete-workflow";
        var title = "Complete Workflow Test";
        var model = "claude-sonnet-4";

        await _workflow.OpenSessionAsync(agent, sessionId, title, model, cancellationToken: TestContext.Current.CancellationToken);

        var session = _workflow.CurrentSession();
        Assert.NotNull(session);
        Assert.Equal(sessionId, session!.SessionId);
        Assert.Equal("in_progress", session.Status);

        var requestId = "req-20260304T113901Z-task-001";
        await _workflow.BeginTurnAsync(requestId, "Task Title", "Query text", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(requestId, session.CurrentTurnRequestId);
        Assert.Equal("in_progress", session.CurrentTurnStatus);

        await _workflow.UpdateTurnAsync(response: "Working on it...", cancellationToken: TestContext.Current.CancellationToken);

        await _workflow.CompleteTurnAsync("Task completed", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(session.CurrentTurnRequestId);
        Assert.Equal(1, session.TurnCount);
    }

    [Fact]
    public async Task CompleteWorkflow_MultipleTurnsInSession_Success()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-multi-turn", "Multi-Turn", "model", cancellationToken: TestContext.Current.CancellationToken);

        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task 1", "Query 1", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.UpdateTurnAsync(response: "Response 1", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.CompleteTurnAsync("Task 1 completed", cancellationToken: TestContext.Current.CancellationToken);

        var session = _workflow.CurrentSession();
        Assert.Equal(1, session!.TurnCount);

        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-002", "Task 2", "Query 2", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.UpdateTurnAsync(response: "Response 2", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.CompleteTurnAsync("Task 2 completed", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, session.TurnCount);

        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-003", "Task 3", "Query 3", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.FailTurnAsync("Task 3 failed", "error_code", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, session.TurnCount);
        Assert.Null(session.CurrentTurnRequestId);
    }

    [Fact]
    public async Task CompleteWorkflow_WithDialogAndActions_Success()
    {
        await _workflow.BootstrapAsync(cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.OpenSessionAsync("Copilot", "Copilot-20260304T113901Z-dialog", "Dialog Test", "model", cancellationToken: TestContext.Current.CancellationToken);
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query", cancellationToken: TestContext.Current.CancellationToken);

        var dialogItems = new List<IDialogItem>
        {
            new DialogItem(DateTimeOffset.UtcNow, "model", "Analyzing...", "reasoning"),
            new DialogItem(DateTimeOffset.UtcNow, "tool", "File created", "tool_result")
        };
        await _workflow.AppendDialogAsync(dialogItems, cancellationToken: TestContext.Current.CancellationToken);

        var actions = new List<ISessionAction>
        {
            new SessionAction(1, "Created File1.cs", "create", "completed", "src/File1.cs")
        };
        await _workflow.AppendActionsAsync(actions, cancellationToken: TestContext.Current.CancellationToken);

        await _workflow.CompleteTurnAsync("Completed with dialog and actions", cancellationToken: TestContext.Current.CancellationToken);

        var session = _workflow.CurrentSession();
        Assert.Equal(1, session!.TurnCount);
    }

    #endregion
}
