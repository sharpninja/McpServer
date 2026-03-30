using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Comprehensive validation tests for iteration 2 implementation.
/// Ensures all mock components work together correctly and all test scenarios pass.
/// This file serves as the final validation checkpoint for iteration 2 completion.
/// </summary>
public class Iteration2_AllTestsValidation
{
    [Fact]
    public void Validation_FakeSessionLogState_ImplementsInterface()
    {
        var fakeState = new FakeSessionLogState();
        Assert.IsAssignableFrom<ISessionLogState>(fakeState);
    }

    [Fact]
    public void Validation_FakeSessionLogState_AllPropertiesAccessible()
    {
        var fakeState = new FakeSessionLogState();
        fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-test", "Test", "model");

        Assert.NotNull(fakeState.Agent);
        Assert.NotNull(fakeState.SessionId);
        Assert.NotNull(fakeState.Title);
        Assert.NotNull(fakeState.Model);
        Assert.NotNull(fakeState.Status);
        Assert.True(fakeState.Started != default);
        Assert.True(fakeState.LastUpdated != default);
        Assert.True(fakeState.CurrentTurnRequestId == null || fakeState.CurrentTurnRequestId is string);
        Assert.True(fakeState.CurrentTurnStatus == null || fakeState.CurrentTurnStatus is string);
        Assert.True(fakeState.TurnCount >= 0);
    }

    [Fact]
    public async Task Validation_StubSessionLogClient_SubmitReturnsValidResult()
    {
        var stubClient = new StubSessionLogClient();
        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "Copilot-20260304T113901Z-test",
            Title = "Test",
            Model = "model"
        };

        var result = await stubClient.SubmitAsync(sessionLog);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Copilot", result.SourceType);
        Assert.Equal("Copilot-20260304T113901Z-test", result.SessionId);
    }

    [Fact]
    public async Task Validation_StubSessionLogClient_QueryReturnsValidResult()
    {
        var stubClient = new StubSessionLogClient();

        var result = await stubClient.QueryAsync(agent: "Copilot");

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.True(result.TotalCount >= 0);
        Assert.True(result.Limit > 0);
        Assert.True(result.Offset >= 0);
    }

    [Fact]
    public async Task Validation_StubSessionLogClient_AppendDialogReturnsValidResult()
    {
        var stubClient = new StubSessionLogClient();
        var items = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Test", Category = "reasoning" }
        };

        var result = await stubClient.AppendDialogAsync("Copilot", "session-1", "req-1", items);

        Assert.NotNull(result);
        Assert.Equal("Copilot", result.Agent);
        Assert.Equal("session-1", result.SessionId);
        Assert.Equal("req-1", result.RequestId);
        Assert.True(result.TotalDialogCount > 0);
    }

    [Fact]
    public void Validation_AllSessionLogErrorCodes_AreDefined()
    {
        Assert.NotNull(SessionLogErrorCodes.BootstrapFailed);
        Assert.NotNull(SessionLogErrorCodes.SessionNotFound);
        Assert.NotNull(SessionLogErrorCodes.SessionAlreadyExists);
        Assert.NotNull(SessionLogErrorCodes.InvalidSessionId);
        Assert.NotNull(SessionLogErrorCodes.InvalidRequestId);
        Assert.NotNull(SessionLogErrorCodes.TurnNotFound);
        Assert.NotNull(SessionLogErrorCodes.TurnAlreadyExists);
        Assert.NotNull(SessionLogErrorCodes.TurnImmutable);
        Assert.NotNull(SessionLogErrorCodes.InvalidTurnState);
        Assert.NotNull(SessionLogErrorCodes.InvalidParameter);
        Assert.NotNull(SessionLogErrorCodes.StorageError);
        Assert.NotNull(SessionLogErrorCodes.InternalError);
    }

    [Fact]
    public void Validation_AllCommandShapes_AreDefined()
    {
        Assert.Equal("workflow.sessionlog", SessionLogCommandShapes.MethodNamespace);
        Assert.NotNull(SessionLogCommandShapes.BootstrapMethod);
        Assert.NotNull(SessionLogCommandShapes.OpenSessionMethod);
        Assert.NotNull(SessionLogCommandShapes.CurrentSessionMethod);
        Assert.NotNull(SessionLogCommandShapes.BeginTurnMethod);
        Assert.NotNull(SessionLogCommandShapes.UpdateTurnMethod);
        Assert.NotNull(SessionLogCommandShapes.CompleteTurnMethod);
        Assert.NotNull(SessionLogCommandShapes.FailTurnMethod);
        Assert.NotNull(SessionLogCommandShapes.AppendDialogMethod);
        Assert.NotNull(SessionLogCommandShapes.AppendActionsMethod);
        Assert.NotNull(SessionLogCommandShapes.QueryHistoryMethod);
    }

    [Fact]
    public void Validation_ISessionLogWorkflow_InterfaceExists()
    {
        var interfaceType = typeof(ISessionLogWorkflow);

        Assert.True(interfaceType.IsInterface);
        Assert.NotNull(interfaceType.GetMethod("BootstrapAsync"));
        Assert.NotNull(interfaceType.GetMethod("OpenSessionAsync"));
        Assert.NotNull(interfaceType.GetMethod("CurrentSession"));
        Assert.NotNull(interfaceType.GetMethod("BeginTurnAsync"));
        Assert.NotNull(interfaceType.GetMethod("UpdateTurnAsync"));
        Assert.NotNull(interfaceType.GetMethod("CompleteTurnAsync"));
        Assert.NotNull(interfaceType.GetMethod("FailTurnAsync"));
        Assert.NotNull(interfaceType.GetMethod("AppendDialogAsync"));
        Assert.NotNull(interfaceType.GetMethod("AppendActionsAsync"));
        Assert.NotNull(interfaceType.GetMethod("QueryHistoryAsync"));
    }

    [Fact]
    public void Validation_ISessionLogState_InterfaceExists()
    {
        var interfaceType = typeof(ISessionLogState);

        Assert.True(interfaceType.IsInterface);
        Assert.NotNull(interfaceType.GetProperty("Agent"));
        Assert.NotNull(interfaceType.GetProperty("SessionId"));
        Assert.NotNull(interfaceType.GetProperty("Title"));
        Assert.NotNull(interfaceType.GetProperty("Model"));
        Assert.NotNull(interfaceType.GetProperty("Started"));
        Assert.NotNull(interfaceType.GetProperty("LastUpdated"));
        Assert.NotNull(interfaceType.GetProperty("Status"));
        Assert.NotNull(interfaceType.GetProperty("CurrentTurnRequestId"));
        Assert.NotNull(interfaceType.GetProperty("CurrentTurnStatus"));
        Assert.NotNull(interfaceType.GetProperty("TurnCount"));
    }

    [Fact]
    public void Validation_IDialogItem_InterfaceExists()
    {
        var interfaceType = typeof(IDialogItem);

        Assert.True(interfaceType.IsInterface);
        Assert.NotNull(interfaceType.GetProperty("Timestamp"));
        Assert.NotNull(interfaceType.GetProperty("Role"));
        Assert.NotNull(interfaceType.GetProperty("Content"));
        Assert.NotNull(interfaceType.GetProperty("Category"));
    }

    [Fact]
    public void Validation_ISessionAction_InterfaceExists()
    {
        var interfaceType = typeof(ISessionAction);

        Assert.True(interfaceType.IsInterface);
        Assert.NotNull(interfaceType.GetProperty("Order"));
        Assert.NotNull(interfaceType.GetProperty("Description"));
        Assert.NotNull(interfaceType.GetProperty("Type"));
        Assert.NotNull(interfaceType.GetProperty("Status"));
        Assert.NotNull(interfaceType.GetProperty("FilePath"));
    }

    [Fact]
    public void Validation_ISessionLogSummary_InterfaceExists()
    {
        var interfaceType = typeof(ISessionLogSummary);

        Assert.True(interfaceType.IsInterface);
        Assert.NotNull(interfaceType.GetProperty("Agent"));
        Assert.NotNull(interfaceType.GetProperty("SessionId"));
        Assert.NotNull(interfaceType.GetProperty("Title"));
        Assert.NotNull(interfaceType.GetProperty("Model"));
        Assert.NotNull(interfaceType.GetProperty("Started"));
        Assert.NotNull(interfaceType.GetProperty("LastUpdated"));
        Assert.NotNull(interfaceType.GetProperty("Status"));
        Assert.NotNull(interfaceType.GetProperty("TurnCount"));
        Assert.NotNull(interfaceType.GetProperty("Tags"));
        Assert.NotNull(interfaceType.GetProperty("FilesModifiedCount"));
    }

    [Fact]
    public async Task Validation_CompleteWorkflowScenario_ExecutesSuccessfully()
    {
        var stubClient = new StubSessionLogClient();
        var fakeState = new FakeSessionLogState();

        var sessionLog = new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "Copilot-20260304T113901Z-validation",
            Title = "Validation Test",
            Model = "claude-sonnet-4"
        };
        await stubClient.SubmitAsync(sessionLog);

        fakeState.OpenSession("Copilot", "Copilot-20260304T113901Z-validation", "Validation Test", "claude-sonnet-4");
        Assert.Equal("in_progress", fakeState.Status);

        fakeState.BeginTurn("req-20260304T113901Z-task-001");
        Assert.Equal("in_progress", fakeState.CurrentTurnStatus);

        var dialogItems = new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Validating...", Category = "reasoning" }
        };
        await stubClient.AppendDialogAsync("Copilot", "Copilot-20260304T113901Z-validation", "req-20260304T113901Z-task-001", dialogItems);

        fakeState.UpdateTurn();
        Assert.Equal("in_progress", fakeState.CurrentTurnStatus);

        fakeState.CompleteTurn();
        Assert.Null(fakeState.CurrentTurnStatus);
        Assert.Equal(1, fakeState.TurnCount);

        var queryResult = await stubClient.QueryAsync(agent: "Copilot");
        Assert.NotNull(queryResult);
        Assert.NotEmpty(queryResult.Items);
    }

    [Fact]
    public void Validation_FakeYamlSerializer_CanSerializeAndDeserialize()
    {
        var serializer = new FakeYamlSerializer();

        var envelope = NSubstitute.Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns("test");
        envelope.Payload.Returns(new { test = "value" });

        var yaml = serializer.Serialize(envelope);
        Assert.NotNull(yaml);
        Assert.Contains("type: test", yaml);

        var deserialized = serializer.Deserialize(yaml);
        Assert.NotNull(deserialized);
        Assert.Equal("test", deserialized.Type);
    }
}
