using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Hybrid search service blending FTS5 BM25 and HNSW vector results using Reciprocal Rank Fusion.
/// FR-SUPPORT-010: Graceful degradation to FTS5-only, vector-only, or LINQ fallback.
/// </summary>
internal sealed class HybridSearchService : IContextSearchService
{
    private const int RrfK = 60;
    private readonly IContextSearchService _fts5;
    private readonly IVectorIndexService _vectorIndex;
    private readonly IEmbeddingService _embedding;
    private readonly McpDbContext _db;
    private readonly ILogger<HybridSearchService> _logger;

    /// <summary>TR-PLANNED-013: Constructor with all search mode dependencies.</summary>
    public HybridSearchService(
        Fts5SearchService fts5,
        IVectorIndexService vectorIndex,
        IEmbeddingService embedding,
        McpDbContext db,
        ILogger<HybridSearchService> logger)
        : this((IContextSearchService)fts5, vectorIndex, embedding, db, logger)
    {
    }

    /// <summary>TR-PLANNED-013: Internal constructor for testing with any IContextSearchService.</summary>
    internal HybridSearchService(
        IContextSearchService fts5,
        IVectorIndexService vectorIndex,
        IEmbeddingService embedding,
        McpDbContext db,
        ILogger<HybridSearchService> logger)
    {
        _fts5 = fts5;
        _vectorIndex = vectorIndex;
        _embedding = embedding;
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContextSearchResult> SearchAsync(string query, int limit = 20, string? sourceType = null, CancellationToken ct = default)
    {
        var overFetchLimit = limit * 2;
        ContextSearchResult? fts5Result = null;
        List<(string ChunkId, float Distance)>? vectorResults = null;
        var mode = "hybrid";

        // FTS5 search
        try
        {
            fts5Result = await _fts5.SearchAsync(query, overFetchLimit, sourceType, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "FTS5 search failed in hybrid mode");
        }

        // Vector search (if embedding available)
        if (_embedding.IsAvailable && _vectorIndex.Count > 0)
        {
            try
            {
                var queryEmbedding = _embedding.GenerateEmbedding(query);
                vectorResults = _vectorIndex.Search(queryEmbedding, overFetchLimit)
                    .ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Vector search failed in hybrid mode");
            }
        }

        // Determine active mode
        if (fts5Result?.Chunks.Count > 0 && vectorResults?.Count > 0)
            mode = "hybrid";
        else if (fts5Result?.Chunks.Count > 0)
            mode = "fts5-only";
        else if (vectorResults?.Count > 0)
            mode = "vector-only";
        else
            mode = "fallback-linq";

        _logger.LogDebug("HybridSearchService: using mode={Mode}", mode);

        if (mode == "fallback-linq")
        {
            return await FallbackLinqSearchAsync(query, limit, sourceType, ct).ConfigureAwait(false);
        }

        // RRF blending
        var rrfScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var chunkMap = new Dictionary<string, ScoredChunk>(StringComparer.Ordinal);

        // FTS5 contributions
        if (fts5Result?.Chunks is not null)
        {
            for (var rank = 0; rank < fts5Result.Chunks.Count; rank++)
            {
                var chunk = fts5Result.Chunks[rank];
                var rrfScore = 1.0 / (RrfK + rank + 1);
                rrfScores[chunk.ChunkId] = rrfScores.GetValueOrDefault(chunk.ChunkId) + rrfScore;
                chunkMap.TryAdd(chunk.ChunkId, chunk);
            }
        }

        // Vector contributions
        if (vectorResults is not null)
        {
            for (var rank = 0; rank < vectorResults.Count; rank++)
            {
                var (chunkId, _) = vectorResults[rank];
                var rrfScore = 1.0 / (RrfK + rank + 1);
                rrfScores[chunkId] = rrfScores.GetValueOrDefault(chunkId) + rrfScore;

                // Load chunk content from DB if not already in map
                if (!chunkMap.ContainsKey(chunkId))
                {
                    var entity = await _db.Chunks.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == chunkId, ct).ConfigureAwait(false);
                    if (entity is not null)
                    {
                        chunkMap[chunkId] = new ScoredChunk
                        {
                            ChunkId = entity.Id,
                            DocumentId = entity.DocumentId,
                            Content = entity.Content,
                            TokenCount = entity.TokenCount,
                            ChunkIndex = entity.ChunkIndex
                        };
                    }
                }
            }
        }

        // Sort by RRF score descending, take top limit
        var merged = rrfScores
            .OrderByDescending(kv => kv.Value)
            .Take(limit)
            .Where(kv => chunkMap.ContainsKey(kv.Key))
            .Select(kv => chunkMap[kv.Key] with { Score = kv.Value })
            .ToList();

        var docIds = merged.Select(c => c.DocumentId).Distinct().ToList();
        var sourceKeys = await _db.Documents
            .Where(d => docIds.Contains(d.Id))
            .Select(d => d.SourceKey)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

        return new ContextSearchResult(merged, sourceKeys);
    }

    /// <inheritdoc />
    public async Task RebuildAsync(CancellationToken ct = default)
    {
        await _fts5.RebuildAsync(ct).ConfigureAwait(false);
        await _vectorIndex.RebuildAsync(ct).ConfigureAwait(false);
    }

    private async Task<ContextSearchResult> FallbackLinqSearchAsync(string query, int limit, string? sourceType, CancellationToken ct)
    {
        _logger.LogDebug("HybridSearchService: falling back to LINQ Contains search");
        var chunksQuery = _db.Chunks.AsNoTracking();
        if (!string.IsNullOrEmpty(sourceType))
        {
            var docIds = await _db.Documents.Where(d => d.SourceType == sourceType).Select(d => d.Id).ToListAsync(ct).ConfigureAwait(false);
            chunksQuery = chunksQuery.Where(c => docIds.Contains(c.DocumentId));
        }

        chunksQuery = chunksQuery.Where(c => c.Content != null && c.Content.Contains(query));
        var chunkList = await chunksQuery
            .OrderBy(c => c.DocumentId).ThenBy(c => c.ChunkIndex)
            .Take(limit)
            .ToListAsync(ct).ConfigureAwait(false);

        var chunks = chunkList.Select(c => new ScoredChunk
        {
            ChunkId = c.Id,
            DocumentId = c.DocumentId,
            Content = c.Content ?? string.Empty,
            Score = 0,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        }).ToList();

        var sourceKeys = await _db.Documents
            .Where(d => chunkList.Select(x => x.DocumentId).Distinct().Contains(d.Id))
            .Select(d => d.SourceKey)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);

        return new ContextSearchResult(chunks, sourceKeys);
    }
}
