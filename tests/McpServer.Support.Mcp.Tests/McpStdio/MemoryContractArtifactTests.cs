using System.Text.Json;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-MEMORY-006 and TEST-MCP-MEMORY-007: Verifies durable contract
/// artifacts expose memory guidance and STDIO memory tool metadata.
/// </summary>
public sealed class MemoryContractArtifactTests
{
    /// <summary>The STDIO contract manifest includes all memory tools and their scope values.</summary>
    [Fact]
    public void StdioToolContract_IncludesMemoryToolsAndScopes()
    {
        var contractPath = Path.Combine(FindRepoRoot(), "docs", "stdio-tool-contract.json");
        using var document = JsonDocument.Parse(File.ReadAllText(contractPath));
        var tools = document.RootElement.GetProperty("tools").EnumerateArray().ToArray();

        var list = Assert.Single(tools, tool => GetString(tool, "name") == "memory_list");
        AssertMemoryToolExists(tools, "memory_get");
        AssertMemoryToolExists(tools, "memory_add");
        AssertMemoryToolExists(tools, "memory_update");
        AssertMemoryToolExists(tools, "memory_remove");

        var listScopes = list
            .GetProperty("parameters")
            .GetProperty("scope")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(["Effective", "Global", "Workspace"], listScopes);

        var add = Assert.Single(tools, tool => GetString(tool, "name") == "memory_add");
        var addScopes = add
            .GetProperty("parameters")
            .GetProperty("scope")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(["Global", "Workspace"], addScopes);
    }

    /// <summary>The default marker prompt documents required memory injection and tool surfaces.</summary>
    [Fact]
    public void DefaultMarkerPrompt_IncludesMcpMemoryGuidance()
    {
        var templatePath = Path.Combine(FindRepoRoot(), "templates", "prompt-templates.yaml");
        var text = File.ReadAllText(templatePath);

        Assert.Contains("## MCP Memories", text, StringComparison.Ordinal);
        Assert.Contains("REQUIRED MEMORIES", text, StringComparison.Ordinal);
        Assert.Contains("- MEMORY-REQ-001: Raw memory text.", text, StringComparison.Ordinal);
        Assert.Contains("- None.", text, StringComparison.Ordinal);
        Assert.Contains("MEMORY-{CATEGORY}-{NNN}", text, StringComparison.Ordinal);
        Assert.Contains("Global memories first sorted by ID", text, StringComparison.Ordinal);
        Assert.Contains("memory_add", text, StringComparison.Ordinal);
        Assert.Contains("workflow.memory.list", text, StringComparison.Ordinal);
        Assert.Contains("do not summarize, paraphrase, reorder within a scope, silently generate new memories, or add secrets", text, StringComparison.Ordinal);
        Assert.Contains("MCP memories are not a secret store", text, StringComparison.Ordinal);
        Assert.Contains("agent-local memory stores are caches or migration sources only", text, StringComparison.Ordinal);
        Assert.Contains("updatedBy", text, StringComparison.Ordinal);
        Assert.Contains("workflow.sessionlog.appendActions", text, StringComparison.Ordinal);
    }

    /// <summary>Memory context documentation includes import safeguards and audit attribution rules.</summary>
    [Fact]
    public void MemoryContextDocumentation_IncludesImportAndAttributionRules()
    {
        var docPath = Path.Combine(FindRepoRoot(), "docs", "context", "memory.md");
        var text = File.ReadAllText(docPath);

        Assert.Contains("Agent-local memory files may be used as private caches or migration sources", text, StringComparison.Ordinal);
        Assert.Contains("Exclude secrets, credentials, private unrelated workspace notes", text, StringComparison.Ordinal);
        Assert.Contains("updatedBy", text, StringComparison.Ordinal);
        Assert.Contains("workflow.sessionlog.appendActions", text, StringComparison.Ordinal);
        Assert.Contains("Do not add private file paths", text, StringComparison.Ordinal);
    }

    private static void AssertMemoryToolExists(JsonElement[] tools, string name)
    {
        Assert.Contains(tools, tool => GetString(tool, "name") == name);
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docs", "stdio-tool-contract.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
