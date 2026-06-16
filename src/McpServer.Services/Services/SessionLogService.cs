using System.Globalization;
using System.Text.Json;
using McpServer.Cqrs.Search;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Implements session log submit (upsert) and query with pagination (MVP-SUPPORT-011).
/// FR-SUPPORT-010: Persists session logs in 4NF-normalized SQLite tables via <see cref="McpDbContext"/>.
/// </summary>
public sealed class SessionLogService : ISessionLogService
{
    private const int MaxLimit = 1000;
    private const string SessionTurnComplianceError =
        "Compliance with Session Logging Requirements is not optional.";

    private readonly McpDbContext _db;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<SessionLogService> _logger;
    private readonly WorkspaceContext? _workspaceContext;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <remarks>
    /// TR-MCP-MT-003A: <paramref name="workspaceContext"/> is optional so the
    /// ingestion / batch import paths (which run without an HTTP scope) keep
    /// working; in those cases <c>WorkspaceId</c> defaults to empty string.
    /// </remarks>
    public SessionLogService(
        McpDbContext db,
        ILogger<SessionLogService> logger,
        IChangeEventBus? eventBus = null,
        WorkspaceContext? workspaceContext = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
        _workspaceContext = workspaceContext;
    }

    private string ResolveWorkspaceId() => _workspaceContext?.WorkspacePath ?? string.Empty;

    private void SyncDbWorkspaceFromContext()
    {
        var workspaceId = ResolveWorkspaceId();
        if (string.IsNullOrWhiteSpace(workspaceId))
            return;

        if (!string.Equals(_db.CurrentWorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
            _db.OverrideWorkspaceId(workspaceId);
    }

    private void StampWorkspaceId(SessionLogEntity session)
    {
        // BUG-SESSIONLOG-WS-002: the session row is stamped from the ambient
        // workspace ONLY when it has no stamp yet (new sessions). An existing
        // session is never re-stamped on update; that "moved" sessions between
        // workspaces and let child rows drift away from their parent.
        if (string.IsNullOrEmpty(session.WorkspaceId))
        {
            var workspaceId = ResolveWorkspaceId();
            if (string.IsNullOrEmpty(workspaceId))
            {
                // No explicit workspace context: defer to McpDbContext.SaveChangesAsync
                // which auto-stamps Added entities (children inherit the parent graph).
                return;
            }

            session.WorkspaceId = workspaceId;
        }

        // Children ALWAYS inherit the parent session's effective stamp so one
        // session never holds mixed WorkspaceIds.
        StampChildrenFromParent(session);
    }

    private static void StampChildrenFromParent(SessionLogEntity session)
    {
        var workspaceId = session.WorkspaceId;
        if (string.IsNullOrEmpty(workspaceId))
            return;

        foreach (var turn in session.Turns)
        {
            turn.WorkspaceId = workspaceId;
            foreach (var action in turn.Actions) action.WorkspaceId = workspaceId;
            foreach (var tag in turn.Tags) tag.WorkspaceId = workspaceId;
            foreach (var context in turn.ContextItems) context.WorkspaceId = workspaceId;
            foreach (var dialog in turn.ProcessingDialog) dialog.WorkspaceId = workspaceId;
            foreach (var commit in turn.Commits) commit.WorkspaceId = workspaceId;
            foreach (var stringItem in turn.StringListItems) stringItem.WorkspaceId = workspaceId;
        }
    }

    /// <inheritdoc />
    public async Task<long> SubmitAsync(UnifiedSessionLogDto dto, string? sourceFilePath = null, string? contentHash = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SyncDbWorkspaceFromContext();

        if (string.IsNullOrWhiteSpace(dto.SourceType))
            throw new ArgumentException("SourceType is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new ArgumentException("SessionId is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(dto.SessionId, dto.SourceType);
            if (sessionIdError is not null)
                throw new ArgumentException(sessionIdError, nameof(dto));
            if (dto.Turns is { Count: > 0 })
            {
                foreach (var turn in dto.Turns)
                {
                    var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(turn.RequestId);
                    if (requestIdError is not null)
                        throw new ArgumentException(requestIdError, nameof(dto));
                }
            }
        }

        var existing = await FindExistingSessionAsync(dto.SourceType, dto.SessionId, cancellationToken).ConfigureAwait(false);

        var wasCreated = existing is null;
        if (existing != null)
        {
            MapDtoToEntity(dto, existing);
            existing.SourceFilePath = sourceFilePath;
            existing.ContentHash = contentHash;
            UpsertTurns(existing, dto.Turns);
            RefreshSessionSummaryFromTurns(existing);
            _logger.LogInformation("Updated session log {SourceType}/{SessionId} (Id={Id})", dto.SourceType, dto.SessionId, existing.Id);
        }
        else
        {
            existing = new SessionLogEntity
            {
                SourceType = dto.SourceType,
                SessionId = dto.SessionId,
                SourceFilePath = sourceFilePath,
                ContentHash = contentHash
            };
            MapDtoToEntity(dto, existing);
            existing.Turns = MapNewTurns(dto.Turns);
            RefreshSessionSummaryFromTurns(existing);
            _db.SessionLogs.Add(existing);
            _logger.LogInformation("Created session log {SourceType}/{SessionId}", dto.SourceType, dto.SessionId);
        }

        await ResolveAgentDefinitionLinkAsync(dto, existing, cancellationToken).ConfigureAwait(false);
        StampWorkspaceId(existing);

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogWarning("UNIQUE constraint race for {SourceType}/{SessionId}, retrying as update", dto.SourceType, dto.SessionId);
            _db.ChangeTracker.Clear();

            existing = await FindExistingSessionAsync(dto.SourceType, dto.SessionId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Session log {dto.SourceType}/{dto.SessionId} disappeared after UNIQUE constraint failure.");

            MapDtoToEntity(dto, existing);
            existing.SourceFilePath = sourceFilePath;
            existing.ContentHash = contentHash;
            UpsertTurns(existing, dto.Turns);
            RefreshSessionSummaryFromTurns(existing);
            await ResolveAgentDefinitionLinkAsync(dto, existing, cancellationToken).ConfigureAwait(false);
            StampWorkspaceId(existing);

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated session log {SourceType}/{SessionId} (Id={Id}) after retry", dto.SourceType, dto.SessionId, existing.Id);
            wasCreated = false;
        }

        dto.AgentDefinitionId = existing.AgentDefinitionId;

        await PublishChangeSafeAsync(
            wasCreated ? ChangeEventActions.Created : ChangeEventActions.Updated,
            $"{dto.SourceType}/{dto.SessionId}",
            $"mcp://workspace/sessionlog/{dto.SourceType}/{dto.SessionId}",
            cancellationToken).ConfigureAwait(false);

        return existing.Id;
    }

    private Task<SessionLogEntity?> FindExistingSessionAsync(string sourceType, string sessionId, CancellationToken cancellationToken) =>
        _db.SessionLogs
            .Include(s => s.Turns)
                .ThenInclude(e => e.Actions)
            .Include(s => s.Turns)
                .ThenInclude(e => e.Tags)
            .Include(s => s.Turns)
                .ThenInclude(e => e.ContextItems)
            .Include(s => s.Turns)
                .ThenInclude(e => e.ProcessingDialog)
            .Include(s => s.Turns)
                .ThenInclude(e => e.Commits)
            .Include(s => s.Turns)
                .ThenInclude(e => e.StringListItems)
            .FirstOrDefaultAsync(s => s.SourceType == sourceType && s.SessionId == sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsUnchangedAsync(string sourceType, string sessionId, string contentHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(contentHash);
        SyncDbWorkspaceFromContext();

        return await _db.SessionLogs
            .AnyAsync(s => s.SourceType == sourceType
                        && s.SessionId == sessionId
                        && s.ContentHash == contentHash, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> AppendProcessingDialogAsync(
        string sourceType,
        string sessionId,
        string requestId,
        IReadOnlyList<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(items);
        SyncDbWorkspaceFromContext();

        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, sourceType);
        if (sessionIdError is not null)
            throw new ArgumentException(sessionIdError, nameof(sessionId));
        var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(requestId);
        if (requestIdError is not null)
            throw new ArgumentException(requestIdError, nameof(requestId));

        // BUG-SESSIONLOG-WS-001..004: child sets carry no workspace query filter,
        // so isolation comes from the explicit parent-session predicate here.
        var currentWorkspaceId = _db.CurrentWorkspaceId;
        var entry = await _db.SessionLogTurns
            .Include(e => e.ProcessingDialog)
            .FirstOrDefaultAsync(e =>
                e.SessionLog!.SourceType == sourceType
                && e.SessionLog.SessionId == sessionId
                && e.SessionLog.WorkspaceId == currentWorkspaceId
                && e.RequestId == requestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Entry not found: {sourceType}/{sessionId}/{requestId}");

        var nextOrdinal = entry.ProcessingDialog.Count > 0
            ? entry.ProcessingDialog.Max(p => p.Ordinal) + 1
            : 0;

        var workspaceId = ResolveWorkspaceId();
        foreach (var item in items)
        {
            entry.ProcessingDialog.Add(new SessionLogProcessingDialogEntity
            {
                Ordinal = nextOrdinal++,
                Timestamp = ParseDateTimeOffset(item.Timestamp) ?? DateTimeOffset.UtcNow,
                Role = item.Role ?? "model",
                Content = item.Content ?? string.Empty,
                Category = item.Category,
                WorkspaceId = workspaceId
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Appended {Count} dialog items to {SourceType}/{SessionId}/{RequestId}",
            items.Count, sourceType, sessionId, requestId);
        await PublishChangeSafeAsync(
            ChangeEventActions.Updated,
            $"{sourceType}/{sessionId}",
            $"mcp://workspace/sessionlog/{sourceType}/{sessionId}",
            cancellationToken).ConfigureAwait(false);

        return entry.ProcessingDialog.Count;
    }

    /// <inheritdoc />
    public async Task<SessionLogQueryResult> QueryAsync(SessionLogQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        SyncDbWorkspaceFromContext();

        var limit = Math.Clamp(request.Limit, 1, MaxLimit);
        var offset = Math.Max(request.Offset, 0);

        IQueryable<SessionLogEntity> query = _db.SessionLogs;

        if (!string.IsNullOrWhiteSpace(request.Agent))
            query = query.Where(s => s.SourceType == request.Agent);

        if (!string.IsNullOrWhiteSpace(request.AgentDefinitionId))
            query = query.Where(s => s.AgentDefinitionId == request.AgentDefinitionId);

        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            var modelFilter = request.Model;
            query = query.Where(s => s.Model != null && EF.Functions.Like(s.Model, "%" + modelFilter + "%"));
        }

        var allSessions = await query
            .Include(s => s.Turns.OrderBy(e => e.Id))
                .ThenInclude(e => e.Actions.OrderBy(a => a.Order))
            .Include(s => s.Turns)
                .ThenInclude(e => e.Tags)
            .Include(s => s.Turns)
                .ThenInclude(e => e.ContextItems.OrderBy(c => c.Ordinal))
            .Include(s => s.Turns)
                .ThenInclude(e => e.ProcessingDialog.OrderBy(p => p.Ordinal))
            .Include(s => s.Turns)
                .ThenInclude(e => e.Commits.OrderBy(c => c.Ordinal))
            .Include(s => s.Turns)
                .ThenInclude(e => e.StringListItems.OrderBy(sl => sl.Ordinal))
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<SessionLogEntity> filtered = allSessions;

        if (request.From.HasValue)
            filtered = filtered.Where(s => s.Started.HasValue && s.Started.Value >= request.From.Value);

        if (request.To.HasValue)
            filtered = filtered.Where(s => s.LastUpdated.HasValue && s.LastUpdated.Value <= request.To.Value);

        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            var matcher = BooleanSearchParser.Parse(request.Text);
            filtered = filtered.Where(s => s.Turns.Any(e => matcher(BuildSearchText(e))));
        }

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        var sessions = filteredList
            .OrderByDescending(s => s.LastUpdated ?? s.Started ?? DateTimeOffset.MinValue)
            .Skip(offset)
            .Take(limit)
            .ToList();

        var items = sessions.Select(MapEntityToDto).ToList();

        return new SessionLogQueryResult
        {
            TotalCount = totalCount,
            Limit = limit,
            Offset = offset,
            Items = items
        };
    }

    /// <inheritdoc />
    public async Task<bool> OpenSessionAsync(string sourceType, string sessionId, string? title = null, string? model = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        SyncDbWorkspaceFromContext();

        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, sourceType);
        if (sessionIdError is not null)
            throw new ArgumentException(sessionIdError, nameof(sessionId));

        var exists = await _db.SessionLogs
            .AnyAsync(s => s.SourceType == sourceType && s.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            return false;

        await SubmitAsync(new UnifiedSessionLogDto
        {
            SourceType = sourceType,
            SessionId = sessionId,
            Title = title,
            Model = model,
            Status = "in_progress",
        }, sourceFilePath: null, contentHash: null, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> RepairWorkspaceStampsAsync(bool dryRun = false, CancellationToken cancellationToken = default)
    {
        SyncDbWorkspaceFromContext();
        var sessions = await _db.SessionLogs
            .IgnoreQueryFilters()
            .Include(s => s.Turns)
                .ThenInclude(t => t.Actions)
            .Include(s => s.Turns)
                .ThenInclude(t => t.Tags)
            .Include(s => s.Turns)
                .ThenInclude(t => t.ContextItems)
            .Include(s => s.Turns)
                .ThenInclude(t => t.ProcessingDialog)
            .Include(s => s.Turns)
                .ThenInclude(t => t.Commits)
            .Include(s => s.Turns)
                .ThenInclude(t => t.StringListItems)
            .AsSplitQuery()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var changed = 0;
        foreach (var session in sessions)
        {
            var workspaceId = session.WorkspaceId;
            foreach (var turn in session.Turns)
            {
                changed += Restamp(turn.WorkspaceId, workspaceId, value => turn.WorkspaceId = value);
                foreach (var action in turn.Actions)
                    changed += Restamp(action.WorkspaceId, workspaceId, value => action.WorkspaceId = value);
                foreach (var tag in turn.Tags)
                    changed += Restamp(tag.WorkspaceId, workspaceId, value => tag.WorkspaceId = value);
                foreach (var context in turn.ContextItems)
                    changed += Restamp(context.WorkspaceId, workspaceId, value => context.WorkspaceId = value);
                foreach (var dialog in turn.ProcessingDialog)
                    changed += Restamp(dialog.WorkspaceId, workspaceId, value => dialog.WorkspaceId = value);
                foreach (var commit in turn.Commits)
                    changed += Restamp(commit.WorkspaceId, workspaceId, value => commit.WorkspaceId = value);
                foreach (var stringItem in turn.StringListItems)
                    changed += Restamp(stringItem.WorkspaceId, workspaceId, value => stringItem.WorkspaceId = value);
            }
        }

        if (changed > 0 && !dryRun)
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        else if (dryRun)
            _db.ChangeTracker.Clear();

        return changed;

        static int Restamp(string current, string workspaceId, Action<string> setWorkspaceId)
        {
            if (string.Equals(current, workspaceId, StringComparison.Ordinal))
                return 0;

            setWorkspaceId(workspaceId);
            return 1;
        }
    }

    /// <inheritdoc />
    public async Task<UnifiedSessionLogDto?> GetAsync(string sourceType, string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        SyncDbWorkspaceFromContext();

        var entity = await _db.SessionLogs
            .Include(s => s.Turns.OrderBy(e => e.Id))
                .ThenInclude(e => e.Actions.OrderBy(a => a.Order))
            .Include(s => s.Turns)
                .ThenInclude(e => e.Tags)
            .Include(s => s.Turns)
                .ThenInclude(e => e.ContextItems.OrderBy(c => c.Ordinal))
            .Include(s => s.Turns)
                .ThenInclude(e => e.ProcessingDialog.OrderBy(p => p.Ordinal))
            .Include(s => s.Turns)
                .ThenInclude(e => e.Commits.OrderBy(c => c.Ordinal))
            .Include(s => s.Turns)
                .ThenInclude(e => e.StringListItems.OrderBy(sl => sl.Ordinal))
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceType == sourceType && s.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : MapEntityToDto(entity);
    }

    /// <inheritdoc />
    public async Task<long> UpsertTurnAsync(string sourceType, string sessionId, UnifiedRequestEntryDto turn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(turn);
        SyncDbWorkspaceFromContext();

        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, sourceType);
        if (sessionIdError is not null)
            throw new ArgumentException(sessionIdError, nameof(sessionId));

        var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(turn.RequestId);
        if (requestIdError is not null)
            throw new ArgumentException(requestIdError, nameof(turn));

        var session = await FindExistingSessionAsync(sourceType, sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session not found: {sourceType}/{sessionId}");

        var existingTurn = session.Turns.FirstOrDefault(t => t.RequestId == turn.RequestId);
        SessionLogTurnEntity persistedTurn;

        if (existingTurn is null)
        {
            persistedTurn = MapSingleEntry(turn);
            session.Turns.Add(persistedTurn);
        }
        else
        {
            UpdateEntryFromDto(existingTurn, turn, mergeOmittedFields: true);
            persistedTurn = existingTurn;
        }

        ValidateTerminalTurnCompliance(MapTurnEntityToDto(persistedTurn));

        // BUG-SESSIONLOG-WS-002: the turn and its children inherit the PARENT
        // session's stamp; the ambient workspace never re-stamps the session here.
        StampTurnChildren(persistedTurn, session.WorkspaceId);

        RefreshSessionSummaryFromTurns(session);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await PublishChangeSafeAsync(
            ChangeEventActions.Updated,
            $"{sourceType}/{sessionId}",
            $"mcp://workspace/sessionlog/{sourceType}/{sessionId}",
            cancellationToken).ConfigureAwait(false);

        return persistedTurn.Id;
    }

    /// <summary>
    /// BUG-SESSIONLOG-WS-002: stamp a turn and all of its loaded child rows with
    /// the parent session's WorkspaceId so a session never holds mixed stamps.
    /// No-op when the parent has no stamp (the ambient auto-stamp path handles it).
    /// </summary>
    private static void StampTurnChildren(SessionLogTurnEntity turn, string? workspaceId)
    {
        if (string.IsNullOrEmpty(workspaceId))
            return;

        turn.WorkspaceId = workspaceId;
        foreach (var action in turn.Actions) action.WorkspaceId = workspaceId;
        foreach (var tag in turn.Tags) tag.WorkspaceId = workspaceId;
        foreach (var context in turn.ContextItems) context.WorkspaceId = workspaceId;
        foreach (var dialog in turn.ProcessingDialog) dialog.WorkspaceId = workspaceId;
        foreach (var commit in turn.Commits) commit.WorkspaceId = workspaceId;
        foreach (var stringItem in turn.StringListItems) stringItem.WorkspaceId = workspaceId;
    }

    /// <inheritdoc />
    public async Task<long> ReplaceTurnAsync(string sourceType, string sessionId, UnifiedRequestEntryDto turn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(turn);
        SyncDbWorkspaceFromContext();

        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, sourceType);
        if (sessionIdError is not null)
            throw new ArgumentException(sessionIdError, nameof(sessionId));

        var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(turn.RequestId);
        if (requestIdError is not null)
            throw new ArgumentException(requestIdError, nameof(turn));

        var session = await FindExistingSessionAsync(sourceType, sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session not found: {sourceType}/{sessionId}");

        var existingTurn = session.Turns.FirstOrDefault(t => t.RequestId == turn.RequestId);
        SessionLogTurnEntity persistedTurn;

        if (existingTurn is null)
        {
            persistedTurn = MapSingleEntry(turn);
            session.Turns.Add(persistedTurn);
        }
        else
        {
            // FR-SUPPORT-010G: PUT replace - omitted scalars reset, collections
            // become exactly the payload (omitted/empty cleared).
            UpdateEntryFromDto(existingTurn, turn, mergeOmittedFields: false);
            persistedTurn = existingTurn;
        }

        ValidateTerminalTurnCompliance(MapTurnEntityToDto(persistedTurn));
        StampTurnChildren(persistedTurn, session.WorkspaceId);
        RefreshSessionSummaryFromTurns(session);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublishChangeSafeAsync(
            ChangeEventActions.Updated,
            $"{sourceType}/{sessionId}",
            $"mcp://workspace/sessionlog/{sourceType}/{sessionId}",
            cancellationToken).ConfigureAwait(false);

        return persistedTurn.Id;
    }

    /// <inheritdoc />
    public async Task<bool> ReplaceTurnSectionAsync(string sourceType, string sessionId, string requestId, string section, UnifiedRequestEntryDto payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(payload);
        SyncDbWorkspaceFromContext();

        var parsed = ParseSection(section); // throws ArgumentException on unknown section
        ValidateTurnIdentifiers(sourceType, sessionId, requestId);

        var session = await FindExistingSessionAsync(sourceType, sessionId, cancellationToken).ConfigureAwait(false);
        var turnEntity = session?.Turns.FirstOrDefault(t => t.RequestId == requestId);
        if (session is null || turnEntity is null)
            return false;

        ApplySectionReplace(turnEntity, parsed, payload);
        StampTurnChildren(turnEntity, session.WorkspaceId);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublishChangeSafeAsync(
            ChangeEventActions.Updated,
            $"{sourceType}/{sessionId}",
            $"mcp://workspace/sessionlog/{sourceType}/{sessionId}",
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public Task<bool> ClearTurnSectionAsync(string sourceType, string sessionId, string requestId, string section, CancellationToken cancellationToken = default)
        => ReplaceTurnSectionAsync(sourceType, sessionId, requestId, section, new UnifiedRequestEntryDto { RequestId = requestId }, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteTurnItemAsync(string sourceType, string sessionId, string requestId, string section, string itemKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(itemKey);
        SyncDbWorkspaceFromContext();

        var parsed = ParseSection(section);
        ValidateTurnIdentifiers(sourceType, sessionId, requestId);

        var session = await FindExistingSessionAsync(sourceType, sessionId, cancellationToken).ConfigureAwait(false);
        var turnEntity = session?.Turns.FirstOrDefault(t => t.RequestId == requestId);
        if (session is null || turnEntity is null)
            return false;

        if (!RemoveSectionItem(turnEntity, parsed, itemKey))
            return false;

        StampTurnChildren(turnEntity, session.WorkspaceId);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublishChangeSafeAsync(
            ChangeEventActions.Updated,
            $"{sourceType}/{sessionId}",
            $"mcp://workspace/sessionlog/{sourceType}/{sessionId}",
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTurnAsync(string sourceType, string sessionId, string requestId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(requestId);
        SyncDbWorkspaceFromContext();
        ValidateTurnIdentifiers(sourceType, sessionId, requestId);

        var session = await FindExistingSessionAsync(sourceType, sessionId, cancellationToken).ConfigureAwait(false);
        var turnEntity = session?.Turns.FirstOrDefault(t => t.RequestId == requestId);
        if (session is null || turnEntity is null)
            return false;

        var sessionRowId = session.Id;
        var turnId = turnEntity.Id;

        // Child FKs are DeleteBehavior.Restrict (no cascade); soft-delete bottom-up with
        // bulk ExecuteUpdate so durable session-log rows retain deletion metadata.
        // Drop tracked entities first so they cannot conflict with bulk updates.
        _db.ChangeTracker.Clear();
        await SoftDeleteTurnRowsAsync(
            turnId,
            DateTimeOffset.UtcNow,
            "session_log_turn_delete",
            cancellationToken).ConfigureAwait(false);

        var remaining = await _db.SessionLogTurns
            .CountAsync(t => t.SessionLogId == sessionRowId, cancellationToken).ConfigureAwait(false);
        await _db.SessionLogs
            .Where(s => s.Id == sessionRowId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.TurnCount, remaining), cancellationToken)
            .ConfigureAwait(false);

        await PublishChangeSafeAsync(
            ChangeEventActions.Updated,
            $"{sourceType}/{sessionId}",
            $"mcp://workspace/sessionlog/{sourceType}/{sessionId}",
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSessionAsync(string sourceType, string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        SyncDbWorkspaceFromContext();

        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, sourceType);
        if (sessionIdError is not null)
            throw new ArgumentException(sessionIdError, nameof(sessionId));

        var session = await FindExistingSessionAsync(sourceType, sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
            return false;

        var sessionRowId = session.Id;
        var turnIds = session.Turns.Select(t => t.Id).ToList();
        var deletedAtUtc = DateTimeOffset.UtcNow;

        _db.ChangeTracker.Clear();
        foreach (var turnId in turnIds)
            await SoftDeleteTurnRowsAsync(
                turnId,
                deletedAtUtc,
                "session_log_session_delete",
                cancellationToken).ConfigureAwait(false);
        await SoftDeleteRowsAsync(
                _db.SessionLogs.Where(s => s.Id == sessionRowId),
                deletedAtUtc,
                "session_log_session_delete",
                cancellationToken)
            .ConfigureAwait(false);

        await PublishChangeSafeAsync(
            ChangeEventActions.Deleted,
            $"{sourceType}/{sessionId}",
            $"mcp://workspace/sessionlog/{sourceType}/{sessionId}",
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// FR-SUPPORT-010G / TR-MCP-DB-003: bulk soft-delete every child row of a turn
    /// and the turn row itself (bottom-up). Child sets carry no workspace query
    /// filter, so the turn id alone scopes the update.
    /// </summary>
    private async Task SoftDeleteTurnRowsAsync(
        long turnId,
        DateTimeOffset deletedAtUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        await SoftDeleteRowsAsync(_db.SessionLogActions.Where(x => x.SessionLogTurnId == turnId), deletedAtUtc, reason, cancellationToken).ConfigureAwait(false);
        await SoftDeleteRowsAsync(_db.SessionLogTurnTags.Where(x => x.SessionLogTurnId == turnId), deletedAtUtc, reason, cancellationToken).ConfigureAwait(false);
        await SoftDeleteRowsAsync(_db.SessionLogTurnContexts.Where(x => x.SessionLogTurnId == turnId), deletedAtUtc, reason, cancellationToken).ConfigureAwait(false);
        await SoftDeleteRowsAsync(_db.SessionLogProcessingDialogs.Where(x => x.SessionLogTurnId == turnId), deletedAtUtc, reason, cancellationToken).ConfigureAwait(false);
        await SoftDeleteRowsAsync(_db.SessionLogCommits.Where(x => x.SessionLogTurnId == turnId), deletedAtUtc, reason, cancellationToken).ConfigureAwait(false);
        await SoftDeleteRowsAsync(_db.SessionLogTurnStringLists.Where(x => x.SessionLogTurnId == turnId), deletedAtUtc, reason, cancellationToken).ConfigureAwait(false);
        await SoftDeleteRowsAsync(_db.SessionLogTurns.Where(x => x.Id == turnId), deletedAtUtc, reason, cancellationToken).ConfigureAwait(false);
    }

    private static Task<int> SoftDeleteRowsAsync<TEntity>(
        IQueryable<TEntity> query,
        DateTimeOffset deletedAtUtc,
        string reason,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        return query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(entity => EF.Property<bool>(entity, "IsDeleted"), true)
                .SetProperty(entity => EF.Property<DateTimeOffset?>(entity, "DeletedAtUtc"), deletedAtUtc)
                .SetProperty(entity => EF.Property<string?>(entity, "DeletedBy"), nameof(SessionLogService))
                .SetProperty(entity => EF.Property<string?>(entity, "DeleteReason"), reason),
            cancellationToken);
    }

    private void ValidateTurnIdentifiers(string sourceType, string sessionId, string requestId)
    {
        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, sourceType);
        if (sessionIdError is not null)
            throw new ArgumentException(sessionIdError, nameof(sessionId));
        var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(requestId);
        if (requestIdError is not null)
            throw new ArgumentException(requestIdError, nameof(requestId));
    }

    private void ApplySectionReplace(SessionLogTurnEntity turn, TurnSection section, UnifiedRequestEntryDto payload)
    {
        switch (section)
        {
            case TurnSection.Actions: ReplaceCollection(turn.Actions, MapActions(payload.Actions)); break;
            case TurnSection.Tags: ReplaceCollection(turn.Tags, MapTags(payload.Tags)); break;
            case TurnSection.Context: ReplaceCollection(turn.ContextItems, MapContextItems(payload.ContextList)); break;
            case TurnSection.Dialog: ReplaceCollection(turn.ProcessingDialog, MapProcessingDialog(payload.ProcessingDialog)); break;
            case TurnSection.Commits: ReplaceCollection(turn.Commits, MapCommits(payload.Commits)); break;
            case TurnSection.DesignDecisions: ReplaceStringListItems(turn, "DesignDecision", payload.DesignDecisions, mergeOmittedFields: false); break;
            case TurnSection.RequirementsDiscovered: ReplaceStringListItems(turn, "Requirement", payload.RequirementsDiscovered, mergeOmittedFields: false); break;
            case TurnSection.FilesModified: ReplaceStringListItems(turn, "FileModified", payload.FilesModified, mergeOmittedFields: false); break;
            case TurnSection.Blockers: ReplaceStringListItems(turn, "Blocker", payload.Blockers, mergeOmittedFields: false); break;
            default: throw new ArgumentOutOfRangeException(nameof(section), section, "Unhandled section.");
        }
    }

    private bool RemoveSectionItem(SessionLogTurnEntity turn, TurnSection section, string itemKey)
    {
        return section switch
        {
            TurnSection.Tags => RemoveMatching(turn.Tags, t => string.Equals(t.Tag, itemKey, StringComparison.Ordinal)),
            TurnSection.Context => RemoveMatching(turn.ContextItems, c => string.Equals(c.ContextItem, itemKey, StringComparison.Ordinal)),
            TurnSection.Commits => RemoveMatching(turn.Commits, c => string.Equals(c.Sha, itemKey, StringComparison.Ordinal)),
            TurnSection.Actions => int.TryParse(itemKey, out var order) && RemoveMatching(turn.Actions, a => a.Order == order),
            TurnSection.Dialog => int.TryParse(itemKey, out var ordinal) && RemoveMatching(turn.ProcessingDialog, d => d.Ordinal == ordinal),
            TurnSection.DesignDecisions => RemoveStringListItem(turn, "DesignDecision", itemKey),
            TurnSection.RequirementsDiscovered => RemoveStringListItem(turn, "Requirement", itemKey),
            TurnSection.FilesModified => RemoveStringListItem(turn, "FileModified", itemKey),
            TurnSection.Blockers => RemoveStringListItem(turn, "Blocker", itemKey),
            _ => false,
        };
    }

    private bool RemoveMatching<TEntity>(ICollection<TEntity> target, Func<TEntity, bool> predicate)
        where TEntity : class
    {
        var matches = target.Where(predicate).ToList();
        if (matches.Count == 0)
            return false;
        foreach (var match in matches)
        {
            _db.Remove(match);
            target.Remove(match);
        }
        return true;
    }

    private bool RemoveStringListItem(SessionLogTurnEntity turn, string listType, string value)
    {
        var matches = turn.StringListItems
            .Where(i => i.ListType == listType && string.Equals(i.Value, value, StringComparison.Ordinal))
            .ToList();
        if (matches.Count == 0)
            return false;
        foreach (var match in matches)
        {
            _db.Remove(match);
            turn.StringListItems.Remove(match);
        }
        return true;
    }

    /// <summary>FR-SUPPORT-010G: logical turn sections addressable by the replace/remove API.</summary>
    private enum TurnSection
    {
        Actions,
        Tags,
        Context,
        Dialog,
        Commits,
        DesignDecisions,
        RequirementsDiscovered,
        FilesModified,
        Blockers,
    }

    private static TurnSection ParseSection(string section)
    {
        return (section?.Trim().ToLowerInvariant()) switch
        {
            "actions" or "action" => TurnSection.Actions,
            "tags" or "tag" => TurnSection.Tags,
            "context" or "contextlist" or "contextitems" => TurnSection.Context,
            "dialog" or "processingdialog" => TurnSection.Dialog,
            "commits" or "commit" => TurnSection.Commits,
            "designdecisions" or "designdecision" or "decisions" => TurnSection.DesignDecisions,
            "requirementsdiscovered" or "requirements" => TurnSection.RequirementsDiscovered,
            "filesmodified" or "files" => TurnSection.FilesModified,
            "blockers" or "blocker" => TurnSection.Blockers,
            _ => throw new ArgumentException(
                $"Unknown session-log turn section '{section}'. Valid sections: actions, tags, context, dialog, commits, designDecisions, requirementsDiscovered, filesModified, blockers.",
                nameof(section)),
        };
    }

    private void ReplaceCollection<TEntity>(ICollection<TEntity> target, List<TEntity> replacement)
        where TEntity : class
    {
        foreach (var existing in target.ToList())
            _db.Remove(existing);
        target.Clear();
        foreach (var item in replacement)
            target.Add(item);
    }

    private static void ValidateTerminalTurnCompliance(UnifiedRequestEntryDto turn)
    {
        if (!IsTerminalTurnStatus(turn.Status))
            return;

        var decisionCount = (turn.DesignDecisions?.Count(static value => !string.IsNullOrWhiteSpace(value)) ?? 0)
            + (turn.ProcessingDialog?.Count(static item =>
                string.Equals(item.Category, "decision", StringComparison.OrdinalIgnoreCase)) ?? 0);
        var actionCount = turn.Actions?.Count ?? 0;
        var commitCount = turn.Commits?.Count ?? 0;

        if (decisionCount > 0 || actionCount > 0 || commitCount > 0)
            return;

        throw new ArgumentException(
            $"Cannot close session turn '{turn.RequestId}' with status '{turn.Status}' because the payload contains no decision, action, or commit items. {SessionTurnComplianceError} Add at least one design decision, session action, or commit entry before retrying.",
            nameof(turn));
    }

    private static bool IsTerminalTurnStatus(string? status) =>
        status is not null
        && (string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase));

    private static string BuildSearchText(SessionLogTurnEntity turn)
        => string.Join(
            " ",
            new[] { turn.QueryText, turn.QueryTitle, turn.Response, turn.Interpretation }
                .Where(static value => !string.IsNullOrWhiteSpace(value)));

    private async Task ResolveAgentDefinitionLinkAsync(UnifiedSessionLogDto dto, SessionLogEntity entity, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(dto.AgentDefinitionId))
        {
            entity.AgentDefinitionId = dto.AgentDefinitionId;
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.SourceType))
            return;

        var linkedAgentId = await _db.AgentDefinitions
            .IgnoreQueryFilters()
            .Where(a => a.Id.ToLower() == dto.SourceType!.ToLower())
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(linkedAgentId))
        {
            entity.AgentDefinitionId = linkedAgentId;
            dto.AgentDefinitionId = linkedAgentId;
        }
    }

    private static void MapDtoToEntity(UnifiedSessionLogDto dto, SessionLogEntity entity)
    {
        // FR-SUPPORT-010F: ADDITIVE merge. Agents routinely send partial session
        // payloads (e.g. a bare status-only close); omitted (null) fields must
        // never clobber existing values. TurnCount/Started/LastUpdated are
        // recomputed from turns by RefreshSessionSummaryFromTurns after mapping.
        if (dto.Title is not null) entity.Title = dto.Title;
        if (dto.Model is not null) entity.Model = dto.Model;
        if (dto.AgentDefinitionId is not null) entity.AgentDefinitionId = dto.AgentDefinitionId;
        if (ParseDateTimeOffset(dto.Started) is { } started) entity.Started = started;
        if (ParseDateTimeOffset(dto.LastUpdated) is { } lastUpdated) entity.LastUpdated = lastUpdated;
        if (dto.Status is not null) entity.Status = dto.Status;
        if (dto.TurnCount > 0) entity.TurnCount = dto.TurnCount;
        if (dto.TotalTokens is not null) entity.TotalTokens = dto.TotalTokens;
        if (dto.CursorSessionLabel is not null) entity.CursorSessionLabel = dto.CursorSessionLabel;

        if (dto.CopilotStatistics is { } stats)
        {
            entity.CopilotAvgSuccessScore = stats.AverageSuccessScore;
            entity.CopilotTotalNetTokens = stats.TotalNetTokens;
            entity.CopilotTotalNetPremiumRequests = stats.TotalNetPremiumRequests;
            entity.CopilotCompletedCount = stats.CompletedCount;
            entity.CopilotInProgressCount = stats.InProgressCount;
        }

        if (dto.Workspace is { } ws)
        {
            entity.Project = ws.Project;
            entity.TargetFramework = ws.TargetFramework;
            entity.Repository = ws.Repository;
            entity.Branch = ws.Branch;
        }
    }

    private static void RefreshSessionSummaryFromTurns(SessionLogEntity session)
    {
        var turns = session.Turns.ToList();
        session.TurnCount = turns.Count;

        var timestamps = turns
            .Select(static turn => turn.Timestamp)
            .Where(static timestamp => timestamp.HasValue)
            .Select(static timestamp => timestamp!.Value)
            .ToList();

        if (timestamps.Count == 0)
        {
            return;
        }

        var earliest = timestamps.Min();
        var latest = timestamps.Max();
        if (session.Started is null || earliest < session.Started.Value)
        {
            session.Started = earliest;
        }

        if (session.LastUpdated is null || latest > session.LastUpdated.Value)
        {
            session.LastUpdated = latest;
        }
    }

    private void UpsertTurns(SessionLogEntity session, List<UnifiedRequestEntryDto>? dtoTurns)
    {
        var incoming = dtoTurns ?? [];
        var deduped = new List<UnifiedRequestEntryDto>();
        var seenRequestIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = incoming.Count - 1; i >= 0; i--)
        {
            var dto = incoming[i];
            if (dto.RequestId == null || seenRequestIds.Add(dto.RequestId))
                deduped.Add(dto);
        }
        deduped.Reverse();

        var existingByRequestId = session.Turns
            .Where(e => e.RequestId != null)
            .ToDictionary(e => e.RequestId!, StringComparer.Ordinal);

        foreach (var dto in deduped)
        {
            if (dto.RequestId != null && existingByRequestId.TryGetValue(dto.RequestId, out var existingEntry))
            {
                // FR-SUPPORT-010F: whole-session submit merges turns additively -
                // omitted turn fields never clobber previously persisted values.
                UpdateEntryFromDto(existingEntry, dto, mergeOmittedFields: true);
            }
            else
            {
                var newEntry = MapSingleEntry(dto);
                session.Turns.Add(newEntry);
            }
        }
    }

    private void UpdateEntryFromDto(
        SessionLogTurnEntity entity,
        UnifiedRequestEntryDto dto,
        bool mergeOmittedFields = false)
    {
        entity.Timestamp = ApplyValue(entity.Timestamp, ParseDateTimeOffset(dto.Timestamp), dto.Timestamp is not null, mergeOmittedFields);
        entity.Model = ApplyValue(entity.Model, dto.Model, dto.Model is not null, mergeOmittedFields);
        entity.ModelProvider = ApplyValue(entity.ModelProvider, dto.ModelProvider, dto.ModelProvider is not null, mergeOmittedFields);
        entity.QueryText = ApplyValue(entity.QueryText, dto.QueryText, dto.QueryText is not null, mergeOmittedFields);
        entity.QueryTitle = ApplyValue(entity.QueryTitle, dto.QueryTitle, dto.QueryTitle is not null, mergeOmittedFields);
        entity.Response = ApplyValue(entity.Response, dto.Response, dto.Response is not null, mergeOmittedFields);
        entity.Interpretation = ApplyValue(entity.Interpretation, dto.Interpretation, dto.Interpretation is not null, mergeOmittedFields);
        entity.Status = ApplyValue(entity.Status, dto.Status, dto.Status is not null, mergeOmittedFields);
        entity.TokenCount = ApplyValue(entity.TokenCount, dto.TokenCount, dto.TokenCount.HasValue, mergeOmittedFields);
        entity.FailureNote = ApplyValue(entity.FailureNote, dto.FailureNote, dto.FailureNote is not null, mergeOmittedFields);
        entity.Score = ApplyValue(entity.Score, dto.Score, dto.Score.HasValue, mergeOmittedFields);
        entity.IsPremium = ApplyValue(entity.IsPremium, dto.IsPremium, dto.IsPremium.HasValue, mergeOmittedFields);
        entity.RawContextJson = ApplyValue(entity.RawContextJson, SerializeJson(dto.RawContext), dto.RawContext is not null, mergeOmittedFields);
        entity.OriginalEntryJson = ApplyValue(entity.OriginalEntryJson, SerializeJson(dto.OriginalEntry), dto.OriginalEntry is not null, mergeOmittedFields);

        if (mergeOmittedFields)
        {
            // PATCH/additive: omitted collections preserved, items appended.
            MergeCollection(entity.Actions, dto.Actions, MapActions, SameAction, mergeOmittedFields);
            MergeCollection(entity.Tags, dto.Tags, MapTags, SameTag, mergeOmittedFields);
            MergeCollection(entity.ContextItems, dto.ContextList, MapContextItems, SameContextItem, mergeOmittedFields);
            MergeCollection(entity.ProcessingDialog, dto.ProcessingDialog, MapProcessingDialog, SameProcessingDialog, mergeOmittedFields);
            MergeCollection(entity.Commits, dto.Commits, MapCommits, SameCommit, mergeOmittedFields);
        }
        else
        {
            // FR-SUPPORT-010G PUT/replace: each collection becomes exactly the
            // payload; omitted or empty collections are cleared.
            ReplaceCollection(entity.Actions, MapActions(dto.Actions));
            ReplaceCollection(entity.Tags, MapTags(dto.Tags));
            ReplaceCollection(entity.ContextItems, MapContextItems(dto.ContextList));
            ReplaceCollection(entity.ProcessingDialog, MapProcessingDialog(dto.ProcessingDialog));
            ReplaceCollection(entity.Commits, MapCommits(dto.Commits));
        }

        ReplaceStringListItems(entity, "DesignDecision", dto.DesignDecisions, mergeOmittedFields);
        ReplaceStringListItems(entity, "Requirement", dto.RequirementsDiscovered, mergeOmittedFields);
        ReplaceStringListItems(entity, "FileModified", dto.FilesModified, mergeOmittedFields);
        ReplaceStringListItems(entity, "Blocker", dto.Blockers, mergeOmittedFields);
    }

    private static T ApplyValue<T>(
        T current,
        T incoming,
        bool supplied,
        bool mergeOmittedFields)
    {
        return !mergeOmittedFields || supplied ? incoming : current;
    }

    private static void MergeCollection<TIncoming, TEntity>(
        ICollection<TEntity> target,
        TIncoming? incoming,
        Func<TIncoming?, List<TEntity>> map,
        Func<TEntity, TEntity, bool> same,
        bool mergeOmittedFields)
        where TEntity : class
    {
        if (incoming is null)
        {
            return;
        }

        foreach (var item in map(incoming))
        {
            if (!target.Any(existing => same(existing, item)))
            {
                target.Add(item);
            }
        }
    }

    private void ReplaceStringListItems(
        SessionLogTurnEntity entity,
        string listType,
        List<string>? incoming,
        bool mergeOmittedFields)
    {
        if (!mergeOmittedFields)
        {
            // FR-SUPPORT-010G PUT/replace: drop existing items of this list type
            // and set to the payload; null/empty clears the list. Despite the
            // method name, the additive path below never actually replaced.
            foreach (var existing in entity.StringListItems.Where(item => item.ListType == listType).ToList())
            {
                _db.Remove(existing);
                entity.StringListItems.Remove(existing);
            }

            var values = incoming ?? [];
            for (var i = 0; i < values.Count; i++)
            {
                entity.StringListItems.Add(new SessionLogTurnStringListEntity
                {
                    ListType = listType,
                    Ordinal = i,
                    Value = values[i]
                });
            }

            return;
        }

        if (incoming is null)
        {
            return;
        }

        var existingValues = entity.StringListItems
            .Where(item => item.ListType == listType)
            .Select(item => item.Value)
            .ToHashSet(StringComparer.Ordinal);
        var ordinal = entity.StringListItems
            .Where(item => item.ListType == listType)
            .Select(item => item.Ordinal)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        foreach (var value in incoming.Where(value => existingValues.Add(value)))
        {
            entity.StringListItems.Add(new SessionLogTurnStringListEntity
            {
                ListType = listType,
                Ordinal = ordinal++,
                Value = value
            });
        }
    }

    private static List<SessionLogTurnEntity> MapNewTurns(List<UnifiedRequestEntryDto>? turns)
    {
        if (turns is null or { Count: 0 })
            return [];

        return turns.Select(MapSingleEntry).ToList();
    }

    private static SessionLogTurnEntity MapSingleEntry(UnifiedRequestEntryDto e)
    {
        return new SessionLogTurnEntity
        {
            RequestId = e.RequestId,
            Timestamp = ParseDateTimeOffset(e.Timestamp),
            Model = e.Model,
            ModelProvider = e.ModelProvider,
            QueryText = e.QueryText,
            QueryTitle = e.QueryTitle,
            Response = e.Response,
            Interpretation = e.Interpretation,
            Status = e.Status,
            TokenCount = e.TokenCount,
            FailureNote = e.FailureNote,
            Score = e.Score,
            IsPremium = e.IsPremium,
            RawContextJson = SerializeJson(e.RawContext),
            OriginalEntryJson = SerializeJson(e.OriginalEntry),
            Actions = MapActions(e.Actions),
            Tags = MapTags(e.Tags),
            ContextItems = MapContextItems(e.ContextList),
            ProcessingDialog = MapProcessingDialog(e.ProcessingDialog),
            Commits = MapCommits(e.Commits),
            StringListItems = MapStringListItems(e)
        };
    }

    private static List<SessionLogActionEntity> MapActions(List<UnifiedActionDto>? actions)
    {
        return actions?.Select((a, i) => new SessionLogActionEntity
        {
            Order = a.Order > 0 ? a.Order : i,
            Description = a.Description,
            Type = a.Type,
            Status = a.Status,
            FilePath = a.FilePath
        }).ToList() ?? [];
    }

    private static List<SessionLogTurnTagEntity> MapTags(List<string>? tags)
    {
        return tags?.Select(t => new SessionLogTurnTagEntity { Tag = t }).ToList() ?? [];
    }

    private static List<SessionLogTurnContextEntity> MapContextItems(List<string>? contextList)
    {
        return contextList?.Select((c, i) => new SessionLogTurnContextEntity
        {
            Ordinal = i,
            ContextItem = c
        }).ToList() ?? [];
    }

    private static List<SessionLogProcessingDialogEntity> MapProcessingDialog(List<ProcessingDialogItemDto>? dialog)
    {
        return dialog?.Select((d, i) => new SessionLogProcessingDialogEntity
        {
            Ordinal = i,
            Timestamp = ParseDateTimeOffset(d.Timestamp) ?? DateTimeOffset.UtcNow,
            Role = d.Role ?? "model",
            Content = d.Content ?? string.Empty,
            Category = d.Category
        }).ToList() ?? [];
    }

    private static List<SessionLogCommitEntity> MapCommits(List<SessionLogCommitDto>? commits)
    {
        return commits?.Select((c, i) => new SessionLogCommitEntity
        {
            Ordinal = i,
            Sha = c.Sha,
            Branch = c.Branch,
            Message = c.Message,
            Author = c.Author,
            CommitTimestamp = ParseDateTimeOffset(c.Timestamp),
            FilesChangedJson = c.FilesChanged is { Count: > 0 }
                ? JsonSerializer.Serialize(c.FilesChanged)
                : null
        }).ToList() ?? [];
    }

    private static bool SameAction(SessionLogActionEntity left, SessionLogActionEntity right) =>
        left.Order == right.Order
        && string.Equals(left.Description, right.Description, StringComparison.Ordinal)
        && string.Equals(left.Type, right.Type, StringComparison.Ordinal)
        && string.Equals(left.Status, right.Status, StringComparison.Ordinal)
        && string.Equals(left.FilePath, right.FilePath, StringComparison.Ordinal);

    private static bool SameTag(SessionLogTurnTagEntity left, SessionLogTurnTagEntity right) =>
        string.Equals(left.Tag, right.Tag, StringComparison.Ordinal);

    private static bool SameContextItem(SessionLogTurnContextEntity left, SessionLogTurnContextEntity right) =>
        string.Equals(left.ContextItem, right.ContextItem, StringComparison.Ordinal);

    private static bool SameProcessingDialog(SessionLogProcessingDialogEntity left, SessionLogProcessingDialogEntity right) =>
        left.Timestamp == right.Timestamp
        && string.Equals(left.Role, right.Role, StringComparison.Ordinal)
        && string.Equals(left.Content, right.Content, StringComparison.Ordinal)
        && string.Equals(left.Category, right.Category, StringComparison.Ordinal);

    private static bool SameCommit(SessionLogCommitEntity left, SessionLogCommitEntity right)
    {
        if (!string.IsNullOrWhiteSpace(left.Sha) || !string.IsNullOrWhiteSpace(right.Sha))
            return string.Equals(left.Sha, right.Sha, StringComparison.Ordinal);

        return string.Equals(left.Branch, right.Branch, StringComparison.Ordinal)
               && string.Equals(left.Message, right.Message, StringComparison.Ordinal)
               && string.Equals(left.Author, right.Author, StringComparison.Ordinal)
               && left.CommitTimestamp == right.CommitTimestamp
               && string.Equals(left.FilesChangedJson, right.FilesChangedJson, StringComparison.Ordinal);
    }

    private static List<SessionLogTurnStringListEntity> MapStringListItems(UnifiedRequestEntryDto dto)
    {
        var items = new List<SessionLogTurnStringListEntity>();
        AddStringListItems(items, "DesignDecision", dto.DesignDecisions);
        AddStringListItems(items, "Requirement", dto.RequirementsDiscovered);
        AddStringListItems(items, "FileModified", dto.FilesModified);
        AddStringListItems(items, "Blocker", dto.Blockers);
        return items;
    }

    private static void AddStringListItems(ICollection<SessionLogTurnStringListEntity> items, string listType, List<string>? values)
    {
        if (values is not { Count: > 0 })
            return;
        for (int i = 0; i < values.Count; i++)
        {
            items.Add(new SessionLogTurnStringListEntity
            {
                ListType = listType,
                Ordinal = i,
                Value = values[i]
            });
        }
    }

    private static UnifiedSessionLogDto MapEntityToDto(SessionLogEntity entity)
    {
        var turns = entity.Turns.OrderBy(e => e.Id).ToList();
        var timestamps = turns
            .Select(static turn => turn.Timestamp)
            .Where(static timestamp => timestamp.HasValue)
            .Select(static timestamp => timestamp!.Value)
            .ToList();
        var started = entity.Started ?? (timestamps.Count > 0 ? timestamps.Min() : null);
        var lastUpdated = entity.LastUpdated ?? (timestamps.Count > 0 ? timestamps.Max() : null);

        return new UnifiedSessionLogDto
        {
            SourceType = entity.SourceType,
            SessionId = entity.SessionId,
            AgentDefinitionId = entity.AgentDefinitionId,
            Title = entity.Title,
            Model = entity.Model,
            Started = started?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            LastUpdated = lastUpdated?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            Status = entity.Status,
            TurnCount = turns.Count,
            TotalTokens = entity.TotalTokens,
            CursorSessionLabel = entity.CursorSessionLabel,
            CopilotStatistics = entity.CopilotAvgSuccessScore.HasValue || entity.CopilotTotalNetTokens.HasValue
                || entity.CopilotTotalNetPremiumRequests.HasValue || entity.CopilotCompletedCount.HasValue || entity.CopilotInProgressCount.HasValue
                ? new CopilotStatisticsDto
                {
                    AverageSuccessScore = entity.CopilotAvgSuccessScore,
                    TotalNetTokens = entity.CopilotTotalNetTokens,
                    TotalNetPremiumRequests = entity.CopilotTotalNetPremiumRequests,
                    CompletedCount = entity.CopilotCompletedCount,
                    InProgressCount = entity.CopilotInProgressCount
                }
                : null,
            Workspace = entity.Project != null || entity.Repository != null
                ? new WorkspaceInfoDto
                {
                    Project = entity.Project,
                    TargetFramework = entity.TargetFramework,
                    Repository = entity.Repository,
                    Branch = entity.Branch
                }
                : null,
            Turns = turns.Select(MapTurnEntityToDto).ToList()
        };
    }

    private static UnifiedRequestEntryDto MapTurnEntityToDto(SessionLogTurnEntity e) => new()
    {
        RequestId = e.RequestId,
        Timestamp = e.Timestamp?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
        Model = e.Model,
        ModelProvider = e.ModelProvider,
        QueryText = e.QueryText,
        QueryTitle = e.QueryTitle,
        Response = e.Response,
        Interpretation = e.Interpretation,
        Status = e.Status,
        TokenCount = e.TokenCount,
        FailureNote = e.FailureNote,
        Score = e.Score,
        IsPremium = e.IsPremium,
        RawContext = DeserializeJson(e.RawContextJson),
        OriginalEntry = DeserializeJson(e.OriginalEntryJson),
        Tags = e.Tags.Count > 0 ? e.Tags.Select(t => t.Tag).ToList() : null,
        ContextList = e.ContextItems.Count > 0
            ? e.ContextItems.OrderBy(c => c.Ordinal).Select(c => c.ContextItem).ToList()
            : null,
        Actions = e.Actions.Count > 0
            ? e.Actions.OrderBy(a => a.Order).Select(a => new UnifiedActionDto
            {
                Order = a.Order,
                Description = a.Description,
                Type = a.Type,
                Status = a.Status,
                FilePath = a.FilePath
            }).ToList()
            : null,
        ProcessingDialog = e.ProcessingDialog.Count > 0
            ? e.ProcessingDialog.OrderBy(p => p.Ordinal).Select(p => new ProcessingDialogItemDto
            {
                Timestamp = p.Timestamp.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                Role = p.Role,
                Content = p.Content,
                Category = p.Category
            }).ToList()
            : null,
        Commits = e.Commits.Count > 0
            ? e.Commits.OrderBy(c => c.Ordinal).Select(c => new SessionLogCommitDto
            {
                Sha = c.Sha,
                Branch = c.Branch,
                Message = c.Message,
                Author = c.Author,
                Timestamp = c.CommitTimestamp?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                FilesChanged = DeserializeStringList(c.FilesChangedJson)
            }).ToList()
            : null,
        DesignDecisions = MapStringListToDto(e.StringListItems, "DesignDecision"),
        RequirementsDiscovered = MapStringListToDto(e.StringListItems, "Requirement"),
        FilesModified = MapStringListToDto(e.StringListItems, "FileModified"),
        Blockers = MapStringListToDto(e.StringListItems, "Blocker")
    };

    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result.ToUniversalTime()
            : null;
    }

    private static string? SerializeJson(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value);
    }

    private static object? DeserializeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        return JsonSerializer.Deserialize<object>(json);
    }

    private static List<string>? MapStringListToDto(ICollection<SessionLogTurnStringListEntity> items, string listType)
    {
        var filtered = items.Where(i => i.ListType == listType).OrderBy(i => i.Ordinal).Select(i => i.Value).ToList();
        return filtered.Count > 0 ? filtered : null;
    }

    private static List<string>? DeserializeStringList(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task PublishChangeSafeAsync(string action, string entityId, string resourceUri, CancellationToken cancellationToken)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.SessionLog,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = resourceUri,
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing session log change event for {EntityId}", entityId);
        }
    }
}
