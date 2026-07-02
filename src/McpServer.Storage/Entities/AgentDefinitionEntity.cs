using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// EF Core entity for agent type definitions stored in the primary instance SQLite database.
/// Contains default configuration for known agent types (copilot, cline, cursor, etc.).
/// </summary>
public class AgentDefinitionEntity
{
    /// <summary>Unique agent type identifier (e.g. "copilot", "cline").</summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = "";

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    [MaxLength(128)]
    public string DisplayName { get; set; } = "";

    /// <summary>Default command to launch this agent.</summary>
    [MaxLength(512)]
    public string DefaultLaunchCommand { get; set; } = "";

    /// <summary>Default instruction/rules file path relative to workspace root.</summary>
    [MaxLength(256)]
    public string DefaultInstructionFile { get; set; } = "";

    /// <summary>4NF default-model rows (former <c>DefaultModelsJson</c>), ordered by ordinal.</summary>
    public List<AgentDefinitionModelEntity> Models { get; set; } = [];

    /// <summary>Default git branch naming strategy.</summary>
    [MaxLength(256)]
    public string DefaultBranchStrategy { get; set; } = "feature/{agent}/{task}";

    /// <summary>Default seed prompt.</summary>
    public string DefaultSeedPrompt { get; set; } = "";

    /// <summary>Whether this is a built-in (non-deletable) definition.</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>When this definition was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When this definition was last modified.</summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>Navigation: workspace configurations using this definition.</summary>
    public ICollection<AgentWorkspaceEntity> WorkspaceConfigs { get; set; } = new List<AgentWorkspaceEntity>();
}
