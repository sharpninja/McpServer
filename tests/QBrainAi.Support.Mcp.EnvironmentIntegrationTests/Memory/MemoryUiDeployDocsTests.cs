namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-017 (historical) / TR-MCP-MEMORY-UI-002-02 (historical):
/// Deploy docs name Nuke UpdateService as the ship path for /memory/.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-017; TR-MCP-MEMORY-UI-002; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryUiDeployDocsTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryS6Catalog moved with it; only the namespace changed.</remarks>
public sealed class MemoryUiDeployDocsTests
{
    /// <summary>AC-TR-MCP-MEMORY-UI-002-02: Deploy docs name Nuke UpdateService as the ship path.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void NamesUpdateService()
    {
        var root = MemoryS6Catalog.FindRepoRoot();
        var userGuide = File.ReadAllText(Path.Combine(root, "docs", "USER-GUIDE.md"));
        var mcpServer = File.ReadAllText(Path.Combine(root, "docs", "MCP-SERVER.md"));
        var memory = File.ReadAllText(Path.Combine(root, "docs", "context", "memory.md"));
        var combined = userGuide + Environment.NewLine + mcpServer + Environment.NewLine + memory;

        Assert.Contains("/memory/", combined, StringComparison.Ordinal);
        Assert.Contains("UpdateService", combined, StringComparison.Ordinal);
        Assert.Contains("Nuke", combined, StringComparison.Ordinal);

        Assert.True(
            SectionNamesUpdateService(userGuide) || SectionNamesUpdateService(mcpServer),
            "USER-GUIDE or MCP-SERVER must name Nuke UpdateService in the /memory/ deploy section.");
    }

    private static bool SectionNamesUpdateService(string text)
    {
        var index = text.IndexOf("/memory/", StringComparison.Ordinal);
        if (index < 0)
            return false;

        var start = Math.Max(0, index - 400);
        var length = Math.Min(text.Length - start, 1200);
        var window = text.Substring(start, length);
        return window.Contains("UpdateService", StringComparison.Ordinal)
            && window.Contains("Nuke", StringComparison.Ordinal);
    }
}
