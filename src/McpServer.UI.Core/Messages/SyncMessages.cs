using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query for <c>/mcp/sync/status</c>.</summary>
public sealed record GetSyncStatusQuery : IQuery<SyncStatusSnapshot>;

/// <summary>Command for <c>/mcp/sync/run</c>.</summary>
public sealed record RunSyncCommand : ICommand<SyncRunSummary>;

/// <summary>List/detail-friendly sync status snapshot.</summary>
public sealed record SyncStatusSnapshot(
    string? LastRun,
    string? CompletedAt,
    string? Status,
    string? Error,
    int? DocumentsIngested,
    int? ChunksWritten,
    int? SessionLogsImported,
    int? IssuesSynced,
    DateTimeOffset CheckedAt);

/// <summary>Summary of a sync run operation.</summary>
public sealed record SyncRunSummary(
    string? RunId,
    string? StartedAt,
    string? CompletedAt,
    string? Status,
    string? Error,
    int DocumentsIngested,
    int ChunksWritten,
    int SessionLogsImported,
    int IssuesSynced,
    DateTimeOffset RecordedAt);
