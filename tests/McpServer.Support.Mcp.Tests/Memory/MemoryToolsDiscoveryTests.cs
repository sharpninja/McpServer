using McpServer.Support.Mcp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-017:
/// New memory verbs appear in tools search, descriptions are agent-oriented,
/// compat verbs remain, and STDIO equals HTTP.
/// </summary>
public sealed class MemoryToolsDiscoveryTests
{
    /// <summary>AC-FR-MCP-MEMORY-017-01: New memory verbs appear in /mcpserver/tools search.</summary>
    [Fact]
    public async Task NewVerbs_AppearInToolsSearch()
    {
        var mcpNames = MemoryS5Catalog.DiscoverMcpToolNames();
        foreach (var verb in MemoryS5Catalog.NewVerbs)
            Assert.Contains(verb, mcpNames);

        await using var db = CreateToolDb();
        var registry = new ToolRegistryService(db, NullLogger<ToolRegistryService>.Instance);
        var seederType = typeof(ToolRegistryService).Assembly.GetTypes()
            .FirstOrDefault(type => type.Name == "MemoryToolRegistrySeeder");
        Assert.NotNull(seederType);
        var seed = seederType!.GetMethod("SeedAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(seed);
        var seeded = seed!.Invoke(null, [registry, TestContext.Current.CancellationToken]);
        if (seeded is Task task)
            await task.ConfigureAwait(true);

        var search = await registry.SearchAsync("memory", ct: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var found = search.Tools.Select(tool => tool.Name).ToList();
        foreach (var verb in MemoryS5Catalog.NewVerbs)
            Assert.Contains(verb, found);
    }

    /// <summary>AC-FR-MCP-MEMORY-017-05: Tool descriptions say when to remember vs recall vs promote.</summary>
    [Fact]
    public void Descriptions_AgentOriented()
    {
        var toolsType = typeof(McpServer.Support.Mcp.McpStdio.FwhMcpTools);
        var descriptions = toolsType.GetMethods()
            .Select(method =>
            {
                var tool = method.GetCustomAttributes(false)
                    .FirstOrDefault(item => item.GetType().Name == "McpServerToolAttribute");
                var name = tool?.GetType().GetProperty("Name")?.GetValue(tool) as string;
                var description = method.GetCustomAttributes(false)
                    .FirstOrDefault(item => item.GetType().Name == "DescriptionAttribute");
                var text = description?.GetType().GetProperty("Description")?.GetValue(description) as string;
                return (name, text);
            })
            .Where(item => item.name is not null)
            .ToDictionary(item => item.name!, item => item.text ?? string.Empty, StringComparer.Ordinal);

        Assert.Contains("memory_remember", descriptions.Keys);
        Assert.Contains("memory_recall", descriptions.Keys);
        Assert.Contains("memory_promote", descriptions.Keys);
        Assert.Contains("when", descriptions["memory_remember"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("when", descriptions["memory_recall"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("when", descriptions["memory_promote"], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("schema echo", descriptions["memory_remember"], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>AC-FR-MCP-MEMORY-017-06: Compat verbs remain listed alongside new verbs.</summary>
    [Fact]
    public void CompatVerbs_StillListed()
    {
        var names = MemoryS5Catalog.DiscoverMcpToolNames();
        foreach (var verb in MemoryS5Catalog.CompatVerbs)
            Assert.Contains(verb, names);
        foreach (var verb in MemoryS5Catalog.NewVerbs)
            Assert.Contains(verb, names);
    }

    /// <summary>AC-FR-MCP-MEMORY-017-09: STDIO and Streamable HTTP advertise the same memory verb set.</summary>
    [Fact]
    public void StdioAndHttp_SameVerbSet()
    {
        var httpNames = MemoryS5Catalog.DiscoverMcpToolNames()
            .Where(name => name.StartsWith("memory_", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var stdioNames = MemoryS5Catalog.DiscoverStdioContractMemoryNames()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var jsonNames = MemoryS5Catalog.DiscoverCommittedToolJsonNames()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var verb in MemoryS5Catalog.NewVerbs.Concat(MemoryS5Catalog.CompatVerbs))
        {
            Assert.Contains(verb, httpNames);
            Assert.Contains(verb, stdioNames);
            Assert.Contains(verb, jsonNames);
        }

        Assert.Equal(httpNames, stdioNames);
    }

    private static McpServer.Support.Mcp.Storage.McpDbContext CreateToolDb()
    {
        var options = new DbContextOptionsBuilder<McpServer.Support.Mcp.Storage.McpDbContext>()
            .UseInMemoryDatabase("s5-tools-" + Guid.NewGuid().ToString("N"))
            .Options;
        var db = new McpServer.Support.Mcp.Storage.McpDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
