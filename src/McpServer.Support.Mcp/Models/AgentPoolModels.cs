namespace McpServer.Support.Mcp.Models;

/// <summary>
/// FR-MCP-055: One-shot request context values.
/// </summary>
public enum AgentPoolOneShotContext
{
    /// <summary>TODO planning context.</summary>
    Plan,

    /// <summary>TODO status reporting context.</summary>
    Status,

    /// <summary>TODO implementation context.</summary>
    Implement,

    /// <summary>Ad-hoc non-template context.</summary>
    AdHoc,
}

/// <summary>
/// FR-MCP-056: One-shot enqueue request payload.
/// </summary>
public sealed record AgentPoolOneShotRequest
{
    /// <summary>
    /// Optional explicit pooled agent name. When omitted, intent-default routing is used.
    /// </summary>
    public string? AgentName { get; init; }

    /// <summary>
    /// Context value for default routing and context-template resolution.
    /// </summary>
    public AgentPoolOneShotContext? Context { get; init; }

    /// <summary>
    /// Optional explicit prompt template identifier.
    /// </summary>
    public string? PromptTemplateId { get; init; }

    /// <summary>
    /// Optional explicit ad-hoc prompt text.
    /// </summary>
    public string? PromptText { get; init; }

    /// <summary>
    /// Optional request id bound to <c>{id}</c> template variables.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Optional caller variables used for template rendering.
    /// </summary>
    public Dictionary<string, object?>? Values { get; init; }

    /// <summary>
    /// When <see langword="true"/>, workspace/context defaults are merged into template variables.
    /// </summary>
    public bool UseWorkspaceContext { get; init; } = true;
}

/// <summary>
/// Represents one queued/processing/completed one-shot item.
/// </summary>
public sealed record AgentPoolQueueItemDto
{
    /// <summary>Queue item identifier.</summary>
    public required string JobId { get; init; }

    /// <summary>Assigned or target agent name.</summary>
    public string? AgentName { get; init; }

    /// <summary>Queue status.</summary>
    public required string Status { get; init; }

    /// <summary>Context value for the one-shot request.</summary>
    public AgentPoolOneShotContext? Context { get; init; }

    /// <summary>Prompt template id used, when applicable.</summary>
    public string? PromptTemplateId { get; init; }

    /// <summary>Rendered prompt text sent to the pooled agent.</summary>
    public string? RenderedPrompt { get; init; }

    /// <summary>Assistant output text for terminal states.</summary>
    public string? ResponseText { get; init; }

    /// <summary>Error text for failed states.</summary>
    public string? Error { get; init; }

    /// <summary>Creation timestamp in UTC.</summary>
    public required DateTimeOffset CreatedUtc { get; init; }

    /// <summary>Processing start timestamp in UTC.</summary>
    public DateTimeOffset? StartedUtc { get; init; }

    /// <summary>Completion timestamp in UTC.</summary>
    public DateTimeOffset? CompletedUtc { get; init; }

    /// <summary>Associated interactive voice session identifier.</summary>
    public string? SessionId { get; init; }
}

/// <summary>
/// Snapshot of pooled agent runtime state.
/// </summary>
public sealed record AgentPoolAgentStatusDto
{
    /// <summary>Configured pooled agent name.</summary>
    public required string AgentName { get; init; }

    /// <summary>Current lifecycle status.</summary>
    public required string Lifecycle { get; init; }

    /// <summary>Current interactive session id.</summary>
    public string? SessionId { get; init; }

    /// <summary>Active queue job id, when processing.</summary>
    public string? ActiveJobId { get; init; }

    /// <summary>Last submitted request prompt.</summary>
    public string? LastRequestPrompt { get; init; }

    /// <summary>Current linked interactive voice count.</summary>
    public int ActiveVoiceLinks { get; init; }

    /// <summary>Current read-only subscriber count.</summary>
    public int ReadOnlySubscribers { get; init; }

    /// <summary>Indicates this agent is interactive-default.</summary>
    public bool IsInteractiveDefault { get; init; }

    /// <summary>Indicates this agent is plan-default.</summary>
    public bool IsTodoPlanDefault { get; init; }

    /// <summary>Indicates this agent is status-default.</summary>
    public bool IsTodoStatusDefault { get; init; }

    /// <summary>Indicates this agent is implement-default.</summary>
    public bool IsTodoImplementDefault { get; init; }
}

/// <summary>
/// Mutation result for queue and lifecycle operations.
/// </summary>
public record AgentPoolMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Error message for failed operations.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Response for one-shot enqueue requests.
/// </summary>
public sealed record AgentPoolEnqueueResult : AgentPoolMutationResult
{
    /// <summary>Queue item identifier.</summary>
    public string? JobId { get; init; }

    /// <summary>Resolved agent name.</summary>
    public string? AgentName { get; init; }

    /// <summary>Resolved prompt text.</summary>
    public string? RenderedPrompt { get; init; }
}

/// <summary>
/// Response for interactive connect requests.
/// </summary>
public sealed record AgentPoolConnectResult : AgentPoolMutationResult
{
    /// <summary>Resolved pooled agent name.</summary>
    public string? AgentName { get; init; }

    /// <summary>Interactive session identifier.</summary>
    public string? SessionId { get; init; }
}

/// <summary>
/// Notification payload emitted on agent/queue transitions.
/// </summary>
public sealed record AgentPoolNotificationEventDto
{
    /// <summary>Event type (e.g. queued, processing, completed, failed).</summary>
    public required string EventType { get; init; }

    /// <summary>Agent name.</summary>
    public string? AgentName { get; init; }

    /// <summary>Queue job id when applicable.</summary>
    public string? JobId { get; init; }

    /// <summary>Session id associated with agent work.</summary>
    public string? SessionId { get; init; }

    /// <summary>Last request prompt snapshot.</summary>
    public string? LastRequestPrompt { get; init; }

    /// <summary>Timestamp in UTC.</summary>
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Optional informational message.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// Event payload for per-job read-only streams.
/// </summary>
public sealed record AgentPoolJobStreamEventDto
{
    /// <summary>Queue job id.</summary>
    public required string JobId { get; init; }

    /// <summary>Event type (queued, processing, chunk, completed, failed, canceled).</summary>
    public required string EventType { get; init; }

    /// <summary>Status text associated with this event.</summary>
    public string? Status { get; init; }

    /// <summary>Text payload (response chunk or final response).</summary>
    public string? Text { get; init; }

    /// <summary>Error payload for failure events.</summary>
    public string? Error { get; init; }

    /// <summary>Timestamp in UTC.</summary>
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Prompt resolution result for one-shot requests.
/// </summary>
public sealed record AgentPoolPromptResolutionResult : AgentPoolMutationResult
{
    /// <summary>The fully populated prompt text.</summary>
    public string? PromptText { get; init; }

    /// <summary>Resolved template identifier when template mode is used.</summary>
    public string? TemplateId { get; init; }

    /// <summary>Indicates whether template resolution was used.</summary>
    public bool TemplateResolved { get; init; }
}
