using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// Unit tests for the federation push endpoint on <see cref="FederationController"/>.
/// FR-MCP-085, TEST-MCP-FED-005.
/// </summary>
public sealed class FederationControllerPushTests
{
    private readonly IFederationPushService _pushService = Substitute.For<IFederationPushService>();

    private static FederationRegistry CreateRegistry(bool enabled = false, string? defaultTarget = null)
    {
        var opts = new FederationOptions { Enabled = enabled };
        if (defaultTarget is not null)
        {
            opts.DefaultTarget = defaultTarget;
            opts.Targets.Add(new FederationTargetOptions { Name = defaultTarget, BaseUrl = "http://remote:7147" });
        }
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private static TunnelRegistry CreateEmptyTunnelRegistry()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new TunnelOptions());
        return new TunnelRegistry([], opts, NullLogger<TunnelRegistry>.Instance);
    }

    private FederationController CreateController(FederationRegistry? registry = null)
    {
        registry ??= CreateRegistry();
        return new FederationController(registry, CreateEmptyTunnelRegistry(), _pushService);
    }

    /// <summary>Valid push request returns 200 with push result.</summary>
    [Fact]
    public async Task Push_ValidRequest_Returns200WithResult()
    {
        var expected = new FederationPushResult(3, 0, []);
        _pushService.PushAllAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var controller = CreateController(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await controller.Push(new FederationPushRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var pushResult = Assert.IsType<FederationPushResult>(ok.Value);
        Assert.Equal(3, pushResult.Succeeded);
        Assert.Equal(0, pushResult.Failed);
    }

    /// <summary>When federation is disabled, push returns 409 Conflict.</summary>
    [Fact]
    public async Task Push_FederationDisabled_Returns409()
    {
        var controller = CreateController(CreateRegistry(enabled: false));
        var result = await controller.Push(new FederationPushRequest(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    /// <summary>When no federation target exists, push returns 404.</summary>
    [Fact]
    public async Task Push_NoTarget_Returns404()
    {
        var controller = CreateController(CreateRegistry(enabled: true)); // no targets
        var result = await controller.Push(new FederationPushRequest(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>Push with specific types filters correctly.</summary>
    [Fact]
    public async Task Push_SpecificTypes_FiltersCorrectly()
    {
        var expected = new FederationPushResult(1, 0, []);
        _pushService.PushTodosAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var controller = CreateController(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await controller.Push(new FederationPushRequest { Types = ["todos"] }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var pushResult = Assert.IsType<FederationPushResult>(ok.Value);
        Assert.Equal(1, pushResult.Succeeded);
        await _pushService.DidNotReceiveWithAnyArgs().PushSessionLogsAsync(default);
    }
}
