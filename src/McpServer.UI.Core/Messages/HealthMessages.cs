using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to check the MCP server health endpoint.</summary>
public sealed record CheckHealthQuery : IQuery<HealthSnapshot>;

/// <summary>
/// Health snapshot used by the Health area list/history and detail views.
/// </summary>
/// <param name="CheckedAt">Timestamp when the health check completed.</param>
/// <param name="Status">Parsed status value (for example, "healthy").</param>
/// <param name="RawPayload">Raw endpoint payload for detail display.</param>
/// <param name="ServerBaseUrl">Server base URL that was checked, if available.</param>
public sealed record HealthSnapshot(
    DateTimeOffset CheckedAt,
    string Status,
    string RawPayload,
    string? ServerBaseUrl = null);
