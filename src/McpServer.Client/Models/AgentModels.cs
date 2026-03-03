using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// Result payload for seeding built-in agent definitions.
/// </summary>
public sealed class AgentSeedDefaultsResult
{
    /// <summary>
    /// Number of seeded definitions.
    /// </summary>
    [JsonPropertyName("seeded")]
    public int Seeded { get; set; }
}

/// <summary>
/// Agent-definition DTO.
/// </summary>
public sealed class AgentDefinition
{
    /// <summary>
    /// Unique agent type identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Default launch command.
    /// </summary>
    [JsonPropertyName("defaultLaunchCommand")]
    public string DefaultLaunchCommand { get; set; } = string.Empty;

    /// <summary>
    /// Default instruction/rules file path.
    /// </summary>
    [JsonPropertyName("defaultInstructionFile")]
    public string DefaultInstructionFile { get; set; } = string.Empty;

    /// <summary>
    /// Default model identifiers.
    /// </summary>
    [JsonPropertyName("defaultModels")]
    public IReadOnlyList<string> DefaultModels { get; set; } = [];

    /// <summary>
    /// Default branch strategy.
    /// </summary>
    [JsonPropertyName("defaultBranchStrategy")]
    public string DefaultBranchStrategy { get; set; } = string.Empty;

    /// <summary>
    /// Default seed prompt.
    /// </summary>
    [JsonPropertyName("defaultSeedPrompt")]
    public string DefaultSeedPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Whether definition is built-in.
    /// </summary>
    [JsonPropertyName("isBuiltIn")]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// UTC creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC last-modified timestamp.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// Request payload to upsert an agent definition.
/// </summary>
public sealed class AgentDefinitionRequest
{
    /// <summary>
    /// Unique agent type identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Default launch command.
    /// </summary>
    [JsonPropertyName("defaultLaunchCommand")]
    public string DefaultLaunchCommand { get; set; } = string.Empty;

    /// <summary>
    /// Default instruction/rules file path.
    /// </summary>
    [JsonPropertyName("defaultInstructionFile")]
    public string DefaultInstructionFile { get; set; } = string.Empty;

    /// <summary>
    /// Default model identifiers.
    /// </summary>
    [JsonPropertyName("defaultModels")]
    public IReadOnlyList<string> DefaultModels { get; set; } = [];

    /// <summary>
    /// Default branch strategy.
    /// </summary>
    [JsonPropertyName("defaultBranchStrategy")]
    public string DefaultBranchStrategy { get; set; } = "feature/{agent}/{task}";

    /// <summary>
    /// Default seed prompt.
    /// </summary>
    [JsonPropertyName("defaultSeedPrompt")]
    public string DefaultSeedPrompt { get; set; } = string.Empty;
}

/// <summary>
/// Result payload for listing agent definitions.
/// </summary>
public sealed class AgentDefinitionListResult
{
    /// <summary>
    /// Agent definitions.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<AgentDefinition> Items { get; set; } = [];

    /// <summary>
    /// Total count.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>
/// Workspace-scoped agent configuration DTO.
/// </summary>
public sealed class AgentWorkspaceConfig
{
    /// <summary>
    /// Database row ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Agent identifier.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Workspace path.
    /// </summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this agent is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether this agent is banned.
    /// </summary>
    [JsonPropertyName("banned")]
    public bool Banned { get; set; }

    /// <summary>
    /// Optional ban reason.
    /// </summary>
    [JsonPropertyName("bannedReason")]
    public string? BannedReason { get; set; }

    /// <summary>
    /// Optional PR number required before unban.
    /// </summary>
    [JsonPropertyName("bannedUntilPr")]
    public int? BannedUntilPr { get; set; }

    /// <summary>
    /// Agent-isolation mode (for example, worktree/clone).
    /// </summary>
    [JsonPropertyName("agentIsolation")]
    public string AgentIsolation { get; set; } = "worktree";

    /// <summary>
    /// Optional launch-command override.
    /// </summary>
    [JsonPropertyName("launchCommandOverride")]
    public string? LaunchCommandOverride { get; set; }

    /// <summary>
    /// Optional model override list.
    /// </summary>
    [JsonPropertyName("modelsOverride")]
    public IReadOnlyList<string>? ModelsOverride { get; set; }

    /// <summary>
    /// Optional branch-strategy override.
    /// </summary>
    [JsonPropertyName("branchStrategyOverride")]
    public string? BranchStrategyOverride { get; set; }

    /// <summary>
    /// Optional seed-prompt override.
    /// </summary>
    [JsonPropertyName("seedPromptOverride")]
    public string? SeedPromptOverride { get; set; }

    /// <summary>
    /// Marker additions.
    /// </summary>
    [JsonPropertyName("markerAdditions")]
    public string MarkerAdditions { get; set; } = string.Empty;

    /// <summary>
    /// Optional instruction-file override list.
    /// </summary>
    [JsonPropertyName("instructionFilesOverride")]
    public IReadOnlyList<string>? InstructionFilesOverride { get; set; }

    /// <summary>
    /// UTC timestamp when added.
    /// </summary>
    [JsonPropertyName("addedAt")]
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// UTC timestamp for last launch.
    /// </summary>
    [JsonPropertyName("lastLaunchedAt")]
    public DateTime? LastLaunchedAt { get; set; }
}

/// <summary>
/// Request payload to upsert a workspace agent configuration.
/// </summary>
public sealed class AgentWorkspaceRequest
{
    /// <summary>
    /// Agent identifier.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this agent should be enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Agent-isolation mode (for example, worktree/clone).
    /// </summary>
    [JsonPropertyName("agentIsolation")]
    public string AgentIsolation { get; set; } = "worktree";

    /// <summary>
    /// Optional launch-command override.
    /// </summary>
    [JsonPropertyName("launchCommandOverride")]
    public string? LaunchCommandOverride { get; set; }

    /// <summary>
    /// Optional model override list.
    /// </summary>
    [JsonPropertyName("modelsOverride")]
    public IReadOnlyList<string>? ModelsOverride { get; set; }

    /// <summary>
    /// Optional branch-strategy override.
    /// </summary>
    [JsonPropertyName("branchStrategyOverride")]
    public string? BranchStrategyOverride { get; set; }

    /// <summary>
    /// Optional seed-prompt override.
    /// </summary>
    [JsonPropertyName("seedPromptOverride")]
    public string? SeedPromptOverride { get; set; }

    /// <summary>
    /// Marker additions.
    /// </summary>
    [JsonPropertyName("markerAdditions")]
    public string MarkerAdditions { get; set; } = string.Empty;

    /// <summary>
    /// Optional instruction-file override list.
    /// </summary>
    [JsonPropertyName("instructionFilesOverride")]
    public IReadOnlyList<string>? InstructionFilesOverride { get; set; }
}

/// <summary>
/// Result payload for listing workspace-agent configurations.
/// </summary>
public sealed class AgentWorkspaceListResult
{
    /// <summary>
    /// Workspace-agent configurations.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<AgentWorkspaceConfig> Items { get; set; } = [];

    /// <summary>
    /// Total count.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>
/// Request payload for banning an agent.
/// </summary>
public sealed class AgentBanRequest
{
    /// <summary>
    /// Optional ban reason.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Optional PR number required before unban.
    /// </summary>
    [JsonPropertyName("bannedUntilPr")]
    public int? BannedUntilPr { get; set; }

    /// <summary>
    /// Whether the ban applies globally.
    /// </summary>
    [JsonPropertyName("global")]
    public bool Global { get; set; }
}

/// <summary>
/// Request payload for logging an agent lifecycle event.
/// </summary>
public sealed class AgentEventRequest
{
    /// <summary>
    /// Agent identifier.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Numeric lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public int EventType { get; set; }

    /// <summary>
    /// Optional event details.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Logged agent lifecycle event.
/// </summary>
public sealed class AgentEvent
{
    /// <summary>
    /// Event identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Agent identifier.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Workspace path.
    /// </summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>
    /// Numeric lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public int EventType { get; set; }

    /// <summary>
    /// Optional user identifier.
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Optional details payload.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>
    /// UTC timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result payload for listing agent events.
/// </summary>
public sealed class AgentEventListResult
{
    /// <summary>
    /// Event list.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<AgentEvent> Items { get; set; } = [];

    /// <summary>
    /// Total count.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>
/// Result payload for agent mutation operations.
/// </summary>
public sealed class AgentMutationResult
{
    /// <summary>
    /// Whether the mutation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message when the mutation fails.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Result payload for agents.yaml validation.
/// </summary>
public sealed class AgentValidateResult
{
    /// <summary>
    /// Whether the file is valid.
    /// </summary>
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    /// <summary>
    /// Optional validation error.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Resolved path to agents.yaml.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }
}
