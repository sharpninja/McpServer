using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018: Harness conditions, metrics, isolation, and modes.
/// </summary>
public sealed class MemoryBenchHarnessTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-05: without_memory has no injection and no successful recall tools.</summary>
    [Fact]
    public void WithoutMemory_NoEffectiveInjectionOrTools()
    {
        var cells = MemoryBenchCatalog.StubGrok().Cells.Where(cell => cell.Condition == "without_memory").ToList();
        Assert.NotEmpty(cells);
        Assert.All(cells, cell =>
        {
            Assert.False(cell.InjectionPresent);
            Assert.DoesNotContain("REQUIRED MEMORIES\n- BENCH", cell.Transcript, StringComparison.Ordinal);
            Assert.Empty(cell.ToolCalls);
        });
    }

    /// <summary>AC-FR-MCP-MEMORY-018-06: with_memory seeds, injects, and exposes recall tools.</summary>
    [Fact]
    public void WithMemory_SeededAndToolsAvailable()
    {
        var pref = MemoryBenchCatalog.StubGrok().Cells
            .Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "with_memory");
        Assert.True(pref.InjectionPresent);
        Assert.Contains("REQUIRED MEMORIES", pref.Transcript, StringComparison.Ordinal);
        Assert.Contains("BENCH-PREF-EDITOR=neovim", pref.Transcript, StringComparison.Ordinal);
        Assert.Contains("memory_recall", pref.ToolCalls);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-07: Default path is Grok first; others stay opt-in until H7a.</summary>
    [Fact]
    public void GrokFirst_OthersOptInUntilValueGate()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.Equal(["grok"], result.Plugins);
        Assert.True(MemoryBenchValueGate.IsH7aAgreed(MemoryBenchCatalog.FindRepoRoot()));
        var harness = new MemoryBenchHarness();
        var claude = harness.Run(MemoryBenchCatalog.FindRepoRoot(), new MemoryBenchRunOptions { Plugins = ["claude-code"] });
        Assert.Equal(["claude-code"], claude.Plugins);
        Assert.Equal(16, claude.Cells.Count);
        var all = harness.Run(MemoryBenchCatalog.FindRepoRoot(), new MemoryBenchRunOptions { Plugins = ["all"] });
        Assert.Equal(MemoryBenchAdapterRegistry.AllPluginIds, all.Plugins);
        Assert.Equal(128, all.Cells.Count);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-08: Each cell records transcript, tools, injection, tokens, latency, score, pass.</summary>
    [Fact]
    public void RecordsPerCellMetrics_IncludingTokens()
    {
        foreach (var cell in MemoryBenchCatalog.StubGrok().Cells)
        {
            Assert.False(string.IsNullOrWhiteSpace(cell.Transcript));
            Assert.NotNull(cell.ToolCalls);
            Assert.True(cell.TokensIn is >= 0);
            Assert.True(cell.TokensOut is >= 0);
            Assert.Equal(cell.TokensIn + cell.TokensOut, cell.TokensTotal);
            Assert.True(cell.TokenSource is not null && MemoryBenchTokenSources.All.Contains(cell.TokenSource));
            Assert.True(cell.LatencyMs >= 0);
            Assert.InRange(cell.Score, 0, 1);
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-018-09: Result artifacts include per-cell tokens and a token summary table.</summary>
    [Fact]
    public void WritesResultArtifacts_WithTokenSummary()
    {
        var directory = Path.Combine(Path.GetTempPath(), "memory-bench-" + Guid.NewGuid().ToString("N"));
        var result = MemoryBenchCatalog.RunGrokStub(directory);
        var json = Directory.GetFiles(directory, "memory-bench-*.json").Single();
        var md = Directory.GetFiles(directory, "memory-bench-*.md").Single();
        Assert.Contains("memory-bench-", Path.GetFileName(json), StringComparison.Ordinal);
        var jsonText = File.ReadAllText(json);
        Assert.Contains("tokens_in", jsonText, StringComparison.Ordinal);
        Assert.Contains("tokens_out", jsonText, StringComparison.Ordinal);
        Assert.Contains("tokens_total", jsonText, StringComparison.Ordinal);
        Assert.Contains("token_source", jsonText, StringComparison.Ordinal);
        var markdown = File.ReadAllText(md);
        Assert.Contains("tokens_total_mean", markdown, StringComparison.Ordinal);
        Assert.Contains(result.SummaryMarkdown, markdown, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-25: Stub/recorded mode does not require cloud credentials.</summary>
    [Fact]
    public void StubMode_NoCloudRequired()
    {
        var previous = Environment.GetEnvironmentVariable("XAI_API_KEY");
        Environment.SetEnvironmentVariable("XAI_API_KEY", null);
        try
        {
            var result = MemoryBenchCatalog.RunGrokStub();
            Assert.Equal(MemoryBenchModes.Stub, result.Mode);
            Assert.Equal(16, result.Cells.Count);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XAI_API_KEY", previous);
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-018-26: Live/stub/recorded modes are tagged on the result.</summary>
    [Fact]
    public void LiveMode_Tagged()
    {
        Assert.Equal(MemoryBenchModes.Stub, MemoryBenchCatalog.StubGrok().Mode);
        var recorded = new MemoryBenchHarness().Run(MemoryBenchCatalog.FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = ["grok"],
            Mode = MemoryBenchModes.Recorded,
        });
        Assert.Equal(MemoryBenchModes.Recorded, recorded.Mode);
        Assert.All(recorded.Cells, cell => Assert.Equal(MemoryBenchTokenSources.Recorded, cell.TokenSource));

        var previous = Environment.GetEnvironmentVariable("XAI_API_KEY");
        Environment.SetEnvironmentVariable("XAI_API_KEY", null);
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                new MemoryBenchGrokAdapter().Execute(new MemoryBenchTurnContext
                {
                    Plugin = "grok",
                    Prompt = MemoryBenchCatalog.LoadPack().Prompts[0],
                    Condition = "without_memory",
                    Mode = MemoryBenchModes.Live,
                }));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XAI_API_KEY", previous);
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-018-27: Seeds are Workspace-scoped in a disposable bench workspace and cleaned up.</summary>
    [Fact]
    public void Seeds_WorkspaceScopedCleanup()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.StartsWith("bench-", result.BenchWorkspaceId, StringComparison.Ordinal);
        Assert.NotEqual(result.PrimaryWorkspaceId, result.BenchWorkspaceId);
        var pack = MemoryBenchCatalog.LoadPack();
        Assert.All(pack.Prompts.SelectMany(prompt => prompt.SeededMemories), seed =>
            Assert.False(string.IsNullOrWhiteSpace(seed.Content)));
        Assert.Contains("Workspace scope", File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md"))
            + File.ReadAllText(MemoryBenchCatalog.PackPath()), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-31: Stub re-run on the same pack is byte-stable for scores and tokens.</summary>
    [Fact]
    public void Rerun_DeterministicStub()
    {
        var first = MemoryBenchCatalog.RunGrokStub();
        var second = MemoryBenchCatalog.RunGrokStub();
        Assert.Equal(
            first.Cells.Select(cell => (cell.PromptId, cell.Condition, cell.Pass, cell.Score, cell.TokensTotal)),
            second.Cells.Select(cell => (cell.PromptId, cell.Condition, cell.Pass, cell.Score, cell.TokensTotal)));
    }
}
