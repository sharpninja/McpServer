using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Represents a directed, typed edge between two
/// <see cref="GraphEntityEntity"/> nodes in the workspace-scoped knowledge graph.
/// </summary>
public sealed class GraphRelationshipEntity
{
    /// <summary>Unique relationship identifier. Format: "gr-{Guid:N}".</summary>
    [Key]
    [StringLength(256)]
    public required string Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Source entity identifier (foreign key).</summary>
    [Required]
    [StringLength(256)]
    public required string SourceEntityId { get; set; }

    /// <summary>Target entity identifier (foreign key).</summary>
    [Required]
    [StringLength(256)]
    public required string TargetEntityId { get; set; }

    /// <summary>Relationship classification (e.g. "depends_on", "authored_by").</summary>
    [Required]
    [StringLength(128)]
    public required string RelationshipType { get; set; }

    /// <summary>Optional free-text description of the relationship.</summary>
    [StringLength(4096)]
    public string? Description { get; set; }

    /// <summary>Numeric weight/strength of the relationship. Default 1.0.</summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>Optional JSON blob for extensible key-value metadata.</summary>
    [StringLength(8192)]
    public string? Metadata { get; set; }

    /// <summary>UTC timestamp when the relationship was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the relationship was last modified.</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Navigation property to the source entity.</summary>
    [ForeignKey(nameof(SourceEntityId))]
    public GraphEntityEntity? SourceEntity { get; set; }

    /// <summary>Navigation property to the target entity.</summary>
    [ForeignKey(nameof(TargetEntityId))]
    public GraphEntityEntity? TargetEntity { get; set; }
}
