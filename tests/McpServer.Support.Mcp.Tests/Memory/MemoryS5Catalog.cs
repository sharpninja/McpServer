using System.Reflection;
using McpServer.Support.Mcp.McpStdio;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016: Shared S5 verb/route/method names and production-surface probes.
/// </summary>
internal static class MemoryS5Catalog
{
    /// <summary>Additive MCP verbs introduced by FR-MCP-MEMORY-017 / TR-MCP-MEMORY-API-002.</summary>
    public static readonly string[] NewVerbs =
    [
        "memory_remember",
        "memory_recall",
        "memory_explore",
        "memory_consolidate",
        "memory_promote",
        "memory_revert",
    ];

    /// <summary>Compat CRUD verbs that must remain registered.</summary>
    public static readonly string[] CompatVerbs =
    [
        "memory_add",
        "memory_list",
        "memory_update",
        "memory_remove",
    ];

    /// <summary>Typed MemoryClient methods that must mirror REST 1:1.</summary>
    public static readonly string[] ClientMethods =
    [
        "RememberAsync",
        "RecallAsync",
        "ExploreAsync",
        "ConsolidateAsync",
        "PromoteAsync",
        "RevertAsync",
        "ListVersionsAsync",
    ];

    /// <summary>REPL workflow methods for remember/recall/explore/consolidate/promote/revert.</summary>
    public static readonly string[] ReplMethods =
    [
        "workflow.memory.remember",
        "workflow.memory.recall",
        "workflow.memory.explore",
        "workflow.memory.consolidate",
        "workflow.memory.promote",
        "workflow.memory.revert",
    ];

    /// <summary>REST route fragments under /mcpserver/memory.</summary>
    public static readonly string[] RestRouteFragments =
    [
        "remember",
        "recall",
        "explore",
        "consolidate",
        "promote",
        "revert",
        "versions",
    ];

    /// <summary>Plugin ids that were deferred until H7a and are required for H7b.</summary>
    public static readonly string[] DeferredPlugins =
    [
        "claude-code",
        "claude-cowork",
        "cline",
        "cline-v2",
        "codex",
        "copilot",
        "opencode",
    ];

    /// <summary>All eight plugin ids in checklist order (Grok first).</summary>
    public static readonly string[] AllPlugins =
    [
        "grok",
        .. DeferredPlugins,
    ];

    /// <summary>Locates the repository root from the test output directory.</summary>
    public static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docs", "stdio-tool-contract.json")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }

    /// <summary>Discovers MCP tool names from <see cref="FwhMcpTools"/> <c>McpServerTool</c> attributes.</summary>
    public static IReadOnlyList<string> DiscoverMcpToolNames()
    {
        var names = new List<string>();
        foreach (var method in typeof(FwhMcpTools).GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            var attribute = method.GetCustomAttributes(inherit: false)
                .FirstOrDefault(item => item.GetType().Name == "McpServerToolAttribute");
            if (attribute is null)
                continue;

            var name = attribute.GetType().GetProperty("Name")?.GetValue(attribute) as string;
            if (!string.IsNullOrWhiteSpace(name))
                names.Add(name);
        }

        return names;
    }

    /// <summary>Reads memory tool names from <c>docs/stdio-tool-contract.json</c>.</summary>
    public static IReadOnlyList<string> DiscoverStdioContractMemoryNames()
    {
        var path = Path.Combine(FindRepoRoot(), "docs", "stdio-tool-contract.json");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString() ?? string.Empty)
            .Where(name => name.StartsWith("memory_", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Reads committed MCP tool JSON file names under <c>mcps/mcpserver/tools</c>.</summary>
    public static IReadOnlyList<string> DiscoverCommittedToolJsonNames()
    {
        var directory = Path.Combine(FindRepoRoot(), "mcps", "mcpserver", "tools");
        if (!Directory.Exists(directory))
            return [];

        return Directory.GetFiles(directory, "memory_*.json")
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .ToList();
    }
}
