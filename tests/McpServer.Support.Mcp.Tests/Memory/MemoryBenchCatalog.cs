using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018: Shared S7/S7a pack paths and a cached Grok stub run.
/// </summary>
internal static class MemoryBenchCatalog
{
    /// <summary>Required prompt classes from FR-MCP-MEMORY-018-02.</summary>
    public static readonly string[] RequiredClasses =
    [
        "preference",
        "decision",
        "fact-paraphrase",
        "procedure",
        "multi-fact",
        "negative-absent",
        "conflict-stale-vs-new",
        "refuse-invented-secret",
    ];

    /// <summary>Locates the repository root.</summary>
    public static string FindRepoRoot() => MemoryS5Catalog.FindRepoRoot();

    /// <summary>Canonical pack path.</summary>
    public static string PackPath() => Path.Combine(FindRepoRoot(), MemoryBenchPackLoader.CanonicalRelativePath);

    /// <summary>Loads the committed pack.</summary>
    public static MemoryBenchPackDocument LoadPack() => MemoryBenchPackLoader.LoadFromRepo(FindRepoRoot());

    /// <summary>Runs the default Grok stub pack once per process.</summary>
    public static MemoryBenchRunResult StubGrok() => StubGrokHolder.Value;

    /// <summary>Runs a fresh Grok stub into a temp results directory.</summary>
    public static MemoryBenchRunResult RunGrokStub(string? resultsDirectory = null, bool gateEnabled = false)
        => RunStub(["grok"], resultsDirectory, gateEnabled);

    /// <summary>Runs a fresh stub for the requested plugins.</summary>
    public static MemoryBenchRunResult RunStub(
        IReadOnlyList<string> plugins,
        string? resultsDirectory = null,
        bool gateEnabled = false,
        string utcStamp = "20000101T000000Z")
    {
        return new MemoryBenchHarness().Run(FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = plugins,
            Mode = MemoryBenchModes.Stub,
            ResultsDirectory = resultsDirectory,
            UtcStamp = utcStamp,
            GateEnabled = gateEnabled,
        });
    }

    /// <summary>Runs the full eight-plugin stub pack once per process after H7a.</summary>
    public static MemoryBenchRunResult StubAll() => StubAllHolder.Value;

    private static class StubGrokHolder
    {
        internal static readonly MemoryBenchRunResult Value = RunGrokStub();
    }

    private static class StubAllHolder
    {
        internal static readonly MemoryBenchRunResult Value = RunStub(["all"]);
    }
}
