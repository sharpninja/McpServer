namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-003: Service for managing MCP agent memories across Global
/// and active Workspace scopes.
/// </summary>
public interface IMemoryService
{
    /// <summary>Adds a new memory.</summary>
    Task<MemoryMutationResult> AddAsync(MemoryAddRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists effective memories visible to the active workspace.</summary>
    Task<MemoryQueryResult> ListAsync(MemoryListRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets one visible memory by id.</summary>
    Task<MemoryItem?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Updates one visible memory by id.</summary>
    Task<MemoryMutationResult> UpdateAsync(string id, MemoryUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes one visible memory by id using the shared soft-delete path.</summary>
    Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default);
}
