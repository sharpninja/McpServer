using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-QUAD-001 and TR-MCP-QUAD-003: Durable audit row for a brain-slot invocation attempt.
/// </summary>
public sealed class BrainSlotInvocationEntity
{
    /// <summary>Stable invocation identifier.</summary>
    [Key]
    [StringLength(128)]
    public string InvocationId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Workspace discriminator.</summary>
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Invoked slot identifier.</summary>
    [Required]
    [StringLength(128)]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Quad role served by the slot.</summary>
    [Required]
    [StringLength(64)]
    public string Role { get; set; } = string.Empty;

    /// <summary>Provider kind used for the call.</summary>
    [Required]
    [StringLength(64)]
    public string ProviderKind { get; set; } = string.Empty;

    /// <summary>Provider model identifier used for the call.</summary>
    [Required]
    [StringLength(256)]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Invocation status.</summary>
    [Required]
    [StringLength(64)]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code.</summary>
    [Required]
    [StringLength(128)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Optional session-log turn identifier.</summary>
    [StringLength(256)]
    public string? TurnId { get; set; }

    /// <summary>Transaction identifier used for commit admission.</summary>
    [StringLength(128)]
    public string? TransactionId { get; set; }

    /// <summary>Committed diffgram identifier, when available.</summary>
    [StringLength(128)]
    public string? DiffgramId { get; set; }

    /// <summary>SHA-256 hash of the input prompt.</summary>
    [StringLength(64)]
    public string? PromptSha256 { get; set; }

    /// <summary>SHA-256 hash of the external model output.</summary>
    [StringLength(64)]
    public string? OutputSha256 { get; set; }

    /// <summary>Whether GraphRAG/context admission was requested.</summary>
    public bool AdmitToGraphRag { get; set; }

    /// <summary>UTC invocation start timestamp.</summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>UTC invocation completion timestamp.</summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>Sanitized metadata JSON for audit correlation.</summary>
    public string? MetadataJson { get; set; }
}
