// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - GraphRAG workflow interface
// FR-MCP-REPL-003: Command Namespace Parity - GraphRAG operation contracts
// FR-MCP-078: Ad-hoc text ingestion workflow contract
// FR-MCP-079: Entity and relationship CRUD workflow contract
// FR-MCP-080: Document management workflow contract
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - GraphRAG workflow contract
// TR-GRAPHRAG-ADHOC-001: Ad-hoc text ingestion interface
// TR-GRAPHRAG-ADHOC-002: Entity and relationship CRUD interface
// TR-GRAPHRAG-ADHOC-003: Document management interface

using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Defines the canonical GraphRAG workflow operations for ad-hoc graph management.
/// Covers lifecycle operations (status, index, query), ad-hoc text ingestion,
/// document management, entity CRUD, and relationship CRUD.
/// </summary>
/// <remarks>
/// <para>All operations delegate to <see cref="McpServer.Client.ContextClient"/> methods.</para>
/// <para>
/// <strong>Operation groups:</strong>
/// <list type="bullet">
/// <item><term>Lifecycle</term><description>Status, index, query</description></item>
/// <item><term>Ingestion</term><description>Raw text ingestion into the corpus</description></item>
/// <item><term>Documents</term><description>List, get chunks, delete documents</description></item>
/// <item><term>Entities</term><description>Create, list, get, update, delete graph entities</description></item>
/// <item><term>Relationships</term><description>Create, list, get, update, delete graph relationships</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IGraphRagWorkflow
{
    // ── Lifecycle ──

    /// <summary>
    /// Gets the current GraphRAG status for the active workspace.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The GraphRAG status result.</returns>
    Task<GraphRagStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers a GraphRAG index operation for the active workspace.
    /// </summary>
    /// <param name="force">When true, forces re-indexing even if data has not changed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The GraphRAG status after indexing.</returns>
    Task<GraphRagStatusResult> IndexAsync(bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a GraphRAG query for the active workspace.
    /// </summary>
    /// <param name="query">The query text.</param>
    /// <param name="mode">Optional query mode (e.g. "local", "global").</param>
    /// <param name="maxChunks">Maximum context chunks to return.</param>
    /// <param name="includeContextChunks">Whether to include context chunks in the response.</param>
    /// <param name="maxEntities">Maximum entities to return.</param>
    /// <param name="maxRelationships">Maximum relationships to return.</param>
    /// <param name="communityDepth">Community traversal depth.</param>
    /// <param name="responseTokenBudget">Token budget for the response.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The query result with answer, citations, and graph data.</returns>
    Task<GraphRagQueryResult> QueryAsync(
        string query,
        string? mode = null,
        int? maxChunks = null,
        bool includeContextChunks = true,
        int? maxEntities = null,
        int? maxRelationships = null,
        int? communityDepth = null,
        int? responseTokenBudget = null,
        CancellationToken cancellationToken = default);

    // ── Ingestion ──

    /// <summary>
    /// Ingests raw text into the GraphRAG corpus.
    /// </summary>
    /// <param name="request">The ingestion request with content and metadata.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The ingestion result with document ID and chunk statistics.</returns>
    Task<GraphRagIngestTextResult> IngestTextAsync(
        GraphRagIngestTextRequest request, CancellationToken cancellationToken = default);

    // ── Document Management ──

    /// <summary>
    /// Lists documents in the GraphRAG corpus with pagination.
    /// </summary>
    /// <param name="skip">Number of documents to skip (for pagination).</param>
    /// <param name="take">Number of documents to return (for pagination).</param>
    /// <param name="sourceType">Optional source type filter.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of document summaries.</returns>
    Task<GraphRagDocumentListResult> ListDocumentsAsync(
        int skip = 0, int take = 50, string? sourceType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all chunks for a specific document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The document's chunks.</returns>
    Task<GraphRagDocumentChunksResult> GetDocumentChunksAsync(
        string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document and its chunks from the corpus.
    /// </summary>
    /// <param name="documentId">The document identifier to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The deletion result with chunk removal statistics.</returns>
    Task<GraphRagDocumentDeleteResult> DeleteDocumentAsync(
        string documentId, CancellationToken cancellationToken = default);

    // ── Entity CRUD ──

    /// <summary>
    /// Creates a new graph entity.
    /// </summary>
    /// <param name="request">The entity creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created entity.</returns>
    Task<GraphEntityResult> CreateEntityAsync(
        GraphEntityRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists graph entities with pagination and optional type filter.
    /// </summary>
    /// <param name="skip">Number of entities to skip.</param>
    /// <param name="take">Number of entities to return.</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of entities.</returns>
    Task<GraphEntityListResult> ListEntitiesAsync(
        int skip = 0, int take = 50, string? entityType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a graph entity by ID.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity details.</returns>
    Task<GraphEntityResult> GetEntityAsync(
        string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing graph entity.
    /// </summary>
    /// <param name="entityId">The entity identifier to update.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<GraphEntityResult> UpdateEntityAsync(
        string entityId, GraphEntityRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a graph entity by ID.
    /// </summary>
    /// <param name="entityId">The entity identifier to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    Task DeleteEntityAsync(
        string entityId, CancellationToken cancellationToken = default);

    // ── Relationship CRUD ──

    /// <summary>
    /// Creates a new graph relationship.
    /// </summary>
    /// <param name="request">The relationship creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created relationship.</returns>
    Task<GraphRelationshipResult> CreateRelationshipAsync(
        GraphRelationshipRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists graph relationships with pagination and optional filters.
    /// </summary>
    /// <param name="skip">Number of relationships to skip.</param>
    /// <param name="take">Number of relationships to return.</param>
    /// <param name="entityId">Optional entity ID filter (returns relationships involving this entity).</param>
    /// <param name="type">Optional relationship type filter.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of relationships.</returns>
    Task<GraphRelationshipListResult> ListRelationshipsAsync(
        int skip = 0, int take = 50, string? entityId = null, string? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a graph relationship by ID.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The relationship details.</returns>
    Task<GraphRelationshipResult> GetRelationshipAsync(
        string relationshipId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing graph relationship.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier to update.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated relationship.</returns>
    Task<GraphRelationshipResult> UpdateRelationshipAsync(
        string relationshipId, GraphRelationshipRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a graph relationship by ID.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    Task DeleteRelationshipAsync(
        string relationshipId, CancellationToken cancellationToken = default);
}
