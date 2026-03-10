using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Runtime orchestration tests for <see cref="AgentService"/>.
/// </summary>
public sealed class AgentServiceRuntimeTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly IAgentProcessManager _processManager;
    private readonly IChangeEventBus _eventBus;
    private readonly AgentIsolationStrategyResolver _isolationResolver;
    private readonly AgentBranchStrategyResolver _branchResolver;
    private readonly AgentService _sut;

    public AgentServiceRuntimeTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"AgentServiceRuntimeTests_{Guid.NewGuid()}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();

        _processManager = Substitute.For<IAgentProcessManager>();
        _eventBus = Substitute.For<IChangeEventBus>();

        _isolationResolver = new AgentIsolationStrategyResolver(
        [
            new NoneAgentIsolationStrategy(),
        ]);

        _branchResolver = new AgentBranchStrategyResolver(
        [
            new DirectAgentBranchStrategy(Substitute.For<IProcessRunner>()),
        ]);

        _sut = new AgentService(
            _db,
            NullLogger<AgentService>.Instance,
            _eventBus,
            _processManager,
            _isolationResolver,
            _branchResolver);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task LaunchAgentAsync_EnabledConfig_DelegatesToProcessManager()
    {
        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "agent-service-launch"));
        SeedDefinitionAndWorkspace(workspacePath, enabled: true, banned: false, launchCommand: "agent --workspace {workspacePath} --id {agentId}");
        _processManager.LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentProcessInfo
            {
                AgentId = "planner",
                WorkspacePath = workspacePath,
                Status = AgentProcessStatus.Running,
                WorkDirectory = workspacePath,
            }));

        var result = await _sut.LaunchAgentAsync(workspacePath, "planner").ConfigureAwait(true);

        Assert.Equal("planner", result.AgentId);
        await _processManager.Received(1).LaunchAsync(
            workspacePath,
            "planner",
            Arg.Is<string>(command => command.Contains("--id planner", StringComparison.Ordinal)),
            workspacePath,
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task LaunchAgentAsync_BannedAgent_ThrowsInvalidOperationException()
    {
        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "agent-service-banned"));
        SeedDefinitionAndWorkspace(workspacePath, enabled: true, banned: true, launchCommand: "agent");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.LaunchAgentAsync(workspacePath, "planner")).ConfigureAwait(true);
        await _processManager.DidNotReceive().LaunchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task LaunchAgentAsync_MissingWorkspaceConfig_ThrowsInvalidOperationException()
    {
        _db.AgentDefinitions.Add(new AgentDefinitionEntity
        {
            Id = "planner",
            DisplayName = "Planner",
            DefaultLaunchCommand = "agent",
            DefaultInstructionFile = string.Empty,
            DefaultModelsJson = "[]",
            DefaultBranchStrategy = "direct",
            DefaultSeedPrompt = string.Empty,
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync().ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.LaunchAgentAsync("C:/missing-ws", "planner")).ConfigureAwait(true);
    }

    [Fact]
    public async Task StopAgentAsync_RunningAgent_DelegatesToProcessManager()
    {
        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "agent-service-stop"));
        SeedDefinitionAndWorkspace(workspacePath, enabled: true, banned: false, launchCommand: "agent");
        _processManager.StopAsync(workspacePath, "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _processManager.GetStatusAsync(workspacePath, "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentProcessInfo?>(new AgentProcessInfo
            {
                AgentId = "planner",
                WorkspacePath = workspacePath,
                Status = AgentProcessStatus.Stopped,
                WorkDirectory = workspacePath,
                ExitCode = 0,
            }));

        var result = await _sut.StopAgentAsync(workspacePath, "planner").ConfigureAwait(true);

        Assert.True(result);
        await _processManager.Received(1).StopAsync(workspacePath, "planner", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private void SeedDefinitionAndWorkspace(string workspacePath, bool enabled, bool banned, string launchCommand)
    {
        var definition = new AgentDefinitionEntity
        {
            Id = "planner",
            DisplayName = "Planner",
            DefaultLaunchCommand = launchCommand,
            DefaultInstructionFile = string.Empty,
            DefaultModelsJson = "[]",
            DefaultBranchStrategy = "direct",
            DefaultSeedPrompt = string.Empty,
            IsBuiltIn = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        };

        _db.AgentDefinitions.Add(definition);
        _db.AgentWorkspaces.Add(new AgentWorkspaceEntity
        {
            AgentDefinitionId = "planner",
            WorkspacePath = workspacePath,
            Enabled = enabled,
            Banned = banned,
            AgentIsolation = "none",
            AddedAt = DateTime.UtcNow,
            RestartPolicy = "never",
        });
        _db.SaveChanges();
    }
}
