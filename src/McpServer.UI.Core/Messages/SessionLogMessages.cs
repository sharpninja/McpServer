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

/// <summary>TR-PLANNED-013: Query to load a single session log by session ID.</summary>
public sealed record GetSessionLogQuery(string SessionId) : IQuery<SessionLogDetail?>;

/// <summary>TR-PLANNED-013: Detailed session log view for Director drill-down screens.</summary>
public sealed record SessionLogDetail(
    string SessionId,
    string SourceType,
    string Title,
    string Status,
    string? Model,
    string? Started,
    string? LastUpdated,
    int EntryCount,
    int? TotalTokens,
    string? CursorSessionLabel,
    SessionLogWorkspaceInfo? Workspace,
    SessionLogCopilotStatistics? CopilotStatistics,
    IReadOnlyList<SessionLogEntryDetail> Entries);

/// <summary>Workspace metadata attached to a session log.</summary>
public sealed record SessionLogWorkspaceInfo(
    string? Project,
    string? TargetFramework,
    string? Repository,
    string? Branch);

/// <summary>Aggregate Copilot usage statistics attached to a session log.</summary>
public sealed record SessionLogCopilotStatistics(
    double? AverageSuccessScore,
    int? TotalNetTokens,
    int? TotalNetPremiumRequests,
    int? CompletedCount,
    int? InProgressCount);

/// <summary>Detailed request entry within a session log.</summary>
public sealed record SessionLogEntryDetail(
    string RequestId,
    string? Timestamp,
    string? QueryTitle,
    string? QueryText,
    string? Response,
    string? Interpretation,
    string? Status,
    string? Model,
    string? ModelProvider,
    int? TokenCount,
    string? FailureNote,
    double? Score,
    bool? IsPremium,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> ContextList,
    IReadOnlyList<string> DesignDecisions,
    IReadOnlyList<string> RequirementsDiscovered,
    IReadOnlyList<string> FilesModified,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<SessionLogActionDetail> Actions,
    IReadOnlyList<SessionLogDialogDetail> ProcessingDialog,
    IReadOnlyList<SessionLogCommitDetail> Commits);

/// <summary>Action detail attached to a session log request entry.</summary>
public sealed record SessionLogActionDetail(
    int Order,
    string? Description,
    string? Type,
    string? Status,
    string? FilePath);

/// <summary>Processing dialog detail attached to a session log request entry.</summary>
public sealed record SessionLogDialogDetail(
    string? Timestamp,
    string? Role,
    string? Category,
    string? Content);

/// <summary>Commit detail attached to a session log request entry.</summary>
public sealed record SessionLogCommitDetail(
    string? Sha,
    string? Branch,
    string? Message,
    string? Author,
    string? Timestamp,
    IReadOnlyList<string> FilesChanged);
