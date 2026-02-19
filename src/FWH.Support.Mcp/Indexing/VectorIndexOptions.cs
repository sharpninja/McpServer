namespace FWH.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-013: Configuration options for the HNSW vector index.
/// FR-SUPPORT-010: Controls graph parameters and persistence path.
/// </summary>
public sealed class VectorIndexOptions
{
    /// <summary>TR-PLANNED-013: Embedding vector dimensions (default 384).</summary>
    public int Dimensions { get; set; } = 384;

    /// <summary>TR-PLANNED-013: Maximum number of elements in the index.</summary>
    public int MaxElements { get; set; } = 100_000;

    /// <summary>TR-PLANNED-013: HNSW M parameter — number of bi-directional links per node.</summary>
    public int M { get; set; } = 16;

    /// <summary>TR-PLANNED-013: Size of dynamic candidate list during construction.</summary>
    public int EfConstruction { get; set; } = 200;

    /// <summary>TR-PLANNED-013: Size of dynamic candidate list during search.</summary>
    public int EfSearch { get; set; } = 50;

    /// <summary>TR-PLANNED-013: File path for persisting the HNSW index.</summary>
    public string IndexPath { get; set; } = "mcp-data/vector.idx";
}
