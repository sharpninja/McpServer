using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-TRIAGESTORE-001: 4NF session-scoped tag. One row per tag on a session log.
/// </summary>
public sealed class SessionLogTagEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent session.</summary>
    public long SessionLogId { get; set; }

    /// <summary>Tag value.</summary>
    [Required]
    [StringLength(256)]
    public required string Tag { get; set; }

    /// <summary>Navigation to the parent session.</summary>
    public SessionLogEntity? SessionLog { get; set; }
}
