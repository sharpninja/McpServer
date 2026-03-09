using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Request payload for creating a voice session.</summary>
public sealed class VoiceSessionCreateRequest
{
    /// <summary>Preferred language tag.</summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>Client or device identifier.</summary>
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    /// <summary>Client display name.</summary>
    [JsonPropertyName("clientName")]
    public string? ClientName { get; set; }

    /// <summary>Workspace root path override.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Pooled agent name for routing or reuse.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    /// <summary>Agent binary path override.</summary>
    [JsonPropertyName("agentPath")]
    public string? AgentPath { get; set; }

    /// <summary>Model override.</summary>
    [JsonPropertyName("agentModel")]
    public string? AgentModel { get; set; }

    /// <summary>Seed prompt used for session bootstrap.</summary>
    [JsonPropertyName("agentSeed")]
    public string? AgentSeed { get; set; }

    /// <summary>Prompt sent when attaching to an active session.</summary>
    [JsonPropertyName("agentPrompt")]
    public string? AgentPrompt { get; set; }

    /// <summary>Environment variables passed to the session.</summary>
    [JsonPropertyName("agentParameters")]
    public Dictionary<string, string>? AgentParameters { get; set; }

    /// <summary>Execution strategy used to run the session.</summary>
    [JsonPropertyName("executionStrategy")]
    public string? ExecutionStrategy { get; set; }

    /// <summary>Whether this is a one-shot session.</summary>
    [JsonPropertyName("oneShotSession")]
    public bool OneShotSession { get; set; }
}

/// <summary>Result of creating a voice session.</summary>
public sealed class VoiceSessionCreateResponse
{
    /// <summary>Voice session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Session status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Session language.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>Requested model identifier.</summary>
    [JsonPropertyName("modelRequested")]
    public string? ModelRequested { get; set; }

    /// <summary>Resolved model identifier.</summary>
    [JsonPropertyName("modelResolved")]
    public string? ModelResolved { get; set; }

    /// <summary>Execution strategy used by the session.</summary>
    [JsonPropertyName("executionStrategy")]
    public string ExecutionStrategy { get; set; } = string.Empty;
}

/// <summary>Request payload for submitting a voice turn.</summary>
public sealed class VoiceTurnRequest
{
    /// <summary>Final user transcript text.</summary>
    [JsonPropertyName("userTranscriptText")]
    public string UserTranscriptText { get; set; } = string.Empty;

    /// <summary>Optional language tag for transcript text.</summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>Client timestamp in UTC ISO-8601 format.</summary>
    [JsonPropertyName("clientTimestampUtc")]
    public string? ClientTimestampUtc { get; set; }
}

/// <summary>Tool-call execution record for a voice turn.</summary>
public sealed class VoiceToolCallRecord
{
    /// <summary>Turn identifier.</summary>
    [JsonPropertyName("turnId")]
    public string TurnId { get; set; } = string.Empty;

    /// <summary>Tool name.</summary>
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    /// <summary>Step number (1-based).</summary>
    [JsonPropertyName("step")]
    public int Step { get; set; }

    /// <summary>Serialized arguments passed to the tool call.</summary>
    [JsonPropertyName("argumentsJson")]
    public string ArgumentsJson { get; set; } = string.Empty;

    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Whether this call performed a mutation.</summary>
    [JsonPropertyName("isMutation")]
    public bool IsMutation { get; set; }

    /// <summary>Short result summary.</summary>
    [JsonPropertyName("resultSummary")]
    public string? ResultSummary { get; set; }

    /// <summary>Error detail if execution failed.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Response payload for a completed voice turn.</summary>
public sealed class VoiceTurnResponse
{
    /// <summary>Voice session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Turn identifier.</summary>
    [JsonPropertyName("turnId")]
    public string TurnId { get; set; } = string.Empty;

    /// <summary>Turn status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Assistant transcript text for UI display.</summary>
    [JsonPropertyName("assistantDisplayText")]
    public string? AssistantDisplayText { get; set; }

    /// <summary>Assistant text intended for TTS.</summary>
    [JsonPropertyName("assistantSpeakText")]
    public string? AssistantSpeakText { get; set; }

    /// <summary>Captured tool call records.</summary>
    [JsonPropertyName("toolCalls")]
    public IReadOnlyList<VoiceToolCallRecord>? ToolCalls { get; set; }

    /// <summary>Error message when the turn fails.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Turn latency in milliseconds.</summary>
    [JsonPropertyName("latencyMs")]
    public int LatencyMs { get; set; }

    /// <summary>Requested model identifier.</summary>
    [JsonPropertyName("modelRequested")]
    public string? ModelRequested { get; set; }

    /// <summary>Resolved model identifier.</summary>
    [JsonPropertyName("modelResolved")]
    public string? ModelResolved { get; set; }
}

/// <summary>Response payload for voice interrupt operations.</summary>
public sealed class VoiceInterruptResponse
{
    /// <summary>Voice session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Whether interruption was signaled.</summary>
    [JsonPropertyName("interrupted")]
    public bool Interrupted { get; set; }

    /// <summary>Status after interruption.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Current state of a voice session.</summary>
public sealed class VoiceSessionStatus
{
    /// <summary>Voice session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Session status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Session language.</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>Creation timestamp (UTC).</summary>
    [JsonPropertyName("createdUtc")]
    public string CreatedUtc { get; set; } = string.Empty;

    /// <summary>Last-updated timestamp (UTC).</summary>
    [JsonPropertyName("lastUpdatedUtc")]
    public string LastUpdatedUtc { get; set; } = string.Empty;

    /// <summary>Whether a turn is currently in progress.</summary>
    [JsonPropertyName("isTurnActive")]
    public bool IsTurnActive { get; set; }

    /// <summary>Execution strategy backing the session.</summary>
    [JsonPropertyName("executionStrategy")]
    public string ExecutionStrategy { get; set; } = string.Empty;

    /// <summary>Most recent error (if any).</summary>
    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }

    /// <summary>Most recent turn identifier.</summary>
    [JsonPropertyName("lastTurnId")]
    public string? LastTurnId { get; set; }

    /// <summary>Completed turn count.</summary>
    [JsonPropertyName("turnCounter")]
    public int TurnCounter { get; set; }

    /// <summary>Transcript entry count.</summary>
    [JsonPropertyName("transcriptCount")]
    public int TranscriptCount { get; set; }
}

/// <summary>Transcript entry captured by a voice session.</summary>
public sealed class VoiceTranscriptEntry
{
    /// <summary>Timestamp in UTC ISO-8601 format.</summary>
    [JsonPropertyName("timestampUtc")]
    public string TimestampUtc { get; set; } = string.Empty;

    /// <summary>Associated turn identifier.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Entry role (user, assistant, tool, system).</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Entry category (transcript, tool_call, tool_result, error).</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Entry text content.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>Transcript response payload for a voice session.</summary>
public sealed class VoiceTranscriptResponse
{
    /// <summary>Voice session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Transcript entries.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<VoiceTranscriptEntry> Items { get; set; } = [];
}

/// <summary>Response payload for the escape endpoint.</summary>
public sealed class VoiceEscapeResponse
{
    /// <summary>Whether ESC was delivered to the active interactive process.</summary>
    [JsonPropertyName("sent")]
    public bool Sent { get; set; }
}

/// <summary>A single SSE event produced during a streaming voice turn.</summary>
public sealed class VoiceTurnStreamEvent
{
    /// <summary>Event type (chunk, tool_status, done, error).</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Text fragment for chunk events.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Turn identifier for done/error events.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Status value for done events.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Error or tool status message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Tool name for tool status events.</summary>
    [JsonPropertyName("toolName")]
    public string? ToolName { get; set; }

    /// <summary>Short tool execution summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>Collected tool calls for done events.</summary>
    [JsonPropertyName("toolCalls")]
    public IReadOnlyList<VoiceToolCallRecord>? ToolCalls { get; set; }

    /// <summary>Latency in milliseconds.</summary>
    [JsonPropertyName("latencyMs")]
    public int? LatencyMs { get; set; }
}
