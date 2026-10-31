using System.Text.Json;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

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

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

    [Fact]
    public async Task CompleteAsync_ToolCallsJsonEmbeddedInProse_ReturnsExternalToolCalls()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "Both roles agree. {\"tool_calls\":[{\"name\":\"write_file\",\"arguments\":{\"path\":\"a.cs\"}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "write the file" }],
            Tools =
            [
                new OpenAiToolDefinition { Function = new OpenAiFunctionDefinition { Name = "write_file", Description = "write a local file" } },
            ],
        };

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        var call = Assert.Single(choice.Message.ToolCalls!);
        Assert.Equal("write_file", call.Function.Name);
        Assert.Contains("a.cs", call.Function.Arguments, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_McpTodoQuery_ReturnsNormalAssistantContent()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_query\",\"arguments\":{\"done\":false}}]}",
        });
        var service = new QuadBrainOpenAiChatService(
            orchestration,
            classifier: null,
            internalToolExecutor: new HandlingExecutor("mcp_todo_query", """{"items":[{"id":"PLAN-X-001","title":"Do it","done":false}],"totalCount":1}"""));

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "list open todo" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.Contains("PLAN-X-001", choice.Message.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_ArbiterDeferredProse_UsesLogicToolCalls()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "I'll inspect the workspace and MCP surface so the decision is traceable, then reconcile.",
            RoleResults =
            [
                new QuadBrainRoleResult
                {
                    Role = BrainSlotRoles.Logic,
                    Output = "{\"tool_calls\":[{\"name\":\"read_file\",\"arguments\":{\"path\":\"AGENTS-README-FIRST.yaml\"}}]}",
                },
            ],
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "update voice chat" }],
            Tools = [Tool("read_file")],
        };

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        Assert.Equal("read_file", Assert.Single(choice.Message.ToolCalls!).Function.Name);
        Assert.Contains("AGENTS-README-FIRST.yaml", choice.Message.ToolCalls![0].Function.Arguments, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_InternalTool_LoopsUntilProseAnswer()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_query\",\"arguments\":{\"done\":false}}]}",
            },
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = """{"intent":"final","content":"PLAN-X-001 is the open work."}""",
            });
        var service = new QuadBrainOpenAiChatService(
            orchestration,
            classifier: null,
            internalToolExecutor: new HandlingExecutor("mcp_todo_query", """{"items":[{"id":"PLAN-X-001"}]}"""));

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "list open todo" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, orchestration.CallCount);
        Assert.Contains("PLAN-X-001", orchestration.Requests[1].Input, StringComparison.Ordinal);
        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Contains("PLAN-X-001 is the open work", choice.Message.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_ProviderFailed_UsesCreativityFinalInsteadOfLooping()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse
            {
                Status = "rejected",
                Reason = BrainSlotReasonCodes.ProviderFailed,
                Output = string.Empty,
                RoleResults =
                [
                    new QuadBrainRoleResult
                    {
                        Role = BrainSlotRoles.Creativity,
                        Output = """{"intent":"final","content":"W1: set AgentModel QuadBrain on the web VM."}""",
                    },
                ],
            },
            new QuadBrainOrchestrationResponse
            {
                Status = "rejected",
                Reason = BrainSlotReasonCodes.ProviderFailed,
                Output = string.Empty,
            });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest
            {
                Messages = [new OpenAiChatMessage { Role = "user", Content = "do it" }],
                Tools = [Tool("read_file")],
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, orchestration.CallCount);
        Assert.Equal("stop", Assert.Single(response.Choices).FinishReason);
        Assert.Equal("W1: set AgentModel QuadBrain on the web VM.", response.Choices[0].Message.Content);
        Assert.DoesNotContain("ProviderFailed", response.Choices[0].Message.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_ArbiterFinalIntent_ReturnsContentWithoutEnvelope()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = """{"intent":"final","content":"Voice chat should call QuadBrain."}""",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "update voice chat" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Equal("Voice chat should call QuadBrain.", choice.Message.Content);
        Assert.DoesNotContain("intent", choice.Message.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_ArbiterContinueIntent_RePromptsThenTools()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = """{"intent":"continue","content":"Locating Common Voice Chat."}""",
            },
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = """{"intent":"tool_calls","tool_calls":[{"name":"read_file","arguments":{"path":"AGENTS-README-FIRST.yaml"}}]}""",
            });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "update voice chat" }],
            Tools = [Tool("read_file")],
        };

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(2, orchestration.CallCount);
        Assert.Contains(QuadBrainOpenAiChatService.ContinueDirective, orchestration.Requests[1].Input, StringComparison.Ordinal);
        Assert.Equal("tool_calls", Assert.Single(response.Choices).FinishReason);
        Assert.Equal("read_file", Assert.Single(response.Choices[0].Message.ToolCalls!).Function.Name);
    }

    [Fact]
    public async Task CompleteAsync_LiveArbiterDeferredProse_RePromptsInsteadOfStopping()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = "Neither role inspected the repo, so I am loading the required MCP skills and locating Common Voice Chat and QuadBrain before reconciling a decision.",
            },
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = "{\"tool_calls\":[{\"name\":\"read_file\",\"arguments\":{\"path\":\"AGENTS-README-FIRST.yaml\"}}]}",
            });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "update the common voice chat to use QuadBrain." }],
            Tools = [Tool("read_file")],
        };

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(2, orchestration.CallCount);
        Assert.Contains(QuadBrainOpenAiChatService.ContinueDirective, orchestration.Requests[1].Input, StringComparison.Ordinal);
        Assert.Equal("tool_calls", Assert.Single(response.Choices).FinishReason);
        Assert.Equal("read_file", Assert.Single(response.Choices[0].Message.ToolCalls!).Function.Name);
    }

    [Fact]
    public async Task CompleteAsync_DeferredWithoutTools_RePromptsThenReturnsAnswer()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = "I'll inspect the workspace, then reconcile.",
            },
            new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = """{"intent":"final","content":"Voice chat should call QuadBrain."}""",
            });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest
            {
                Messages = [new OpenAiChatMessage { Role = "user", Content = "update voice chat" }],
                Tools = [Tool("read_file")],
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, orchestration.CallCount);
        Assert.Contains(QuadBrainOpenAiChatService.ContinueDirective, orchestration.Requests[1].Input, StringComparison.Ordinal);
        Assert.Equal("stop", Assert.Single(response.Choices).FinishReason);
        Assert.Contains("Voice chat should call QuadBrain", response.Choices[0].Message.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_IncomingToolResult_IsFoldedIntoOrchestrationInput()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "next step after the file",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages =
            [
                new OpenAiChatMessage { Role = "user", Content = "update voice chat" },
                new OpenAiChatMessage { Role = "tool", ToolCallId = "call_0", Content = "AGENTS-README-FIRST.yaml contents" },
            ],
        };

        await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains("AGENTS-README-FIRST.yaml contents", orchestration.LastRequest!.Input, StringComparison.Ordinal);
        Assert.Contains("tool [call_0]", orchestration.LastRequest.Input, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-018): multiple external tool calls are preserved for the agent loop.</summary>
    [Fact]
    public async Task CompleteAsync_OrchestrationEmitsMultipleToolCalls_ReturnsAllToolCalls()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"edit_local_file\",\"arguments\":{\"path\":\"a.txt\"}},{\"name\":\"run_tests\",\"arguments\":{\"filter\":\"QBAgent\"}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "do both" }] }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        Assert.Collection(
            choice.Message.ToolCalls!,
            first => Assert.Equal("edit_local_file", first.Function.Name),
            second => Assert.Equal("run_tests", second.Function.Name));
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-017): <c>tool_choice: none</c> suppresses tool calls and withholds tool definitions from orchestration.</summary>
    [Fact]
    public async Task CompleteAsync_ToolChoiceNone_SuppressesToolCallsAndToolPrompt()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"edit_local_file\",\"arguments\":{\"path\":\"a.txt\"}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "answer only" }],
            Tools = [Tool("edit_local_file")],
            ToolChoice = ToolChoice("\"none\""),
        };

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.Contains("tool_calls", choice.Message.Content!, StringComparison.Ordinal);
        Assert.DoesNotContain("Available tools", orchestration.LastRequest!.Input, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-017): <c>tool_choice: required</c> rejects requests that provide no callable tools.</summary>
    [Fact]
    public async Task CompleteAsync_ToolChoiceRequiredWithoutTools_Throws()
    {
        var service = new QuadBrainOpenAiChatService(new CapturingOrchestrationService(new QuadBrainOrchestrationResponse()));
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "call a tool" }],
            ToolChoice = ToolChoice("\"required\""),
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Contains("requires at least one tool", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-017): a specific function choice must match a declared tool and is surfaced when selected.</summary>
    [Fact]
    public async Task CompleteAsync_SpecificToolChoice_ReturnsMatchingToolCall()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"edit_local_file\",\"arguments\":{\"path\":\"a.txt\"}}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "edit the file" }],
            Tools = [Tool("edit_local_file"), Tool("run_tests")],
            ToolChoice = ToolChoice("{\"type\":\"function\",\"function\":{\"name\":\"edit_local_file\"}}"),
        };

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        Assert.Equal("edit_local_file", Assert.Single(choice.Message.ToolCalls!).Function.Name);
        Assert.Contains("must call the tool 'edit_local_file'", orchestration.LastRequest!.Input, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-017): a specific function choice cannot name a tool absent from the request.</summary>
    [Fact]
    public async Task CompleteAsync_SpecificToolChoiceForUnknownTool_Throws()
    {
        var service = new QuadBrainOpenAiChatService(new CapturingOrchestrationService(new QuadBrainOrchestrationResponse()));
        var request = new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "edit the file" }],
            Tools = [Tool("edit_local_file")],
            ToolChoice = ToolChoice("{\"type\":\"function\",\"function\":{\"name\":\"run_tests\"}}"),
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Contains("run_tests", ex.Message, StringComparison.Ordinal);
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

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Equal("here is the plan", choice.Message.Content);
        Assert.Null(choice.Message.ToolCalls);
    }

    /// <summary>External tool failures are fed back into QuadBrain so Arbiter can choose the next step.</summary>
    [Fact]
    public async Task CompleteAsync_ExternalToolFailure_FeedsErrorIntoOrchestration()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = """{"intent":"final","content":"read_file failed; use mcp_repo_read next."}""",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);
        var request = new OpenAiChatCompletionRequest
        {
            Messages =
            [
                new OpenAiChatMessage { Role = "user", Content = "Create Hello World in C++" },
                new OpenAiChatMessage { Role = "tool", Content = "Error: Function failed. path not in allowlist" },
            ],
        };

        var response = await service.CompleteAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(orchestration.LastRequest);
        Assert.Contains("Function failed", orchestration.LastRequest!.Input, StringComparison.Ordinal);
        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Contains("read_file failed", choice.Message.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("external tool execution failed", choice.Message.Content, StringComparison.OrdinalIgnoreCase);
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
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] }, cancellationToken: TestContext.Current.CancellationToken)
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
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.False(string.IsNullOrWhiteSpace(choice.Message.Content));
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
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.Contains("Note", choice.Message.Content!, StringComparison.Ordinal);
        Assert.Contains("mcp_todo_update", choice.Message.Content!, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBEXEC-001 (AC-5): a failed internal tool is recorded to the session log, not only noted.</summary>
    [Fact]
    public async Task CompleteAsync_InternalToolFailure_RecordsFailureToSessionLog()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{}}]}",
        });
        var logger = new RecordingInteractionLogger();
        var service = new QuadBrainOpenAiChatService(
            orchestration, classifier: null, internalToolExecutor: new FailingExecutor("mcp_todo_update"), interactionLogger: logger);

        await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var (tool, error) = Assert.Single(logger.FailedTools);
        Assert.Equal("mcp_todo_update", tool);
        Assert.Equal("transaction rejected", error);
    }

    /// <summary>A successfully executed internal tool is NOT recorded as a failure.</summary>
    [Fact]
    public async Task CompleteAsync_InternalToolSuccess_RecordsNoFailure()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{}}]}",
        });
        var logger = new RecordingInteractionLogger();
        var service = new QuadBrainOpenAiChatService(
            orchestration, classifier: null, internalToolExecutor: new HandlingExecutor("mcp_todo_update"), interactionLogger: logger);

        await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Empty(logger.FailedTools);
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-019): the response carries a best-effort non-zero usage estimate.</summary>
    [Fact]
    public async Task CompleteAsync_PopulatesBestEffortUsageEstimate()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "a reasonably long arbiter answer that spans several tokens",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(new OpenAiChatCompletionRequest
        {
            Messages = [new OpenAiChatMessage { Role = "user", Content = "please plan the work in detail" }],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(response.Usage.PromptTokens > 0);
        Assert.True(response.Usage.CompletionTokens > 0);
        Assert.Equal(response.Usage.PromptTokens + response.Usage.CompletionTokens, response.Usage.TotalTokens);
    }

    /// <summary>FR-MCP-QUAD-SESSION-001: the session id (and turn id) attach the run to its session via metadata.</summary>
    [Fact]
    public async Task CompleteAsync_WithSessionId_AttachesSessionAndTurnToOrchestration()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "ok",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);

        await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] },
            sessionId: "sess-1",
            turnId: "turn-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("sess-1", orchestration.LastRequest!.Metadata["sessionId"]);
        Assert.Equal("turn-1", orchestration.LastRequest.Metadata["turnId"]);
        Assert.Equal("turn-1", orchestration.LastRequest.TurnId);
    }

    /// <summary>Without a session header no session/turn metadata is attached (anonymous run).</summary>
    [Fact]
    public async Task CompleteAsync_WithoutSessionId_AttachesNoSessionMetadata()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "ok",
        });
        var service = new QuadBrainOpenAiChatService(orchestration);

        await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.False(orchestration.LastRequest!.Metadata.ContainsKey("sessionId"));
        Assert.Null(orchestration.LastRequest.TurnId);
    }

    /// <summary>TEST-MCP-QBOLLAMA-001: A session-bound QuadBrain response completes the MCP session turn.</summary>
    [Fact]
    public async Task CompleteAsync_WithSessionAndTurn_CompletesSessionTurnWithAssistantResponse()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "the final answer",
            TransactionId = "txn-session",
        });
        var sessionLog = new RecordingSessionLogService();
        var service = new QuadBrainOpenAiChatService(orchestration, sessionLog: sessionLog);

        var response = await service.CompleteAsync(
                new OpenAiChatCompletionRequest { Model = "qbagent", Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] },
                sessionId: "session-1",
                turnId: "turn-1", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("the final answer", Assert.Single(response.Choices).Message.Content);
        Assert.Equal("QBAgent", sessionLog.SourceType);
        Assert.Equal("session-1", sessionLog.SessionId);
        Assert.NotNull(sessionLog.Turn);
        Assert.Equal("turn-1", sessionLog.Turn!.RequestId);
        Assert.Equal("completed", sessionLog.Turn.Status);
        Assert.Equal("the final answer", sessionLog.Turn.Response);
        Assert.Equal("qbagent", sessionLog.Turn.Model);
        var action = Assert.Single(sessionLog.Turn.Actions!);
        Assert.Equal("quadbrain_response", action.Type);
        Assert.Equal("completed", action.Status);
    }

    /// <summary>Rejected or empty orchestration must not look like a successful empty assistant turn.</summary>
    [Fact]
    public async Task CompleteAsync_RejectedEmptyOutput_ReturnsVisibleNoDecisionMessage()
    {
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "rejected",
            Reason = "QuadNotReady",
            Output = null,
        });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.False(string.IsNullOrWhiteSpace(choice.Message.Content));
        Assert.Contains("no decision", choice.Message.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("QuadNotReady", choice.Message.Content, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-QBEXEC-001/002: every catalog mcp_* name is executed internally and is not emitted as a tool_call.</summary>
    [Theory]
    [MemberData(nameof(CatalogToolNames))]
    public async Task CompleteAsync_CatalogName_StopsWithoutEmittingToolCall(string name)
    {
        var fixture = new QuadBrainExecutorTestFixture();
        var executor = fixture.CreateExecutor();
        var arguments = await QuadBrainExecutorTestFixture.CatalogArgumentsAsync(executor, name).ConfigureAwait(true);
        var executed = await executor.TryExecuteAsync(
            QuadBrainExecutorTestFixture.Call(name, arguments),
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var orchestration = new CapturingOrchestrationService(new QuadBrainOrchestrationResponse
        {
            Status = "committed",
            Output = "{\"tool_calls\":[{\"name\":\"" + name + "\",\"arguments\":" + arguments + "}]}",
        });
        var service = new QuadBrainOpenAiChatService(orchestration, classifier: null, internalToolExecutor: executor);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "go" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.False(string.IsNullOrWhiteSpace(choice.Message.Content));
        if (!string.IsNullOrWhiteSpace(executed.ResultJson))
            Assert.Contains(executed.ResultJson, choice.Message.Content, StringComparison.Ordinal);
        else
            Assert.Contains(name + " completed.", choice.Message.Content, StringComparison.Ordinal);
    }

    public static TheoryData<string> CatalogToolNames()
    {
        var data = new TheoryData<string>();
        foreach (var catalogName in QuadBrainMcpToolCatalog.All)
            data.Add(catalogName);
        return data;
    }

    /// <summary>An empty message list is rejected.</summary>
    [Fact]
    public async Task CompleteAsync_NoMessages_Throws()
    {
        var service = new QuadBrainOpenAiChatService(
            new CapturingOrchestrationService(new QuadBrainOrchestrationResponse { Status = "committed" }));

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CompleteAsync(new OpenAiChatCompletionRequest(), cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    private sealed class HandlingExecutor(string handledToolName, string? resultJson = null) : IQuadBrainInternalToolExecutor
    {
        public Task<InternalToolExecutionOutcome> TryExecuteAsync(
            OpenAiToolCall toolCall,
            string? turnId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(toolCall.Function.Name == handledToolName
                ? InternalToolExecutionOutcome.Ok(resultJson)
                : InternalToolExecutionOutcome.Unhandled);
    }

    // FR-MCP-QBOPENAI-001 ac-7: handles every MCP-internal tool and answers with the tool's name.
    private sealed class EchoingMcpExecutor : IQuadBrainInternalToolExecutor
    {
        public Task<InternalToolExecutionOutcome> TryExecuteAsync(
            OpenAiToolCall toolCall,
            string? turnId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(InternalToolExecutionOutcome.Ok($$"""{"tool":"{{toolCall.Function.Name}}"}"""));
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

    private sealed class RecordingInteractionLogger : IBrainInteractionSessionLogger
    {
        public List<(string Tool, string? Error)> FailedTools { get; } = [];

        public Task LogInteractionAsync(
            string sourceType, string? sessionId, string? turnId, string role, string prompt, string? output,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LogInternalToolFailureAsync(
            string sourceType, string? sessionId, string? turnId, string toolName, string? error,
            CancellationToken cancellationToken = default)
        {
            FailedTools.Add((toolName, error));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSessionLogService : ISessionLogService
    {
        public string? SourceType { get; private set; }

        public string? SessionId { get; private set; }

        public UnifiedRequestEntryDto? Turn { get; private set; }

        public Task<long> UpsertTurnAsync(
            string sourceType,
            string sessionId,
            UnifiedRequestEntryDto turn,
            CancellationToken cancellationToken = default)
        {
            SourceType = sourceType;
            SessionId = sessionId;
            Turn = turn;
            return Task.FromResult(1L);
        }

        public Task<long> SubmitAsync(
            UnifiedSessionLogDto dto,
            string? sourceFilePath = null,
            string? contentHash = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> IsUnchangedAsync(string sourceType, string sessionId, string contentHash, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> AppendProcessingDialogAsync(
            string sourceType,
            string sessionId,
            string requestId,
            IReadOnlyList<ProcessingDialogItemDto> items,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SessionLogQueryResult> QueryAsync(SessionLogQueryRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UnifiedSessionLogDto?> GetAsync(string sourceType, string sessionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<long> ReplaceTurnAsync(
            string sourceType,
            string sessionId,
            UnifiedRequestEntryDto turn,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<long> SetSessionTitleAsync(string sourceType, string sessionId, string title, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<long> SetTurnTitleAsync(string sourceType, string sessionId, string requestId, string title, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ReplaceTurnSectionAsync(
            string sourceType,
            string sessionId,
            string requestId,
            string section,
            UnifiedRequestEntryDto payload,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ClearTurnSectionAsync(
            string sourceType,
            string sessionId,
            string requestId,
            string section,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteTurnItemAsync(
            string sourceType,
            string sessionId,
            string requestId,
            string section,
            string itemKey,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteTurnAsync(string sourceType, string sessionId, string requestId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteSessionAsync(string sourceType, string sessionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> OpenSessionAsync(
            string sourceType,
            string sessionId,
            string? title = null,
            string? model = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> RepairWorkspaceStampsAsync(bool dryRun = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// TEST-MCP-QBOPENAI-001 AC-001 mock gate (FR-MCP-QBOPENAI-001 ac-5 and ac-6; PLAN-SESSIONTRIAGEFIX-001 B13, operator decision 2026-10-15
    /// "Both rules (Recommended)"; ported into IMPL-UNITGREEN-001 by operator decision 2026-10-29 "Port ac-5/ac-6 into PR 87"): the round
    /// outcome assertion accepts a reply returned after one round and the last reply returned after the eight-round budget, each with finish
    /// reason stop, and rejects empty content, an extra round, a reply other than the last, and a finish reason other than stop. Data:
    /// hand-built choices and round counts; no orchestration.
    /// </summary>
    [Fact]
    public void ChatRoundOutcomeAssertion_AcceptsFinalAnswerAndRejectsEmptyOrExtraRounds()
    {
        AssertChatRoundOutcome(1, "the plain answer", 1, Choice("the plain answer", "stop"));
        AssertChatRoundOutcome(8, "reply 8", 8, Choice("reply 8", "stop"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertChatRoundOutcome(8, "reply 8", 8, Choice(null, "stop")));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertChatRoundOutcome(1, "the plain answer", 2, Choice("the plain answer", "stop")));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertChatRoundOutcome(8, "reply 8", 8, Choice("reply 1", "stop")));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertChatRoundOutcome(1, "the plain answer", 1, Choice("the plain answer", "tool_calls")));
    }

    /// <summary>
    /// TEST-MCP-QBOPENAI-001 AC-001, FR-MCP-QBOPENAI-001 ac-5: with no tools and no tool_choice, a reply that is not an envelope is the final
    /// answer after one round. Data: scripted orchestration whose second reply would show a second round.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_NoToolsNonEnvelopeReply_ReturnsItAfterOneRound()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "the plain answer" },
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "a second round ran" });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "Hello" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertChatRoundOutcome(1, "the plain answer", orchestration.CallCount, Assert.Single(response.Choices));
    }

    /// <summary>
    /// TEST-MCP-QBOPENAI-001 AC-001, FR-MCP-QBOPENAI-001 ac-5: with no tools and no tool_choice, an explicit continue envelope still asks for
    /// another round, and the final envelope that follows is the answer. Data: scripted continue, then final envelopes.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_NoToolsContinueEnvelope_StillRePrompts()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse { Status = "committed", Output = """{"intent":"continue","content":"Looking."}""" },
            new QuadBrainOrchestrationResponse { Status = "committed", Output = """{"intent":"final","content":"done"}""" });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "Hello" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertChatRoundOutcome(2, "done", orchestration.CallCount, Assert.Single(response.Choices));
        Assert.Contains(QuadBrainOpenAiChatService.ContinueDirective, orchestration.Requests[1].Input, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBOPENAI-001 AC-001, FR-MCP-QBOPENAI-001 ac-6: with a tool offered, eight distinct replies that are not envelopes use the whole
    /// round budget, and the eighth reply comes back instead of empty content. Data: scripted replies "reply 1" to "reply 9" (a ninth round would
    /// show the budget overrun).
    /// </summary>
    [Fact]
    public async Task CompleteAsync_RoundsExhaustedWithoutFinal_ReturnsLastArbiterOutput()
    {
        var orchestration = new ScriptedOrchestrationService(
            Enumerable.Range(1, 9).Select(n => new QuadBrainOrchestrationResponse { Status = "committed", Output = $"reply {n}" }).ToArray());
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest
            {
                Messages = [new OpenAiChatMessage { Role = "user", Content = "update voice chat" }],
                Tools = [Tool("read_file")],
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertChatRoundOutcome(8, "reply 8", orchestration.CallCount, Assert.Single(response.Choices));
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-7 and ac-6 contract, mock gate (operator 2026-10-29 "QuadBrain ACs (Recommended)", 2026-10-30 "F2: ac-6 vs ac-7
    /// (Recommended)"; holistic review finding SRC-2): the display-content rows hold for a scripted reference resolver. A plain reply and an
    /// envelope's content come back as they are, an empty output gets the no-decision message, and an Arbiter envelope without displayable
    /// content gets a plain message that names the round budget and the envelope's intent. Data: hand-written outputs; no orchestration.
    /// </summary>
    [Fact]
    public void DisplayContentContract_ScriptedReference_SatisfiesTheRows()
    {
        foreach (var row in DisplayContentRows)
            AssertDisplayContent(row, ScriptedDisplayContent(row.Output, row.RoundsUsed, NoDecision));
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-7 contract, negative controls inside a passing harness: a resolver that passes a content-less envelope through
    /// raw (the SRC-2 defect), one that returns empty content, one whose message omits the intent, one whose message omits the round budget,
    /// and one that answers every output with the message are each rejected by at least one row. Data: the same rows as the reference.
    /// </summary>
    [Fact]
    public void DisplayContentContract_WrongResolvers_AreRejectedInsidePassingHarness()
    {
        Func<string?, int, string, string>[] wrongResolvers =
        [
            (output, _, noDecision) => string.IsNullOrWhiteSpace(output)
                ? noDecision
                : QuadBrainArbiterEnvelope.FormatForDisplay(output, showIntent: false) is { Length: > 0 } shown ? shown : output,
            (output, _, noDecision) => string.IsNullOrWhiteSpace(output) ? noDecision : QuadBrainArbiterEnvelope.FormatForDisplay(output, showIntent: false),
            (output, rounds, noDecision) =>
            {
                var reference = ScriptedDisplayContent(output, rounds, noDecision);
                var envelope = QuadBrainArbiterEnvelope.TryParse(output);
                return envelope is not null && string.IsNullOrWhiteSpace(QuadBrainArbiterEnvelope.FormatForDisplay(output, showIntent: false))
                    ? reference.Replace(envelope.Intent, "unknown", StringComparison.Ordinal)
                    : reference;
            },
            (output, rounds, noDecision) => ScriptedDisplayContent(output, rounds, noDecision)
                .Replace($" of {QuadBrainOpenAiChatService.MaxProcessingRounds} orchestration rounds", " orchestration rounds", StringComparison.Ordinal),
            (output, rounds, _) => $"QuadBrain gave no displayable answer after {rounds} of {QuadBrainOpenAiChatService.MaxProcessingRounds} orchestration rounds (last Arbiter intent: {QuadBrainArbiterEnvelope.TryParse(output)?.Intent ?? "none"}).",
        ];

        foreach (var resolver in wrongResolvers)
        {
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            {
                foreach (var row in DisplayContentRows)
                    AssertDisplayContent(row, resolver(row.Output, row.RoundsUsed, NoDecision));
            });
        }
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-7 mock gate: the internal-round outcome assertion accepts the internal tool results of the round that spent the
    /// budget, with finish reason stop and no tool calls, and rejects the tool-call envelope, another round's results, empty content and an
    /// extra round. Data: hand-built choices and round counts; no orchestration.
    /// </summary>
    [Fact]
    public void InternalRoundOutcomeAssertion_AcceptsTheRoundsToolResultsAndRejectsEnvelopesOrOtherRounds()
    {
        const string results = """{"tool":"mcp_round_8"}""";
        AssertInternalRoundOutcome(8, results, 8, Choice(results, "stop"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertInternalRoundOutcome(8, results, 8, Choice("""{"tool_calls":[{"name":"mcp_round_8","arguments":{}}]}""", "stop")));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertInternalRoundOutcome(8, results, 8, Choice("""{"tool":"mcp_round_7"}""", "stop")));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertInternalRoundOutcome(8, results, 8, Choice(null, "stop")));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertInternalRoundOutcome(8, results, 9, Choice(results, "stop")));
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-6 and ac-7 binding: the display-content rows hold for the production resolver. Data: the contract rows.
    /// </summary>
    [Fact]
    public void DisplayContentContract_Production_SatisfiesTheRows()
    {
        foreach (var row in DisplayContentRows)
            AssertDisplayContent(row, QuadBrainOpenAiChatService.ResolveDisplayContent(row.Output, row.RoundsUsed, NoDecision));
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-5 binding (holistic review finding SRC-3): a tool_choice of auto with no tools counts as no tool_choice,
    /// so a reply that is not an envelope is the answer after one round. Data: scripted orchestration whose second reply would show a
    /// second round.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ToolChoiceAutoNoTools_ReturnsNonEnvelopeReplyAfterOneRound()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "the plain answer" },
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "a second round ran" });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest
            {
                Messages = [new OpenAiChatMessage { Role = "user", Content = "Hello" }],
                ToolChoice = ToolChoice("\"auto\""),
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertChatRoundOutcome(1, "the plain answer", orchestration.CallCount, Assert.Single(response.Choices));
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-5 binding (SRC-3): with no tools, a committed Arbiter output that is empty is answered with the
    /// no-decision message after one round. Data: scripted empty output, then a reply that would show a second round.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_NoToolsEmptyCommittedOutput_ReturnsNoDecisionAfterOneRound()
    {
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "" },
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "a second round ran" });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "Hello" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertChatRoundOutcome(1, NoDecision, orchestration.CallCount, Assert.Single(response.Choices));
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-7 binding (SRC-2): a repeated continue envelope without content ends the loop with a plain message that
    /// names the round budget and the intent, never the raw envelope. Data: a tool offered; the same content-less continue envelope twice.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_RepeatedContentlessContinue_ReturnsBudgetMessage()
    {
        const string continueEnvelope = """{"intent":"continue"}""";
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse { Status = "committed", Output = continueEnvelope },
            new QuadBrainOrchestrationResponse { Status = "committed", Output = continueEnvelope },
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "a third round ran" });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest
            {
                Messages = [new OpenAiChatMessage { Role = "user", Content = "update voice chat" }],
                Tools = [Tool("read_file")],
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal(2, orchestration.CallCount);
        Assert.Equal("stop", choice.FinishReason);
        AssertDisplayContent(new DisplayContentRow(continueEnvelope, 2, null, "continue"), choice.Message.Content);
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-7 binding (SRC-2): a final envelope without content is answered with the plain message after one round.
    /// Data: no tools; one content-less final envelope, then a reply that would show a second round.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_FinalEnvelopeWithoutContent_ReturnsBudgetMessage()
    {
        const string finalEnvelope = """{"intent":"final"}""";
        var orchestration = new ScriptedOrchestrationService(
            new QuadBrainOrchestrationResponse { Status = "committed", Output = finalEnvelope },
            new QuadBrainOrchestrationResponse { Status = "committed", Output = "a second round ran" });
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "Hello" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal(1, orchestration.CallCount);
        Assert.Equal("stop", choice.FinishReason);
        AssertDisplayContent(new DisplayContentRow(finalEnvelope, 1, null, "final"), choice.Message.Content);
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-6 and ac-7 binding (F2): when the round budget ends on content-less continue envelopes, the content is
    /// the plain message that names the budget and the intent, not the last envelope. Data: a tool offered; nine distinct content-less
    /// continue envelopes (a ninth round would show the budget overrun).
    /// </summary>
    [Fact]
    public async Task CompleteAsync_RoundsExhaustedOnContentlessEnvelopes_ReturnsBudgetMessage()
    {
        var orchestration = new ScriptedOrchestrationService(
            Enumerable.Range(1, 9)
                .Select(n => new QuadBrainOrchestrationResponse { Status = "committed", Output = $$$"""{"intent":"continue","step":{{{n}}}}""" })
                .ToArray());
        var service = new QuadBrainOpenAiChatService(orchestration);

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest
            {
                Messages = [new OpenAiChatMessage { Role = "user", Content = "update voice chat" }],
                Tools = [Tool("read_file")],
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var choice = Assert.Single(response.Choices);
        Assert.Equal(QuadBrainOpenAiChatService.MaxProcessingRounds, orchestration.CallCount);
        Assert.Equal("stop", choice.FinishReason);
        AssertDisplayContent(
            new DisplayContentRow("""{"intent":"continue","step":8}""", QuadBrainOpenAiChatService.MaxProcessingRounds, null, "continue"),
            choice.Message.Content);
    }

    /// <summary>
    /// FR-MCP-QBOPENAI-001 ac-7 binding (SRC-2): when the round budget ends after an internal-tool round, the content is that round's
    /// internal tool results, not the tool-call envelope. Data: nine rounds, each calling a different MCP-internal tool that a scripted
    /// executor handles (distinct names, so the repeated-signature exit never fires).
    /// </summary>
    [Fact]
    public async Task CompleteAsync_RoundsExhaustedAfterInternalToolRound_ReturnsThatRoundsToolResults()
    {
        var orchestration = new ScriptedOrchestrationService(
            Enumerable.Range(1, 9)
                .Select(n => new QuadBrainOrchestrationResponse { Status = "committed", Output = $$$"""{"tool_calls":[{"name":"mcp_round_{{{n}}}","arguments":{}}]}""" })
                .ToArray());
        var service = new QuadBrainOpenAiChatService(orchestration, classifier: null, internalToolExecutor: new EchoingMcpExecutor());

        var response = await service.CompleteAsync(
            new OpenAiChatCompletionRequest { Messages = [new OpenAiChatMessage { Role = "user", Content = "list open todo" }] },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertInternalRoundOutcome(
            QuadBrainOpenAiChatService.MaxProcessingRounds,
            """{"tool":"mcp_round_8"}""",
            orchestration.CallCount,
            Assert.Single(response.Choices));
    }

    // FR-MCP-QBOPENAI-001 ac-5 to ac-7: the no-decision message the scripted orchestration's committed empty output earns.
    private const string NoDecision = "QuadBrain returned no decision (committed).";

    // FR-MCP-QBOPENAI-001 ac-6 and ac-7 rows: an output, the rounds used, and either the exact content or the intent the plain message names.
    private static readonly DisplayContentRow[] DisplayContentRows =
    [
        new("the plain answer", 1, "the plain answer", null),
        new("""{"intent":"final","content":"done"}""", 2, "done", null),
        new("", 1, NoDecision, null),
        new("""{"intent":"continue"}""", 3, null, "continue"),
        new("""{"intent":"final"}""", 1, null, "final"),
        new("""{"intent":"reject","content":"   "}""", 8, null, "reject"),
        new("""{"tool_calls":[{"name":"read_file","arguments":{}}]}""", 8, null, "tool_calls"),
    ];

    // FR-MCP-QBOPENAI-001 ac-7: the content is never empty; an exact row gets its content; otherwise the content is not an Arbiter envelope and
    // names the round budget and the row's intent.
    private static void AssertDisplayContent(DisplayContentRow row, string? content)
    {
        Assert.False(string.IsNullOrWhiteSpace(content), $"Empty content for output '{row.Output}'.");
        if (row.ExpectedExact is not null)
        {
            Assert.Equal(row.ExpectedExact, content);
            return;
        }

        Assert.Null(QuadBrainArbiterEnvelope.TryParse(content));
        Assert.Matches($@"\b{QuadBrainOpenAiChatService.MaxProcessingRounds}\b", content);
        Assert.Contains(row.ExpectedIntent!, content, StringComparison.Ordinal);
    }

    // FR-MCP-QBOPENAI-001 ac-7 scripted reference: display text when there is some, the no-decision message for an empty output, otherwise a
    // plain message naming the rounds used, the round budget and the envelope's intent.
    private static string ScriptedDisplayContent(string? output, int roundsUsed, string noDecision)
    {
        if (string.IsNullOrWhiteSpace(output))
            return noDecision;
        var shown = QuadBrainArbiterEnvelope.FormatForDisplay(output, showIntent: false);
        if (!string.IsNullOrWhiteSpace(shown))
            return shown;
        var intent = QuadBrainArbiterEnvelope.TryParse(output)?.Intent ?? "none";
        return $"QuadBrain gave no displayable answer after {roundsUsed} of {QuadBrainOpenAiChatService.MaxProcessingRounds} orchestration rounds (last Arbiter intent: {intent}).";
    }

    // FR-MCP-QBOPENAI-001 ac-7: after an internal-tool round spends the budget, the content is that round's tool results, not an envelope.
    private static void AssertInternalRoundOutcome(int expectedRounds, string expectedToolResults, int rounds, OpenAiChatChoice choice)
    {
        Assert.Equal(expectedRounds, rounds);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.Equal(expectedToolResults, choice.Message.Content);
        Assert.Null(QuadBrainArbiterEnvelope.TryParse(choice.Message.Content));
    }

    private sealed record DisplayContentRow(string? Output, int RoundsUsed, string? ExpectedExact, string? ExpectedIntent);

    // FR-MCP-QBOPENAI-001 ac-5 and ac-6: the expected number of orchestration rounds ran, and the reply the requirement names came back as
    // non-empty assistant content with finish reason stop.
    private static void AssertChatRoundOutcome(int expectedRounds, string expectedContent, int rounds, OpenAiChatChoice choice)
    {
        Assert.Equal(expectedRounds, rounds);
        Assert.Equal("stop", choice.FinishReason);
        Assert.False(string.IsNullOrWhiteSpace(choice.Message.Content), "The assistant message is empty.");
        Assert.Equal(expectedContent, choice.Message.Content);
    }

    private static OpenAiChatChoice Choice(string? content, string finishReason)
        => new() { Index = 0, Message = new OpenAiChatResponseMessage { Role = "assistant", Content = content }, FinishReason = finishReason };

    private static OpenAiToolDefinition Tool(string name)
        => new() { Function = new OpenAiFunctionDefinition { Name = name, Description = $"Run {name}." } };

    private static JsonElement ToolChoice(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class ScriptedOrchestrationService(params QuadBrainOrchestrationResponse[] responses)
        : IQuadBrainOrchestrationService
    {
        private int _index;

        public List<QuadBrainOrchestrationRequest> Requests { get; } = [];

        public int CallCount => Requests.Count;

        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            var index = Math.Min(_index, responses.Length - 1);
            _index++;
            return Task.FromResult(responses[index]);
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
