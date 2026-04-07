using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Represents an explicit graph entity node
/// in the workspace-scoped knowledge graph.
/// </summary>
public sealed class GraphEntityEntity
{
    /// <summary>Unique entity identifier. Format: "ge-{Guid:N}".</summary>
    [Key]
    [MaxLength(256)]
    public required string Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Display name of the entity.</summary>
    [Required]
    [MaxLength(512)]
    public required string Name { get; set; }

    /// <summary>Entity classification (e.g. "person", "organization", "concept").</summary>
    [Required]
    [MaxLength(128)]
    public required string EntityType { get; set; }

    /// <summary>Optional free-text description.</summary>
    [MaxLength(4096)]
    public string? Description { get; set; }

    /// <summary>Optional JSON blob for extensible key-value metadata.</summary>
    [MaxLength(8192)]
    public string? Metadata { get; set; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the entity was last modified.</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Relationships where this entity is the source.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<GraphRelationshipEntity> SourceRelationships { get; set; } = new List<GraphRelationshipEntity>();

    /// <summary>Relationships where this entity is the target.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<GraphRelationshipEntity> TargetRelationships { get; set; } = new List<GraphRelationshipEntity>();
}
