using System.Text.Json;
using McpServer.Cqrs.Search;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YamlDotNet.Core;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-005 / TR-MCP-TODO-006 (provider-agnostic): EF Core-backed TODO
/// service. Persistence flows through <see cref="McpDbContext"/> and therefore
/// through whichever provider <c>Mcp:Database:Provider</c> selects via
/// <c>McpDatabaseProviderFactory</c> (TR-MCP-CFG-007).
/// </summary>
/// <remarks>
/// Covers CRUD, append-only audit history, deterministic YAML projection,
/// projection-status checks, and operator-requested projection repair. This is
/// the sole live TODO store; the legacy provider-specific store has been retired.
/// </remarks>
internal sealed class EfTodoService : ITodoService, ITodoStore, ITodoCompensationService, IDisposable
{
    private const int RequirementIdMaxLength = 128;
    private const string DefaultTodoRelativePath = "docs/Project/TODO.yaml";
    private const string StandardItemKind = "standard";
    private const string CodeReviewPhaseItemKind = "code_review_phase";
    private const string CodeReviewSectionKey = "code-review-remediation";
    private const string ApiSource = "api";
    private const string DescriptionListType = "Description";
    private const string TechnicalDetailListType = "TechnicalDetail";
    private const string DependsOnListType = "DependsOn";
    private const string FunctionalRequirementListType = "FunctionalRequirement";
    private const string TechnicalRequirementListType = "TechnicalRequirement";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<IngestionOptions> _ingestionOptions;
    private readonly IOptions<TodoStorageOptions> _storageOptions;
    private readonly IWriteAuditLog _auditLog;
    private readonly ILogger<EfTodoService> _logger;
    private readonly IChangeEventBus? _eventBus;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly string? _fixedWorkspacePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="EfTodoService"/> class.
    /// </summary>
    public EfTodoService(
        IServiceScopeFactory scopeFactory,
        IOptions<IngestionOptions> ingestionOptions,
        IOptions<TodoStorageOptions> storageOptions,
        IWriteAuditLog auditLog,
        ILogger<EfTodoService> logger,
        IChangeEventBus? eventBus = null,
        IHttpContextAccessor? httpContextAccessor = null,
        string? fixedWorkspacePath = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _ingestionOptions = ingestionOptions ?? throw new ArgumentNullException(nameof(ingestionOptions));
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
        _httpContextAccessor = httpContextAccessor;
        _fixedWorkspacePath = string.IsNullOrWhiteSpace(fixedWorkspacePath)
            ? null
            : Path.GetFullPath(fixedWorkspacePath);
    }

    /// <inheritdoc />
    public void Dispose() => _writeLock.Dispose();

    /// <inheritdoc />
    public async Task<TodoCompensationSnapshot?> CaptureForRestoreAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            return await CaptureForRestoreCoreAsync(scope.Context, id, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoMutationResult> RestoreAsync(TodoCompensationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.State is not EfTodoCompensationState state)
        {
            return new TodoMutationResult(
                false,
                $"TODO compensation snapshot provider '{snapshot.Provider}' is not supported by {nameof(EfTodoService)}.",
                FailureKind: TodoMutationFailureKind.Conflict);
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            var workspaceId = string.IsNullOrWhiteSpace(state.Item.WorkspaceId)
                ? ctx.CurrentWorkspaceId
                : state.Item.WorkspaceId;
            var existing = await ctx.TodoItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(row => row.WorkspaceId == workspaceId && row.Id == state.Item.Id, cancellationToken)
                .ConfigureAwait(false);
            TodoItemEntity restored;
            if (existing is null)
            {
                restored = CloneTodoItem(state.Item);
                ctx.TodoItems.Add(restored);
            }
            else
            {
                CopyTodoItem(state.Item, existing);
                restored = existing;
            }

            // Restore the 4NF child rows dependent-side: replace whatever rows exist (including
            // soft-deleted ones) with the captured snapshot's children.
            var staleListRows = await ctx.TodoItemListItems
                .IgnoreQueryFilters()
                .Where(r => r.WorkspaceId == workspaceId && r.TodoId == state.Item.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var staleTaskRows = await ctx.TodoItemTasks
                .IgnoreQueryFilters()
                .Where(r => r.WorkspaceId == workspaceId && r.TodoId == state.Item.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            ctx.TodoItemListItems.RemoveRange(staleListRows);
            ctx.TodoItemTasks.RemoveRange(staleTaskRows);
            ctx.TodoItemListItems.AddRange(restored.ListItems);
            ctx.TodoItemTasks.AddRange(restored.ImplementationTaskRows);

            ApplySoftDeleteState(ctx.Entry(restored), state.SoftDelete);
            await RestoreDocumentMetadataAsync(ctx, state.DocumentMetadata, cancellationToken).ConfigureAwait(false);

            var restoredFlat = ToFlatItem(restored);
            await SyncTodoRequirementLinksAsync(ctx, restored, restoredFlat, cancellationToken).ConfigureAwait(false);
            await AppendAuditAsync(ctx, restored.Id, "restored", restoredFlat, null, "transaction_rollback", cancellationToken).ConfigureAwait(false);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await FinalizeMutationAsync(ChangeEventActions.Updated, restored.Id, restoredFlat, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoCompensatedMutationResult> UpdateWithRestorePointAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(request);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var snapshot = await CaptureForRestoreCoreAsync(scope.Context, id, cancellationToken).ConfigureAwait(false);
            var result = await UpdateCoreAsync(scope.Context, id, request, cancellationToken).ConfigureAwait(false);
            return new TodoCompensatedMutationResult
            {
                Result = result,
                Snapshot = snapshot,
            };
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoCompensatedMutationResult> DeleteWithRestorePointAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var snapshot = await CaptureForRestoreCoreAsync(scope.Context, id, cancellationToken).ConfigureAwait(false);
            var result = await DeleteCoreAsync(scope.Context, id, cancellationToken).ConfigureAwait(false);
            return new TodoCompensatedMutationResult
            {
                Result = result,
                Snapshot = snapshot,
            };
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> DeleteCreatedAsync(string id, CancellationToken cancellationToken = default)
        => DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var scope = CreateScope();
        var ctx = scope.Context;

        // Push row-level filters to SQL so an id/section/priority/done query does not
        // materialize the whole workspace. Keyword search, flattening, and priority-rank
        // ordering remain client-side (ApplyFilters below) over the reduced set.
        IQueryable<TodoItemEntity> query = ctx.TodoItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Id))
        {
            var id = request.Id.ToUpperInvariant();
            query = query.Where(t => t.Id.ToUpper() == id);
        }
        if (!string.IsNullOrWhiteSpace(request.Section))
        {
            var section = request.Section.ToUpperInvariant();
            query = query.Where(t => t.Section.ToUpper() == section);
        }
        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priority = request.Priority.ToUpperInvariant();
            query = query.Where(t => t.Priority.ToUpper() == priority);
        }
        if (request.Done.HasValue)
        {
            query = query.Where(t => t.Done == request.Done.Value);
        }

        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        await AttachTodoChildrenAsync(ctx, rows, cancellationToken).ConfigureAwait(false);
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
        if (row is null)
            return null;
        await AttachTodoChildrenAsync(scope.Context, [row], cancellationToken).ConfigureAwait(false);
        return ToFlatItem(row);
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

            var existingItem = await ctx.TodoItems.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken).ConfigureAwait(false);
            if (existingItem is not null)
            {
                await AttachTodoChildrenAsync(ctx, [existingItem], cancellationToken).ConfigureAwait(false);
                var existingFlat = ToFlatItem(existingItem);
                if (!string.IsNullOrWhiteSpace(request.IdempotencyKey)
                    && string.Equals(existingItem.IdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal)
                    && TodoPayloadFingerprint.AreEquivalent(request, existingFlat))
                {
                    return new TodoMutationResult(true, Item: existingFlat);
                }

                return new TodoMutationResult(false, $"Item with id '{request.Id}' already exists.", FailureKind: TodoMutationFailureKind.Conflict);
            }

            var allEntities = await ctx.TodoItems.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
            await AttachTodoChildrenAsync(ctx, allEntities, cancellationToken).ConfigureAwait(false);
            var all = allEntities.Select(ToFlatItem).ToList();
            var depIdError = TodoValidator.ValidateDependencyIds(request.DependsOn, all, "dependsOn");
            if (depIdError is not null)
                return new TodoMutationResult(false, depIdError, FailureKind: TodoMutationFailureKind.Validation);
            var depError = TodoValidator.ValidateDependencies(request.Id, request.DependsOn?.ToList() ?? [], all);
            if (depError is not null)
                return new TodoMutationResult(false, depError, FailureKind: TodoMutationFailureKind.Validation);

            var itemKind = DetermineItemKind(normalizedSection);
            var sectionOrder = await ResolveSectionOrderAsync(ctx, normalizedSection, cancellationToken).ConfigureAwait(false);
            var itemOrder = await GetNextItemOrderAsync(ctx, normalizedSection, normalizedPriority, itemKind, cancellationToken).ConfigureAwait(false);

            var workspaceId = ctx.CurrentWorkspaceId;
            var entity = new TodoItemEntity
            {
                WorkspaceId = workspaceId,
                Id = request.Id,
                Title = request.Title,
                Section = normalizedSection,
                Priority = normalizedPriority,
                Done = false,
                Estimate = request.Estimate,
                Note = request.Note,
                Remaining = request.Remaining,
                ItemKind = itemKind,
                SectionOrder = sectionOrder,
                ItemOrder = itemOrder,
                PhaseLabel = itemKind == CodeReviewPhaseItemKind ? request.Phase ?? request.Title : null,
                IdempotencyKey = request.IdempotencyKey,
            };

            ctx.TodoItems.Add(entity);
            var listItems = BuildListItems(
                workspaceId,
                entity.Id,
                request.Description,
                request.TechnicalDetails,
                request.DependsOn,
                request.FunctionalRequirements,
                request.TechnicalRequirements);
            var taskRows = BuildTaskRows(workspaceId, entity.Id, request.ImplementationTasks);
            ctx.TodoItemListItems.AddRange(listItems);
            ctx.TodoItemTasks.AddRange(taskRows);
            entity.ListItems = listItems;
            entity.ImplementationTaskRows = taskRows;
            var flat = ToFlatItem(entity);
            await SyncTodoRequirementLinksAsync(ctx, entity, flat, cancellationToken).ConfigureAwait(false);
            await AppendAuditAsync(ctx, entity.Id, ChangeEventActions.Created, flat, null, ApiSource, cancellationToken).ConfigureAwait(false);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await FinalizeMutationAsync(ChangeEventActions.Created, entity.Id, flat, cancellationToken).ConfigureAwait(false);
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
            return await UpdateCoreAsync(scope.Context, id, request, cancellationToken).ConfigureAwait(false);
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
            return await DeleteCoreAsync(scope.Context, id, cancellationToken).ConfigureAwait(false);
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

            await AttachTodoChildrenAsync(ctx, [current], cancellationToken).ConfigureAwait(false);

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
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GetProjectionStatusCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                await ProjectDatabaseToYamlAsync(cancellationToken).ConfigureAwait(false);
                var status = await GetProjectionStatusCoreAsync(cancellationToken).ConfigureAwait(false);
                return new TodoProjectionRepairResult(true, null, status);
            }
            catch (Exception ex) when (IsProjectionException(ex))
            {
                var todoPath = ResolveTodoPath();
                _logger.LogError(ex, "Operator-requested TODO projection repair failed for {TodoFilePath}.", todoPath);
                await TryRecordProjectionFailureAsync(ex).ConfigureAwait(false);
                var status = await GetProjectionStatusCoreAsync(cancellationToken).ConfigureAwait(false);
                return new TodoProjectionRepairResult(false, $"Failed to repair TODO projection at '{todoPath}': {ex.Message}", status);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task<TodoCompensationSnapshot?> CaptureForRestoreCoreAsync(
        McpDbContext ctx,
        string id,
        CancellationToken cancellationToken)
    {
        var workspaceId = ctx.CurrentWorkspaceId;
        var item = await ctx.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(row => row.WorkspaceId == workspaceId && row.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (item is null)
            return null;

        // Capture the live 4NF child rows bypassing filters (the item itself may be soft-deleted,
        // in which case its children are too; the snapshot must still carry them for restore).
        item.ListItems = await ctx.TodoItemListItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId && r.TodoId == id)
            .OrderBy(r => r.Ordinal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        item.ImplementationTaskRows = await ctx.TodoItemTasks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId && r.TodoId == id)
            .OrderBy(r => r.Ordinal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var metadata = await ctx.TodoDocumentMetadata
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(row => row.WorkspaceId == workspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (metadata is not null)
            await AttachMetadataChildrenAsync(ctx, metadata, cancellationToken).ConfigureAwait(false);
        var state = new EfTodoCompensationState(
            CloneTodoItem(item),
            ReadSoftDeleteState(ctx.Entry(item)),
            metadata is null
                ? null
                : new EfTodoDocumentMetadataState(
                    CloneDocumentMetadata(metadata),
                    ReadSoftDeleteState(ctx.Entry(metadata))));

        return new TodoCompensationSnapshot
        {
            Provider = nameof(EfTodoService),
            State = state,
        };
    }

    private async Task<TodoMutationResult> UpdateCoreAsync(
        McpDbContext ctx,
        string id,
        TodoUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await ctx.TodoItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return new TodoMutationResult(false, $"Item with id '{id}' not found.", FailureKind: TodoMutationFailureKind.NotFound);

        await AttachTodoChildrenAsync(ctx, [existing], cancellationToken).ConfigureAwait(false);
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
        existing.CompletedDate = request.CompletedDate ?? existing.CompletedDate;
        existing.DoneSummary = request.DoneSummary ?? existing.DoneSummary;
        existing.Remaining = request.Remaining ?? existing.Remaining;
        existing.Reference = request.Reference ?? existing.Reference;

        // 4NF children: null request lists keep the previous values; supplied lists replace them.
        var updatedListItems = BuildListItems(
            existing.WorkspaceId,
            existing.Id,
            request.Description ?? previousFlat.Description,
            request.TechnicalDetails ?? previousFlat.TechnicalDetails,
            request.DependsOn ?? previousFlat.DependsOn,
            request.FunctionalRequirements ?? previousFlat.FunctionalRequirements,
            request.TechnicalRequirements ?? previousFlat.TechnicalRequirements);
        var updatedTaskRows = BuildTaskRows(
            existing.WorkspaceId,
            existing.Id,
            request.ImplementationTasks ?? previousFlat.ImplementationTasks);
        await ReplaceTodoChildrenAsync(ctx, existing, updatedListItems, updatedTaskRows, cancellationToken).ConfigureAwait(false);
        existing.ItemKind = updatedKind;
        existing.PhaseLabel = updatedKind == CodeReviewPhaseItemKind
            ? request.Phase ?? existing.PhaseLabel ?? existing.Title
            : null;
        if (updatedKind == CodeReviewPhaseItemKind && request.Reference is not null)
        {
            var metadata = await GetOrCreateDocumentMetadataAsync(ctx, cancellationToken).ConfigureAwait(false);
            metadata.CodeReviewReference = request.Reference;
        }

        var updatedDependsOn = ListValues(existing, DependsOnListType);
        var allEntities = await ctx.TodoItems.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        await AttachTodoChildrenAsync(ctx, allEntities, cancellationToken).ConfigureAwait(false);
        var all = allEntities.Select(ToFlatItem).ToList();
        var depIdError = TodoValidator.ValidateDependencyIds(updatedDependsOn, all, "dependsOn");
        if (depIdError is not null)
            return new TodoMutationResult(false, depIdError, FailureKind: TodoMutationFailureKind.Validation);
        var depError = TodoValidator.ValidateDependencies(id, updatedDependsOn?.ToList() ?? [], all);
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
        await SyncTodoRequirementLinksAsync(ctx, existing, updatedFlat, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync(ctx, existing.Id, ChangeEventActions.Updated, updatedFlat, previousFlat, ApiSource, cancellationToken).ConfigureAwait(false);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await FinalizeMutationAsync(ChangeEventActions.Updated, existing.Id, updatedFlat, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TodoMutationResult> DeleteCoreAsync(
        McpDbContext ctx,
        string id,
        CancellationToken cancellationToken)
    {
        var existing = await ctx.TodoItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return new TodoMutationResult(false, $"Item with id '{id}' not found.", FailureKind: TodoMutationFailureKind.NotFound);

        await AttachTodoChildrenAsync(ctx, [existing], cancellationToken).ConfigureAwait(false);
        var snapshot = ToFlatItem(existing);
        await SoftDeleteTodoRequirementLinksAsync(ctx, existing, cancellationToken).ConfigureAwait(false);
        var childLists = await ctx.TodoItemListItems
            .Where(r => r.WorkspaceId == existing.WorkspaceId && r.TodoId == existing.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var childTasks = await ctx.TodoItemTasks
            .Where(r => r.WorkspaceId == existing.WorkspaceId && r.TodoId == existing.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        ctx.TodoItemListItems.RemoveRange(childLists);
        ctx.TodoItemTasks.RemoveRange(childTasks);
        ctx.TodoItems.Remove(existing);
        await AppendAuditAsync(ctx, id, ChangeEventActions.Deleted, snapshot, snapshot, ApiSource, cancellationToken).ConfigureAwait(false);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await FinalizeMutationAsync(ChangeEventActions.Deleted, id, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TodoMutationResult> FinalizeMutationAsync(
        string action,
        string id,
        TodoFlatItem? item,
        CancellationToken cancellationToken)
    {
        await PublishChangeSafeAsync(action, id, cancellationToken).ConfigureAwait(false);

        try
        {
            await ProjectDatabaseToYamlAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsProjectionException(ex))
        {
            var todoPath = ResolveTodoPath();
            await TryRecordProjectionFailureAsync(ex).ConfigureAwait(false);
            _logger.LogError(ex, "TODO mutation for {Id} committed in database but projection to {TodoFilePath} failed.", id, todoPath);
            var message = $"TODO '{id}' was committed to authoritative database storage, but projection to '{todoPath}' failed: {ex.Message}";
            return new TodoMutationResult(false, message, item, TodoMutationFailureKind.ProjectionFailed);
        }

        return new TodoMutationResult(true, Item: item);
    }

    private async Task ProjectDatabaseToYamlAsync(CancellationToken cancellationToken)
    {
        var todoPath = ResolveTodoPath();
        var file = await BuildProjectedTodoFileAsync(cancellationToken).ConfigureAwait(false);
        await TodoYamlFileSerializer.WriteAtomicallyAsync(todoPath, file, cancellationToken).ConfigureAwait(false);

        try
        {
            await using var scope = CreateScope();
            var metadata = await GetOrCreateDocumentMetadataAsync(scope.Context, CancellationToken.None).ConfigureAwait(false);
            metadata.LastProjectedToYamlUtc = DateTime.UtcNow.ToString("O");
            metadata.LastProjectionFailureUtc = null;
            metadata.LastProjectionFailureMessage = null;
            await scope.Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
        {
            _logger.LogWarning(ex, "TODO.yaml projection succeeded for {TodoFilePath}, but projection metadata could not be updated.", todoPath);
        }
    }

    private async Task<TodoFile> BuildProjectedTodoFileAsync(CancellationToken cancellationToken)
    {
        await using var scope = CreateScope();
        var ctx = scope.Context;
        var items = await ctx.TodoItems
            .AsNoTracking()
            .OrderBy(static item => item.SectionOrder)
            .ThenBy(static item => item.Section)
            .ThenBy(static item => item.Priority)
            .ThenBy(static item => item.ItemOrder)
            .ThenBy(static item => item.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        await AttachTodoChildrenAsync(ctx, items, cancellationToken).ConfigureAwait(false);
        var metadata = await ReadDocumentMetadataAsync(ctx, cancellationToken).ConfigureAwait(false);
        return BuildProjectedTodoFile(items, metadata);
    }

    private TodoFile BuildProjectedTodoFile(IReadOnlyList<TodoItemEntity> items, TodoDocumentMetadataEntity metadata)
    {
        var file = new TodoFile();
        foreach (var sectionGroup in items
            .Where(static item => string.Equals(item.ItemKind, StandardItemKind, StringComparison.OrdinalIgnoreCase))
            .GroupBy(static item => item.Section, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Min(static item => item.SectionOrder))
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var section = new TodoSection
            {
                HighPriority = BuildPriorityItems(sectionGroup, "high"),
                MediumPriority = BuildPriorityItems(sectionGroup, "medium"),
                LowPriority = BuildPriorityItems(sectionGroup, "low"),
            };

            file.Sections[sectionGroup.Key] = section;
        }

        var phases = items
            .Where(static item => string.Equals(item.ItemKind, CodeReviewPhaseItemKind, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static item => item.ItemOrder)
            .ThenBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(item => new CodeReviewPhase
            {
                Id = item.Id,
                Phase = item.PhaseLabel,
                Estimate = item.Estimate,
                Done = item.Done,
                Title = item.Title,
                ImplementationTasks = TaskValues(item)?
                    .Select(static task => new ImplementationTask { Task = task.Task, Done = task.Done })
                    .ToList(),
            })
            .ToList();

        if (phases.Count > 0 || !string.IsNullOrWhiteSpace(metadata.CodeReviewReference))
        {
            file.CodeReviewRemediation = new CodeReviewSection
            {
                Reference = metadata.CodeReviewReference,
                Phases = phases.Count == 0 ? null : phases,
            };
        }

        var completedGroups = metadata.CompletedGroups
            .OrderBy(g => g.Ordinal)
            .Select(g => new CompletedGroup
            {
                Date = g.Date,
                Items = g.Items.Count == 0
                    ? null
                    : g.Items
                        .OrderBy(i => i.Ordinal)
                        .Select(i => new CompletedItem { Id = i.ItemId, Qualifier = i.Qualifier, Summary = i.Summary })
                        .ToList(),
            })
            .ToList();
        file.Completed = completedGroups.Count == 0 ? null : completedGroups;

        var notes = metadata.Notes.OrderBy(n => n.Ordinal).Select(n => n.Value).ToList();
        file.Notes = notes.Count == 0 ? null : notes;
        return file;
    }

    /// <summary>
    /// Loads and attaches the 4NF note and completed-archive child rows onto the (non-mapped)
    /// holders of a document-metadata singleton.
    /// </summary>
    private static async Task AttachMetadataChildrenAsync(McpDbContext ctx, TodoDocumentMetadataEntity metadata, CancellationToken cancellationToken)
    {
        metadata.Notes = await ctx.TodoDocumentNotes
            .AsNoTracking()
            .Where(n => n.WorkspaceId == metadata.WorkspaceId && n.SingletonId == metadata.SingletonId)
            .OrderBy(n => n.Ordinal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        metadata.CompletedGroups = await ctx.TodoCompletedGroups
            .AsNoTracking()
            .Include(g => g.Items)
            .Where(g => g.WorkspaceId == metadata.WorkspaceId && g.SingletonId == metadata.SingletonId)
            .OrderBy(g => g.Ordinal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<TodoProjectionStatusResult> GetProjectionStatusCoreAsync(CancellationToken cancellationToken)
    {
        await using var scope = CreateScope();
        var ctx = scope.Context;
        var metadata = await ReadDocumentMetadataAsync(ctx, cancellationToken).ConfigureAwait(false);
        var projectedFile = await BuildProjectedTodoFileAsync(cancellationToken).ConfigureAwait(false);
        var todoPath = ResolveTodoPath();

        var projectionTargetExists = File.Exists(todoPath);
        var projectionConsistent = false;
        string? consistencyMessage = null;

        if (!projectionTargetExists)
        {
            consistencyMessage = Directory.Exists(todoPath)
                ? $"Projected TODO target '{todoPath}' is a directory instead of a file."
                : $"Projected TODO file '{todoPath}' does not exist.";
        }
        else
        {
            try
            {
                var actualFile = await TodoYamlFileSerializer.ReadIfExistsAsync(todoPath, cancellationToken).ConfigureAwait(false);
                if (actualFile is null)
                {
                    consistencyMessage = $"Projected TODO file '{todoPath}' could not be loaded for consistency verification.";
                }
                else
                {
                    projectionConsistent = string.Equals(
                        NormalizeYaml(TodoYamlFileSerializer.Serialize(actualFile)),
                        NormalizeYaml(TodoYamlFileSerializer.Serialize(projectedFile)),
                        StringComparison.Ordinal);

                    if (!projectionConsistent)
                        consistencyMessage = $"Projected TODO file '{todoPath}' does not match authoritative database state.";
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or YamlException)
            {
                consistencyMessage = $"Projected TODO file '{todoPath}' could not be read for consistency verification: {ex.Message}";
            }
        }

        var repairRequired = !projectionTargetExists || !projectionConsistent;
        var historicalFailureMessage = string.IsNullOrWhiteSpace(metadata.LastProjectionFailureMessage)
            ? null
            : $"Last recorded projection failure at {metadata.LastProjectionFailureUtc ?? "an unknown time"}: {metadata.LastProjectionFailureMessage}";

        var message = consistencyMessage
            ?? (repairRequired
                ? historicalFailureMessage ?? "TODO.yaml requires repair to match authoritative database state."
                : historicalFailureMessage is null
                    ? "TODO.yaml matches authoritative database state."
                    : $"TODO.yaml matches authoritative database state. {historicalFailureMessage}");

        // Report the configured Mcp database engine (sqlserver/postgresql/sqlite), not the
        // TodoStorage transport label. Legacy TodoStorage.Provider=sqlite is only an alias for
        // database mode and must not be surfaced as the authoritative data source.
        return new TodoProjectionStatusResult(
            AuthoritativeStore: "database",
            AuthoritativeDataSource: ResolveAuthoritativeDatabaseEngine(ctx),
            ProjectionTargetPath: todoPath,
            ProjectionTargetExists: projectionTargetExists,
            ProjectionConsistent: projectionConsistent,
            RepairRequired: repairRequired,
            VerifiedAtUtc: DateTime.UtcNow.ToString("O"),
            LastImportedFromYamlUtc: metadata.LastImportedFromYamlUtc,
            LastProjectedToYamlUtc: metadata.LastProjectedToYamlUtc,
            LastProjectionFailureUtc: metadata.LastProjectionFailureUtc,
            LastProjectionFailure: metadata.LastProjectionFailureMessage,
            Message: message);
    }

    /// <summary>
    /// Maps the active EF Core provider to the canonical Mcp:Database:Provider name.
    /// </summary>
    private static string ResolveAuthoritativeDatabaseEngine(McpDbContext ctx)
    {
        var efName = ctx.Database.ProviderName ?? string.Empty;
        if (efName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            return "sqlserver";
        if (efName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ||
            efName.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
            return "postgresql";
        if (efName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            return "sqlite";
        return string.IsNullOrWhiteSpace(efName) ? TodoStorageOptions.DatabaseProvider : efName;
    }

    private async Task TryRecordProjectionFailureAsync(Exception ex)
    {
        try
        {
            await using var scope = CreateScope();
            var metadata = await GetOrCreateDocumentMetadataAsync(scope.Context, CancellationToken.None).ConfigureAwait(false);
            metadata.LastProjectionFailureUtc = DateTime.UtcNow.ToString("O");
            metadata.LastProjectionFailureMessage = ex.Message;
            await scope.Context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception recordEx) when (recordEx is InvalidOperationException or DbUpdateException)
        {
            _logger.LogWarning(recordEx, "Failed to persist TODO projection failure metadata for {TodoFilePath}.", ResolveTodoPath());
        }
    }

    private static async Task<TodoDocumentMetadataEntity> ReadDocumentMetadataAsync(McpDbContext ctx, CancellationToken cancellationToken)
    {
        var metadata = await ctx.TodoDocumentMetadata.AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (metadata is null)
            return new TodoDocumentMetadataEntity { SingletonId = 1 };

        await AttachMetadataChildrenAsync(ctx, metadata, cancellationToken).ConfigureAwait(false);
        return metadata;
    }

    private static async Task<TodoDocumentMetadataEntity> GetOrCreateDocumentMetadataAsync(McpDbContext ctx, CancellationToken cancellationToken)
    {
        var metadata = await ctx.TodoDocumentMetadata.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (metadata is not null)
            return metadata;

        metadata = new TodoDocumentMetadataEntity { SingletonId = 1 };
        ctx.TodoDocumentMetadata.Add(metadata);
        return metadata;
    }

    private List<TodoItem>? BuildPriorityItems(IEnumerable<TodoItemEntity> group, string priority)
    {
        var items = group
            .Where(item => string.Equals(item.Priority, priority, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static item => item.ItemOrder)
            .ThenBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(ToProjectedTodoItem)
            .ToList();

        return items.Count == 0 ? null : items;
    }

    private TodoItem ToProjectedTodoItem(TodoItemEntity item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Estimate = item.Estimate,
        Note = item.Note,
        Done = item.Done,
        CompletedDate = item.CompletedDate,
        Description = ListValues(item, DescriptionListType),
        DoneSummary = item.DoneSummary,
        Remaining = item.Remaining,
        TechnicalDetails = ListValues(item, TechnicalDetailListType),
        PriorityNote = item.PriorityNote,
        Reference = item.Reference,
        DependsOn = ListValues(item, DependsOnListType),
        FunctionalRequirements = ListValues(item, FunctionalRequirementListType),
        TechnicalRequirements = ListValues(item, TechnicalRequirementListType),
        ImplementationTasks = TaskValues(item)?
            .Select(static task => new ImplementationTask { Task = task.Task, Done = task.Done })
            .ToList(),
    };

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

    private async Task SyncTodoRequirementLinksAsync(
        McpDbContext ctx,
        TodoItemEntity entity,
        TodoFlatItem flat,
        CancellationToken cancellationToken)
    {
        var workspaceId = string.IsNullOrWhiteSpace(entity.WorkspaceId)
            ? ctx.CurrentWorkspaceId
            : entity.WorkspaceId;
        var now = DateTimeOffset.UtcNow;
        var desired = NormalizeRequirementLinks(flat)
            .Select(link => (WorkspaceId: workspaceId, TodoId: entity.Id, link.Kind, link.Id))
            .ToArray();

        foreach (var link in desired)
        {
            await EnsureRequirementAsync(ctx, link.WorkspaceId, link.Kind, link.Id, now, cancellationToken).ConfigureAwait(false);
        }

        var existingLinks = await ctx.TodoRequirementLinks
            .IgnoreQueryFilters()
            .Where(row => row.WorkspaceId == workspaceId && row.TodoId == entity.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var desiredKeys = desired
            .Select(link => BuildTodoRequirementKey(link.Kind, link.Id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var existing in existingLinks)
        {
            var key = BuildTodoRequirementKey(existing.RequirementKind, existing.RequirementId);
            SetSoftDeleteState(ctx.Entry(existing), !desiredKeys.Contains(key), !desiredKeys.Contains(key) ? "todo_requirement_sync" : null);
        }

        foreach (var link in desired)
        {
            var existing = existingLinks.SingleOrDefault(row =>
                string.Equals(row.RequirementKind, link.Kind, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.RequirementId, link.Id, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                SetSoftDeleteState(ctx.Entry(existing), false, null);
                continue;
            }

            ctx.TodoRequirementLinks.Add(new TodoRequirementLinkEntity
            {
                WorkspaceId = link.WorkspaceId,
                TodoId = link.TodoId,
                RequirementKind = link.Kind,
                RequirementId = link.Id,
                CreatedAtUtc = now,
            });
        }
    }

    private static async Task EnsureRequirementAsync(
        McpDbContext ctx,
        string workspaceId,
        string kind,
        string id,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await ctx.Requirements
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(row => row.WorkspaceId == workspaceId && row.Kind == kind && row.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            SetSoftDeleteState(ctx.Entry(existing), false, null);
            return;
        }

        if (!IsValidRequirementId(id, kind))
            return;

        // Use a stable ancient timestamp for backfills to avoid "createdAt at list/read time" symptom.
        // Title==id and placeholder body are intentional markers for dangling TODO links; consumers
        // should prefer list + filter or explicit creates over relying on these.
        var backfillTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        ctx.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = workspaceId,
            Kind = kind,
            Id = id,
            Title = id,
            Body = $"Placeholder requirement backfilled for TODO link {id}.",
            Priority = "medium",
            Status = "pending",
            CreatedAtUtc = backfillTime.ToString("O"),
            UpdatedAtUtc = backfillTime.ToString("O"),
        });
    }

    private static async Task SoftDeleteTodoRequirementLinksAsync(
        McpDbContext ctx,
        TodoItemEntity entity,
        CancellationToken cancellationToken)
    {
        var workspaceId = string.IsNullOrWhiteSpace(entity.WorkspaceId)
            ? ctx.CurrentWorkspaceId
            : entity.WorkspaceId;
        var links = await ctx.TodoRequirementLinks
            .Where(row => row.WorkspaceId == workspaceId && row.TodoId == entity.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var link in links)
        {
            ctx.TodoRequirementLinks.Remove(link);
        }
    }

    private static IEnumerable<(string Kind, string Id)> NormalizeRequirementLinks(TodoFlatItem flat)
    {
        foreach (var id in NormalizeRequirementIds(flat.FunctionalRequirements).Where(id => IsValidRequirementId(id, "fr")))
            yield return ("fr", id);
        foreach (var id in NormalizeRequirementIds(flat.TechnicalRequirements).Where(id => IsValidRequirementId(id, "tr")))
            yield return ("tr", id);
    }

    private static IEnumerable<string> NormalizeRequirementIds(IReadOnlyList<string>? ids)
    {
        return ids?
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => ExtractRequirementId(id))
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    private static string ExtractRequirementId(string value)
    {
        var trimmed = value.Trim();
        var delimiter = trimmed.IndexOfAny([' ', '\t', '\r', '\n', ':']);
        if (delimiter > 0)
            trimmed = trimmed[..delimiter];

        return trimmed.Length <= RequirementIdMaxLength
            ? trimmed
            : trimmed[..RequirementIdMaxLength];
    }

    private static bool IsValidRequirementId(string id, string kind)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var k = kind.ToLowerInvariant();
        if (k == "fr")
            return System.Text.RegularExpressions.Regex.IsMatch(id, @"^FR-[A-Z0-9]+(-[A-Z0-9]+)*-\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (k == "tr")
            return System.Text.RegularExpressions.Regex.IsMatch(id, @"^TR-[A-Z0-9]+(-[A-Z0-9]+)*-\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (k == "test")
            return System.Text.RegularExpressions.Regex.IsMatch(id, @"^TEST-[A-Z0-9]+(-[A-Z0-9]+)*-\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return true;
    }

    private static string BuildTodoRequirementKey(string kind, string id) => kind.ToLowerInvariant() + ":" + id;

    private static void SetSoftDeleteState(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool isDeleted, string? reason)
    {
        entry.Property("IsDeleted").CurrentValue = isDeleted;
        entry.Property("DeletedAtUtc").CurrentValue = isDeleted ? DateTimeOffset.UtcNow : null;
        entry.Property("DeletedBy").CurrentValue = isDeleted ? nameof(EfTodoService) : null;
        entry.Property("DeleteReason").CurrentValue = reason;
    }

    private static SoftDeleteState ReadSoftDeleteState(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        => new(
            entry.Property("IsDeleted").CurrentValue is true,
            entry.Property("DeletedAtUtc").CurrentValue as DateTimeOffset?,
            entry.Property("DeletedBy").CurrentValue as string,
            entry.Property("DeleteReason").CurrentValue as string);

    private static void ApplySoftDeleteState(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, SoftDeleteState state)
    {
        entry.Property("IsDeleted").CurrentValue = state.IsDeleted;
        entry.Property("DeletedAtUtc").CurrentValue = state.DeletedAtUtc;
        entry.Property("DeletedBy").CurrentValue = state.DeletedBy;
        entry.Property("DeleteReason").CurrentValue = state.DeleteReason;
    }

    private async Task RestoreDocumentMetadataAsync(
        McpDbContext ctx,
        EfTodoDocumentMetadataState? state,
        CancellationToken cancellationToken)
    {
        var workspaceId = state?.Metadata.WorkspaceId ?? ctx.CurrentWorkspaceId;
        var existing = await ctx.TodoDocumentMetadata
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(row => row.WorkspaceId == workspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (state is null)
        {
            if (existing is not null)
                ctx.TodoDocumentMetadata.Remove(existing);
            return;
        }

        if (existing is null)
        {
            existing = CloneDocumentMetadata(state.Metadata);
            ctx.TodoDocumentMetadata.Add(existing);
        }
        else
        {
            CopyDocumentMetadata(state.Metadata, existing);
        }

        // Restore the 4NF note/completed child rows dependent-side: replace whatever rows exist
        // (including soft-deleted ones) with the captured snapshot's children.
        var staleNotes = await ctx.TodoDocumentNotes
            .IgnoreQueryFilters()
            .Where(n => n.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var staleGroups = await ctx.TodoCompletedGroups
            .IgnoreQueryFilters()
            .Include(g => g.Items)
            .Where(g => g.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        ctx.TodoCompletedItems.RemoveRange(staleGroups.SelectMany(g => g.Items));
        ctx.TodoCompletedGroups.RemoveRange(staleGroups);
        ctx.TodoDocumentNotes.RemoveRange(staleNotes);
        ctx.TodoDocumentNotes.AddRange(existing.Notes);
        ctx.TodoCompletedGroups.AddRange(existing.CompletedGroups);

        ApplySoftDeleteState(ctx.Entry(existing), state.SoftDelete);
    }

    private static TodoItemEntity CloneTodoItem(TodoItemEntity source)
    {
        var target = new TodoItemEntity
        {
            WorkspaceId = source.WorkspaceId,
            Id = source.Id,
            Title = source.Title,
            Section = source.Section,
            Priority = source.Priority,
        };
        CopyTodoItem(source, target);
        return target;
    }

    private static void CopyTodoItem(TodoItemEntity source, TodoItemEntity target)
    {
        target.WorkspaceId = source.WorkspaceId;
        target.Id = source.Id;
        target.Title = source.Title;
        target.Section = source.Section;
        target.Priority = source.Priority;
        target.Done = source.Done;
        target.Estimate = source.Estimate;
        target.Note = source.Note;
        target.ListItems = source.ListItems
            .Select(r => new TodoItemListItemEntity
            {
                WorkspaceId = r.WorkspaceId,
                TodoId = r.TodoId,
                ListType = r.ListType,
                Ordinal = r.Ordinal,
                Value = r.Value,
            })
            .ToList();
        target.ImplementationTaskRows = source.ImplementationTaskRows
            .Select(r => new TodoItemTaskEntity
            {
                WorkspaceId = r.WorkspaceId,
                TodoId = r.TodoId,
                Ordinal = r.Ordinal,
                Task = r.Task,
                Done = r.Done,
            })
            .ToList();
        target.CompletedDate = source.CompletedDate;
        target.DoneSummary = source.DoneSummary;
        target.Remaining = source.Remaining;
        target.PriorityNote = source.PriorityNote;
        target.Reference = source.Reference;
        target.ItemKind = source.ItemKind;
        target.SectionOrder = source.SectionOrder;
        target.ItemOrder = source.ItemOrder;
        target.PhaseLabel = source.PhaseLabel;
    }

    private static TodoDocumentMetadataEntity CloneDocumentMetadata(TodoDocumentMetadataEntity source)
    {
        var target = new TodoDocumentMetadataEntity
        {
            WorkspaceId = source.WorkspaceId,
            SingletonId = source.SingletonId,
        };
        CopyDocumentMetadata(source, target);
        return target;
    }

    private static void CopyDocumentMetadata(TodoDocumentMetadataEntity source, TodoDocumentMetadataEntity target)
    {
        target.WorkspaceId = source.WorkspaceId;
        target.SingletonId = source.SingletonId;
        target.Notes = source.Notes
            .Select(n => new TodoDocumentNoteEntity
            {
                WorkspaceId = n.WorkspaceId,
                SingletonId = n.SingletonId,
                Ordinal = n.Ordinal,
                Value = n.Value,
            })
            .ToList();
        target.CompletedGroups = source.CompletedGroups
            .Select(g => new TodoCompletedGroupEntity
            {
                WorkspaceId = g.WorkspaceId,
                SingletonId = g.SingletonId,
                Ordinal = g.Ordinal,
                Date = g.Date,
                Items = g.Items
                    .Select(i => new TodoCompletedItemEntity
                    {
                        WorkspaceId = i.WorkspaceId,
                        Ordinal = i.Ordinal,
                        ItemId = i.ItemId,
                        Qualifier = i.Qualifier,
                        Summary = i.Summary,
                    })
                    .ToList(),
            })
            .ToList();
        target.CodeReviewReference = source.CodeReviewReference;
        target.LastImportedFromYamlUtc = source.LastImportedFromYamlUtc;
        target.LastProjectedToYamlUtc = source.LastProjectedToYamlUtc;
        target.LastProjectionFailureUtc = source.LastProjectionFailureUtc;
        target.LastProjectionFailureMessage = source.LastProjectionFailureMessage;
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

    private string ResolveTodoPath()
    {
        var repoRoot = _ingestionOptions.Value.RepoRoot ?? ".";
        var todoRel = string.IsNullOrWhiteSpace(_ingestionOptions.Value.TodoFilePath)
            ? DefaultTodoRelativePath
            : _ingestionOptions.Value.TodoFilePath;
        return Path.GetFullPath(Path.IsPathRooted(todoRel) ? todoRel : Path.Combine(repoRoot, todoRel));
    }

    private DbScope CreateScope()
    {
        var scope = _scopeFactory.CreateAsyncScope();
        // TR-MCP-TODO-008: EfTodoService is a singleton but McpDbContext applies a
        // per-workspace global query filter fed by the scoped WorkspaceContext.
        // The new scope we just created is detached from the HTTP request scope,
        // so we must copy the request-scope's workspace identity onto the new
        // scope's DbContext before any query runs. When called outside an HTTP
        // request (STDIO/hosted services/tests) we leave the fresh scope's
        // WorkspaceContext as-is.
        var requestCtx = _httpContextAccessor?.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        var workspacePath = requestCtx is not null && !string.IsNullOrEmpty(requestCtx.WorkspacePath)
            ? requestCtx.WorkspacePath
            : _fixedWorkspacePath;
        if (!string.IsNullOrEmpty(workspacePath))
        {
            var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            ctx.OverrideWorkspaceId(workspacePath);
            return new DbScope(scope, ctx);
        }
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

        public DbScope(AsyncServiceScope scope, McpDbContext context)
        {
            _scope = scope;
            Context = context;
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
            IdempotencyKey = e.IdempotencyKey,
        };

    /// <summary>Builds the 4NF string-list child rows (explicit composite keys, fresh ordinals) for a TODO.</summary>
    private static List<TodoItemListItemEntity> BuildListItems(
        string workspaceId,
        string todoId,
        IReadOnlyList<string>? description,
        IReadOnlyList<string>? technicalDetails,
        IReadOnlyList<string>? dependsOn,
        IReadOnlyList<string>? functionalRequirements,
        IReadOnlyList<string>? technicalRequirements)
    {
        var rows = new List<TodoItemListItemEntity>();
        AddListItems(rows, workspaceId, todoId, DescriptionListType, description);
        AddListItems(rows, workspaceId, todoId, TechnicalDetailListType, technicalDetails);
        AddListItems(rows, workspaceId, todoId, DependsOnListType, dependsOn);
        AddListItems(rows, workspaceId, todoId, FunctionalRequirementListType, functionalRequirements);
        AddListItems(rows, workspaceId, todoId, TechnicalRequirementListType, technicalRequirements);
        return rows;
    }

    private static void AddListItems(List<TodoItemListItemEntity> rows, string workspaceId, string todoId, string listType, IReadOnlyList<string>? values)
    {
        if (values is null)
            return;
        for (var i = 0; i < values.Count; i++)
        {
            rows.Add(new TodoItemListItemEntity
            {
                WorkspaceId = workspaceId,
                TodoId = todoId,
                ListType = listType,
                Ordinal = i,
                Value = values[i],
            });
        }
    }

    /// <summary>Builds the 4NF implementation sub-task child rows (explicit composite keys, fresh ordinals).</summary>
    private static List<TodoItemTaskEntity> BuildTaskRows(string workspaceId, string todoId, IReadOnlyList<TodoFlatTask>? tasks)
    {
        var rows = new List<TodoItemTaskEntity>();
        if (tasks is null)
            return rows;
        for (var i = 0; i < tasks.Count; i++)
        {
            rows.Add(new TodoItemTaskEntity
            {
                WorkspaceId = workspaceId,
                TodoId = todoId,
                Ordinal = i,
                Task = tasks[i].Task ?? string.Empty,
                Done = tasks[i].Done,
            });
        }

        return rows;
    }

    /// <summary>
    /// Loads and attaches the 4NF child rows onto the (non-mapped) holders of each TODO so
    /// <see cref="ToFlatItem"/>, projection, and clone paths can read them. Children are written
    /// from the dependent side; see <see cref="TodoItemEntity.ListItems"/> for the rationale.
    /// </summary>
    private static async Task AttachTodoChildrenAsync(McpDbContext ctx, IReadOnlyCollection<TodoItemEntity> items, CancellationToken ct)
    {
        if (items.Count == 0)
            return;
        var ids = items.Select(i => i.Id).Distinct(StringComparer.Ordinal).ToList();
        var listRows = await ctx.TodoItemListItems
            .AsNoTracking()
            .Where(r => ids.Contains(r.TodoId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var taskRows = await ctx.TodoItemTasks
            .AsNoTracking()
            .Where(r => ids.Contains(r.TodoId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var listLookup = listRows.ToLookup(r => (r.WorkspaceId, r.TodoId));
        var taskLookup = taskRows.ToLookup(r => (r.WorkspaceId, r.TodoId));
        foreach (var item in items)
        {
            item.ListItems = listLookup[(item.WorkspaceId, item.Id)].OrderBy(r => r.Ordinal).ToList();
            item.ImplementationTaskRows = taskLookup[(item.WorkspaceId, item.Id)].OrderBy(r => r.Ordinal).ToList();
        }
    }

    /// <summary>
    /// Replaces a TODO's 4NF child rows (dependent-side write): removes the tracked existing rows
    /// and re-adds the supplied sets, keeping the entity holders in sync for flat mapping.
    /// </summary>
    private static async Task ReplaceTodoChildrenAsync(
        McpDbContext ctx,
        TodoItemEntity entity,
        List<TodoItemListItemEntity> listItems,
        List<TodoItemTaskEntity> taskRows,
        CancellationToken ct)
    {
        var existingLists = await ctx.TodoItemListItems
            .Where(r => r.WorkspaceId == entity.WorkspaceId && r.TodoId == entity.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existingTasks = await ctx.TodoItemTasks
            .Where(r => r.WorkspaceId == entity.WorkspaceId && r.TodoId == entity.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        ctx.TodoItemListItems.RemoveRange(existingLists);
        ctx.TodoItemTasks.RemoveRange(existingTasks);
        ctx.TodoItemListItems.AddRange(listItems);
        ctx.TodoItemTasks.AddRange(taskRows);
        entity.ListItems = listItems;
        entity.ImplementationTaskRows = taskRows;
    }

    private static List<string>? ListValues(TodoItemEntity e, string listType)
    {
        var values = e.ListItems
            .Where(r => string.Equals(r.ListType, listType, StringComparison.Ordinal))
            .OrderBy(r => r.Ordinal)
            .Select(r => r.Value)
            .ToList();
        return values.Count > 0 ? values : null;
    }

    private static List<TodoFlatTask>? TaskValues(TodoItemEntity e)
    {
        var values = e.ImplementationTaskRows
            .OrderBy(r => r.Ordinal)
            .Select(r => new TodoFlatTask(r.Task, r.Done))
            .ToList();
        return values.Count > 0 ? values : null;
    }

    private TodoFlatItem ToFlatItem(TodoItemEntity e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Section = e.Section,
        Priority = e.Priority,
        Done = e.Done,
        Estimate = e.Estimate,
        Note = e.Note,
        Description = ListValues(e, DescriptionListType),
        TechnicalDetails = ListValues(e, TechnicalDetailListType),
        ImplementationTasks = TaskValues(e),
        CompletedDate = e.CompletedDate,
        DoneSummary = e.DoneSummary,
        Remaining = e.Remaining,
        PriorityNote = e.PriorityNote,
        Reference = e.Reference,
        Phase = e.PhaseLabel,
        DependsOn = ListValues(e, DependsOnListType),
        FunctionalRequirements = ListValues(e, FunctionalRequirementListType),
        TechnicalRequirements = ListValues(e, TechnicalRequirementListType),
        IdempotencyKey = e.IdempotencyKey,
    };

    private static string? SerializeList(IReadOnlyList<string>? value)
        => value is null
            ? null
            : JsonSerializer.Serialize(value.ToList(), typeof(List<string>), McpServicesJsonContext.Default);

    private static string? SerializeTasks(IReadOnlyList<TodoFlatTask>? value)
        => value is null
            ? null
            : JsonSerializer.Serialize(value.ToList(), typeof(List<TodoFlatTask>), McpServicesJsonContext.Default);

    private static string? SerializeFlatItem(TodoFlatItem? item)
        => item is null
            ? null
            : JsonSerializer.Serialize(item, typeof(TodoFlatItem), McpServicesJsonContext.Default);

    private static List<string>? DeserializeList(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : (List<string>?)JsonSerializer.Deserialize(value, typeof(List<string>), McpServicesJsonContext.Default);

    private static List<TodoFlatTask>? DeserializeTasks(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : (List<TodoFlatTask>?)JsonSerializer.Deserialize(value, typeof(List<TodoFlatTask>), McpServicesJsonContext.Default);

    private static TodoFlatItem? DeserializeFlatItem(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : (TodoFlatItem?)JsonSerializer.Deserialize(value, typeof(TodoFlatItem), McpServicesJsonContext.Default);

    private static string NormalizeYaml(string yaml)
        => yaml.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private static bool IsProjectionException(Exception ex)
        => ex is IOException
            or UnauthorizedAccessException
            or JsonException
            or YamlException
            or InvalidOperationException
            or DbUpdateException;

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

    private sealed record EfTodoCompensationState(
        TodoItemEntity Item,
        SoftDeleteState SoftDelete,
        EfTodoDocumentMetadataState? DocumentMetadata);

    private sealed record EfTodoDocumentMetadataState(
        TodoDocumentMetadataEntity Metadata,
        SoftDeleteState SoftDelete);

    private readonly record struct SoftDeleteState(
        bool IsDeleted,
        DateTimeOffset? DeletedAtUtc,
        string? DeletedBy,
        string? DeleteReason);
}
