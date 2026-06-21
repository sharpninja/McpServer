namespace McpServer.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-CORE-013: Abstraction for generating text embeddings for vector search.
/// FR-SUPPORT-010: Supports ONNX-based embedding models (e.g. all-MiniLM-L6-v2).
/// </summary>
public interface IEmbeddingService
{
    /// <summary>TR-PLANNED-CORE-013: Generate an embedding vector for a single text.</summary>
    /// <param name="text">Input text to embed.</param>
    /// <returns>Embedding vector (e.g. float[384] for MiniLM).</returns>
    float[] GenerateEmbedding(string text);

    /// <summary>TR-PLANNED-CORE-013: Generate embedding vectors for a batch of texts.</summary>
    /// <param name="texts">Input texts to embed.</param>
    /// <returns>Array of embedding vectors.</returns>
    ReadOnlyMemory<float>[] GenerateEmbeddings(IReadOnlyList<string> texts);

    /// <summary>TR-PLANNED-CORE-013: Embedding vector dimensions.</summary>
    int Dimensions { get; }

    /// <summary>TR-PLANNED-CORE-013: Whether the embedding model is loaded and ready.</summary>
    bool IsAvailable { get; }
}
