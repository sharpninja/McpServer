using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// EF-backed implementation of <see cref="IMemoryService"/>.
/// </summary>
public sealed partial class MemoryService : IMemoryService
{
    private static readonly string[] AllQueryFilters = ["Workspace", "SoftDelete"];

    private readonly McpDbContext _db;
    private readonly ILogger<MemoryService> _logger;

    /// <summary>Initializes a new instance of the <see cref="MemoryService"/> class.</summary>
    public MemoryService(McpDbContext db, ILogger<MemoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MemoryMutationResult> AddAsync(MemoryAddRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = NormalizeCategory(request.Category);
        if (category is null)
            return Validation("Memory category is required.");

        if (string.IsNullOrWhiteSpace(request.Text) && string.IsNullOrWhiteSpace(request.Content))
            return Validation("Memory text is required.");

        if (!Enum.IsDefined(request.Scope))
            return Validation("Memory scope must be Global or Workspace.");

        var content = string.IsNullOrWhiteSpace(request.Content) ? request.Text : request.Content;
        if (MemoryPolicy.RejectsSecrets(content) || MemoryPolicy.RejectsSecrets(request.Text))
            return Validation("Memory content matches a rejected secret pattern.");

        if (request.Title is { Length: > MemoryLimits.MaxTitleLength })
            return Validation("Title exceeds the configured maximum length.");
        if (content.Length > MemoryLimits.MaxContentLength)
            return Validation("Content exceeds the configured maximum length.");
        if (request.Tags is not null && request.Tags.Any(tag => tag is { Length: > MemoryLimits.MaxTagLength }))
            return Validation("Tag exceeds the configured maximum length.");
        if (request.Confidence is < 0 or > 1)
            return Validation("Confidence must be in [0,1].");
        if (!string.IsNullOrWhiteSpace(request.Type)
            && !MemoryLimits.AllowedTypes.Contains(request.Type.Trim()))
        {
            return Validation("Type is not an allowed memory type.");
        }

        var workspaceId = ResolveWorkspaceId(request.Scope);
        if (request.Scope == MemoryScope.Workspace && workspaceId is null)
            return Validation("Workspace memory requires an active workspace.");

        var id = string.IsNullOrWhiteSpace(request.Id)
            ? await GenerateNextIdAsync(category, cancellationToken).ConfigureAwait(false)
            : NormalizeId(request.Id);
        if (!IsValidMemoryId(id))
            return Validation("Memory id must match MEMORY-{CATEGORY}-{NNN}.");

        var duplicate = await _db.Memories
            .IgnoreQueryFilters()
            .AnyAsync(memory => memory.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (duplicate)
            return new MemoryMutationResult(false, $"Memory '{id}' already exists.", FailureKind: MemoryMutationFailureKind.Conflict);

        var now = DateTimeOffset.UtcNow;
        var entity = new MemoryEntity
        {
            Id = id,
            Category = category,
            Scope = ToEntityScope(request.Scope),
            WorkspaceId = workspaceId,
            Text = string.IsNullOrWhiteSpace(request.Text) ? content : request.Text,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            UpdatedBy = NormalizeOptional(request.UpdatedBy),
            Title = NormalizeOptional(request.Title),
            Summary = NormalizeOptional(request.Summary),
            Content = content,
            Type = NormalizeOptional(request.Type),
            TagsJson = MemoryLayerMapper.SerializeTags(request.Tags),
            Confidence = request.Confidence ?? MemoryLimits.DefaultConfidence,
            SourceKind = NormalizeOptional(request.SourceKind),
            SourceRef = NormalizeOptional(request.SourceRef),
            CreatedBy = NormalizeOptional(request.CreatedBy),
            EmbeddingStatus = "pending",
        };

        _db.Memories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await new MemoryCompetitiveOperations(_db).AppendVersionAsync(entity, cancellationToken).ConfigureAwait(false);
        await MemorySessionLogHook.TryAppendAsync(_db, entity.Id, "memory_add", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Memory created: {MemoryId} ({Scope})", entity.Id, entity.Scope);
        return new MemoryMutationResult(true, Memory: MemoryLayerMapper.ToItem(entity));
    }

    /// <inheritdoc />
    public async Task<MemoryQueryResult> ListAsync(MemoryListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        IQueryable<MemoryEntity> query = _db.Memories.AsNoTracking();

        if (request.Scope is not null)
        {
            var scope = ToEntityScope(request.Scope.Value);
            query = query.Where(memory => memory.Scope == scope);
        }

        var category = NormalizeCategory(request.Category);
        if (category is not null)
            query = query.Where(memory => memory.Category == category);

        var keyword = NormalizeOptional(request.Keyword);
        var rows = await query
            .OrderBy(memory => memory.Scope == MemoryEntity.GlobalScope ? 0 : 1)
            .ThenBy(memory => memory.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (keyword is not null)
        {
            var lowered = keyword.ToLowerInvariant();
            rows = rows.Where(memory =>
                    memory.Id.ToLowerInvariant().Contains(lowered, StringComparison.Ordinal)
                    || memory.Category.ToLowerInvariant().Contains(lowered, StringComparison.Ordinal)
                    || memory.Text.ToLowerInvariant().Contains(lowered, StringComparison.Ordinal)
                    || (memory.Title ?? string.Empty).ToLowerInvariant().Contains(lowered, StringComparison.Ordinal)
                    || MemoryLayerMapper.EffectiveContent(memory).ToLowerInvariant().Contains(lowered, StringComparison.Ordinal))
                .ToList();
        }

        var items = rows.Select(MemoryLayerMapper.ToItem).ToList();
        return new MemoryQueryResult(items, items.Count);
    }

    /// <inheritdoc />
    public async Task<MemoryItem?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var normalizedId = NormalizeId(id);
        if (!IsValidMemoryId(normalizedId))
            return null;

        var entity = await _db.Memories
            .AsNoTracking()
            .FirstOrDefaultAsync(memory => memory.Id == normalizedId, cancellationToken)
            .ConfigureAwait(false);
        return entity is null ? null : MemoryLayerMapper.ToItem(entity);
    }

    /// <inheritdoc />
    public async Task<MemoryMutationResult> UpdateAsync(string id, MemoryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedId = NormalizeId(id);
        if (!IsValidMemoryId(normalizedId))
            return Validation("Memory id must match MEMORY-{CATEGORY}-{NNN}.");

        var entity = await _db.Memories
            .FirstOrDefaultAsync(memory => memory.Id == normalizedId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            return new MemoryMutationResult(false, $"Memory '{normalizedId}' not found.", FailureKind: MemoryMutationFailureKind.NotFound);

        if (request.Category is not null)
        {
            var category = NormalizeCategory(request.Category);
            if (category is null)
                return Validation("Memory category cannot be empty.");
            entity.Category = category;
        }

        if (request.Scope is not null)
        {
            if (!Enum.IsDefined(request.Scope.Value))
                return Validation("Memory scope must be Global or Workspace.");

            var workspaceId = ResolveWorkspaceId(request.Scope.Value);
            if (request.Scope.Value == MemoryScope.Workspace && workspaceId is null)
                return Validation("Workspace memory requires an active workspace.");

            entity.Scope = ToEntityScope(request.Scope.Value);
            entity.WorkspaceId = workspaceId;
        }

        var contentChanged = false;
        if (request.Text is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Validation("Memory text cannot be empty.");
            if (MemoryPolicy.RejectsSecrets(request.Text))
                return Validation("Memory content matches a rejected secret pattern.");
            entity.Text = request.Text;
            entity.Content = request.Text;
            contentChanged = true;
        }

        if (request.Content is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return Validation("Memory content cannot be empty.");
            if (MemoryPolicy.RejectsSecrets(request.Content))
                return Validation("Memory content matches a rejected secret pattern.");
            entity.Content = request.Content;
            entity.Text = request.Content;
            contentChanged = true;
        }

        if (request.Title is not null)
        {
            if (request.Title.Length > MemoryLimits.MaxTitleLength)
                return Validation("Title exceeds the configured maximum length.");
            entity.Title = NormalizeOptional(request.Title);
        }

        if (request.Summary is not null)
            entity.Summary = NormalizeOptional(request.Summary);

        if (request.Type is not null)
        {
            if (!MemoryLimits.AllowedTypes.Contains(request.Type.Trim()))
                return Validation("Type is not an allowed memory type.");
            entity.Type = NormalizeOptional(request.Type);
        }

        if (request.Tags is not null)
        {
            if (request.Tags.Any(tag => tag is { Length: > MemoryLimits.MaxTagLength }))
                return Validation("Tag exceeds the configured maximum length.");
            entity.TagsJson = MemoryLayerMapper.SerializeTags(request.Tags);
        }

        if (request.Confidence is not null)
        {
            if (request.Confidence is < 0 or > 1)
                return Validation("Confidence must be in [0,1].");
            entity.Confidence = request.Confidence;
        }

        if (request.SourceKind is not null)
            entity.SourceKind = NormalizeOptional(request.SourceKind);
        if (request.SourceRef is not null)
            entity.SourceRef = NormalizeOptional(request.SourceRef);

        entity.Version++;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (request.UpdatedBy is not null)
            entity.UpdatedBy = NormalizeOptional(request.UpdatedBy);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (contentChanged)
            await new MemoryCompetitiveOperations(_db).AppendVersionAsync(entity, cancellationToken).ConfigureAwait(false);
        await MemorySessionLogHook.TryAppendAsync(_db, entity.Id, "memory_update", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Memory updated: {MemoryId}", entity.Id);
        return new MemoryMutationResult(true, Memory: MemoryLayerMapper.ToItem(entity));
    }

    /// <inheritdoc />
    public async Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        var normalizedId = NormalizeId(id);
        if (!IsValidMemoryId(normalizedId))
            return Validation("Memory id must match MEMORY-{CATEGORY}-{NNN}.");

        var entity = await _db.Memories
            .FirstOrDefaultAsync(memory => memory.Id == normalizedId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            return new MemoryMutationResult(false, $"Memory '{normalizedId}' not found.", FailureKind: MemoryMutationFailureKind.NotFound);

        var item = MemoryLayerMapper.ToItem(entity);
        _db.Memories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await MemorySessionLogHook.TryAppendAsync(_db, entity.Id, "memory_remove", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Memory removed: {MemoryId}", entity.Id);
        return new MemoryMutationResult(true, Memory: item);
    }

    private async Task<string> GenerateNextIdAsync(string category, CancellationToken cancellationToken)
    {
        var prefix = $"MEMORY-{category}-";
        var ids = await _db.Memories
            .IgnoreQueryFilters(AllQueryFilters)
            .Where(memory => memory.Id.StartsWith(prefix))
            .Select(memory => memory.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var regex = MemoryIdSuffixRegex(prefix);
        var next = ids
            .Select(id => regex.Match(id))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{next:000}";
    }

    private static Regex MemoryIdSuffixRegex(string prefix)
    {
        return new Regex("^" + Regex.Escape(prefix) + "([0-9]+)$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    }

    private string? ResolveWorkspaceId(MemoryScope scope)
    {
        return scope == MemoryScope.Global
            ? null
            : NormalizeOptional(_db.CurrentWorkspaceId);
    }

    private static MemoryMutationResult Validation(string error)
    {
        return new MemoryMutationResult(false, error, FailureKind: MemoryMutationFailureKind.Validation);
    }

    private static string NormalizeId(string? id)
    {
        return (id ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool IsValidMemoryId(string id)
    {
        return MemoryIdRegex().IsMatch(id);
    }

    private static string? NormalizeCategory(string? category)
    {
        var trimmed = NormalizeOptional(category);
        if (trimmed is null)
            return null;

        var normalized = CategoryUnsafeCharactersRegex().Replace(trimmed.ToUpperInvariant(), "-").Trim('-');
        normalized = RepeatedHyphenRegex().Replace(normalized, "-");
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string ToEntityScope(MemoryScope scope)
    {
        return scope == MemoryScope.Global ? MemoryEntity.GlobalScope : MemoryEntity.WorkspaceScope;
    }

    private static MemoryItem ToItem(MemoryEntity entity) => MemoryLayerMapper.ToItem(entity);

    [GeneratedRegex("[^A-Z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex CategoryUnsafeCharactersRegex();

    [GeneratedRegex("-+", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedHyphenRegex();

    [GeneratedRegex("^MEMORY-[A-Z0-9]+(?:-[A-Z0-9]+)*-[0-9]{3,}$", RegexOptions.CultureInvariant)]
    private static partial Regex MemoryIdRegex();
}
