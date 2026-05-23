using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Workspace-scoped GraphRAG orchestration service.
/// FR-MCP-078/079/080, TR-GRAPHRAG-ADHOC-001/002/003: Provides lifecycle,
/// ad-hoc text ingestion, document management, entity CRUD, and relationship CRUD.
/// </summary>
public interface IGraphRagService
{
    /// <summary>Gets the current GraphRAG status for the workspace.</summary>
    Task<GraphRagStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Initializes the GraphRAG directory structure for the workspace.</summary>
    Task<GraphRagStatusResponse> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Indexes the GraphRAG corpus for the workspace.</summary>
    Task<GraphRagStatusResponse> IndexAsync(GraphRagIndexRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>Queries the GraphRAG corpus.</summary>
    Task<GraphRagQueryResponse> QueryAsync(GraphRagQueryRequest request, CancellationToken cancellationToken = default);

    // ── Ad-Hoc Text Ingestion (FR-MCP-078, TR-GRAPHRAG-ADHOC-001) ──

    /// <summary>FR-MCP-078, TR-GRAPHRAG-ADHOC-001: Ingests raw text into the GraphRAG corpus, creating a document and chunks.</summary>
    Task<GraphRagIngestTextResponse> IngestTextAsync(GraphRagIngestTextRequest request, CancellationToken ct = default);

    // ── Document Management (FR-MCP-080, TR-GRAPHRAG-ADHOC-003) ──

    /// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Lists documents in the GraphRAG corpus with pagination and optional source type filter.</summary>
    Task<GraphRagDocumentListResponse> ListDocumentsAsync(int skip = 0, int take = 50, string? sourceType = null, CancellationToken ct = default);

    /// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Retrieves all chunks for a specific document, or null if the document does not exist.</summary>
    Task<GraphRagDocumentChunksResponse?> GetDocumentChunksAsync(string documentId, CancellationToken ct = default);

    /// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Deletes a document and its chunks from the corpus and vector index.</summary>
    Task<GraphRagDocumentDeleteResponse> DeleteDocumentAsync(string documentId, CancellationToken ct = default);

    // ── Entity CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Creates a new graph entity.</summary>
    Task<GraphEntityResponse> CreateEntityAsync(GraphEntityRequest request, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Retrieves a graph entity by ID, or null if not found.</summary>
    Task<GraphEntityResponse?> GetEntityAsync(string entityId, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Updates an existing graph entity, returning null if not found.</summary>
    Task<GraphEntityResponse?> UpdateEntityAsync(string entityId, GraphEntityRequest request, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Lists graph entities with pagination and optional entity type filter.</summary>
    Task<GraphEntityListResponse> ListEntitiesAsync(int skip = 0, int take = 50, string? entityType = null, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Deletes a graph entity by ID. Returns true if found and removed.</summary>
    Task<bool> DeleteEntityAsync(string entityId, CancellationToken ct = default);

    // ── Relationship CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Creates a new graph relationship between two entities.</summary>
    Task<GraphRelationshipResponse> CreateRelationshipAsync(GraphRelationshipRequest request, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Retrieves a graph relationship by ID, or null if not found.</summary>
    Task<GraphRelationshipResponse?> GetRelationshipAsync(string relationshipId, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Updates an existing graph relationship, returning null if not found.</summary>
    Task<GraphRelationshipResponse?> UpdateRelationshipAsync(string relationshipId, GraphRelationshipRequest request, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Lists graph relationships with pagination and optional filters.</summary>
    Task<GraphRelationshipListResponse> ListRelationshipsAsync(int skip = 0, int take = 50, string? entityId = null, string? relationshipType = null, CancellationToken ct = default);

    /// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Deletes a graph relationship by ID. Returns true if found and removed.</summary>
    Task<bool> DeleteRelationshipAsync(string relationshipId, CancellationToken ct = default);
}
