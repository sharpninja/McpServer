using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005: 4NF generic string-list entity for a TODO item. One row per value across the
/// description, technical-details, depends-on, functional-requirement, and technical-requirement
/// lists (discriminated by <see cref="ListType"/>), replacing the item's <c>DescriptionJson</c>,
/// <c>TechnicalDetailsJson</c>, <c>DependsOnJson</c>, <c>FunctionalRequirementsJson</c>, and
/// <c>TechnicalRequirementsJson</c> columns. The requirement list types keep the raw user strings;
/// <c>TodoRequirementLinks</c> remains the normalized requirement join. Written/read from the
/// dependent side because the composite (WorkspaceId, TodoId) parent key includes the tenant
/// column (see <c>RequirementAcceptanceCriterionEntity</c> for the rationale).
/// </summary>
public sealed class TodoItemListItemEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Owning TODO workspace discriminator (part of the composite parent key).</summary>
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Owning TODO id (part of the composite parent key).</summary>
    [StringLength(128)]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Discriminator identifying which list this item belongs to (Description, TechnicalDetail, DependsOn, FunctionalRequirement, TechnicalRequirement).</summary>
    [Required]
    [StringLength(32)]
    public required string ListType { get; set; }

    /// <summary>Ordinal position within the list.</summary>
    public int Ordinal { get; set; }

    /// <summary>The string value of this list item.</summary>
    public required string Value { get; set; }

    /// <summary>Navigation to the owning TODO item.</summary>
    public TodoItemEntity? TodoItem { get; set; }
}
