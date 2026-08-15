using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): unit tests for <see cref="StorageConnectivityHealthCheck"/>,
/// the cheap DB connectivity probe registered with tags <c>ready</c> + <c>storage</c> (and
/// intentionally NOT <c>live</c>, so /health keeps liveness semantics).
/// Fixture: a scoped <see cref="McpDbContext"/> backed by SQLite file paths - a file in a
/// nonexistent directory models the unreachable store; an existing file models the reachable one.
/// </summary>
public sealed class StorageConnectivityHealthCheckTests
{
    private static StorageConnectivityHealthCheck Build(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<McpDbContext>(o => o.UseSqlite(connectionString));
        var provider = services.BuildServiceProvider();
        return new StorageConnectivityHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<StorageConnectivityHealthCheck>.Instance);
    }

    /// <summary>AC: an unreachable store (missing database file) reports Unhealthy without throwing.</summary>
    [Fact]
    public async Task CheckHealth_UnreachableStore_ReportsUnhealthy()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "mcp.db");
        var check = Build($"Data Source={missingPath}");

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    /// <summary>AC: a reachable store (existing database file) reports Healthy.</summary>
    [Fact]
    public async Task CheckHealth_ReachableStore_ReportsHealthy()
    {
        var path = Path.Combine(Path.GetTempPath(), $"storage-health-{Guid.NewGuid():N}.db");
        await using (File.Create(path))
        {
        }

        try
        {
            var check = Build($"Data Source={path}");

            var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>AC: a mis-registered scope (no McpDbContext) degrades to Unhealthy instead of throwing.</summary>
    [Fact]
    public async Task CheckHealth_ContextResolutionFails_ReportsUnhealthy()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var check = new StorageConnectivityHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<StorageConnectivityHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
