namespace McpServer.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-013: Abstraction for HNSW vector index operations.
/// FR-SUPPORT-010: Supports nearest-neighbor search for embedding-based retrieval.
/// </summary>
public interface IVectorIndexService
{
    /// <summary>TR-PLANNED-013: Add a vector to the index.</summary>
    /// <param name="chunkId">Chunk identifier.</param>
    /// <param name="embedding">Embedding vector.</param>
    void AddVector(string chunkId, float[] embedding);

    /// <summary>TR-PLANNED-013: Search for nearest neighbors.</summary>
    /// <param name="queryEmbedding">Query embedding vector.</param>
    /// <param name="k">Number of nearest neighbors to return.</param>
    /// <returns>List of (ChunkId, Distance) pairs sorted by distance.</returns>
    IReadOnlyList<(string ChunkId, float Distance)> Search(float[] queryEmbedding, int k = 20);

    /// <summary>TR-PLANNED-013: Persist the index to disk.</summary>
    Task SaveAsync(string path, CancellationToken ct = default);

    /// <summary>TR-PLANNED-013: Load the index from disk.</summary>
    Task LoadAsync(string path, CancellationToken ct = default);

    /// <summary>TR-PLANNED-013: Rebuild the index from scratch.</summary>
    Task RebuildAsync(CancellationToken ct = default);

    /// <summary>TR-PLANNED-013: Number of vectors in the index.</summary>
    int Count { get; }
}
