using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-019: Shared v2 multi-turn pack paths and a cached Grok stub run.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-019; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryBenchMultiTurnCatalog.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests with the four multi-turn bench classes, its only users (integration-only support; slice S6, hostile validation finding R14-F4); only the namespace changed.</remarks>
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
        => RunStub(["grok"], resultsDirectory, gateEnabled);

    /// <summary>Runs a fresh multi-turn stub for the requested plugins.</summary>
    public static MemoryBenchMultiTurnRunResult RunStub(
        IReadOnlyList<string> plugins,
        string? resultsDirectory = null,
        bool gateEnabled = false,
        string utcStamp = "20000101T000000Z")
    {
        return new MemoryBenchMultiTurnHarness().Run(FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = plugins,
            Mode = MemoryBenchModes.Stub,
            ResultsDirectory = resultsDirectory,
            UtcStamp = utcStamp,
            GateEnabled = gateEnabled,
        });
    }

    /// <summary>Runs the full eight-plugin multi-turn stub pack once per process after H7a.</summary>
    public static MemoryBenchMultiTurnRunResult StubAll() => StubAllHolder.Value;

    private static class StubGrokHolder
    {
        internal static readonly MemoryBenchMultiTurnRunResult Value = RunGrokStub();
    }

    private static class StubAllHolder
    {
        internal static readonly MemoryBenchMultiTurnRunResult Value = RunStub(["all"]);
    }
}
