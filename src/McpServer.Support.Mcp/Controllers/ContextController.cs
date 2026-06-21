using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-CORE-013: Context retrieval endpoints for MCP.
/// FR-SUPPORT-010: Hybrid search and deterministic context packs.
/// </summary>
[ApiController]
[Route("mcpserver/context")]
public sealed class ContextController : ControllerBase
{
    private const string DeferredContextMutationMessage =
        "Context ingestion and rebuild mutations are not transaction compensated while required turn transactions are active.";

    private readonly McpDbContext _db;
    private readonly IContextSearchService _searchService;
    private readonly IGraphRagService _graphRagService;
    private readonly GraphRagOptions _graphRagOptions;
    private readonly IngestionCoordinator _ingestionCoordinator;
    private readonly ITurnTransactionCoordinator? _transactionCoordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>TR-PLANNED-CORE-013: Constructor.</summary>
    public ContextController(
        McpDbContext db,
        IContextSearchService searchService,
        IGraphRagService graphRagService,
        IngestionCoordinator ingestionCoordinator,
        IOptions<GraphRagOptions> graphRagOptions,
        ITurnTransactionCoordinator? transactionCoordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _db = db;
        _searchService = searchService;
        _graphRagService = graphRagService;
        _ingestionCoordinator = ingestionCoordinator;
        _graphRagOptions = graphRagOptions.Value;
        _transactionCoordinator = transactionCoordinator;
        _transactionOptions = transactionOptions;
    }

    /// <summary>TR-PLANNED-CORE-013: Hybrid search with filters (context.search).</summary>
    /// <param name="request">Search request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results.</returns>
    [HttpPost("search")]
    public async Task<ActionResult<object>> SearchAsync([FromBody] ContextSearchRequest request, CancellationToken cancellationToken)
    {
        var query = (request?.Query ?? string.Empty).Trim();
        var limit = Math.Clamp(request?.Limit ?? 20, 1, 100);
        var sourceType = request?.SourceType;

        if (_graphRagOptions.Enabled
            && _graphRagOptions.EnhanceContextSearch
            && !string.IsNullOrWhiteSpace(query)
            && string.IsNullOrWhiteSpace(sourceType))
        {
            var graphResult = await _graphRagService.QueryAsync(new GraphRagQueryRequest
            {
                Query = query,
                Mode = _graphRagOptions.DefaultQueryMode,
                MaxChunks = limit,
                IncludeContextChunks = true
            }, cancellationToken).ConfigureAwait(false);

            var graphChunks = graphResult.Chunks.ToList();
            return Ok(new
            {
                query,
                chunks = graphChunks,
                sourceKeys = graphResult.SourceKeys,
                graphRag = new
                {
                    mode = graphResult.Mode,
                    graphResult.FallbackUsed,
                    graphResult.Backend,
                    graphResult.Answer,
                    graphResult.Citations
                }
            });
        }

        var result = await _searchService.SearchAsync(query, limit, sourceType, cancellationToken).ConfigureAwait(false);
        var chunks = result.Chunks.Select(c => new ContextChunk
        {
            Id = c.ChunkId,
            DocumentId = c.DocumentId,
            Content = c.Content,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        }).ToList();
        return Ok(new
        {
            query,
            chunks,
            sourceKeys = result.SourceKeys,
            graphRag = new
            {
                mode = _graphRagOptions.DefaultQueryMode,
                fallbackUsed = true,
                backend = "context-search",
                reason = _graphRagOptions.Enabled && _graphRagOptions.EnhanceContextSearch
                    ? string.IsNullOrWhiteSpace(query)
                        ? "empty_query_forces_legacy_path"
                        : !string.IsNullOrWhiteSpace(sourceType)
                            ? "sourceType_filter_forces_legacy_path"
                            : "graphrag_disabled_or_not_enabled_for_context"
                    : "graphrag_disabled_or_not_enabled_for_context"
            }
        });
    }

    /// <summary>TR-PLANNED-CORE-013: Rebuild the FTS5 search index.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status of the rebuild operation.</returns>
    [HttpPost("rebuild-index")]
    public async Task<ActionResult<object>> RebuildIndexAsync(CancellationToken cancellationToken)
    {
        if (ShouldDeferContextMutation(out var transactionError))
        {
            return Conflict(new { error = transactionError });
        }

        await _searchService.RebuildAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { status = "rebuilt" });
    }

    /// <summary>FR-SUPPORT-010: Deterministic context pack (context.pack).</summary>
    /// <param name="request">Pack request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Context pack with ordered chunks.</returns>
    [HttpPost("pack")]
    public async Task<ActionResult<ContextPack>> GetPackAsync([FromBody] ContextPackRequest request, CancellationToken cancellationToken)
    {
        var queryId = request?.QueryId ?? Guid.NewGuid().ToString("N");
        var limit = Math.Clamp(request?.Limit ?? 20, 1, 100);
        var query = (request?.Query ?? string.Empty).Trim();

        IQueryable<Storage.Entities.ContextChunkEntity> chunksQuery = _db.Chunks.AsNoTracking();
        if (!string.IsNullOrEmpty(query))
        {
            var providerName = _db.Database.ProviderName ?? string.Empty;
            var supportsILike = providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                || providerName.Contains("Postgres", StringComparison.OrdinalIgnoreCase);

            chunksQuery = supportsILike
                ? chunksQuery.Where(c => c.Content != null && EF.Functions.ILike(c.Content, $"%{query}%"))
                : chunksQuery.Where(c => c.Content != null && c.Content.Contains(query));
        }
        var chunkEntities = await chunksQuery
            .OrderBy(c => c.DocumentId)
            .ThenBy(c => c.ChunkIndex)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var chunks = chunkEntities.Select(c => new ContextChunk
        {
            Id = c.Id,
            DocumentId = c.DocumentId,
            Content = c.Content,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        }).ToList<ContextChunk>();
        var docIds = chunkEntities.Select(c => c.DocumentId).Distinct().ToList();
        var sourceKeys = await _db.Documents.Where(d => docIds.Contains(d.Id)).Select(d => d.SourceKey).ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new ContextPack
        {
            QueryId = queryId,
            Chunks = chunks,
            SourceKeys = sourceKeys
        });
    }

    /// <summary>TR-PLANNED-CORE-013: List indexed sources (context.sources).</summary>
    [HttpGet("sources")]
    public async Task<ActionResult<object>> GetSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await _db.Documents.AsNoTracking()
            .Select(d => new { d.SourceKey, d.SourceType, d.IngestedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { sources });
    }

    /// <summary>
    /// FR-MCP-065, TR-MCP-INGEST-003: Ingests context directly from a website URL.
    /// </summary>
    /// <param name="request">Website ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Structured URL ingestion result with optional GraphRAG trigger status.</returns>
    [HttpPost("ingest-website")]
    public async Task<ActionResult<WebsiteIngestResult>> IngestWebsiteAsync([FromBody] WebsiteIngestRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "url is required." });
        }

        if (ShouldDeferContextMutation(out var transactionError))
        {
            return Conflict(new { error = transactionError });
        }

        var result = await _ingestionCoordinator.IngestWebsiteAsync(request, cancellationToken).ConfigureAwait(false);

        if (request.TriggerGraphRagIndex)
        {
            try
            {
                await _graphRagService.IndexAsync(new GraphRagIndexRequest { Force = request.ForceRefresh }, cancellationToken).ConfigureAwait(false);
                result.GraphRagIndexed = true;
            }
            catch (Exception ex)
            {
                result.GraphRagIndexed = false;
                result.GraphRagIndexError = ex.Message;
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// FR-MCP-065, TR-MCP-INGEST-003: Streams website ingestion progress via SSE.
    /// </summary>
    /// <param name="request">Website ingestion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("ingest-website/stream")]
    public async Task IngestWebsiteStreamAsync([FromBody] WebsiteIngestRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Request body is required." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "url is required." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (ShouldDeferContextMutation(out var transactionError))
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            await Response.WriteAsJsonAsync(new { error = transactionError }, cancellationToken).ConfigureAwait(false);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        static async Task WriteSseEventAsync(HttpResponse response, string eventName, object payload, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(payload);
            await response.WriteAsync($"event: {eventName}\n", ct).ConfigureAwait(false);
            await response.WriteAsync($"data: {json}\n\n", ct).ConfigureAwait(false);
            await response.Body.FlushAsync(ct).ConfigureAwait(false);
        }

        try
        {
            var result = await _ingestionCoordinator.IngestWebsiteStreamingAsync(
                request,
                progress => WriteSseEventAsync(Response, progress.EventType, progress, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (request.TriggerGraphRagIndex)
            {
                await WriteSseEventAsync(Response, "indexing", new
                {
                    runId = result.RunId,
                    status = "running",
                    message = "GraphRAG indexing started."
                }, cancellationToken).ConfigureAwait(false);

                try
                {
                    await _graphRagService.IndexAsync(new GraphRagIndexRequest { Force = request.ForceRefresh }, cancellationToken).ConfigureAwait(false);
                    result.GraphRagIndexed = true;

                    await WriteSseEventAsync(Response, "indexing", new
                    {
                        runId = result.RunId,
                        status = "completed",
                        message = "GraphRAG indexing completed."
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    result.GraphRagIndexed = false;
                    result.GraphRagIndexError = ex.Message;

                    await WriteSseEventAsync(Response, "indexing", new
                    {
                        runId = result.RunId,
                        status = "failed",
                        message = ex.Message
                    }, cancellationToken).ConfigureAwait(false);
                }
            }

            await WriteSseEventAsync(Response, "result", result, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }
    }

    private bool ShouldDeferContextMutation(out string error)
    {
        error = string.Empty;
        if (_transactionCoordinator is null)
            return false;

        var status = _transactionCoordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredContextMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);
}

/// <summary>Request for context search. TR-PLANNED-CORE-013.</summary>
public sealed class ContextSearchRequest
{
    /// <summary>Search query text.</summary>
    public string? Query { get; set; }

    /// <summary>Optional source type filter.</summary>
    public string? SourceType { get; set; }

    /// <summary>Max chunks to return.</summary>
    public int Limit { get; set; } = 20;
}

/// <summary>Request for context pack. FR-SUPPORT-010.</summary>
public sealed class ContextPackRequest
{
    /// <summary>Query identifier for reproducibility.</summary>
    public string? QueryId { get; set; }

    /// <summary>Search query text.</summary>
    public string? Query { get; set; }

    /// <summary>Max chunks in pack.</summary>
    public int Limit { get; set; } = 20;
}
