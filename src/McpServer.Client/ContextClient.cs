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
        return await PostAsync<ContextSearchResult>("mcpserver/context/search", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Trigger a full index rebuild.</summary>
    public async Task<RebuildIndexResult> RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<RebuildIndexResult>("mcpserver/context/rebuild-index", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get a deterministic context pack for a query.</summary>
    public async Task<ContextPack> PackAsync(
        string query, string? queryId = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var request = new ContextPackRequest { Query = query, QueryId = queryId, Limit = limit };
        return await PostAsync<ContextPack>("mcpserver/context/pack", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>List all indexed document sources.</summary>
    public async Task<ContextSourcesResult> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ContextSourcesResult>("mcpserver/context/sources", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get GraphRAG status for the active workspace.</summary>
    public async Task<GraphRagStatusResult> GraphRagStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<GraphRagStatusResult>("mcpserver/graphrag/status", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Trigger GraphRAG indexing for the active workspace.</summary>
    public async Task<GraphRagStatusResult> GraphRagIndexAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        return await PostAsync<GraphRagStatusResult>("mcpserver/graphrag/index", new GraphRagIndexRequest { Force = force }, cancellationToken).ConfigureAwait(false);
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
        return await PostAsync<GraphRagQueryResult>("mcpserver/graphrag/query", request, cancellationToken).ConfigureAwait(false);
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
