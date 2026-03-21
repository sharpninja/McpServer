using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// Runtime endpoint tests for <see cref="AgentController"/>.
/// </summary>
public sealed class AgentControllerRuntimeTests
{
    [Fact]
    public async Task LaunchAgent_WhenWorkspaceMissing_ReturnsBadRequest()
    {
        var service = Substitute.For<IAgentService>();
        var controller = CreateController(service);

        var result = await controller.LaunchAgent("planner", null, CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Workspace path required.", badRequest.Value);
    }

    [Fact]
    public async Task LaunchAgent_WhenServiceReturnsInfo_ReturnsOk()
    {
        var service = Substitute.For<IAgentService>();
        service.LaunchAgentAsync("C:/ws", "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentProcessInfo
            {
                AgentId = "planner",
                WorkspacePath = "C:/ws",
                Status = AgentProcessStatus.Running,
            }));

        var controller = CreateController(service, "C:/ws");

        var result = await controller.LaunchAgent("planner", null, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<AgentProcessInfo>(ok.Value);
        Assert.Equal("planner", payload.AgentId);
    }

    [Fact]
    public async Task StopAgent_WhenNotRunning_ReturnsConflict()
    {
        var service = Substitute.For<IAgentService>();
        service.StopAgentAsync("C:/ws", "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var controller = CreateController(service, "C:/ws");

        var result = await controller.StopAgent("planner", null, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task GetProcessStatus_WhenMissing_ReturnsNotFound()
    {
        var service = Substitute.For<IAgentService>();
        service.GetAgentProcessStatusAsync("C:/ws", "planner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AgentProcessInfo?>(null));

        var controller = CreateController(service, "C:/ws");

        var result = await controller.GetProcessStatus("planner", null, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ListRunningAgents_ReturnsWrappedPayload()
    {
        var service = Substitute.For<IAgentService>();
        service.ListRunningAgentsAsync("C:/ws", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentProcessInfo>>(
            [
                new AgentProcessInfo
                {
                    AgentId = "planner",
                    WorkspacePath = "C:/ws",
                    Status = AgentProcessStatus.Running,
                }
            ]));

        var controller = CreateController(service, "C:/ws");

        var result = await controller.ListRunningAgents(null, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    private static AgentController CreateController(IAgentService service, string? workspacePath = null)
    {
        var controller = new AgentController(service, NullLogger<AgentController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            }
        };

        if (workspacePath is not null)
            controller.HttpContext.Items["WorkspacePath"] = workspacePath;

        return controller;
    }
}
