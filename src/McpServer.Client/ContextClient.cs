using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>Client for context search endpoints (/mcp/context).</summary>
public sealed class ContextClient : McpClientBase
{
    /// <summary>Initializes a new instance of <see cref="ContextClient"/>.</summary>
    public ContextClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    /// <summary>Perform a hybrid semantic + full-text search.</summary>
    public async Task<ContextSearchResult> SearchAsync(
        string query, string? sourceType = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var request = new ContextSearchRequest { Query = query, SourceType = sourceType, Limit = limit };
        return await PostAsync<ContextSearchResult>("mcp/context/search", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Trigger a full index rebuild.</summary>
    public async Task<RebuildIndexResult> RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        return await PostAsync<RebuildIndexResult>("mcp/context/rebuild-index", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Get a deterministic context pack for a query.</summary>
    public async Task<ContextPack> PackAsync(
        string query, string? queryId = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var request = new ContextPackRequest { Query = query, QueryId = queryId, Limit = limit };
        return await PostAsync<ContextPack>("mcp/context/pack", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>List all indexed document sources.</summary>
    public async Task<ContextSourcesResult> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ContextSourcesResult>("mcp/context/sources", cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Result of a rebuild index operation.</summary>
public sealed class RebuildIndexResult
{
    /// <summary>Operation status.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>Result of listing context sources.</summary>
public sealed class ContextSourcesResult
{
    /// <summary>Indexed sources.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("sources")]
    public IReadOnlyList<ContextSource> Sources { get; set; } = [];
}
