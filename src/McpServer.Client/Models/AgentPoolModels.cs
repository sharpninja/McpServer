using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>One-shot request context values for pooled agent routing.</summary>
public enum AgentPoolOneShotContext
{
    /// <summary>TODO planning context.</summary>
    Plan,

    /// <summary>TODO status context.</summary>
    Status,

    /// <summary>TODO implementation context.</summary>
    Implement,

    /// <summary>Ad-hoc prompt context.</summary>
    AdHoc,

    /// <summary>TR-HANDOFF-AGENT-001: Extract a structured MCP TODO draft from a handoff document.</summary>
    HandoffTodoDraft,
}

/// <summary>Request payload for one-shot queue and resolve operations.</summary>
public sealed class AgentPoolOneShotRequest
{
    /// <summary>Optional explicit pooled agent name.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    /// <summary>Optional workspace path for routing.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Optional context value for default routing/template resolution.</summary>
    [JsonPropertyName("context")]
    public AgentPoolOneShotContext? Context { get; set; }

    /// <summary>Optional prompt template identifier.</summary>
    [JsonPropertyName("promptTemplateId")]
    public string? PromptTemplateId { get; set; }

    /// <summary>Optional ad-hoc prompt text.</summary>
    [JsonPropertyName("promptText")]
    public string? PromptText { get; set; }

    /// <summary>Optional request identifier bound to template variables.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Optional caller-provided template variables.</summary>
    [JsonPropertyName("values")]
    public Dictionary<string, object?>? Values { get; set; }

    /// <summary>Whether workspace context variables should be merged.</summary>
    [JsonPropertyName("useWorkspaceContext")]
    public bool UseWorkspaceContext { get; set; } = true;
}

/// <summary>Snapshot of pooled runtime status for one configured agent.</summary>
public sealed class AgentPoolAgentStatus
{
    /// <summary>Pooled agent name.</summary>
    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>Workspace path this agent instance is scoped to.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Lifecycle status (offline, starting, idle, busy, stopping, error).</summary>
    [JsonPropertyName("lifecycle")]
    public string Lifecycle { get; set; } = string.Empty;

    /// <summary>Current interactive voice session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>Active queue job id when busy.</summary>
    [JsonPropertyName("activeJobId")]
    public string? ActiveJobId { get; set; }

    /// <summary>Last submitted request prompt.</summary>
    [JsonPropertyName("lastRequestPrompt")]
    public string? LastRequestPrompt { get; set; }

    /// <summary>Number of active interactive links.</summary>
    [JsonPropertyName("activeVoiceLinks")]
    public int ActiveVoiceLinks { get; set; }

    /// <summary>Number of read-only subscribers.</summary>
    [JsonPropertyName("readOnlySubscribers")]
    public int ReadOnlySubscribers { get; set; }

    /// <summary>Whether this agent is interactive-default.</summary>
    [JsonPropertyName("isInteractiveDefault")]
    public bool IsInteractiveDefault { get; set; }

    /// <summary>Whether this agent is plan-default.</summary>
    [JsonPropertyName("isTodoPlanDefault")]
    public bool IsTodoPlanDefault { get; set; }

    /// <summary>Whether this agent is status-default.</summary>
    [JsonPropertyName("isTodoStatusDefault")]
    public bool IsTodoStatusDefault { get; set; }

    /// <summary>Whether this agent is implement-default.</summary>
    [JsonPropertyName("isTodoImplementDefault")]
    public bool IsTodoImplementDefault { get; set; }
}

/// <summary>One-shot queue item snapshot.</summary>
public sealed class AgentPoolQueueItem
{
    /// <summary>Queue job identifier.</summary>
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>Assigned agent name.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    /// <summary>Workspace path this queue item is scoped to.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Queue status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>One-shot context.</summary>
    [JsonPropertyName("context")]
    public AgentPoolOneShotContext? Context { get; set; }

    /// <summary>Template id used for resolution.</summary>
    [JsonPropertyName("promptTemplateId")]
    public string? PromptTemplateId { get; set; }

    /// <summary>Rendered prompt sent to runtime.</summary>
    [JsonPropertyName("renderedPrompt")]
    public string? RenderedPrompt { get; set; }

    /// <summary>Terminal response text.</summary>
    [JsonPropertyName("responseText")]
    public string? ResponseText { get; set; }

    /// <summary>Terminal error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdUtc")]
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>UTC processing start timestamp.</summary>
    [JsonPropertyName("startedUtc")]
    public DateTimeOffset? StartedUtc { get; set; }

    /// <summary>UTC completion timestamp.</summary>
    [JsonPropertyName("completedUtc")]
    public DateTimeOffset? CompletedUtc { get; set; }

    /// <summary>Associated session id, if any.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}

/// <summary>Base mutation result for agent-pool operations.</summary>
public class AgentPoolMutationResult
{
    /// <summary>Whether the mutation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error text on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Result for one-shot enqueue operations.</summary>
public sealed class AgentPoolEnqueueResult : AgentPoolMutationResult
{
    /// <summary>Created queue job id.</summary>
    [JsonPropertyName("jobId")]
    public string? JobId { get; set; }

    /// <summary>Resolved agent name.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    /// <summary>Rendered prompt used for the request.</summary>
    [JsonPropertyName("renderedPrompt")]
    public string? RenderedPrompt { get; set; }
}

/// <summary>Result for interactive connect operations.</summary>
public sealed class AgentPoolConnectResult : AgentPoolMutationResult
{
    /// <summary>Resolved agent name.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    /// <summary>Connected interactive session id.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}

/// <summary>Result for prompt resolution operations.</summary>
public sealed class AgentPoolPromptResolutionResult : AgentPoolMutationResult
{
    /// <summary>Resolved prompt text.</summary>
    [JsonPropertyName("promptText")]
    public string? PromptText { get; set; }

    /// <summary>Resolved template id.</summary>
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    /// <summary>Whether template resolution was used.</summary>
    [JsonPropertyName("templateResolved")]
    public bool TemplateResolved { get; set; }
}

/// <summary>Global runtime notification event emitted by the pool.</summary>
public sealed class AgentPoolNotificationEvent
{
    /// <summary>Event type (queued, processing, completed, failed, canceled).</summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Agent name.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    /// <summary>Workspace path scoping this event.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Queue job id.</summary>
    [JsonPropertyName("jobId")]
    public string? JobId { get; set; }

    /// <summary>Associated session id.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>Last request prompt snapshot.</summary>
    [JsonPropertyName("lastRequestPrompt")]
    public string? LastRequestPrompt { get; set; }

    /// <summary>UTC event timestamp.</summary>
    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>Optional message payload.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>Read-only stream event for a single queue job.</summary>
public sealed class AgentPoolJobStreamEvent
{
    /// <summary>Queue job id.</summary>
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>Event type.</summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Status text.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Text payload.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Error payload.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>UTC event timestamp.</summary>
    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; set; }
}
