using System.Globalization;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
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
    private readonly ILogger<SessionLogService> _logger;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public SessionLogService(McpDbContext db, ILogger<SessionLogService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<long> SubmitAsync(UnifiedSessionLogDto dto, string? sourceFilePath = null, string? contentHash = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.SourceType))
            throw new ArgumentException("SourceType is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.SessionId))
            throw new ArgumentException("SessionId is required.", nameof(dto));

        var existing = await FindExistingSessionAsync(dto.SourceType, dto.SessionId, cancellationToken).ConfigureAwait(false);

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

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Race condition: another request inserted the same (SourceType, SessionId) between
            // our query and save. Detach the failed entity, re-query, and update instead.
            _logger.LogWarning("UNIQUE constraint race for {SourceType}/{SessionId}, retrying as update", dto.SourceType, dto.SessionId);
            _db.ChangeTracker.Clear();

            existing = await FindExistingSessionAsync(dto.SourceType, dto.SessionId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Session log {dto.SourceType}/{dto.SessionId} disappeared after UNIQUE constraint failure.");

            MapDtoToEntity(dto, existing);
            existing.SourceFilePath = sourceFilePath;
            existing.ContentHash = contentHash;
            UpsertEntries(existing, dto.Entries);

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated session log {SourceType}/{SessionId} (Id={Id}) after retry", dto.SourceType, dto.SessionId, existing.Id);
        }

        return existing.Id;
    }

    private Task<SessionLogEntity?> FindExistingSessionAsync(string sourceType, string sessionId, CancellationToken cancellationToken) =>
        _db.SessionLogs
            .Include(s => s.Entries)
                .ThenInclude(e => e.Actions)
            .Include(s => s.Entries)
                .ThenInclude(e => e.Tags)
            .Include(s => s.Entries)
                .ThenInclude(e => e.ContextItems)
            .Include(s => s.Entries)
                .ThenInclude(e => e.ProcessingDialog)
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

        var entry = await _db.SessionLogEntries
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

        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            var modelFilter = request.Model;
            query = query.Where(s => s.Model != null && EF.Functions.Like(s.Model, "%" + modelFilter + "%"));
        }

        // SQLite cannot translate DateTimeOffset comparisons or ORDER BY in LINQ.
        // Load candidate sessions with server-side string filters, then apply
        // DateTimeOffset filtering, ordering, and paging on the client side.
        // Session logs are a low-volume entity so this is acceptable.
        var allSessions = await query
            .Include(s => s.Entries.OrderBy(e => e.Id))
                .ThenInclude(e => e.Actions.OrderBy(a => a.Order))
            .Include(s => s.Entries)
                .ThenInclude(e => e.Tags)
            .Include(s => s.Entries)
                .ThenInclude(e => e.ContextItems.OrderBy(c => c.Ordinal))
            .Include(s => s.Entries)
                .ThenInclude(e => e.ProcessingDialog.OrderBy(p => p.Ordinal))
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Client-side DateTimeOffset filtering
        IEnumerable<SessionLogEntity> filtered = allSessions;

        if (request.From.HasValue)
            filtered = filtered.Where(s => s.Started.HasValue && s.Started.Value >= request.From.Value);

        if (request.To.HasValue)
            filtered = filtered.Where(s => s.LastUpdated.HasValue && s.LastUpdated.Value <= request.To.Value);

        // Client-side text search
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

    private static void MapDtoToEntity(UnifiedSessionLogDto dto, SessionLogEntity entity)
    {
        entity.Title = dto.Title;
        entity.Model = dto.Model;
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

    /// <summary>
    /// Upserts entries on an existing session: entries are keyed by RequestId.
    /// Existing entries with a matching RequestId are updated in place.
    /// New entries (RequestId not yet present) are added.
    /// Stale entries (present in DB but absent from the DTO) are removed.
    /// </summary>
    private void UpsertEntries(SessionLogEntity session, List<UnifiedRequestEntryDto>? dtoEntries)
    {
        var incoming = dtoEntries ?? [];

        // Deduplicate incoming entries by RequestId — keep last occurrence
        var deduped = new List<UnifiedRequestEntryDto>();
        var seenRequestIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = incoming.Count - 1; i >= 0; i--)
        {
            var dto = incoming[i];
            if (dto.RequestId == null || seenRequestIds.Add(dto.RequestId))
                deduped.Add(dto);
        }
        deduped.Reverse();

        // Build a lookup of existing entries by RequestId for O(1) matching
        var existingByRequestId = session.Entries
            .Where(e => e.RequestId != null)
            .ToDictionary(e => e.RequestId!, StringComparer.Ordinal);

        // Track which existing entries are still present in the DTO
        var matchedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var dto in deduped)
        {
            if (dto.RequestId != null && existingByRequestId.TryGetValue(dto.RequestId, out var existingEntry))
            {
                // Update existing entry in place
                UpdateEntryFromDto(existingEntry, dto);
                matchedIds.Add(dto.RequestId);
            }
            else
            {
                // New entry — insert
                var newEntry = MapSingleEntry(dto);
                session.Entries.Add(newEntry);
            }
        }

        // Remove stale entries no longer in the DTO (cascade deletes actions/tags/context)
        var stale = session.Entries
            .Where(e => e.RequestId != null && !matchedIds.Contains(e.RequestId)
                        && existingByRequestId.ContainsKey(e.RequestId))
            .ToList();
        if (stale.Count > 0)
        {
            _db.SessionLogEntries.RemoveRange(stale);
        }
    }

    /// <summary>
    /// Updates an existing entry entity from a DTO, replacing its child collections.
    /// </summary>
    private void UpdateEntryFromDto(SessionLogEntryEntity entity, UnifiedRequestEntryDto dto)
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

        // Replace child collections (cascade delete handles old rows)
        _db.SessionLogActions.RemoveRange(entity.Actions);
        _db.SessionLogEntryTags.RemoveRange(entity.Tags);
        _db.SessionLogEntryContexts.RemoveRange(entity.ContextItems);
        _db.SessionLogProcessingDialogs.RemoveRange(entity.ProcessingDialog);

        entity.Actions = MapActions(dto.Actions);
        entity.Tags = MapTags(dto.Tags);
        entity.ContextItems = MapContextItems(dto.ContextList);
        entity.ProcessingDialog = MapProcessingDialog(dto.ProcessingDialog);
    }

    private static List<SessionLogEntryEntity> MapNewEntries(List<UnifiedRequestEntryDto>? entries)
    {
        if (entries is null or { Count: 0 })
            return [];

        return entries.Select(MapSingleEntry).ToList();
    }

    private static SessionLogEntryEntity MapSingleEntry(UnifiedRequestEntryDto e)
    {
        return new SessionLogEntryEntity
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
            ProcessingDialog = MapProcessingDialog(e.ProcessingDialog)
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

    private static List<SessionLogEntryTagEntity> MapTags(List<string>? tags)
    {
        return tags?.Select(t => new SessionLogEntryTagEntity { Tag = t }).ToList() ?? [];
    }

    private static List<SessionLogEntryContextEntity> MapContextItems(List<string>? contextList)
    {
        return contextList?.Select((c, i) => new SessionLogEntryContextEntity
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

    private static UnifiedSessionLogDto MapEntityToDto(SessionLogEntity entity)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = entity.SourceType,
            SessionId = entity.SessionId,
            Title = entity.Title,
            Model = entity.Model,
            Started = entity.Started?.ToString("o", CultureInfo.InvariantCulture),
            LastUpdated = entity.LastUpdated?.ToString("o", CultureInfo.InvariantCulture),
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
                Timestamp = e.Timestamp?.ToString("o", CultureInfo.InvariantCulture),
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
                        Timestamp = p.Timestamp.ToString("o", CultureInfo.InvariantCulture),
                        Role = p.Role,
                        Content = p.Content,
                        Category = p.Category
                    }).ToList()
                    : null
            }).ToList()
        };
    }

    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result
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
}
