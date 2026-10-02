using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>TR-MCP-HOSTILEREVIEW-002: Artifact identity stored without body text.</summary>
public sealed class HostileReviewArtifactLinkEntity
{
    /// <summary>Row id.</summary>
    [Key]
    public long LinkId { get; set; }

    /// <summary>Parent request id.</summary>
    [Required]
    [StringLength(128)]
    public required string RequestId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public required string WorkspaceId { get; set; }

    /// <summary>Artifact type (todo, requirement, session, triage, plan, file, commit, document).</summary>
    [Required]
    [StringLength(32)]
    public required string ArtifactType { get; set; }

    /// <summary>Stable locator/id. Never a raw body.</summary>
    [Required]
    [StringLength(1024)]
    public required string ArtifactId { get; set; }

    /// <summary>SHA-256 when resolved.</summary>
    [StringLength(64)]
    public string? ContentSha256 { get; set; }

    /// <summary>True when submit resolved the identity.</summary>
    public bool Resolved { get; set; }

    /// <summary>Parent request.</summary>
    [ForeignKey(nameof(RequestId))]
    public HostileReviewRequestEntity? Request { get; set; }
}
