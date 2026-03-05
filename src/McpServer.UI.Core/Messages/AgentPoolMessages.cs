using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list pooled runtime agents.</summary>
public sealed record ListAgentPoolAgentsQuery : IQuery<ListAgentPoolAgentsResult>;

/// <summary>Result of listing pooled runtime agents.</summary>
public sealed record ListAgentPoolAgentsResult(IReadOnlyList<AgentPoolRuntimeAgentSnapshot> Items, int TotalCount);

/// <summary>List snapshot for a pooled runtime agent.</summary>
public sealed record AgentPoolRuntimeAgentSnapshot(
    string AgentName,
    string Lifecycle,
    string? SessionId,
    string? ActiveJobId,
    int ActiveVoiceLinks,
    string? WorkspacePath);

/// <summary>Query to list pooled queue items.</summary>
public sealed record ListAgentPoolQueueQuery : IQuery<ListAgentPoolQueueResult>;

/// <summary>Result of listing pooled queue items.</summary>
public sealed record ListAgentPoolQueueResult(IReadOnlyList<AgentPoolQueueItemSnapshot> Items, int TotalCount);

/// <summary>Queue-item snapshot from pooled runtime.</summary>
public sealed record AgentPoolQueueItemSnapshot(
    string JobId,
    string? AgentName,
    string Status,
    string? Context,
    string? RenderedPrompt,
    string? WorkspacePath);

/// <summary>Command to start a pooled runtime agent.</summary>
public sealed record StartAgentPoolAgentCommand(string AgentName) : ICommand<AgentPoolMutationOutcome>;

/// <summary>Command to stop a pooled runtime agent.</summary>
public sealed record StopAgentPoolAgentCommand(string AgentName) : ICommand<AgentPoolMutationOutcome>;

/// <summary>Command to recycle a pooled runtime agent.</summary>
public sealed record RecycleAgentPoolAgentCommand(string AgentName) : ICommand<AgentPoolMutationOutcome>;

/// <summary>Command to connect to a pooled runtime agent session.</summary>
public sealed record ConnectAgentPoolAgentCommand(string AgentName) : ICommand<AgentPoolConnectOutcome>;

/// <summary>Command to cancel a pooled queue item.</summary>
public sealed record CancelAgentPoolQueueItemCommand(string JobId) : ICommand<AgentPoolMutationOutcome>;

/// <summary>Command to remove a pooled queue item.</summary>
public sealed record RemoveAgentPoolQueueItemCommand(string JobId) : ICommand<AgentPoolMutationOutcome>;

/// <summary>Command to move a pooled queue item up.</summary>
public sealed record MoveAgentPoolQueueItemUpCommand(string JobId) : ICommand<AgentPoolMutationOutcome>;

/// <summary>Command to move a pooled queue item down.</summary>
public sealed record MoveAgentPoolQueueItemDownCommand(string JobId) : ICommand<AgentPoolMutationOutcome>;

/// <summary>Prompt contexts supported by pooled one-shot enqueue/resolve operations.</summary>
public enum AgentPoolPromptContext
{
    /// <summary>Ad-hoc freeform prompt.</summary>
    AdHoc,
    /// <summary>TODO status prompt.</summary>
    Status,
    /// <summary>TODO implementation prompt.</summary>
    Implement,
    /// <summary>TODO planning prompt.</summary>
    Plan
}

/// <summary>Request payload for pooled one-shot resolve/enqueue operations.</summary>
public sealed record AgentPoolEnqueueDraft
{
    /// <summary>Optional target agent name. Null/empty allows auto-selection.</summary>
    public string? AgentName { get; init; }

    /// <summary>Prompt context discriminator.</summary>
    public AgentPoolPromptContext Context { get; init; } = AgentPoolPromptContext.AdHoc;

    /// <summary>Prompt text to process.</summary>
    public required string PromptText { get; init; }

    /// <summary>Whether workspace context should be included by the backend.</summary>
    public bool UseWorkspaceContext { get; init; } = true;
}

/// <summary>Command to resolve prompt text without enqueueing.</summary>
public sealed record ResolveAgentPoolPromptCommand(AgentPoolEnqueueDraft Request) : ICommand<AgentPoolPromptResolutionOutcome>;

/// <summary>Command to enqueue a pooled one-shot request.</summary>
public sealed record EnqueueAgentPoolPromptCommand(AgentPoolEnqueueDraft Request) : ICommand<AgentPoolEnqueueOutcome>;

/// <summary>Outcome for pooled mutation commands.</summary>
public sealed record AgentPoolMutationOutcome(
    bool Success,
    string? Error);

/// <summary>Outcome for pooled connect operation.</summary>
public sealed record AgentPoolConnectOutcome(
    bool Success,
    string? Error,
    string? SessionId,
    int? SshPort,
    int? AgentPort,
    string? AgentName);

/// <summary>Outcome for pooled prompt-resolution operation.</summary>
public sealed record AgentPoolPromptResolutionOutcome(
    bool Success,
    string? Error,
    string? PromptText,
    bool IsDefaultPrompt,
    bool IsGlobalPrompt,
    bool IsWorkspacePrompt);

/// <summary>Outcome for pooled one-shot enqueue operation.</summary>
public sealed record AgentPoolEnqueueOutcome(
    bool Success,
    string? Error,
    string? JobId,
    string? AgentName);
