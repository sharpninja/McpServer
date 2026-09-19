namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018: Token-first summary tables.
/// </summary>
public sealed class MemoryBenchReportTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-24: Summary compares with_memory vs without_memory pass rates.</summary>
    [Fact]
    public void SummaryComparesConditions()
    {
        var result = MemoryBenchCatalog.StubGrok();
        var withMemory = result.Summary.Single(row => row.Plugin == "grok" && row.Condition == "with_memory");
        var withoutMemory = result.Summary.Single(row => row.Plugin == "grok" && row.Condition == "without_memory");
        Assert.Equal(8, withMemory.PromptsN);
        Assert.Equal(8, withoutMemory.PromptsN);
        Assert.InRange(withMemory.PassRate, 0, 1);
        Assert.InRange(withoutMemory.PassRate, 0, 1);
        Assert.Contains("pass_rate", result.SummaryMarkdown, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-018-38: Summary table includes the required token columns.</summary>
    [Fact]
    public void SummaryTable_TokenColumns()
    {
        var markdown = MemoryBenchCatalog.StubGrok().SummaryMarkdown;
        foreach (var column in new[]
        {
            "plugin", "condition", "prompts_n", "tokens_total_sum", "tokens_total_mean",
            "tokens_total_median", "tokens_in_mean", "tokens_out_mean", "pass_rate",
        })
        {
            Assert.Contains(column, markdown, StringComparison.Ordinal);
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-018-40: Headline numbers are macro mean tokens_total.</summary>
    [Fact]
    public void Headline_MacroMeanTokens()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.True(result.MacroMeanTokensWithMemory > 0);
        Assert.True(result.MacroMeanTokensWithoutMemory > 0);
        Assert.Contains("Headline macro mean tokens_total", result.SummaryMarkdown, StringComparison.Ordinal);
        Assert.Contains("with_memory=", result.SummaryMarkdown, StringComparison.Ordinal);
        Assert.Contains("without_memory=", result.SummaryMarkdown, StringComparison.Ordinal);
    }
}
