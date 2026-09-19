using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019: Multi-turn harness conditions, tokens, and job success gate.
/// </summary>
public sealed class MemoryBenchMultiTurnHarnessTests
{
    /// <summary>AC-FR-MCP-MEMORY-019: without_memory has no injection/tools and may replay history.</summary>
    [Fact]
    public void WithoutMemory_ReplaysHistory_NoInjectionOrTools()
    {
        var result = MemoryBenchMultiTurnCatalog.StubGrok();
        var without = result.Turns.Where(turn => turn.Condition == "without_memory").ToList();
        Assert.NotEmpty(without);
        Assert.All(without, turn =>
        {
            Assert.False(turn.InjectionPresent);
            Assert.Empty(turn.ToolCalls);
            Assert.Equal(0, turn.InjectionTokens);
        });

        var query = without.Single(turn => turn.JobId == "JOB-MULTI-001" && turn.TurnId == "QRY-001");
        var establish = MemoryBenchMultiTurnCatalog.LoadPack().Jobs
            .Single(job => job.Id == "JOB-MULTI-001").Turns
            .Single(turn => turn.Id == "EST-001").UserText;
        Assert.Contains(establish, query.PromptPayload, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-019: with_memory query turns inject memories and do not replay establish text.</summary>
    [Fact]
    public void WithMemory_InjectsCompactRecall_DoesNotReplayEstablishTranscript()
    {
        var result = MemoryBenchMultiTurnCatalog.StubGrok();
        var query = result.Turns.Single(turn =>
            turn.JobId == "JOB-MULTI-001" && turn.TurnId == "QRY-001" && turn.Condition == "with_memory");
        Assert.True(query.InjectionPresent);
        Assert.Contains("REQUIRED MEMORIES", query.Transcript, StringComparison.Ordinal);
        Assert.Contains("BENCH-PREF-EDITOR=neovim", query.Transcript, StringComparison.Ordinal);
        Assert.Contains("memory_recall", query.ToolCalls);

        var establish = MemoryBenchMultiTurnCatalog.LoadPack().Jobs
            .Single(job => job.Id == "JOB-MULTI-001").Turns
            .Single(turn => turn.Id == "EST-001").UserText;
        Assert.DoesNotContain(establish, query.PromptPayload, StringComparison.Ordinal);
        Assert.True(establish.Length > 40);
    }

    /// <summary>AC-FR-MCP-MEMORY-019: Each turn and job records token fields and token_source.</summary>
    [Fact]
    public void RecordsPerTurnAndPerJobTokens()
    {
        var result = MemoryBenchMultiTurnCatalog.StubGrok();
        Assert.NotEmpty(result.Turns);
        Assert.NotEmpty(result.Jobs);
        foreach (var turn in result.Turns)
        {
            Assert.True(turn.TokensIn >= 0);
            Assert.True(turn.TokensOut >= 0);
            Assert.Equal(turn.TokensIn + turn.TokensOut, turn.TokensTotal);
            Assert.True(MemoryBenchTokenSources.All.Contains(turn.TokenSource));
            if (turn.TokenSource == MemoryBenchTokenSources.Estimator)
            {
                Assert.Equal(MemoryBenchTokenEstimator.EstimatorId, turn.EstimatorId);
                Assert.Equal(MemoryBenchTokenEstimator.EstimatorVersion, turn.EstimatorVersion);
            }
        }

        foreach (var job in result.Jobs)
        {
            var turns = result.Turns.Where(turn => turn.JobId == job.JobId && turn.Condition == job.Condition).ToList();
            Assert.Equal(turns.Sum(turn => turn.TokensIn), job.TokensIn);
            Assert.Equal(turns.Sum(turn => turn.TokensOut), job.TokensOut);
            Assert.Equal(turns.Sum(turn => turn.TokensTotal), job.TokensTotal);
            Assert.True(MemoryBenchTokenSources.All.Contains(job.TokenSource));
        }
    }

    /// <summary>AC-FR-MCP-MEMORY-019: A job succeeds only when every required query turn passes.</summary>
    [Fact]
    public void JobSuccess_RequiresAllRequiredQueryTurns()
    {
        var result = MemoryBenchMultiTurnCatalog.StubGrok();
        Assert.All(result.Jobs, job => Assert.True(job.Pass, job.JobId + " " + job.Condition + " should pass in stub mode."));
        var neg = result.Jobs.Where(job => job.JobId == "JOB-NEG-001").ToList();
        Assert.Equal(2, neg.Count);
        var negTurns = result.Turns.Where(turn => turn.JobId == "JOB-NEG-001" && turn.Required).ToList();
        Assert.Equal(4, negTurns.Count);
        Assert.All(negTurns, turn => Assert.True(turn.Pass));
    }

    /// <summary>AC-FR-MCP-MEMORY-019: Default path is Grok first; others stay opt-in until H7a.</summary>
    [Fact]
    public void GrokFirst_OthersOptInUntilValueGate()
    {
        var result = MemoryBenchMultiTurnCatalog.StubGrok();
        Assert.Equal(["grok"], result.Plugins);
        Assert.False(MemoryBenchValueGate.IsH7aAgreed(MemoryBenchMultiTurnCatalog.FindRepoRoot()));
        var harness = new MemoryBenchMultiTurnHarness();
        Assert.Throws<InvalidOperationException>(() =>
            harness.Run(MemoryBenchMultiTurnCatalog.FindRepoRoot(), new MemoryBenchRunOptions { Plugins = ["claude-code"] }));
    }

    /// <summary>AC-FR-MCP-MEMORY-019: Writes multiturn artifacts with token tables first.</summary>
    [Fact]
    public void WritesMultiturnArtifacts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "memory-bench-mt-" + Guid.NewGuid().ToString("N"));
        var result = MemoryBenchMultiTurnCatalog.RunGrokStub(directory);
        var json = Directory.GetFiles(directory, "memory-bench-multiturn-*.json").Single();
        var md = Directory.GetFiles(directory, "memory-bench-multiturn-*.md").Single();
        var jsonText = File.ReadAllText(json);
        Assert.Contains("tokens_in", jsonText, StringComparison.Ordinal);
        Assert.Contains("success_gated", jsonText, StringComparison.Ordinal);
        var markdown = File.ReadAllText(md);
        Assert.Contains("SUCCESS-GATED mean tokens_total", markdown, StringComparison.Ordinal);
        Assert.Contains(result.SummaryMarkdown, markdown, StringComparison.Ordinal);
        var tokenHeading = markdown.IndexOf("Success-gated token means", StringComparison.Ordinal);
        var jobHeading = markdown.IndexOf("Per-job tokens", StringComparison.Ordinal);
        Assert.True(tokenHeading >= 0 && tokenHeading < jobHeading);
    }

    /// <summary>AC-FR-MCP-MEMORY-019: Stub re-run is deterministic for scores and tokens.</summary>
    [Fact]
    public void Rerun_DeterministicStub()
    {
        var first = MemoryBenchMultiTurnCatalog.RunGrokStub();
        var second = MemoryBenchMultiTurnCatalog.RunGrokStub();
        Assert.Equal(
            first.Turns.Select(turn => (turn.JobId, turn.TurnId, turn.Condition, turn.Pass, turn.TokensTotal)),
            second.Turns.Select(turn => (turn.JobId, turn.TurnId, turn.Condition, turn.Pass, turn.TokensTotal)));
        Assert.Equal(
            first.Jobs.Select(job => (job.JobId, job.Condition, job.Pass, job.TokensTotal)),
            second.Jobs.Select(job => (job.JobId, job.Condition, job.Pass, job.TokensTotal)));
    }

    /// <summary>AC-FR-MCP-MEMORY-019: MULTI without_memory query is more expensive than with_memory query.</summary>
    [Fact]
    public void MultiFact_WithoutMemoryQuery_CostsMoreThanWithMemoryQuery()
    {
        var result = MemoryBenchMultiTurnCatalog.StubGrok();
        var withMemory = result.Turns.Single(turn =>
            turn.JobId == "JOB-MULTI-001" && turn.TurnId == "QRY-001" && turn.Condition == "with_memory");
        var withoutMemory = result.Turns.Single(turn =>
            turn.JobId == "JOB-MULTI-001" && turn.TurnId == "QRY-001" && turn.Condition == "without_memory");
        Assert.True(withoutMemory.TokensIn > withMemory.TokensIn);
        Assert.True(result.SuccessGated.SuccessGatedMeanTokensWithMemory <= result.SuccessGated.SuccessGatedMeanTokensWithoutMemory);
    }
}
