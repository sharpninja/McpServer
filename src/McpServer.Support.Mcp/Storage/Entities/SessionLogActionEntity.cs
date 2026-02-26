using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-013: 4NF action entity. One row per action within a session log entry.
/// FR-SUPPORT-010: Eliminates multi-valued dependency on actions.
/// </summary>
public sealed class SessionLogActionEntity
{
    /// <summary>TR-PLANNED-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-013: Foreign key to parent entry.</summary>
    public long SessionLogEntryId { get; set; }

    /// <summary>TR-PLANNED-013: Execution order within the request.</summary>
    public int Order { get; set; }

    /// <summary>TR-PLANNED-013: Human-readable description of the action.</summary>
    public string? Description { get; set; }

    /// <summary>TR-PLANNED-013: Action type (e.g. edit, create, delete).</summary>
    [MaxLength(64)]
    public string? Type { get; set; }

    /// <summary>TR-PLANNED-013: Action status (e.g. completed, failed).</summary>
    [MaxLength(64)]
    public string? Status { get; set; }

    /// <summary>TR-PLANNED-013: File path affected by this action.</summary>
    [MaxLength(1024)]
    public string? FilePath { get; set; }

    /// <summary>TR-PLANNED-013: Navigation to parent entry.</summary>
    public SessionLogEntryEntity? SessionLogEntry { get; set; }
}
