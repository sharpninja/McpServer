namespace McpServer.Support.Mcp.Models;

/// <summary>DTOs for agent definitions and workspace agent configurations.</summary>

/// <summary>
/// An agent type definition with default configuration values.
/// Stored in the primary instance SQLite database.
/// </summary>
public sealed record AgentDefinitionDto
{
    /// <summary>Unique agent type identifier (e.g. "copilot", "cline", "cursor").</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Default command to launch this agent.</summary>
    public string DefaultLaunchCommand { get; init; } = "";

    /// <summary>Default instruction/rules file path relative to workspace root.</summary>
    public string DefaultInstructionFile { get; init; } = "";

    /// <summary>Default AI models this agent supports.</summary>
    public IReadOnlyList<string> DefaultModels { get; init; } = [];

    /// <summary>Default git branch strategy. Supported values are direct, feature-branch, and worktree.</summary>
    public string DefaultBranchStrategy { get; init; } = "direct";

    /// <summary>Default seed prompt injected when the agent starts a session.</summary>
    public string DefaultSeedPrompt { get; init; } = "";

    /// <summary>Whether this is a built-in (non-deletable) definition.</summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>When this definition was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>When this definition was last modified.</summary>
    public DateTime ModifiedAt { get; init; }
}

/// <summary>
/// Per-workspace agent configuration. Links an agent definition to a specific workspace
/// with optional overrides.
/// </summary>
public sealed record AgentWorkspaceConfigDto
{
    /// <summary>Database record ID.</summary>
    public int Id { get; init; }

    /// <summary>Agent type identifier (FK to <see cref="AgentDefinitionDto"/>).</summary>
    public required string AgentId { get; init; }

    /// <summary>Workspace path this agent is configured for.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Whether this agent is enabled in the workspace.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Whether this agent is banned from the workspace.</summary>
    public bool Banned { get; init; }

    /// <summary>Reason for banning (if banned).</summary>
    public string? BannedReason { get; init; }

    /// <summary>PR number that must be merged/closed before the agent is unbanned.</summary>
    public int? BannedUntilPr { get; init; }

    /// <summary>Isolation strategy: none, worktree, or clone.</summary>
    public string AgentIsolation { get; init; } = "worktree";

    /// <summary>Override launch command (null = use definition default).</summary>
    public string? LaunchCommandOverride { get; init; }

    /// <summary>Override models (null = use definition default).</summary>
    public IReadOnlyList<string>? ModelsOverride { get; init; }

    /// <summary>Override branch strategy (null = use definition default).</summary>
    public string? BranchStrategyOverride { get; init; }

    /// <summary>Override seed prompt (null = use definition default).</summary>
    public string? SeedPromptOverride { get; init; }

    /// <summary>Additional content appended to the marker file for this agent.</summary>
    public string MarkerAdditions { get; init; } = "";

    /// <summary>Override instruction files (null = use definition default).</summary>
    public IReadOnlyList<string>? InstructionFilesOverride { get; init; }

    /// <summary>Configured restart policy: never, on-failure, or always.</summary>
    public string RestartPolicy { get; init; } = "never";

    /// <summary>When this agent was added to the workspace.</summary>
    public DateTime AddedAt { get; init; }

    /// <summary>When this agent was last launched in the workspace.</summary>
    public DateTime? LastLaunchedAt { get; init; }
}

/// <summary>Agent lifecycle event types.</summary>
public enum AgentEventType
{
    /// <summary>Agent was added to a workspace.</summary>
    Add,
    /// <summary>Agent was launched.</summary>
    Launch,
    /// <summary>Agent process exited.</summary>
    Exit,
    /// <summary>Agent was banned.</summary>
    Ban,
    /// <summary>Agent was unbanned.</summary>
    Unban,
    /// <summary>Agent was deleted from a workspace.</summary>
    Delete,
    /// <summary>A merge PR was created for the agent's workspace.</summary>
    Merge,
    /// <summary>Workspace was initialized for agents.</summary>
    Init
}

/// <summary>Agent lifecycle event log entry.</summary>
public sealed record AgentEventDto
{
    /// <summary>Event ID.</summary>
    public long Id { get; init; }

    /// <summary>Agent type identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Workspace path.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Type of event.</summary>
    public required AgentEventType EventType { get; init; }

    /// <summary>User ID from JWT sub claim.</summary>
    public string? UserId { get; init; }

    /// <summary>Additional event details (JSON).</summary>
    public string? Details { get; init; }

    /// <summary>When the event occurred.</summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>Request to create or update an agent definition.</summary>
public sealed record AgentDefinitionRequest
{
    /// <summary>Unique agent type identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Default command to launch this agent.</summary>
    public string DefaultLaunchCommand { get; init; } = "";

    /// <summary>Default instruction/rules file path relative to workspace root.</summary>
    public string DefaultInstructionFile { get; init; } = "";

    /// <summary>Default AI models this agent supports.</summary>
    public IReadOnlyList<string> DefaultModels { get; init; } = [];

    /// <summary>Default branch strategy (direct, feature-branch, worktree).</summary>
    public string DefaultBranchStrategy { get; init; } = "direct";

    /// <summary>Default seed prompt injected when the agent starts a session.</summary>
    public string DefaultSeedPrompt { get; init; } = "";
}

/// <summary>Request to add or update an agent in a workspace.</summary>
public sealed record AgentWorkspaceRequest
{
    /// <summary>Agent type identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Whether this agent is enabled in the workspace.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Isolation strategy: none, worktree, or clone.</summary>
    public string AgentIsolation { get; init; } = "worktree";

    /// <summary>Override launch command (null = use definition default).</summary>
    public string? LaunchCommandOverride { get; init; }

    /// <summary>Override models (null = use definition default).</summary>
    public IReadOnlyList<string>? ModelsOverride { get; init; }

    /// <summary>Override branch strategy (null = use definition default).</summary>
    public string? BranchStrategyOverride { get; init; }

    /// <summary>Override seed prompt (null = use definition default).</summary>
    public string? SeedPromptOverride { get; init; }

    /// <summary>Additional content appended to the marker file for this agent.</summary>
    public string MarkerAdditions { get; init; } = "";

    /// <summary>Override instruction files (null = use definition default).</summary>
    public IReadOnlyList<string>? InstructionFilesOverride { get; init; }

    /// <summary>Restart policy for the runtime process.</summary>
    public string RestartPolicy { get; init; } = "never";
}

/// <summary>Request to ban an agent.</summary>
public sealed record AgentBanRequest
{
    /// <summary>Reason for banning the agent.</summary>
    public string? Reason { get; init; }

    /// <summary>PR number that must be merged/closed before the agent is unbanned.</summary>
    public int? BannedUntilPr { get; init; }

    /// <summary>Whether to ban the agent globally across all workspaces.</summary>
    public bool Global { get; init; }
}

/// <summary>Request to log an agent lifecycle event.</summary>
public sealed record AgentEventRequest
{
    /// <summary>Agent type identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Type of lifecycle event.</summary>
    public required AgentEventType EventType { get; init; }

    /// <summary>Additional event details (JSON).</summary>
    public string? Details { get; init; }
}

/// <summary>Result of an agent mutation operation.</summary>
public sealed record AgentMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Error message if the operation failed.</summary>
    public string? Error { get; init; }
}

/// <summary>Result of listing agent definitions.</summary>
public sealed record AgentDefinitionListResult
{
    /// <summary>Agent definitions.</summary>
    public IReadOnlyList<AgentDefinitionDto> Items { get; init; } = [];

    /// <summary>Total count of definitions.</summary>
    public int TotalCount { get; init; }
}

/// <summary>Result of listing workspace agent configs.</summary>
public sealed record AgentWorkspaceListResult
{
    /// <summary>Workspace agent configurations.</summary>
    public IReadOnlyList<AgentWorkspaceConfigDto> Items { get; init; } = [];

    /// <summary>Total count of configurations.</summary>
    public int TotalCount { get; init; }
}

/// <summary>Result of listing agent events.</summary>
public sealed record AgentEventListResult
{
    /// <summary>Agent lifecycle events.</summary>
    public IReadOnlyList<AgentEventDto> Items { get; init; } = [];

    /// <summary>Total count of events.</summary>
    public int TotalCount { get; init; }
}
