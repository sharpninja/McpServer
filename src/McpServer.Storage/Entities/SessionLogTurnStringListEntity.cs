using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-CORE-013: 4NF generic string-list entity for session log entry properties.
/// Stores DesignDecisions, RequirementsDiscovered, FilesModified, and Blockers.
/// FR-SUPPORT-010: Eliminates multi-valued dependency on string-list properties.
/// </summary>
public sealed class SessionLogTurnStringListEntity
{
    /// <summary>TR-PLANNED-CORE-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-CORE-013: Foreign key to parent entry.</summary>
    public long SessionLogTurnId { get; set; }

    /// <summary>Discriminator identifying which list this item belongs to (DesignDecision, Requirement, FileModified, Blocker).</summary>
    [Required]
    [MaxLength(32)]
    public required string ListType { get; set; }

    /// <summary>Ordinal position within the list.</summary>
    public int Ordinal { get; set; }

    /// <summary>The string value of this list item.</summary>
    public required string Value { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Navigation to parent entry.</summary>
    public SessionLogTurnEntity? SessionLogTurn { get; set; }
}

