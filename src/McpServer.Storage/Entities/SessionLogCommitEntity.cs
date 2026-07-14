using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-CORE-013: 4NF commit entity. One row per git commit within a session log entry.
/// FR-SUPPORT-010: Eliminates multi-valued dependency on commits.
/// </summary>
public sealed class SessionLogCommitEntity
{
    /// <summary>TR-PLANNED-CORE-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-CORE-013: Foreign key to parent entry.</summary>
    public long SessionLogTurnId { get; set; }

    /// <summary>Ordinal position within the entry's commit list.</summary>
    public int Ordinal { get; set; }

    /// <summary>Git commit SHA hash (full or abbreviated).</summary>
    [StringLength(64)]
    public string? Sha { get; set; }

    /// <summary>Git branch name.</summary>
    [StringLength(256)]
    public string? Branch { get; set; }

    /// <summary>Commit message text.</summary>
    public string? Message { get; set; }

    /// <summary>Commit author name or email.</summary>
    [StringLength(256)]
    public string? Author { get; set; }

    /// <summary>Commit timestamp (UTC).</summary>
    public DateTimeOffset? CommitTimestamp { get; set; }

    /// <summary>TR-PLANNED-CORE-013: 4NF child rows, one per changed file path.</summary>
    public List<SessionLogCommitFileEntity> Files { get; set; } = [];

    /// <summary>TR-PLANNED-CORE-013: Navigation to parent entry.</summary>
    public SessionLogTurnEntity? SessionLogTurn { get; set; }
}

