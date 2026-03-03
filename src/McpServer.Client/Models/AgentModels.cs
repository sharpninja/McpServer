using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// Result payload for seeding built-in agent definitions.
/// </summary>
public sealed class AgentSeedDefaultsResult
{
    /// <summary>
    /// Number of seeded definitions.
    /// </summary>
    [JsonPropertyName("seeded")]
    public int Seeded { get; set; }
}

/// <summary>
/// Request payload for logging an agent lifecycle event.
/// </summary>
public sealed class AgentEventRequest
{
    /// <summary>
    /// Agent identifier.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Numeric lifecycle event type.
    /// </summary>
    [JsonPropertyName("eventType")]
    public int EventType { get; set; }

    /// <summary>
    /// Optional event details.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Result payload for agent mutation operations.
/// </summary>
public sealed class AgentMutationResult
{
    /// <summary>
    /// Whether the mutation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message when the mutation fails.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
