using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-MEMORY-MODEL-002 / FR-MCP-MEMORY-014: Append-only version snapshot for a memory.
/// S1 Green must map this entity; S1 Red asserts the mapping is still missing or incomplete.
/// </summary>
public sealed class MemoryVersionEntity
{
    /// <summary>Surrogate key.</summary>
    [Key]
    public long VersionRowId { get; set; }

    /// <summary>Parent memory id. FK to <see cref="MemoryEntity.Id"/>.</summary>
    [Required]
    [StringLength(128)]
    public required string MemoryId { get; set; }

    /// <summary>Contiguous positive version number.</summary>
    public int VersionNumber { get; set; }

    /// <summary>Title at this snapshot.</summary>
    [StringLength(256)]
    public string? Title { get; set; }

    /// <summary>Content at this snapshot.</summary>
    [Required]
    public required string Content { get; set; }

    /// <summary>UTC timestamp when the snapshot was written.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
