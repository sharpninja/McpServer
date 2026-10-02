namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-017:
/// Onboarding docs, examples, and the marker template document new and old verbs.
/// </summary>
public sealed class MemoryOnboardingDocsTests
{
    /// <summary>AC-FR-MCP-MEMORY-017-02: Committed examples for /mcp-transport and STDIO include the new verbs.</summary>
    [Fact]
    public void Examples_ListNewVerbs()
    {
        var root = MemoryS5Catalog.FindRepoRoot();
        var stdio = File.ReadAllText(Path.Combine(root, "docs", "stdio-tool-contract.json"));
        var exampleCandidates = new[]
        {
            Path.Combine(root, "docs", "examples", "memory-stdio-http.mcp.json"),
            Path.Combine(root, "docs", "examples", "give-yourself-memory.md"),
            Path.Combine(root, "docs", "context", "memory.md"),
        };
        var exampleText = string.Join(Environment.NewLine, exampleCandidates
            .Where(File.Exists)
            .Select(File.ReadAllText));

        foreach (var verb in MemoryS5Catalog.NewVerbs)
        {
            Assert.Contains(verb, stdio, StringComparison.Ordinal);
            Assert.Contains(verb, exampleText, StringComparison.Ordinal);
        }

        Assert.True(
            File.Exists(Path.Combine(root, "docs", "examples", "memory-stdio-http.mcp.json")),
            "Committed .mcp.json example for /mcp-transport and STDIO is required.");
    }

    /// <summary>AC-FR-MCP-MEMORY-017-04: One-prompt give-yourself-memory sample exists with real tool names.</summary>
    [Fact]
    public void GiveYourselfMemory_SampleExists()
    {
        var path = Path.Combine(MemoryS5Catalog.FindRepoRoot(), "docs", "examples", "give-yourself-memory.md");
        Assert.True(File.Exists(path), "docs/examples/give-yourself-memory.md must exist.");
        var text = File.ReadAllText(path);
        Assert.Contains("give-yourself-memory", text, StringComparison.OrdinalIgnoreCase);
        foreach (var verb in MemoryS5Catalog.NewVerbs)
            Assert.Contains(verb, text, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-017-07: Marker template documents new verbs without removing old ones.</summary>
    [Fact]
    public void MarkerTemplate_DocumentsNewAndOld()
    {
        var path = Path.Combine(MemoryS5Catalog.FindRepoRoot(), "templates", "prompt-templates.yaml");
        var text = File.ReadAllText(path);
        var sectionStart = text.IndexOf("## MCP Memories", StringComparison.Ordinal);
        Assert.True(sectionStart >= 0, "MCP Memories section is required.");
        var sectionEnd = text.IndexOf("\n      ## ", sectionStart + 10, StringComparison.Ordinal);
        var section = sectionEnd > sectionStart ? text[sectionStart..sectionEnd] : text[sectionStart..];

        foreach (var verb in MemoryS5Catalog.CompatVerbs)
            Assert.Contains(verb, section, StringComparison.Ordinal);
        foreach (var verb in MemoryS5Catalog.NewVerbs)
            Assert.Contains(verb, section, StringComparison.Ordinal);
    }
}
