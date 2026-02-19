using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FWH.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-013: Persisted chunk for full-text and vector retrieval.
/// </summary>
public sealed class ContextChunkEntity
{
    /// <summary>TR-PLANNED-013: Unique chunk identifier.</summary>
    [Key]
    [MaxLength(256)]
    public required string Id { get; set; }

    /// <summary>TR-PLANNED-013: Parent document identifier.</summary>
    [Required]
    [MaxLength(256)]
    public required string DocumentId { get; set; }

    /// <summary>TR-PLANNED-013: Chunk text content.</summary>
    [Required]
    public required string Content { get; set; }

    /// <summary>TR-PLANNED-013: Estimated token count.</summary>
    public int TokenCount { get; set; }

    /// <summary>TR-PLANNED-013: Zero-based index within document.</summary>
    public int ChunkIndex { get; set; }

    /// <summary>TR-PLANNED-013: Embedding vector stored as BLOB (nullable, populated during ingestion).</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Suppressed globally in Directory.Build.props")]
    public byte[]? Embedding { get; set; }

    /// <summary>Navigation to document.</summary>
    [ForeignKey(nameof(DocumentId))]
    public ContextDocumentEntity? Document { get; set; }
}
