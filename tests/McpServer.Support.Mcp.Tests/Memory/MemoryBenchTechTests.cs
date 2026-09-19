using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / TR-MCP-MEMORY-BENCH-002: Harness technology contracts.
/// </summary>
public sealed class MemoryBenchTechTests
{
    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-01: Harness is pwsh/dotnet/plugin runners; no Python product path.</summary>
    [Fact]
    public void NoPythonProductPath()
    {
        var root = MemoryBenchCatalog.FindRepoRoot();
        var harness = File.ReadAllText(Path.Combine(root, "src", "McpServer.Services", "Memory", "Bench", "MemoryBenchHarness.cs"));
        var nuke = File.ReadAllText(Path.Combine(root, "build", "Build.BenchMemory.cs"));
        Assert.DoesNotContain(".py", harness, StringComparison.Ordinal);
        Assert.DoesNotContain("python", nuke, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DotNetTest", nuke, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-02: Plugins are invoked through a documented adapter interface.</summary>
    [Fact]
    public void PluginAdapterInterface()
    {
        IMemoryBenchPluginAdapter adapter = new MemoryBenchGrokAdapter();
        Assert.Equal("grok", adapter.PluginId);
        Assert.Equal("recorded-fixture", adapter.ValidationEntrypoint);
        var readme = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md"));
        Assert.Contains("recorded-fixture", readme, StringComparison.Ordinal);
        Assert.Contains("IMemoryBenchPluginAdapter", typeof(IMemoryBenchPluginAdapter).Name, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-03: Result JSON is schema-validated before write.</summary>
    [Fact]
    public void ResultSchema_Validated()
    {
        var directory = Path.Combine(Path.GetTempPath(), "memory-bench-schema-" + Guid.NewGuid().ToString("N"));
        MemoryBenchCatalog.RunGrokStub(directory);
        var json = File.ReadAllText(Directory.GetFiles(directory, "memory-bench-*.json").Single());
        MemoryBenchResultSchema.ValidateJson(json);
        Assert.True(File.Exists(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), MemoryBenchResultSchema.RelativePath)));
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-04: Pack schema is validated in CI via the loader.</summary>
    [Fact]
    public void PackSchema_Validated()
    {
        MemoryBenchPackLoader.Validate(MemoryBenchCatalog.LoadPack(), File.ReadAllText(MemoryBenchCatalog.PackPath()));
        Assert.True(File.Exists(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), MemoryBenchResultSchema.PackSchemaRelativePath)));
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-05: Injection and tools can be suppressed independently.</summary>
    [Fact]
    public void IndependentSuppressionFlags()
    {
        var noInjection = new MemoryBenchHarness().Run(MemoryBenchCatalog.FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = ["grok"],
            SuppressInjection = true,
            SuppressTools = false,
        });
        var pref = noInjection.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "with_memory");
        Assert.False(pref.InjectionPresent);
        Assert.Contains("memory_recall", pref.ToolCalls);

        var noTools = new MemoryBenchHarness().Run(MemoryBenchCatalog.FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = ["grok"],
            SuppressInjection = false,
            SuppressTools = true,
        });
        var prefTools = noTools.Cells.Single(cell => cell.PromptId == "PREF-001" && cell.Condition == "with_memory");
        Assert.True(prefTools.InjectionPresent);
        Assert.Empty(prefTools.ToolCalls);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-06: Latency is captured but is not the sole pass/fail.</summary>
    [Fact]
    public void Latency_OptionalBudget()
    {
        var pack = MemoryBenchCatalog.LoadPack();
        Assert.All(pack.Prompts, prompt => Assert.Null(prompt.LatencyBudgetMs));
        Assert.All(MemoryBenchCatalog.StubGrok().Cells, cell => Assert.True(cell.LatencyMs >= 0));
        var pref = pack.Prompts.Single(prompt => prompt.Id == "PREF-001");
        var (score, pass, _) = MemoryBenchScoring.Score(pref, "neovim", "with_memory");
        Assert.True(pass);
        Assert.Equal(1, score);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-07: Nuke target BenchMemory exists and defaults to Grok stub.</summary>
    [Fact]
    public void CiTarget_BenchMemory()
    {
        var source = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "build", "Build.BenchMemory.cs"));
        Assert.Contains("public Target BenchMemory", source, StringComparison.Ordinal);
        Assert.Contains("readonly string Plugin = \"grok\"", source, StringComparison.Ordinal);
        Assert.Contains("stub", File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "docs", "benchmarks", "README.md")), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-08: results/* is gitignored except baselines.</summary>
    [Fact]
    public void Results_GitignorePolicy()
    {
        var gitignore = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), ".gitignore"));
        Assert.Contains("docs/benchmarks/results/*", gitignore, StringComparison.Ordinal);
        Assert.Contains("!docs/benchmarks/results/.gitkeep", gitignore, StringComparison.Ordinal);
        Assert.Contains("!docs/benchmarks/results/baselines/", gitignore, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-09: Schema requires tokens_* integers and token_source enum.</summary>
    [Fact]
    public void ResultSchema_RequiresTokenFields()
    {
        var schema = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), MemoryBenchResultSchema.RelativePath));
        Assert.Contains("tokens_in", schema, StringComparison.Ordinal);
        Assert.Contains("tokens_out", schema, StringComparison.Ordinal);
        Assert.Contains("tokens_total", schema, StringComparison.Ordinal);
        Assert.Contains("token_source", schema, StringComparison.Ordinal);
        Assert.Contains("\"host\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"estimator\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"recorded\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"minimum\": 0", schema, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-10: Missing token fields hard-fail finalize.</summary>
    [Fact]
    public void MissingTokens_HardFail()
    {
        var incomplete = new MemoryBenchRunResult
        {
            Cells =
            [
                new MemoryBenchCellResult
                {
                    Plugin = "grok",
                    PromptId = "PREF-001",
                    Condition = "with_memory",
                    Transcript = "missing tokens",
                },
            ],
        };
        var ex = Assert.Throws<MemoryBenchTokenSchemaException>(() => MemoryBenchHarness.Finalize(incomplete));
        Assert.Contains("lacks required token fields", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-11: Estimator is pure and versioned.</summary>
    [Fact]
    public void Estimator_VersionedDeterministic()
    {
        Assert.Equal("memory-bench-whitespace", MemoryBenchTokenEstimator.EstimatorId);
        Assert.Equal("1.0.0", MemoryBenchTokenEstimator.EstimatorVersion);
        Assert.Equal(MemoryBenchTokenEstimator.Count("one two"), MemoryBenchTokenEstimator.Count("one two"));
        Assert.Equal(2, MemoryBenchTokenEstimator.Count("one two"));
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-12: CI summary prints token means first.</summary>
    [Fact]
    public void CiSummary_TokensFirst()
    {
        var source = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "build", "Build.BenchMemory.cs"));
        var tokenIndex = source.IndexOf("TOKEN MEANS", StringComparison.Ordinal);
        var testIndex = source.IndexOf("DotNetTest", StringComparison.Ordinal);
        Assert.True(tokenIndex >= 0 && testIndex > tokenIndex, "Token means must print before tests.");
    }

    /// <summary>AC-TR-MCP-MEMORY-BENCH-002-13: Default plugin is grok; all is explicit and post-H7a.</summary>
    [Fact]
    public void DefaultPlugin_IsGrok()
    {
        var source = File.ReadAllText(Path.Combine(MemoryBenchCatalog.FindRepoRoot(), "build", "Build.BenchMemory.cs"));
        Assert.Contains("readonly string Plugin = \"grok\"", source, StringComparison.Ordinal);
        Assert.Contains("Plugin all is blocked until H7a", source, StringComparison.Ordinal);
        Assert.Equal("grok", MemoryBenchAdapterRegistry.DefaultPlugin);
    }
}
