using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list session logs with optional filters and pagination.</summary>
public sealed record ListSessionLogsQuery : IQuery<ListSessionLogsResult>
{
    /// <summary>Filter by agent/source type.</summary>
    public string? Agent { get; init; }

    /// <summary>Filter by model.</summary>
    public string? Model { get; init; }

    /// <summary>Full-text filter.</summary>
    public string? Text { get; init; }

    /// <summary>Page size (default 20).</summary>
    public int Limit { get; init; } = 20;

    /// <summary>Page offset (default 0).</summary>
    public int Offset { get; init; }
}

/// <summary>Result of a session-log list query.</summary>
public sealed record ListSessionLogsResult(
    IReadOnlyList<SessionLogSummary> Items,
    int TotalCount,
    int Limit,
    int Offset);

/// <summary>List-friendly summary of a session log record.</summary>
public sealed record SessionLogSummary(
    string SessionId,
    string SourceType,
    string Title,
    string Status,
    string? Model,
    string? Started,
    string? LastUpdated,
    int EntryCount);
