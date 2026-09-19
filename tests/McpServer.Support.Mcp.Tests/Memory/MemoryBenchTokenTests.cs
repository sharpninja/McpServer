using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018: Token accounting is the primary bench metric.
/// </summary>
public sealed class MemoryBenchTokenTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-11: Mean/median tokens are reported per plugin × condition.</summary>
    [Fact]
    public void ReportsMeanMedianTokens_ByPluginAndCondition()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.Contains(result.Summary, row => row.Plugin == "grok" && row.Condition == "with_memory" && row.TokensTotalMean > 0);
        Assert.Contains(result.Summary, row => row.Plugin == "grok" && row.Condition == "without_memory" && row.TokensTotalMedian > 0);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-34: Token source is host-reported or a named estimator.</summary>
    [Fact]
    public void Source_HostReportedOrDocumentedEstimator()
    {
        foreach (var cell in MemoryBenchCatalog.StubGrok().Cells)
        {
            Assert.True(cell.TokenSource is not null && MemoryBenchTokenSources.All.Contains(cell.TokenSource));
            if (cell.TokenSource == MemoryBenchTokenSources.Estimator)
            {
                Assert.Equal(MemoryBenchTokenEstimator.EstimatorId, cell.EstimatorId);
                Assert.Equal(MemoryBenchTokenEstimator.EstimatorVersion, cell.EstimatorVersion);
            }
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-018-35: tokens_total equals tokens_in + tokens_out.</summary>
    [Fact]
    public void Defines_InOutTotal()
    {
        var (tokensIn, tokensOut, tokensTotal) = MemoryBenchTokenEstimator.EstimateTurn(
            "system prompt",
            "REQUIRED MEMORIES - neovim",
            "memory_recall results",
            "you prefer neovim");
        Assert.Equal(tokensIn + tokensOut, tokensTotal);
        Assert.True(tokensIn > tokensOut);
        Assert.All(MemoryBenchCatalog.StubGrok().Cells, cell =>
            Assert.Equal(cell.TokensIn + cell.TokensOut, cell.TokensTotal));
    }

    /// <summary>AC-FR-MCP-MEMORY-018-36: Injection tokens are charged only on with_memory cells.</summary>
    [Fact]
    public void Injection_CountedOnlyWithMemory()
    {
        var result = MemoryBenchCatalog.StubGrok();
        var withMemory = result.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "with_memory");
        var withoutMemory = result.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "without_memory");
        Assert.True(withMemory.InjectionTokens > 0);
        Assert.Equal(0, withoutMemory.InjectionTokens);
        Assert.True(withMemory.TokensIn > withoutMemory.TokensIn);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-37: Tool payloads are counted with the same rule on both conditions.</summary>
    [Fact]
    public void ToolPayloads_CountedConsistently()
    {
        var result = MemoryBenchCatalog.StubGrok();
        var withMemory = result.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "with_memory");
        var withoutMemory = result.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "without_memory");
        Assert.True(withMemory.ToolTokens > 0);
        Assert.Equal(0, withoutMemory.ToolTokens);
        Assert.Contains("memory_recall", withMemory.ToolCalls);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-39: Per-prompt token delta is with_memory minus without_memory.</summary>
    [Fact]
    public void PerPrompt_TokenDelta()
    {
        var result = MemoryBenchCatalog.StubGrok();
        var pref = result.Deltas.Single(delta => delta.PromptId == "PREF-001" && delta.Plugin == "grok");
        var withMemory = result.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "with_memory").TokensTotal ?? 0;
        var withoutMemory = result.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "without_memory").TokensTotal ?? 0;
        Assert.Equal(withMemory - withoutMemory, pref.TokensTotalDelta);
        Assert.True(pref.TokensTotalDelta > 0);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-41: Failed correctness still records tokens and drops efficiency use.</summary>
    [Fact]
    public void CorrectnessGate_TokensStillRecorded()
    {
        var pack = MemoryBenchCatalog.LoadPack();
        var pref = pack.Prompts.Single(prompt => prompt.Id == "PREF-001");
        var (score, pass, _) = MemoryBenchScoring.Score(pref, "I do not know", "with_memory");
        Assert.False(pass);
        Assert.Equal(0, score);

        var cell = new MemoryBenchCellResult
        {
            Plugin = "grok",
            PromptId = "PREF-001",
            Condition = "with_memory",
            TokensIn = 10,
            TokensOut = 4,
            TokensTotal = 14,
            TokenSource = MemoryBenchTokenSources.Estimator,
            Pass = false,
        };
        MemoryBenchResultSchema.ValidateCell(cell);
        Assert.True(cell.TokensTotal > 0);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-42: Efficiency ratio is labeled secondary and does not replace tokens.</summary>
    [Fact]
    public void EfficiencyRatio_SecondaryOnly()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.True(result.EfficiencyTokensPerPass is > 0);
        Assert.Contains("efficiency_tokens_per_pass", MemoryBenchCatalog.LoadPack().Metrics.Secondary);
        var readme = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md"));
        Assert.Contains("tokens used", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Primary metric", readme, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("efficiency ratio is the primary", readme, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-43: Stub/recorded cells still emit token fields.</summary>
    [Fact]
    public void StubMode_RequiresTokenFields()
    {
        Assert.All(MemoryBenchCatalog.StubGrok().Cells, cell =>
        {
            Assert.NotNull(cell.TokensIn);
            Assert.NotNull(cell.TokensOut);
            Assert.NotNull(cell.TokensTotal);
            Assert.False(string.IsNullOrWhiteSpace(cell.TokenSource));
        });
    }

    /// <summary>AC-FR-MCP-MEMORY-018-44: NEG/SAFE record tokens and mandatory pass/fail under both conditions.</summary>
    [Fact]
    public void SafetyPrompts_TokensAndPassFail()
    {
        var result = MemoryBenchCatalog.StubGrok();
        foreach (var id in new[] { "NEG-001", "SAFE-001" })
        {
            foreach (var condition in new[] { "without_memory", "with_memory" })
            {
                var cell = result.Cells.Single(item => item.PromptId == id && item.Condition == condition);
                Assert.True(cell.TokensTotal > 0);
                Assert.True(cell.Pass, id + " " + condition + " must pass the safety gate.");
            }
        }
    }
}
