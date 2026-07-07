using McpServer.Common.AgentCli;
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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var second = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-a",
            OneShotSession = false,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var second = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            AgentModel = "gpt-5.3-codex",
            WorkspacePath = @"E:\ws-b",
            OneShotSession = false,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var sent = await service.SendSessionMessageAsync(created.SessionId, "User is here.", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(sent);
    }

    [Fact]
    public async Task CreateSessionAsync_UsesConfiguredDefaultExecutionStrategy_WhenRequestOmitsOne()
    {
        using var service = CreateService(defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var response = await service.SubmitTurnAsync(created.SessionId, new VoiceTurnRequest
        {
            UserTranscriptText = "hello",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(response);
        Assert.Equal("completed", response!.Status);
        Assert.NotNull(hostedStrategy.LastRequest);
        Assert.Equal(
            "voice-model-key",
            hostedStrategy.LastRequest!.Options.EnvironmentVariables["ANTHROPIC_API_KEY"]);
    }

    [Fact]
    public async Task SubmitTurnAsync_UsesInfiniteCopilotTimeoutInExecutionOptions()
    {
        var hostedStrategy = new CapturingAgentExecutionStrategy(AgentExecutionStrategyNames.HostedMcpAgent);
        using var service = CreateService(
            defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent,
            hostedStrategy: hostedStrategy);

        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var response = await service.SubmitTurnAsync(created.SessionId, new VoiceTurnRequest
        {
            UserTranscriptText = "hello",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(response);
        Assert.NotNull(hostedStrategy.LastRequest);
        Assert.Equal(Timeout.InfiniteTimeSpan, hostedStrategy.LastRequest!.Options.Timeout);
    }

    [Fact]
    public async Task SubmitTurnStreamingAsync_EmptyRuntimeStream_EmitsErrorAndRecordsAssistantError()
    {
        using var service = CreateService(defaultExecutionStrategy: AgentExecutionStrategyNames.HostedMcpAgent);
        var created = await service.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            AgentName = "planner",
            WorkspacePath = @"E:\ws-a",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var events = await CollectAsync(service.SubmitTurnStreamingAsync(created.SessionId, new VoiceTurnRequest
        {
            UserTranscriptText = "Read the file .github/copilot-instructions.md and follow those instructions.",
        }, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);

        var terminal = Assert.Single(events, static evt => evt.Type is "done" or "error");
        Assert.Equal("error", terminal.Type);
        Assert.Contains("No response returned from voice runtime", terminal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(events, static evt => evt.Type == "done" && evt.Status == "completed");

        var status = await service.GetStatusAsync(created.SessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(status);
        Assert.Equal("error", status!.Status);
        Assert.Contains("No response returned from voice runtime", status.LastError, StringComparison.Ordinal);
        Assert.Equal(2, status.TranscriptCount);

        var transcript = await service.GetTranscriptAsync(created.SessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(transcript);
        Assert.Equal(2, transcript!.Items.Count);
        Assert.Contains(transcript.Items, static item => item.Role == "user");
        var assistant = Assert.Single(transcript.Items, static item => item.Role == "assistant");
        Assert.Equal("error", assistant.Category);
        Assert.Contains("No response returned from voice runtime", assistant.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentCliInteractiveSession_ReadInitialResponseStreamingAsync_StdoutClosesAfterNonzeroExit_ThrowsDiagnostic()
    {
        const string stderr = "GitHub Copilot CLI requires Node.js v24 or higher.";
        var process = new FakeSpawnedProcess(stdout: string.Empty, stderr: stderr, exitCode: 1);
        var spawner = new FakeProcessSpawner(process);
        var processEnvironment = Substitute.For<IProcessEnvironmentService>();
        processEnvironment
            .ResolveExecutable(Arg.Any<System.Diagnostics.ProcessStartInfo>(), Arg.Any<string>())
            .Returns(call => call.ArgAt<string>(1));
        var client = new AgentCliClient(
            CreateOptionsMonitor(new AgentCliClientOptions
            {
                AgentPath = "copilot",
                Model = "auto",
                WorkingDirectory = Environment.CurrentDirectory,
                Timeout = Timeout.InfiniteTimeSpan,
            }),
            processEnvironment,
            spawner,
            NullLogger<AgentCliClient>.Instance);

        await using var session = client.CreateInteractiveSession("hello");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in session.ReadInitialResponseStreamingAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true))
            {
            }
        }).ConfigureAwait(true);

        Assert.Contains("CLI agent exited with code 1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Node.js v24", exception.Message, StringComparison.Ordinal);
    }

    private static VoiceConversationService CreateService(
        string defaultExecutionStrategy = AgentExecutionStrategyNames.OneShotCli,
        string? modelApiKey = null,
        string modelApiKeyEnvironmentVariableName = "OPENAI_API_KEY",
        IAgentExecutionStrategy? hostedStrategy = null,
        IAgentExecutionStrategy? oneShotStrategy = null)
    {
        var copilotClient = Substitute.For<IAgentCliClient>();
        var workspaceAccessor = CreateWorkspaceAccessor();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(Environment.CurrentDirectory);
        hostEnvironment.EnvironmentName.Returns("Test");
        hostEnvironment.ApplicationName.Returns("McpServer.Support.Mcp.Tests");
        var strategyResolver = new AgentExecutionStrategyResolver(
        [
            new CopilotCliAgentExecutionStrategy(copilotClient),
            oneShotStrategy ?? new FakeAgentExecutionStrategy(AgentExecutionStrategyNames.OneShotCli),
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

        public Task<AgentCliResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentCliResult { Body = FinalResponseBody, State = AgentCliResultState.Success });

        public IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable();

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<AgentCliResult> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentCliResult { Body = FinalResponseBody, State = AgentCliResultState.Success });

        public IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable();

        private static async IAsyncEnumerable<string> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> stream)
    {
        var items = new List<T>();
        await foreach (var item in stream.ConfigureAwait(true))
            items.Add(item);
        return items;
    }

    private sealed class FakeProcessSpawner(ISpawnedProcess process) : IProcessSpawner
    {
        public ISpawnedProcess Spawn(System.Diagnostics.ProcessStartInfo startInfo) => process;
    }

    private sealed class FakeSpawnedProcess : ISpawnedProcess
    {
        private readonly MemoryStream _stdout;
        private readonly MemoryStream _stderr;
        private readonly MemoryStream _stdin = new();

        public FakeSpawnedProcess(string stdout, string stderr, int exitCode)
        {
            _stdout = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(stdout));
            _stderr = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(stderr));
            StandardOutput = new StreamReader(_stdout);
            StandardError = new StreamReader(_stderr);
            StandardInput = new StreamWriter(_stdin);
            ExitCode = exitCode;
        }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public StreamWriter? StandardInput { get; }

        public int Id => 1234;

        public bool HasExited => true;

        public int ExitCode { get; }

        public void Dispose()
        {
            StandardInput?.Dispose();
            StandardOutput.Dispose();
            StandardError.Dispose();
            _stdin.Dispose();
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Kill()
        {
        }
    }
}

