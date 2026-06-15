using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Executes session-log mutations through the turn transaction
/// coordinator when available.
/// </summary>
public sealed class TransactionGatedSessionLogService : ISessionLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ISessionLogService _inner;
    private readonly McpDbContext _db;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly WorkspaceContext? _workspaceContext;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedSessionLogService"/> class.</summary>
    /// <param name="inner">Local session-log service that performs durable mutations.</param>
    /// <param name="db">Scoped database context used for exact graph restoration.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="workspaceContext">Optional scoped workspace context used to keep the EF filter aligned.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedSessionLogService(
        ISessionLogService inner,
        McpDbContext db,
        ITurnTransactionCoordinator? coordinator = null,
        WorkspaceContext? workspaceContext = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _coordinator = coordinator;
        _workspaceContext = workspaceContext;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<SessionLogQueryResult> QueryAsync(
        SessionLogQueryRequest request,
        CancellationToken cancellationToken = default)
        => _inner.QueryAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<UnifiedSessionLogDto?> GetAsync(
        string sourceType,
        string sessionId,
        CancellationToken cancellationToken = default)
        => _inner.GetAsync(sourceType, sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsUnchangedAsync(
        string sourceType,
        string sessionId,
        string contentHash,
        CancellationToken cancellationToken = default)
        => _inner.IsUnchangedAsync(sourceType, sessionId, contentHash, cancellationToken);

    /// <inheritdoc />
    public Task<long> SubmitAsync(
        UnifiedSessionLogDto dto,
        string? sourceFilePath = null,
        string? contentHash = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return ExecuteMutationAsync(
            "sessionlog.submit",
            new SubmitPayload(dto, sourceFilePath, contentHash),
            dto.SourceType ?? string.Empty,
            dto.SessionId ?? string.Empty,
            MissingSnapshotRollbackPolicy.RestorePostMutation,
            ct => _inner.SubmitAsync(dto, sourceFilePath, contentHash, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AppendProcessingDialogAsync(
        string sourceType,
        string sessionId,
        string requestId,
        IReadOnlyList<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.dialog",
            new DialogPayload(sourceType, sessionId, requestId, items),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.AppendProcessingDialogAsync(sourceType, sessionId, requestId, items, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<long> UpsertTurnAsync(
        string sourceType,
        string sessionId,
        UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.upsert_turn",
            new TurnPayload(sourceType, sessionId, turn),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.UpsertTurnAsync(sourceType, sessionId, turn, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<long> ReplaceTurnAsync(
        string sourceType,
        string sessionId,
        UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.replace_turn",
            new TurnPayload(sourceType, sessionId, turn),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.ReplaceTurnAsync(sourceType, sessionId, turn, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ReplaceTurnSectionAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        UnifiedRequestEntryDto payload,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.replace_section",
            new SectionPayload(sourceType, sessionId, requestId, section, payload),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.ReplaceTurnSectionAsync(sourceType, sessionId, requestId, section, payload, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> ClearTurnSectionAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.clear_section",
            new ClearSectionPayload(sourceType, sessionId, requestId, section),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.ClearTurnSectionAsync(sourceType, sessionId, requestId, section, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteTurnItemAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        string itemKey,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.delete_item",
            new DeleteItemPayload(sourceType, sessionId, requestId, section, itemKey),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.DeleteTurnItemAsync(sourceType, sessionId, requestId, section, itemKey, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteTurnAsync(
        string sourceType,
        string sessionId,
        string requestId,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.delete_turn",
            new DeleteTurnPayload(sourceType, sessionId, requestId),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.DeleteTurnAsync(sourceType, sessionId, requestId, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteSessionAsync(
        string sourceType,
        string sessionId,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.delete_session",
            new SessionPayload(sourceType, sessionId),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePreMutation,
            ct => _inner.DeleteSessionAsync(sourceType, sessionId, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> OpenSessionAsync(
        string sourceType,
        string sessionId,
        string? title = null,
        string? model = null,
        CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "sessionlog.open",
            new OpenSessionPayload(sourceType, sessionId, title, model),
            sourceType,
            sessionId,
            MissingSnapshotRollbackPolicy.RestorePostMutation,
            ct => _inner.OpenSessionAsync(sourceType, sessionId, title, model, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<int> RepairWorkspaceStampsAsync(
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (dryRun)
            return _inner.RepairWorkspaceStampsAsync(true, cancellationToken);

        ThrowIfUncompensatedRepairBlocked();
        return _inner.RepairWorkspaceStampsAsync(false, cancellationToken);
    }

    private async Task<TResult> ExecuteMutationAsync<TResult>(
        string operationName,
        object operationBody,
        string sourceType,
        string sessionId,
        MissingSnapshotRollbackPolicy missingSnapshotPolicy,
        Func<CancellationToken, Task<TResult>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
            return await mutation(cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message);

        var transaction = BuildTransactionRequest(operationName, operationBody);
        var mutationApplied = false;
        TResult resultValue = default!;

        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    var before = await CaptureSessionAsync(sourceType, sessionId, ct).ConfigureAwait(false);
                    resultValue = await mutation(ct).ConfigureAwait(false);
                    mutationApplied = true;
                    var restoreCreatedRecord = before.Session is null
                                                && missingSnapshotPolicy == MissingSnapshotRollbackPolicy.RestorePostMutation;
                    var rollbackSnapshot = restoreCreatedRecord
                        ? await CaptureSessionAsync(sourceType, sessionId, ct).ConfigureAwait(false)
                        : before;

                    return new TurnMutationResult
                    {
                        Success = true,
                        ResultJson = JsonSerializer.Serialize(resultValue, JsonOptions),
                        RollbackAsync = rollbackSnapshot.Session is not null
                            ? restoreCreatedRecord
                                ? rollbackCt => RestoreCreatedSessionAsync(rollbackSnapshot, rollbackCt)
                                : rollbackCt => RestoreSessionAsync(rollbackSnapshot, rollbackCt)
                            : null,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (mutationApplied && IsTransactionSuccess(result))
            return resultValue;

        throw ToTransactionException(operationName, result);
    }

    private TurnTransactionRequest BuildTransactionRequest(string operationName, object operationBody)
    {
        var sequence = NextSequence();
        return new TurnTransactionRequest
        {
            TurnId = $"{operationName}-{sequence}",
            OperationName = operationName,
            OperationBodyJson = JsonSerializer.Serialize(operationBody, JsonOptions),
            Sequence = sequence,
            Mutating = true,
        };
    }

    private long NextSequence()
    {
        while (true)
        {
            var current = Volatile.Read(ref _lastSequence);
            var next = Math.Max(current + 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (Interlocked.CompareExchange(ref _lastSequence, next, current) == current)
                return next;
        }
    }

    private async Task<SessionGraphSnapshot> CaptureSessionAsync(
        string sourceType,
        string sessionId,
        CancellationToken cancellationToken)
    {
        SyncDbWorkspaceFromContext();
        var workspaceId = _db.CurrentWorkspaceId;
        if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(sessionId))
            return new SessionGraphSnapshot(sourceType, sessionId, workspaceId, null);

        var session = await SessionQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entity => entity.SourceType == sourceType
                          && entity.SessionId == sessionId
                          && entity.WorkspaceId == workspaceId,
                cancellationToken)
            .ConfigureAwait(false);
        return new SessionGraphSnapshot(sourceType, sessionId, workspaceId, session);
    }

    private async Task RestoreSessionAsync(
        SessionGraphSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _db.ChangeTracker.Clear();
            var current = await SessionQuery()
                .FirstOrDefaultAsync(
                    entity => entity.SourceType == snapshot.SourceType
                              && entity.SessionId == snapshot.SessionId
                              && entity.WorkspaceId == snapshot.WorkspaceId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (current is not null)
            {
                var sessionRowId = current.Id;
                var turnIds = current.Turns.Select(turn => turn.Id).ToArray();
                _db.ChangeTracker.Clear();
                foreach (var turnId in turnIds)
                    await DeleteTurnRowsAsync(turnId, cancellationToken).ConfigureAwait(false);
                await _db.SessionLogs
                    .IgnoreQueryFilters()
                    .Where(entity => entity.Id == sessionRowId)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            if (snapshot.Session is not null)
            {
                _db.SessionLogs.Add(CloneSession(snapshot.Session));
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task RestoreCreatedSessionAsync(
        SessionGraphSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        _db.ChangeTracker.Clear();
        var currentExists = await _db.SessionLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                entity => entity.SourceType == snapshot.SourceType
                          && entity.SessionId == snapshot.SessionId
                          && entity.WorkspaceId == snapshot.WorkspaceId,
                cancellationToken)
            .ConfigureAwait(false);
        if (currentExists || snapshot.Session is null)
            return;

        _db.SessionLogs.Add(CloneSession(snapshot.Session));
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<SessionLogEntity> SessionQuery()
        => _db.SessionLogs
            .IgnoreQueryFilters()
            .Include(session => session.Turns.OrderBy(turn => turn.Id))
                .ThenInclude(turn => turn.Actions.OrderBy(action => action.Order))
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Tags)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.ContextItems.OrderBy(context => context.Ordinal))
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.ProcessingDialog.OrderBy(dialog => dialog.Ordinal))
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Commits.OrderBy(commit => commit.Ordinal))
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.StringListItems.OrderBy(item => item.Ordinal))
            .AsSplitQuery();

    private async Task DeleteTurnRowsAsync(long turnId, CancellationToken cancellationToken)
    {
        await _db.SessionLogActions.IgnoreQueryFilters().Where(row => row.SessionLogTurnId == turnId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.SessionLogTurnTags.IgnoreQueryFilters().Where(row => row.SessionLogTurnId == turnId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.SessionLogTurnContexts.IgnoreQueryFilters().Where(row => row.SessionLogTurnId == turnId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.SessionLogProcessingDialogs.IgnoreQueryFilters().Where(row => row.SessionLogTurnId == turnId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.SessionLogCommits.IgnoreQueryFilters().Where(row => row.SessionLogTurnId == turnId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.SessionLogTurnStringLists.IgnoreQueryFilters().Where(row => row.SessionLogTurnId == turnId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.SessionLogTurns.IgnoreQueryFilters().Where(row => row.Id == turnId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private void SyncDbWorkspaceFromContext()
    {
        var workspaceId = _workspaceContext?.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspaceId))
            return;

        if (!string.Equals(_db.CurrentWorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
            _db.OverrideWorkspaceId(workspaceId);
    }

    private static SessionLogEntity CloneSession(SessionLogEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            SourceType = source.SourceType,
            SessionId = source.SessionId,
            AgentDefinitionId = source.AgentDefinitionId,
            Title = source.Title,
            Model = source.Model,
            Started = source.Started,
            LastUpdated = source.LastUpdated,
            Status = source.Status,
            TurnCount = source.TurnCount,
            TotalTokens = source.TotalTokens,
            CursorSessionLabel = source.CursorSessionLabel,
            CopilotAvgSuccessScore = source.CopilotAvgSuccessScore,
            CopilotTotalNetTokens = source.CopilotTotalNetTokens,
            CopilotTotalNetPremiumRequests = source.CopilotTotalNetPremiumRequests,
            CopilotCompletedCount = source.CopilotCompletedCount,
            CopilotInProgressCount = source.CopilotInProgressCount,
            Project = source.Project,
            TargetFramework = source.TargetFramework,
            Repository = source.Repository,
            Branch = source.Branch,
            SourceFilePath = source.SourceFilePath,
            ContentHash = source.ContentHash,
            Turns = source.Turns.OrderBy(turn => turn.Id).Select(CloneTurn).ToList(),
        };

    private static SessionLogTurnEntity CloneTurn(SessionLogTurnEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            RequestId = source.RequestId,
            Timestamp = source.Timestamp,
            Model = source.Model,
            ModelProvider = source.ModelProvider,
            QueryText = source.QueryText,
            QueryTitle = source.QueryTitle,
            Response = source.Response,
            Interpretation = source.Interpretation,
            Status = source.Status,
            TokenCount = source.TokenCount,
            FailureNote = source.FailureNote,
            Score = source.Score,
            IsPremium = source.IsPremium,
            RawContextJson = source.RawContextJson,
            OriginalEntryJson = source.OriginalEntryJson,
            Actions = source.Actions.OrderBy(action => action.Order).Select(CloneAction).ToList(),
            Tags = source.Tags.Select(CloneTag).ToList(),
            ContextItems = source.ContextItems.OrderBy(context => context.Ordinal).Select(CloneContext).ToList(),
            ProcessingDialog = source.ProcessingDialog.OrderBy(dialog => dialog.Ordinal).Select(CloneDialog).ToList(),
            Commits = source.Commits.OrderBy(commit => commit.Ordinal).Select(CloneCommit).ToList(),
            StringListItems = source.StringListItems.OrderBy(item => item.Ordinal).Select(CloneStringListItem).ToList(),
        };

    private static SessionLogActionEntity CloneAction(SessionLogActionEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            Order = source.Order,
            Description = source.Description,
            Type = source.Type,
            Status = source.Status,
            FilePath = source.FilePath,
        };

    private static SessionLogTurnTagEntity CloneTag(SessionLogTurnTagEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            Tag = source.Tag,
        };

    private static SessionLogTurnContextEntity CloneContext(SessionLogTurnContextEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            Ordinal = source.Ordinal,
            ContextItem = source.ContextItem,
        };

    private static SessionLogProcessingDialogEntity CloneDialog(SessionLogProcessingDialogEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            Ordinal = source.Ordinal,
            Timestamp = source.Timestamp,
            Role = source.Role,
            Content = source.Content,
            Category = source.Category,
        };

    private static SessionLogCommitEntity CloneCommit(SessionLogCommitEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            Ordinal = source.Ordinal,
            Sha = source.Sha,
            Branch = source.Branch,
            Message = source.Message,
            Author = source.Author,
            CommitTimestamp = source.CommitTimestamp,
            FilesChangedJson = source.FilesChangedJson,
        };

    private static SessionLogTurnStringListEntity CloneStringListItem(SessionLogTurnStringListEntity source)
        => new()
        {
            WorkspaceId = source.WorkspaceId,
            ListType = source.ListType,
            Ordinal = source.Ordinal,
            Value = source.Value,
        };

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private void ThrowIfUncompensatedRepairBlocked()
    {
        if (_coordinator is null)
            return;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message);
        }

        if (status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true))
        {
            throw new InvalidOperationException(
                "Session-log workspace stamp repair is not transaction compensated while required turn transactions are active.");
        }
    }

    private static InvalidOperationException ToTransactionException(
        string operationName,
        TurnTransactionResult result)
    {
        var transactionId = string.IsNullOrWhiteSpace(result.TransactionId)
            ? "unassigned"
            : result.TransactionId;
        var message = string.IsNullOrWhiteSpace(result.Message)
            ? result.Reason.ToString()
            : result.Message;
        if (result.RollbackAttempted)
        {
            message = result.RollbackSucceeded
                ? $"{message} Rollback completed."
                : $"{message} Rollback failed: {result.RollbackError ?? "unknown error"}.";
        }

        return new InvalidOperationException(
            $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private enum MissingSnapshotRollbackPolicy
    {
        RestorePreMutation,
        RestorePostMutation,
    }

    private sealed record SessionGraphSnapshot(
        string SourceType,
        string SessionId,
        string WorkspaceId,
        SessionLogEntity? Session);

    private sealed record SubmitPayload(UnifiedSessionLogDto Dto, string? SourceFilePath, string? ContentHash);

    private sealed record SessionPayload(string SourceType, string SessionId);

    private sealed record OpenSessionPayload(string SourceType, string SessionId, string? Title, string? Model);

    private sealed record TurnPayload(string SourceType, string SessionId, UnifiedRequestEntryDto Turn);

    private sealed record DialogPayload(
        string SourceType,
        string SessionId,
        string RequestId,
        IReadOnlyList<ProcessingDialogItemDto> Items);

    private sealed record SectionPayload(
        string SourceType,
        string SessionId,
        string RequestId,
        string Section,
        UnifiedRequestEntryDto Payload);

    private sealed record ClearSectionPayload(
        string SourceType,
        string SessionId,
        string RequestId,
        string Section);

    private sealed record DeleteItemPayload(
        string SourceType,
        string SessionId,
        string RequestId,
        string Section,
        string ItemKey);

    private sealed record DeleteTurnPayload(string SourceType, string SessionId, string RequestId);
}
