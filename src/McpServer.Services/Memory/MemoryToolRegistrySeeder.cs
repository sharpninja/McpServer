namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-017: Seeds additive and compat memory verbs into the tool registry
/// so <c>/mcpserver/tools</c> search lists them.
/// </summary>
public static class MemoryToolRegistrySeeder
{
    /// <summary>Registers S5 memory verbs when they are not already present.</summary>
    public static async Task SeedAsync(IToolRegistryService registry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registry);

        foreach (var verb in MemorySurfaceCatalog.NewVerbs.Concat(MemorySurfaceCatalog.CompatVerbs))
        {
            var created = await registry.CreateAsync(
                    new ToolCreateRequest(
                        verb,
                        DescriptionFor(verb),
                        ["memory", verb],
                        WorkspacePath: null),
                    cancellationToken)
                .ConfigureAwait(false);
            if (created.Success)
                continue;

            if (created.Error is not null
                && created.Error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
        }
    }

    private static string DescriptionFor(string verb) => verb switch
    {
        "memory_remember" => MemorySurfaceCatalog.RememberDescription,
        "memory_recall" => MemorySurfaceCatalog.RecallDescription,
        "memory_explore" => MemorySurfaceCatalog.ExploreDescription,
        "memory_consolidate" => MemorySurfaceCatalog.ConsolidateDescription,
        "memory_promote" => MemorySurfaceCatalog.PromoteDescription,
        "memory_revert" => MemorySurfaceCatalog.RevertDescription,
        "memory_add" => "Add a memory item. Defaults to Workspace scope.",
        "memory_list" => "List effective memory items. Optional filters: scope, category, keyword.",
        "memory_update" => "Update a memory item by id. Only provided fields are changed.",
        "memory_remove" => "Remove a visible memory item by id.",
        _ => verb,
    };
}
