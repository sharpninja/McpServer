using System.Text.Json;
using McpServer.Cqrs.Search;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-005 / TR-MCP-TODO-006 (provider-agnostic): EF Core-backed TODO
/// service. Persistence flows through <see cref="McpDbContext"/> and therefore
/// through whichever provider <c>Mcp:Database:Provider</c> selects via
/// <c>McpDatabaseProviderFactory</c> (TR-MCP-CFG-007).
/// </summary>
/// <remarks>
/// Functional parity with <c>SqliteTodoService</c> for CRUD + audit is the
/// acceptance criteria for phase 3. YAML-projection (TR-MCP-TODO-006) is
/// reduced to a no-op status reporter in this initial port; the full
/// projection refactor is tracked as a follow-up inside the same TR.
/// </remarks>
internal sealed class EfTodoService : ITodoService, ITodoStore, IDisposable
{
    private const string StandardItemKind = "standard";
    private const string CodeReviewPhaseItemKind = "code_review_phase";
    private const string CodeReviewSectionKey = "code-review-remediation";
    private const string ApiSource = "api";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<IngestionOptions> _ingestionOptions;
    private readonly IOptions<TodoStorageOptions> _storageOptions;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILogger<EfTodoService> _logger;
    private readonly IChangeEventBus? _eventBus;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Initializes a new instance of the <see cref="EfTodoService"/> class.
    /// </summary>
    public EfTodoService(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILogger<EfTodoService> logger,
        IChangeEventBus? eventBus = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _ingestionOptions = ingestionOptions ?? throw new ArgumentNullException(nameof(ingestionOptions));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public void Dispose() => _writeLock.Dispose();

    /// <inheritdoc />
    public async Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var scope = CreateScope();
        var ctx = scope.Context;
        var rows = await ctx.TodoItems.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var flat = rows.Select(ToFlatItem).ToList();
        var ordered = flat
            .OrderBy(i => i.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => PriorityRank(i.Priority))
            .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var filtered = ApplyFilters(ordered, request);
        return new TodoQueryResult(filtered, filtered.Count);
    }

    /// <inheritdoc />
    public async Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        await using var scope = CreateScope();
        var row = await scope.Context.TodoItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : ToFlatItem(row);
    }

    /// <inheritdoc />
    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idError = TodoValidator.ValidateTodoId(request.Id);
        if (idError is not null)
            return new TodoMutationResult(false, idError, FailureKind: TodoMutationFailureKind.Validation);

        var normalizedSection = NormalizeSection(request.Section);
        var normalizedPriority = NormalizePriority(normalizedSection, request.Priority);
        var priorityError = TodoValidator.ValidatePriority(normalizedPriority);
        if (priorityError is not null)
            return new TodoMutationResult(false, priorityError, FailureKind: TodoMutationFailureKind.Validation);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;

            if (await ctx.TodoItems.AnyAsync(i => i.Id == request.Id, cancellationToken).ConfigureAwait(false))
                return new TodoMutationResult(false, $"Item with id '{request.Id}' already exists.", FailureKind: TodoMutationFailureKind.Conflict);

            var all = await ctx.TodoItems.AsNoTracking().Select(ToFlatItemExpression).ToListAsync(cancellationToken).ConfigureAwait(false);
            var depIdError = TodoValidator.ValidateDependencyIds(request.DependsOn, all, "dependsOn");
            if (depIdError is not null)
                return new TodoMutationResult(false, depIdError, FailureKind: TodoMutationFailureKind.Validation);
            var depError = TodoValidator.ValidateDependencies(request.Id, request.DependsOn?.ToList() ?? [], all);
            if (depError is not null)
                return new TodoMutationResult(false, depError, FailureKind: TodoMutationFailureKind.Validation);

            var itemKind = DetermineItemKind(normalizedSection);
            var sectionOrder = await ResolveSectionOrderAsync(ctx, normalizedSection, cancellationToken).ConfigureAwait(false);
            var itemOrder = await GetNextItemOrderAsync(ctx, normalizedSection, normalizedPriority, itemKind, cancellationToken).ConfigureAwait(false);

            var entity = new TodoItemEntity
            {
                Id = request.Id,
                Title = request.Title,
                Section = normalizedSection,
                Priority = normalizedPriority,
                Done = false,
                Estimate = request.Estimate,
                Note = request.Note,
                DescriptionJson = SerializeList(request.Description),
                TechnicalDetailsJson = SerializeList(request.TechnicalDetails),
                ImplementationTasksJson = SerializeTasks(request.ImplementationTasks),
                Remaining = request.Remaining,
                DependsOnJson = SerializeList(request.DependsOn),
                FunctionalRequirementsJson = SerializeList(request.FunctionalRequirements),
                TechnicalRequirementsJson = SerializeList(request.TechnicalRequirements),
                ItemKind = itemKind,
                SectionOrder = sectionOrder,
                ItemOrder = itemOrder,
                PhaseLabel = itemKind == CodeReviewPhaseItemKind ? request.Phase ?? request.Title : null,
            };

            ctx.TodoItems.Add(entity);
            var flat = ToFlatItem(entity);
            await AppendAuditAsync(ctx, entity.Id, ChangeEventActions.Created, flat, null, ApiSource, cancellationToken).ConfigureAwait(false);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await PublishChangeSafeAsync(ChangeEventActions.Created, entity.Id, cancellationToken).ConfigureAwait(false);
            return new TodoMutationResult(true, Item: flat);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(request);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;

            var existing = await ctx.TodoItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.", FailureKind: TodoMutationFailureKind.NotFound);

            var previousFlat = ToFlatItem(existing);
            var updatedSection = NormalizeSection(request.Section ?? existing.Section);
            var updatedPriority = NormalizePriority(updatedSection, request.Priority ?? existing.Priority);
            var priorityError = TodoValidator.ValidatePriority(updatedPriority);
            if (priorityError is not null)
                return new TodoMutationResult(false, priorityError, FailureKind: TodoMutationFailureKind.Validation);

            var updatedKind = DetermineItemKind(updatedSection);
            var prevSection = existing.Section;
            var prevPriority = existing.Priority;
            var prevKind = existing.ItemKind;

            existing.Title = request.Title ?? existing.Title;
            existing.Section = updatedSection;
            existing.Priority = updatedPriority;
            existing.Done = request.Done ?? existing.Done;
            existing.Estimate = request.Estimate ?? existing.Estimate;
            existing.Note = request.Note ?? existing.Note;
            existing.DescriptionJson = request.Description is null ? existing.DescriptionJson : SerializeList(request.Description);
            existing.TechnicalDetailsJson = request.TechnicalDetails is null ? existing.TechnicalDetailsJson : SerializeList(request.TechnicalDetails);
            existing.ImplementationTasksJson = request.ImplementationTasks is null ? existing.ImplementationTasksJson : SerializeTasks(request.ImplementationTasks);
            existing.CompletedDate = request.CompletedDate ?? existing.CompletedDate;
            existing.DoneSummary = request.DoneSummary ?? existing.DoneSummary;
            existing.Remaining = request.Remaining ?? existing.Remaining;
            existing.Reference = request.Reference ?? existing.Reference;
            existing.DependsOnJson = request.DependsOn is null ? existing.DependsOnJson : SerializeList(request.DependsOn);
            existing.FunctionalRequirementsJson = request.FunctionalRequirements is null ? existing.FunctionalRequirementsJson : SerializeList(request.FunctionalRequirements);
            existing.TechnicalRequirementsJson = request.TechnicalRequirements is null ? existing.TechnicalRequirementsJson : SerializeList(request.TechnicalRequirements);
            existing.ItemKind = updatedKind;
            existing.PhaseLabel = updatedKind == CodeReviewPhaseItemKind
                ? request.Phase ?? existing.PhaseLabel ?? existing.Title
                : null;

            var all = await ctx.TodoItems.AsNoTracking().Select(ToFlatItemExpression).ToListAsync(cancellationToken).ConfigureAwait(false);
            var depIdError = TodoValidator.ValidateDependencyIds(DeserializeList(existing.DependsOnJson), all, "dependsOn");
            if (depIdError is not null)
                return new TodoMutationResult(false, depIdError, FailureKind: TodoMutationFailureKind.Validation);
            var depError = TodoValidator.ValidateDependencies(id, DeserializeList(existing.DependsOnJson)?.ToList() ?? [], all);
            if (depError is not null)
                return new TodoMutationResult(false, depError, FailureKind: TodoMutationFailureKind.Validation);

            existing.SectionOrder = await ResolveSectionOrderAsync(ctx, existing.Section, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(prevSection, existing.Section, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(prevPriority, existing.Priority, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(prevKind, existing.ItemKind, StringComparison.OrdinalIgnoreCase))
            {
                existing.ItemOrder = await GetNextItemOrderAsync(ctx, existing.Section, existing.Priority, existing.ItemKind, cancellationToken).ConfigureAwait(false);
            }

            var updatedFlat = ToFlatItem(existing);
            await AppendAuditAsync(ctx, existing.Id, ChangeEventActions.Updated, updatedFlat, previousFlat, ApiSource, cancellationToken).ConfigureAwait(false);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await PublishChangeSafeAsync(ChangeEventActions.Updated, existing.Id, cancellationToken).ConfigureAwait(false);
            return new TodoMutationResult(true, Item: updatedFlat);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;

            var existing = await ctx.TodoItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return new TodoMutationResult(false, $"Item with id '{id}' not found.", FailureKind: TodoMutationFailureKind.NotFound);

            var snapshot = ToFlatItem(existing);
            ctx.TodoItems.Remove(existing);
            await AppendAuditAsync(ctx, id, ChangeEventActions.Deleted, snapshot, snapshot, ApiSource, cancellationToken).ConfigureAwait(false);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await PublishChangeSafeAsync(ChangeEventActions.Deleted, id, cancellationToken).ConfigureAwait(false);
            return new TodoMutationResult(true);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var effectiveLimit = Math.Clamp(limit <= 0 ? 50 : limit, 1, 500);
        var effectiveOffset = Math.Max(offset, 0);

        await using var scope = CreateScope();
        var ctx = scope.Context;

        var totalCount = await ctx.TodoAuditHistory.CountAsync(h => h.TodoId == id, cancellationToken).ConfigureAwait(false);
        if (totalCount == 0)
        {
            var current = await ctx.TodoItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
            if (current is null)
                return new TodoAuditQueryResult([], 0);

            return new TodoAuditQueryResult(
            [
                new TodoAuditEntry
                {
                    AuditId = 0,
                    TodoId = current.Id,
                    Version = 1,
                    Action = "imported",
                    RecordedAtUtc = DateTime.UtcNow.ToString("O"),
                    Snapshot = ToFlatItem(current),
                    Source = "database-backfill",
                },
            ],
            1);
        }

        var rows = await ctx.TodoAuditHistory
            .AsNoTracking()
            .Where(h => h.TodoId == id)
            .OrderBy(h => h.Version)
            .Skip(effectiveOffset)
            .Take(effectiveLimit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = rows.Select(r => new TodoAuditEntry
        {
            AuditId = r.AuditId,
            TodoId = r.TodoId,
            Version = r.Version,
            Action = r.Action,
            RecordedAtUtc = r.RecordedAtUtc,
            Snapshot = DeserializeFlatItem(r.SnapshotJson),
            PreviousSnapshot = DeserializeFlatItem(r.PreviousSnapshotJson),
            Source = r.Source,
        }).ToList();
        return new TodoAuditQueryResult(entries, totalCount);
    }

    /// <inheritdoc />
    public async Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = CreateScope();
        var ctx = scope.Context;
        var meta = await ctx.TodoDocumentMetadata.AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var todoPath = ResolveTodoPath();
        var targetExists = !string.IsNullOrWhiteSpace(todoPath) && File.Exists(todoPath);
        return new TodoProjectionStatusResult(
            AuthoritativeStore: "database",
            AuthoritativeDataSource: _storageOptions.Value.Provider ?? TodoStorageOptions.DatabaseProvider,
            ProjectionTargetPath: todoPath ?? string.Empty,
            ProjectionTargetExists: targetExists,
            ProjectionConsistent: true,
            RepairRequired: false,
            VerifiedAtUtc: DateTime.UtcNow.ToString("O"),
            LastImportedFromYamlUtc: meta?.LastImportedFromYamlUtc,
            LastProjectedToYamlUtc: meta?.LastProjectedToYamlUtc,
            LastProjectionFailureUtc: meta?.LastProjectionFailureUtc,
            LastProjectionFailure: meta?.LastProjectionFailureMessage,
            Message: "Projection to TODO.yaml is deferred in the EF port; database is authoritative (TR-MCP-TODO-005).");
    }

    /// <inheritdoc />
    public async Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
    {
        var status = await GetProjectionStatusAsync(cancellationToken).ConfigureAwait(false);
        return new TodoProjectionRepairResult(true, null, status);
    }

    private async Task AppendAuditAsync(
        McpDbContext ctx,
        string todoId,
        string action,
        TodoFlatItem? snapshot,
        TodoFlatItem? previousSnapshot,
        string source,
        CancellationToken cancellationToken)
    {
        var maxVersion = await ctx.TodoAuditHistory
            .Where(h => h.TodoId == todoId)
            .Select(h => (int?)h.Version)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) ?? 0;

        ctx.TodoAuditHistory.Add(new TodoAuditHistoryEntity
        {
            TodoId = todoId,
            Version = maxVersion + 1,
            Action = action,
            RecordedAtUtc = DateTime.UtcNow.ToString("O"),
            SnapshotJson = SerializeFlatItem(snapshot),
            PreviousSnapshotJson = SerializeFlatItem(previousSnapshot),
            Source = source,
        });
    }

    private async Task<int> ResolveSectionOrderAsync(McpDbContext ctx, string section, CancellationToken cancellationToken)
    {
        var existing = await ctx.TodoItems
            .Where(i => i.Section == section)
            .Select(i => (int?)i.SectionOrder)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing.HasValue)
            return existing.Value;

        var maxOrder = await ctx.TodoItems
            .Select(i => (int?)i.SectionOrder)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) ?? 0;
        return maxOrder + 1;
    }

    private async Task<int> GetNextItemOrderAsync(McpDbContext ctx, string section, string priority, string itemKind, CancellationToken cancellationToken)
    {
        var maxOrder = await ctx.TodoItems
            .Where(i => i.Section == section && i.Priority == priority && i.ItemKind == itemKind)
            .Select(i => (int?)i.ItemOrder)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) ?? 0;
        return maxOrder + 1;
    }

    private async Task PublishChangeSafeAsync(string action, string entityId, CancellationToken cancellationToken)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Todo,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/todo/{entityId}",
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing EF TODO change event for {EntityId}", entityId);
        }
    }

    private string? ResolveTodoPath()
    {
        var repoRoot = _ingestionOptions.Value.RepoRoot ?? ".";
        var todoRel = _ingestionOptions.Value.TodoFilePath;
        if (string.IsNullOrWhiteSpace(todoRel))
            return null;
        return Path.GetFullPath(Path.IsPathRooted(todoRel) ? todoRel : Path.Combine(repoRoot, todoRel));
    }

    private DbScope CreateScope()
    {
        var scope = _scopeFactory.CreateAsyncScope();
        return new DbScope(scope);
    }

    private readonly struct DbScope : IAsyncDisposable
    {
        private readonly AsyncServiceScope _scope;

        public DbScope(AsyncServiceScope scope)
        {
            _scope = scope;
            Context = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        }

        public McpDbContext Context { get; }

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }

    private static int PriorityRank(string priority) => (priority ?? string.Empty).ToLowerInvariant() switch
    {
        "high" => 0,
        "medium" => 1,
        _ => 2,
    };

    private static string NormalizeSection(string section)
        => string.IsNullOrWhiteSpace(section) ? string.Empty : section.Trim();

    private static string NormalizePriority(string section, string priority)
        => string.Equals(section, CodeReviewSectionKey, StringComparison.OrdinalIgnoreCase) ? "high" : priority;

    private static string DetermineItemKind(string section)
        => string.Equals(section, CodeReviewSectionKey, StringComparison.OrdinalIgnoreCase) ? CodeReviewPhaseItemKind : StandardItemKind;

    private static System.Linq.Expressions.Expression<Func<TodoItemEntity, TodoFlatItem>> ToFlatItemExpression =>
        e => new TodoFlatItem
        {
            Id = e.Id,
            Title = e.Title,
            Section = e.Section,
            Priority = e.Priority,
            Done = e.Done,
            Estimate = e.Estimate,
            Note = e.Note,
            CompletedDate = e.CompletedDate,
            DoneSummary = e.DoneSummary,
            Remaining = e.Remaining,
            PriorityNote = e.PriorityNote,
            Reference = e.Reference,
            Phase = e.PhaseLabel,
        };

    private TodoFlatItem ToFlatItem(TodoItemEntity e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Section = e.Section,
        Priority = e.Priority,
        Done = e.Done,
        Estimate = e.Estimate,
        Note = e.Note,
        Description = DeserializeList(e.DescriptionJson),
        TechnicalDetails = DeserializeList(e.TechnicalDetailsJson),
        ImplementationTasks = DeserializeTasks(e.ImplementationTasksJson),
        CompletedDate = e.CompletedDate,
        DoneSummary = e.DoneSummary,
        Remaining = e.Remaining,
        PriorityNote = e.PriorityNote,
        Reference = e.Reference,
        Phase = e.PhaseLabel,
        DependsOn = DeserializeList(e.DependsOnJson),
        FunctionalRequirements = DeserializeList(e.FunctionalRequirementsJson),
        TechnicalRequirements = DeserializeList(e.TechnicalRequirementsJson),
    };

    private string? SerializeList(IReadOnlyList<string>? value) => value is null ? null : JsonSerializer.Serialize(value, _json);

    private string? SerializeTasks(IReadOnlyList<TodoFlatTask>? value) => value is null ? null : JsonSerializer.Serialize(value, _json);

    private string? SerializeFlatItem(TodoFlatItem? item) => item is null ? null : JsonSerializer.Serialize(item, _json);

    private List<string>? DeserializeList(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<List<string>>(value, _json);

    private List<TodoFlatTask>? DeserializeTasks(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<List<TodoFlatTask>>(value, _json);

    private TodoFlatItem? DeserializeFlatItem(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<TodoFlatItem>(value, _json);

    private static List<TodoFlatItem> ApplyFilters(List<TodoFlatItem> items, TodoQueryRequest request)
    {
        IEnumerable<TodoFlatItem> filtered = items;

        if (!string.IsNullOrWhiteSpace(request.Id))
            filtered = filtered.Where(i => string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Section))
            filtered = filtered.Where(i => string.Equals(i.Section, request.Section, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Priority))
            filtered = filtered.Where(i => string.Equals(i.Priority, request.Priority, StringComparison.OrdinalIgnoreCase));

        if (request.Done.HasValue)
            filtered = filtered.Where(i => i.Done == request.Done.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var matcher = BooleanSearchParser.Parse(request.Keyword);
            filtered = filtered.Where(i => matcher(BuildKeywordSearchText(i)));
        }

        return filtered.ToList();
    }

    private static string BuildKeywordSearchText(TodoFlatItem item)
        => string.Join(
            " ",
            new[] { item.Id, item.Title, item.Note, item.DoneSummary, item.Remaining, item.Phase }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Concat(item.Description ?? Array.Empty<string>())
                .Concat(item.TechnicalDetails ?? Array.Empty<string>())
                .Concat(item.ImplementationTasks?.Select(static task => task.Task) ?? Array.Empty<string>())
                .Where(static value => !string.IsNullOrWhiteSpace(value)));
}
