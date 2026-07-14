using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// EF Core entity for agent lifecycle event audit log.
/// Records every agent action (launch, exit, ban, merge, etc.) for audit and continuity.
/// </summary>
public class AgentEventLogEntity
{
    /// <summary>Auto-increment primary key.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Agent type identifier.</summary>
    [StringLength(64)]
    public string AgentId { get; set; } = "";

    /// <summary>Workspace path where the event occurred.</summary>
    [StringLength(1024)]
    public string WorkspacePath { get; set; } = "";

    /// <summary>Type of event (Add, Launch, Exit, Ban, Unban, Delete, Merge, Init).</summary>
    [StringLength(32)]
    public string EventType { get; set; } = "";

    /// <summary>User ID from JWT sub claim (who triggered the event).</summary>
    [StringLength(256)]
    public string? UserId { get; set; }

    /// <summary>Additional event details (JSON).</summary>
    public string? DetailsJson { get; set; }

    /// <summary>When the event occurred.</summary>
    public DateTime Timestamp { get; set; }
}
