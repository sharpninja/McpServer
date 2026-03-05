using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// EF Core entity for per-workspace agent configuration.
/// Links an <see cref="AgentDefinitionEntity"/> to a specific workspace with optional overrides.
/// </summary>
public class AgentWorkspaceEntity
{
    /// <summary>Auto-increment primary key.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Agent type identifier (FK to <see cref="AgentDefinitionEntity"/>).</summary>
    [MaxLength(64)]
    public string AgentDefinitionId { get; set; } = "";

    /// <summary>Absolute workspace path.</summary>
    [MaxLength(1024)]
    public string WorkspacePath { get; set; } = "";

    /// <summary>Whether this agent is enabled in the workspace.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Whether this agent is banned from the workspace.</summary>
    public bool Banned { get; set; }

    /// <summary>Reason for banning.</summary>
    [MaxLength(512)]
    public string? BannedReason { get; set; }

    /// <summary>PR number that must be merged/closed before unbanning.</summary>
    public int? BannedUntilPr { get; set; }

    /// <summary>Isolation strategy: "worktree" or "clone".</summary>
    [MaxLength(16)]
    public string AgentIsolation { get; set; } = "worktree";

    /// <summary>Override launch command (null = use definition default).</summary>
    [MaxLength(512)]
    public string? LaunchCommandOverride { get; set; }

    /// <summary>Override models (JSON array, null = use definition default).</summary>
    public string? ModelsOverrideJson { get; set; }

    /// <summary>Override branch strategy (null = use definition default).</summary>
    [MaxLength(256)]
    public string? BranchStrategyOverride { get; set; }

    /// <summary>Override seed prompt (null = use definition default).</summary>
    public string? SeedPromptOverride { get; set; }

    /// <summary>Additional content appended to the marker file for this agent.</summary>
    public string MarkerAdditions { get; set; } = "";

    /// <summary>Override instruction files (JSON array, null = use definition default).</summary>
    public string? InstructionFilesOverrideJson { get; set; }

    /// <summary>When this agent was added to the workspace.</summary>
    public DateTime AddedAt { get; set; }

    /// <summary>When this agent was last launched in the workspace.</summary>
    public DateTime? LastLaunchedAt { get; set; }

    /// <summary>Navigation: agent definition.</summary>
    [ForeignKey(nameof(AgentDefinitionId))]
    public AgentDefinitionEntity? AgentDefinition { get; set; }
}
