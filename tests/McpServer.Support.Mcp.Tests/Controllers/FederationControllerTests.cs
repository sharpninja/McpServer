using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="FederationController"/>. Validates management API behaviour:
/// enable/disable, target CRUD, workspace routing, and tunnel discovery. FR-MCP-077.
/// </summary>
public sealed class FederationControllerTests
{
    private static FederationRegistry CreateRegistry(Action<FederationOptions>? configure = null)
    {
        var opts = new FederationOptions();
        configure?.Invoke(opts);
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private static TunnelRegistry CreateEmptyTunnelRegistry()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new TunnelOptions());
        return new TunnelRegistry(
            [],
            opts,
            NullLogger<TunnelRegistry>.Instance);
    }

    private static FederationController CreateController(
        FederationRegistry? registry = null,
        TunnelRegistry? tunnels = null)
    {
        registry ??= CreateRegistry();
        tunnels ??= CreateEmptyTunnelRegistry();
        return new FederationController(registry, tunnels);
    }

    // --- GetStatus ---

    /// <summary>GetStatus returns disabled state when federation is not enabled.</summary>
    [Fact]
    public void GetStatus_Default_ReturnsDisabled()
    {
        var controller = CreateController();
        var result = controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.False(status.Enabled);
        Assert.Empty(status.Targets);
    }

    // --- Enable / Disable ---

    /// <summary>Enable sets federation to enabled.</summary>
    [Fact]
    public void Enable_SetsEnabledTrue()
    {
        var controller = CreateController();
        var result = controller.Enable();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.True(status.Enabled);
    }

    /// <summary>Disable sets federation to disabled.</summary>
    [Fact]
    public void Disable_SetsEnabledFalse()
    {
        var registry = CreateRegistry(o => o.Enabled = true);
        var controller = CreateController(registry);

        var result = controller.Disable();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.False(status.Enabled);
    }

    // --- ListTargets ---

    /// <summary>ListTargets returns empty list when no targets are configured.</summary>
    [Fact]
    public void ListTargets_NoTargets_ReturnsEmpty()
    {
        var result = CreateController().ListTargets();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var targets = Assert.IsAssignableFrom<IReadOnlyList<FederationTargetInfo>>(ok.Value);
        Assert.Empty(targets);
    }

    // --- AddTarget ---

    /// <summary>Adding a valid target returns 201 Created with the new target info.</summary>
    [Fact]
    public void AddTarget_ValidOptions_Returns201()
    {
        var controller = CreateController();
        var opts = new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" };

        var result = controller.AddTarget(opts);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var info = Assert.IsType<FederationTargetInfo>(created.Value);
        Assert.Equal("remote", info.Name);
    }

    /// <summary>Adding a duplicate target name returns 409 Conflict.</summary>
    [Fact]
    public void AddTarget_DuplicateName_Returns409()
    {
        var controller = CreateController();
        var opts = new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" };
        controller.AddTarget(opts);

        var result = controller.AddTarget(opts);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // --- RemoveTarget ---

    /// <summary>Removing an existing target returns 204 No Content.</summary>
    [Fact]
    public void RemoveTarget_Existing_Returns204()
    {
        var registry = CreateRegistry(o =>
            o.Targets.Add(new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" }));
        var controller = CreateController(registry);

        var result = controller.RemoveTarget("remote");

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>Removing a non-existent target returns 404.</summary>
    [Fact]
    public void RemoveTarget_NonExistent_Returns404()
    {
        var result = CreateController().RemoveTarget("ghost");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- SetDefault / ClearDefault ---

    /// <summary>SetDefault with a valid name returns updated status with new default.</summary>
    [Fact]
    public void SetDefault_ValidTarget_UpdatesStatus()
    {
        var registry = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });
        var controller = CreateController(registry);

        var result = controller.SetDefault("t1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.True(status.Targets.Single().IsDefault);
    }

    /// <summary>SetDefault with a non-existent target returns 404.</summary>
    [Fact]
    public void SetDefault_UnknownTarget_Returns404()
    {
        var result = CreateController().SetDefault("ghost");
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>ClearDefault removes the default target and returns updated status.</summary>
    [Fact]
    public void ClearDefault_ReturnsStatusWithNoDefault()
    {
        var registry = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "t1";
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });
        var controller = CreateController(registry);

        controller.ClearDefault();
        var result = controller.GetStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<FederationStatusResponse>(ok.Value);
        Assert.DoesNotContain(status.Targets, t => t.IsDefault);
    }

    // --- AddRoute / RemoveRoute ---

    /// <summary>Adding a valid route returns 200 with updated route list.</summary>
    [Fact]
    public void AddRoute_ValidTargetName_Returns200()
    {
        var registry = CreateRegistry(o =>
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" }));
        var controller = CreateController(registry);

        var result = controller.AddRoute(new WorkspaceRouteOptions
        {
            WorkspacePath = @"C:\ws\alpha",
            TargetName = "t1",
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var routes = Assert.IsAssignableFrom<IReadOnlyList<WorkspaceRouteInfo>>(ok.Value);
        Assert.Single(routes);
    }

    /// <summary>Adding a route with an unknown target returns 404.</summary>
    [Fact]
    public void AddRoute_UnknownTarget_Returns404()
    {
        var result = CreateController().AddRoute(new WorkspaceRouteOptions
        {
            WorkspacePath = @"C:\ws\alpha",
            TargetName = "ghost",
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    /// <summary>Removing an existing route returns 204.</summary>
    [Fact]
    public void RemoveRoute_Existing_Returns204()
    {
        var registry = CreateRegistry(o =>
        {
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
            o.WorkspaceRoutes.Add(new WorkspaceRouteOptions { WorkspacePath = @"C:\ws\alpha", TargetName = "t1" });
        });
        var controller = CreateController(registry);

        var result = controller.RemoveRoute(new WorkspaceRouteOptions { WorkspacePath = @"C:\ws\alpha" });

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>Removing a non-existent route returns 404.</summary>
    [Fact]
    public void RemoveRoute_NonExistent_Returns404()
    {
        var result = CreateController().RemoveRoute(new WorkspaceRouteOptions { WorkspacePath = @"C:\ghost" });
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- DiscoverFromTunnels ---

    /// <summary>DiscoverFromTunnels returns 0 discovered when no tunnels are running.</summary>
    [Fact]
    public async Task DiscoverFromTunnels_NoRunningTunnels_Returns0Discovered()
    {
        var controller = CreateController();
        var result = await controller.DiscoverFromTunnels(CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var discovery = Assert.IsType<TunnelDiscoveryResult>(ok.Value);
        Assert.Equal(0, discovery.Discovered);
    }
}
