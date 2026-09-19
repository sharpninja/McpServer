using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019: Success-gated token aggregation excludes failed jobs.
/// Failed without_memory runs must not create a false efficiency win.
/// </summary>
public sealed class MemoryBenchSuccessGatedAggregationTests
{
    /// <summary>
    /// AC-FR-MCP-MEMORY-019: Macro mean/median tokens_total use only successful jobs.
    /// Failed jobs stay visible with pass=false and are dropped from the means.
    /// </summary>
    [Fact]
    public void SuccessGatedMeans_ExcludeFailedJobs()
    {
        var jobs = new[]
        {
            Job("A", "with_memory", pass: true, tokensIn: 70, tokensOut: 30),
            Job("A", "without_memory", pass: true, tokensIn: 160, tokensOut: 40),
            Job("B", "with_memory", pass: true, tokensIn: 50, tokensOut: 30),
            Job("B", "without_memory", pass: false, tokensIn: 6, tokensOut: 4),
            Job("C", "with_memory", pass: false, tokensIn: 40, tokensOut: 10),
            Job("C", "without_memory", pass: false, tokensIn: 3, tokensOut: 2),
        };

        var summary = MemoryBenchSuccessGatedAggregation.Compute(jobs);

        Assert.Equal(3, summary.JobsN);
        Assert.Equal(2, summary.SuccessfulJobsWithMemory);
        Assert.Equal(1, summary.SuccessfulJobsWithoutMemory);
        Assert.Equal(1, summary.SuccessfulJobsPaired);
        Assert.Equal(2 / 3.0, summary.SuccessRateWithMemory);
        Assert.Equal(1 / 3.0, summary.SuccessRateWithoutMemory);
        Assert.Equal(90, summary.SuccessGatedMeanTokensWithMemory);
        Assert.Equal(90, summary.SuccessGatedMedianTokensWithMemory);
        Assert.Equal(200, summary.SuccessGatedMeanTokensWithoutMemory);
        Assert.Equal(200, summary.SuccessGatedMedianTokensWithoutMemory);
        Assert.Equal(100, summary.PairedSuccessMeanTokensWithMemory);
        Assert.Equal(200, summary.PairedSuccessMeanTokensWithoutMemory);
        Assert.True(summary.WithMemoryWonOnPairedSuccessTokens);
        Assert.Contains("excluded from the success-gated token means", summary.ExclusionNote, StringComparison.Ordinal);
        Assert.Contains("pass=false", summary.ExclusionNote, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-FR-MCP-MEMORY-019: Cheap failed without_memory jobs must not pull the without_memory mean down.
    /// </summary>
    [Fact]
    public void MustNotClaimEfficiencyWin_FromFailedWithoutMemory()
    {
        var jobs = new[]
        {
            Job("A", "with_memory", pass: true, tokensIn: 90, tokensOut: 10),
            Job("A", "without_memory", pass: true, tokensIn: 180, tokensOut: 20),
            Job("B", "with_memory", pass: true, tokensIn: 90, tokensOut: 10),
            Job("B", "without_memory", pass: false, tokensIn: 4, tokensOut: 1),
        };

        var summary = MemoryBenchSuccessGatedAggregation.Compute(jobs);
        var naiveWithoutMean = (200 + 5) / 2.0;

        Assert.Equal(100, summary.SuccessGatedMeanTokensWithMemory);
        Assert.Equal(200, summary.SuccessGatedMeanTokensWithoutMemory);
        Assert.True(summary.SuccessGatedMeanTokensWithoutMemory > naiveWithoutMean);
        Assert.True(summary.WithMemoryWonOnPairedSuccessTokens);
        Assert.Equal(1, summary.SuccessfulJobsPaired);
        Assert.Contains("Do not claim an efficiency win from failed without_memory runs", summary.ExclusionNote, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-FR-MCP-MEMORY-019: Empty success sets yield null means rather than a fabricated zero win.
    /// </summary>
    [Fact]
    public void NoSuccesses_YieldsNullMeans()
    {
        var jobs = new[]
        {
            Job("A", "with_memory", pass: false, tokensIn: 10, tokensOut: 2),
            Job("A", "without_memory", pass: false, tokensIn: 4, tokensOut: 1),
        };

        var summary = MemoryBenchSuccessGatedAggregation.Compute(jobs);
        Assert.Null(summary.SuccessGatedMeanTokensWithMemory);
        Assert.Null(summary.SuccessGatedMeanTokensWithoutMemory);
        Assert.Null(summary.PairedSuccessMeanTokensWithMemory);
        Assert.Null(summary.WithMemoryWonOnPairedSuccessTokens);
        Assert.Equal(0, summary.SuccessRateWithMemory);
        Assert.Equal(0, summary.SuccessRateWithoutMemory);
    }

    /// <summary>
    /// AC-FR-MCP-MEMORY-019: Report markdown highlights success-gated means and names the exclusion rule.
    /// </summary>
    [Fact]
    public void Report_HighlightsSuccessGatedMeans_AndNamesFailedJobExclusion()
    {
        var result = new MemoryBenchMultiTurnRunResult
        {
            PackId = "memory-prompt-pack-v2-multiturn",
            Jobs =
            [
                Job("A", "with_memory", pass: true, tokensIn: 70, tokensOut: 30),
                Job("A", "without_memory", pass: true, tokensIn: 160, tokensOut: 40),
                Job("B", "with_memory", pass: true, tokensIn: 50, tokensOut: 30),
                Job("B", "without_memory", pass: false, tokensIn: 6, tokensOut: 4),
            ],
            Turns =
            [
                new MemoryBenchTurnCellResult
                {
                    Plugin = "grok",
                    JobId = "A",
                    TurnId = "QRY-001",
                    Role = "query",
                    Condition = "with_memory",
                    TokensIn = 70,
                    TokensOut = 30,
                    TokensTotal = 100,
                    TokenSource = MemoryBenchTokenSources.Estimator,
                    Pass = true,
                    Required = true,
                },
            ],
        };

        MemoryBenchMultiTurnReport.Populate(result);
        Assert.Contains("SUCCESS-GATED mean tokens_total", result.SummaryMarkdown, StringComparison.Ordinal);
        Assert.Contains("with_memory=90", result.SummaryMarkdown, StringComparison.Ordinal);
        Assert.Contains("without_memory=200", result.SummaryMarkdown, StringComparison.Ordinal);
        Assert.Contains("excluded from the success-gated token means", result.SummaryMarkdown, StringComparison.Ordinal);
        Assert.Contains("pass=false", result.SummaryMarkdown, StringComparison.Ordinal);
        Assert.Contains("| grok | B | without_memory | false |", result.SummaryMarkdown, StringComparison.Ordinal);
    }

    private static MemoryBenchJobCellResult Job(string id, string condition, bool pass, int tokensIn, int tokensOut)
    {
        return new MemoryBenchJobCellResult
        {
            Plugin = "grok",
            JobId = id,
            Class = "preference",
            Condition = condition,
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            TokensTotal = tokensIn + tokensOut,
            TokenSource = MemoryBenchTokenSources.Estimator,
            Pass = pass,
            FailedTurnIds = pass ? [] : ["QRY-001"],
            TurnsN = 2,
        };
    }
}
