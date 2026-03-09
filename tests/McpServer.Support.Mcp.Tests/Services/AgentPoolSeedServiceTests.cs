using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

public sealed class AgentPoolSeedServiceTests
{
    [Fact]
    public async Task StartAsync_SeedsEachEnabledWorkspace()
    {
        var pool = Substitute.For<IAgentPoolService>();
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkspaceListResult(
            [
                CreateWorkspace(@"E:\ws-a", enabled: true),
                CreateWorkspace(@"E:\ws-b", enabled: false),
                CreateWorkspace(@"E:\ws-c", enabled: true),
            ], 3)));

        var scopeFactory = CreateScopeFactory(workspaceService);

        var service = new AgentPoolSeedService(
            pool,
            scopeFactory,
            CreateOptionsMonitor(new AgentPoolOptions
            {
                Enabled = true,
                Agents = [new AgentPoolDefinitionOptions { AgentName = "planner" }]
            }),
            NullLogger<AgentPoolSeedService>.Instance);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(true);

        await pool.Received(1).SeedWorkspaceAgentsAsync(@"E:\ws-a", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await pool.Received(1).SeedWorkspaceAgentsAsync(@"E:\ws-c", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await pool.DidNotReceive().SeedWorkspaceAgentsAsync(@"E:\ws-b", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task StartAsync_WhenPoolDisabled_DoesNotSeed()
    {
        var pool = Substitute.For<IAgentPoolService>();
        var workspaceService = Substitute.For<IWorkspaceService>();
        var scopeFactory = CreateScopeFactory(workspaceService);

        var service = new AgentPoolSeedService(
            pool,
            scopeFactory,
            CreateOptionsMonitor(new AgentPoolOptions { Enabled = false }),
            NullLogger<AgentPoolSeedService>.Instance);

        await service.StartAsync(CancellationToken.None).ConfigureAwait(true);

        await workspaceService.DidNotReceive().ListAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await pool.DidNotReceive().SeedWorkspaceAgentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static WorkspaceDto CreateWorkspace(string path, bool enabled)
        => new()
        {
            WorkspacePath = path,
            Name = Path.GetFileName(path),
            TodoPath = "docs/Project/TODO.yaml",
            IsEnabled = enabled,
            IsPrimary = false,
            DateTimeCreated = DateTimeOffset.UtcNow,
            DateTimeModified = DateTimeOffset.UtcNow,
            StatusPrompt = "status",
            ImplementPrompt = "implement",
            PlanPrompt = "plan",
        };

    private static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value) where T : class
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        monitor.Get(Arg.Any<string>()).Returns(value);
        monitor.OnChange(Arg.Any<Action<T, string?>>()).Returns(new NoopDisposable());
        return monitor;
    }

    private static IServiceScopeFactory CreateScopeFactory(IWorkspaceService workspaceService)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IWorkspaceService)).Returns(workspaceService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        return scopeFactory;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
