using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-013: 4NF tag entity. One row per tag on a session log entry.
/// FR-SUPPORT-010: Eliminates multi-valued dependency on tags.
/// </summary>
public sealed class SessionLogEntryTagEntity
{
    /// <summary>TR-PLANNED-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-013: Foreign key to parent entry.</summary>
    public long SessionLogEntryId { get; set; }

    /// <summary>TR-PLANNED-013: Tag value.</summary>
    [Required]
    [MaxLength(256)]
    public required string Tag { get; set; }

    /// <summary>TR-PLANNED-013: Navigation to parent entry.</summary>
    public SessionLogEntryEntity? SessionLogEntry { get; set; }
}
