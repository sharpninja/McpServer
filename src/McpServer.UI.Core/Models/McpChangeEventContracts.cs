using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.UI.Core.Models;

/// <summary>
/// Incoming change event payload emitted by <c>/mcpserver/events</c>.
/// Includes extension data for forward-compatible fields.
/// </summary>
public sealed record McpIncomingChangeEvent
{
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("entityId")]
    public string? EntityId { get; init; }

    [JsonPropertyName("resourceUri")]
    public string? ResourceUri { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    [JsonPropertyName("agentId")]
    public string? AgentId { get; init; }

    [JsonPropertyName("eventType")]
    public string? EventType { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>SSE event name from the <c>event:</c> line when provided.</summary>
    [JsonIgnore]
    public string? SseEvent { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }
}

