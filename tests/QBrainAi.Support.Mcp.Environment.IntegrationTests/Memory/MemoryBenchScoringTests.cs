using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / FR-MCP-MEMORY-018-10 (historical): Deterministic or locked auto-rubric scoring.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-018; TEST-MCP-MEMORY-018; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchScoringTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryBenchCatalog is linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryBenchScoringTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-10: exact/contains are deterministic; rubric uses a locked checklist.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void DeterministicOrDocumentedRubric()
    {
        var pack = MemoryBenchCatalog.LoadPack();
        var pref = pack.Prompts.Single(prompt => prompt.Id == "PREF-001");
        var first = MemoryBenchScoring.Score(pref, "You prefer neovim", "with_memory");
        var second = MemoryBenchScoring.Score(pref, "You prefer neovim", "with_memory");
        Assert.Equal(first, second);
        Assert.True(first.Pass);

        var proc = pack.Prompts.Single(prompt => prompt.Id == "PROC-001");
        Assert.Equal("rubric", proc.Scoring);
        var rubric = MemoryBenchScoring.Score(proc, "Use memory_remember with Workspace scope.", "with_memory");
        Assert.True(rubric.Pass);
        Assert.Contains(MemoryBenchScoring.LockedAutoRubricId, rubric.Notes, StringComparison.Ordinal);
    }
}
