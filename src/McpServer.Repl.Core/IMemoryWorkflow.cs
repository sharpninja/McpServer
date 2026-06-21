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
}
