using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019 (historical): Canonical multi-turn pack existence and schema.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-019; TEST-MCP-MEMORY-019; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchMultiTurnPackTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchMultiTurnCatalog moved with it; only the namespace changed.</remarks>
public sealed class MemoryBenchMultiTurnPackTests
{
    /// <summary>AC-FR-MCP-MEMORY-019-01: v2 pack lives at the canonical path and is versioned.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void PackFile_ExistsAndVersioned()
    {
        var path = MemoryBenchMultiTurnCatalog.PackPath();
        Assert.True(File.Exists(path), "docs/benchmarks/memory-prompt-pack-v2-multiturn.yaml must exist.");
        var pack = MemoryBenchMultiTurnCatalog.LoadPack();
        Assert.Equal("memory-prompt-pack-v2-multiturn", pack.Id);
        Assert.Equal("2.0", pack.Version);
        Assert.Equal("multi_turn_jobs", pack.Kind);
        Assert.Equal("real_efficiency_bench", pack.PackRole);
        Assert.Equal("smoke/regression", pack.V1Role);
        Assert.Equal("grok", pack.PilotPlugin);
    }

    /// <summary>AC-FR-MCP-MEMORY-019-02: Pack covers preference, decision, fact, multi-fact, and negative/refuse.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void CoversRequiredJobClasses()
    {
        var pack = MemoryBenchMultiTurnCatalog.LoadPack();
        Assert.True(pack.Jobs.Count >= 4);
        foreach (var required in MemoryBenchMultiTurnCatalog.RequiredClasses)
            Assert.Contains(pack.Jobs, job => job.Class == required);
    }

    /// <summary>AC-FR-MCP-MEMORY-019-03: Each job has establish then required query turns.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void JobsHaveEstablishThenRequiredQuery()
    {
        foreach (var job in MemoryBenchMultiTurnCatalog.LoadPack().Jobs)
        {
            Assert.Contains(job.Turns, turn => turn.Role == "establish");
            Assert.Contains(job.Turns, turn => turn.Role == "query" && turn.Required);
            var firstQuery = job.Turns.FindIndex(turn => turn.Role == "query");
            Assert.True(job.Turns.Take(firstQuery).All(turn => turn.Role == "establish"));
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-019-04: Query turns do not restate gold facts in the user message.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void QueryTurns_DoNotRestateFacts()
    {
        foreach (var job in MemoryBenchMultiTurnCatalog.LoadPack().Jobs)
        {
            foreach (var turn in job.Turns.Where(item => item.Role == "query"))
            {
                foreach (var banned in turn.QueryMustNotContain)
                    Assert.DoesNotContain(banned, turn.UserText, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-019-05: Pack declares paired conditions and token fields.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void PairedConditionsAndTokenFields()
    {
        var pack = MemoryBenchMultiTurnCatalog.LoadPack();
        Assert.Contains("without_memory", pack.Conditions);
        Assert.Contains("with_memory", pack.Conditions);
        foreach (var field in new[] { "tokens_in", "tokens_out", "tokens_total", "token_source" })
            Assert.Contains(field, pack.TokenFields);
    }
}
