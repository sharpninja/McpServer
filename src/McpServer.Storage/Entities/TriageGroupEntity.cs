using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TRIAGE-001 and TR-MCP-TRIAGE-002: Durable grouped triage report aggregate.
/// </summary>
public sealed class TriageGroupEntity
{
    /// <summary>Workspace discriminator used by MCP multi-tenant filters.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Durable triage group id.</summary>
    [Key]
    [StringLength(128)]
    public required string GroupId { get; set; }

    /// <summary>Deterministic workspace-scoped grouping key.</summary>
    [Required]
    [StringLength(128)]
    public required string GroupKey { get; set; }

    /// <summary>Effective workspace path used for grouping.</summary>
    [Required]
    [StringLength(1024)]
    public required string EffectiveWorkspacePath { get; set; }

    /// <summary>Representative title.</summary>
    [Required]
    [StringLength(512)]
    public required string Title { get; set; }

    /// <summary>Representative summary.</summary>
    [Required]
    public required string Summary { get; set; }

    /// <summary>Group status.</summary>
    [Required]
    [StringLength(64)]
    public required string Status { get; set; }

    /// <summary>Number of reports currently attached to the group.</summary>
    public int ReportCount { get; set; }

    /// <summary>UTC timestamp of first report.</summary>
    public DateTimeOffset FirstReportAtUtc { get; set; }

    /// <summary>UTC timestamp of latest report.</summary>
    public DateTimeOffset LastReportAtUtc { get; set; }

    /// <summary>UTC quiet deadline.</summary>
    public DateTimeOffset QuietDeadlineUtc { get; set; }

    /// <summary>Whether the report was detected as MCP Server core or plugin related.</summary>
    public bool IsMcpServerRelated { get; set; }

    /// <summary>Created backlog TODO id when research succeeds.</summary>
    [StringLength(128)]
    public string? CreatedTodoId { get; set; }

    /// <summary>Inspectable latest failure state.</summary>
    public string? LastError { get; set; }
}
