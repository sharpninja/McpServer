using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: FTS5 full-text search implementation using SQLite's FTS5 module.
/// FR-SUPPORT-010: BM25 ranking, snippet extraction, optional sourceType filter.
/// </summary>
internal sealed class Fts5SearchService : IContextSearchService
{
    private readonly McpDbContext _db;
    private readonly ILogger<Fts5SearchService> _logger;

    /// <summary>TR-PLANNED-013: Constructor for DI.</summary>
    public Fts5SearchService(McpDbContext db, ILogger<Fts5SearchService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContextSearchResult> SearchAsync(string query, int limit = 20, string? sourceType = null, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim();
        var lim = Math.Clamp(limit, 1, 100);

        if (string.IsNullOrEmpty(q))
        {
            return new ContextSearchResult([], []);
        }

        // Escape FTS5 special characters and form a simple prefix query
        var ftsQuery = EscapeFts5Query(q);

        try
        {
            var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync(ct).ConfigureAwait(false);

            using var cmd = conn.CreateCommand();

            if (!string.IsNullOrEmpty(sourceType))
            {
                cmd.CommandText = """
                    SELECT c.Id, c.DocumentId, c.Content, c.TokenCount, c.ChunkIndex,
                           bm25(chunks_fts) AS score,
                           snippet(chunks_fts, 1, '<b>', '</b>', '...', 64) AS snippet
                    FROM chunks_fts f
                    JOIN Chunks c ON f.ChunkId = c.Id
                    JOIN Documents d ON c.DocumentId = d.Id
                    WHERE chunks_fts MATCH @query AND d.SourceType = @sourceType
                    ORDER BY score
                    LIMIT @limit
                    """;
                var pSourceType = cmd.CreateParameter();
                pSourceType.ParameterName = "@sourceType";
                pSourceType.Value = sourceType;
                cmd.Parameters.Add(pSourceType);
            }
            else
            {
                cmd.CommandText = """
                    SELECT c.Id, c.DocumentId, c.Content, c.TokenCount, c.ChunkIndex,
                           bm25(chunks_fts) AS score,
                           snippet(chunks_fts, 1, '<b>', '</b>', '...', 64) AS snippet
                    FROM chunks_fts f
                    JOIN Chunks c ON f.ChunkId = c.Id
                    WHERE chunks_fts MATCH @query
                    ORDER BY score
                    LIMIT @limit
                    """;
            }

            var pQuery = cmd.CreateParameter();
            pQuery.ParameterName = "@query";
            pQuery.Value = ftsQuery;
            cmd.Parameters.Add(pQuery);

            var pLimit = cmd.CreateParameter();
            pLimit.ParameterName = "@limit";
            pLimit.Value = lim;
            cmd.Parameters.Add(pLimit);

            var chunks = new List<ScoredChunk>();
            var docIds = new HashSet<string>();

            using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var chunkId = reader.GetString(0);
                var documentId = reader.GetString(1);
                var content = reader.GetString(2);
                var tokenCount = reader.GetInt32(3);
                var chunkIndex = reader.GetInt32(4);
                var score = reader.GetDouble(5);
                var snippet = reader.IsDBNull(6) ? null : reader.GetString(6);

                chunks.Add(new ScoredChunk
                {
                    ChunkId = chunkId,
                    DocumentId = documentId,
                    Content = content,
                    Score = score,
                    Snippet = snippet,
                    TokenCount = tokenCount,
                    ChunkIndex = chunkIndex
                });
                docIds.Add(documentId);
            }

            var sourceKeys = await _db.Documents
                .Where(d => docIds.Contains(d.Id))
                .Select(d => d.SourceKey)
                .Distinct()
                .ToListAsync(ct).ConfigureAwait(false);

            return new ContextSearchResult(chunks, sourceKeys);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "FTS5 search failed, falling back to LINQ Contains");
            return await FallbackSearchAsync(q, lim, sourceType, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RebuildAsync(CancellationToken ct = default)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync(ct).ConfigureAwait(false);

            // Repopulate FTS5 index from Chunks table
            using var deleteCmd = conn.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM chunks_fts";
            await deleteCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = "INSERT INTO chunks_fts(ChunkId, Content) SELECT Id, Content FROM Chunks";
            await insertCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            using var rebuildCmd = conn.CreateCommand();
            rebuildCmd.CommandText = "INSERT INTO chunks_fts(chunks_fts) VALUES('rebuild')";
            await rebuildCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("FTS5 index rebuilt successfully");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to rebuild FTS5 index");
            throw;
        }
    }

    private async Task<ContextSearchResult> FallbackSearchAsync(string query, int limit, string? sourceType, CancellationToken ct)
    {
        var chunksQuery = _db.Chunks.AsNoTracking();
        if (!string.IsNullOrEmpty(sourceType))
        {
            var docIds = await _db.Documents.Where(d => d.SourceType == sourceType).Select(d => d.Id).ToListAsync(ct).ConfigureAwait(false);
            chunksQuery = chunksQuery.Where(c => docIds.Contains(c.DocumentId));
        }
        chunksQuery = chunksQuery.Where(c => c.Content != null && c.Content.Contains(query));

        var chunkList = await chunksQuery
            .OrderBy(c => c.DocumentId)
            .ThenBy(c => c.ChunkIndex)
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

    private static string EscapeFts5Query(string query)
    {
        // Wrap each word in double quotes to treat as literals (handles special chars)
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return "\"\"";
        return string.Join(" OR ", words.Select(w => "\"" + w.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""));
    }
}
