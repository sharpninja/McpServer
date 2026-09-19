// FR-MCP-REPL-003: Command Namespace Parity - memory workflow interface
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - memory workflow delegation

using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Defines the canonical memory workflow operations exposed through <c>workflow.memory.*</c>.
/// </summary>
public interface IMemoryWorkflow
{
    /// <summary>Lists effective memories visible to the active workspace.</summary>
    Task<MemoryQueryResult> ListAsync(
        MemoryScope? scope = null,
        string? category = null,
        string? keyword = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one visible memory by id.</summary>
    Task<MemoryItem> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Adds a memory item.</summary>
    Task<MemoryMutationResult> AddAsync(MemoryAddRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a visible memory item by id.</summary>
    Task<MemoryMutationResult> UpdateAsync(string id, MemoryUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a visible memory item by id.</summary>
    Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Remembers a multi-layer memory.</summary>
    Task<MemorySurfaceResult> RememberAsync(MemoryRememberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Recalls memories by meaning or keyword.</summary>
    Task<MemorySurfaceResult> RecallAsync(MemoryRecallRequest request, CancellationToken cancellationToken = default);

    /// <summary>Explores a memory neighborhood.</summary>
    Task<MemorySurfaceResult> ExploreAsync(MemoryExploreRequest request, CancellationToken cancellationToken = default);

    /// <summary>Plans or applies consolidate/sleep merge.</summary>
    Task<MemorySurfaceResult> ConsolidateAsync(MemoryConsolidateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Promotes a session-log or context source into memory.</summary>
    Task<MemorySurfaceResult> PromoteAsync(MemoryPromoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reverts a memory to snapshot N.</summary>
    Task<MemorySurfaceResult> RevertAsync(string id, int versionNumber, CancellationToken cancellationToken = default);
}
