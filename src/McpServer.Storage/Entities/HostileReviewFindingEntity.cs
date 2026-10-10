using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>TR-MCP-HOSTILEREVIEW-004: Normalized finding. Evidence summaries are sanitized snippets only.</summary>
public sealed class HostileReviewFindingEntity
{
    /// <summary>Row id.</summary>
    [Key]
    public long FindingId { get; set; }

    /// <summary>Parent request id.</summary>
    [Required]
    [StringLength(128)]
    public required string RequestId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public required string WorkspaceId { get; set; }

    /// <summary>Taxonomy category.</summary>
    [Required]
    [StringLength(64)]
    public required string Category { get; set; }

    /// <summary>Severity.</summary>
    [Required]
    [StringLength(32)]
    public required string Severity { get; set; }

    /// <summary>Linked artifact identity.</summary>
    [StringLength(1024)]
    public string? ArtifactId { get; set; }

    /// <summary>Location hint.</summary>
    [StringLength(512)]
    public string? Location { get; set; }

    /// <summary>Violated requirement/AC id.</summary>
    [StringLength(128)]
    public string? RequirementId { get; set; }

    /// <summary>Sanitized evidence summary. Never a raw prompt.</summary>
    [StringLength(2000)]
    public string? EvidenceSummary { get; set; }

    /// <summary>Recommendation text.</summary>
    [StringLength(2000)]
    public string? Recommendation { get; set; }

    /// <summary>Parent request.</summary>
    [ForeignKey(nameof(RequestId))]
    public HostileReviewRequestEntity? Request { get; set; }
}
