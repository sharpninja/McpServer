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

    /// <summary>Agent type identifier.</summary>
    [MaxLength(64)]
    public string AgentId { get; set; } = "";

    /// <summary>Workspace path where the event occurred.</summary>
    [MaxLength(1024)]
    public string WorkspacePath { get; set; } = "";

    /// <summary>Type of event (Add, Launch, Exit, Ban, Unban, Delete, Merge, Init).</summary>
    [MaxLength(32)]
    public string EventType { get; set; } = "";

    /// <summary>User ID from JWT sub claim (who triggered the event).</summary>
    [MaxLength(256)]
    public string? UserId { get; set; }

    /// <summary>Additional event details (JSON).</summary>
    public string? DetailsJson { get; set; }

    /// <summary>When the event occurred.</summary>
    public DateTime Timestamp { get; set; }
}
