using System.Collections.Generic;
using System;
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

/// <summary>Server startup diagnostics returned by <c>GET /server-startup-utc</c>.</summary>
public sealed class ServerStartupResult
{
    /// <summary>Server startup timestamp in UTC.</summary>
    [JsonPropertyName("serverStartedAtUtc")]
    public DateTimeOffset ServerStartedAtUtc { get; set; }

    /// <summary>Server current timestamp in UTC.</summary>
    [JsonPropertyName("nowUtc")]
    public DateTimeOffset NowUtc { get; set; }

    /// <summary>Operating-system process identifier.</summary>
    [JsonPropertyName("processId")]
    public int ProcessId { get; set; }

    /// <summary>Optional workspace associated with the diagnostic response.</summary>
    [JsonPropertyName("workspace")]
    public string? Workspace { get; set; }

    /// <summary>Optional listening port associated with the diagnostic response.</summary>
    [JsonPropertyName("port")]
    public int? Port { get; set; }
}

/// <summary>Marker-file timestamp diagnostics returned by <c>GET /marker-file-timestamp</c>.</summary>
public sealed class MarkerFileTimestampResult
{
    /// <summary>Normalized repository path.</summary>
    [JsonPropertyName("repoPath")]
    public string? RepoPath { get; set; }

    /// <summary>Expected marker file path.</summary>
    [JsonPropertyName("markerPath")]
    public string? MarkerPath { get; set; }

    /// <summary>Whether the marker file exists.</summary>
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    /// <summary>Marker last-write timestamp in UTC when the file exists.</summary>
    [JsonPropertyName("lastWriteTimeUtc")]
    public DateTimeOffset? LastWriteTimeUtc { get; set; }

    /// <summary>Marker creation timestamp in UTC when the file exists.</summary>
    [JsonPropertyName("creationTimeUtc")]
    public DateTimeOffset? CreationTimeUtc { get; set; }

    /// <summary>Marker file length in bytes when the file exists.</summary>
    [JsonPropertyName("length")]
    public long? Length { get; set; }

    /// <summary>Error message when the diagnostic request is invalid.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
