using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019: Shared v2 multi-turn pack paths and a cached Grok stub run.
/// </summary>
internal static class MemoryBenchMultiTurnCatalog
{
    /// <summary>Required job classes for the v2 pack.</summary>
    public static readonly string[] RequiredClasses =
    [
        "preference",
        "decision",
        "fact",
        "multi-fact",
        "negative-refuse",
    ];

    /// <summary>Locates the repository root.</summary>
    public static string FindRepoRoot() => MemoryS5Catalog.FindRepoRoot();

    /// <summary>Canonical v2 pack path.</summary>
    public static string PackPath()
        => Path.Combine(FindRepoRoot(), MemoryBenchMultiTurnPackLoader.CanonicalRelativePath);

    /// <summary>Loads the committed v2 pack.</summary>
    public static MemoryBenchMultiTurnPackDocument LoadPack()
        => MemoryBenchMultiTurnPackLoader.LoadFromRepo(FindRepoRoot());

    /// <summary>Runs the default Grok stub pack once per process.</summary>
    public static MemoryBenchMultiTurnRunResult StubGrok() => StubGrokHolder.Value;

    /// <summary>Runs a fresh Grok multi-turn stub.</summary>
    public static MemoryBenchMultiTurnRunResult RunGrokStub(string? resultsDirectory = null, bool gateEnabled = false)
    {
        return new MemoryBenchMultiTurnHarness().Run(FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = ["grok"],
            Mode = MemoryBenchModes.Stub,
            ResultsDirectory = resultsDirectory,
            UtcStamp = "20000101T000000Z",
            GateEnabled = gateEnabled,
        });
    }

    private static class StubGrokHolder
    {
        internal static readonly MemoryBenchMultiTurnRunResult Value = RunGrokStub();
    }
}
