using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013: Context retrieval endpoints for MCP.
/// FR-SUPPORT-010: Hybrid search and deterministic context packs.
/// </summary>
[ApiController]
[Route("mcpserver/context")]
public sealed class ContextController : ControllerBase
{
    private readonly McpDbContext _db;
    private readonly IContextSearchService _searchService;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public ContextController(McpDbContext db, IContextSearchService searchService)
    {
        _db = db;
        _searchService = searchService;
    }

    /// <summary>TR-PLANNED-013: Hybrid search with filters (context.search).</summary>
    /// <param name="request">Search request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results.</returns>
    [HttpPost("search")]
    public async Task<ActionResult<object>> SearchAsync([FromBody] ContextSearchRequest request, CancellationToken cancellationToken)
    {
        var query = (request?.Query ?? string.Empty).Trim();
        var limit = Math.Clamp(request?.Limit ?? 20, 1, 100);
        var sourceType = request?.SourceType;

        var result = await _searchService.SearchAsync(query, limit, sourceType, cancellationToken).ConfigureAwait(false);
        var chunks = result.Chunks.Select(c => new ContextChunk
        {
            Id = c.ChunkId,
            DocumentId = c.DocumentId,
            Content = c.Content,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        }).ToList();
        return Ok(new { query, chunks, sourceKeys = result.SourceKeys });
    }

    /// <summary>TR-PLANNED-013: Rebuild the FTS5 search index.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status of the rebuild operation.</returns>
    [HttpPost("rebuild-index")]
    public async Task<ActionResult<object>> RebuildIndexAsync(CancellationToken cancellationToken)
    {
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
            chunksQuery = chunksQuery.Where(c => c.Content != null && c.Content.Contains(query));
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

    /// <summary>TR-PLANNED-013: List indexed sources (context.sources).</summary>
    [HttpGet("sources")]
    public async Task<ActionResult<object>> GetSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await _db.Documents.AsNoTracking()
            .Select(d => new { d.SourceKey, d.SourceType, d.IngestedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { sources });
    }
}

/// <summary>Request for context search. TR-PLANNED-013.</summary>
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
