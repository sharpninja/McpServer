using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task SubmitTurnAsync_StrategyProgressOnlyOutput_ReturnsIncompleteAndDoesNotPersistFinalAssistantTranscript()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService(
            new FakeAgentExecutionStrategy(
                "test-strategy",
                new AgentCliResult
                {
                    State = AgentCliResultState.Success,
                    Body = "Following workspace bootstrap, then answering from the evidence.",
                }),
            useEchoHelperFallback: false);
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest
            {
                WorkspacePath = workspaceRoot,
                ExecutionStrategy = "test-strategy",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = "Please answer directly now." },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("incomplete", result!.Status);
        Assert.Null(result.AssistantDisplayText);
        Assert.Contains("FINAL ANSWER", result.Error, StringComparison.OrdinalIgnoreCase);

        var status = await service.GetStatusAsync(created.SessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(status);
        Assert.False(status!.IsTurnActive);
        Assert.Equal("incomplete", status.Status);

        var transcript = await service.GetTranscriptAsync(created.SessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(transcript);
        Assert.DoesNotContain(
            transcript!.Items,
            item => string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Category, "transcript", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            transcript.Items,
            item => string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Category, "progress", StringComparison.OrdinalIgnoreCase)
                && item.Text.Contains("Following workspace bootstrap", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SubmitTurnAsync_StrategyPlanOnlyOutput_ReturnsIncompleteAndRetainsProgressOnly()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService(
            new FakeAgentExecutionStrategy(
                "test-strategy",
                new AgentCliResult
                {
                    State = AgentCliResultState.Success,
                    Body = "Plan: inspect the marker, check the TODO state, then provide the final recommendation.",
                }),
            useEchoHelperFallback: false);
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest
            {
                WorkspacePath = workspaceRoot,
                ExecutionStrategy = "test-strategy",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = "What is the fix?" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("incomplete", result!.Status);
        Assert.Null(result.AssistantDisplayText);
        Assert.Contains("FINAL ANSWER", result.Error, StringComparison.OrdinalIgnoreCase);

        var transcript = await service.GetTranscriptAsync(created.SessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(transcript);
        Assert.DoesNotContain(
            transcript!.Items,
            item => string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Category, "transcript", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            transcript.Items,
            item => string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Category, "progress", StringComparison.OrdinalIgnoreCase)
                && item.Text.Contains("Plan: inspect", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SubmitTurnAsync_StrategyEmptyFinalAnswerMarker_ReturnsIncomplete()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService(
            new FakeAgentExecutionStrategy(
                "test-strategy",
                new AgentCliResult
                {
                    State = AgentCliResultState.Success,
                    Body = "I inspected the issue.\nFINAL ANSWER:\n   ",
                }),
            useEchoHelperFallback: false);
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest
            {
                WorkspacePath = workspaceRoot,
                ExecutionStrategy = "test-strategy",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = "What is the fix?" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("incomplete", result!.Status);
        Assert.Null(result.AssistantDisplayText);
    }

    [Fact]
    public async Task SubmitTurnAsync_StrategyFailure_PersistsErrorTranscript()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService(
            new FakeAgentExecutionStrategy(
                "test-strategy",
                new AgentCliResult
                {
                    State = AgentCliResultState.Error,
                    Stderr = "helper timed out after 00:02:00",
                }),
            useEchoHelperFallback: false);
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest
            {
                WorkspacePath = workspaceRoot,
                ExecutionStrategy = "test-strategy",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = "Run the helper." },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("error", result!.Status);

        var transcript = await service.GetTranscriptAsync(created.SessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(transcript);
        Assert.Contains(
            transcript!.Items,
            item => string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Category, "error", StringComparison.OrdinalIgnoreCase)
                && item.Text.Contains("helper timed out", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SubmitTurnAsync_StrategyReceivesFiniteHelperTimeout()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var strategy = new CapturingAgentExecutionStrategy(
            "test-strategy",
            new AgentCliResult
            {
                State = AgentCliResultState.Success,
                Body = "FINAL ANSWER: use the bounded helper timeout.",
            });
        var service = CreateService(
            strategy,
            useEchoHelperFallback: false,
            configureOptions: options => options.HelperTimeout = TimeSpan.FromSeconds(75));
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest
            {
                WorkspacePath = workspaceRoot,
                ExecutionStrategy = "test-strategy",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = "Check timeout wiring." },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("completed", result!.Status);
        Assert.Equal(TimeSpan.FromSeconds(75), strategy.LastRequest?.Options.Timeout);
    }

    [Fact]
    public async Task SubmitTurnAsync_StrategyFinalAnswerMarker_PersistsFinalAnswerOnly()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var service = CreateService(
            new FakeAgentExecutionStrategy(
                "test-strategy",
                new AgentCliResult
                {
                    State = AgentCliResultState.Success,
                    Body = "I will inspect the workspace.\nFINAL ANSWER:\nUse workflow.todo.update with id BUG-TRIAGE-022.",
                }),
            useEchoHelperFallback: false);
        var created = await service.CreateSessionAsync(
            new AgentHelpSessionCreateRequest
            {
                WorkspacePath = workspaceRoot,
                ExecutionStrategy = "test-strategy",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await service.SubmitTurnAsync(
            created.SessionId,
            new AgentHelpTurnRequest { UserMessage = "How do I update the TODO?" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("completed", result!.Status);
        Assert.Equal("Use workflow.todo.update with id BUG-TRIAGE-022.", result.AssistantDisplayText);

        var transcript = await service.GetTranscriptAsync(created.SessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(transcript);
        var assistant = Assert.Single(
            transcript!.Items,
            item => string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Category, "transcript", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Use workflow.todo.update with id BUG-TRIAGE-022.", assistant.Text);
        Assert.DoesNotContain("I will inspect", assistant.Text, StringComparison.Ordinal);
    }

    private static AgentHelpConversationService CreateService(
        IAgentExecutionStrategy? strategy = null,
        bool useEchoHelperFallback = true,
        Action<AgentHelpOptions>? configureOptions = null)
    {
        var options = new AgentHelpOptions
        {
            Enabled = true,
            UseEchoHelperFallback = useEchoHelperFallback,
            GuardEnabled = true,
            CorpusBootstrapEnabled = false,
        };
        configureOptions?.Invoke(options);
        var monitor = new AgentHelpTestOptionsMonitor<AgentHelpOptions>(options);
        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "." });
        var primaryTodo = Substitute.For<ITodoService>();
        var todoFactory = Substitute.For<ITodoServiceFactory>();
        todoFactory.CreatePrimary().Returns(primaryTodo);
        todoFactory.CreateForWorkspace(Arg.Any<string>(), Arg.Any<WorkspaceContext>()).Returns(primaryTodo);
        var accessor = new WorkspaceServiceAccessor(
            new TodoServiceResolver(primaryTodo, ingestionOptions, todoFactory),
            new HttpContextAccessor(),
            ingestionOptions);
        var services = new ServiceCollection();
        if (strategy is not null)
            services.AddSingleton<IAgentExecutionStrategyResolver>(new FakeAgentExecutionStrategyResolver(strategy));
        var serviceProvider = services.BuildServiceProvider();

        return new AgentHelpConversationService(
            new AgentHelpInboundGuard(),
            new HelpTranscriptWriter(monitor, NullLogger<HelpTranscriptWriter>.Instance),
            new AgentHelpIncidentLogger(monitor, NullLogger<AgentHelpIncidentLogger>.Instance),
            new AgentHelpCorpusService(
                monitor,
                new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                AgentHelpPinnedPathResolverTestFactory.Create(),
                NullLogger<AgentHelpCorpusService>.Instance),
            accessor,
            monitor,
            NullLogger<AgentHelpConversationService>.Instance,
            serviceProvider);
    }

    private sealed class FakeAgentExecutionStrategyResolver(IAgentExecutionStrategy strategy) : IAgentExecutionStrategyResolver
    {
        public IAgentExecutionStrategy Resolve(string? strategyName) => strategy;
    }

    private class FakeAgentExecutionStrategy(string name, AgentCliResult result) : IAgentExecutionStrategy
    {
        public string Name { get; } = name;

        public virtual ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IAgentExecutionSession>(new FakeAgentExecutionSession(result));
    }

    private sealed class CapturingAgentExecutionStrategy(string name, AgentCliResult result)
        : FakeAgentExecutionStrategy(name, result)
    {
        public AgentExecutionSessionRequest? LastRequest { get; private set; }

        public override ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return base.CreateSessionAsync(request, cancellationToken);
        }
    }

    private sealed class FakeAgentExecutionSession(AgentCliResult result) : IAgentExecutionSession
    {
        public bool IsAlive => false;

        public int? ProcessId => null;

        public Task<AgentCliResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public async IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(result.Body))
                yield return result.Body;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public Task<AgentCliResult> SendAsync(string prompt, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public async IAsyncEnumerable<string> SendStreamingAsync(
            string prompt,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(result.Body))
                yield return result.Body;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EndAsync(TimeSpan timeout) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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
