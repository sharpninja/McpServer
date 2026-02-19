namespace FWH.Support.Mcp.Models;

/// <summary>
/// TR-PLANNED-013: Chunked content for full-text and vector retrieval.
/// </summary>
public sealed record ContextChunk
{
    /// <summary>TR-PLANNED-013: Unique chunk identifier.</summary>
    public required string Id { get; init; }

    /// <summary>TR-PLANNED-013: Parent document identifier.</summary>
    public required string DocumentId { get; init; }

    /// <summary>TR-PLANNED-013: Chunk text content.</summary>
    public required string Content { get; init; }

    /// <summary>TR-PLANNED-013: Estimated token count.</summary>
    public int TokenCount { get; init; }

    /// <summary>TR-PLANNED-013: Zero-based index within document.</summary>
    public int ChunkIndex { get; init; }
}
