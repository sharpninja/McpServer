using System.Globalization;
using System.Text.Json;
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

    private readonly McpDbContext _db;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<SessionLogService> _logger;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public SessionLogService(McpDbContext db, ILogger<SessionLogService> logger, IChangeEventBus? eventBus = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public async Task<long> SubmitAsync(UnifiedSessionLogDto dto, string? sourceFilePath = null, string? contentHash = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.SourceType))
            throw new ArgumentException("SourceType is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new ArgumentException("SessionId is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(dto.SessionId, dto.SourceType);
            if (sessionIdError is not null)
                throw new ArgumentException(sessionIdError, nameof(dto));
            if (dto.Entries is { Count: > 0 })
            {
                foreach (var entry in dto.Entries)
                {
                    var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(entry.RequestId);
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
            UpsertEntries(existing, dto.Entries);
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
            existing.Entries = MapNewEntries(dto.Entries);
            _db.SessionLogs.Add(existing);
            _logger.LogInformation("Created session log {SourceType}/{SessionId}", dto.SourceType, dto.SessionId);
        }

        await ResolveAgentDefinitionLinkAsync(dto, existing, cancellationToken).ConfigureAwait(false);

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
            UpsertEntries(existing, dto.Entries);
            await ResolveAgentDefinitionLinkAsync(dto, existing, cancellationToken).ConfigureAwait(false);

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
            .IgnoreQueryFilters()
            .Include(s => s.Entries)
                .ThenInclude(e => e.Actions)
            .Include(s => s.Entries)
                .ThenInclude(e => e.Tags)
            .Include(s => s.Entries)
                .ThenInclude(e => e.ContextItems)
            .Include(s => s.Entries)
                .ThenInclude(e => e.ProcessingDialog)
            .Include(s => s.Entries)
                .ThenInclude(e => e.Commits)
            .Include(s => s.Entries)
                .ThenInclude(e => e.StringListItems)
            .FirstOrDefaultAsync(s => s.SourceType == sourceType && s.SessionId == sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsUnchangedAsync(string sourceType, string sessionId, string contentHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(sessionId);
        ArgumentNullException.ThrowIfNull(contentHash);

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
        var sessionIdError = SessionLogIdentifierValidator.ValidateSessionId(sessionId, sourceType);
        if (sessionIdError is not null)
            throw new ArgumentException(sessionIdError, nameof(sessionId));
        var requestIdError = SessionLogIdentifierValidator.ValidateRequestId(requestId);
        if (requestIdError is not null)
            throw new ArgumentException(requestIdError, nameof(requestId));

        var entry = await _db.SessionLogTurns
            .Include(e => e.ProcessingDialog)
            .FirstOrDefaultAsync(e =>
                e.SessionLog!.SourceType == sourceType
                && e.SessionLog.SessionId == sessionId
                && e.RequestId == requestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Entry not found: {sourceType}/{sessionId}/{requestId}");

        var nextOrdinal = entry.ProcessingDialog.Count > 0
            ? entry.ProcessingDialog.Max(p => p.Ordinal) + 1
            : 0;

        foreach (var item in items)
        {
            entry.ProcessingDialog.Add(new SessionLogProcessingDialogEntity
            {
                Ordinal = nextOrdinal++,
                Timestamp = ParseDateTimeOffset(item.Timestamp) ?? DateTimeOffset.UtcNow,
                Role = item.Role ?? "model",
                Content = item.Content ?? string.Empty,
                Category = item.Category
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
            .Include(s => s.Entries.OrderBy(e => e.Id))
                .ThenInclude(e => e.Actions.OrderBy(a => a.Order))
            .Include(s => s.Entries)
                .ThenInclude(e => e.Tags)
            .Include(s => s.Entries)
                .ThenInclude(e => e.ContextItems.OrderBy(c => c.Ordinal))
            .Include(s => s.Entries)
                .ThenInclude(e => e.ProcessingDialog.OrderBy(p => p.Ordinal))
            .Include(s => s.Entries)
                .ThenInclude(e => e.Commits.OrderBy(c => c.Ordinal))
            .Include(s => s.Entries)
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
            var text = request.Text;
            filtered = filtered.Where(s => s.Entries.Any(e =>
                (e.QueryText?.Contains(text, StringComparison.OrdinalIgnoreCase) == true) ||
                (e.QueryTitle?.Contains(text, StringComparison.OrdinalIgnoreCase) == true) ||
                (e.Response?.Contains(text, StringComparison.OrdinalIgnoreCase) == true) ||
                (e.Interpretation?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)));
        }

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        var sessions = filteredList
            .OrderByDescending(s => s.Started ?? DateTimeOffset.MinValue)
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
        entity.Title = dto.Title;
        entity.Model = dto.Model;
        entity.AgentDefinitionId = dto.AgentDefinitionId;
        entity.Started = ParseDateTimeOffset(dto.Started);
        entity.LastUpdated = ParseDateTimeOffset(dto.LastUpdated);
        entity.Status = dto.Status;
        entity.EntryCount = dto.EntryCount;
        entity.TotalTokens = dto.TotalTokens;
        entity.CursorSessionLabel = dto.CursorSessionLabel;

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

    private void UpsertEntries(SessionLogEntity session, List<UnifiedRequestEntryDto>? dtoEntries)
    {
        var incoming = dtoEntries ?? [];
        var deduped = new List<UnifiedRequestEntryDto>();
        var seenRequestIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = incoming.Count - 1; i >= 0; i--)
        {
            var dto = incoming[i];
            if (dto.RequestId == null || seenRequestIds.Add(dto.RequestId))
                deduped.Add(dto);
        }
        deduped.Reverse();

        var existingByRequestId = session.Entries
            .Where(e => e.RequestId != null)
            .ToDictionary(e => e.RequestId!, StringComparer.Ordinal);

        var matchedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var dto in deduped)
        {
            if (dto.RequestId != null && existingByRequestId.TryGetValue(dto.RequestId, out var existingEntry))
            {
                UpdateEntryFromDto(existingEntry, dto);
                matchedIds.Add(dto.RequestId);
            }
            else
            {
                var newEntry = MapSingleEntry(dto);
                session.Entries.Add(newEntry);
            }
        }

        var stale = session.Entries
            .Where(e => e.RequestId != null && !matchedIds.Contains(e.RequestId)
                        && existingByRequestId.ContainsKey(e.RequestId))
            .ToList();
        if (stale.Count > 0)
        {
            _db.SessionLogTurns.RemoveRange(stale);
        }
    }

    private void UpdateEntryFromDto(SessionLogTurnEntity entity, UnifiedRequestEntryDto dto)
    {
        entity.Timestamp = ParseDateTimeOffset(dto.Timestamp);
        entity.Model = dto.Model;
        entity.ModelProvider = dto.ModelProvider;
        entity.QueryText = dto.QueryText;
        entity.QueryTitle = dto.QueryTitle;
        entity.Response = dto.Response;
        entity.Interpretation = dto.Interpretation;
        entity.Status = dto.Status;
        entity.TokenCount = dto.TokenCount;
        entity.FailureNote = dto.FailureNote;
        entity.Score = dto.Score;
        entity.IsPremium = dto.IsPremium;
        entity.RawContextJson = SerializeJson(dto.RawContext);
        entity.OriginalEntryJson = SerializeJson(dto.OriginalEntry);

        _db.SessionLogActions.RemoveRange(entity.Actions);
        _db.SessionLogTurnTags.RemoveRange(entity.Tags);
        _db.SessionLogTurnContexts.RemoveRange(entity.ContextItems);
        _db.SessionLogProcessingDialogs.RemoveRange(entity.ProcessingDialog);
        _db.SessionLogCommits.RemoveRange(entity.Commits);
        _db.SessionLogTurnStringLists.RemoveRange(entity.StringListItems);

        entity.Actions = MapActions(dto.Actions);
        entity.Tags = MapTags(dto.Tags);
        entity.ContextItems = MapContextItems(dto.ContextList);
        entity.ProcessingDialog = MapProcessingDialog(dto.ProcessingDialog);
        entity.Commits = MapCommits(dto.Commits);
        entity.StringListItems = MapStringListItems(dto);
    }

    private static List<SessionLogTurnEntity> MapNewEntries(List<UnifiedRequestEntryDto>? entries)
    {
        if (entries is null or { Count: 0 })
            return [];

        return entries.Select(MapSingleEntry).ToList();
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

    private static List<SessionLogTurnStringListEntity> MapStringListItems(UnifiedRequestEntryDto dto)
    {
        var items = new List<SessionLogTurnStringListEntity>();
        AddStringListItems(items, "DesignDecision", dto.DesignDecisions);
        AddStringListItems(items, "Requirement", dto.RequirementsDiscovered);
        AddStringListItems(items, "FileModified", dto.FilesModified);
        AddStringListItems(items, "Blocker", dto.Blockers);
        return items;
    }

    private static void AddStringListItems(List<SessionLogTurnStringListEntity> items, string listType, List<string>? values)
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
        return new UnifiedSessionLogDto
        {
            SourceType = entity.SourceType,
            SessionId = entity.SessionId,
            AgentDefinitionId = entity.AgentDefinitionId,
            Title = entity.Title,
            Model = entity.Model,
            Started = entity.Started?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            LastUpdated = entity.LastUpdated?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            Status = entity.Status,
            EntryCount = entity.EntryCount,
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
            Entries = entity.Entries.Select(e => new UnifiedRequestEntryDto
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
            }).ToList()
        };
    }

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
