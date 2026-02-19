using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-013: 4NF session log entity. One row per session, keyed by (SourceType, SessionId).
/// FR-SUPPORT-010: Persisted in MCP SQLite database for session log normalization.
/// </summary>
public sealed class SessionLogEntity
{
    /// <summary>TR-PLANNED-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-PLANNED-013: Agent source type (e.g. Cursor, Copilot). Unique with SessionId.</summary>
    [Required]
    [MaxLength(64)]
    public required string SourceType { get; set; }

    /// <summary>TR-PLANNED-013: Unique session identifier within the source type.</summary>
    [Required]
    [MaxLength(256)]
    public required string SessionId { get; set; }

    /// <summary>TR-PLANNED-013: Human-readable session title.</summary>
    [MaxLength(1024)]
    public string? Title { get; set; }

    /// <summary>TR-PLANNED-013: AI model used for the session.</summary>
    [MaxLength(128)]
    public string? Model { get; set; }

    /// <summary>TR-PLANNED-013: Session start timestamp (UTC).</summary>
    public DateTimeOffset? Started { get; set; }

    /// <summary>TR-PLANNED-013: Last update timestamp (UTC).</summary>
    public DateTimeOffset? LastUpdated { get; set; }

    /// <summary>TR-PLANNED-013: Session status (e.g. completed, in_progress).</summary>
    [MaxLength(64)]
    public string? Status { get; set; }

    /// <summary>TR-PLANNED-013: Number of request/response entries.</summary>
    public int EntryCount { get; set; }

    /// <summary>TR-PLANNED-013: Total token count across all entries.</summary>
    public int? TotalTokens { get; set; }

    /// <summary>TR-PLANNED-013: Cursor-specific session label.</summary>
    [MaxLength(512)]
    public string? CursorSessionLabel { get; set; }

    // Copilot statistics (inlined per plan — no separate table needed for single-valued attributes)

    /// <summary>TR-PLANNED-013: Average success score across entries.</summary>
    public double? CopilotAvgSuccessScore { get; set; }

    /// <summary>TR-PLANNED-013: Total net tokens used.</summary>
    public int? CopilotTotalNetTokens { get; set; }

    /// <summary>TR-PLANNED-013: Total net premium requests.</summary>
    public int? CopilotTotalNetPremiumRequests { get; set; }

    /// <summary>TR-PLANNED-013: Number of completed entries.</summary>
    public int? CopilotCompletedCount { get; set; }

    /// <summary>TR-PLANNED-013: Number of in-progress entries.</summary>
    public int? CopilotInProgressCount { get; set; }

    // Workspace info (inlined per plan — avoids separate table for single-valued attributes)

    /// <summary>TR-PLANNED-013: Project name from workspace.</summary>
    [MaxLength(256)]
    public string? Project { get; set; }

    /// <summary>TR-PLANNED-013: Target framework from workspace.</summary>
    [MaxLength(64)]
    public string? TargetFramework { get; set; }

    /// <summary>TR-PLANNED-013: Repository URL or name from workspace.</summary>
    [MaxLength(512)]
    public string? Repository { get; set; }

    /// <summary>TR-PLANNED-013: Git branch name from workspace.</summary>
    [MaxLength(256)]
    public string? Branch { get; set; }

    /// <summary>TR-PLANNED-013: Full path to the source JSON file that was imported.</summary>
    [MaxLength(2048)]
    public string? SourceFilePath { get; set; }

    /// <summary>TR-PLANNED-013: SHA-256 hash of the source file content at the time the record was last updated. Used to skip unchanged files during sync.</summary>
    [MaxLength(64)]
    public string? ContentHash { get; set; }

    /// <summary>TR-PLANNED-013: Navigation to session log entries.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<SessionLogEntryEntity> Entries { get; set; } = new List<SessionLogEntryEntity>();
}
