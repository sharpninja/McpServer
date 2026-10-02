using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: Dedicated memory ANN/FTS side row, separate from the
/// context/repo index. Workspace-safe: Global rows have a null workspace stamp;
/// Workspace rows carry the owning workspace id.
/// </summary>
public sealed class MemoryIndexEntity
{
    /// <summary>MEMORY-* id this index row belongs to.</summary>
    [Key]
    [StringLength(128)]
    public required string MemoryId { get; set; }

    /// <summary>Owning workspace path for Workspace-scoped memories; null for Global.</summary>
    [StringLength(1024)]
    public string? WorkspaceId { get; set; }

    /// <summary>SHA-256 hex of the indexed title+content used to detect stale ready rows.</summary>
    [Required]
    [StringLength(64)]
    public required string ContentHash { get; set; }

    /// <summary>JSON-serialized local/ONNX embedding vector for the indexed content.</summary>
    [Required]
    public required string EmbeddingJson { get; set; }

    /// <summary>UTC timestamp when this index row was last written.</summary>
    public DateTimeOffset IndexedAtUtc { get; set; }
}
