using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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

    private static VoiceConversationService CreateService()
    {
        var workspaceAccessor = CreateWorkspaceAccessor();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(Environment.CurrentDirectory);
        hostEnvironment.EnvironmentName.Returns("Test");
        hostEnvironment.ApplicationName.Returns("McpServer.Support.Mcp.Tests");

        return new VoiceConversationService(
            Substitute.For<ICopilotClient>(),
            workspaceAccessor,
            configuration,
            CreateOptionsMonitor(new VoiceConversationOptions
            {
                Enabled = true,
                CopilotModel = "gpt-5.3-codex",
                SessionIdleTimeoutMinutes = 15,
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
}
