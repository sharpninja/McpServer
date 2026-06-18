using McpServer.Client;
using McpServer.McpAgent;
using McpServer.McpAgent.Hosting;
using McpServer.QBAgent;
using McpServer.QBAgent.Skills;
using McpServer.QBAgent.Tools;
using McpServer.Support.Mcp.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-QBTOOLSINT-001: End-to-end test of the QBAgent tool + skill surface. The real Microsoft Agent
/// Framework loop runs with QuadBrain (scripted) as the model over the in-memory server; the agent loads a skill
/// then writes and edits a workspace file, with the file tools routed through the MCP client to the server's
/// RepoFileService (FR-MCP-QBTOOLS-001/006/007, FR-MCP-QBSKILLS-002).
/// </summary>
public sealed class QBAgentToolsIntegrationTests
{
    /// <summary>The agent loads a skill, then write_file + edit_file land on the server workspace file.</summary>
    [Fact]
    public async Task Agent_LoadsSkill_ThenWritesAndEditsFile_ThroughServer()
    {
        // The test workspace allowlist permits src/McpServer.Cqrs/**/*.cs, so the agent operates on that path.
        var orchestration = new ScriptedOrchestration(
            "{\"tool_calls\":[{\"name\":\"load_skill\",\"arguments\":{\"name\":\"e2e-skill\"}}]}",
            "{\"tool_calls\":[{\"name\":\"write_file\",\"arguments\":{\"path\":\"src/McpServer.Cqrs/E2e.cs\",\"content\":\"class World {}\"}}]}",
            "{\"tool_calls\":[{\"name\":\"edit_file\",\"arguments\":{\"path\":\"src/McpServer.Cqrs/E2e.cs\",\"oldString\":\"World\",\"newString\":\"There\"}}]}",
            "done");
        using var factory = BuildFactory(orchestration);
        var token = ResolveToken(factory);
        var skillsRoot = CreateSkillsRoot();

        try
        {
            await using var agentProvider = BuildAgentProvider(factory, token);
            var agent = agentProvider.GetRequiredService<IMcpHostedAgent>();

            // File tools route through an MCP client pointed at the in-memory server.
            using var serverHttp = new HttpClient(factory.Server.CreateHandler()) { BaseAddress = new Uri("http://localhost") };
            var client = new McpServerClient(serverHttp, new McpServerClientOptions { ApiKey = token, WorkspacePath = factory.WorkspacePath });

            var runner = new FakeProcessRunner();
            using var toolSet = QBAgentExternalToolSurface.Create(client, agent.PowerShellSessions, runner, factory.WorkspacePath, allowGitPush: false);
            var registry = new SkillRegistry([skillsRoot], new SkillManifestParser());
            var tools = new List<AITool>(toolSet.Tools);
            tools.AddRange(new SkillTool(registry).CreateTools());

            // QuadBrain is the model, routed over the in-memory server.
            using var modelHttp = new HttpClient(factory.Server.CreateHandler());
            using var chatClient = QBAgentChatClientFactory.Create(
                new McpAgentOptions { BaseUrl = new Uri("http://localhost"), ApiKey = token }, modelHttp);
            var chatAgent = agent.CreateChatClientAgent(chatClient);
            var runOptions = agent.CreateRunOptions(new ChatClientAgentRunOptions { ChatOptions = new ChatOptions { Tools = tools } });
            var session = await chatAgent.CreateSessionAsync().ConfigureAwait(true);

            var response = await chatAgent.RunAsync(
                [new ChatMessage(ChatRole.User, "do the task")], session, runOptions).ConfigureAwait(true);

            Assert.Equal(4, orchestration.Calls); // load_skill, write_file, edit_file, final
            Assert.Contains("done", response.Text, StringComparison.Ordinal);

            var written = await File.ReadAllTextAsync(
                Path.Combine(factory.WorkspacePath, "src", "McpServer.Cqrs", "E2e.cs")).ConfigureAwait(true);
            Assert.Equal("class There {}", written);
        }
        finally
        {
            if (Directory.Exists(skillsRoot))
                Directory.Delete(skillsRoot, true);
        }
    }

    private static CustomWebApplicationFactory BuildFactory(IQuadBrainOrchestrationService orchestration)
        => new(services =>
        {
            services.RemoveAll<IQuadBrainOrchestrationService>();
            services.AddSingleton(orchestration);
        });

    private static ServiceProvider BuildAgentProvider(CustomWebApplicationFactory factory, string token)
    {
        var services = new ServiceCollection();
        services.AddMcpServerMcpAgent(options =>
        {
            options.BaseUrl = new Uri("http://localhost");
            options.ApiKey = token;
            options.WorkspacePath = factory.WorkspacePath;
        });
        return services.BuildServiceProvider();
    }

    private static string ResolveToken(CustomWebApplicationFactory factory)
    {
        var probe = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(probe, factory.Services);
        return probe.DefaultRequestHeaders.GetValues("X-Api-Key").First();
    }

    private static string CreateSkillsRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"qbe2e_skills_{Guid.NewGuid():N}");
        var skillDir = Path.Combine(root, "e2e-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"),
            "---\nname: e2e-skill\ndescription: An end-to-end test skill.\n---\n\nDo the task carefully.\n");
        return root;
    }

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

    private sealed class FakeProcessRunner : IProcessRunner
    {
        public Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct = default)
            => Task.FromResult(new ProcessRunResult(0, "ok", null));

        public Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken ct = default)
            => Task.FromResult(new ProcessRunResult(0, "ok", null));
    }
}
