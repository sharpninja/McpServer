using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-004: Agent Help session termination and guardrail behavior tests.
/// </summary>
public sealed class AgentHelpConversationServiceTests
{
    [Fact]
    public async Task SubmitTurnAsync_InjectionMessage_TerminatesSessionAndPersistsGuardrailViolation()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService();
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest { WorkspacePath = workspaceRoot },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var injection = File.ReadAllText(
            AgentHelpTestPaths.ResolveFixturePath("injection/ignore-previous-instructions.txt")).Trim();

        var blocked = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = injection },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(blocked);
        Assert.Equal("terminated_guardrail", blocked!.Status);
        Assert.False(string.IsNullOrWhiteSpace(blocked.IncidentId));

        var status = await service.GetStatusAsync(created.SessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(status);
        Assert.True(status!.Terminated);
        Assert.Equal("terminated_guardrail", status.Status);

        var rejected = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = "Can you help with tests?" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(rejected);
        Assert.Equal("terminated_guardrail", rejected!.Status);

        var dataRoot = Path.Combine(workspaceRoot, ".mcpServer");
        var transcriptPath = Path.Combine(dataRoot, "agent-help", "transcripts", $"{created.SessionId}.jsonl");
        Assert.True(File.Exists(transcriptPath));
        var transcriptJson = await File.ReadAllTextAsync(transcriptPath, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains("\"category\":\"guardrail_violation\"", transcriptJson, StringComparison.Ordinal);

        var incidentDir = Path.Combine(dataRoot, "agent-help", "incidents");
        Assert.True(Directory.Exists(incidentDir));
        Assert.NotEmpty(Directory.GetFiles(incidentDir, "*.json"));
    }

    [Fact]
    public async Task SubmitTurnStreamingAsync_InjectionMessage_YieldsSessionTerminatedWithIncidentId()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService();
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest { WorkspacePath = workspaceRoot },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var injection = File.ReadAllText(
            AgentHelpTestPaths.ResolveFixturePath("injection/ignore-previous-instructions.txt")).Trim();

        var events = new List<AgentHelpStreamEvent>();
        await foreach (var evt in service.SubmitTurnStreamingAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = injection },
            TestContext.Current.CancellationToken).ConfigureAwait(false))
        {
            events.Add(evt);
        }

        Assert.Contains(events, e => string.Equals(e.Type, "session_terminated", StringComparison.Ordinal));
        var terminated = events.Single(e => string.Equals(e.Type, "session_terminated", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(terminated.IncidentId));
        Assert.Equal("terminated_guardrail", terminated.Status);
        Assert.DoesNotContain(events, e => string.Equals(e.Type, "chunk", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SubmitTurnStreamingAsync_BenignMessage_YieldsChunkAndDone()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService();
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest { WorkspacePath = workspaceRoot },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var benign = File.ReadAllText(
            AgentHelpTestPaths.ResolveFixturePath("bypass/normal-help-request.txt")).Trim();

        var events = new List<AgentHelpStreamEvent>();
        await foreach (var evt in service.SubmitTurnStreamingAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = benign },
            TestContext.Current.CancellationToken).ConfigureAwait(false))
        {
            events.Add(evt);
        }

        Assert.Contains(events, e => string.Equals(e.Type, "chunk", StringComparison.Ordinal));
        Assert.Contains(events, e => string.Equals(e.Type, "done", StringComparison.Ordinal));
    }

    private static AgentHelpConversationService CreateService()
    {
        var options = new AgentHelpOptions
        {
            Enabled = true,
            UseEchoHelperFallback = true,
            GuardEnabled = true,
            CorpusBootstrapEnabled = false,
        };
        var monitor = new TestOptionsMonitor<AgentHelpOptions>(options);
        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "." });
        var primaryTodo = Substitute.For<ITodoService>();
        var todoFactory = Substitute.For<ITodoServiceFactory>();
        todoFactory.CreatePrimary().Returns(primaryTodo);
        todoFactory.CreateForWorkspace(Arg.Any<string>(), Arg.Any<WorkspaceContext>()).Returns(primaryTodo);
        var accessor = new WorkspaceServiceAccessor(
            new TodoServiceResolver(primaryTodo, ingestionOptions, todoFactory),
            new HttpContextAccessor(),
            ingestionOptions);

        return new AgentHelpConversationService(
            new AgentHelpInboundGuard(),
            new HelpTranscriptWriter(monitor, NullLogger<HelpTranscriptWriter>.Instance),
            new AgentHelpIncidentLogger(monitor, NullLogger<AgentHelpIncidentLogger>.Instance),
            new AgentHelpCorpusService(monitor, NullLogger<AgentHelpCorpusService>.Instance),
            accessor,
            monitor,
            NullLogger<AgentHelpConversationService>.Instance);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class
    {
        public TestOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}