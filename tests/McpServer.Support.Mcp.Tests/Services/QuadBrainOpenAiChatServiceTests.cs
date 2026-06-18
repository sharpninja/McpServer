using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBOPENAI-001: Verifies the OpenAI-compatible chat surface (FR-MCP-QBOPENAI-001) maps an inbound
/// OpenAI chat request onto QuadBrain orchestration and returns an OpenAI-shaped response carrying the Arbiter
/// output, and rejects an empty message list.
/// </summary>
public sealed class QuadBrainOpenAiChatServiceTests
{
    /// <summary>The full role-tagged transcript is sent to orchestration and the Arbiter output is returned as the assistant message.</summary>
    [Fact]
    public async Task CompleteAsync_MapsTranscriptToOrchestrationAndArbiterOutputToAssistant()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "the arbiter answer",
            TransactionId = "txn-9",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Model = "qbagent",
            Messages =
            [
                new OpenAiChatMessage { Role = "system", Content = "be precise" },
                new OpenAiChatMessage { Role = "user", Content = "implement the feature" },
            ],
        };

        var response = await service.CompleteAsync(request).ConfigureAwait(true);

        Assert.Equal("chat.completion", response.Object);
        Assert.Equal("qbagent", response.Model);
        var choice = Assert.Single(response.Choices);
        Assert.Equal("assistant", choice.Message.Role);
        Assert.Equal("the arbiter answer", choice.Message.Content);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Contains("txn-9", response.Id, StringComparison.Ordinal);

        Assert.NotNull(orchestration.LastRequest);
        Assert.Contains("implement the feature", orchestration.LastRequest!.Input, StringComparison.Ordinal);
        Assert.Contains("be precise", orchestration.LastRequest.Input, StringComparison.Ordinal);
    }

    /// <summary>When QuadBrain elects an EXTERNAL tool, the response carries it as an OpenAI tool call for the agent.</summary>
    [Fact]
    public async Task CompleteAsync_OrchestrationEmitsToolCallJson_ReturnsToolCalls()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"edit_local_file\",\"arguments\":{\"path\":\"a.txt\",\"content\":\"x\"}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "write the file" }],
            Tools =
            [
                new OpenAiToolDefinition { Function = new OpenAiFunctionDefinition { Name = "edit_local_file", Description = "write a local file (agent-side)" } },
            ],
        };

        var response = await service.CompleteAsync(request).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        Assert.Null(choice.Message.Content);
        var call = Assert.Single(choice.Message.ToolCalls!);
        Assert.Equal("edit_local_file", call.Function.Name);
        Assert.Equal("function", call.Type);
        Assert.Contains("a.txt", call.Function.Arguments, StringComparison.Ordinal);
        // Tool definitions are surfaced to orchestration so the model knows what it can call.
        Assert.Contains("edit_local_file", orchestration.LastRequest!.Input, StringComparison.Ordinal);
    }

    /// <summary>Plain (non-JSON) output is returned as assistant content, not tool calls.</summary>
    [Fact]
    public async Task CompleteAsync_PlainOutput_ReturnsContent()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "here is the plan",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "plan it" }],
        };

        var response = await service.CompleteAsync(request).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Equal("here is the plan", choice.Message.Content);
        Assert.Null(choice.Message.ToolCalls);
    }

    /// <summary>MCP-internal tool calls are executed server-side and stripped; external calls reach the agent.</summary>
    [Fact]
    public async Task CompleteAsync_StripsInternalToolCalls_KeepsExternal()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{\"id\":\"X\"}},{\"name\":\"do_local\",\"arguments\":{}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration, classifier: null, internalToolExecutor: new HandlingExecutor("mcp_todo_update"));

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] })
            .ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        var call = Assert.Single(choice.Message.ToolCalls!);
        Assert.Equal("do_local", call.Function.Name);
    }

    /// <summary>When every elected tool is internal and executes, no tool calls are emitted to the agent.</summary>
    [Fact]
    public async Task CompleteAsync_AllInternalExecuted_EmitsNoToolCalls()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration, classifier: null, internalToolExecutor: new HandlingExecutor("mcp_todo_update"));

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] })
            .ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
    }

    /// <summary>A failed internal tool is surfaced as a note (assistant content), not emitted as a tool call.</summary>
    [Fact]
    public async Task CompleteAsync_InternalToolFailure_BecomesNoteNotToolCall()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration, classifier: null, internalToolExecutor: new FailingExecutor("mcp_todo_update"));

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] })
            .ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.Contains("Note", choice.Message.Content!, StringComparison.Ordinal);
        Assert.Contains("mcp_todo_update", choice.Message.Content!, StringComparison.Ordinal);
    }

    /// <summary>An empty message list is rejected.</summary>
    [Fact]
    public async Task CompleteAsync_NoMessages_Throws()
    {
        var service = new QuadBrainOpenAiChatService(
            new CapturingOrchestrationService(new QuadBrainOrchestrationResponse { Status = "committed" }));

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CompleteAsync(new OpenAiChatCompletionRequest())).ConfigureAwait(true);
    }

    private sealed class HandlingExecutor(string handledToolName) : IQuadBrainInternalToolExecutor
    {
        public Task<InternalToolExecutionOutcome> TryExecuteAsync(
            OpenAiToolCall toolCall,
            string? turnId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(toolCall.Function.Name == handledToolName
                ? InternalToolExecutionOutcome.Ok()
                : InternalToolExecutionOutcome.Unhandled);
    }

    private sealed class FailingExecutor(string failedToolName) : IQuadBrainInternalToolExecutor
    {
        public Task<InternalToolExecutionOutcome> TryExecuteAsync(
            OpenAiToolCall toolCall,
            string? turnId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(toolCall.Function.Name == failedToolName
                ? InternalToolExecutionOutcome.Fail("transaction rejected")
                : InternalToolExecutionOutcome.Unhandled);
    }

    private sealed class CapturingOrchestrationService(QuadBrainOrchestrationResponse response)
        : IQuadBrainOrchestrationService
    {
        public QuadBrainOrchestrationRequest? LastRequest { get; private set; }

        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }

        public Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
            AotReconciliationRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
            QuadBrainWeightUpdateRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
