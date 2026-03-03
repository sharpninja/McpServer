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

/// <summary>Outcome for an agent mutation command.</summary>
public sealed record AgentMutationOutcome(
    bool Success,
    string? Error);
