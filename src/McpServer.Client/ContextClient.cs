using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for context search endpoints (<c>/mcpserver/context</c>). Provides hybrid
/// semantic + full-text search over indexed workspace content, deterministic context packs,
/// index rebuilds, and source listing.
/// </summary>
/// <seealso cref="McpServerClient.Context"/>
public sealed class ContextClient : McpClientBase
{
    /// <inheritdoc />
    public ContextClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal ContextClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Perform a hybrid semantic + full-text search over indexed workspace content.</summary>
    public async Task<ContextSearchResult> SearchAsync(
        string query, string? sourceType = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var request = new ContextSearchRequest { Query = query, SourceType = sourceType, Limit = limit };
        return await PostAsync<ContextSearchResult>("mcpserver/context/search", request, cancellationToken);
    }

    /// <summary>Trigger a full index rebuild.</summary>
    public async Task<RebuildIndexResult> RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<RebuildIndexResult>("mcpserver/context/rebuild-index", null, cancellationToken);
    }

    /// <summary>Get a deterministic context pack for a query.</summary>
    public async Task<ContextPack> PackAsync(
        string query, string? queryId = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var request = new ContextPackRequest { Query = query, QueryId = queryId, Limit = limit };
        return await PostAsync<ContextPack>("mcpserver/context/pack", request, cancellationToken);
    }

    /// <summary>List all indexed document sources.</summary>
    public async Task<ContextSourcesResult> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ContextSourcesResult>("mcpserver/context/sources", cancellationToken);
    }

    /// <summary>Ingest context directly from a website URL without staging files first.</summary>
    public async Task<WebsiteIngestResult> IngestWebsiteAsync(
        string url,
        bool includeSubpages = false,
        int maxPages = 20,
        int maxDepth = 1,
        int maxBytesPerPage = 262_144,
        bool forceRefresh = false,
        bool triggerGraphRagIndex = false,
        CancellationToken cancellationToken = default)
    {
        var request = new WebsiteIngestRequest
        {
            Url = url,
            IncludeSubpages = includeSubpages,
            MaxPages = maxPages,
            MaxDepth = maxDepth,
            MaxBytesPerPage = maxBytesPerPage,
            ForceRefresh = forceRefresh,
            TriggerGraphRagIndex = triggerGraphRagIndex
        };
        return await PostAsync<WebsiteIngestResult>("mcpserver/context/ingest-website", request, cancellationToken);
    }

    /// <summary>Get GraphRAG status for the active workspace.</summary>
    public async Task<GraphRagStatusResult> GraphRagStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphRagStatusResult>("mcpserver/graphrag/status", cancellationToken);
    }

    /// <summary>Trigger GraphRAG indexing for the active workspace.</summary>
    public async Task<GraphRagStatusResult> GraphRagIndexAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphRagStatusResult>("mcpserver/graphrag/index", new GraphRagIndexRequest { Force = force }, cancellationToken);
    }

    /// <summary>Run a GraphRAG query for the active workspace.</summary>
    public async Task<GraphRagQueryResult> GraphRagQueryAsync(
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
        var request = new GraphRagQueryRequest
        {
            Query = query,
            Mode = mode,
            MaxChunks = maxChunks,
            IncludeContextChunks = includeContextChunks,
            MaxEntities = maxEntities,
            MaxRelationships = maxRelationships,
            CommunityDepth = communityDepth,
            ResponseTokenBudget = responseTokenBudget
        };
        return await PostAsync<GraphRagQueryResult>("mcpserver/graphrag/query", request, cancellationToken);
    }

    // ── Ad-Hoc Text Ingestion (FR-MCP-078, TR-GRAPHRAG-ADHOC-001) ──

    /// <summary>Ingest raw text into the GraphRAG corpus.</summary>
    public async Task<GraphRagIngestTextResult> GraphRagIngestTextAsync(
        GraphRagIngestTextRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphRagIngestTextResult>("mcpserver/graphrag/documents/ingest", request, cancellationToken);
    }

    // ── Document Management (FR-MCP-080, TR-GRAPHRAG-ADHOC-003) ──

    /// <summary>List documents in the GraphRAG corpus with pagination.</summary>
    public async Task<GraphRagDocumentListResult> GraphRagListDocumentsAsync(
        int skip = 0, int take = 50, string? sourceType = null, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/graphrag/documents?skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(sourceType))
            path += $"&sourceType={Uri.EscapeDataString(sourceType)}";
        return await GetAsync<GraphRagDocumentListResult>(path, cancellationToken);
    }

    /// <summary>Retrieve all chunks for a specific document.</summary>
    public async Task<GraphRagDocumentChunksResult> GraphRagGetDocumentChunksAsync(
        string documentId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphRagDocumentChunksResult>(
            $"mcpserver/graphrag/documents/{Uri.EscapeDataString(documentId)}/chunks", cancellationToken);
    }

    /// <summary>Delete a document and its chunks from the corpus.</summary>
    public async Task<GraphRagDocumentDeleteResult> GraphRagDeleteDocumentAsync(
        string documentId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<GraphRagDocumentDeleteResult>(
            $"mcpserver/graphrag/documents/{Uri.EscapeDataString(documentId)}", cancellationToken);
    }

    // ── Entity CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>Create a new graph entity.</summary>
    public async Task<GraphEntityResult> GraphRagCreateEntityAsync(
        GraphEntityRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphEntityResult>("mcpserver/graphrag/entities", request, cancellationToken);
    }

    /// <summary>List graph entities with pagination and optional type filter.</summary>
    public async Task<GraphEntityListResult> GraphRagListEntitiesAsync(
        int skip = 0, int take = 50, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/graphrag/entities?skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(entityType))
            path += $"&entityType={Uri.EscapeDataString(entityType)}";
        return await GetAsync<GraphEntityListResult>(path, cancellationToken);
    }

    /// <summary>Retrieve a graph entity by ID.</summary>
    public async Task<GraphEntityResult> GraphRagGetEntityAsync(
        string entityId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphEntityResult>(
            $"mcpserver/graphrag/entities/{Uri.EscapeDataString(entityId)}", cancellationToken);
    }

    /// <summary>Update an existing graph entity.</summary>
    public async Task<GraphEntityResult> GraphRagUpdateEntityAsync(
        string entityId, GraphEntityRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<GraphEntityResult>(
            $"mcpserver/graphrag/entities/{Uri.EscapeDataString(entityId)}", request, cancellationToken);
    }

    /// <summary>Delete a graph entity by ID.</summary>
    public async Task GraphRagDeleteEntityAsync(
        string entityId, CancellationToken cancellationToken = default)
    {
        await SendForStatusAsync(HttpMethod.Delete,
            $"mcpserver/graphrag/entities/{Uri.EscapeDataString(entityId)}", null, cancellationToken);
    }

    // ── Relationship CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>Create a new graph relationship.</summary>
    public async Task<GraphRelationshipResult> GraphRagCreateRelationshipAsync(
        GraphRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphRelationshipResult>("mcpserver/graphrag/relationships", request, cancellationToken);
    }

    /// <summary>List graph relationships with pagination and optional filters.</summary>
    public async Task<GraphRelationshipListResult> GraphRagListRelationshipsAsync(
        int skip = 0, int take = 50, string? entityId = null, string? type = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/graphrag/relationships?skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(entityId))
            path += $"&entityId={Uri.EscapeDataString(entityId)}";
        if (!string.IsNullOrWhiteSpace(type))
            path += $"&type={Uri.EscapeDataString(type)}";
        return await GetAsync<GraphRelationshipListResult>(path, cancellationToken);
    }

    /// <summary>Retrieve a graph relationship by ID.</summary>
    public async Task<GraphRelationshipResult> GraphRagGetRelationshipAsync(
        string relationshipId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphRelationshipResult>(
            $"mcpserver/graphrag/relationships/{Uri.EscapeDataString(relationshipId)}", cancellationToken);
    }

    /// <summary>Update an existing graph relationship.</summary>
    public async Task<GraphRelationshipResult> GraphRagUpdateRelationshipAsync(
        string relationshipId, GraphRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<GraphRelationshipResult>(
            $"mcpserver/graphrag/relationships/{Uri.EscapeDataString(relationshipId)}", request, cancellationToken);
    }

    /// <summary>Delete a graph relationship by ID.</summary>
    public async Task GraphRagDeleteRelationshipAsync(
        string relationshipId, CancellationToken cancellationToken = default)
    {
        await SendForStatusAsync(HttpMethod.Delete,
            $"mcpserver/graphrag/relationships/{Uri.EscapeDataString(relationshipId)}", null, cancellationToken);
    }
}

/// <summary>Result of a <see cref="ContextClient.RebuildIndexAsync"/> operation.</summary>
public sealed class RebuildIndexResult
{
    /// <summary>Human-readable operation status (e.g. <c>"completed"</c>).</summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>Result of <see cref="ContextClient.ListSourcesAsync"/> containing all indexed document sources.</summary>
public sealed class ContextSourcesResult
{
    /// <summary>Collection of indexed sources with their keys, types, and ingestion timestamps.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("sources")]
    public IReadOnlyList<ContextSource> Sources { get; set; } = [];
}
