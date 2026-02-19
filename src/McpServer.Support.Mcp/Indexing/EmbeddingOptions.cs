namespace McpServer.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-013: Configuration options for the ONNX embedding service.
/// FR-SUPPORT-010: Controls model acquisition and inference parameters for all-MiniLM-L6-v2.
/// </summary>
public sealed class EmbeddingOptions
{
    /// <summary>TR-PLANNED-013: Path to the ONNX model file. If null, uses auto-download to LocalAppData.</summary>
    public string? ModelPath { get; set; }

    /// <summary>TR-PLANNED-013: Embedding vector dimensions (default 384 for MiniLM).</summary>
    public int Dimensions { get; set; } = 384;

    /// <summary>TR-PLANNED-013: Maximum input sequence length in tokens (default 128).</summary>
    public int MaxSequenceLength { get; set; } = 128;

    /// <summary>TR-PLANNED-013: Whether to auto-download the model on first use.</summary>
    public bool AutoDownload { get; set; } = true;
}
