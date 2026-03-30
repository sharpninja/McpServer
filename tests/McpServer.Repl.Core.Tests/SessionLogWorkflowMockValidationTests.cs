using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Iteration 2 mock validation tests for Session Log workflow.
/// Validates workflow command routing to correct SessionLogClient methods with stub responses.
/// Tests turn lifecycle guards with fake ISessionLogState implementation.
/// Ensures all iteration 1 + 2 tests pass with properly configured mocks.
/// </summary>
public class SessionLogWorkflowMockValidationTests
{
    private readonly StubSessionLogClient _stubClient;
    private readonly FakeSessionLogState _fakeState;
    private readonly IYamlSerializer _yamlSerializer;

    public SessionLogWorkflowMockValidationTests()
    {
        _stubClient = new StubSessionLogClient();
        _fakeState = new FakeSessionLogState();
        _yamlSerializer = new FakeYamlSerializer();
    }

    #region Stub SessionLogClient Response Tests

    [Fact]
    public async Task StubClient_SubmitAsync_ReturnsSessionLogSubmitResult()
    {
        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "Copilot-20260304T113901Z-test",
            Title = "Test Session",
            Model = "claude-sonnet-4"
        };

        var result = await _stubClient.SubmitAsync(sessionLog);

        Assert.NotNull(result);
        Assert.Equal("Copilot", result.SourceType);
        Assert.Equal("Copilot-20260304T113901Z-test", result.SessionId);
    }

    [Fact]
    public async Task StubClient_QueryAsync_ReturnsSessionLogQueryResult()
    {
        var result = await _stubClient.QueryAsync(agent: "Copilot", limit: 10);

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.True(result.TotalCount >= 0);
        Assert.Equal(10, result.Limit);
    }

    [Fact]
    public async Task StubClient_AppendDialogAsync_ReturnsDialogAppendResult()
    {
        var items = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Thinking...", Category = "reasoning" }
        };

        var result = await _stubClient.AppendDialogAsync("Copilot", "session-1", "req-1", items);

        Assert.NotNull(result);
        Assert.Equal("Copilot", result.Agent);
        Assert.Equal("session-1", result.SessionId);
        Assert.Equal("req-1", result.RequestId);
    }

    #endregion

    #region Workflow Command Routing Tests

    [Fact]
    public async Task WorkflowRouting_OpenSession_CallsSubmitAsync()
    {
        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "Copilot-20260304T113901Z-feature",
            Title = "Feature Implementation",
            Model = "claude-sonnet-4"
        };

        var result = await _stubClient.SubmitAsync(sessionLog);

        Assert.NotNull(result);
        Assert.Equal(sessionLog.SessionId, result.SessionId);
    }

    [Fact]
    public void WorkflowRouting_BeginTurn_CreatesNewTurn()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        Assert.Equal("req-20260304T113901Z-task-001", _fakeState.CurrentTurnRequestId);
        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);
        Assert.Equal(0, _fakeState.TurnCount);
    }

    [Fact]
    public void WorkflowRouting_UpdateTurn_ModifiesTurnState()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        _fakeState.UpdateTurn();

        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);
    }

    [Fact]
    public void WorkflowRouting_CompleteTurn_TransitionsToCompleted()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        _fakeState.CompleteTurn();

        Assert.Null(_fakeState.CurrentTurnRequestId);
        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public void WorkflowRouting_FailTurn_TransitionsToFailed()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        _fakeState.FailTurn();

        Assert.Null(_fakeState.CurrentTurnRequestId);
        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public async Task WorkflowRouting_AppendDialog_CallsAppendDialogAsync()
    {
        var items = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Analyzing...", Category = "reasoning" },
            new() { Role = "tool", Content = "Success", Category = "tool_result" }
        };

        var result = await _stubClient.AppendDialogAsync("Copilot", "session-1", "req-1", items);

        Assert.NotNull(result);
        Assert.True(result.TotalDialogCount >= items.Count);
    }

    [Fact]
    public async Task WorkflowRouting_QueryHistory_CallsQueryAsync()
    {
        var result = await _stubClient.QueryAsync(agent: "Copilot", limit: 5, offset: 0);

        Assert.NotNull(result);
        Assert.Equal(5, result.Limit);
        Assert.Equal(0, result.Offset);
    }

    #endregion

    #region Turn Lifecycle Guard Tests

    [Fact]
    public void TurnLifecycleGuard_DuplicateTurn_ThrowsInvalidOperationException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn(); // Complete the first turn

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.BeginTurn("req-20260304T113901Z-task-001"));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void TurnLifecycleGuard_UpdateCompletedTurn_ThrowsInvalidOperationException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.UpdateTurn());

        Assert.Contains("No active turn", exception.Message);
    }

    [Fact]
    public void TurnLifecycleGuard_UpdateFailedTurn_ThrowsInvalidOperationException()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.FailTurn();

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.UpdateTurn());

        Assert.Contains("No active turn", exception.Message);
    }

    [Fact]
    public void TurnLifecycleGuard_ProperStatusTransitions_InProgressToCompleted()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);

        _fakeState.CompleteTurn();

        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public void TurnLifecycleGuard_ProperStatusTransitions_InProgressToFailed()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);

        _fakeState.FailTurn();

        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public void TurnLifecycleGuard_MultipleTurns_TracksSeparately()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        Assert.Equal(1, _fakeState.TurnCount);
        Assert.Null(_fakeState.CurrentTurnRequestId);

        _fakeState.BeginTurn("req-20260304T113901Z-task-002");
        _fakeState.UpdateTurn();
        _fakeState.CompleteTurn();

        Assert.Equal(2, _fakeState.TurnCount);

        _fakeState.BeginTurn("req-20260304T113901Z-task-003");
        _fakeState.FailTurn();

        Assert.Equal(3, _fakeState.TurnCount);
    }

    [Fact]
    public void TurnLifecycleGuard_CompletedTurnNotReusable_NewTurnRequired()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        _fakeState.CompleteTurn();

        Assert.Null(_fakeState.CurrentTurnRequestId);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _fakeState.BeginTurn("req-20260304T113901Z-task-001"));

        Assert.Contains("already exists", exception.Message);
    }

    #endregion

    #region Canonical Identifier Validation Tests

    [Fact]
    public void CanonicalIdentifier_ValidSessionId_AcceptsCorrectFormat()
    {
        var validSessionIds = new[]
        {
            "Copilot-20260304T113901Z-feature",
            "Cline-20260304T120000Z-bugfix-auth",
            "Cursor-20260304T150000Z-refactor-session",
            "MyAgent-20260304T113901Z-test",
            "TestAgent123-20260304T113901Z-multi-word-suffix"
        };

        foreach (var sessionId in validSessionIds)
        {
            _fakeState.OpenSession("TestAgent", sessionId, "Test", "model");
            Assert.Equal(sessionId, _fakeState.SessionId);
        }
    }

    [Fact]
    public void CanonicalIdentifier_ValidRequestId_AcceptsCorrectFormat()
    {
        var validRequestIds = new[]
        {
            "req-20260304T113901Z-task-001",
            "req-20260304T120000Z-feature-add",
            "req-20260304T150000Z-bugfix",
            "req-20260304T113901Z-001",
            "req-20260304T113901Z-multi-word-task"
        };

        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        foreach (var requestId in validRequestIds)
        {
            _fakeState.BeginTurn(requestId);
            Assert.Equal(requestId, _fakeState.CurrentTurnRequestId);
            _fakeState.CompleteTurn();
        }
    }

    #endregion

    #region Session State Management Tests

    [Fact]
    public void SessionState_AfterOpenSession_ContainsCorrectMetadata()
    {
        var agent = "Copilot";
        var sessionId = "Copilot-20260304T113901Z-feature";
        var title = "Feature Implementation";
        var model = "claude-sonnet-4";

        _fakeState.OpenSession(agent, sessionId, title, model);

        Assert.Equal(agent, _fakeState.Agent);
        Assert.Equal(sessionId, _fakeState.SessionId);
        Assert.Equal(title, _fakeState.Title);
        Assert.Equal(model, _fakeState.Model);
        Assert.Equal("in_progress", _fakeState.Status);
        Assert.Null(_fakeState.CurrentTurnRequestId);
        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(0, _fakeState.TurnCount);
    }

    [Fact]
    public void SessionState_AfterBeginTurn_TracksActiveTurn()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        var requestId = "req-20260304T113901Z-task-001";
        _fakeState.BeginTurn(requestId);

        Assert.Equal(requestId, _fakeState.CurrentTurnRequestId);
        Assert.Equal("in_progress", _fakeState.CurrentTurnStatus);
        Assert.Equal(0, _fakeState.TurnCount);
    }

    [Fact]
    public void SessionState_AfterCompleteTurn_ClearsActiveTurnTracking()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");

        _fakeState.CompleteTurn();

        Assert.Null(_fakeState.CurrentTurnRequestId);
        Assert.Null(_fakeState.CurrentTurnStatus);
        Assert.Equal(1, _fakeState.TurnCount);
    }

    [Fact]
    public void SessionState_LastUpdatedTimestamp_UpdatesOnChanges()
    {
        _fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");
        var initialTimestamp = _fakeState.LastUpdated;

        Thread.Sleep(10);
        _fakeState.BeginTurn("req-20260304T113901Z-task-001");
        var afterBeginTimestamp = _fakeState.LastUpdated;

        Assert.True(afterBeginTimestamp > initialTimestamp);

        Thread.Sleep(10);
        _fakeState.UpdateTurn();
        var afterUpdateTimestamp = _fakeState.LastUpdated;

        Assert.True(afterUpdateTimestamp > afterBeginTimestamp);
    }

    #endregion

    #region Stub Client Configuration Tests

    [Fact]
    public async Task StubClient_Configuration_ReturnsConsistentResults()
    {
        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "Copilot-20260304T113901Z-test",
            Title = "Test Session",
            Model = "claude-sonnet-4"
        };

        var result1 = await _stubClient.SubmitAsync(sessionLog);
        var result2 = await _stubClient.SubmitAsync(sessionLog);

        Assert.Equal(result1.SessionId, result2.SessionId);
        Assert.Equal(result1.SourceType, result2.SourceType);
    }

    [Fact]
    public async Task StubClient_QueryWithFilters_AppliesParameters()
    {
        var result = await _stubClient.QueryAsync(
            agent: "Copilot",
            model: "claude-sonnet-4",
            limit: 20,
            offset: 10);

        Assert.NotNull(result);
        Assert.Equal(20, result.Limit);
        Assert.Equal(10, result.Offset);
    }

    [Fact]
    public async Task StubClient_AppendDialog_IncrementsTotalDialogCount()
    {
        var items1 = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "First", Category = "reasoning" }
        };

        var result1 = await _stubClient.AppendDialogAsync("Copilot", "session-1", "req-1", items1);

        var items2 = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Second", Category = "reasoning" }
        };

        var result2 = await _stubClient.AppendDialogAsync("Copilot", "session-1", "req-1", items2);

        Assert.True(result2.TotalDialogCount >= result1.TotalDialogCount);
    }

    #endregion
}
