using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-REQAC-001: 4NF acceptance-criterion row for a requirement. One row per criterion,
/// replacing the multi-valued dependency previously stored as
/// <see cref="RequirementEntity"/>'s <c>AcceptanceCriteriaJson</c> column. Mirrors the
/// {id, text, isSatisfied, evidence} shape of an acceptance criterion.
/// </summary>
public sealed class RequirementAcceptanceCriterionEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Owning requirement workspace discriminator (part of the composite parent key).</summary>
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Owning requirement kind (part of the composite parent key).</summary>
    [StringLength(16)]
    public string RequirementKind { get; set; } = string.Empty;

    /// <summary>Owning requirement id (part of the composite parent key).</summary>
    [StringLength(128)]
    public string RequirementId { get; set; } = string.Empty;

    /// <summary>Ordinal position within the requirement's acceptance-criteria list.</summary>
    public int Ordinal { get; set; }

    /// <summary>Criterion identifier (the acceptance criterion's own id).</summary>
    [StringLength(128)]
    public string CriterionId { get; set; } = string.Empty;

    /// <summary>Criterion text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Whether the criterion is currently satisfied.</summary>
    public bool IsSatisfied { get; set; }

    /// <summary>Optional evidence for the criterion.</summary>
    public string? Evidence { get; set; }

    /// <summary>Navigation to the owning requirement.</summary>
    public RequirementEntity? Requirement { get; set; }
}
