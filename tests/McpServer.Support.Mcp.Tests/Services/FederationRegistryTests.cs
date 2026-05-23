using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FederationRegistry"/>. Validates target routing logic,
/// runtime mutations, and enable/disable behaviour. FR-MCP-077.
/// </summary>
public sealed class FederationRegistryTests
{
    private static FederationRegistry CreateRegistry(Action<FederationOptions>? configure = null)
    {
        var opts = new FederationOptions();
        configure?.Invoke(opts);
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    // --- IsEnabled ---

    /// <summary>Registry is disabled by default when no configuration is provided.</summary>
    [Fact]
    public void IsEnabled_Default_IsFalse()
    {
        var sut = CreateRegistry();
        Assert.False(sut.IsEnabled);
    }

    /// <summary>Registry reflects configuration when Enabled is true.</summary>
    [Fact]
    public void IsEnabled_WhenConfiguredTrue_IsTrue()
    {
        var sut = CreateRegistry(o => o.Enabled = true);
        Assert.True(sut.IsEnabled);
    }

    /// <summary>SetEnabled toggles the runtime state correctly.</summary>
    [Fact]
    public void SetEnabled_TogglesState()
    {
        var sut = CreateRegistry();
        sut.SetEnabled(true);
        Assert.True(sut.IsEnabled);
        sut.SetEnabled(false);
        Assert.False(sut.IsEnabled);
    }

    // --- ResolveTarget ---

    /// <summary>Returns null when federation is disabled regardless of targets.</summary>
    [Fact]
    public void ResolveTarget_WhenDisabled_ReturnsNull()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = false;
            o.DefaultTarget = "t1";
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });

        Assert.Null(sut.ResolveTarget(null));
    }

    /// <summary>Returns null when enabled but no targets and no workspace route exist.</summary>
    [Fact]
    public void ResolveTarget_WhenEnabledNoTargets_ReturnsNull()
    {
        var sut = CreateRegistry(o => o.Enabled = true);
        Assert.Null(sut.ResolveTarget(@"C:\ws\alpha"));
    }

    /// <summary>Returns the default target when enabled and no workspace route matches.</summary>
    [Fact]
    public void ResolveTarget_FallsBackToDefault()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "remote";
            o.Targets.Add(new FederationTargetOptions { Name = "remote", BaseUrl = "https://x.ngrok.io" });
        });

        var target = sut.ResolveTarget(@"C:\ws\alpha");
        Assert.NotNull(target);
        Assert.Equal("remote", target.Name);
    }

    /// <summary>Workspace-specific route takes priority over the global default.</summary>
    [Fact]
    public void ResolveTarget_WorkspaceRoute_TakesPriorityOverDefault()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "default-target";
            o.Targets.Add(new FederationTargetOptions { Name = "default-target", BaseUrl = "http://localhost:7148" });
            o.Targets.Add(new FederationTargetOptions { Name = "special-target", BaseUrl = "http://localhost:7149" });
            o.WorkspaceRoutes.Add(new WorkspaceRouteOptions
            {
                WorkspacePath = @"C:\ws\alpha",
                TargetName = "special-target",
            });
        });

        var target = sut.ResolveTarget(@"C:\ws\alpha");
        Assert.NotNull(target);
        Assert.Equal("special-target", target.Name);
    }

    /// <summary>Workspace path not in routes falls back to default target.</summary>
    [Fact]
    public void ResolveTarget_UnknownWorkspace_FallsBackToDefault()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "default-target";
            o.Targets.Add(new FederationTargetOptions { Name = "default-target", BaseUrl = "http://localhost:7148" });
            o.WorkspaceRoutes.Add(new WorkspaceRouteOptions
            {
                WorkspacePath = @"C:\ws\alpha",
                TargetName = "default-target",
            });
        });

        var target = sut.ResolveTarget(@"C:\ws\OTHER");
        Assert.NotNull(target);
        Assert.Equal("default-target", target.Name);
    }

    /// <summary>Null workspace path still resolves the default target.</summary>
    [Fact]
    public void ResolveTarget_NullWorkspace_ReturnsDefault()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "t1";
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });

        Assert.NotNull(sut.ResolveTarget(null));
    }

    /// <summary>LocalProxy resolves the configured hub target with the stable hub access token.</summary>
    [Fact]
    public void ResolveTarget_LocalProxy_UsesHubAccessToken()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.Role = FederationRole.LocalProxy;
            o.HubBaseUrl = "http://hub.example:7147/";
            o.HubAccessToken = "hub-secret";
        });

        var target = sut.ResolveTarget(@"C:\ws\alpha");

        Assert.NotNull(target);
        Assert.Equal("hub", target.Name);
        Assert.Equal("http://hub.example:7147", target.BaseUrl);
        Assert.Equal("hub-secret", target.ApiKey);
        Assert.True(sut.HasHubAccessToken);
    }

    // --- TryAddTarget ---

    /// <summary>Adding a valid target succeeds and it appears in List().</summary>
    [Fact]
    public void TryAddTarget_ValidOptions_AddsTarget()
    {
        var sut = CreateRegistry();
        var opts = new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" };

        var result = sut.TryAddTarget(opts, out var error);

        Assert.True(result);
        Assert.Null(error);
        Assert.Single(sut.List());
    }

    /// <summary>Adding a target with a duplicate name fails.</summary>
    [Fact]
    public void TryAddTarget_DuplicateName_ReturnsFalse()
    {
        var sut = CreateRegistry();
        var opts = new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" };
        sut.TryAddTarget(opts, out _);

        var result = sut.TryAddTarget(opts, out var error);

        Assert.False(result);
        Assert.NotNull(error);
    }

    /// <summary>Adding a target with an empty name fails.</summary>
    [Fact]
    public void TryAddTarget_EmptyName_ReturnsFalse()
    {
        var sut = CreateRegistry();
        var opts = new FederationTargetOptions { Name = "", BaseUrl = "http://localhost:7148" };

        var result = sut.TryAddTarget(opts, out var error);

        Assert.False(result);
        Assert.NotNull(error);
    }

    // --- TryRemoveTarget ---

    /// <summary>Removing an existing target returns true and clears it from List().</summary>
    [Fact]
    public void TryRemoveTarget_ExistingTarget_RemovesIt()
    {
        var sut = CreateRegistry();
        sut.TryAddTarget(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" }, out _);

        var result = sut.TryRemoveTarget("t1");

        Assert.True(result);
        Assert.Empty(sut.List());
    }

    /// <summary>Removing the default target also clears the default pointer.</summary>
    [Fact]
    public void TryRemoveTarget_DefaultTarget_ClearsDefault()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "t1";
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });

        sut.TryRemoveTarget("t1");

        Assert.Null(sut.ResolveTarget(null));
    }

    /// <summary>Removing a non-existent target returns false.</summary>
    [Fact]
    public void TryRemoveTarget_NonExistent_ReturnsFalse()
    {
        var sut = CreateRegistry();
        Assert.False(sut.TryRemoveTarget("ghost"));
    }

    // --- SetDefaultTarget ---

    /// <summary>SetDefaultTarget with a valid name succeeds.</summary>
    [Fact]
    public void SetDefaultTarget_ValidName_ReturnsTrue()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });

        Assert.True(sut.SetDefaultTarget("t1"));
        var target = sut.ResolveTarget(null);
        Assert.Equal("t1", target?.Name);
    }

    /// <summary>SetDefaultTarget with null clears the default.</summary>
    [Fact]
    public void SetDefaultTarget_Null_ClearsDefault()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "t1";
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });

        sut.SetDefaultTarget(null);
        Assert.Null(sut.ResolveTarget(null));
    }

    // --- SetWorkspaceRoute / RemoveWorkspaceRoute ---

    /// <summary>Adding a workspace route routes correctly after SetWorkspaceRoute.</summary>
    [Fact]
    public void SetWorkspaceRoute_ValidTarget_RoutesCorrectly()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
        });

        var result = sut.SetWorkspaceRoute(@"C:\ws\alpha", "t1");

        Assert.True(result);
        Assert.Equal("t1", sut.ResolveTarget(@"C:\ws\alpha")?.Name);
    }

    /// <summary>SetWorkspaceRoute returns false when the target does not exist.</summary>
    [Fact]
    public void SetWorkspaceRoute_UnknownTarget_ReturnsFalse()
    {
        var sut = CreateRegistry(o => o.Enabled = true);
        Assert.False(sut.SetWorkspaceRoute(@"C:\ws\alpha", "ghost"));
    }

    /// <summary>RemoveWorkspaceRoute removes an existing rule and returns true.</summary>
    [Fact]
    public void RemoveWorkspaceRoute_Existing_RemovesAndReturnsTrue()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
            o.WorkspaceRoutes.Add(new WorkspaceRouteOptions { WorkspacePath = @"C:\ws\alpha", TargetName = "t1" });
        });

        var result = sut.RemoveWorkspaceRoute(@"C:\ws\alpha");

        Assert.True(result);
        Assert.Null(sut.ResolveTarget(@"C:\ws\alpha"));
    }

    // --- List / ListRoutes ---

    /// <summary>List reflects all configured targets with correct IsDefault flag.</summary>
    [Fact]
    public void List_ReturnsAllTargetsWithCorrectIsDefault()
    {
        var sut = CreateRegistry(o =>
        {
            o.Enabled = true;
            o.DefaultTarget = "t1";
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
            o.Targets.Add(new FederationTargetOptions { Name = "t2", BaseUrl = "http://localhost:7149" });
        });

        var list = sut.List();

        Assert.Equal(2, list.Count);
        Assert.True(list.Single(t => t.Name == "t1").IsDefault);
        Assert.False(list.Single(t => t.Name == "t2").IsDefault);
    }

    /// <summary>ListRoutes reflects all configured workspace routing rules.</summary>
    [Fact]
    public void ListRoutes_ReturnsAllRoutes()
    {
        var sut = CreateRegistry(o =>
        {
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148" });
            o.WorkspaceRoutes.Add(new WorkspaceRouteOptions { WorkspacePath = @"C:\ws\alpha", TargetName = "t1" });
            o.WorkspaceRoutes.Add(new WorkspaceRouteOptions { WorkspacePath = @"C:\ws\beta", TargetName = "t1" });
        });

        var routes = sut.ListRoutes();
        Assert.Equal(2, routes.Count);
    }

    /// <summary>HasApiKey is true when target has an API key configured.</summary>
    [Fact]
    public void List_TargetWithApiKey_HasApiKeyIsTrue()
    {
        var sut = CreateRegistry(o =>
            o.Targets.Add(new FederationTargetOptions { Name = "t1", BaseUrl = "http://localhost:7148", ApiKey = "secret" }));

        Assert.True(sut.List()[0].HasApiKey);
    }
}
