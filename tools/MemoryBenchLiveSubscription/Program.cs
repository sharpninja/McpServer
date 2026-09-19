using MemoryBenchLiveSubscription;
using McpServer.Support.Mcp.Services;

var repositoryRoot = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? Path.GetFullPath(args[0])
    : FindRepositoryRoot();
var stamp = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
    ? args[1]
    : DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

var answersPath = Path.Combine(
    repositoryRoot,
    "docs",
    "benchmarks",
    "live",
    "grok-subscription-cloud-agent-v1.json");
var resultsDirectory = Path.Combine(repositoryRoot, "docs", "benchmarks", "results");
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
var markdown = File.ReadAllText(mdPath);
markdown += """

## Live subject

- plugin: grok
- mode: live
- subject: cursor-grok-4.6-high-fast (Cursor cloud subscription context)
- XAI_API_KEY: not used
- adapter: MemoryBenchSubscriptionGrokAdapter (not MemoryBenchGrokAdapter fixtures)
- token_source: estimator (`memory-bench-whitespace` / `1.0.0`) because the host did not report usage
- h7a-value-gate.json: left `agree=false` (policy placeholder; this run is evidence, not hostile AGREE)

""";
File.WriteAllText(mdPath, markdown);

Console.WriteLine("pack_id=" + result.PackId);
Console.WriteLine("mode=" + result.Mode);
Console.WriteLine("json=" + jsonPath);
Console.WriteLine("md=" + mdPath);
Console.WriteLine("macro_mean_tokens_total_with_memory=" + result.MacroMeanTokensWithMemory.ToString("0.###"));
Console.WriteLine("macro_mean_tokens_total_without_memory=" + result.MacroMeanTokensWithoutMemory.ToString("0.###"));
foreach (var row in result.Summary)
    Console.WriteLine("summary " + row.Plugin + " " + row.Condition + " pass_rate=" + row.PassRate.ToString("0.###") + " tokens_total_mean=" + row.TokensTotalMean.ToString("0.###"));

var failed = result.Cells.Where(cell => !cell.Pass).ToList();
if (failed.Count == 0)
    Console.WriteLine("cell_failures=none");
else
{
    Console.WriteLine("cell_failures=" + failed.Count);
    foreach (var cell in failed)
        Console.WriteLine("FAIL " + cell.PromptId + " " + cell.Condition + " notes=" + cell.Notes);
}

return failed.Count == 0 ? 0 : 2;

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
