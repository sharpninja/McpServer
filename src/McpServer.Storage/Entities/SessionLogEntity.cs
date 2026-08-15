using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-CORE-013: 4NF session log entity. One row per workspace session, keyed by (WorkspaceId, SourceType, SessionId).
/// FR-SUPPORT-010: Persisted in MCP SQLite database for session log normalization.
/// </summary>
public sealed class SessionLogEntity
{
    /// <summary>TR-PLANNED-CORE-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-CORE-013: Agent source type (e.g. Cursor, Copilot). Unique with WorkspaceId and SessionId.</summary>
    [Required]
    [StringLength(64)]
    public required string SourceType { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Unique session identifier within the workspace and source type.</summary>
    [Required]
    [StringLength(256)]
    public required string SessionId { get; set; }

    /// <summary>Optional foreign key link to a known agent definition.</summary>
    [StringLength(64)]
    public string? AgentDefinitionId { get; set; }

    /// <summary>Optional navigation to the linked agent definition.</summary>
    [ForeignKey(nameof(AgentDefinitionId))]
    public AgentDefinitionEntity? AgentDefinition { get; set; }

    /// <summary>Provider-native agent session identifier captured in the session header.</summary>
    [StringLength(256)]
    public string? AgentSessionId { get; set; }

    /// <summary>Provider-native transcript file path captured in the session header.</summary>
    [StringLength(2048)]
    public string? AgentSessionTranscriptFile { get; set; }

    /// <summary>Agent executable path captured in the session header.</summary>
    [StringLength(2048)]
    public string? AgentExecutablePath { get; set; }

    /// <summary>Agent executable version captured in the session header.</summary>
    [StringLength(128)]
    public string? AgentExecutableVersion { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Human-readable session title.</summary>
    [StringLength(1024)]
    public string? Title { get; set; }

    /// <summary>TR-PLANNED-CORE-013: AI model used for the session.</summary>
    [StringLength(128)]
    public string? Model { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Session start timestamp (UTC).</summary>
    public DateTimeOffset? Started { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Last update timestamp (UTC).</summary>
    public DateTimeOffset? LastUpdated { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Session status (e.g. completed, in_progress).</summary>
    [StringLength(64)]
    public string? Status { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Number of request/response turns.</summary>
    [Column("EntryCount")]
    public int TurnCount { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Total token count across all turns.</summary>
    public int? TotalTokens { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Cursor-specific session label.</summary>
    [StringLength(512)]
    public string? CursorSessionLabel { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Average success score across turns.</summary>
    public double? CopilotAvgSuccessScore { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Total net tokens used.</summary>
    public int? CopilotTotalNetTokens { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Total net premium requests.</summary>
    public int? CopilotTotalNetPremiumRequests { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Number of completed turns.</summary>
    public int? CopilotCompletedCount { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Number of in-progress turns.</summary>
    public int? CopilotInProgressCount { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Project name from workspace.</summary>
    [StringLength(256)]
    public string? Project { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Target framework from workspace.</summary>
    [StringLength(64)]
    public string? TargetFramework { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Repository URL or name from workspace.</summary>
    [StringLength(512)]
    public string? Repository { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Git branch name from workspace.</summary>
    [StringLength(256)]
    public string? Branch { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Full path to the source JSON file that was imported.</summary>
    [StringLength(2048)]
    public string? SourceFilePath { get; set; }

    /// <summary>TR-PLANNED-CORE-013: SHA-256 hash of the source file content at the time the record was last updated. Used to skip unchanged files during sync.</summary>
    [StringLength(64)]
    public string? ContentHash { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Navigation to session log turns.</summary>
    public ICollection<SessionLogTurnEntity> Turns { get; } = new List<SessionLogTurnEntity>();
}
