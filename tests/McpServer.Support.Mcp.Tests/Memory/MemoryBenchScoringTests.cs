using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-10: Deterministic or locked auto-rubric scoring.
/// </summary>
public sealed class MemoryBenchScoringTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-10: exact/contains are deterministic; rubric uses a locked checklist.</summary>
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
