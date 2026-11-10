using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 (historical) / TR-MCP-MEMORY-MODEL-002 (historical): S1 Red acceptance for model/migrations.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-010; TR-MCP-MEMORY-MODEL-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Memory/MemoryMigrationTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases and the helpers only they use (FindRepoRoot) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class MemoryMigrationTestsBase in Memory/MemoryMigrationTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class MemoryMigrationTests : MemoryMigrationTestsBase, IDisposable
{
    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-01: Migrations apply cleanly on SQLite, PostgreSQL, and SQL Server test hosts.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Applies_OnAllProviders()
    {
        var root = FindRepoRoot();
        var sqlite = Directory.GetFiles(Path.Combine(root, "src", "QBrainAi.Storage.SqliteMigrations"), "*MemoryVersion*", SearchOption.AllDirectories);
        var postgres = Directory.GetFiles(Path.Combine(root, "src", "QBrainAi.Storage.PostgreSqlMigrations"), "*MemoryVersion*", SearchOption.AllDirectories);
        var sqlserver = Directory.GetFiles(Path.Combine(root, "src", "QBrainAi.Storage.SqlServerMigrations"), "*MemoryVersion*", SearchOption.AllDirectories);

        Assert.NotEmpty(sqlite);
        Assert.NotEmpty(postgres);
        Assert.NotEmpty(sqlserver);
        Assert.NotNull(_harness.FindMappedEntity(typeof(MemoryVersionEntity)));
        Assert.NotNull(_harness.FindMappedEntity(typeof(MemoryEdgeEntity)));
    }

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-06: Down migration or forward-only policy is documented; test host can recreate schema.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SchemaRecreate_Works()
    {
        using var extra = new MemoryS1Harness();
        using var ctx = extra.CreateContext(extra.WorkspaceA);
        Assert.True(ctx.Database.CanConnect());
        var docs = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "plans", "mcp-memory-002.md"));
        Assert.Contains("forward-only", docs, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(extra.FindMappedEntity(typeof(MemoryVersionEntity)));
    }

    private static string FindRepoRoot() => TestRepositoryRoot.Resolve();
}
