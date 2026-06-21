using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for GraphRAG endpoints (<c>/mcpserver/graphrag</c>). Provides typed access to
/// lifecycle operations, ad-hoc text ingestion, document management, entity CRUD, and
/// relationship CRUD.
/// </summary>
/// <seealso cref="McpServerClient.GraphRag"/>
public sealed class GraphRagClient : McpClientBase
{
    /// <inheritdoc />
    public GraphRagClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal GraphRagClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Gets GraphRAG status for the active workspace.</summary>
    public async Task<GraphRagStatusResult> StatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphRagStatusResult>("mcpserver/graphrag/status", cancellationToken);
    }

    /// <summary>Triggers GraphRAG indexing for the active workspace.</summary>
    public async Task<GraphRagStatusResult> IndexAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphRagStatusResult>(
            "mcpserver/graphrag/index",
            new GraphRagIndexRequest { Force = force },
            cancellationToken);
    }

    /// <summary>Runs a GraphRAG query for the active workspace.</summary>
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
        var request = new GraphRagQueryRequest
        {
            Query = query,
            Mode = mode,
            MaxChunks = maxChunks,
            IncludeContextChunks = includeContextChunks,
            MaxEntities = maxEntities,
            MaxRelationships = maxRelationships,
            CommunityDepth = communityDepth,
            ResponseTokenBudget = responseTokenBudget,
        };

        return await PostAsync<GraphRagQueryResult>("mcpserver/graphrag/query", request, cancellationToken);
    }

    /// <summary>Ingests raw text into the GraphRAG corpus.</summary>
    public async Task<GraphRagIngestTextResult> IngestTextAsync(
        GraphRagIngestTextRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphRagIngestTextResult>(
            "mcpserver/graphrag/documents/ingest",
            request,
            cancellationToken);
    }

    /// <summary>Lists documents in the GraphRAG corpus with pagination.</summary>
    public async Task<GraphRagDocumentListResult> ListDocumentsAsync(
        int skip = 0,
        int take = 50,
        string? sourceType = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/graphrag/documents?skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(sourceType))
            path += $"&sourceType={Uri.EscapeDataString(sourceType)}";
        return await GetAsync<GraphRagDocumentListResult>(path, cancellationToken);
    }

    /// <summary>Gets all chunks for a specific GraphRAG document.</summary>
    public async Task<GraphRagDocumentChunksResult> GetDocumentChunksAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphRagDocumentChunksResult>(
            $"mcpserver/graphrag/documents/{Uri.EscapeDataString(documentId)}/chunks",
            cancellationToken);
    }

    /// <summary>Deletes a GraphRAG document and its chunks.</summary>
    public async Task<GraphRagDocumentDeleteResult> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default)
    {
        return await DeleteAsync<GraphRagDocumentDeleteResult>(
            $"mcpserver/graphrag/documents/{Uri.EscapeDataString(documentId)}",
            cancellationToken);
    }

    /// <summary>Creates a graph entity.</summary>
    public async Task<GraphEntityResult> CreateEntityAsync(
        GraphEntityRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphEntityResult>("mcpserver/graphrag/entities", request, cancellationToken);
    }

    /// <summary>Lists graph entities with pagination and an optional entity type filter.</summary>
    public async Task<GraphEntityListResult> ListEntitiesAsync(
        int skip = 0,
        int take = 50,
        string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/graphrag/entities?skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(entityType))
            path += $"&entityType={Uri.EscapeDataString(entityType)}";
        return await GetAsync<GraphEntityListResult>(path, cancellationToken);
    }

    /// <summary>Gets a graph entity by identifier.</summary>
    public async Task<GraphEntityResult> GetEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphEntityResult>(
            $"mcpserver/graphrag/entities/{Uri.EscapeDataString(entityId)}",
            cancellationToken);
    }

    /// <summary>Updates a graph entity by identifier.</summary>
    public async Task<GraphEntityResult> UpdateEntityAsync(
        string entityId,
        GraphEntityRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<GraphEntityResult>(
            $"mcpserver/graphrag/entities/{Uri.EscapeDataString(entityId)}",
            request,
            cancellationToken);
    }

    /// <summary>Deletes a graph entity by identifier.</summary>
    public async Task DeleteEntityAsync(string entityId, CancellationToken cancellationToken = default)
    {
        await SendForStatusAsync(
            HttpMethod.Delete,
            $"mcpserver/graphrag/entities/{Uri.EscapeDataString(entityId)}",
            null,
            cancellationToken);
    }

    /// <summary>Creates a graph relationship.</summary>
    public async Task<GraphRelationshipResult> CreateRelationshipAsync(
        GraphRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphRelationshipResult>("mcpserver/graphrag/relationships", request, cancellationToken);
    }

    /// <summary>Lists graph relationships with pagination and optional filters.</summary>
    public async Task<GraphRelationshipListResult> ListRelationshipsAsync(
        int skip = 0,
        int take = 50,
        string? entityId = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/graphrag/relationships?skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(entityId))
            path += $"&entityId={Uri.EscapeDataString(entityId)}";
        if (!string.IsNullOrWhiteSpace(type))
            path += $"&type={Uri.EscapeDataString(type)}";
        return await GetAsync<GraphRelationshipListResult>(path, cancellationToken);
    }

    /// <summary>Gets a graph relationship by identifier.</summary>
    public async Task<GraphRelationshipResult> GetRelationshipAsync(
        string relationshipId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphRelationshipResult>(
            $"mcpserver/graphrag/relationships/{Uri.EscapeDataString(relationshipId)}",
            cancellationToken);
    }

    /// <summary>Updates a graph relationship by identifier.</summary>
    public async Task<GraphRelationshipResult> UpdateRelationshipAsync(
        string relationshipId,
        GraphRelationshipRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<GraphRelationshipResult>(
            $"mcpserver/graphrag/relationships/{Uri.EscapeDataString(relationshipId)}",
            request,
            cancellationToken);
    }

    /// <summary>Deletes a graph relationship by identifier.</summary>
    public async Task DeleteRelationshipAsync(string relationshipId, CancellationToken cancellationToken = default)
    {
        await SendForStatusAsync(
            HttpMethod.Delete,
            $"mcpserver/graphrag/relationships/{Uri.EscapeDataString(relationshipId)}",
            null,
            cancellationToken);
    }
}
