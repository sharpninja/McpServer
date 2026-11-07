using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// Configurable paths, unit half: the instance resolver gives two configured instances isolated ports and SQLite data sources, from in-memory configuration over temporary directories.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (RepoIngestor_ResolvesRelativeRepoRoot_AgainstCurrentDirectory and VectorIndexService_SaveAsync_UsesConfiguredRelativeIndexPath) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_tempDirectories, Dispose and CreateTempDirectory) moved once into the abstract base class ConfigurablePathIntegrationTestsBase in Configuration/ConfigurablePathIntegrationTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class ConfigurablePathIntegrationTests : ConfigurablePathIntegrationTestsBase, IDisposable
{
    [Fact]
    public void McpInstanceResolver_ResolvesIsolatedSettings_ForTwoInstances()
    {
        var tempRoot = CreateTempDirectory();
        var alphaRoot = Path.Combine(tempRoot, "alpha");
        var betaRoot = Path.Combine(tempRoot, "beta");
        var alphaData = Path.Combine(tempRoot, "alpha-data");
        var betaData = Path.Combine(tempRoot, "beta-data");
        Directory.CreateDirectory(alphaRoot);
        Directory.CreateDirectory(betaRoot);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Instances:alpha:Port"] = "7147",
                ["Mcp:Instances:alpha:RepoRoot"] = alphaRoot,
                ["Mcp:Instances:alpha:DataSource"] = "alpha.db",
                ["Mcp:Instances:alpha:DataDirectory"] = alphaData,
                ["Mcp:Instances:beta:Port"] = "7157",
                ["Mcp:Instances:beta:RepoRoot"] = betaRoot,
                ["Mcp:Instances:beta:DataSource"] = "beta.db",
                ["Mcp:Instances:beta:DataDirectory"] = betaData,
            })
            .Build();

        McpInstanceResolver.ValidateInstances(config);

        var alphaPort = McpInstanceResolver.GetEffectiveMcpInt(config, "alpha", "Port", 0);
        var betaPort = McpInstanceResolver.GetEffectiveMcpInt(config, "beta", "Port", 0);
        var alphaDb = McpInstanceResolver.ResolveSqliteDataSource(config, "alpha");
        var betaDb = McpInstanceResolver.ResolveSqliteDataSource(config, "beta");

        Assert.NotEqual(alphaPort, betaPort);
        Assert.NotEqual(alphaDb, betaDb);
        Assert.EndsWith(Path.Combine("alpha-data", "alpha.db"), alphaDb, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("beta-data", "beta.db"), betaDb, StringComparison.OrdinalIgnoreCase);
    }
}
