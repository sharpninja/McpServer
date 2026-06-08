// FR-MCP-REPL-003: Command Namespace Parity - memory workflow implementation
// TR-MCP-REPL-002: DI-Integrated REPL Host - memory workflow registration target
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - memory workflow delegation

using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Production memory workflow implementation that delegates to <see cref="MemoryClient"/>.
/// </summary>
public sealed class MemoryWorkflow : IMemoryWorkflow
{
    private readonly MemoryClient _client;

    /// <summary>Initializes a new instance of the <see cref="MemoryWorkflow"/> class.</summary>
    /// <param name="client">The typed memory client used for transport.</param>
    public MemoryWorkflow(MemoryClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public Task<MemoryQueryResult> ListAsync(
        MemoryScope? scope = null,
        string? category = null,
        string? keyword = null,
        CancellationToken cancellationToken = default)
        => _client.ListAsync(scope, category, keyword, cancellationToken);

    /// <inheritdoc />
    public Task<MemoryItem> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Memory ID cannot be null or empty.", nameof(id));
        }

        return _client.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryMutationResult> AddAsync(MemoryAddRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _client.AddAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryMutationResult> UpdateAsync(string id, MemoryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Memory ID cannot be null or empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(request);
        return _client.UpdateAsync(id, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Memory ID cannot be null or empty.", nameof(id));
        }

        return _client.RemoveAsync(id, cancellationToken);
    }
}
