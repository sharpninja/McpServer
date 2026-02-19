namespace FWH.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Abstraction for context chunk search (FTS5, vector, or hybrid).
/// FR-SUPPORT-010: Enables ranked search with BM25 scoring and snippet extraction.
/// </summary>
public interface IContextSearchService
{
    /// <summary>TR-PLANNED-013: Search indexed context chunks by query text.</summary>
    /// <param name="query">Search query text.</param>
    /// <param name="limit">Maximum number of results to return (default 20).</param>
    /// <param name="sourceType">Optional source type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Search result with scored chunks and source keys.</returns>
    Task<ContextSearchResult> SearchAsync(string query, int limit = 20, string? sourceType = null, CancellationToken ct = default);

    /// <summary>TR-PLANNED-013: Rebuild the search index.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task RebuildAsync(CancellationToken ct = default);
}

/// <summary>TR-PLANNED-013: Result of a context search operation.</summary>
/// <param name="Chunks">Scored and ranked chunks.</param>
/// <param name="SourceKeys">Distinct source keys from matching documents.</param>
public sealed record ContextSearchResult(IReadOnlyList<ScoredChunk> Chunks, IReadOnlyList<string> SourceKeys);

/// <summary>TR-PLANNED-013: A chunk with its relevance score and optional snippet.</summary>
public sealed record ScoredChunk
{
    /// <summary>Unique chunk identifier.</summary>
    public required string ChunkId { get; init; }

    /// <summary>Parent document identifier.</summary>
    public required string DocumentId { get; init; }

    /// <summary>Chunk text content.</summary>
    public required string Content { get; init; }

    /// <summary>Relevance score (lower is better for BM25).</summary>
    public double Score { get; init; }

    /// <summary>Optional highlighted snippet from FTS5.</summary>
    public string? Snippet { get; init; }

    /// <summary>Estimated token count.</summary>
    public int TokenCount { get; init; }

    /// <summary>Zero-based index within document.</summary>
    public int ChunkIndex { get; init; }
}
