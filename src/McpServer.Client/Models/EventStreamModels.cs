using System;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Workspace change notification event emitted over SSE.</summary>
public sealed class ChangeEvent
{
    /// <summary>Event category (for example: todo, repo, session_log).</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Mutation action (created, updated, deleted).</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>Affected entity identifier, when available.</summary>
    [JsonPropertyName("entityId")]
    public string? EntityId { get; set; }

    /// <summary>Associated MCP resource URI, when available.</summary>
    [JsonPropertyName("resourceUri")]
    public string? ResourceUri { get; set; }

    /// <summary>Timestamp for the emitted event.</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}
