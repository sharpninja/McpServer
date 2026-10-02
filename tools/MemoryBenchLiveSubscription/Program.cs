using MemoryBenchLiveSubscription;
using McpServer.Support.Mcp.Services;

var repositoryRoot = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) && args[0] != "multiturn"
    ? Path.GetFullPath(args[0])
    : FindRepositoryRoot();
var stamp = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) && args[1] != "multiturn"
    ? args[1]
    : DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
var multiTurn = args.Any(arg => string.Equals(arg, "multiturn", StringComparison.OrdinalIgnoreCase));
var resultsDirectory = Path.Combine(repositoryRoot, "docs", "benchmarks", "results");

if (multiTurn)
    return RunMultiTurn(repositoryRoot, stamp, resultsDirectory);

return RunV1(repositoryRoot, stamp, resultsDirectory);

static int RunV1(string repositoryRoot, string stamp, string resultsDirectory)
{
    var answersPath = Path.Combine(repositoryRoot, "docs", "benchmarks", "live", "grok-subscription-cloud-agent-v1.json");
    var adapter = MemoryBenchSubscriptionGrokAdapter.LoadFromFile(answersPath);
    var result = new MemoryBenchHarness().Run(repositoryRoot, new MemoryBenchRunOptions
    {
        Plugins = ["grok"],
        Mode = MemoryBenchModes.Live,
        ResultsDirectory = resultsDirectory,
        UtcStamp = stamp,
        GateEnabled = true,
        Adapters = new Dictionary<string, IMemoryBenchPluginAdapter>(StringComparer.OrdinalIgnoreCase)
        {
            ["grok"] = adapter,
        },
    });

    var jsonPath = Path.Combine(resultsDirectory, "memory-bench-" + stamp + ".json");
    var mdPath = Path.Combine(resultsDirectory, "memory-bench-" + stamp + ".md");
    AppendLiveFooter(mdPath, "MemoryBenchSubscriptionGrokAdapter (not MemoryBenchGrokAdapter fixtures)", "v1 smoke/regression");
    Console.WriteLine("pack_id=" + result.PackId);
    Console.WriteLine("mode=" + result.Mode);
    Console.WriteLine("json=" + jsonPath);
    Console.WriteLine("md=" + mdPath);
    var failed = result.Cells.Where(cell => !cell.Pass).ToList();
    return failed.Count == 0 ? 0 : 2;
}

static int RunMultiTurn(string repositoryRoot, string stamp, string resultsDirectory)
{
    var answersPath = Path.Combine(repositoryRoot, "docs", "benchmarks", "live", "grok-subscription-cloud-agent-v2-multiturn.json");
    var adapter = MemoryBenchSubscriptionGrokMultiTurnAdapter.LoadFromFile(answersPath);
    var result = new MemoryBenchMultiTurnHarness().Run(repositoryRoot, new MemoryBenchRunOptions
    {
        Plugins = ["grok"],
        Mode = MemoryBenchModes.Live,
        ResultsDirectory = resultsDirectory,
        UtcStamp = stamp,
        GateEnabled = true,
        MultiTurnAdapters = new Dictionary<string, IMemoryBenchMultiTurnAdapter>(StringComparer.OrdinalIgnoreCase)
        {
            ["grok"] = adapter,
        },
    });

    var jsonPath = Path.Combine(resultsDirectory, "memory-bench-multiturn-" + stamp + ".json");
    var mdPath = Path.Combine(resultsDirectory, "memory-bench-multiturn-" + stamp + ".md");
    var gated = result.SuccessGated;
    AppendLiveFooter(
        mdPath,
        "MemoryBenchSubscriptionGrokMultiTurnAdapter (not MemoryBenchGrokMultiTurnAdapter fixtures)",
        "v2 real efficiency bench");
    Console.WriteLine("pack_id=" + result.PackId);
    Console.WriteLine("mode=" + result.Mode);
    Console.WriteLine("json=" + jsonPath);
    Console.WriteLine("md=" + mdPath);
    Console.WriteLine("success_rate_with_memory=" + gated.SuccessRateWithMemory.ToString("0.###"));
    Console.WriteLine("success_rate_without_memory=" + gated.SuccessRateWithoutMemory.ToString("0.###"));
    Console.WriteLine("success_gated_mean_tokens_with_memory=" + (gated.SuccessGatedMeanTokensWithMemory?.ToString("0.###") ?? "n/a"));
    Console.WriteLine("success_gated_mean_tokens_without_memory=" + (gated.SuccessGatedMeanTokensWithoutMemory?.ToString("0.###") ?? "n/a"));
    Console.WriteLine("paired_success_mean_tokens_with_memory=" + (gated.PairedSuccessMeanTokensWithMemory?.ToString("0.###") ?? "n/a"));
    Console.WriteLine("paired_success_mean_tokens_without_memory=" + (gated.PairedSuccessMeanTokensWithoutMemory?.ToString("0.###") ?? "n/a"));
    Console.WriteLine("with_memory_won_on_paired_success_tokens=" + (gated.WithMemoryWonOnPairedSuccessTokens?.ToString() ?? "n/a"));
    var failed = result.Jobs.Where(job => !job.Pass).ToList();
    if (failed.Count == 0)
        Console.WriteLine("job_failures=none");
    else
    {
        Console.WriteLine("job_failures=" + failed.Count);
        foreach (var job in failed)
            Console.WriteLine("FAIL " + job.JobId + " " + job.Condition + " failed_turns=" + string.Join(',', job.FailedTurnIds));
    }

    return failed.Count == 0 ? 0 : 2;
}

static void AppendLiveFooter(string mdPath, string adapter, string packRole)
{
    var markdown = File.ReadAllText(mdPath);
    markdown += $"""

## Live subject

- plugin: grok
- mode: live
- pack_role: {packRole}
- subject: cursor-grok-4.6-high-fast (Cursor cloud subscription context)
- XAI_API_KEY: not used
- adapter: {adapter}
- token_source: estimator (`memory-bench-whitespace` / `1.0.0`) because the host did not report usage
- h7a-value-gate.json: left `agree=false` (policy placeholder; this run is evidence, not hostile AGREE)

""";
    File.WriteAllText(mdPath, markdown);
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "docs", "benchmarks", "memory-prompt-pack-v1.yaml")))
            return directory.FullName;
        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate the McpServer repository root.");
}
