using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// Health-check response payload for <c>GET /health</c>.
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    /// Overall health status (for example, <c>Healthy</c>).
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Application version (informational version including git SHA).
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Individual health-check entries.
    /// </summary>
    [JsonPropertyName("checks")]
    public IReadOnlyList<HealthCheckEntry> Checks { get; set; } = [];
}

/// <summary>
/// Single health-check entry from <see cref="HealthCheckResult"/>.
/// </summary>
public sealed class HealthCheckEntry
{
    /// <summary>
    /// Health-check name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Health-check status.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Optional description for the check.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Check duration (seconds), when provided by the endpoint.
    /// </summary>
    [JsonPropertyName("duration")]
    public double? Duration { get; set; }
}
