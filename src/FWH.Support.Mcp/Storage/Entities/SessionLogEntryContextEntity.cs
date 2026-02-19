using System.ComponentModel.DataAnnotations;

namespace FWH.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-013: 4NF context item entity. One row per context reference on a session log entry.
/// FR-SUPPORT-010: Eliminates multi-valued dependency on context list.
/// </summary>
public sealed class SessionLogEntryContextEntity
{
    /// <summary>TR-PLANNED-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-PLANNED-013: Foreign key to parent entry.</summary>
    public long SessionLogEntryId { get; set; }

    /// <summary>TR-PLANNED-013: Ordinal position within the context list.</summary>
    public int Ordinal { get; set; }

    /// <summary>TR-PLANNED-013: Context item value (path, URL, or reference).</summary>
    [Required]
    [MaxLength(2048)]
    public required string ContextItem { get; set; }

    /// <summary>TR-PLANNED-013: Navigation to parent entry.</summary>
    public SessionLogEntryEntity? SessionLogEntry { get; set; }
}
