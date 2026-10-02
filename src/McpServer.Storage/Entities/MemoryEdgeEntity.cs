using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-MEMORY-MODEL-002: Explicit directed memory edge. Unique (From, To, EdgeType)
/// must be enforced at the database layer in S1 Green.
/// </summary>
public sealed class MemoryEdgeEntity
{
    /// <summary>Surrogate key.</summary>
    [Key]
    public long EdgeRowId { get; set; }

    /// <summary>Source memory id.</summary>
    [Required]
    [StringLength(128)]
    public required string FromMemoryId { get; set; }

    /// <summary>Target memory id.</summary>
    [Required]
    [StringLength(128)]
    public required string ToMemoryId { get; set; }

    /// <summary>Edge type token.</summary>
    [Required]
    [StringLength(64)]
    public required string EdgeType { get; set; }

    /// <summary>Edge weight.</summary>
    public double Weight { get; set; }
}
