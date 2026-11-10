using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-USECASE-018 AC-006: the per-test state and helpers that both halves of FederationTopologyServiceTests need (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from FederationTopologyServiceTests (_workspacePath, Dispose and CreateProvider) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class FederationTopologyServiceTestsBase : IDisposable
{
    internal readonly string _workspacePath = Path.Combine(
        Path.GetTempPath(), "QBrainAi.Tests", nameof(FederationTopologyServiceTests),
        Guid.NewGuid().ToString("N"));

    /// <summary>Removes the isolated workspace after each test.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    internal static ServiceProvider CreateProvider(params IFederationStateAdapter[] adapters)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<FederationOptions>(options => options.Sync.HeartbeatSeconds = 5);
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"fed-topology-{Guid.NewGuid():N}";
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        foreach (var adapter in adapters)
            services.AddSingleton(adapter);

        services.AddSingleton<IFederationTopologyService, FederationTopologyService>();
        return services.BuildServiceProvider();
    }
}
