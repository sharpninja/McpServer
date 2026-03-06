using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.UI.Core.Models;

#pragma warning disable CS1591

/// <summary>Request to create an MCP voice session.</summary>
public sealed record McpVoiceSessionCreateRequest
{
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("clientName")]
    public string? ClientName { get; init; }
}

/// <summary>Response returned when an MCP voice session is created.</summary>
public sealed record McpVoiceSessionCreateResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; init; } = "en-US";

    [JsonPropertyName("modelRequested")]
    public string? ModelRequested { get; init; }

    [JsonPropertyName("modelResolved")]
    public string? ModelResolved { get; init; }
}

/// <summary>Request body for a single voice turn.</summary>
public sealed record McpVoiceTurnRequest
{
    [JsonPropertyName("userTranscriptText")]
    public required string UserTranscriptText { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("clientTimestampUtc")]
    public string? ClientTimestampUtc { get; init; }
}

/// <summary>Response body for a completed voice turn.</summary>
public sealed record McpVoiceTurnResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("turnId")]
    public string TurnId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("assistantDisplayText")]
    public string? AssistantDisplayText { get; init; }

    [JsonPropertyName("assistantSpeakText")]
    public string? AssistantSpeakText { get; init; }

    [JsonPropertyName("toolCalls")]
    public IReadOnlyList<McpVoiceToolCallRecord>? ToolCalls { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("latencyMs")]
    public int LatencyMs { get; init; }

    [JsonPropertyName("modelRequested")]
    public string? ModelRequested { get; init; }

    [JsonPropertyName("modelResolved")]
    public string? ModelResolved { get; init; }
}

/// <summary>Interrupt response for an MCP voice session.</summary>
public sealed record McpVoiceInterruptResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("interrupted")]
    public bool Interrupted { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

/// <summary>Status snapshot for a voice session.</summary>
public sealed record McpVoiceSessionStatus
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; init; } = "en-US";

    [JsonPropertyName("createdUtc")]
    public string CreatedUtc { get; init; } = string.Empty;

    [JsonPropertyName("lastUpdatedUtc")]
    public string LastUpdatedUtc { get; init; } = string.Empty;

    [JsonPropertyName("isTurnActive")]
    public bool IsTurnActive { get; init; }

    [JsonPropertyName("lastError")]
    public string? LastError { get; init; }

    [JsonPropertyName("lastTurnId")]
    public string? LastTurnId { get; init; }

    [JsonPropertyName("turnCounter")]
    public int TurnCounter { get; init; }

    [JsonPropertyName("transcriptCount")]
    public int TranscriptCount { get; init; }
}

/// <summary>Transcript response payload for a voice session.</summary>
public sealed record McpVoiceTranscriptResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("items")]
    public IReadOnlyList<McpVoiceTranscriptEntry> Items { get; init; } = [];
}

/// <summary>Transcript entry captured for a voice session.</summary>
public sealed record McpVoiceTranscriptEntry
{
    [JsonPropertyName("timestampUtc")]
    public string TimestampUtc { get; init; } = string.Empty;

    [JsonPropertyName("turnId")]
    public string? TurnId { get; init; }

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

/// <summary>Tool-call record emitted by a voice turn.</summary>
public sealed record McpVoiceToolCallRecord
{
    [JsonPropertyName("turnId")]
    public string TurnId { get; init; } = string.Empty;

    [JsonPropertyName("toolName")]
    public string ToolName { get; init; } = string.Empty;

    [JsonPropertyName("step")]
    public int Step { get; init; }

    [JsonPropertyName("argumentsJson")]
    public string ArgumentsJson { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("isMutation")]
    public bool IsMutation { get; init; }

    [JsonPropertyName("resultSummary")]
    public string? ResultSummary { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>A single Server-Sent Event emitted during a streaming voice turn.</summary>
public sealed record McpVoiceTurnStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("turnId")]
    public string? TurnId { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("toolName")]
    public string? ToolName { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("toolCalls")]
    public IReadOnlyList<McpVoiceToolCallRecord>? ToolCalls { get; init; }

    [JsonPropertyName("latencyMs")]
    public int? LatencyMs { get; init; }
}

#pragma warning restore CS1591
