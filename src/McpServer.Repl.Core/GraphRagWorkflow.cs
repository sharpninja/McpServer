// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - GraphRAG workflow implementation
// FR-MCP-REPL-003: Command Namespace Parity - GraphRAG operation implementation
// FR-MCP-078: Ad-hoc text ingestion workflow implementation
// FR-MCP-079: Entity and relationship CRUD workflow implementation
// FR-MCP-080: Document management workflow implementation
// TR-MCP-REPL-002: DI-Integrated REPL Host - GraphRAG workflow DI registration
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - GraphRAG workflow delegation
// TR-GRAPHRAG-ADHOC-001: Ad-hoc text ingestion delegation
// TR-GRAPHRAG-ADHOC-002: Entity and relationship CRUD delegation
// TR-GRAPHRAG-ADHOC-003: Document management delegation

using System;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Production GraphRAG workflow implementation.
/// Delegates all operations to <see cref="ContextClient"/> without duplicating business logic.
/// </summary>
public sealed class GraphRagWorkflow : IGraphRagWorkflow
{
    private readonly ContextClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphRagWorkflow"/> class.
    /// </summary>
    /// <param name="client">The ContextClient to use for GraphRAG operations.</param>
    public GraphRagWorkflow(ContextClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    // ── Lifecycle ──

    /// <inheritdoc />
    public async Task<GraphRagStatusResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _client.GraphRagStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRagStatusResult> IndexAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        return await _client.GraphRagIndexAsync(force, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRagQueryResult> QueryAsync(
        string query,
        string? mode = null,
        int? maxChunks = null,
        bool includeContextChunks = true,
        int? maxEntities = null,
        int? maxRelationships = null,
        int? communityDepth = null,
        int? responseTokenBudget = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.GraphRagQueryAsync(
            query, mode, maxChunks, includeContextChunks,
            maxEntities, maxRelationships, communityDepth, responseTokenBudget,
            cancellationToken).ConfigureAwait(false);
    }

    // ── Ingestion ──

    /// <inheritdoc />
    public async Task<GraphRagIngestTextResult> IngestTextAsync(
        GraphRagIngestTextRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _client.GraphRagIngestTextAsync(request, cancellationToken).ConfigureAwait(false);
    }

    // ── Document Management ──

    /// <inheritdoc />
    public async Task<GraphRagDocumentListResult> ListDocumentsAsync(
        int skip = 0, int take = 50, string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.GraphRagListDocumentsAsync(skip, take, sourceType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRagDocumentChunksResult> GetDocumentChunksAsync(
        string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        return await _client.GraphRagGetDocumentChunksAsync(documentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRagDocumentDeleteResult> DeleteDocumentAsync(
        string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        return await _client.GraphRagDeleteDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
    }

    // ── Entity CRUD ──

    /// <inheritdoc />
    public async Task<GraphEntityResult> CreateEntityAsync(
        GraphEntityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _client.GraphRagCreateEntityAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphEntityListResult> ListEntitiesAsync(
        int skip = 0, int take = 50, string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.GraphRagListEntitiesAsync(skip, take, entityType, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphEntityResult> GetEntityAsync(
        string entityId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityId);
        return await _client.GraphRagGetEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphEntityResult> UpdateEntityAsync(
        string entityId, GraphEntityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(request);
        return await _client.GraphRagUpdateEntityAsync(entityId, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteEntityAsync(
        string entityId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityId);
        await _client.GraphRagDeleteEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
    }

    // ── Relationship CRUD ──

    /// <inheritdoc />
    public async Task<GraphRelationshipResult> CreateRelationshipAsync(
        GraphRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _client.GraphRagCreateRelationshipAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRelationshipListResult> ListRelationshipsAsync(
        int skip = 0, int take = 50, string? entityId = null, string? type = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.GraphRagListRelationshipsAsync(skip, take, entityId, type, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRelationshipResult> GetRelationshipAsync(
        string relationshipId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationshipId);
        return await _client.GraphRagGetRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphRelationshipResult> UpdateRelationshipAsync(
        string relationshipId, GraphRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationshipId);
        ArgumentNullException.ThrowIfNull(request);
        return await _client.GraphRagUpdateRelationshipAsync(relationshipId, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteRelationshipAsync(
        string relationshipId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationshipId);
        await _client.GraphRagDeleteRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
    }
}
