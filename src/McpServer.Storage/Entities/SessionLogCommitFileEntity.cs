using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-CORE-013: 4NF file-changed entity. One row per file path changed in a commit.
/// FR-SUPPORT-010: Eliminates the multi-valued dependency previously stored as
/// <c>SessionLogCommitEntity.FilesChangedJson</c>.
/// </summary>
public sealed class SessionLogCommitFileEntity
{
    /// <summary>TR-PLANNED-CORE-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-CORE-013: Foreign key to the parent commit.</summary>
    public long SessionLogCommitId { get; set; }

    /// <summary>Ordinal position within the commit's changed-file list.</summary>
    public int Ordinal { get; set; }

    /// <summary>Changed file path.</summary>
    public required string Path { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Navigation to the parent commit.</summary>
    public SessionLogCommitEntity? SessionLogCommit { get; set; }
}
