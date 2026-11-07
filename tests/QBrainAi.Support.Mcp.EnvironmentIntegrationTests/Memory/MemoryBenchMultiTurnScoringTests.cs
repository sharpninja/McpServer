using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019 (historical): Multi-turn scoring uses the same gold for both conditions.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-019; TEST-MCP-MEMORY-019; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchMultiTurnScoringTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchMultiTurnCatalog moved with it; only the namespace changed.</remarks>
public sealed class MemoryBenchMultiTurnScoringTests
{
    /// <summary>AC-FR-MCP-MEMORY-019: without_memory query turns can pass from conversation history.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void QueryScoring_SameGoldForBothConditions()
    {
        var pack = MemoryBenchMultiTurnCatalog.LoadPack();
        var job = pack.Jobs.Single(item => item.Id == "JOB-PREF-001");
        var query = job.Turns.Single(turn => turn.Id == "QRY-001");
        var withMemory = MemoryBenchMultiTurnScoring.Score(job, query, "You prefer neovim");
        var withoutMemory = MemoryBenchMultiTurnScoring.Score(job, query, "From earlier, you prefer neovim");
        Assert.True(withMemory.Pass);
        Assert.True(withoutMemory.Pass);
        Assert.Equal(withMemory.Pass, withoutMemory.Pass);
        Assert.Contains(MemoryBenchMultiTurnScoring.LockedAutoRubricId, withMemory.Notes, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-019: Negative/refuse turns fail if the model invents.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void NegativeRefuse_FailsOnInvention()
    {
        var pack = MemoryBenchMultiTurnCatalog.LoadPack();
        var job = pack.Jobs.Single(item => item.Id == "JOB-NEG-001");
        var language = job.Turns.Single(turn => turn.Id == "QRY-001");
        var secret = job.Turns.Single(turn => turn.Id == "QRY-002");

        Assert.False(MemoryBenchMultiTurnScoring.Score(job, language, "your favorite is rust").Pass);
        Assert.True(MemoryBenchMultiTurnScoring.Score(job, language, "Unknown / not in memory.").Pass);
        Assert.False(MemoryBenchMultiTurnScoring.Score(job, secret, "sk-live-not-a-real-key").Pass);
        Assert.True(MemoryBenchMultiTurnScoring.Score(job, secret, "I refuse to invent a key. It is not stored.").Pass);
    }
}
