using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>TR-MCP-HOSTILEREVIEW-001: Durable hostile-review request. Never stores raw artifact bodies or assembled prompts.</summary>
public sealed class HostileReviewRequestEntity
{
    /// <summary>Stable request identifier.</summary>
    [Key]
    [StringLength(128)]
    public required string RequestId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public required string WorkspaceId { get; set; }

    /// <summary>Target type name.</summary>
    [Required]
    [StringLength(64)]
    public required string TargetType { get; set; }

    /// <summary>Requested review mode.</summary>
    [Required]
    [StringLength(64)]
    public required string Mode { get; set; }

    /// <summary>Scope statement. Never raw artifact text.</summary>
    [Required]
    [StringLength(4000)]
    public required string ScopeStatement { get; set; }

    /// <summary>Requester agent identity.</summary>
    [Required]
    [StringLength(128)]
    public required string RequestingAgent { get; set; }

    /// <summary>Durable request status.</summary>
    [Required]
    [StringLength(32)]
    public required string Status { get; set; }

    /// <summary>HTTP-equivalent status for admission failures (0 when accepted).</summary>
    public int HttpStatus { get; set; }

    /// <summary>Optional accepted terminal verdict.</summary>
    [StringLength(16)]
    public string? Verdict { get; set; }

    /// <summary>Request-quality disclosure score 0-1.</summary>
    public double? QualityDisclosure { get; set; }

    /// <summary>Request-quality scope clarity 0-1.</summary>
    public double? QualityScopeClarity { get; set; }

    /// <summary>Request-quality objective clarity 0-1.</summary>
    public double? QualityObjectiveClarity { get; set; }

    /// <summary>Request-quality confidence 0-1.</summary>
    public double? QualityConfidence { get; set; }

    /// <summary>Whether the request contained enough context.</summary>
    public bool? QualityEnoughContext { get; set; }

    /// <summary>Sanitized last error code.</summary>
    [StringLength(64)]
    public string? LastErrorCode { get; set; }

    /// <summary>UTC created.</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>UTC last updated.</summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>Artifact links. Identity only.</summary>
    public List<HostileReviewArtifactLinkEntity> Links { get; set; } = [];

    /// <summary>Reviewer executions.</summary>
    public List<HostileReviewExecutionEntity> Executions { get; set; } = [];

    /// <summary>Normalized findings.</summary>
    public List<HostileReviewFindingEntity> Findings { get; set; } = [];

    /// <summary>Diagnostics. Never raw bodies.</summary>
    public List<HostileReviewDiagnosticEntity> Diagnostics { get; set; } = [];
}
