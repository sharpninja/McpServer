using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-REQSCOPE-001 / TEST-MCP-WORKSPACE-LAYER-001 / TEST-MCP-REQSCOPE-002 /
/// TEST-MCP-REQSCOPE-003: red tests for requirement scope layers, workspace current
/// layer state, scoped requirement windows, layer sunset windows, and effective results.
/// </summary>
public sealed class RequirementScopeLayerServiceTests
{
    /// <summary>
    /// TEST-MCP-REQSCOPE-001: the seeded layer catalog is workspace-isolated and accepts
    /// durable layer creation without leaking across workspaces.
    /// </summary>
    [Fact]
    public async Task LayerCatalog_SeedsLayerOneAndIsWorkspaceIsolated()
    {
        using var fixture = new RequirementsScopeFixture();
        var workspaceA = fixture.CreateWorkspace("a");
        var workspaceB = fixture.CreateWorkspace("b");
        var service = fixture.CreateService();

        fixture.SetWorkspace(workspaceA);
        var initial = await service.GetRequirementLayersAsync(ct: TestContext.Current.CancellationToken);

        var layer1 = Assert.Single(initial);
        Assert.Equal("layer-1", layer1.Key);
        Assert.Equal(1, layer1.Order);
        Assert.Null(layer1.ScopeEndLayerKey);

        await service.CreateRequirementLayerAsync(new RequirementScopeLayerEntry(
            Key: "layer-2",
            Order: 2,
            Name: "Layer 2",
            Description: "Second implementation layer"), ct: TestContext.Current.CancellationToken);

        var workspaceALayers = await service.GetRequirementLayersAsync(ct: TestContext.Current.CancellationToken);
        Assert.Contains(workspaceALayers, x => x.Key == "layer-2");

        fixture.SetWorkspace(workspaceB);
        var workspaceBLayers = await service.GetRequirementLayersAsync(ct: TestContext.Current.CancellationToken);

        Assert.Single(workspaceBLayers);
        Assert.DoesNotContain(workspaceBLayers, x => x.Key == "layer-2");
    }

    /// <summary>
    /// TEST-MCP-REQSCOPE-001: duplicate keys/orders, invalid layer sunsets, and
    /// immutable key/order changes are rejected.
    /// </summary>
    [Fact]
    public async Task LayerCatalog_RejectsDuplicatesAndInvalidLayerSunset()
    {
        using var fixture = new RequirementsScopeFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("validation"));
        var service = fixture.CreateService();

        await service.CreateRequirementLayerAsync(new RequirementScopeLayerEntry("layer-2", 2, "Layer 2"), ct: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            service.CreateRequirementLayerAsync(new RequirementScopeLayerEntry("layer-2", 3, "Duplicate key"), ct: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            service.CreateRequirementLayerAsync(new RequirementScopeLayerEntry("layer-3", 2, "Duplicate order"), ct: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRequirementLayerAsync(new RequirementScopeLayerUpdateRequest("layer-2")
            {
                ScopeEndLayerKey = "layer-1"
            }, ct: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<RequirementsNotFoundException>(() =>
            service.UpdateRequirementLayerAsync(new RequirementScopeLayerUpdateRequest("changed")
            {
                Order = 99
            }, ct: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRequirementLayerAsync(new RequirementScopeLayerUpdateRequest("layer-2")
            {
                Order = 99
            }, ct: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// TEST-MCP-WORKSPACE-LAYER-001 / TEST-MCP-REQSCOPE-003: workspace current layer
    /// defaults to layer-1, validates same-workspace layer references, and drives
    /// effective requirement visibility.
    /// </summary>
    [Fact]
    public async Task EffectiveRequirements_UseWorkspaceCurrentLayerRequirementEndsAndLayerSunsets()
    {
        using var fixture = new RequirementsScopeFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("effective"));
        var service = fixture.CreateService();

        await service.CreateRequirementLayerAsync(new RequirementScopeLayerEntry("layer-2", 2, "Layer 2"), ct: TestContext.Current.CancellationToken);
        await service.CreateRequirementLayerAsync(new RequirementScopeLayerEntry("layer-3", 3, "Layer 3"), ct: TestContext.Current.CancellationToken);

        await service.AddFrAsync(new FrEntry(
            "FR-MCP-901",
            "Future FR",
            "Applies only from layer 2.",
            Priority: "high",
            ScopeStartLayerKey: "layer-2"), ct: TestContext.Current.CancellationToken);
        await service.AddFrAsync(new FrEntry(
            "FR-MCP-902",
            "Expired FR",
            "Applies only at layer 1.",
            Priority: "high",
            ScopeEndLayerKey: "layer-1"), ct: TestContext.Current.CancellationToken);
        await service.AddTrAsync(new TrEntry(
            "TR-MCP-REQSCOPE-901",
            "Future TR",
            "TR applies from layer 2.",
            Priority: "high",
            ScopeStartLayerKey: "layer-2"), ct: TestContext.Current.CancellationToken);
        await service.AddTestAsync(new TestEntry(
            "TEST-MCP-901",
            "TEST applies from layer 2.",
            Priority: "high",
            ScopeStartLayerKey: "layer-2"), ct: TestContext.Current.CancellationToken);
        await service.UpsertMappingAsync(new FrTrMapping(
            "FR-MCP-901",
            ["TR-MCP-REQSCOPE-901"],
            ["TEST-MCP-901"]), ct: TestContext.Current.CancellationToken);

        var before = await service.GetEffectiveRequirementsAsync(ct: TestContext.Current.CancellationToken);
        Assert.Equal("layer-1", before.CurrentLayer.Key);
        Assert.DoesNotContain(before.Functional, x => x.Id == "FR-MCP-901");
        Assert.Contains(before.Functional, x => x.Id == "FR-MCP-902");

        await service.SetWorkspaceCurrentRequirementLayerAsync("layer-2", ct: TestContext.Current.CancellationToken);
        var layer2 = await service.GetEffectiveRequirementsAsync(ct: TestContext.Current.CancellationToken);
        Assert.Equal("layer-2", layer2.CurrentLayer.Key);
        Assert.Contains(layer2.Functional, x => x.Id == "FR-MCP-901");
        Assert.DoesNotContain(layer2.Functional, x => x.Id == "FR-MCP-902");
        Assert.Single(layer2.Mappings);

        await service.UpdateRequirementLayerAsync(new RequirementScopeLayerUpdateRequest("layer-2")
        {
            ScopeEndLayerKey = "layer-2"
        }, ct: TestContext.Current.CancellationToken);
        await service.SetWorkspaceCurrentRequirementLayerAsync("layer-3", ct: TestContext.Current.CancellationToken);

        var layer3 = await service.GetEffectiveRequirementsAsync(ct: TestContext.Current.CancellationToken);
        Assert.DoesNotContain(layer3.Functional, x => x.Id == "FR-MCP-901");
        Assert.Empty(layer3.Mappings);
    }

    /// <summary>
    /// TEST-MCP-REQSCOPE-002: requirement mutations reject missing layer references
    /// and end-before-start applicability windows.
    /// </summary>
    [Fact]
    public async Task RequirementScopeMutation_RejectsMissingLayerAndEndBeforeStart()
    {
        using var fixture = new RequirementsScopeFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("mutations"));
        var service = fixture.CreateService();
        await service.CreateRequirementLayerAsync(new RequirementScopeLayerEntry("layer-2", 2, "Layer 2"), ct: TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<RequirementsNotFoundException>(() =>
            service.AddFrAsync(new FrEntry(
                "FR-MCP-903",
                "Missing start",
                "Invalid start layer.",
                ScopeStartLayerKey: "missing-layer"), ct: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddFrAsync(new FrEntry(
                "FR-MCP-904",
                "End before start",
                "Invalid layer window.",
                ScopeStartLayerKey: "layer-2",
                ScopeEndLayerKey: "layer-1"), ct: TestContext.Current.CancellationToken));
    }

    private sealed class RequirementsScopeFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _requestScope;
        private readonly DefaultHttpContext _httpContext;
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly string _root = Path.Combine(Path.GetTempPath(), "mcp-reqscope-tests-" + Guid.NewGuid().ToString("N"));

        public RequirementsScopeFixture()
        {
            var services = new ServiceCollection();
            _connection.Open();
            services.AddDbContext<McpDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<WorkspaceContext>();
            services.AddHttpContextAccessor();
            services.AddSingleton<IOptions<RequirementsOptions>>(Microsoft.Extensions.Options.Options.Create(new RequirementsOptions()));
            services.AddSingleton(NullLogger<RequirementsDatabaseDocumentService>.Instance);
            _provider = services.BuildServiceProvider();
            using (var schemaScope = _provider.CreateScope())
            {
                schemaScope.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();
            }

            _requestScope = _provider.CreateScope();
            _httpContext = new DefaultHttpContext { RequestServices = _requestScope.ServiceProvider };
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
        }

        public string CreateWorkspace(string name)
        {
            var path = Path.Combine(_root, name);
            Directory.CreateDirectory(Path.Combine(path, "docs", "Project"));
            return path;
        }

        public RequirementsDatabaseDocumentService CreateService() =>
            new(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                _provider.GetRequiredService<IOptions<RequirementsOptions>>(),
                NullLogger<RequirementsDatabaseDocumentService>.Instance,
                _provider.GetRequiredService<IHttpContextAccessor>());

        public void SetWorkspace(string workspacePath)
        {
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
            var ctx = _httpContext.RequestServices.GetRequiredService<WorkspaceContext>();
            ctx.WorkspacePath = workspacePath;
            ctx.WorkspaceName = Path.GetFileName(workspacePath);
        }

        public void Dispose()
        {
            _requestScope.Dispose();
            _provider.Dispose();
            _connection.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }
}
