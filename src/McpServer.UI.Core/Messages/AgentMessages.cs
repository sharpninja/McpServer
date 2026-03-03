using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list global agent definitions.</summary>
public sealed record ListAgentDefinitionsQuery : IQuery<ListAgentDefinitionsResult>;

/// <summary>Result of listing global agent definitions.</summary>
public sealed record ListAgentDefinitionsResult(IReadOnlyList<AgentDefinitionSummaryItem> Items, int TotalCount);

/// <summary>List-friendly summary for an agent definition.</summary>
public sealed record AgentDefinitionSummaryItem(
    string Id,
    string DisplayName,
    bool IsBuiltIn);

/// <summary>Query to load a single agent definition.</summary>
public sealed record GetAgentDefinitionQuery(string AgentType) : IQuery<AgentDefinitionDetail?>;

/// <summary>Detailed global agent definition.</summary>
public sealed record AgentDefinitionDetail(
    string Id,
    string DisplayName,
    string DefaultLaunchCommand,
    string DefaultInstructionFile,
    IReadOnlyList<string> DefaultModels,
    string DefaultBranchStrategy,
    string DefaultSeedPrompt,
    bool IsBuiltIn);

/// <summary>Command to create or update a global agent definition.</summary>
public sealed record UpsertAgentDefinitionCommand : ICommand<AgentMutationOutcome>
{
    /// <summary>Unique agent identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Default launch command.</summary>
    public string DefaultLaunchCommand { get; init; } = string.Empty;

    /// <summary>Default instruction file path.</summary>
    public string DefaultInstructionFile { get; init; } = string.Empty;

    /// <summary>Default model IDs.</summary>
    public IReadOnlyList<string> DefaultModels { get; init; } = [];

    /// <summary>Default branch strategy.</summary>
    public string DefaultBranchStrategy { get; init; } = "feature/{agent}/{task}";

    /// <summary>Default seed prompt.</summary>
    public string DefaultSeedPrompt { get; init; } = string.Empty;
}

/// <summary>Command to delete a global agent definition.</summary>
public sealed record DeleteAgentDefinitionCommand(string AgentType) : ICommand<AgentMutationOutcome>;

/// <summary>Command to seed built-in agent definitions.</summary>
public sealed record SeedAgentDefaultsCommand : ICommand<AgentSeedOutcome>;

/// <summary>Result payload for seeding built-in definitions.</summary>
public sealed record AgentSeedOutcome(int Seeded);

/// <summary>Query to list workspace agent configurations.</summary>
public sealed record ListWorkspaceAgentsQuery(string? WorkspacePath = null) : IQuery<ListWorkspaceAgentsResult>;

/// <summary>Result of listing workspace agent configurations.</summary>
public sealed record ListWorkspaceAgentsResult(IReadOnlyList<WorkspaceAgentItem> Items, int TotalCount);

/// <summary>Workspace agent summary item.</summary>
public sealed record WorkspaceAgentItem(
    int Id,
    string AgentId,
    string WorkspacePath,
    bool Enabled,
    bool Banned,
    string? BannedReason,
    int? BannedUntilPr,
    string AgentIsolation,
    string? LaunchCommandOverride,
    IReadOnlyList<string> ModelsOverride,
    string? BranchStrategyOverride,
    string? SeedPromptOverride,
    string MarkerAdditions,
    IReadOnlyList<string> InstructionFilesOverride,
    DateTime AddedAt,
    DateTime? LastLaunchedAt);

/// <summary>Query to get a single workspace agent configuration.</summary>
public sealed record GetWorkspaceAgentQuery(string AgentId, string? WorkspacePath = null) : IQuery<WorkspaceAgentDetail?>;

/// <summary>Detailed workspace agent configuration.</summary>
public sealed record WorkspaceAgentDetail(
    int Id,
    string AgentId,
    string WorkspacePath,
    bool Enabled,
    bool Banned,
    string? BannedReason,
    int? BannedUntilPr,
    string AgentIsolation,
    string? LaunchCommandOverride,
    IReadOnlyList<string> ModelsOverride,
    string? BranchStrategyOverride,
    string? SeedPromptOverride,
    string MarkerAdditions,
    IReadOnlyList<string> InstructionFilesOverride,
    DateTime AddedAt,
    DateTime? LastLaunchedAt);

/// <summary>Command to create or update a workspace agent configuration.</summary>
public sealed record UpsertWorkspaceAgentCommand : ICommand<AgentMutationOutcome>
{
    /// <summary>Agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Workspace path used as route query context.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Whether the workspace assignment is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Isolation mode (for example, worktree/clone).</summary>
    public string AgentIsolation { get; init; } = "worktree";

    /// <summary>Optional launch command override.</summary>
    public string? LaunchCommandOverride { get; init; }

    /// <summary>Optional model override list.</summary>
    public IReadOnlyList<string>? ModelsOverride { get; init; }

    /// <summary>Optional branch strategy override.</summary>
    public string? BranchStrategyOverride { get; init; }

    /// <summary>Optional seed prompt override.</summary>
    public string? SeedPromptOverride { get; init; }

    /// <summary>Optional marker additions.</summary>
    public string? MarkerAdditions { get; init; }

    /// <summary>Optional instruction files override list.</summary>
    public IReadOnlyList<string>? InstructionFilesOverride { get; init; }
}

/// <summary>Command to assign (upsert) an agent in a workspace.</summary>
public sealed record AssignWorkspaceAgentCommand : ICommand<AgentMutationOutcome>
{
    /// <summary>Agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Workspace path used as route query context.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Whether the workspace assignment is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Isolation mode (for example, worktree/clone).</summary>
    public string AgentIsolation { get; init; } = "worktree";
}

/// <summary>Command to delete a workspace agent configuration.</summary>
public sealed record DeleteWorkspaceAgentCommand(string AgentId, string? WorkspacePath = null) : ICommand<AgentMutationOutcome>;

/// <summary>Command to ban an agent.</summary>
public sealed record BanAgentCommand : ICommand<AgentMutationOutcome>
{
    /// <summary>Agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Optional ban reason.</summary>
    public string? Reason { get; init; }

    /// <summary>Optional PR number required before unban.</summary>
    public int? BannedUntilPr { get; init; }

    /// <summary>Whether the ban applies globally.</summary>
    public bool Global { get; init; }

    /// <summary>Optional workspace path for workspace-scoped bans.</summary>
    public string? WorkspacePath { get; init; }
}

/// <summary>Command to unban an agent.</summary>
public sealed record UnbanAgentCommand(
    string AgentId,
    string? WorkspacePath = null,
    bool Global = false) : ICommand<AgentMutationOutcome>;

/// <summary>Command to log an agent event.</summary>
public sealed record LogAgentEventCommand : ICommand<AgentMutationOutcome>
{
    /// <summary>Agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Numeric event type.</summary>
    public required int EventType { get; init; }

    /// <summary>Optional event detail payload.</summary>
    public string? Details { get; init; }

    /// <summary>Optional workspace path.</summary>
    public string? WorkspacePath { get; init; }
}

/// <summary>Query to list agent events.</summary>
public sealed record GetAgentEventsQuery(
    string AgentId,
    string? WorkspacePath = null,
    int Limit = 50) : IQuery<AgentEventsResult>;

/// <summary>Agent events list result.</summary>
public sealed record AgentEventsResult(IReadOnlyList<AgentEventItem> Items, int TotalCount);

/// <summary>Agent event item.</summary>
public sealed record AgentEventItem(
    long Id,
    string AgentId,
    string WorkspacePath,
    int EventType,
    string? UserId,
    string? Details,
    DateTime Timestamp);

/// <summary>Query to validate an agents.yaml file.</summary>
public sealed record ValidateAgentQuery(string? WorkspacePath = null) : IQuery<AgentValidateOutcome>;

/// <summary>Agent validation result.</summary>
public sealed record AgentValidateOutcome(bool Valid, string? Error, string? Path);

/// <summary>Outcome for an agent mutation command.</summary>
public sealed record AgentMutationOutcome(
    bool Success,
    string? Error);
