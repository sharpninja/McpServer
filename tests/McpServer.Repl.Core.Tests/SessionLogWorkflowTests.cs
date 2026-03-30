using McpServer.Repl.Core;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YamlDotNet.Serialization;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Iteration 2 unit tests for Session Log workflow orchestration.
/// Tests session creation, turn lifecycle (begin/update/complete/fail), active-session state management,
/// duplicate turn prevention, canonical identifier handling, restart/reconnect behavior,
/// and structured error responses.
/// Uses stub SessionLogClient and fake ISessionLogState with in-memory tracking.
/// Validates workflow command routing to correct SessionLogClient methods.
/// Verifies turn lifecycle guards: no duplicate turns, proper status transitions.
/// </summary>
public class SessionLogWorkflowTests
{
    private readonly ISessionLogWorkflow _workflow;
    private readonly IYamlSerializer _yamlSerializer;
    private readonly FakeSessionLogState _fakeState;

    public SessionLogWorkflowTests()
    {
        _yamlSerializer = new FakeYamlSerializer();
        _fakeState = new FakeSessionLogState();
        _workflow = Substitute.For<ISessionLogWorkflow>();
    }

    #region Bootstrap Tests

    [Fact]
    public async Task BootstrapAsync_FirstCall_InitializesSubsystem()
    {
        var initialized = false;
        _workflow.BootstrapAsync(default).Returns(callInfo =>
        {
            initialized = true;
            return Task.CompletedTask;
        });

        await _workflow.BootstrapAsync();

        Assert.True(initialized);
        await _workflow.Received(1).BootstrapAsync(default);
    }

    [Fact]
    public async Task BootstrapAsync_IdempotentCall_DoesNotThrow()
    {
        await _workflow.BootstrapAsync();
        await _workflow.BootstrapAsync();
        await _workflow.BootstrapAsync();

        await _workflow.Received(3).BootstrapAsync(default);
    }

    [Fact]
    public async Task BootstrapAsync_ConfigurationError_ThrowsInvalidOperationException()
    {
        _workflow.BootstrapAsync(default)
            .Throws(new InvalidOperationException("Storage initialization failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.BootstrapAsync());
    }

    #endregion

    #region Session Creation Tests

    [Fact]
    public async Task OpenSessionAsync_ValidParameters_CreatesSession()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-feature-auth";
        var title = "Implementing JWT authentication";
        var model = "claude-sonnet-4-20250514";

        await _workflow.OpenSessionAsync(agent, sessionId, title, model);

        await _workflow.Received(1).OpenSessionAsync(agent, sessionId, title, model, default);
    }

    [Fact]
    public async Task OpenSessionAsync_ValidSessionId_MatchesCanonicalFormat()
    {
        var validSessionIds = new[]
        {
            "Copilot-20260304T113901Z-feature",
            "Cline-20260304T120000Z-bugfix-auth",
            "Cursor-20260304T150000Z-refactor-session",
            "MyAgent-20260304T113901Z-test"
        };

        foreach (var sessionId in validSessionIds)
        {
            await _workflow.OpenSessionAsync("TestAgent", sessionId, "Test", "test-model");
        }

        await _workflow.Received(validSessionIds.Length)
            .OpenSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), default);
    }

    [Fact]
    public async Task OpenSessionAsync_InvalidSessionId_ThrowsArgumentException()
    {
        var invalidSessionIds = new[]
        {
            "copilot-20260304T113901Z-feature",        // lowercase prefix
            "Copilot-20260304-feature",                 // missing time component
            "Copilot-20260304T113901Z",                 // missing suffix
            "req-20260304T113901Z-feature",             // wrong prefix (req is for requestId)
            "Copilot-invalid-timestamp-feature",        // invalid timestamp
            "Copilot-20260304T113901Z-Feature"          // uppercase in suffix
        };

        foreach (var sessionId in invalidSessionIds)
        {
            _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model", default)
                .Throws(new ArgumentException($"Invalid session ID format: {sessionId}"));

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model"));
        }
    }

    [Fact]
    public async Task OpenSessionAsync_NullOrEmptyParameters_ThrowsArgumentException()
    {
        var validSessionId = "Copilot-20260304T113901Z-test";

        _workflow.OpenSessionAsync(null!, validSessionId, "title", "model", default)
            .Throws(new ArgumentException("Agent cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.OpenSessionAsync(null!, validSessionId, "title", "model"));

        _workflow.OpenSessionAsync("agent", null!, "title", "model", default)
            .Throws(new ArgumentException("SessionId cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.OpenSessionAsync("agent", null!, "title", "model"));

        _workflow.OpenSessionAsync("agent", validSessionId, null!, "model", default)
            .Throws(new ArgumentException("Title cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.OpenSessionAsync("agent", validSessionId, null!, "model"));

        _workflow.OpenSessionAsync("agent", validSessionId, "title", null!, default)
            .Throws(new ArgumentException("Model cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.OpenSessionAsync("agent", validSessionId, "title", null!));
    }

    [Fact]
    public async Task OpenSessionAsync_DuplicateSessionId_ThrowsInvalidOperationException()
    {
        var sessionId = "Copilot-20260304T113901Z-duplicate";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");

        _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model", default)
            .Throws(new InvalidOperationException($"Session with ID {sessionId} already exists"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model"));
    }

    [Fact]
    public async Task OpenSessionAsync_AgentPrefixMismatch_ThrowsArgumentException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var mismatchedAgent = "Cline";

        _workflow.OpenSessionAsync(mismatchedAgent, sessionId, "Test", "model", default)
            .Throws(new ArgumentException("Session ID prefix must match agent name"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.OpenSessionAsync(mismatchedAgent, sessionId, "Test", "model"));
    }

    #endregion

    #region Active Session State Tests

    [Fact]
    public void CurrentSession_NoActiveSession_ReturnsNull()
    {
        _workflow.CurrentSession().Returns((ISessionLogState?)null);

        var session = _workflow.CurrentSession();

        Assert.Null(session);
    }

    [Fact]
    public async Task CurrentSession_AfterOpenSession_ReturnsSessionState()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-test";
        var title = "Test Session";
        var model = "claude-sonnet-4";

        var mockState = CreateMockSessionState(agent, sessionId, title, model);
        _workflow.CurrentSession().Returns(mockState);

        await _workflow.OpenSessionAsync(agent, sessionId, title, model);
        var session = _workflow.CurrentSession();

        Assert.NotNull(session);
        Assert.Equal(agent, session!.Agent);
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal(title, session.Title);
        Assert.Equal(model, session.Model);
        Assert.Equal("in_progress", session.Status);
        Assert.Null(session.CurrentTurnRequestId);
        Assert.Null(session.CurrentTurnStatus);
        Assert.Equal(0, session.TurnCount);
    }

    [Fact]
    public async Task CurrentSession_AfterBeginTurn_ReturnsActiveTurnInfo()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");

        var mockState = CreateMockSessionState("Copilot", sessionId, "Test", "model",
            currentTurnRequestId: requestId, currentTurnStatus: "in_progress", turnCount: 1);
        _workflow.CurrentSession().Returns(mockState);

        await _workflow.BeginTurnAsync(requestId, "Task Title", "Task Description");

        var session = _workflow.CurrentSession();
        Assert.NotNull(session);
        Assert.Equal(requestId, session!.CurrentTurnRequestId);
        Assert.Equal("in_progress", session.CurrentTurnStatus);
        Assert.Equal(1, session.TurnCount);
    }

    [Fact]
    public async Task CurrentSession_AfterCompleteTurn_ClearsActiveTurn()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Description");

        var mockStateBeforeComplete = CreateMockSessionState("Copilot", sessionId, "Test", "model",
            currentTurnRequestId: requestId, currentTurnStatus: "in_progress", turnCount: 1);
        var mockStateAfterComplete = CreateMockSessionState("Copilot", sessionId, "Test", "model",
            currentTurnRequestId: null, currentTurnStatus: null, turnCount: 1);

        _workflow.CurrentSession().Returns(mockStateBeforeComplete, mockStateAfterComplete);

        var beforeComplete = _workflow.CurrentSession();
        Assert.Equal(requestId, beforeComplete!.CurrentTurnRequestId);

        await _workflow.CompleteTurnAsync("Task completed");

        var afterComplete = _workflow.CurrentSession();
        Assert.Null(afterComplete!.CurrentTurnRequestId);
        Assert.Null(afterComplete.CurrentTurnStatus);
        Assert.Equal(1, afterComplete.TurnCount);
    }

    #endregion

    #region Turn Lifecycle Tests

    [Fact]
    public async Task BeginTurnAsync_ValidRequestId_CreatesTurnInProgress()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task Title", "Query text");

        await _workflow.Received(1).BeginTurnAsync(requestId, "Task Title", "Query text", default);
    }

    [Fact]
    public async Task BeginTurnAsync_InvalidRequestId_ThrowsArgumentException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");

        var invalidRequestIds = new[]
        {
            "request-20260304T113901Z-task",            // wrong prefix
            "req-20260304-task",                        // missing time component
            "req-invalid-timestamp-task",               // invalid timestamp
            "req-20260304T113901Z",                     // missing suffix
            "req-20260304T113901Z-Task"                 // uppercase in suffix
        };

        foreach (var invalidId in invalidRequestIds)
        {
            _workflow.BeginTurnAsync(invalidId, "Title", "Query", default)
                .Throws(new ArgumentException($"Invalid request ID format: {invalidId}"));

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _workflow.BeginTurnAsync(invalidId, "Title", "Query"));
        }
    }

    [Fact]
    public async Task BeginTurnAsync_NoActiveSession_ThrowsInvalidOperationException()
    {
        _workflow.CurrentSession().Returns((ISessionLogState?)null);
        _workflow.BeginTurnAsync("req-20260304T113901Z-task", "Title", "Query", default)
            .Throws(new InvalidOperationException("No active session"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.BeginTurnAsync("req-20260304T113901Z-task", "Title", "Query"));
    }

    [Fact]
    public async Task BeginTurnAsync_DuplicateRequestId_ThrowsInvalidOperationException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-duplicate";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        _workflow.BeginTurnAsync(requestId, "Task", "Query", default)
            .Throws(new InvalidOperationException($"Turn with request ID {requestId} already exists"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.BeginTurnAsync(requestId, "Task", "Query"));
    }

    [Fact]
    public async Task UpdateTurnAsync_ActiveTurn_UpdatesFields()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        var response = "Generated response";
        var interpretation = "User wants to implement feature X";
        var tokenCount = 1250;
        var tags = new List<string> { "feature", "security" };
        var contextList = new List<string> { "src/File1.cs", "src/File2.cs" };

        await _workflow.UpdateTurnAsync(response, interpretation, tokenCount, tags, contextList);

        await _workflow.Received(1).UpdateTurnAsync(response, interpretation, tokenCount,
            Arg.Is<IReadOnlyList<string>?>(t => t != null && t.SequenceEqual(tags)),
            Arg.Is<IReadOnlyList<string>?>(c => c != null && c.SequenceEqual(contextList)), default);
    }

    [Fact]
    public async Task UpdateTurnAsync_PartialUpdate_PreservesExistingValues()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        await _workflow.UpdateTurnAsync(response: "Response only");
        await _workflow.Received(1).UpdateTurnAsync("Response only", null, null, null, null, default);

        await _workflow.UpdateTurnAsync(interpretation: "Interpretation only");
        await _workflow.Received(1).UpdateTurnAsync(null, "Interpretation only", null, null, null, default);

        await _workflow.UpdateTurnAsync(tokenCount: 500);
        await _workflow.Received(1).UpdateTurnAsync(null, null, 500, null, null, default);
    }

    [Fact]
    public async Task UpdateTurnAsync_NoActiveTurn_ThrowsInvalidOperationException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");

        var mockState = CreateMockSessionState("Copilot", sessionId, "Test", "model");
        _workflow.CurrentSession().Returns(mockState);
        _workflow.UpdateTurnAsync(Arg.Any<string>(), null, null, null, null, default)
            .Throws(new InvalidOperationException("No active turn"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateTurnAsync(response: "Response"));
    }

    [Fact]
    public async Task UpdateTurnAsync_CompletedTurn_ThrowsInvalidOperationException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");
        await _workflow.CompleteTurnAsync("Completed");

        _workflow.UpdateTurnAsync(Arg.Any<string>(), null, null, null, null, default)
            .Throws(new InvalidOperationException("Turn is immutable (status: completed)"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateTurnAsync(response: "Updated response"));
    }

    [Fact]
    public async Task CompleteTurnAsync_ActiveTurn_MarksAsCompleted()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        var finalResponse = "Task completed successfully";
        await _workflow.CompleteTurnAsync(finalResponse);

        await _workflow.Received(1).CompleteTurnAsync(finalResponse, default);
    }

    [Fact]
    public async Task CompleteTurnAsync_NullOrEmptyResponse_ThrowsArgumentException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        _workflow.CompleteTurnAsync(null!, default)
            .Throws(new ArgumentException("Response cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.CompleteTurnAsync(null!));

        _workflow.CompleteTurnAsync("", default)
            .Throws(new ArgumentException("Response cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.CompleteTurnAsync(""));
    }

    [Fact]
    public async Task CompleteTurnAsync_TurnImmutable_CannotModifyAfter()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");
        await _workflow.CompleteTurnAsync("Completed");

        _workflow.UpdateTurnAsync(Arg.Any<string>(), null, null, null, null, default)
            .Throws(new InvalidOperationException("Turn is immutable"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateTurnAsync(response: "Cannot update"));

        _workflow.AppendDialogAsync(Arg.Any<IReadOnlyList<IDialogItem>>(), default)
            .Throws(new InvalidOperationException("Turn is immutable"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.AppendDialogAsync(new List<IDialogItem>()));

        _workflow.AppendActionsAsync(Arg.Any<IReadOnlyList<ISessionAction>>(), default)
            .Throws(new InvalidOperationException("Turn is immutable"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.AppendActionsAsync(new List<ISessionAction>()));
    }

    [Fact]
    public async Task FailTurnAsync_ActiveTurn_MarksAsFailed()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        var errorMessage = "Unable to complete task due to missing dependencies";
        var errorCode = "dependency_missing";

        await _workflow.FailTurnAsync(errorMessage, errorCode);

        await _workflow.Received(1).FailTurnAsync(errorMessage, errorCode, default);
    }

    [Fact]
    public async Task FailTurnAsync_NullOrEmptyErrorMessage_ThrowsArgumentException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        _workflow.FailTurnAsync(null!, null, default)
            .Throws(new ArgumentException("Error message cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.FailTurnAsync(null!));

        _workflow.FailTurnAsync("", null, default)
            .Throws(new ArgumentException("Error message cannot be null or empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.FailTurnAsync(""));
    }

    [Fact]
    public async Task FailTurnAsync_TurnImmutable_CannotModifyAfter()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");
        await _workflow.FailTurnAsync("Task failed", "task_error");

        _workflow.UpdateTurnAsync(Arg.Any<string>(), null, null, null, null, default)
            .Throws(new InvalidOperationException("Turn is immutable"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateTurnAsync(response: "Cannot update"));

        _workflow.CompleteTurnAsync(Arg.Any<string>(), default)
            .Throws(new InvalidOperationException("Turn is immutable"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.CompleteTurnAsync("Cannot complete"));
    }

    #endregion

    #region Dialog and Action Append Tests

    [Fact]
    public async Task AppendDialogAsync_ValidDialogItems_AppendsToTurn()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        var dialogItems = new List<IDialogItem>
        {
            CreateMockDialogItem("model", "Analyzing requirements...", "reasoning"),
            CreateMockDialogItem("tool", "File created", "tool_result")
        };

        await _workflow.AppendDialogAsync(dialogItems);

        await _workflow.Received(1).AppendDialogAsync(
            Arg.Is<IReadOnlyList<IDialogItem>>(d => d != null && d.Count == 2), default);
    }

    [Fact]
    public async Task AppendDialogAsync_NullOrEmptyItems_ThrowsArgumentException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        _workflow.AppendDialogAsync(null!, default)
            .Throws(new ArgumentNullException("dialogItems"));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.AppendDialogAsync(null!));

        _workflow.AppendDialogAsync(Arg.Is<IReadOnlyList<IDialogItem>>(l => l != null && l.Count == 0), default)
            .Throws(new ArgumentException("DialogItems cannot be empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.AppendDialogAsync(new List<IDialogItem>()));
    }

    [Fact]
    public async Task AppendActionsAsync_ValidActions_AppendsToTurn()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        var actions = new List<ISessionAction>
        {
            CreateMockAction(1, "Created File1.cs", "create", "completed", "src/File1.cs"),
            CreateMockAction(2, "Edited File2.cs", "edit", "completed", "src/File2.cs")
        };

        await _workflow.AppendActionsAsync(actions);

        await _workflow.Received(1).AppendActionsAsync(
            Arg.Is<IReadOnlyList<ISessionAction>>(a => a != null && a.Count == 2), default);
    }

    [Fact]
    public async Task AppendActionsAsync_NullOrEmptyActions_ThrowsArgumentException()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");

        _workflow.AppendActionsAsync(null!, default)
            .Throws(new ArgumentNullException("actions"));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _workflow.AppendActionsAsync(null!));

        _workflow.AppendActionsAsync(Arg.Is<IReadOnlyList<ISessionAction>>(l => l != null && l.Count == 0), default)
            .Throws(new ArgumentException("Actions cannot be empty"));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.AppendActionsAsync(new List<ISessionAction>()));
    }

    #endregion

    #region Restart and Reconnect Behavior Tests

    [Fact]
    public async Task RestartScenario_AfterRestart_NoActiveSession()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");

        var beforeRestart = _workflow.CurrentSession();
        Assert.NotNull(beforeRestart);

        _workflow.CurrentSession().Returns((ISessionLogState?)null);

        var afterRestart = _workflow.CurrentSession();
        Assert.Null(afterRestart);
    }

    [Fact]
    public async Task ReconnectScenario_CanQueryHistoryAfterRestart()
    {
        var sessionId = "Copilot-20260304T113901Z-old-session";
        await _workflow.OpenSessionAsync("Copilot", sessionId, "Old Session", "model");
        await _workflow.BeginTurnAsync("req-20260304T113901Z-task-001", "Task", "Query");
        await _workflow.CompleteTurnAsync("Done");

        _workflow.CurrentSession().Returns((ISessionLogState?)null);

        var mockSummaries = new List<ISessionLogSummary>
        {
            CreateMockSessionSummary("Copilot", sessionId, "Old Session", "model", 1)
        };

        _workflow.QueryHistoryAsync("Copilot", 10, 0, default)
            .Returns(mockSummaries);

        var history = await _workflow.QueryHistoryAsync("Copilot");

        Assert.NotNull(history);
        Assert.Single(history);
        Assert.Equal(sessionId, history![0].SessionId);
    }

    [Fact]
    public async Task ReconnectScenario_CanOpenNewSessionAfterRestart()
    {
        _workflow.CurrentSession().Returns((ISessionLogState?)null);

        var newSessionId = "Copilot-20260304T150000Z-new-session";
        await _workflow.OpenSessionAsync("Copilot", newSessionId, "New Session", "model");

        var mockState = CreateMockSessionState("Copilot", newSessionId, "New Session", "model");
        _workflow.CurrentSession().Returns(mockState);

        var session = _workflow.CurrentSession();
        Assert.NotNull(session);
        Assert.Equal(newSessionId, session!.SessionId);
    }

    #endregion

    #region Query History Tests

    [Fact]
    public async Task QueryHistoryAsync_NoFilter_ReturnsAllSessions()
    {
        var mockSummaries = new List<ISessionLogSummary>
        {
            CreateMockSessionSummary("Copilot", "Copilot-20260304T113901Z-s1", "Session 1", "model", 3),
            CreateMockSessionSummary("Cline", "Cline-20260304T120000Z-s2", "Session 2", "model", 5),
            CreateMockSessionSummary("Cursor", "Cursor-20260304T130000Z-s3", "Session 3", "model", 2)
        };

        _workflow.QueryHistoryAsync(null, 10, 0, default).Returns(mockSummaries);

        var history = await _workflow.QueryHistoryAsync();

        Assert.NotNull(history);
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public async Task QueryHistoryAsync_FilterByAgent_ReturnsMatchingSessions()
    {
        var mockSummaries = new List<ISessionLogSummary>
        {
            CreateMockSessionSummary("Copilot", "Copilot-20260304T113901Z-s1", "Session 1", "model", 3),
            CreateMockSessionSummary("Copilot", "Copilot-20260304T120000Z-s2", "Session 2", "model", 5)
        };

        _workflow.QueryHistoryAsync("Copilot", 10, 0, default).Returns(mockSummaries);

        var history = await _workflow.QueryHistoryAsync("Copilot");

        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.All(history, s => Assert.Equal("Copilot", s.Agent));
    }

    [Fact]
    public async Task QueryHistoryAsync_Pagination_ReturnsCorrectSlice()
    {
        var page1 = new List<ISessionLogSummary>
        {
            CreateMockSessionSummary("Copilot", "Copilot-20260304T113901Z-s1", "Session 1", "model", 1),
            CreateMockSessionSummary("Copilot", "Copilot-20260304T120000Z-s2", "Session 2", "model", 1)
        };

        var page2 = new List<ISessionLogSummary>
        {
            CreateMockSessionSummary("Copilot", "Copilot-20260304T130000Z-s3", "Session 3", "model", 1)
        };

        _workflow.QueryHistoryAsync(null, 2, 0, default).Returns(page1);
        _workflow.QueryHistoryAsync(null, 2, 2, default).Returns(page2);

        var firstPage = await _workflow.QueryHistoryAsync(limit: 2, offset: 0);
        Assert.Equal(2, firstPage!.Count);

        var secondPage = await _workflow.QueryHistoryAsync(limit: 2, offset: 2);
        Assert.Single(secondPage!);
    }

    [Fact]
    public async Task QueryHistoryAsync_NegativeLimitOrOffset_ThrowsArgumentOutOfRangeException()
    {
        _workflow.QueryHistoryAsync(null, -1, 0, default)
            .Throws(new ArgumentOutOfRangeException("limit"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _workflow.QueryHistoryAsync(limit: -1));

        _workflow.QueryHistoryAsync(null, 10, -1, default)
            .Throws(new ArgumentOutOfRangeException("offset"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _workflow.QueryHistoryAsync(offset: -1));
    }

    #endregion

    #region YAML Request/Response Shaping Tests

    [Fact]
    public void YamlShaping_BootstrapRequest_MatchesExpectedStructure()
    {
        var requestPayload = new
        {
            requestId = "req-20260304T113901Z-bootstrap-001",
            method = SessionLogCommandShapes.BootstrapMethod,
            @params = new { }
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("request", requestPayload));

        Assert.Contains("type: request", yaml);
        Assert.Contains("method: workflow.sessionlog.bootstrap", yaml);
    }

    [Fact]
    public void YamlShaping_OpenSessionRequest_MatchesExpectedStructure()
    {
        var requestPayload = new
        {
            requestId = "req-20260304T113901Z-open-001",
            method = SessionLogCommandShapes.OpenSessionMethod,
            @params = new
            {
                agent = "Copilot",
                sessionId = "Copilot-20260304T113901Z-feature-auth",
                title = "Implementing JWT authentication",
                model = "claude-sonnet-4-20250514"
            }
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("request", requestPayload));

        Assert.Contains("type: request", yaml);
        Assert.Contains("method: workflow.sessionlog.openSession", yaml);
        Assert.Contains("agent: Copilot", yaml);
        Assert.Contains("sessionId: Copilot-20260304T113901Z-feature-auth", yaml);
    }

    [Fact]
    public void YamlShaping_BeginTurnRequest_MatchesExpectedStructure()
    {
        var requestPayload = new
        {
            requestId = "req-20260304T113901Z-beginturn-001",
            method = SessionLogCommandShapes.BeginTurnMethod,
            @params = new
            {
                requestId = "req-20260304T113901Z-add-jwt-001",
                queryTitle = "Add JWT authentication",
                queryText = "Implement JWT token generation and validation"
            }
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("request", requestPayload));

        Assert.Contains("type: request", yaml);
        Assert.Contains("method: workflow.sessionlog.beginTurn", yaml);
        Assert.Contains("queryTitle: Add JWT authentication", yaml);
    }

    [Fact]
    public void YamlShaping_ErrorResponse_MatchesExpectedStructure()
    {
        var errorPayload = new
        {
            requestId = "req-20260304T113901Z-open-001",
            code = SessionLogErrorCodes.InvalidSessionId,
            message = "Session ID does not conform to canonical format",
            details = new Dictionary<string, object?>
            {
                ["providedId"] = "copilot-20260304-feature-auth",
                ["expectedFormat"] = "<Agent>-<yyyyMMddTHHmmssZ>-<suffix>"
            }
        };

        var yaml = _yamlSerializer.Serialize(CreateEnvelope("error", errorPayload));

        Assert.Contains("type: error", yaml);
        Assert.Contains("code: invalid_session_id", yaml);
        Assert.Contains("message: Session ID does not conform to canonical format", yaml);
    }

    #endregion

    #region Structured Error Response Tests

    [Fact]
    public async Task ErrorResponse_InvalidSessionId_ReturnsStructuredError()
    {
        var invalidSessionId = "copilot-20260304T113901Z-test";

        _workflow.OpenSessionAsync("Copilot", invalidSessionId, "Test", "model", default)
            .Throws(new ArgumentException("Invalid session ID format"));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _workflow.OpenSessionAsync("Copilot", invalidSessionId, "Test", "model"));

        Assert.Contains("Invalid session ID", exception.Message);
    }

    [Fact]
    public async Task ErrorResponse_SessionNotFound_ReturnsStructuredError()
    {
        _workflow.CurrentSession().Returns((ISessionLogState?)null);
        _workflow.BeginTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), default)
            .Throws(new InvalidOperationException("No active session exists"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.BeginTurnAsync("req-20260304T113901Z-task", "Title", "Query"));

        Assert.Contains("No active session", exception.Message);
    }

    [Fact]
    public async Task ErrorResponse_TurnImmutable_ReturnsStructuredError()
    {
        var sessionId = "Copilot-20260304T113901Z-test";
        var requestId = "req-20260304T113901Z-task-001";

        await _workflow.OpenSessionAsync("Copilot", sessionId, "Test", "model");
        await _workflow.BeginTurnAsync(requestId, "Task", "Query");
        await _workflow.CompleteTurnAsync("Done");

        _workflow.UpdateTurnAsync(Arg.Any<string>(), null, null, null, null, default)
            .Throws(new InvalidOperationException("Turn is immutable (status: completed)"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _workflow.UpdateTurnAsync(response: "Update"));

        Assert.Contains("immutable", exception.Message);
    }

    [Fact]
    public void ErrorCodes_AllCodesAreDefined()
    {
        Assert.Equal("bootstrap_failed", SessionLogErrorCodes.BootstrapFailed);
        Assert.Equal("session_not_found", SessionLogErrorCodes.SessionNotFound);
        Assert.Equal("session_already_exists", SessionLogErrorCodes.SessionAlreadyExists);
        Assert.Equal("invalid_session_id", SessionLogErrorCodes.InvalidSessionId);
        Assert.Equal("invalid_request_id", SessionLogErrorCodes.InvalidRequestId);
        Assert.Equal("turn_not_found", SessionLogErrorCodes.TurnNotFound);
        Assert.Equal("turn_already_exists", SessionLogErrorCodes.TurnAlreadyExists);
        Assert.Equal("turn_immutable", SessionLogErrorCodes.TurnImmutable);
        Assert.Equal("invalid_turn_state", SessionLogErrorCodes.InvalidTurnState);
        Assert.Equal("invalid_parameter", SessionLogErrorCodes.InvalidParameter);
        Assert.Equal("storage_error", SessionLogErrorCodes.StorageError);
        Assert.Equal("internal_error", SessionLogErrorCodes.InternalError);
    }

    #endregion

    #region Turn Lifecycle Guard Tests

    [Fact]
    public void FakeSessionLogState_NoDuplicateTurns_EnforcesDuplicatePrevention()
    {
        var state = new FakeSessionLogState();
        
        state.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        state.BeginTurn("req-20260304T113901Z-task-001");

        var exception = Assert.Throws<InvalidOperationException>(
            () => state.BeginTurn("req-20260304T113901Z-task-001"));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void FakeSessionLogState_ProperStatusTransitions_EnforcesStateMachine()
    {
        var state = new FakeSessionLogState();
        
        state.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        state.BeginTurn("req-20260304T113901Z-task-001");
        
        Assert.Equal("in_progress", state.CurrentTurnStatus);

        state.CompleteTurn();
        Assert.Null(state.CurrentTurnStatus);
        Assert.Equal(1, state.TurnCount);

        state.BeginTurn("req-20260304T113901Z-task-002");
        Assert.Equal("in_progress", state.CurrentTurnStatus);
        
        state.FailTurn();
        Assert.Null(state.CurrentTurnStatus);
        Assert.Equal(2, state.TurnCount);
    }

    [Fact]
    public void FakeSessionLogState_CompletedTurnImmutable_ThrowsOnModify()
    {
        var state = new FakeSessionLogState();
        
        state.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        state.BeginTurn("req-20260304T113901Z-task-001");
        state.CompleteTurn();

        Assert.Throws<InvalidOperationException>(() => state.UpdateTurn());
    }

    [Fact]
    public void FakeSessionLogState_FailedTurnImmutable_ThrowsOnModify()
    {
        var state = new FakeSessionLogState();
        
        state.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        state.BeginTurn("req-20260304T113901Z-task-001");
        state.FailTurn();

        Assert.Throws<InvalidOperationException>(() => state.UpdateTurn());
    }

    #endregion

    #region Helper Methods

    private static ISessionLogState CreateMockSessionState(
        string agent,
        string sessionId,
        string title,
        string model,
        string? currentTurnRequestId = null,
        string? currentTurnStatus = null,
        int turnCount = 0)
    {
        var state = Substitute.For<ISessionLogState>();
        state.Agent.Returns(agent);
        state.SessionId.Returns(sessionId);
        state.Title.Returns(title);
        state.Model.Returns(model);
        state.Started.Returns(DateTimeOffset.UtcNow);
        state.LastUpdated.Returns(DateTimeOffset.UtcNow);
        state.Status.Returns("in_progress");
        state.CurrentTurnRequestId.Returns(currentTurnRequestId);
        state.CurrentTurnStatus.Returns(currentTurnStatus);
        state.TurnCount.Returns(turnCount);
        return state;
    }

    private static IDialogItem CreateMockDialogItem(string role, string content, string category)
    {
        var item = Substitute.For<IDialogItem>();
        item.Timestamp.Returns(DateTimeOffset.UtcNow);
        item.Role.Returns(role);
        item.Content.Returns(content);
        item.Category.Returns(category);
        return item;
    }

    private static ISessionAction CreateMockAction(int order, string description, string type, string status, string filePath)
    {
        var action = Substitute.For<ISessionAction>();
        action.Order.Returns(order);
        action.Description.Returns(description);
        action.Type.Returns(type);
        action.Status.Returns(status);
        action.FilePath.Returns(filePath);
        return action;
    }

    private static ISessionLogSummary CreateMockSessionSummary(
        string agent,
        string sessionId,
        string title,
        string model,
        int turnCount)
    {
        var summary = Substitute.For<ISessionLogSummary>();
        summary.Agent.Returns(agent);
        summary.SessionId.Returns(sessionId);
        summary.Title.Returns(title);
        summary.Model.Returns(model);
        summary.Started.Returns(DateTimeOffset.UtcNow.AddHours(-2));
        summary.LastUpdated.Returns(DateTimeOffset.UtcNow);
        summary.Status.Returns("completed");
        summary.TurnCount.Returns(turnCount);
        summary.Tags.Returns(new List<string> { "test" });
        summary.FilesModifiedCount.Returns(5);
        return summary;
    }

    private IYamlEnvelope CreateEnvelope(string type, object payload)
    {
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns(type);
        envelope.Payload.Returns(payload);
        return envelope;
    }

    #endregion
}

/// <summary>
/// Fake in-memory implementation of ISessionLogState for testing turn lifecycle.
/// Tracks session and turn state with validation rules.
/// </summary>
internal sealed class FakeSessionLogState : ISessionLogState
{
    private readonly HashSet<string> _completedRequestIds = new();
    private string? _currentTurnRequestId;
    private string? _lastCompletedStatus;

    public string Agent { get; private set; } = string.Empty;
    public string SessionId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public DateTimeOffset Started { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }
    public string Status { get; private set; } = "in_progress";
    public string? CurrentTurnRequestId => _currentTurnRequestId;
    public string? CurrentTurnStatus { get; private set; }
    public int TurnCount { get; private set; }

    public void OpenSession(string agent, string sessionId, string title, string model)
    {
        Agent = agent;
        SessionId = sessionId;
        Title = title;
        Model = model;
        Started = DateTimeOffset.UtcNow;
        LastUpdated = Started;
        Status = "in_progress";
    }

    public void BeginTurn(string requestId)
    {
        if (string.IsNullOrEmpty(SessionId))
        {
            throw new InvalidOperationException("No session is active");
        }

        if (_completedRequestIds.Contains(requestId))
        {
            throw new InvalidOperationException($"Turn with request ID {requestId} already exists");
        }

        if (_currentTurnRequestId != null)
        {
            throw new InvalidOperationException("A turn is already in progress");
        }

        _currentTurnRequestId = requestId;
        CurrentTurnStatus = "in_progress";
        LastUpdated = DateTimeOffset.UtcNow;
    }

    public void UpdateTurn()
    {
        if (_currentTurnRequestId == null)
        {
            throw new InvalidOperationException("No active turn");
        }

        if (_lastCompletedStatus != null)
        {
            throw new InvalidOperationException($"Turn is immutable (status: {_lastCompletedStatus})");
        }

        LastUpdated = DateTimeOffset.UtcNow;
    }

    public void CompleteTurn()
    {
        if (_currentTurnRequestId == null)
        {
            throw new InvalidOperationException("No active turn");
        }

        _completedRequestIds.Add(_currentTurnRequestId);
        _lastCompletedStatus = "completed";
        _currentTurnRequestId = null;
        CurrentTurnStatus = null;
        TurnCount++;
        LastUpdated = DateTimeOffset.UtcNow;
        _lastCompletedStatus = null;
    }

    public void FailTurn()
    {
        if (_currentTurnRequestId == null)
        {
            throw new InvalidOperationException("No active turn");
        }

        _completedRequestIds.Add(_currentTurnRequestId);
        _lastCompletedStatus = "failed";
        _currentTurnRequestId = null;
        CurrentTurnStatus = null;
        TurnCount++;
        LastUpdated = DateTimeOffset.UtcNow;
        _lastCompletedStatus = null;
    }
}
