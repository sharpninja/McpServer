// TR-MCP-REPL-005 / Phase 1d: Context/GraphRAG MCP tools partial of FwhMcpTools.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>Search indexed context chunks by query text.</summary>
    /// <returns>JSON string with matching chunks and source keys.</returns>
    [McpServerTool(Name = "context_search"), Description("Search indexed context chunks by query text. Optional sourceType filter and limit (1-100).")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ContextSearch(
        [Description("Search query text")] string query,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Max chunks to return (default 20)")] int limit = 20,
        [Description("Optional source type filter")] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var q = (query ?? string.Empty).Trim();
        var lim = Math.Clamp(limit, 1, 100);
        var productChunks = await LoadProductRequirementChunksAsync(workspacePath, q, sourceType, cancellationToken)
            .ConfigureAwait(false);

        if (string.Equals(sourceType, "product-requirements", StringComparison.OrdinalIgnoreCase))
        {
            return JsonSerializer.Serialize(new
            {
                query = q,
                chunks = productChunks.Take(lim).ToList(),
                sourceKeys = productChunks.Select(c => c.DocumentId).Distinct().ToList(),
            });
        }

        var result = await _searchService.SearchAsync(q, lim, sourceType, cancellationToken).ConfigureAwait(false);
        var chunks = productChunks.Concat(result.Chunks.Select(c => new ContextChunk
        {
            Id = c.ChunkId,
            DocumentId = c.DocumentId,
            Content = c.Content,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        })).Take(lim).ToList();
        var sourceKeys = productChunks.Select(c => c.DocumentId).Concat(result.SourceKeys).Distinct().ToList();
        return JsonSerializer.Serialize(new { query = q, chunks, sourceKeys });
    }

    /// <summary>Get a deterministic context pack by query.</summary>
    /// <returns>JSON string with ordered context pack.</returns>
    [McpServerTool(Name = "context_pack"), Description("Get a deterministic context pack by query. Optional queryId and limit (1-100).")]
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public async Task<string> ContextPack(
        [Description("Search query text")] string query,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Max chunks in pack (default 20)")] int limit = 20,
        [Description("Optional query id for reproducibility")] string? queryId = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var q = (query ?? string.Empty).Trim();
        var lim = Math.Clamp(limit, 1, 100);
        var qid = queryId ?? Guid.NewGuid().ToString("N");
        var productChunks = await LoadProductRequirementChunksAsync(workspacePath, q, "product-requirements", cancellationToken)
            .ConfigureAwait(false);
        var remaining = Math.Max(0, lim - productChunks.Count);
        var chunksQuery = _db.Chunks.AsNoTracking();
        if (!string.IsNullOrEmpty(q))
            chunksQuery = chunksQuery.Where(c => c.Content != null && c.Content.Contains(q));
        var chunkEntities = await chunksQuery
            .OrderBy(c => c.DocumentId)
            .ThenBy(c => c.ChunkIndex)
            .Take(remaining)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var chunks = productChunks.Concat(chunkEntities.Select(c => new ContextChunk
        {
            Id = c.Id,
            DocumentId = c.DocumentId,
            Content = c.Content ?? string.Empty,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        })).Take(lim).ToList();
        var docIds = chunkEntities.Select(c => c.DocumentId).Distinct().ToList();
        var sourceKeys = await _db.Documents.Where(d => docIds.Contains(d.Id)).Select(d => d.SourceKey).ToListAsync(cancellationToken).ConfigureAwait(false);
        sourceKeys = productChunks.Select(c => c.DocumentId).Concat(sourceKeys).Distinct().ToList();
        var pack = new ContextPack
        {
            QueryId = qid,
            Chunks = chunks,
            SourceKeys = sourceKeys
        };
        return JsonSerializer.Serialize(pack);
    }

    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    private async Task<List<ContextChunk>> LoadProductRequirementChunksAsync(
        string workspacePath,
        string query,
        string? sourceType,
        CancellationToken cancellationToken)
    {
        if (_dispatcher is null)
            return [];

        var result = await _dispatcher.QueryAsync(
            new GetProductRequirementContextQuery(workspacePath, query, sourceType),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure || result.Value is null)
            return [];

        return result.Value.Select(c => new ContextChunk
        {
            Id = $"product-req:{c.OriginWorkspaceId}:{c.RequirementId}",
            DocumentId = $"product-requirements:{c.OriginWorkspaceId}:{c.RequirementId}",
            Content = c.Content,
            TokenCount = Math.Max(1, c.Content.Length / 4),
            ChunkIndex = 0,
        }).ToList();
    }

    /// <summary>List indexed document sources.</summary>
    /// <returns>JSON string with source keys, types, and ingestion timestamps.</returns>
    [McpServerTool(Name = "context_sources"), Description("List indexed document sources (sourceKey, sourceType, ingestedAt).")]
    public async Task<string> ContextSources(
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var sources = await _db.Documents.AsNoTracking()
            .Select(d => new { d.SourceKey, d.SourceType, d.IngestedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { sources });
    }

    /// <summary>FR-MCP-065, TR-MCP-INGEST-003: Ingest context directly from a website URL.</summary>
    [McpServerTool(Name = "context_ingest_website"), Description("Ingest context directly from a website URL with bounded crawl controls.")]
    public async Task<string> ContextIngestWebsite(
        [Description("Website URL to ingest")] string url,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Crawl same-host subpages")] bool includeSubpages = false,
        [Description("Maximum pages to fetch (default 20)")] int maxPages = 20,
        [Description("Maximum crawl depth when subpages are enabled (default 1)")] int maxDepth = 1,
        [Description("Maximum bytes downloaded per page (default 262144)")] int maxBytesPerPage = 262144,
        [Description("Force refresh semantics for existing documents")] bool forceRefresh = false,
        [Description("Trigger GraphRAG index after ingest")] bool triggerGraphRagIndex = false,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        if (ShouldDeferContextMutation(out var transactionError))
            return JsonSerializer.Serialize(new { error = transactionError, code = "turn_transaction_gate" });

        var result = await _coordinator.IngestWebsiteAsync(new WebsiteIngestRequest
        {
            Url = url,
            IncludeSubpages = includeSubpages,
            MaxPages = maxPages,
            MaxDepth = maxDepth,
            MaxBytesPerPage = maxBytesPerPage,
            ForceRefresh = forceRefresh,
            TriggerGraphRagIndex = triggerGraphRagIndex,
        }, cancellationToken).ConfigureAwait(false);

        if (triggerGraphRagIndex)
        {
            try
            {
                await _graphRagService.IndexAsync(new GraphRagIndexRequest { Force = forceRefresh }, cancellationToken).ConfigureAwait(false);
                result.GraphRagIndexed = true;
            }
            catch (Exception ex)
            {
                result.GraphRagIndexed = false;
                result.GraphRagIndexError = ex.Message;
            }
        }

        return JsonSerializer.Serialize(result);
    }

    /// <summary>Get GraphRAG readiness status for the workspace.</summary>
    [McpServerTool(Name = "graphrag_status"), Description("Get GraphRAG status for the workspace or global scope (initialized, indexed, backend, last index time).")]
    public async Task<string> GraphRagStatus(
        [Description("Workspace path (required for workspace scope)")] string workspacePath,
        [Description("Storage scope: workspace or global")] string scope = "workspace",
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var storageScope = ParseGraphRagStorageScope(scope);
        var status = await _graphRagService.GetStatusAsync(storageScope, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(status);
    }

    /// <summary>Trigger GraphRAG indexing for the workspace.</summary>
    [McpServerTool(Name = "graphrag_index"), Description("Initialize or rebuild GraphRAG index for the workspace or global scope.")]
    public async Task<string> GraphRagIndex(
        [Description("Workspace path (required for workspace scope)")] string workspacePath,
        [Description("Force re-index if true")] bool force = false,
        [Description("Storage scope: workspace or global")] string scope = "workspace",
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var storageScope = ParseGraphRagStorageScope(scope);
        var statusBefore = await _graphRagService.GetStatusAsync(storageScope, cancellationToken).ConfigureAwait(false);
        if (!force && string.Equals(statusBefore.State, "indexing", StringComparison.OrdinalIgnoreCase))
            return JsonSerializer.Serialize(new { error = "GraphRAG index already in progress for this workspace.", code = "index_conflict" });
        try
        {
            var status = await _graphRagService.IndexAsync(
                new GraphRagIndexRequest { Force = force, Scope = storageScope },
                cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(status);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, code = "index_conflict" });
        }
    }

    /// <summary>Run a GraphRAG query with citations and optional context chunks.</summary>
    [McpServerTool(Name = "graphrag_query"), Description("Run a GraphRAG query for the workspace or global scope and return answer, citations, and optional context chunks.")]
    public async Task<string> GraphRagQuery(
        [Description("Query text")] string query,
        [Description("Workspace path (required for workspace scope)")] string workspacePath,
        [Description("Storage scope: workspace or global")] string scope = "workspace",
        [Description("Query mode (local/global/drift), optional")] string? mode = null,
        [Description("Maximum context chunks to return (default 20)")] int maxChunks = 20,
        [Description("Include context chunks in response (default true)")] bool includeContextChunks = true,
        [Description("Maximum entities to include (default service setting)")] int? maxEntities = null,
        [Description("Maximum relationships to include (default service setting)")] int? maxRelationships = null,
        [Description("Community depth for graph summarization (default service setting)")] int? communityDepth = null,
        [Description("Response token budget hint (optional)")] int? responseTokenBudget = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var storageScope = ParseGraphRagStorageScope(scope);
        var result = await _graphRagService.QueryAsync(new GraphRagQueryRequest
        {
            Scope = storageScope,
            Query = query,
            Mode = mode,
            MaxChunks = maxChunks,
            IncludeContextChunks = includeContextChunks,
            MaxEntities = maxEntities,
            MaxRelationships = maxRelationships,
            CommunityDepth = communityDepth,
            ResponseTokenBudget = responseTokenBudget,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    private static GraphRagStorageScope ParseGraphRagStorageScope(string? scope)
        => string.Equals(scope, "global", StringComparison.OrdinalIgnoreCase)
            ? GraphRagStorageScope.Global
            : GraphRagStorageScope.Workspace;

    // ── Ad-Hoc Text Ingestion (FR-MCP-078, TR-GRAPHRAG-ADHOC-001) ──

    /// <summary>FR-MCP-078: Ingest raw text into the GraphRAG corpus.</summary>
    /// <returns>JSON string with document ID, chunk count, and source metadata.</returns>
    [McpServerTool(Name = "graphrag_ingest_text"), Description("Ingest raw text into the GraphRAG corpus, creating a document and chunks.")]
    public async Task<string> GraphRagIngestText(
        [Description("Text content to ingest")] string content,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional document title")] string? title = null,
        [Description("Source type classification (default 'adhoc-text')")] string? sourceType = null,
        [Description("Source key / path for the document")] string? sourceKey = null,
        [Description("Trigger full reindex after ingestion")] bool triggerReindex = false,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        if (string.IsNullOrWhiteSpace(content))
            return JsonSerializer.Serialize(new { error = "content is required" });
        var result = await _graphRagService.IngestTextAsync(new GraphRagIngestTextRequest
        {
            Content = content,
            Title = title,
            SourceType = sourceType,
            SourceKey = sourceKey,
            TriggerReindex = triggerReindex,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-080: List documents in the GraphRAG corpus.</summary>
    /// <returns>JSON string with paginated document list.</returns>
    [McpServerTool(Name = "graphrag_list_documents"), Description("List documents in the GraphRAG corpus with pagination and optional source type filter.")]
    public async Task<string> GraphRagListDocuments(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Number of documents to skip (default 0)")] int skip = 0,
        [Description("Maximum documents to return (default 50)")] int take = 50,
        [Description("Optional source type filter")] string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.ListDocumentsAsync(skip, take, sourceType, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-080: Get all chunks for a specific document.</summary>
    /// <returns>JSON string with document chunks or error.</returns>
    [McpServerTool(Name = "graphrag_get_document_chunks"), Description("Retrieve all chunks for a specific GraphRAG document by document ID.")]
    public async Task<string> GraphRagGetDocumentChunks(
        [Description("Document ID")] string documentId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.GetDocumentChunksAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Document '{documentId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-080: Delete a document and its chunks from the corpus.</summary>
    /// <returns>JSON string with deletion result.</returns>
    [McpServerTool(Name = "graphrag_delete_document"), Description("Delete a document and its chunks from the GraphRAG corpus.")]
    public async Task<string> GraphRagDeleteDocument(
        [Description("Document ID to delete")] string documentId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.DeleteDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    // ── Entity CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079: Create a new graph entity.</summary>
    /// <returns>JSON string with the created entity.</returns>
    [McpServerTool(Name = "graphrag_create_entity"), Description("Create a new graph entity in the GraphRAG knowledge graph.")]
    public async Task<string> GraphRagCreateEntity(
        [Description("Entity display name")] string name,
        [Description("Entity type (e.g. person, organization, concept)")] string entityType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional description")] string? description = null,
        [Description("Optional JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.CreateEntityAsync(new GraphEntityRequest
        {
            Name = name,
            EntityType = entityType,
            Description = description,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: List graph entities with pagination.</summary>
    /// <returns>JSON string with paginated entity list.</returns>
    [McpServerTool(Name = "graphrag_list_entities"), Description("List graph entities with pagination and optional type filter.")]
    public async Task<string> GraphRagListEntities(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Number of entities to skip (default 0)")] int skip = 0,
        [Description("Maximum entities to return (default 50)")] int take = 50,
        [Description("Optional entity type filter")] string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.ListEntitiesAsync(skip, take, entityType, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Get a graph entity by ID.</summary>
    /// <returns>JSON string with entity or error.</returns>
    [McpServerTool(Name = "graphrag_get_entity"), Description("Retrieve a graph entity by its ID.")]
    public async Task<string> GraphRagGetEntity(
        [Description("Entity ID")] string entityId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.GetEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Entity '{entityId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Update an existing graph entity.</summary>
    /// <returns>JSON string with updated entity or error.</returns>
    [McpServerTool(Name = "graphrag_update_entity"), Description("Update an existing graph entity by ID.")]
    public async Task<string> GraphRagUpdateEntity(
        [Description("Entity ID to update")] string entityId,
        [Description("Updated entity name")] string name,
        [Description("Updated entity type")] string entityType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated description")] string? description = null,
        [Description("Updated JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.UpdateEntityAsync(entityId, new GraphEntityRequest
        {
            Name = name,
            EntityType = entityType,
            Description = description,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Entity '{entityId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Delete a graph entity by ID.</summary>
    /// <returns>JSON string indicating deletion success.</returns>
    [McpServerTool(Name = "graphrag_delete_entity"), Description("Delete a graph entity by its ID.")]
    public async Task<string> GraphRagDeleteEntity(
        [Description("Entity ID to delete")] string entityId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var deleted = await _graphRagService.DeleteEntityAsync(entityId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { deleted, entityId });
    }

    // ── Relationship CRUD (FR-MCP-079, TR-GRAPHRAG-ADHOC-002) ──

    /// <summary>FR-MCP-079: Create a new graph relationship.</summary>
    /// <returns>JSON string with the created relationship.</returns>
    [McpServerTool(Name = "graphrag_create_relationship"), Description("Create a new graph relationship between two entities.")]
    public async Task<string> GraphRagCreateRelationship(
        [Description("Source entity ID")] string sourceEntityId,
        [Description("Target entity ID")] string targetEntityId,
        [Description("Relationship type")] string relationshipType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional description")] string? description = null,
        [Description("Weight/strength (default 1.0)")] double weight = 1.0,
        [Description("Optional JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.CreateRelationshipAsync(new GraphRelationshipRequest
        {
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipType = relationshipType,
            Description = description,
            Weight = weight,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: List graph relationships with pagination.</summary>
    /// <returns>JSON string with paginated relationship list.</returns>
    [McpServerTool(Name = "graphrag_list_relationships"), Description("List graph relationships with pagination and optional entity/type filters.")]
    public async Task<string> GraphRagListRelationships(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Number of relationships to skip (default 0)")] int skip = 0,
        [Description("Maximum relationships to return (default 50)")] int take = 50,
        [Description("Optional entity ID filter")] string? entityId = null,
        [Description("Optional relationship type filter")] string? relationshipType = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.ListRelationshipsAsync(skip, take, entityId, relationshipType, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Get a graph relationship by ID.</summary>
    /// <returns>JSON string with relationship or error.</returns>
    [McpServerTool(Name = "graphrag_get_relationship"), Description("Retrieve a graph relationship by its ID.")]
    public async Task<string> GraphRagGetRelationship(
        [Description("Relationship ID")] string relationshipId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.GetRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Relationship '{relationshipId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Update an existing graph relationship.</summary>
    /// <returns>JSON string with updated relationship or error.</returns>
    [McpServerTool(Name = "graphrag_update_relationship"), Description("Update an existing graph relationship by ID.")]
    public async Task<string> GraphRagUpdateRelationship(
        [Description("Relationship ID to update")] string relationshipId,
        [Description("Source entity ID")] string sourceEntityId,
        [Description("Target entity ID")] string targetEntityId,
        [Description("Relationship type")] string relationshipType,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Updated description")] string? description = null,
        [Description("Updated weight (default 1.0)")] double weight = 1.0,
        [Description("Updated JSON metadata")] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _graphRagService.UpdateRelationshipAsync(relationshipId, new GraphRelationshipRequest
        {
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            RelationshipType = relationshipType,
            Description = description,
            Weight = weight,
            Metadata = metadata,
        }, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return JsonSerializer.Serialize(new { error = $"Relationship '{relationshipId}' not found" });
        return JsonSerializer.Serialize(result);
    }

    /// <summary>FR-MCP-079: Delete a graph relationship by ID.</summary>
    /// <returns>JSON string indicating deletion success.</returns>
    [McpServerTool(Name = "graphrag_delete_relationship"), Description("Delete a graph relationship by its ID.")]
    public async Task<string> GraphRagDeleteRelationship(
        [Description("Relationship ID to delete")] string relationshipId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var deleted = await _graphRagService.DeleteRelationshipAsync(relationshipId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(new { deleted, relationshipId });
    }
}
