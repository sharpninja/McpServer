using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="ISessionLogApiClient"/> backed by the selected workspace
/// <see cref="McpServer.Client.McpServerClient"/> in <see cref="DirectorMcpContext"/>.
/// </summary>
internal sealed class SessionLogApiClientAdapter : ISessionLogApiClient
{
    private const int DetailPageSize = 200;

    private readonly DirectorMcpContext _context;

    /// <summary>Initializes a new adapter instance.</summary>
    /// <param name="context">Director connection context.</param>
    public SessionLogApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ListSessionLogsResult> ListSessionLogsAsync(ListSessionLogsQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.SessionLog.QueryAsync(
            agent: string.IsNullOrWhiteSpace(query.Agent) ? null : query.Agent,
            model: string.IsNullOrWhiteSpace(query.Model) ? null : query.Model,
            text: string.IsNullOrWhiteSpace(query.Text) ? null : query.Text,
            limit: Math.Max(1, query.Limit),
            offset: Math.Max(0, query.Offset),
            cancellationToken: cancellationToken).ConfigureAwait(true);

        var items = result.Items
            .Select(MapSummary)
            .ToList();

        var totalCount = result.TotalCount;
        var limit = result.Limit <= 0 ? Math.Max(1, query.Limit) : result.Limit;
        var offset = result.Offset < 0 ? Math.Max(0, query.Offset) : result.Offset;
        return new ListSessionLogsResult(items, totalCount, limit, offset);
    }

    /// <inheritdoc />
    public async Task<SessionLogDetail?> GetSessionLogAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId is required.", nameof(sessionId));

        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var offset = 0;

        while (true)
        {
            var page = await client.SessionLog.QueryAsync(
                    limit: DetailPageSize,
                    offset: offset,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(true);

            var match = page.Items.FirstOrDefault(item => string.Equals(item.SessionId, sessionId, StringComparison.Ordinal));
            if (match is not null)
                return MapDetail(match);

            if (page.Items.Count == 0 || offset + page.Items.Count >= page.TotalCount)
                return null;

            offset += page.Items.Count;
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogSubmitOutcome> SubmitSessionLogAsync(SubmitSessionLogCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.SessionLog.SubmitAsync(MapSubmit(command.SessionLog), cancellationToken).ConfigureAwait(true);
        return new SessionLogSubmitOutcome(result.Id, result.SourceType, result.SessionId);
    }

    /// <inheritdoc />
    public async Task<SessionLogDialogAppendOutcome> AppendSessionLogDialogAsync(AppendSessionLogDialogCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        var items = command.Items
            .Select(item => new ProcessingDialogItemDto
            {
                Timestamp = item.Timestamp,
                Role = item.Role,
                Category = item.Category,
                Content = item.Content
            })
            .ToList();

        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.SessionLog.AppendDialogAsync(
                command.Agent,
                command.SessionId,
                command.RequestId,
                items,
                cancellationToken)
            .ConfigureAwait(true);

        return new SessionLogDialogAppendOutcome(result.Agent, result.SessionId, result.RequestId, result.TotalDialogCount);
    }

    private static SessionLogSummary MapSummary(UnifiedSessionLogDto item)
        => new(
            SessionId: item.SessionId ?? string.Empty,
            SourceType: item.SourceType ?? string.Empty,
            Title: item.Title ?? string.Empty,
            Status: item.Status ?? string.Empty,
            Model: item.Model,
            Started: item.Started,
            LastUpdated: item.LastUpdated,
            EntryCount: item.EntryCount);

    private static UnifiedSessionLogDto MapSubmit(SessionLogDetail detail)
        => new()
        {
            SessionId = detail.SessionId,
            SourceType = detail.SourceType,
            Title = detail.Title,
            Status = detail.Status,
            Model = detail.Model,
            Started = detail.Started,
            LastUpdated = detail.LastUpdated,
            EntryCount = detail.EntryCount,
            TotalTokens = detail.TotalTokens,
            CursorSessionLabel = detail.CursorSessionLabel,
            Workspace = detail.Workspace is null
                ? null
                : new WorkspaceInfoDto
                {
                    Project = detail.Workspace.Project,
                    TargetFramework = detail.Workspace.TargetFramework,
                    Repository = detail.Workspace.Repository,
                    Branch = detail.Workspace.Branch
                },
            CopilotStatistics = detail.CopilotStatistics is null
                ? null
                : new CopilotStatisticsDto
                {
                    AverageSuccessScore = detail.CopilotStatistics.AverageSuccessScore,
                    TotalNetTokens = detail.CopilotStatistics.TotalNetTokens,
                    TotalNetPremiumRequests = detail.CopilotStatistics.TotalNetPremiumRequests,
                    CompletedCount = detail.CopilotStatistics.CompletedCount,
                    InProgressCount = detail.CopilotStatistics.InProgressCount
                },
            Entries = detail.Entries.Select(MapSubmitEntry).ToList()
        };

    private static UnifiedRequestEntryDto MapSubmitEntry(SessionLogEntryDetail entry)
        => new()
        {
            RequestId = entry.RequestId,
            Timestamp = entry.Timestamp,
            QueryText = entry.QueryText,
            QueryTitle = entry.QueryTitle,
            Response = entry.Response,
            Interpretation = entry.Interpretation,
            Status = entry.Status,
            Model = entry.Model,
            ModelProvider = entry.ModelProvider,
            TokenCount = entry.TokenCount,
            FailureNote = entry.FailureNote,
            Score = entry.Score,
            IsPremium = entry.IsPremium,
            Tags = entry.Tags.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            ContextList = entry.ContextList.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            DesignDecisions = entry.DesignDecisions.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            RequirementsDiscovered = entry.RequirementsDiscovered.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            FilesModified = entry.FilesModified.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            Blockers = entry.Blockers.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList(),
            Actions = entry.Actions.Select(MapSubmitAction).ToList(),
            ProcessingDialog = entry.ProcessingDialog.Select(MapSubmitDialog).ToList(),
            Commits = entry.Commits.Select(MapSubmitCommit).ToList()
        };

    private static UnifiedActionDto MapSubmitAction(SessionLogActionDetail action)
        => new()
        {
            Order = action.Order,
            Description = action.Description,
            Type = action.Type,
            Status = action.Status,
            FilePath = action.FilePath
        };

    private static ProcessingDialogItemDto MapSubmitDialog(SessionLogDialogDetail dialog)
        => new()
        {
            Timestamp = dialog.Timestamp,
            Role = dialog.Role,
            Category = dialog.Category,
            Content = dialog.Content
        };

    private static SessionLogCommitDto MapSubmitCommit(SessionLogCommitDetail commit)
        => new()
        {
            Sha = commit.Sha,
            Branch = commit.Branch,
            Message = commit.Message,
            Author = commit.Author,
            Timestamp = commit.Timestamp,
            FilesChanged = commit.FilesChanged.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList()
        };

    private static SessionLogDetail MapDetail(UnifiedSessionLogDto item)
        => new(
            SessionId: item.SessionId ?? string.Empty,
            SourceType: item.SourceType ?? string.Empty,
            Title: item.Title ?? string.Empty,
            Status: item.Status ?? string.Empty,
            Model: item.Model,
            Started: item.Started,
            LastUpdated: item.LastUpdated,
            EntryCount: item.EntryCount,
            TotalTokens: item.TotalTokens,
            CursorSessionLabel: item.CursorSessionLabel,
            Workspace: item.Workspace is null ? null : new SessionLogWorkspaceInfo(
                item.Workspace.Project,
                item.Workspace.TargetFramework,
                item.Workspace.Repository,
                item.Workspace.Branch),
            CopilotStatistics: item.CopilotStatistics is null ? null : new SessionLogCopilotStatistics(
                item.CopilotStatistics.AverageSuccessScore,
                item.CopilotStatistics.TotalNetTokens,
                item.CopilotStatistics.TotalNetPremiumRequests,
                item.CopilotStatistics.CompletedCount,
                item.CopilotStatistics.InProgressCount),
            Entries: item.Entries?.Select(MapEntry).ToList() ?? []);

    private static SessionLogEntryDetail MapEntry(UnifiedRequestEntryDto entry)
        => new(
            RequestId: entry.RequestId ?? string.Empty,
            Timestamp: entry.Timestamp,
            QueryTitle: entry.QueryTitle,
            QueryText: entry.QueryText,
            Response: entry.Response,
            Interpretation: entry.Interpretation,
            Status: entry.Status,
            Model: entry.Model,
            ModelProvider: entry.ModelProvider,
            TokenCount: entry.TokenCount,
            FailureNote: entry.FailureNote,
            Score: entry.Score,
            IsPremium: entry.IsPremium,
            Tags: CopyStrings(entry.Tags),
            ContextList: CopyStrings(entry.ContextList),
            DesignDecisions: CopyStrings(entry.DesignDecisions),
            RequirementsDiscovered: CopyStrings(entry.RequirementsDiscovered),
            FilesModified: CopyStrings(entry.FilesModified),
            Blockers: CopyStrings(entry.Blockers),
            Actions: entry.Actions?.Select(MapAction).ToList() ?? [],
            ProcessingDialog: entry.ProcessingDialog?.Select(MapDialog).ToList() ?? [],
            Commits: entry.Commits?.Select(MapCommit).ToList() ?? []);

    private static SessionLogActionDetail MapAction(UnifiedActionDto action)
        => new(
            Order: action.Order,
            Description: action.Description,
            Type: action.Type,
            Status: action.Status,
            FilePath: action.FilePath);

    private static SessionLogDialogDetail MapDialog(ProcessingDialogItemDto dialog)
        => new(
            Timestamp: dialog.Timestamp,
            Role: dialog.Role,
            Category: dialog.Category,
            Content: dialog.Content);

    private static SessionLogCommitDetail MapCommit(SessionLogCommitDto commit)
        => new(
            Sha: commit.Sha,
            Branch: commit.Branch,
            Message: commit.Message,
            Author: commit.Author,
            Timestamp: commit.Timestamp,
            FilesChanged: CopyStrings(commit.FilesChanged));

    private static IReadOnlyList<string> CopyStrings(IEnumerable<string>? values)
        => values?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [];
}
