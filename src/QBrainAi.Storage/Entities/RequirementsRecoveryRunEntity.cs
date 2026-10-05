using System.ComponentModel.DataAnnotations;

namespace QBrainAi.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-REQRECOVERY-001: durable idempotency record for one atomic requirements recovery apply.
/// The pair (WorkspaceId, IdempotencyKey) is unique. Dry-run does not insert a row.
/// </summary>
public sealed class RequirementsRecoveryRunEntity
{
    /// <summary>Resolved workspace discriminator, normally the absolute workspace path.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Caller-supplied idempotency key, unique within the workspace.</summary>
    [Required]
    [StringLength(128)]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of the canonical recovery payload.</summary>
    [Required]
    [StringLength(64)]
    public string PayloadHash { get; set; } = string.Empty;

    /// <summary>Durable run status. Applied runs use <c>applied</c>.</summary>
    [Required]
    [StringLength(32)]
    public string Status { get; set; } = "applied";

    /// <summary>JSON of the recovery result returned to the caller.</summary>
    [Required]
    public string ResultJson { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the run row was committed.</summary>
    [Required]
    [StringLength(64)]
    public string CreatedAtUtc { get; set; } = string.Empty;
}
