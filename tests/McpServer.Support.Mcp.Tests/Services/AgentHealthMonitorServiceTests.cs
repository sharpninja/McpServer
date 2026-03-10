using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Focused tests for <see cref="AgentHealthMonitorService"/> restart-policy behavior.
/// </summary>
public sealed class AgentHealthMonitorServiceTests
{
    [Fact]
    public async Task ExecuteAsync_OnFailurePolicy_RestartsFailedAgent()
    {
        var processManager = Substitute.For<IAgentProcessManager>();
        var agentService = Substitute.For<IAgentService>();
        processManager.ListRunningAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentProcessInfo>>(
            [
                new AgentProcessInfo
                {
                    AgentId = "planner",
                    WorkspacePath = "C:/ws-a",
                    Status = AgentProcessStatus.Failed,
                    ExitCode = 1,
                }
            ]));
        agentService.GetWorkspaceAgentAsync("C:/ws-a", "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentWorkspaceConfigDto?>(new AgentWorkspaceConfigDto
            {
                AgentId = "planner",
                WorkspacePath = "C:/ws-a",
                RestartPolicy = "on-failure",
            }));
        agentService.LaunchAgentAsync("C:/ws-a", "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentProcessInfo { AgentId = "planner", WorkspacePath = "C:/ws-a", Status = AgentProcessStatus.Running }));

        var options = Options.Create(new AgentProcessManagerOptions
        {
            HealthCheckIntervalSeconds = 3600,
            RestartBackoffBaseSeconds = 0,
            MaxRestarts = 3,
        });
        var sut = new AgentHealthMonitorService(processManager, agentService, options, NullLogger<AgentHealthMonitorService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Record.ExceptionAsync(() => sut.StartAsync(cts.Token)).ConfigureAwait(true);
        await Task.Delay(25).ConfigureAwait(true);
        await sut.StopAsync(CancellationToken.None).ConfigureAwait(true);

        await agentService.Received().LaunchAgentAsync("C:/ws-a", "planner", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ExecuteAsync_NeverPolicy_DoesNotRestartFailedAgent()
    {
        var processManager = Substitute.For<IAgentProcessManager>();
        var agentService = Substitute.For<IAgentService>();
        processManager.ListRunningAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentProcessInfo>>(
            [
                new AgentProcessInfo
                {
                    AgentId = "planner",
                    WorkspacePath = "C:/ws-a",
                    Status = AgentProcessStatus.Failed,
                    ExitCode = 1,
                }
            ]));
        agentService.GetWorkspaceAgentAsync("C:/ws-a", "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentWorkspaceConfigDto?>(new AgentWorkspaceConfigDto
            {
                AgentId = "planner",
                WorkspacePath = "C:/ws-a",
                RestartPolicy = "never",
            }));

        var options = Options.Create(new AgentProcessManagerOptions
        {
            HealthCheckIntervalSeconds = 3600,
            RestartBackoffBaseSeconds = 0,
            MaxRestarts = 3,
        });
        var sut = new AgentHealthMonitorService(processManager, agentService, options, NullLogger<AgentHealthMonitorService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Record.ExceptionAsync(() => sut.StartAsync(cts.Token)).ConfigureAwait(true);
        await Task.Delay(25).ConfigureAwait(true);
        await sut.StopAsync(CancellationToken.None).ConfigureAwait(true);

        await agentService.DidNotReceive().LaunchAgentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }
}
