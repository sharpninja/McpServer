using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>TR-MCP-HOSTILEREVIEW-002: Sanitized diagnostic. Never includes raw artifact bodies.</summary>
public sealed class HostileReviewDiagnosticEntity
{
    /// <summary>Row id.</summary>
    [Key]
    public long DiagnosticId { get; set; }

    /// <summary>Parent request id.</summary>
    [Required]
    [StringLength(128)]
    public required string RequestId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public required string WorkspaceId { get; set; }

    /// <summary>Stable code.</summary>
    [Required]
    [StringLength(64)]
    public required string Code { get; set; }

    /// <summary>Human message without secret or body text.</summary>
    [Required]
    [StringLength(1024)]
    public required string Message { get; set; }

    /// <summary>Parent request.</summary>
    [ForeignKey(nameof(RequestId))]
    public HostileReviewRequestEntity? Request { get; set; }
}
