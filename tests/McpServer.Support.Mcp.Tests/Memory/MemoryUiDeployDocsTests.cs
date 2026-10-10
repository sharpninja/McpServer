namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-017 / TR-MCP-MEMORY-UI-002-02:
/// Deploy docs name Nuke UpdateService as the ship path for /memory/.
/// </summary>
public sealed class MemoryUiDeployDocsTests
{
    /// <summary>AC-TR-MCP-MEMORY-UI-002-02: Deploy docs name Nuke UpdateService as the ship path.</summary>
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
