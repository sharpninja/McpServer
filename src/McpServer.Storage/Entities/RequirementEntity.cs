using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// Authoritative database row for one functional, technical, or testing
/// requirement. Markdown files are import/export projections only.
/// </summary>
public sealed class RequirementEntity
{
    /// <summary>Resolved workspace discriminator, normally the absolute workspace path.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Requirement kind: <c>fr</c>, <c>tr</c>, or <c>test</c>.</summary>
    [Required]
    [StringLength(16)]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Canonical requirement identifier.</summary>
    [Required]
    [StringLength(128)]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable title for FR/TR rows.</summary>
    [StringLength(1024)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Requirement body or testing condition text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Requirement priority, such as <c>high</c>, <c>medium</c>, or <c>low</c>.</summary>
    [Required]
    [StringLength(32)]
    public string Priority { get; set; } = "medium";

    /// <summary>Requirement lifecycle status, such as <c>pending</c> or <c>completed</c>.</summary>
    [Required]
    [StringLength(64)]
    public string Status { get; set; } = "pending";

    /// <summary>Optional operator notes that are not part of the canonical body text.</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// TR-MCP-REQAC-001: 4NF acceptance-criteria child rows ({id, text, isSatisfied, evidence}),
    /// replacing the former JSON array column. Empty when the requirement has no criteria.
    /// </summary>
    /// <remarks>
    /// Not an EF navigation: it is loaded/attached explicitly by the service. A principal-side
    /// collection navigation to <see cref="RequirementAcceptanceCriterionEntity"/> triggers EF
    /// relationship fixup that nulls the composite (tenant-column) foreign key on multi-entity
    /// inserts, so the child rows are written from the dependent side and read via an explicit
    /// query, mirroring <c>RequirementTraceabilityLinkEntity</c>.
    /// </remarks>
    [NotMapped]
    public List<RequirementAcceptanceCriterionEntity> AcceptanceCriteria { get; set; } = [];

    /// <summary>First requirement scope layer where this requirement applies.</summary>
    [Required]
    [StringLength(128)]
    public string ScopeStartLayerKey { get; set; } = "layer-1";

    /// <summary>Optional last requirement scope layer where this requirement applies.</summary>
    [StringLength(128)]
    public string? ScopeEndLayerKey { get; set; }

    /// <summary>UTC timestamp when the row was first created.</summary>
    [Required]
    [StringLength(64)]
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the row was last modified.</summary>
    [Required]
    [StringLength(64)]
    public string UpdatedAtUtc { get; set; } = string.Empty;
}
