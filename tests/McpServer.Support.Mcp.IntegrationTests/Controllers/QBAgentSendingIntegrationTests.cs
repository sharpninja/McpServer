using System.Collections.Concurrent;
using McpServer.McpAgent;
using McpServer.McpAgent.Hosting;
using McpServer.QBAgent;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-QBAGENTINT-001: End-to-end tests for QBAgent acting as the sender. The real Microsoft Agent Framework
/// loop runs with QuadBrain (the OpenAI-compatible <c>/v1/chat/completions</c> endpoint) as its model, routed over
/// the in-memory test server. Exercises FR-MCP-QBAGENT-001 (agent sends/receives), FR-MCP-QBOPENAI-001 (OpenAI wire),
/// and FR-MCP-QBEXEC-001 (internal tools execute server-side and never reach the agent).
/// </summary>
public sealed class QBAgentSendingIntegrationTests
{
    /// <summary>ac-1: a prompt with no tool action returns the plain assistant text to the agent.</summary>
    [Fact]
    public async Task QBAgent_NoToolAction_ReturnsPlainResponse()
    {
        var orchestration = new ScriptedOrchestration("the plain arbiter answer");
        using var factory = BuildFactory(orchestration, new RecordingExecutor());

        await using var agentProvider = BuildAgentProvider(factory, out var token);
        var text = await RunPromptAsync(agentProvider, factory, token, externalTool: null, "explain the change").ConfigureAwait(true);

        Assert.Equal(1, orchestration.Calls);
        Assert.Contains("the plain arbiter answer", text, StringComparison.Ordinal);
    }

    /// <summary>ac-2: an external tool call is executed by the agent loop, then the turn continues to a final answer.</summary>
    [Fact]
    public async Task QBAgent_ExternalToolCall_AgentExecutesAndContinues()
    {
        var orchestration = new ScriptedOrchestration(
            "{\"tool_calls\":[{\"name\":\"apply_patch\",\"arguments\":{\"path\":\"src/x.cs\"}}]}",
            "patch applied to src/x.cs");
        using var factory = BuildFactory(orchestration, new RecordingExecutor());

        var invoked = new ConcurrentBag<string>();
        AITool applyPatch = AIFunctionFactory.Create(
            (string path) => { invoked.Add(path); return "patched " + path; },
            "apply_patch",
            "Apply a patch to a local file outside the MCP server.");

        await using var agentProvider = BuildAgentProvider(factory, out var token);
        var text = await RunPromptAsync(agentProvider, factory, token, applyPatch, "patch the file").ConfigureAwait(true);

        Assert.Equal("src/x.cs", Assert.Single(invoked));
        Assert.Equal(2, orchestration.Calls); // round 1 emits the tool call, round 2 produces the final answer
        Assert.Contains("patch applied to src/x.cs", text, StringComparison.Ordinal);
    }

    /// <summary>ac-3: an MCP-internal tool runs server-side and is stripped, so no tool call reaches the agent loop.</summary>
    [Fact]
    public async Task QBAgent_InternalTool_ExecutedServerSide_NeverReachesAgent()
    {
        var orchestration = new ScriptedOrchestration(
            "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{\"id\":\"X\"}}]}");
        var executor = new RecordingExecutor("mcp_todo_update");
        using var factory = BuildFactory(orchestration, executor);

        var invoked = new ConcurrentBag<string>();
        AITool external = AIFunctionFactory.Create(
            (string path) => { invoked.Add(path); return "should not run"; },
            "apply_patch",
            "Apply a patch to a local file outside the MCP server.");

        await using var agentProvider = BuildAgentProvider(factory, out var token);
        var text = await RunPromptAsync(agentProvider, factory, token, external, "update the todo").ConfigureAwait(true);

        Assert.Equal("mcp_todo_update", Assert.Single(executor.Executed)); // ran server-side
        Assert.Empty(invoked);                                             // agent executed nothing
        Assert.Equal(1, orchestration.Calls);                             // no second round - nothing to feed back
        Assert.NotNull(text);
    }

    private static CustomWebApplicationFactory BuildFactory(
        IQuadBrainOrchestrationService orchestration,
        IQuadBrainInternalToolExecutor executor)
        => new(services =>
        {
            services.RemoveAll<IQuadBrainOrchestrationService>();
            services.AddSingleton(orchestration);
            services.RemoveAll<IQuadBrainInternalToolExecutor>();
            services.AddSingleton(executor);
        });

    private static ServiceProvider BuildAgentProvider(CustomWebApplicationFactory factory, out string token)
    {
        var resolved = ResolveToken(factory);
        token = resolved;
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.BaseUrl = new Uri("http://localhost");
            options.ApiKey = resolved;
            options.WorkspacePath = factory.WorkspacePath;
        });
        return services.BuildServiceProvider();
    }

    private static async Task<string> RunPromptAsync(
        IServiceProvider agentProvider,
        CustomWebApplicationFactory factory,
        string token,
        AITool? externalTool,
        string prompt)
    {
        var agent = agentProvider.GetRequiredService<IMcpHostedAgent>();

        // QuadBrain is the model behind the loop, routed over the in-memory test server.
        using var httpClient = new HttpClient(factory.Server.CreateHandler());
        using var chatClient = QBAgentChatClientFactory.Create(
            new McpAgentOptions { BaseUrl = new Uri("http://localhost"), ApiKey = token }, httpClient);

        var chatAgent = agent.CreateChatClientAgent(chatClient);
        var baseOptions = externalTool is null
            ? null
            : new ChatClientAgentRunOptions { ChatOptions = new ChatOptions { Tools = [externalTool] } };
        var runOptions = agent.CreateRunOptions(baseOptions);
        var session = await chatAgent.CreateSessionAsync().ConfigureAwait(false);
        var response = await chatAgent.RunAsync(
            [new ChatMessage(ChatRole.User, prompt)], session, runOptions).ConfigureAwait(false);
        return response.Text ?? string.Empty;
    }

    private static string ResolveToken(CustomWebApplicationFactory factory)
    {
        var probe = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(probe, factory.Services);
        return probe.DefaultRequestHeaders.GetValues("X-Api-Key").First();
    }

    /// <summary>A QuadBrain orchestration double that returns a scripted output per call (one per model round).</summary>
    private sealed class ScriptedOrchestration : IQuadBrainOrchestrationService
    {
        private readonly Queue<string> _outputs;
        private int _calls;

        public ScriptedOrchestration(params string[] outputs) => _outputs = new Queue<string>(outputs);

        public int Calls => _calls;

        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            var output = _outputs.Count > 0 ? _outputs.Dequeue() : string.Empty;
            return Task.FromResult(new QuadBrainOrchestrationResponse { Status = "committed", Output = output });
        }

        public Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
            AotReconciliationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
            QuadBrainWeightUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    /// <summary>An internal-tool executor double that records and succeeds for the configured internal tool name.</summary>
    private sealed class RecordingExecutor(string? handled = null) : IQuadBrainInternalToolExecutor
    {
        private readonly ConcurrentBag<string> _executed = [];

        public IReadOnlyCollection<string> Executed => _executed;

        public Task<InternalToolExecutionOutcome> TryExecuteAsync(
            OpenAiToolCall toolCall, string? turnId, CancellationToken cancellationToken = default)
        {
            if (handled is not null && toolCall.Function.Name == handled)
            {
                _executed.Add(toolCall.Function.Name);
                return Task.FromResult(InternalToolExecutionOutcome.Ok());
            }

            return Task.FromResult(InternalToolExecutionOutcome.Unhandled);
        }
    }
}
