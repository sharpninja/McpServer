using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class VoiceConversationServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_SameAgentAndWorkspace_ReusesExistingIdleSession()
    {
        using var service = CreateService();

        var first = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-a",
            OneShotSession = false,
        }).ConfigureAwait(true);

        var second = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-a",
            OneShotSession = false,
        }).ConfigureAwait(true);

        Assert.Equal(first.SessionId, second.SessionId);
    }

    [Fact]
    public async Task CreateSessionAsync_SameAgentDifferentWorkspace_DoesNotReuseSession()
    {
        using var service = CreateService();

        var first = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-a",
            OneShotSession = false,
        }).ConfigureAwait(true);

        var second = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-b",
            OneShotSession = false,
        }).ConfigureAwait(true);

        Assert.NotEqual(first.SessionId, second.SessionId);
    }

    [Fact]
    public async Task SendSessionMessageAsync_OneShotSession_ReturnsFalse()
    {
        using var service = CreateService();
        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            OneShotSession = true,
        }).ConfigureAwait(true);

        var sent = await service.SendSessionMessageAsync(created.SessionId, "User is here.").ConfigureAwait(true);

        Assert.False(sent);
    }

    [Fact]
    public async Task CreateSessionAsync_UsesConfiguredDefaultExecutionStrategy_WhenRequestOmitsOne()
    {
        using var service = CreateService(defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
        }).ConfigureAwait(true);

        Assert.Equal(AgentExecutionStrategyNames.HostedMcpAgent, created.ExecutionStrategy);
    }

    [Fact]
    public async Task CreateSessionAsync_ExplicitExecutionStrategy_OverridesConfiguredDefault()
    {
        using var service = CreateService(defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            ExecutionStrategy = AgentExecutionStrategyNames.CopilotCli,
        }).ConfigureAwait(true);

        Assert.Equal(AgentExecutionStrategyNames.CopilotCli, created.ExecutionStrategy);
    }

    [Fact]
    public async Task SubmitTurnAsync_IncludesConfiguredModelApiKeyInExecutionOptions()
    {
        var hostedStrategy = new CapturingAgentExecutionStrategy(AgentExecutionStrategyNames.HostedMcpAgent);
        using var service = CreateService(
            defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent,
            modelApiKey: "voice-model-key",
            modelApiKeyEnvironmentVariableName: "ANTHROPIC_API_KEY",
            hostedStrategy: hostedStrategy);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
        }).ConfigureAwait(true);
        var response = await service.SubmitTurnAsync(created.SessionId, new VoiceTurnRequest
        {
            UserTranscriptText = "hello",
        }).ConfigureAwait(true);

        Assert.NotNull(response);
        Assert.Equal("completed", response!.Status);
        Assert.NotNull(hostedStrategy.LastRequest);
        Assert.Equal(
            "voice-model-key",
            hostedStrategy.LastRequest!.Options.EnvironmentVariables["ANTHROPIC_API_KEY"]);
    }

    private static VoiceConversationService CreateService(
        string defaultExecutionStrategy = AgentExecutionStrategyNames.CopilotCli,
        string? modelApiKey = null,
        string modelApiKeyEnvironmentVariableName = "OPENAI_API_KEY",
        IAgentExecutionStrategy? hostedStrategy = null)
    {
        var copilotClient = Substitute.For<ICopilotClient>();
        var workspaceAccessor = CreateWorkspaceAccessor();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(Environment.CurrentDirectory);
        hostEnvironment.EnvironmentName.Returns("Test");
        hostEnvironment.ApplicationName.Returns("McpServer.Support.Mcp.Tests");
        var strategyResolver = new AgentExecutionStrategyResolver(
        [
            new CopilotCliAgentExecutionStrategy(copilotClient),
            hostedStrategy ?? new FakeAgentExecutionStrategy(AgentExecutionStrategyNames.HostedMcpAgent),
        ]);
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var issueSyncService = Substitute.For<IIssueTodoSyncService>();
        var services = new ServiceCollection();
        services.AddSingleton<IAgentExecutionStrategyResolver>(strategyResolver);
        services.AddSingleton(gitHubCliService);
        services.AddSingleton(issueSyncService);
        services.AddSingleton(new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance));
        services.AddSingleton(new TodoUpdateService(workspaceAccessor, issueSyncService, NullLogger<TodoUpdateService>.Instance));
        var serviceProvider = services.BuildServiceProvider();

        return new VoiceConversationService(
            copilotClient,
            serviceProvider,
            workspaceAccessor,
            configuration,
            CreateOptionsMonitor(new VoiceConversationOptions
            {
                Enabled = true,
                CopilotModel = "gpt-5.3-codex",
                DefaultExecutionStrategy = defaultExecutionStrategy,
                ModelApiKey = modelApiKey,
                ModelApiKeyEnvironmentVariableName = modelApiKeyEnvironmentVariableName,
                SessionIdleTimeoutMinutes = TimeSpan.FromMinutes(15),
            }),
            CreateOptionsMonitor(new TodoPromptOptions { BaseUrl = "http://localhost:7147" }),
            hostEnvironment,
            NullLogger<VoiceConversationService>.Instance,
            NullLoggerFactory.Instance);
    }

    private static WorkspaceServiceAccessor CreateWorkspaceAccessor()
    {
        var todoService = Substitute.For<ITodoService>();
        var todoFactory = Substitute.For<ITodoServiceFactory>();
        todoFactory.CreateForWorkspace(Arg.Any<string>(), Arg.Any<WorkspaceContext>()).Returns(todoService);
        var resolver = new TodoServiceResolver(
            todoService,
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = Environment.CurrentDirectory }),
            todoFactory);

        return new WorkspaceServiceAccessor(
            resolver,
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = Environment.CurrentDirectory }));
    }

    private static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value) where T : class
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        monitor.Get(Arg.Any<string>()).Returns(value);
        monitor.OnChange(Arg.Any<Action<T, string?>>()).Returns(new NoopDisposable());
        return monitor;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class FakeAgentExecutionStrategy(string name) : IAgentExecutionStrategy
    {
        public string Name { get; } = name;

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IAgentExecutionSession>(new FakeAgentExecutionSession());
    }

    private sealed class CapturingAgentExecutionStrategy(string name) : IAgentExecutionStrategy
    {
        public AgentExecutionSessionRequest? LastRequest { get; private set; }

        public string Name { get; } = name;

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return ValueTask.FromResult<IAgentExecutionSession>(new FakeAgentExecutionSession());
        }
    }

    private sealed class FakeAgentExecutionSession : IAgentExecutionSession
    {
        private const string FinalResponseBody = """{"type":"final_response","displayText":"done","speakText":"done"}""";

        public bool IsAlive => true;

        public int? ProcessId => null;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task EndAsync(TimeSpan timeout) => Task.CompletedTask;

        public Task<CopilotResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CopilotResult { Body = FinalResponseBody, State = CopilotResultState.Success });

        public IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable();

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<CopilotResult> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CopilotResult { Body = FinalResponseBody, State = CopilotResultState.Success });

        public IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable();

        private static async IAsyncEnumerable<string> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

