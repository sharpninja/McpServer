using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>TR-HANDOFF-AUDIT-001: Normalized diagnostic row for a handoff run.</summary>
public sealed class HandoffDiagnosticEntity
{
    /// <summary>Surrogate key.</summary>
    [Key]
    public long DiagnosticId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public required string WorkspaceId { get; set; }

    /// <summary>Parent run identifier.</summary>
    [Required]
    [StringLength(128)]
    public required string RunId { get; set; }

    /// <summary>Stable diagnostic code.</summary>
    [Required]
    [StringLength(128)]
    public required string Code { get; set; }

    /// <summary>Severity name.</summary>
    [Required]
    [StringLength(16)]
    public required string Severity { get; set; }

    /// <summary>Field name when the diagnostic is field-specific.</summary>
    [StringLength(64)]
    public string? Field { get; set; }

    /// <summary>Message. Must not include raw source content or credentials.</summary>
    [Required]
    public required string Message { get; set; }

    /// <summary>Display order within the run.</summary>
    public int Ordinal { get; set; }

    /// <summary>Parent run.</summary>
    [ForeignKey(nameof(RunId))]
    public HandoffIngestionRunEntity? Run { get; set; }
}
