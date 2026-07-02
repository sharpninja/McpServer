using System.Text;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

[Trait("Category", "Integration")]
public sealed class AgentPoolControllerTests
{
    private static WorkspaceContext CreateWorkspaceContext()
        => new() { WorkspacePath = @"E:\test-workspace" };

    [Fact]
    public async Task StartAgentAsync_FailedMutation_ReturnsBadRequest()
    {
        var service = Substitute.For<IAgentPoolService>();
        service.StartAgentAsync("planner", @"E:\test-workspace", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentPoolMutationResult
            {
                Success = false,
                Error = "boom",
            }));

        var controller = new AgentPoolController(service, CreateWorkspaceContext());
        var result = await controller.StartAgentAsync("planner", CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<AgentPoolMutationResult>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.Equal("boom", payload.Error);
    }

    [Fact]
    public async Task ConnectDefaultAgentAsync_NoEligibleAgent_ReturnsNotFound()
    {
        var service = Substitute.For<IAgentPoolService>();
        service.ConnectInteractiveAsync(null, @"E:\test-workspace", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentPoolConnectResult
            {
                Success = false,
                Error = "none",
            }));

        var controller = new AgentPoolController(service, CreateWorkspaceContext());
        var result = await controller.ConnectDefaultAgentAsync(CancellationToken.None).ConfigureAwait(true);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var payload = Assert.IsType<AgentPoolConnectResult>(notFound.Value);
        Assert.False(payload.Success);
        Assert.Equal("none", payload.Error);
    }

    [Fact]
    public async Task StreamNotificationsAsync_WritesSseDataFrames()
    {
        var service = Substitute.For<IAgentPoolService>();
        service.SubscribeNotificationsAsync(Arg.Any<CancellationToken>())
            .Returns(NotificationStream());

        var controller = new AgentPoolController(service, CreateWorkspaceContext());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        await controller.StreamNotificationsAsync(CancellationToken.None).ConfigureAwait(true);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync().ConfigureAwait(true);
        Assert.Contains("data: ", payload, StringComparison.Ordinal);
        Assert.Contains("\"eventType\":\"queued\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"eventType\":\"completed\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamJobAsync_WritesSseDataFrames()
    {
        var service = Substitute.For<IAgentPoolService>();
        service.SubscribeJobStreamAsync("job-1", Arg.Any<CancellationToken>())
            .Returns(JobStream());

        var controller = new AgentPoolController(service, CreateWorkspaceContext());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        await controller.StreamJobAsync("job-1", CancellationToken.None).ConfigureAwait(true);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync().ConfigureAwait(true);
        Assert.Contains("data: ", payload, StringComparison.Ordinal);
        Assert.Contains("\"eventType\":\"snapshot\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"eventType\":\"completed\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnqueueOneShotAsync_UsesWorkspaceFromContext()
    {
        var service = Substitute.For<IAgentPoolService>();
        service.EnqueueOneShotAsync(Arg.Any<AgentPoolOneShotRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new AgentPoolEnqueueResult { Success = true, JobId = "job-1" }));

        var controller = new AgentPoolController(service, CreateWorkspaceContext());
        var request = new AgentPoolOneShotRequest
        {
            Context = AgentPoolOneShotContext.AdHoc,
            PromptText = "Hello",
            WorkspacePath = @"E:\different-workspace",
        };

        _ = await controller.EnqueueOneShotAsync(request, CancellationToken.None).ConfigureAwait(true);

        await service.Received(1).EnqueueOneShotAsync(
            Arg.Is<AgentPoolOneShotRequest>(r => r != null && r.WorkspacePath == @"E:\test-workspace"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task GetAgentsAsync_IgnoresWorkspaceQueryAndUsesContext()
    {
        var service = Substitute.For<IAgentPoolService>();
        service.GetAgentsAsync(@"E:\test-workspace", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AgentPoolAgentStatusDto>>([]));

        var controller = new AgentPoolController(service, CreateWorkspaceContext());
        _ = await controller.GetAgentsAsync(@"E:\different-workspace", CancellationToken.None).ConfigureAwait(true);

        await service.Received(1).GetAgentsAsync(@"E:\test-workspace", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await service.DidNotReceive().GetAgentsAsync(@"E:\different-workspace", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static async IAsyncEnumerable<AgentPoolNotificationEventDto> NotificationStream()
    {
        yield return new AgentPoolNotificationEventDto
        {
            EventType = "queued",
            JobId = "job-1",
            AgentName = "planner",
        };
        await Task.Yield();
        yield return new AgentPoolNotificationEventDto
        {
            EventType = "completed",
            JobId = "job-1",
            AgentName = "planner",
        };
    }

    private static async IAsyncEnumerable<AgentPoolJobStreamEventDto> JobStream()
    {
        yield return new AgentPoolJobStreamEventDto
        {
            JobId = "job-1",
            EventType = "snapshot",
            Status = "queued",
        };
        await Task.Yield();
        yield return new AgentPoolJobStreamEventDto
        {
            JobId = "job-1",
            EventType = "completed",
            Status = "completed",
            Text = "done",
        };
    }
}
