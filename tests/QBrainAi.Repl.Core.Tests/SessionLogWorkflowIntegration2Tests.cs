using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client;
using QBrainAi.Client.Models;
using QBrainAi.Repl.Core;
using QBrainAi.Repl.Core.Tests.TestSupport;
using Xunit;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// Iteration 2 integration tests validating complete Session Log workflow with stubs and fakes.
/// Tests end-to-end scenarios using StubSessionLogClient and FakeSessionLogState together.
/// Validates that workflow operations correctly integrate with client API calls and state management.
/// </summary>
public class SessionLogWorkflowIntegration2Tests
{
    private readonly StubSessionLogClient _stubClient;
    private readonly FakeSessionLogState _fakeState;

    public SessionLogWorkflowIntegration2Tests()
    {
        _stubClient = new StubSessionLogClient();
        _fakeState = new FakeSessionLogState();
    }

    #region Complete Workflow Integration Tests

    [Fact]
    public async Task CompleteWorkflow_OpenSessionBeginTurnComplete_Success()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-complete-workflow";
        var title = "Complete Workflow Test";
        var model = "claude-sonnet-4";

        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = agent,
            SessionId = sessionId,
            Title = title,
            Model = model
        };

        var submitResult = await _stubClient.SubmitAsync(sessionLog, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(submitResult);
        Assert.Equal(sessionId, submitResult.SessionId);

        _fakeState.OpenSession(agent, sessionId, title, model);
        Assert.Equal(sessionId, _fakeState.SessionId);
        Assert.Equal("in_progress", _fakeState.Status);

        var requestId = "req-20260304T113901Z-task-001";
        _fakeState.BeginTurn(requestId);
        Assert.Equal(requestId, _fakeState.CurrentTurnRequestId);
        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);

        _fakeState.UpdateTurn();
        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);

        _fakeState.CompleteTurn();
        Assert.Null(_fakeState.CurrentTurnRequestId);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public async Task CompleteWorkflow_OpenSessionBeginTurnFail_Success()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-fail-workflow";
        var title = "Fail Workflow Test";
        var model = "claude-sonnet-4";

        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = agent,
            SessionId = sessionId,
            Title = title,
            Model = model
        };

        await _stubClient.SubmitAsync(sessionLog, cancellationToken: TestContext.Current.CancellationToken);

        _fakeState.OpenSession(agent, sessionId, title, model);

        var requestId = "req-20260304T113901Z-task-001";
        _fakeState.BeginTurn(requestId);

        _fakeState.FailTurn();
        Assert.Null(_fakeState.CurrentTurnRequestId);
        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public async Task CompleteWorkflow_MultipleTurnsInSession_Success()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-multi-turn";
        var title = "Multi-Turn Session";
        var model = "claude-sonnet-4";

        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = agent,
            SessionId = sessionId,
            Title = title,
            Model = model
        };

        await _stubClient.SubmitAsync(sessionLog, cancellationToken: TestContext.Current.CancellationToken);
        _fakeState.OpenSession(agent, sessionId, title, model);

        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.UpdateTurn();
        _fakeState.CompleteTurn();
        Assert.Equal(1, _fakeState.TurnCount);

        _fakeState.BeginTurn("req-20260304T113901Z-task-002");
        _fakeState.UpdateTurn();
        _fakeState.CompleteTurn();
        Assert.Equal(2, _fakeState.TurnCount);

        _fakeState.BeginTurn("req-20260304T113901Z-task-003");
        _fakeState.FailTurn();
        Assert.Equal(3, _fakeState.TurnCount);

        Assert.Null(_fakeState.CurrentTurnRequestId);
    }

    [Fact]
    public async Task CompleteWorkflow_AppendDialogDuringTurn_Success()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-dialog";
        var requestId = "req-20260304T113901Z-task-001";

        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = agent,
            SessionId = sessionId,
            Title = "Dialog Test",
            Model = "claude-sonnet-4"
        };

        await _stubClient.SubmitAsync(sessionLog, cancellationToken: TestContext.Current.CancellationToken);
        _fakeState.OpenSession(agent, sessionId, "Dialog Test", "claude-sonnet-4");
        _fakeState.BeginTurn(requestId);

        var dialogItems = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Analyzing...", Category = "reasoning" },
            new() { Role = "tool", Content = "File created", Category = "tool_result" }
        };

        var result = await _stubClient.AppendDialogAsync(agent, sessionId, requestId, dialogItems, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(agent, result.Agent);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(requestId, result.RequestId);
        Assert.True(result.TotalDialogCount >= 2);

        _fakeState.CompleteTurn();
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public async Task CompleteWorkflow_QueryHistoryAfterCompletion_ReturnsSession()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-history";

        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = agent,
            SessionId = sessionId,
            Title = "History Test",
            Model = "claude-sonnet-4"
        };

        await _stubClient.SubmitAsync(sessionLog, cancellationToken: TestContext.Current.CancellationToken);
        _fakeState.OpenSession(agent, sessionId, "History Test", "claude-sonnet-4");

        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        var queryResult = await _stubClient.QueryAsync(agent: agent, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(queryResult);
        Assert.NotEmpty(queryResult.Items);
        Assert.Contains(queryResult.Items, item => item.SessionId == sessionId);
    }

    #endregion

    #region Error Handling Integration Tests

    [Fact]
    public void ErrorHandling_BeginTurnWithoutSession_ThrowsException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.BeginTurn("req-20260304T113901Z-task-001"));

        Assert.Contains("No session", exception.Message);
    }

    [Fact]
    public void ErrorHandling_UpdateTurnWithoutBeginTurn_ThrowsException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.UpdateTurn());

        Assert.Contains("No active turn", exception.Message);
    }

    [Fact]
    public void ErrorHandling_CompleteTurnWithoutBeginTurn_ThrowsException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.CompleteTurn());

        Assert.Contains("No active turn", exception.Message);
    }

    [Fact]
    public void ErrorHandling_DuplicateTurnRequestId_ThrowsException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.BeginTurn("req-20260304T113901Z-task-001"));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void ErrorHandling_UpdateAfterComplete_ThrowsException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.UpdateTurn());

        Assert.Contains("No active turn", exception.Message);
    }

    [Fact]
    public void ErrorHandling_UpdateAfterFail_ThrowsException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.FailTurn();

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.UpdateTurn());

        Assert.Contains("No active turn", exception.Message);
    }

    #endregion

    #region State Transition Validation Tests

    [Fact]
    public void StateTransition_InProgressToCompleted_Valid()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);

        _fakeState.CompleteTurn();

        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public void StateTransition_InProgressToFailed_Valid()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);

        _fakeState.FailTurn();

        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public void StateTransition_InProgressUpdateInProgress_Valid()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);

        _fakeState.UpdateTurn();

        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);
    }

    [Fact]
    public void StateTransition_CompletedToAny_Invalid()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        Assert.Throws<InvalidOperationException>(() => _fakeState.UpdateTurn());
        Assert.Throws<InvalidOperationException>(() => _fakeState.CompleteTurn());
        Assert.Throws<InvalidOperationException>(() => _fakeState.FailTurn());
    }

    [Fact]
    public void StateTransition_FailedToAny_Invalid()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.FailTurn();

        Assert.Throws<InvalidOperationException>(() => _fakeState.UpdateTurn());
        Assert.Throws<InvalidOperationException>(() => _fakeState.CompleteTurn());
        Assert.Throws<InvalidOperationException>(() => _fakeState.FailTurn());
    }

    #endregion

    #region Concurrent Turn Prevention Tests

    [Fact]
    public void ConcurrentTurnPrevention_CannotBeginWhileTurnActive()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.BeginTurn("req-20260304T113901Z-task-002"));

        Assert.Contains("already in progress", exception.Message);
    }

    [Fact]
    public void ConcurrentTurnPrevention_CanBeginAfterComplete()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        _fakeState.BeginTurn("req-20260304T113901Z-task-002");
        Assert.Equal("req-20260304T113901Z-task-002", _fakeState.CurrentTurnRequestId);
    }

    [Fact]
    public void ConcurrentTurnPrevention_CanBeginAfterFail()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.FailTurn();

        _fakeState.BeginTurn("req-20260304T113901Z-task-002");
        Assert.Equal("req-20260304T113901Z-task-002", _fakeState.CurrentTurnRequestId);
    }

    #endregion

    #region Client Response Validation Tests

    [Fact]
    public async Task ClientResponse_SubmitAsync_ReturnsCorrectStructure()
    {
        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "Copilot-20260304T113901Z-test",
            Title = "Test",
            Model = "claude-sonnet-4"
        };

        var result = await _stubClient.SubmitAsync(sessionLog, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(sessionLog.SourceType, result.SourceType);
        Assert.Equal(sessionLog.SessionId, result.SessionId);
    }

    [Fact]
    public async Task ClientResponse_QueryAsync_ReturnsCorrectStructure()
    {
        var result = await _stubClient.QueryAsync(agent: "Copilot", limit: 15, offset: 5, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.True(result.TotalCount >= 0);
        Assert.Equal(15, result.Limit);
        Assert.Equal(5, result.Offset);
    }

    [Fact]
    public async Task ClientResponse_AppendDialogAsync_ReturnsCorrectStructure()
    {
        var items = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Test", Category = "reasoning" }
        };

        var result = await _stubClient.AppendDialogAsync("Copilot", "session-1", "req-1", items, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Copilot", result.Agent);
        Assert.Equal("session-1", result.SessionId);
        Assert.Equal("req-1", result.RequestId);
        Assert.True(result.TotalDialogCount > 0);
    }

    #endregion

    #region Session State Metadata Tests

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 (appended 2026-11-03): opening a session records every metadata field and the scripted
    /// open time exactly as Started and LastUpdated, from a test-owned clock (hostile validation round 10 finding
    /// R10-F3: the case formerly compared the times with the live clock).
    /// </summary>
    [Fact]
    public void SessionMetadata_AfterOpenSession_ContainsAllFields()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-metadata";
        var title = "Metadata Test Session";
        var model = "claude-sonnet-4-20250514";
        var opened = new DateTimeOffset(2026, 3, 4, 11, 39, 1, TimeSpan.Zero);
        var state = new FakeSessionLogState(new ScriptedClock(opened).Next);

        state.OpenSession(agent, sessionId, title, model);

        Assert.Equal(agent, state.Agent);
        Assert.Equal(sessionId, state.SessionId);
        Assert.Equal(title, state.Title);
        Assert.Equal(model, state.Model);
        Assert.Equal("in_progress", state.Status);
        RecordedTimeContractTests.AssertRecordedTimes([opened, opened], [state.Started, state.LastUpdated], "open session");
        Assert.Equal(0, state.TurnCount);
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 (appended 2026-11-03): open, begin, update and complete each record the next scripted
    /// time exactly as LastUpdated, with no sleep (hostile validation round 10 finding R10-F3: the case formerly slept to
    /// separate live-clock times).
    /// </summary>
    [Fact]
    public void SessionMetadata_LastUpdated_UpdatesOnChanges()
    {
        var first = new DateTimeOffset(2026, 3, 4, 11, 39, 1, TimeSpan.Zero);
        DateTimeOffset[] times = [first, first.AddSeconds(1), first.AddSeconds(2), first.AddSeconds(3)];
        var state = new FakeSessionLogState(new ScriptedClock(times).Next);
        var recorded = new List<DateTimeOffset>();

        state.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        recorded.Add(state.LastUpdated);
        state.BeginTurn("req-20260304T113901Z-task-001");
        recorded.Add(state.LastUpdated);
        state.UpdateTurn();
        recorded.Add(state.LastUpdated);
        state.CompleteTurn();
        recorded.Add(state.LastUpdated);

        RecordedTimeContractTests.AssertRecordedTimes(times, recorded, "session transitions");
    }

    #endregion
}
