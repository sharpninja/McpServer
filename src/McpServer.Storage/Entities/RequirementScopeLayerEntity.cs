using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-REQSCOPE-001: ordered workspace-scoped layer that controls requirement
/// implementation and enforcement visibility.
/// </summary>
public sealed class RequirementScopeLayerEntity
{
    /// <summary>Resolved workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Stable layer key, for example <c>layer-1</c>.</summary>
    [Required]
    [StringLength(128)]
    public string Key { get; set; } = string.Empty;

    /// <summary>Immutable numeric order for layer comparisons.</summary>
    public int Order { get; set; }

    /// <summary>Human-readable layer name.</summary>
    [Required]
    [StringLength(512)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional layer description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional last layer where requirements starting in this layer apply.</summary>
    [StringLength(128)]
    public string? ScopeEndLayerKey { get; set; }

    /// <summary>UTC timestamp when the layer was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the layer was last updated.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
