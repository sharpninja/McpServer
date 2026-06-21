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

        if (string.IsNullOrWhiteSpace(request.Text))
            return Validation("Memory text is required.");

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
            Text = request.Text,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            UpdatedBy = NormalizeOptional(request.UpdatedBy),
        };

        _db.Memories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Memory created: {MemoryId} ({Scope})", entity.Id, entity.Scope);
        return new MemoryMutationResult(true, Memory: ToItem(entity));
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
        if (keyword is not null)
        {
            query = query.Where(memory =>
                memory.Id.Contains(keyword)
                || memory.Category.Contains(keyword)
                || memory.Text.Contains(keyword));
        }

        var rows = await query
            .OrderBy(memory => memory.Scope == MemoryEntity.GlobalScope ? 0 : 1)
            .ThenBy(memory => memory.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(ToItem).ToList();
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
        return entity is null ? null : ToItem(entity);
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
            var workspaceId = ResolveWorkspaceId(request.Scope.Value);
            if (request.Scope.Value == MemoryScope.Workspace && workspaceId is null)
                return Validation("Workspace memory requires an active workspace.");

            entity.Scope = ToEntityScope(request.Scope.Value);
            entity.WorkspaceId = workspaceId;
        }

        if (request.Text is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Validation("Memory text cannot be empty.");
            entity.Text = request.Text;
        }

        entity.Version++;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (request.UpdatedBy is not null)
            entity.UpdatedBy = NormalizeOptional(request.UpdatedBy);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Memory updated: {MemoryId}", entity.Id);
        return new MemoryMutationResult(true, Memory: ToItem(entity));
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

        var item = ToItem(entity);
        _db.Memories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private static MemoryScope ToScope(string scope)
    {
        return string.Equals(scope, MemoryEntity.GlobalScope, StringComparison.Ordinal)
            ? MemoryScope.Global
            : MemoryScope.Workspace;
    }

    private static MemoryItem ToItem(MemoryEntity entity)
    {
        return new MemoryItem
        {
            Id = entity.Id,
            Category = entity.Category,
            Scope = ToScope(entity.Scope),
            WorkspacePath = entity.WorkspaceId,
            Text = entity.Text,
            Version = entity.Version,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            UpdatedBy = entity.UpdatedBy,
        };
    }

    [GeneratedRegex("[^A-Z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex CategoryUnsafeCharactersRegex();

    [GeneratedRegex("-+", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedHyphenRegex();

    [GeneratedRegex("^MEMORY-[A-Z0-9]+(?:-[A-Z0-9]+)*-[0-9]{3,}$", RegexOptions.CultureInvariant)]
    private static partial Regex MemoryIdRegex();
}
