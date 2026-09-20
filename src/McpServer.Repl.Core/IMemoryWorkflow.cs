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
    Task<MemoryRememberResult> RememberAsync(MemoryRememberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Recalls memories by meaning or keyword and preserves ranked hits.</summary>
    Task<MemoryRecallResult> RecallAsync(MemoryRecallRequest request, CancellationToken cancellationToken = default);

    /// <summary>Explores a memory neighborhood and preserves neighbor items/hits.</summary>
    Task<MemoryExploreResult> ExploreAsync(MemoryExploreRequest request, CancellationToken cancellationToken = default);

    /// <summary>Plans or applies consolidate/sleep merge and preserves plan items.</summary>
    Task<MemoryConsolidateResult> ConsolidateAsync(MemoryConsolidateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Promotes a session-log or context source into memory.</summary>
    Task<MemoryPromoteResult> PromoteAsync(MemoryPromoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reverts a memory to snapshot N.</summary>
    Task<MemoryRevertResult> RevertAsync(string id, int versionNumber, CancellationToken cancellationToken = default);
}
