using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018: Canonical pack existence and schema.
/// </summary>
public sealed class MemoryBenchPackTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-01: Pack file lives at the canonical path and is versioned.</summary>
    [Fact]
    public void PackFile_ExistsAndVersioned()
    {
        var path = MemoryBenchCatalog.PackPath();
        Assert.True(File.Exists(path), "docs/benchmarks/memory-prompt-pack-v1.yaml must exist.");
        var pack = MemoryBenchCatalog.LoadPack();
        Assert.Equal("memory-prompt-pack-v1", pack.Id);
        Assert.False(string.IsNullOrWhiteSpace(pack.Version));
        Assert.Contains("version", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-02: Pack covers the eight required prompt classes.</summary>
    [Fact]
    public void CoversEightPromptClasses()
    {
        var pack = MemoryBenchCatalog.LoadPack();
        Assert.True(pack.Prompts.Count >= 8);
        foreach (var required in MemoryBenchCatalog.RequiredClasses)
            Assert.Contains(pack.Prompts, prompt => prompt.Class == required);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-03: Each prompt has the required entry fields.</summary>
    [Fact]
    public void EntrySchema_Complete()
    {
        foreach (var prompt in MemoryBenchCatalog.LoadPack().Prompts)
        {
            Assert.False(string.IsNullOrWhiteSpace(prompt.Id));
            Assert.False(string.IsNullOrWhiteSpace(prompt.Class));
            Assert.False(string.IsNullOrWhiteSpace(prompt.UserText));
            Assert.False(string.IsNullOrWhiteSpace(prompt.GoldAnswerRubric));
            Assert.NotNull(prompt.SeededMemories);
            Assert.NotNull(prompt.ForbiddenClaims);
            Assert.Contains(prompt.Scoring, new[] { "exact", "contains", "rubric" });
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-018-04: Pack declares paired without_memory / with_memory conditions.</summary>
    [Fact]
    public void PairedConditions()
    {
        var pack = MemoryBenchCatalog.LoadPack();
        Assert.Contains("without_memory", pack.Conditions);
        Assert.Contains("with_memory", pack.Conditions);
        Assert.All(pack.Prompts, prompt => Assert.False(string.IsNullOrWhiteSpace(prompt.UserText)));
    }

    /// <summary>AC-FR-MCP-MEMORY-018-30: Fixtures use synthetic BENCH-* tokens, not real secrets.</summary>
    [Fact]
    public void NoRealSecretsInFixtures()
    {
        var raw = File.ReadAllText(MemoryBenchCatalog.PackPath());
        Assert.Contains("BENCH-PREF-EDITOR=neovim", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-live-", raw, StringComparison.OrdinalIgnoreCase);
        MemoryBenchPackLoader.Validate(MemoryBenchCatalog.LoadPack(), raw);
    }
}
