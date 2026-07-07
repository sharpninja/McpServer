using System.Collections.Concurrent;
using System.Text.Json;
using McpServer.Client;
using McpServer.McpAgent;
using McpServer.McpAgent.Hosting;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.QBAgent;
using McpServer.QBAgent.Tools;
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
[Trait("Category", "Integration")]
public sealed class QBAgentSendingIntegrationTests
{
    private const string QBAgentVisibleModel = "QuadBrain";
    private const string QBAgentVisibleEndpoint = "McpServer /v1/chat/completions";
    private static readonly object ArtifactGate = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>ac-1: a prompt with no tool action returns the plain assistant text to the agent.</summary>
    [Fact]
    public async Task QBAgent_NoToolAction_ReturnsPlainResponse()
    {
        var orchestration = new ScriptedOrchestration("the plain arbiter answer");
        using var factory = BuildFactory(orchestration, new RecordingExecutor());

        await using var agentProvider = BuildAgentProvider(factory, out var token);
        var text = await RunPromptAsync(agentProvider, factory, token, externalTools: null, "explain the change").ConfigureAwait(true);

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
        var text = await RunPromptAsync(agentProvider, factory, token, [applyPatch], "patch the file").ConfigureAwait(true);

        Assert.Equal("src/x.cs", Assert.Single(invoked));
        Assert.Equal(2, orchestration.Calls); // round 1 emits the tool call, round 2 produces the final answer
        Assert.Contains("patch applied to src/x.cs", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBAGENTINT-001: A coding prompt from QBAgent must surface the action QBAgent should execute,
    /// execute that external action, continue the turn, and write a JSONL transcript of the interaction.
    /// </summary>
    [Fact]
    public async Task QBAgent_CreateHelloWorldCppPrompt_ExecutesWriteFileActionAndWritesTranscript()
    {
        const string prompt = "Create Hello World in C++";
        const string relativePath = "examples/hello.cpp";
        var orchestration = new ScriptedOrchestration(
            """
            {"tool_calls":[{"name":"write_file","arguments":{"path":"examples/hello.cpp","content":"#include <iostream>\n\nint main() {\n    std::cout << \"Hello, World!\" << std::endl;\n    return 0;\n}\n"}}]}
            """,
            "QBAgent action complete: wrote examples/hello.cpp with the Hello World C++ program and can compile it with g++ examples/hello.cpp -o hello.");
        using var factory = BuildFactory(
            orchestration,
            new RecordingExecutor(),
            new Dictionary<string, string?> { ["Mcp:RepoAllowlist:2"] = "examples/**/*.cpp" });

        await using var agentProvider = BuildAgentProvider(factory, out var token);
        var absolutePath = Path.Combine(factory.WorkspacePath, "examples", "hello.cpp");
        Assert.False(File.Exists(absolutePath), "The temp workspace must start without hello.cpp so the test proves a real write.");
        using var qbagentHttp = new HttpClient(factory.Server.CreateHandler()) { Timeout = TimeSpan.FromMinutes(10) };
        var qbagentClient = new McpServerClient(
            qbagentHttp,
            new McpServerClientOptions
            {
                BaseUrl = new Uri("http://localhost"),
                ApiKey = token,
                WorkspacePath = factory.WorkspacePath,
            });
        using var toolSet = QBAgentExternalToolSurface.Create(
            qbagentClient,
            new FakePowerShellSessionManager(new PowerShellSessionCommandResult { Success = true, Output = "unused" }),
            new FakeProcessRunner(new ProcessRunResult(0, "unused", null)),
            factory.WorkspacePath,
            allowGitPush: false);

        var text = await RunPromptAsync(agentProvider, factory, token, toolSet.Tools, prompt).ConfigureAwait(true);

        Assert.True(File.Exists(absolutePath), $"Expected QBAgent to write {absolutePath} through the real file tool.");
        var writtenContent = File.ReadAllText(absolutePath);
        Assert.Contains("#include <iostream>", writtenContent, StringComparison.Ordinal);
        Assert.Contains("int main", writtenContent, StringComparison.Ordinal);
        Assert.Contains("std::cout", writtenContent, StringComparison.Ordinal);
        Assert.Equal(2, orchestration.Calls);
        Assert.Contains(orchestration.Inputs, input => input.Contains("tool:", StringComparison.OrdinalIgnoreCase)
            && input.Contains("examples/hello.cpp", StringComparison.OrdinalIgnoreCase)
            && input.Contains("written", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("wrote examples/hello.cpp", text, StringComparison.Ordinal);
        Assert.Contains("g++ examples/hello.cpp -o hello", text, StringComparison.Ordinal);

        var transcriptPath = WriteQBAgentActionTranscript(
            nameof(QBAgent_CreateHelloWorldCppPrompt_ExecutesWriteFileActionAndWritesTranscript),
            prompt,
            relativePath,
            absolutePath,
            writtenContent,
            "wrote examples/hello.cpp",
            text);
        Assert.True(File.Exists(transcriptPath), $"Expected QBAgent action transcript at {transcriptPath}.");
        var transcript = File.ReadAllText(transcriptPath);
        Assert.Contains("\"recordType\":\"qbagentReceivedToolCall\"", transcript, StringComparison.Ordinal);
        Assert.Contains("\"recordType\":\"qbagentExecutedAction\"", transcript, StringComparison.Ordinal);
        Assert.Contains("\"recordType\":\"qbagentDisplayedOutput\"", transcript, StringComparison.Ordinal);
        Assert.Contains("\"model\":\"QuadBrain\"", transcript, StringComparison.Ordinal);
        Assert.Contains("\"endpoint\":\"McpServer /v1/chat/completions\"", transcript, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBAGENTINT-002: A failed required external tool must stop the agent from fabricating completion.
    /// </summary>
    [Fact]
    public async Task QBAgent_ExternalToolFailure_DoesNotFabricateCompletedAction()
    {
        const string prompt = "Create Hello World in C++";
        const string relativePath = "blocked/hello.cpp";
        const string content = "#include <iostream>\n\nint main() {\n    std::cout << \"Hello, World!\" << std::endl;\n    return 0;\n}\n";
        var orchestration = new ScriptedOrchestration(
            """
            {"tool_calls":[{"name":"write_file","arguments":{"path":"blocked/hello.cpp","content":"#include <iostream>\n\nint main() {\n    std::cout << \"Hello, World!\" << std::endl;\n    return 0;\n}\n"}}]}
            """,
            "QBAgent action complete: wrote blocked/hello.cpp with the Hello World C++ program.");
        using var factory = BuildFactory(orchestration, new RecordingExecutor());

        await using var agentProvider = BuildAgentProvider(factory, out var token);
        var absolutePath = Path.Combine(factory.WorkspacePath, "blocked", "hello.cpp");
        using var qbagentHttp = new HttpClient(factory.Server.CreateHandler()) { Timeout = TimeSpan.FromMinutes(10) };
        var qbagentClient = new McpServerClient(
            qbagentHttp,
            new McpServerClientOptions
            {
                BaseUrl = new Uri("http://localhost"),
                ApiKey = token,
                WorkspacePath = factory.WorkspacePath,
            });
        using var toolSet = QBAgentExternalToolSurface.Create(
            qbagentClient,
            new FakePowerShellSessionManager(new PowerShellSessionCommandResult { Success = true, Output = "unused" }),
            new FakeProcessRunner(new ProcessRunResult(0, "unused", null)),
            factory.WorkspacePath,
            allowGitPush: false);

        var text = await RunPromptAsync(agentProvider, factory, token, toolSet.Tools, prompt).ConfigureAwait(true);

        Assert.False(File.Exists(absolutePath), $"QBAgent must not create {absolutePath} after the MCP file tool rejects the path.");
        Assert.Equal(1, orchestration.Calls);
        Assert.Contains("external tool execution failed", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("requested action was not completed", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("QBAgent action complete", text, StringComparison.Ordinal);
        Assert.DoesNotContain("wrote blocked/hello.cpp", text, StringComparison.Ordinal);

        var transcriptPath = WriteQBAgentToolFailureTranscript(
            nameof(QBAgent_ExternalToolFailure_DoesNotFabricateCompletedAction),
            prompt,
            relativePath,
            absolutePath,
            content,
            text);
        Assert.True(File.Exists(transcriptPath), $"Expected QBAgent failure transcript at {transcriptPath}.");
        var transcript = File.ReadAllText(transcriptPath);
        Assert.Contains("\"recordType\":\"qbagentRejectedToolResult\"", transcript, StringComparison.Ordinal);
        Assert.Contains("\"fileExists\":false", transcript, StringComparison.Ordinal);
        Assert.Contains("\"model\":\"QuadBrain\"", transcript, StringComparison.Ordinal);
        Assert.Contains("\"endpoint\":\"McpServer /v1/chat/completions\"", transcript, StringComparison.Ordinal);
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
        var text = await RunPromptAsync(agentProvider, factory, token, [external], "update the todo").ConfigureAwait(true);

        Assert.Equal("mcp_todo_update", Assert.Single(executor.Executed)); // ran server-side
        Assert.Empty(invoked);                                             // agent executed nothing
        Assert.Equal(1, orchestration.Calls);                             // no second round - nothing to feed back
        Assert.NotNull(text);
    }

    private static CustomWebApplicationFactory BuildFactory(
        IQuadBrainOrchestrationService orchestration,
        IQuadBrainInternalToolExecutor executor,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
        => new(services =>
        {
            services.RemoveAll<IQuadBrainOrchestrationService>();
            services.AddSingleton(orchestration);
            services.RemoveAll<IQuadBrainInternalToolExecutor>();
            services.AddSingleton(executor);
        }, configurationOverrides);

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
        IReadOnlyList<AITool>? externalTools,
        string prompt)
    {
        var agent = agentProvider.GetRequiredService<IMcpHostedAgent>();

        // QuadBrain is the model behind the loop, routed over the in-memory test server.
        using var httpClient = new HttpClient(factory.Server.CreateHandler());
        using var chatClient = QBAgentChatClientFactory.Create(
            new McpAgentOptions { BaseUrl = new Uri("http://localhost"), ApiKey = token }, httpClient);

        var chatAgent = agent.CreateChatClientAgent(chatClient);
        var baseOptions = externalTools is null
            ? null
            : new ChatClientAgentRunOptions { ChatOptions = new ChatOptions { Tools = [.. externalTools] } };
        var runOptions = agent.CreateRunOptions(baseOptions);
        var session = await chatAgent.CreateSessionAsync().ConfigureAwait(false);
        var response = await chatAgent.RunAsync(
            [new ChatMessage(ChatRole.User, prompt)], session, runOptions).ConfigureAwait(false);
        return response.Text ?? string.Empty;
    }

    private static string WriteQBAgentActionTranscript(
        string testName,
        string prompt,
        string relativePath,
        string absolutePath,
        string content,
        string toolResult,
        string displayedOutput)
    {
        var artifactRoot = Path.Combine(
            CustomWebApplicationFactory.ResolveSolutionRoot(),
            "TestResults",
            nameof(QBAgentSendingIntegrationTests));
        var createdAtUtc = DateTimeOffset.UtcNow;
        var transcriptPath = Path.Combine(
            artifactRoot,
            $"qbagent-create-hello-world-cpp-{createdAtUtc:yyyyMMddTHHmmssfffZ}.jsonl");
        var lines = new[]
        {
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentRun",
                createdAtUtc,
                testName,
                model = QBAgentVisibleModel,
                endpoint = QBAgentVisibleEndpoint,
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentPrompt",
                createdAtUtc,
                prompt,
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentReceivedToolCall",
                createdAtUtc,
                toolName = "write_file",
                path = relativePath,
                content,
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentExecutedAction",
                createdAtUtc,
                toolName = "write_file",
                path = relativePath,
                absolutePath,
                fileExists = File.Exists(absolutePath),
                result = toolResult,
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentDisplayedOutput",
                createdAtUtc,
                displayedOutput,
            }, JsonOptions),
        };

        lock (ArtifactGate)
        {
            Directory.CreateDirectory(artifactRoot);
            File.WriteAllLines(transcriptPath, lines);
        }

        return transcriptPath;
    }

    private static string WriteQBAgentToolFailureTranscript(
        string testName,
        string prompt,
        string relativePath,
        string absolutePath,
        string content,
        string displayedOutput)
    {
        var artifactRoot = Path.Combine(
            CustomWebApplicationFactory.ResolveSolutionRoot(),
            "TestResults",
            nameof(QBAgentSendingIntegrationTests));
        var createdAtUtc = DateTimeOffset.UtcNow;
        var transcriptPath = Path.Combine(
            artifactRoot,
            $"qbagent-tool-failure-{createdAtUtc:yyyyMMddTHHmmssfffZ}.jsonl");
        var lines = new[]
        {
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentRun",
                createdAtUtc,
                testName,
                model = QBAgentVisibleModel,
                endpoint = QBAgentVisibleEndpoint,
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentPrompt",
                createdAtUtc,
                prompt,
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentReceivedToolCall",
                createdAtUtc,
                toolName = "write_file",
                path = relativePath,
                content,
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentRejectedToolResult",
                createdAtUtc,
                toolName = "write_file",
                path = relativePath,
                absolutePath,
                fileExists = File.Exists(absolutePath),
            }, JsonOptions),
            JsonSerializer.Serialize(new
            {
                recordType = "qbagentDisplayedOutput",
                createdAtUtc,
                displayedOutput,
            }, JsonOptions),
        };

        lock (ArtifactGate)
        {
            Directory.CreateDirectory(artifactRoot);
            File.WriteAllLines(transcriptPath, lines);
        }

        return transcriptPath;
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

        public ConcurrentBag<string> Inputs { get; } = [];

        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            Inputs.Add(request.Input);
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
    private sealed class FakeProcessRunner(ProcessRunResult result) : IProcessRunner
    {
        public Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct = default)
            => Task.FromResult(result);

        public Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken ct = default)
            => Task.FromResult(result);
    }

    private sealed class FakePowerShellSessionManager(PowerShellSessionCommandResult commandResult) : IHostedPowerShellSessionManager
    {
        public PowerShellSessionCreateResult CreateSession(string workspacePath, string? workingDirectory = null)
            => new() { Success = true, SessionId = "ps-qbagent-test", CurrentLocation = workspacePath };

        public Task<PowerShellSessionCommandResult> ExecuteCommandAsync(
            string sessionId,
            string command,
            CancellationToken cancellationToken = default)
            => Task.FromResult(commandResult);

        public Task<PowerShellSessionCommandResult> ExecuteInteractiveCommandAsync(
            string sessionId,
            string command,
            Func<CancellationToken, string?> readLine,
            TextWriter outputWriter,
            TextWriter errorWriter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(commandResult);

        public PowerShellSessionCloseResult CloseSession(string sessionId)
            => new() { Success = true, SessionId = sessionId };

        public void Dispose()
        {
        }
    }
}
