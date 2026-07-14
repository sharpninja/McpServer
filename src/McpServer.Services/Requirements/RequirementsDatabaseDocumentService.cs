using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>
/// Database-backed requirements service. FR/TR/TEST rows and traceability links
/// are stored in <see cref="McpDbContext"/> and scoped by the active workspace.
/// Markdown files are used only for bootstrap import and export rendering.
/// </summary>
public sealed class RequirementsDatabaseDocumentService : IRequirementsDocumentService, IRequirementsCompensation, IDisposable
{
    private const string FrKind = "fr";
    private const string TrKind = "tr";
    private const string TestKind = "test";

    private static readonly System.Text.RegularExpressions.Regex RequirementIdShapeRegex = new(
        @"^(FR|TR|TEST)-[A-Z0-9]+(-[A-Z0-9]+)*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex RequirementTokenRegex = new(
        @"^[A-Z0-9]+$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly string[] SoftDeleteQueryFilter = ["SoftDelete"];

    // TR-MCP-REQAC-001: acceptance criteria are stored as 4NF child rows
    // (RequirementAcceptanceCriterionEntity); these map between the child rows and the public model.

    /// <summary>Maps acceptance-criterion child rows to the public model, ordered by position.</summary>
    private static IReadOnlyList<AcceptanceCriterion> ToCriterionModels(IEnumerable<RequirementAcceptanceCriterionEntity> rows) =>
        rows.OrderBy(r => r.Ordinal)
            .Select(r => new AcceptanceCriterion
            {
                Id = r.CriterionId,
                Text = r.Text,
                IsSatisfied = r.IsSatisfied,
                Evidence = r.Evidence,
            })
            .ToList();

    /// <summary>Builds acceptance-criterion child rows (with fresh ordinals) from the public model.
    /// The composite parent-key columns are set explicitly (WorkspaceId is shared with the workspace
    /// foreign key, so EF relationship fixup cannot be relied on to populate them).</summary>
    private static List<RequirementAcceptanceCriterionEntity> ToCriterionEntities(
        IReadOnlyList<AcceptanceCriterion>? criteria,
        string workspaceId,
        string requirementKind,
        string requirementId)
    {
        var rows = new List<RequirementAcceptanceCriterionEntity>();
        if (criteria is null)
            return rows;
        for (var i = 0; i < criteria.Count; i++)
        {
            var c = criteria[i];
            rows.Add(new RequirementAcceptanceCriterionEntity
            {
                WorkspaceId = workspaceId,
                RequirementKind = requirementKind,
                RequirementId = requirementId,
                Ordinal = i,
                CriterionId = c.Id,
                Text = c.Text,
                IsSatisfied = c.IsSatisfied,
                Evidence = c.Evidence,
            });
        }

        return rows;
    }

    /// <summary>Replaces a requirement's acceptance-criteria child rows. Writes from the dependent
    /// side (explicit foreign-key columns via the DbSet) rather than a principal navigation, because
    /// the composite (WorkspaceId, RequirementKind, RequirementId) key shares the tenant column with
    /// the workspace foreign key and principal-side collection fixup nulls a key column on insert.</summary>
    private static async Task SetCriteriaAsync(McpDbContext ctx, RequirementEntity row, IReadOnlyList<AcceptanceCriterion>? criteria, CancellationToken ct)
    {
        var existing = await ctx.RequirementAcceptanceCriteria
            .Where(c => c.WorkspaceId == row.WorkspaceId && c.RequirementKind == row.Kind && c.RequirementId == row.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        ctx.RequirementAcceptanceCriteria.RemoveRange(existing);
        ctx.RequirementAcceptanceCriteria.AddRange(ToCriterionEntities(criteria, row.WorkspaceId, row.Kind, row.Id));
    }

    /// <summary>Loads and attaches acceptance-criterion child rows onto the (non-mapped) AcceptanceCriteria
    /// holder of each requirement, so <c>MapFr/MapTr/MapTest</c> and clone paths can read them.</summary>
    private static async Task AttachCriteriaAsync(McpDbContext ctx, IReadOnlyCollection<RequirementEntity> rows, CancellationToken ct)
    {
        if (rows.Count == 0)
            return;
        var ids = rows.Select(r => r.Id).Distinct(StringComparer.Ordinal).ToList();
        var criteria = await ctx.RequirementAcceptanceCriteria
            .AsNoTracking()
            .Where(c => ids.Contains(c.RequirementId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var lookup = criteria.ToLookup(c => (c.WorkspaceId, c.RequirementKind, c.RequirementId));
        foreach (var row in rows)
        {
            row.AcceptanceCriteria = lookup[(row.WorkspaceId, row.Kind, row.Id)]
                .OrderBy(c => c.Ordinal)
                .ToList();
        }
    }

    /// <summary>Deep-clones acceptance-criterion child rows (preserving their composite parent-key
    /// columns) for a cloned/copied requirement that keeps the same identity.</summary>
    private static List<RequirementAcceptanceCriterionEntity> CloneCriteria(IEnumerable<RequirementAcceptanceCriterionEntity> source) =>
        source.OrderBy(r => r.Ordinal)
            .Select(r => new RequirementAcceptanceCriterionEntity
            {
                WorkspaceId = r.WorkspaceId,
                RequirementKind = r.RequirementKind,
                RequirementId = r.RequirementId,
                Ordinal = r.Ordinal,
                CriterionId = r.CriterionId,
                Text = r.Text,
                IsSatisfied = r.IsSatisfied,
                Evidence = r.Evidence,
            })
            .ToList();

    /// <summary>Maps a stored requirement row to an <see cref="FrEntry"/> including acceptance criteria.</summary>
    private static FrEntry MapFr(RequirementEntity x) =>
        new(x.Id, x.Title, x.Body, x.WorkspaceId, x.Priority, x.Status, x.Notes, ToCriterionModels(x.AcceptanceCriteria), x.ScopeStartLayerKey, x.ScopeEndLayerKey);

    /// <summary>Maps a stored requirement row to a <see cref="TrEntry"/> including acceptance criteria.</summary>
    private static TrEntry MapTr(RequirementEntity x) =>
        new(x.Id, x.Title, x.Body, x.WorkspaceId, x.Priority, x.Status, x.Notes, ToCriterionModels(x.AcceptanceCriteria), x.ScopeStartLayerKey, x.ScopeEndLayerKey);

    /// <summary>Maps a stored requirement row to a <see cref="TestEntry"/> including acceptance criteria.</summary>
    private static TestEntry MapTest(RequirementEntity x) =>
        new(x.Id, x.Body, x.WorkspaceId, x.Title, x.Priority, x.Status, x.Notes, ToCriterionModels(x.AcceptanceCriteria), x.ScopeStartLayerKey, x.ScopeEndLayerKey);

    /// <summary>Maps a stored requirement scope layer row to the public model.</summary>
    private static RequirementScopeLayerEntry MapLayer(RequirementScopeLayerEntity x) =>
        new(x.Key, x.Order, x.Name, x.Description, x.ScopeEndLayerKey, x.WorkspaceId, x.CreatedAtUtc, x.UpdatedAtUtc);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RequirementsOptions _options;
    private readonly ILogger<RequirementsDatabaseDocumentService> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IChangeEventBus? _eventBus;
    private readonly IRequirementsWikiExportOrchestrator _wikiExportOrchestrator;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Initializes a new DB-backed requirements service.</summary>
    public RequirementsDatabaseDocumentService(
        IServiceScopeFactory scopeFactory,
        IOptions<RequirementsOptions> options,
        ILogger<RequirementsDatabaseDocumentService> logger,
        IHttpContextAccessor? httpContextAccessor = null,
        IChangeEventBus? eventBus = null,
        IRequirementsWikiExportOrchestrator? wikiExportOrchestrator = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor;
        _eventBus = eventBus;
        _wikiExportOrchestrator = wikiExportOrchestrator
            ?? new RequirementsWikiExportOrchestrator(new DisabledRequirementsDocFxWorkflowRunner());
    }

    /// <inheritdoc />
    public void Dispose() => _writeLock.Dispose();

    /// <inheritdoc />
    public async Task<IReadOnlyList<RequirementScopeLayerEntry>> GetRequirementLayersAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        var ctx = scope.Context;
        await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
        var rows = await ctx.RequirementScopeLayers
            .AsNoTracking()
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.Select(MapLayer).ToArray();
    }

    /// <inheritdoc />
    public async Task<RequirementScopeLayerEntry> CreateRequirementLayerAsync(RequirementScopeLayerEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var workspaceId = RequireWorkspaceId(ctx);
            var normalized = NormalizeLayerEntry(entry, workspaceId);

            if (await ctx.RequirementScopeLayers.AnyAsync(x => x.Key == normalized.Key, ct).ConfigureAwait(false))
                throw new RequirementsConflictException($"Requirement scope layer '{normalized.Key}' already exists.");
            if (await ctx.RequirementScopeLayers.AnyAsync(x => x.Order == normalized.Order, ct).ConfigureAwait(false))
                throw new RequirementsConflictException($"Requirement scope layer order '{normalized.Order}' already exists.");

            await ValidateLayerSunsetAsync(ctx, normalized.Key, normalized.Order, normalized.ScopeEndLayerKey, allowSelf: true, ct).ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow;
            var row = new RequirementScopeLayerEntity
            {
                WorkspaceId = workspaceId,
                Key = normalized.Key,
                Order = normalized.Order,
                Name = normalized.Name,
                Description = normalized.Description,
                ScopeEndLayerKey = NormalizeOptionalLayerKey(normalized.ScopeEndLayerKey),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            ctx.RequirementScopeLayers.Add(row);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            return MapLayer(row);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RequirementScopeLayerEntry> UpdateRequirementLayerAsync(RequirementScopeLayerUpdateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var key = NormalizeLayerKey(request.Key);
            var row = await ctx.RequirementScopeLayers.FirstOrDefaultAsync(x => x.Key == key, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"Requirement scope layer '{key}' was not found.");

            if (request.Order.HasValue && request.Order.Value != row.Order)
                throw new InvalidOperationException("Requirement scope layer order is immutable.");

            if (!string.IsNullOrWhiteSpace(request.Name))
                row.Name = request.Name.Trim();
            if (request.Description is not null)
                row.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            if (request.ScopeEndLayerKey is not null)
            {
                var scopeEnd = NormalizeOptionalLayerKey(request.ScopeEndLayerKey);
                await ValidateLayerSunsetAsync(ctx, row.Key, row.Order, scopeEnd, allowSelf: true, ct).ConfigureAwait(false);
                row.ScopeEndLayerKey = scopeEnd;
            }

            row.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            return MapLayer(row);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RequirementScopeLayerEntry> GetWorkspaceCurrentRequirementLayerAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        var ctx = scope.Context;
        await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
        var workspace = await EnsureWorkspaceRowAsync(ctx, RequireWorkspaceId(ctx), ct).ConfigureAwait(false);
        var layer = await FindLayerAsync(ctx, workspace.CurrentRequirementLayerKey, ct).ConfigureAwait(false);
        return MapLayer(layer);
    }

    /// <inheritdoc />
    public async Task<RequirementScopeLayerEntry> SetWorkspaceCurrentRequirementLayerAsync(string layerKey, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var normalizedKey = NormalizeLayerKey(layerKey);
            var layer = await FindLayerAsync(ctx, normalizedKey, ct).ConfigureAwait(false);
            var workspace = await EnsureWorkspaceRowAsync(ctx, RequireWorkspaceId(ctx), ct).ConfigureAwait(false);
            workspace.CurrentRequirementLayerKey = normalizedKey;
            workspace.DateTimeModified = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            return MapLayer(layer);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<EffectiveRequirementsResult> GetEffectiveRequirementsAsync(string? layerKey = null, CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        var ctx = scope.Context;
        await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
        var workspace = await EnsureWorkspaceRowAsync(ctx, RequireWorkspaceId(ctx), ct).ConfigureAwait(false);
        var resolvedKey = string.IsNullOrWhiteSpace(layerKey) ? workspace.CurrentRequirementLayerKey : NormalizeLayerKey(layerKey);
        var currentLayer = await FindLayerAsync(ctx, resolvedKey, ct).ConfigureAwait(false);
        var layers = await ctx.RequirementScopeLayers.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
        var layerOrders = layers.ToDictionary(x => x.Key, x => x.Order, StringComparer.OrdinalIgnoreCase);
        var layerEndOrders = layers.ToDictionary(
            x => x.Key,
            x => string.IsNullOrWhiteSpace(x.ScopeEndLayerKey) ? (int?)null : layerOrders[x.ScopeEndLayerKey!],
            StringComparer.OrdinalIgnoreCase);
        var rows = await ctx.Requirements
            .AsNoTracking()
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        await AttachCriteriaAsync(ctx, rows, ct).ConfigureAwait(false);

        var effectiveRows = rows
            .Where(row => IsRequirementEffective(row, currentLayer.Order, layerOrders, layerEndOrders))
            .ToList();
        var effectiveKeys = effectiveRows
            .Select(row => $"{row.Kind}\0{row.Id}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var links = await ctx.RequirementTraceabilityLinks
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var effectiveLinks = links
            .Where(link => effectiveKeys.Contains($"{FrKind}\0{link.FrId}") && effectiveKeys.Contains($"{link.TargetKind}\0{link.TargetId}"))
            .GroupBy(link => link.FrId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FrTrMapping(
                group.Key,
                group.Where(link => link.TargetKind == TrKind).Select(link => link.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.Where(link => link.TargetKind == TestKind).Select(link => link.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                RequireWorkspaceId(ctx)))
            .ToArray();

        return new EffectiveRequirementsResult(
            MapLayer(currentLayer),
            effectiveRows.Where(x => x.Kind == FrKind).Select(MapFr).ToArray(),
            effectiveRows.Where(x => x.Kind == TrKind).Select(MapTr).ToArray(),
            effectiveRows.Where(x => x.Kind == TestKind).Select(MapTest).ToArray(),
            effectiveLinks);
    }

    /// <inheritdoc />
    public async Task<RequirementsCompensationSnapshot> CaptureRequirementsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, cancellationToken).ConfigureAwait(false);
            var workspaceId = RequireWorkspaceId(ctx);
            var requirements = await ctx.Requirements
                .IgnoreQueryFilters(SoftDeleteQueryFilter)
                .Where(row => row.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            await AttachCriteriaAsync(ctx, requirements, cancellationToken).ConfigureAwait(false);
            var links = await ctx.RequirementTraceabilityLinks
                .IgnoreQueryFilters(SoftDeleteQueryFilter)
                .Where(row => row.WorkspaceId == workspaceId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var state = new RequirementsDatabaseCompensationState(
                workspaceId,
                requirements
                    .Select(row => new RequirementEntityCompensationState(CloneRequirement(row), ReadSoftDeleteState(ctx.Entry(row))))
                    .ToArray(),
                links
                    .Select(row => new RequirementTraceabilityLinkCompensationState(CloneTraceabilityLink(row), ReadSoftDeleteState(ctx.Entry(row))))
                    .ToArray());

            return new RequirementsCompensationSnapshot(
                requirements.Where(row => !IsSoftDeleted(ctx, row) && row.Kind == FrKind).Select(MapFr).ToArray(),
                requirements.Where(row => !IsSoftDeleted(ctx, row) && row.Kind == TrKind).Select(MapTr).ToArray(),
                requirements.Where(row => !IsSoftDeleted(ctx, row) && row.Kind == TestKind).Select(MapTest).ToArray(),
                BuildVisibleMappings(ctx, links),
                Provider: nameof(RequirementsDatabaseDocumentService),
                State: state);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RestoreRequirementsSnapshotAsync(
        RequirementsCompensationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.State is not RequirementsDatabaseCompensationState state)
            throw new InvalidOperationException($"Requirements compensation snapshot provider '{snapshot.Provider}' is not supported by {nameof(RequirementsDatabaseDocumentService)}.");

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, cancellationToken).ConfigureAwait(false);
            await RestoreRequirementsCoreAsync(ctx, state, cancellationToken).ConfigureAwait(false);
            await RestoreTraceabilityLinksCoreAsync(ctx, state, cancellationToken).ConfigureAwait(false);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FrEntry>> GetAllFrAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var rows = await scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == FrKind)
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        await AttachCriteriaAsync(scope.Context, rows, ct).ConfigureAwait(false);
        return rows.Select(MapFr).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FrEntry>> QueryFrAsync(string? area = null, string? status = null, CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var query = scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == FrKind);
        query = ApplyAreaFilter(query, "FR", area);
        query = ApplyStatusFilter(query, status);
        var rows = await query
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        await AttachCriteriaAsync(scope.Context, rows, ct).ConfigureAwait(false);
        return rows.Select(MapFr).ToArray();
    }

    /// <inheritdoc />
    public async Task<int> PurgeInvalidPlaceholdersAsync(CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);

            // Fetch translatable part first (Body startsWith is supported), then apply non-translatable regex + null check in memory to avoid EF translation failure.
            var candidates = await scope.Context.Requirements
                .Where(x => x.Kind == FrKind &&
                            x.Body != null &&
                            x.Body.StartsWith("Placeholder requirement backfilled"))
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var toDelete = candidates
                .Where(x => string.IsNullOrEmpty(x.Id) || !IsValidRequirementId(x.Id, "FR"))
                .ToList();

            if (toDelete.Count > 0)
            {
                var badFrIds = toDelete.Select(x => x.Id).ToList();
                // Load links using the (workspace filtered) ctx and remove to avoid FK on req delete. Use per-id to ensure tracked entities.
                foreach (var id in badFrIds.Where(i => !string.IsNullOrEmpty(i)))
                {
                    var linksForId = await scope.Context.RequirementTraceabilityLinks
                        .IgnoreQueryFilters()
                        .Where(l => l.FrId == id)
                        .ToListAsync(ct)
                        .ConfigureAwait(false);
                    if (linksForId.Count > 0)
                        scope.Context.RequirementTraceabilityLinks.RemoveRange(linksForId);
                }
                scope.Context.Requirements.RemoveRange(toDelete);
                await scope.Context.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            return toDelete.Count;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<FrEntry?> GetFrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var row = await FindRequirementAsync(scope.Context, FrKind, id, asTracking: false, ct).ConfigureAwait(false);
        return row is null ? null : MapFr(row);
    }

    /// <inheritdoc />
    public async Task AddFrAsync(FrEntry entry, CancellationToken ct = default)
    {
        ValidateFr(entry);
        await AddRequirementAsync(FrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default)
    {
        ValidateFr(entry);
        await UpdateRequirementAsync(FrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteFrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var row = await FindRequirementAsync(ctx, FrKind, id, asTracking: true, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"FR '{id}' was not found.");
            ctx.Requirements.Remove(row);
            ctx.RequirementTraceabilityLinks.RemoveRange(ctx.RequirementTraceabilityLinks.Where(x => x.FrId == id));
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrEntry>> GetAllTrAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var rows = await scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == TrKind)
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        await AttachCriteriaAsync(scope.Context, rows, ct).ConfigureAwait(false);
        return rows.Select(MapTr).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrEntry>> QueryTrAsync(string? area = null, string? subarea = null, string? status = null, CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var query = scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == TrKind);
        query = ApplyAreaFilter(query, "TR", area);
        query = ApplyTrSubareaFilter(query, subarea);
        query = ApplyStatusFilter(query, status);
        var rows = await query
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        await AttachCriteriaAsync(scope.Context, rows, ct).ConfigureAwait(false);
        return rows.Select(MapTr).ToArray();
    }

    /// <inheritdoc />
    public async Task<TrEntry?> GetTrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var row = await FindRequirementAsync(scope.Context, TrKind, id, asTracking: false, ct).ConfigureAwait(false);
        return row is null ? null : MapTr(row);
    }

    /// <inheritdoc />
    public async Task AddTrAsync(TrEntry entry, CancellationToken ct = default)
    {
        ValidateTr(entry);
        await AddRequirementAsync(TrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateTrAsync(TrEntry entry, CancellationToken ct = default)
    {
        ValidateTr(entry);
        await UpdateRequirementAsync(TrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteTrAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await DeleteRequirementAndTargetLinksAsync(TrKind, id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TestEntry>> GetAllTestAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var rows = await scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == TestKind)
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        await AttachCriteriaAsync(scope.Context, rows, ct).ConfigureAwait(false);
        return rows.Select(MapTest).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TestEntry>> QueryTestAsync(string? area = null, string? status = null, CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var query = scope.Context.Requirements
            .AsNoTracking()
            .Where(x => x.Kind == TestKind);
        query = ApplyAreaFilter(query, "TEST", area);
        query = ApplyStatusFilter(query, status);
        var rows = await query
            .OrderBy(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        await AttachCriteriaAsync(scope.Context, rows, ct).ConfigureAwait(false);
        return rows.Select(MapTest).ToArray();
    }

    /// <inheritdoc />
    public async Task<TestEntry?> GetTestAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var row = await FindRequirementAsync(scope.Context, TestKind, id, asTracking: false, ct).ConfigureAwait(false);
        return row is null ? null : MapTest(row);
    }

    /// <inheritdoc />
    public async Task AddTestAsync(TestEntry entry, CancellationToken ct = default)
    {
        ValidateTest(entry);
        await AddRequirementAsync(TestKind, entry.Id, entry.Title, entry.Condition, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default)
    {
        ValidateTest(entry);
        await UpdateRequirementAsync(TestKind, entry.Id, entry.Title, entry.Condition, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteTestAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id, nameof(id));
        await DeleteRequirementAndTargetLinksAsync(TestKind, id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchEntries> AddBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ValidateBatchEntries(entries);
        ValidateBatchUniqueIds(entries);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            await using var transaction = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var workspaceId = RequireWorkspaceId(ctx);
            var now = Now();
            var toInsert = new List<RequirementBatchValue>();
            foreach (var value in EnumerateBatch(entries))
            {
                _ = await ValidateRequirementScopeAsync(ctx, value.ScopeStartLayerKey, value.ScopeEndLayerKey, ct).ConfigureAwait(false);
                bool exists = await ctx.Requirements.AnyAsync(x => x.Kind == value.Kind && x.Id == value.Id, ct).ConfigureAwait(false);
                if (!exists)
                {
                    toInsert.Add(value);
                }
                // Idempotent create: pre-existing records are left as-is (mitigates double-submit races from clients/plugins that send the batch twice).
            }

            foreach (var value in toInsert)
            {
                var requirementScope = await ValidateRequirementScopeAsync(ctx, value.ScopeStartLayerKey, value.ScopeEndLayerKey, ct).ConfigureAwait(false);
                var entity = new RequirementEntity
                {
                    WorkspaceId = workspaceId,
                    Kind = value.Kind,
                    Id = value.Id,
                    Title = value.Title,
                    Body = value.Body,
                    Priority = NormalizePriority(value.Priority),
                    Status = NormalizeStatus(value.Status),
                    Notes = value.Notes,
                    ScopeStartLayerKey = requirementScope.Start,
                    ScopeEndLayerKey = requirementScope.End,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                ctx.Requirements.Add(entity);
                ctx.RequirementAcceptanceCriteria.AddRange(
                    ToCriterionEntities(value.AcceptanceCriteria, workspaceId, value.Kind, value.Id));
            }

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);

            var result = NormalizeBatchResult(entries, workspaceId);
            await PublishBatchRequirementsChangeSafeAsync(ChangeEventActions.Created, result, ct).ConfigureAwait(false);
            return result;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RequirementsBatchEntries> UpdateBatchAsync(RequirementsBatchEntries entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ValidateBatchEntries(entries);
        ValidateBatchUniqueIds(entries);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            await using var transaction = await ctx.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var updates = new List<(RequirementEntity Row, RequirementBatchValue Value)>();
            foreach (var value in EnumerateBatch(entries))
            {
                var row = await FindRequirementAsync(ctx, value.Kind, value.Id, asTracking: true, ct).ConfigureAwait(false)
                    ?? throw new RequirementsNotFoundException($"{value.Kind.ToUpperInvariant()} '{value.Id}' was not found.");
                _ = await ValidateRequirementScopeAsync(ctx, value.ScopeStartLayerKey, value.ScopeEndLayerKey, ct).ConfigureAwait(false);
                updates.Add((row, value));
            }

            var now = Now();
            foreach (var (row, value) in updates)
            {
                var requirementScope = await ValidateRequirementScopeAsync(ctx, value.ScopeStartLayerKey, value.ScopeEndLayerKey, ct).ConfigureAwait(false);
                row.Title = value.Title;
                row.Body = value.Body;
                row.Priority = NormalizePriority(value.Priority);
                row.Status = NormalizeStatus(value.Status);
                row.Notes = value.Notes;
                await SetCriteriaAsync(ctx, row, value.AcceptanceCriteria, ct).ConfigureAwait(false);
                row.ScopeStartLayerKey = requirementScope.Start;
                row.ScopeEndLayerKey = requirementScope.End;
                row.UpdatedAtUtc = now;
            }

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);

            var result = NormalizeBatchResult(entries, RequireWorkspaceId(ctx));
            await PublishBatchRequirementsChangeSafeAsync(ChangeEventActions.Updated, result, ct).ConfigureAwait(false);
            return result;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FrTrMapping>> GetAllMappingsAsync(CancellationToken ct = default)
    {
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var links = await scope.Context.RequirementTraceabilityLinks
            .AsNoTracking()
            .OrderBy(x => x.FrId)
            .ThenBy(x => x.TargetKind)
            .ThenBy(x => x.TargetId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return links
            .GroupBy(x => x.FrId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FrTrMapping(
                group.Key,
                group.Where(x => x.TargetKind == TrKind).Select(x => x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.Where(x => x.TargetKind == TestKind).Select(x => x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.First().WorkspaceId))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<FrTrMapping?> GetMappingAsync(string frId, CancellationToken ct = default)
    {
        ValidateId(frId, nameof(frId));
        // Filter to the requested FR server-side instead of materializing every
        // traceability link in the workspace and scanning in memory.
        var target = frId.Trim().ToUpperInvariant();
        await using var scope = CreateScope();
        await EnsureBootstrappedAsync(scope.Context, ct).ConfigureAwait(false);
        var links = await scope.Context.RequirementTraceabilityLinks
            .AsNoTracking()
            .Where(x => x.FrId.ToUpper() == target)
            .OrderBy(x => x.TargetKind)
            .ThenBy(x => x.TargetId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (links.Count == 0)
        {
            return null;
        }

        return new FrTrMapping(
            links[0].FrId,
            links.Where(x => x.TargetKind == TrKind).Select(x => x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            links.Where(x => x.TargetKind == TestKind).Select(x => x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            links[0].WorkspaceId);
    }

    /// <inheritdoc />
    public async Task UpsertMappingAsync(FrTrMapping mapping, CancellationToken ct = default)
    {
        ValidateMapping(mapping);
        var normalizedTrIds = NormalizeIds(mapping.TrIds);
        var normalizedTestIds = NormalizeIds(mapping.TestIds);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            await ValidateMappingTargetsAsync(ctx, mapping.FrId, normalizedTrIds, normalizedTestIds, ct).ConfigureAwait(false);

            var workspaceId = RequireWorkspaceId(ctx);
            var existingLinks = await ctx.RequirementTraceabilityLinks
                .IgnoreQueryFilters(SoftDeleteQueryFilter)
                .Where(x => x.WorkspaceId == workspaceId && x.FrId == mapping.FrId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var timestamp = DateTimeOffset.UtcNow;
            var now = timestamp.ToString("O");
            var desiredKeys = normalizedTrIds
                .Select(trId => BuildTraceabilityLinkKey(TrKind, trId))
                .Concat(normalizedTestIds.Select(testId => BuildTraceabilityLinkKey(TestKind, testId)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var existingLink in existingLinks.Where(link => !desiredKeys.Contains(BuildTraceabilityLinkKey(link.TargetKind, link.TargetId))))
                MarkSoftDeleted(ctx, existingLink, timestamp, "requirements_mapping_replaced");

            foreach (var trId in normalizedTrIds)
                UpsertTraceabilityLink(ctx, existingLinks, workspaceId, mapping.FrId, TrKind, trId, now);
            foreach (var testId in normalizedTestIds)
                UpsertTraceabilityLink(ctx, existingLinks, workspaceId, mapping.FrId, TestKind, testId, now);

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, mapping.FrId, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static void UpsertTraceabilityLink(
        McpDbContext ctx,
        IReadOnlyList<RequirementTraceabilityLinkEntity> existingLinks,
        string workspaceId,
        string frId,
        string targetKind,
        string targetId,
        string createdAtUtc)
    {
        var existing = existingLinks.FirstOrDefault(link =>
            string.Equals(link.TargetKind, targetKind, StringComparison.OrdinalIgnoreCase)
            && string.Equals(link.TargetId, targetId, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity
            {
                WorkspaceId = workspaceId,
                FrId = frId,
                TargetKind = targetKind,
                TargetId = targetId,
                CreatedAtUtc = createdAtUtc
            });
            return;
        }

        existing.SourceKind = FrKind;
        if (IsSoftDeleted(ctx, existing))
        {
            existing.CreatedAtUtc = createdAtUtc;
            ClearSoftDelete(ctx, existing);
        }
    }

    private static string BuildTraceabilityLinkKey(string targetKind, string targetId)
        => $"{targetKind}\0{targetId}";

    /// <inheritdoc />
    public async Task DeleteMappingAsync(string frId, CancellationToken ct = default)
    {
        ValidateId(frId, nameof(frId));
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var links = await ctx.RequirementTraceabilityLinks.Where(x => x.FrId == frId).ToListAsync(ct).ConfigureAwait(false);
            if (links.Count == 0)
                throw new RequirementsNotFoundException($"Mapping row '{frId}' was not found.");
            ctx.RequirementTraceabilityLinks.RemoveRange(links);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, frId, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<(string Content, string MimeType)> GenerateDocumentAsync(RequirementsDocType docType, CancellationToken ct = default)
    {
        return docType switch
        {
            RequirementsDocType.Functional => (RequirementsDocumentRenderer.RenderFunctional(await GetAllFrAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Technical => (RequirementsDocumentRenderer.RenderTechnical(await GetAllTrAsync(ct).ConfigureAwait(false), await GetAllMappingsAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Testing => (RequirementsDocumentRenderer.RenderTesting(await GetAllTestAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Mapping => (RequirementsDocumentRenderer.RenderMapping(await GetAllMappingsAsync(ct).ConfigureAwait(false)), "text/markdown"),
            RequirementsDocType.Matrix => (RequirementsDocumentRenderer.RenderMatrix(
                await GetAllFrAsync(ct).ConfigureAwait(false),
                await GetAllTrAsync(ct).ConfigureAwait(false),
                await GetAllTestAsync(ct).ConfigureAwait(false),
                ReadExistingMatrixForExport(null)), "text/markdown"),
            RequirementsDocType.All => throw new ArgumentOutOfRangeException(nameof(docType), "Use GenerateAllAsync for docType=All."),
            _ => throw new ArgumentOutOfRangeException(nameof(docType), docType, "Unknown requirements document type.")
        };
    }

    /// <inheritdoc />
    public async Task<RequirementsDocumentExportResult> GenerateAllAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
    {
        var fr = await GetAllFrAsync(ct).ConfigureAwait(false);
        var tr = await GetAllTrAsync(ct).ConfigureAwait(false);
        var test = await GetAllTestAsync(ct).ConfigureAwait(false);
        var mapping = await GetAllMappingsAsync(ct).ConfigureAwait(false);

        var generated = (generatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var documents = RequirementsWikiDocumentRenderer.RenderCanonicalFiles(fr, tr, test, mapping, ReadExistingMatrixForExport(outputRootPath));
        return await RequirementsDocumentExportWriter.WriteAsync(
            outputRootPath,
            "markdown",
            "all",
            generated,
            documents,
            ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RequirementsDocumentExportResult> GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default)
    {
        var fr = await GetAllFrAsync(ct).ConfigureAwait(false);
        var tr = await GetAllTrAsync(ct).ConfigureAwait(false);
        var test = await GetAllTestAsync(ct).ConfigureAwait(false);
        var mapping = await GetAllMappingsAsync(ct).ConfigureAwait(false);

        var generated = (generatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var request = new RequirementsWikiExportRequest(
            outputRootPath,
            generated,
            TryGetRequestWorkspacePath(),
            _options,
            fr,
            tr,
            test,
            mapping,
            ReadExistingMatrixForWikiExport(outputRootPath));
        return await _wikiExportOrchestrator.ExportAsync(request, ct).ConfigureAwait(false);
    }

    private async Task AddRequirementAsync(
        string kind,
        string id,
        string title,
        string body,
        string priority,
        string status,
        string? notes,
        IReadOnlyList<AcceptanceCriterion>? acceptanceCriteria,
        string? scopeStartLayerKey,
        string? scopeEndLayerKey,
        CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var requirementScope = await ValidateRequirementScopeAsync(ctx, scopeStartLayerKey, scopeEndLayerKey, ct).ConfigureAwait(false);
            var existing = await ctx.Requirements
                .IgnoreQueryFilters(SoftDeleteQueryFilter)
                .FirstOrDefaultAsync(x => x.Kind == kind && x.Id == id, ct)
                .ConfigureAwait(false);
            if (existing is not null && !IsSoftDeleted(ctx, existing))
                throw new RequirementsConflictException($"{kind.ToUpperInvariant()} '{id}' already exists.");

            var now = Now();
            if (existing is null)
            {
                ctx.Requirements.Add(new RequirementEntity
                {
                    WorkspaceId = RequireWorkspaceId(ctx),
                    Kind = kind,
                    Id = id,
                    Title = title,
                    Body = body,
                    Priority = NormalizePriority(priority),
                    Status = NormalizeStatus(status),
                    Notes = notes,
                    ScopeStartLayerKey = requirementScope.Start,
                    ScopeEndLayerKey = requirementScope.End,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
                ctx.RequirementAcceptanceCriteria.AddRange(
                    ToCriterionEntities(acceptanceCriteria, RequireWorkspaceId(ctx), kind, id));
            }
            else
            {
                existing.Title = title;
                existing.Body = body;
                existing.Priority = NormalizePriority(priority);
                existing.Status = NormalizeStatus(status);
                existing.Notes = notes;
                await SetCriteriaAsync(ctx, existing, acceptanceCriteria, ct).ConfigureAwait(false);
                existing.ScopeStartLayerKey = requirementScope.Start;
                existing.ScopeEndLayerKey = requirementScope.End;
                existing.CreatedAtUtc = now;
                existing.UpdatedAtUtc = now;
                ClearSoftDelete(ctx, existing);
            }

            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Created, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task UpdateRequirementAsync(
        string kind,
        string id,
        string title,
        string body,
        string priority,
        string status,
        string? notes,
        IReadOnlyList<AcceptanceCriterion>? acceptanceCriteria,
        string? scopeStartLayerKey,
        string? scopeEndLayerKey,
        CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var requirementScope = await ValidateRequirementScopeAsync(ctx, scopeStartLayerKey, scopeEndLayerKey, ct).ConfigureAwait(false);
            var row = await FindRequirementAsync(ctx, kind, id, asTracking: true, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"{kind.ToUpperInvariant()} '{id}' was not found.");
            row.Title = title;
            row.Body = body;
            row.Priority = NormalizePriority(priority);
            row.Status = NormalizeStatus(status);
            row.Notes = notes;
            await SetCriteriaAsync(ctx, row, acceptanceCriteria, ct).ConfigureAwait(false);
            row.ScopeStartLayerKey = requirementScope.Start;
            row.ScopeEndLayerKey = requirementScope.End;
            row.UpdatedAtUtc = Now();
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Updated, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static IEnumerable<RequirementBatchValue> EnumerateBatch(RequirementsBatchEntries entries)
    {
        foreach (var entry in entries.Functional)
            yield return new RequirementBatchValue(FrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey);
        foreach (var entry in entries.Technical)
            yield return new RequirementBatchValue(TrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey);
        foreach (var entry in entries.Testing)
            yield return new RequirementBatchValue(TestKind, entry.Id, entry.Title, entry.Condition, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, entry.ScopeStartLayerKey, entry.ScopeEndLayerKey);
    }

    private static RequirementsBatchEntries NormalizeBatchResult(RequirementsBatchEntries entries, string workspaceId) =>
        new(
            entries.Functional
                .Select(entry => entry with
                {
                    WorkspaceId = workspaceId,
                    Priority = NormalizePriority(entry.Priority),
                    Status = NormalizeStatus(entry.Status),
                    ScopeStartLayerKey = string.IsNullOrWhiteSpace(entry.ScopeStartLayerKey) ? RequirementScopeLayerDefaults.DefaultLayerKey : entry.ScopeStartLayerKey.Trim(),
                    ScopeEndLayerKey = NormalizeOptionalLayerKey(entry.ScopeEndLayerKey)
                })
                .ToArray(),
            entries.Technical
                .Select(entry => entry with
                {
                    WorkspaceId = workspaceId,
                    Priority = NormalizePriority(entry.Priority),
                    Status = NormalizeStatus(entry.Status),
                    ScopeStartLayerKey = string.IsNullOrWhiteSpace(entry.ScopeStartLayerKey) ? RequirementScopeLayerDefaults.DefaultLayerKey : entry.ScopeStartLayerKey.Trim(),
                    ScopeEndLayerKey = NormalizeOptionalLayerKey(entry.ScopeEndLayerKey)
                })
                .ToArray(),
            entries.Testing
                .Select(entry => entry with
                {
                    WorkspaceId = workspaceId,
                    Priority = NormalizePriority(entry.Priority),
                    Status = NormalizeStatus(entry.Status),
                    ScopeStartLayerKey = string.IsNullOrWhiteSpace(entry.ScopeStartLayerKey) ? RequirementScopeLayerDefaults.DefaultLayerKey : entry.ScopeStartLayerKey.Trim(),
                    ScopeEndLayerKey = NormalizeOptionalLayerKey(entry.ScopeEndLayerKey)
                })
                .ToArray());

    private static void ValidateBatchEntries(RequirementsBatchEntries entries)
    {
        ArgumentNullException.ThrowIfNull(entries.Functional);
        ArgumentNullException.ThrowIfNull(entries.Technical);
        ArgumentNullException.ThrowIfNull(entries.Testing);

        foreach (var entry in entries.Functional)
            ValidateFr(entry);
        foreach (var entry in entries.Technical)
            ValidateTr(entry);
        foreach (var entry in entries.Testing)
            ValidateTest(entry);
    }

    private static void ValidateBatchUniqueIds(RequirementsBatchEntries entries)
    {
        ValidateUniqueIds(entries.Functional, static item => item.Id, "FR");
        ValidateUniqueIds(entries.Technical, static item => item.Id, "TR");
        ValidateUniqueIds(entries.Testing, static item => item.Id, "TEST");
    }

    private static void ValidateUniqueIds<T>(IReadOnlyList<T> items, Func<T, string> getId, string label)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var id = getId(item);
            if (!seen.Add(id.Trim()))
                throw new ArgumentException($"Duplicate {label} ID '{id}' in batch.", nameof(items));
        }
    }

    private async Task PublishBatchRequirementsChangeSafeAsync(string action, RequirementsBatchEntries entries, CancellationToken ct)
    {
        foreach (var entry in entries.Functional)
            await PublishRequirementsChangeSafeAsync(action, entry.Id, ct).ConfigureAwait(false);
        foreach (var entry in entries.Technical)
            await PublishRequirementsChangeSafeAsync(action, entry.Id, ct).ConfigureAwait(false);
        foreach (var entry in entries.Testing)
            await PublishRequirementsChangeSafeAsync(action, entry.Id, ct).ConfigureAwait(false);
    }

    private async Task DeleteRequirementAndTargetLinksAsync(string kind, string id, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var row = await FindRequirementAsync(ctx, kind, id, asTracking: true, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"{kind.ToUpperInvariant()} '{id}' was not found.");
            ctx.Requirements.Remove(row);
            ctx.RequirementTraceabilityLinks.RemoveRange(ctx.RequirementTraceabilityLinks.Where(x => x.TargetKind == kind && x.TargetId == id));
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            await PublishRequirementsChangeSafeAsync(ChangeEventActions.Deleted, id, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task ValidateMappingTargetsAsync(
        McpDbContext ctx,
        string frId,
        IReadOnlyList<string> trIds,
        IReadOnlyList<string> testIds,
        CancellationToken ct)
    {
        var workspaceId = RequireWorkspaceId(ctx);
        var requirements = ctx.Requirements.IgnoreQueryFilters().Where(x => x.WorkspaceId == workspaceId);

        if (!await requirements.AnyAsync(x => x.Kind == FrKind && x.Id == frId, ct).ConfigureAwait(false))
            throw new ArgumentException($"FR '{frId}' does not exist.", nameof(frId));

        foreach (var trId in trIds)
        {
            if (!await requirements.AnyAsync(x => x.Kind == TrKind && x.Id == trId, ct).ConfigureAwait(false))
                throw new ArgumentException($"TR '{trId}' does not exist.", nameof(trIds));
        }

        foreach (var testId in testIds)
        {
            if (!await requirements.AnyAsync(x => x.Kind == TestKind && x.Id == testId, ct).ConfigureAwait(false))
                throw new ArgumentException($"TEST '{testId}' does not exist.", nameof(testIds));
        }
    }

    private async Task<RequirementEntity?> FindRequirementAsync(McpDbContext ctx, string kind, string id, bool asTracking, CancellationToken ct)
    {
        var query = asTracking ? ctx.Requirements : ctx.Requirements.AsNoTracking();
        var row = await query.FirstOrDefaultAsync(x => x.Kind == kind && x.Id == id, ct).ConfigureAwait(false);
        if (row is not null)
            await AttachCriteriaAsync(ctx, [row], ct).ConfigureAwait(false);
        return row;
    }

    private async Task EnsureBootstrappedAsync(McpDbContext ctx, CancellationToken ct)
    {
        var workspaceId = RequireWorkspaceId(ctx);
        await EnsureWorkspaceRowAsync(ctx, workspaceId, ct).ConfigureAwait(false);
        await EnsureDefaultLayerAsync(ctx, workspaceId, ct).ConfigureAwait(false);

        if (await ctx.Requirements.AnyAsync(ct).ConfigureAwait(false))
            return;

        var paths = ResolveDocumentPaths(ctx.CurrentWorkspaceId);
        if (!File.Exists(paths.Functional) && !File.Exists(paths.Technical) && !File.Exists(paths.Testing) && !File.Exists(paths.Mapping))
            return;

        var now = Now();
        var staleLinks = await ctx.RequirementTraceabilityLinks.ToListAsync(ct).ConfigureAwait(false);
        if (staleLinks.Count > 0)
        {
            ctx.RequirementTraceabilityLinks.RemoveRange(staleLinks);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var importedRequirements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in RequirementsDocumentParser.ParseFunctional(ReadFileIfExists(paths.Functional)))
        {
            if (!importedRequirements.Add($"{FrKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = FrKind, Id = entry.Id, Title = entry.Title, Body = entry.Body, ScopeStartLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey, CreatedAtUtc = now, UpdatedAtUtc = now });
        }
        foreach (var entry in RequirementsDocumentParser.ParseTechnical(ReadFileIfExists(paths.Technical)))
        {
            if (!importedRequirements.Add($"{TrKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = TrKind, Id = entry.Id, Title = entry.Title, Body = entry.Body, ScopeStartLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey, CreatedAtUtc = now, UpdatedAtUtc = now });
        }
        foreach (var entry in RequirementsDocumentParser.ParseTesting(ReadFileIfExists(paths.Testing)))
        {
            if (!importedRequirements.Add($"{TestKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = TestKind, Id = entry.Id, Title = string.Empty, Body = entry.Condition, ScopeStartLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey, CreatedAtUtc = now, UpdatedAtUtc = now });
        }

        var importedLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in RequirementsDocumentParser.ParseMapping(ReadFileIfExists(paths.Mapping)))
        {
            if (!importedRequirements.Contains($"{FrKind}\0{mapping.FrId}"))
                continue;

            foreach (var trId in NormalizeIds(mapping.TrIds))
            {
                if (!importedRequirements.Contains($"{TrKind}\0{trId}") || !importedLinks.Add($"{mapping.FrId}\0{TrKind}\0{trId}"))
                    continue;
                ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity { WorkspaceId = workspaceId, FrId = mapping.FrId, TargetKind = TrKind, TargetId = trId, CreatedAtUtc = now });
            }

            foreach (var testId in NormalizeIds(mapping.TestIds))
            {
                if (!importedRequirements.Contains($"{TestKind}\0{testId}") || !importedLinks.Add($"{mapping.FrId}\0{TestKind}\0{testId}"))
                    continue;
                ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity { WorkspaceId = workspaceId, FrId = mapping.FrId, TargetKind = TestKind, TargetId = testId, CreatedAtUtc = now });
            }
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static async Task<WorkspaceEntity> EnsureWorkspaceRowAsync(McpDbContext ctx, string workspaceId, CancellationToken ct)
    {
        var workspace = await ctx.Workspaces.FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId, ct).ConfigureAwait(false);
        if (workspace is not null)
        {
            if (string.IsNullOrWhiteSpace(workspace.CurrentRequirementLayerKey))
                workspace.CurrentRequirementLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey;
            return workspace;
        }

        workspace = new WorkspaceEntity
        {
            WorkspaceId = workspaceId,
            WorkspacePath = workspaceId,
            Name = string.IsNullOrWhiteSpace(Path.GetFileName(workspaceId)) ? "workspace" : Path.GetFileName(workspaceId),
            TodoPath = "docs/todo.yaml",
            CurrentRequirementLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey,
            IsEnabled = true,
            DateTimeCreated = DateTimeOffset.UtcNow,
            DateTimeModified = DateTimeOffset.UtcNow
        };
        ctx.Workspaces.Add(workspace);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        return workspace;
    }

    private static async Task EnsureDefaultLayerAsync(McpDbContext ctx, string workspaceId, CancellationToken ct)
    {
        if (await ctx.RequirementScopeLayers.AnyAsync(x => x.Key == RequirementScopeLayerDefaults.DefaultLayerKey, ct).ConfigureAwait(false))
            return;

        var now = DateTimeOffset.UtcNow;
        ctx.RequirementScopeLayers.Add(new RequirementScopeLayerEntity
        {
            WorkspaceId = workspaceId,
            Key = RequirementScopeLayerDefaults.DefaultLayerKey,
            Order = 1,
            Name = "Layer 1",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private DbScope CreateScope()
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var requestCtx = _httpContextAccessor?.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        var workspacePath = requestCtx?.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
            workspacePath = TryInferWorkspacePathFromOptions();
        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            workspacePath = Path.GetFullPath(workspacePath);
            var scopedWorkspace = scope.ServiceProvider.GetService<WorkspaceContext>();
            if (scopedWorkspace is not null)
            {
                scopedWorkspace.WorkspacePath = workspacePath;
                scopedWorkspace.WorkspaceName = requestCtx?.WorkspaceName ?? Path.GetFileName(workspacePath);
            }
        }

        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        if (!string.IsNullOrWhiteSpace(workspacePath))
            ctx.OverrideWorkspaceId(Path.GetFullPath(workspacePath));

        return new DbScope(scope, ctx);
    }

    private RequirementDocumentPaths ResolveDocumentPaths(string workspaceId)
    {
        if (!string.IsNullOrWhiteSpace(workspaceId))
        {
            var projectDir = Path.Combine(workspaceId, "docs", "Project");
            return new RequirementDocumentPaths(
                Path.Combine(projectDir, RequirementsDocumentRenderer.FunctionalFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.TechnicalFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.TestingFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.MappingFileName),
                Path.Combine(projectDir, RequirementsDocumentRenderer.MatrixFileName));
        }

        return new RequirementDocumentPaths(
            _options.FunctionalRequirementsPath,
            _options.TechnicalRequirementsPath,
            _options.TestingRequirementsPath,
            _options.MappingPath,
            _options.MatrixPath);
    }

    private string? ReadExistingMatrixForExport(string? outputRootPath)
    {
        if (!string.IsNullOrWhiteSpace(outputRootPath))
        {
            var outputMatrix = Path.Combine(outputRootPath, RequirementsDocumentRenderer.MatrixFileName);
            var outputMatrixMarkdown = ReadFileIfExists(outputMatrix);
            if (outputMatrixMarkdown is not null)
                return outputMatrixMarkdown;
        }

        var workspacePath = TryGetRequestWorkspacePath();
        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            var workspaceMatrix = Path.Combine(workspacePath, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName);
            var workspaceMatrixMarkdown = ReadFileIfExists(workspaceMatrix);
            if (workspaceMatrixMarkdown is not null)
                return workspaceMatrixMarkdown;
        }

        return ReadFileIfExists(_options.MatrixPath);
    }

    private string? ReadExistingMatrixForWikiExport(string outputRootPath)
    {
        var projectRoot = Directory.GetParent(Path.GetFullPath(outputRootPath))?.FullName;
        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            var projectMatrix = Path.Combine(projectRoot, RequirementsDocumentRenderer.MatrixFileName);
            var projectMatrixMarkdown = ReadFileIfExists(projectMatrix);
            if (projectMatrixMarkdown is not null)
                return projectMatrixMarkdown;
        }

        return ReadExistingMatrixForExport(null);
    }

    private string? TryInferWorkspacePathFromOptions()
    {
        var functional = _options.FunctionalRequirementsPath;
        if (string.IsNullOrWhiteSpace(functional))
            return null;
        var projectDir = Path.GetDirectoryName(Path.GetFullPath(functional));
        var docsDir = projectDir is null ? null : Directory.GetParent(projectDir)?.FullName;
        return docsDir is null ? null : Directory.GetParent(docsDir)?.FullName;
    }

    private string? TryGetRequestWorkspacePath()
    {
        var requestCtx = _httpContextAccessor?.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        var workspacePath = requestCtx?.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
            workspacePath = TryInferWorkspacePathFromOptions();
        return string.IsNullOrWhiteSpace(workspacePath) ? null : Path.GetFullPath(workspacePath);
    }

    private static IReadOnlyList<string> NormalizeIds(IEnumerable<string>? ids) =>
        ids is null
            ? []
            : ids.Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private static string? ReadFileIfExists(string? path) =>
        string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null : File.ReadAllText(path);

    private static IQueryable<RequirementEntity> ApplyAreaFilter(IQueryable<RequirementEntity> query, string expectedPrefix, string? area)
    {
        var token = NormalizeRequirementToken(area);
        if (token is null)
            return query;

        var idPrefix = expectedPrefix + "-" + token + "-";
        return query.Where(x => EF.Functions.Like(x.Id, idPrefix + "%"));
    }

    private static IQueryable<RequirementEntity> ApplyTrSubareaFilter(IQueryable<RequirementEntity> query, string? subarea)
    {
        var token = NormalizeRequirementToken(subarea);
        if (token is null)
            return query;

        return query.Where(x => EF.Functions.Like(x.Id, "TR-%-" + token + "-%"));
    }

    private static IQueryable<RequirementEntity> ApplyStatusFilter(IQueryable<RequirementEntity> query, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return query;

        var normalized = NormalizeStatus(status);
        return query.Where(x => x.Status == normalized);
    }

    private static string? NormalizeRequirementToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var token = value.Trim().ToUpperInvariant();
        return RequirementTokenRegex.IsMatch(token) ? token : "\0";
    }

    private static void ValidateFr(FrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id), "FR");
        if (string.IsNullOrWhiteSpace(entry.Title))
            throw new ArgumentException("FR title is required.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("FR body is required.", nameof(entry));
    }

    private static void ValidateTr(TrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id), "TR");
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("TR body is required.", nameof(entry));
    }

    private static void ValidateTest(TestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id), "TEST");
        if (string.IsNullOrWhiteSpace(entry.Condition))
            throw new ArgumentException("TEST condition is required.", nameof(entry));
    }

    private static string NormalizePriority(string? priority) =>
        string.IsNullOrWhiteSpace(priority) ? "medium" : priority.Trim().ToLowerInvariant();

    private static string NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "pending" : status.Trim().ToLowerInvariant();

    private static void ValidateMapping(FrTrMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        ValidateId(mapping.FrId, nameof(mapping.FrId), "FR");
        ArgumentNullException.ThrowIfNull(mapping.TrIds);
        ArgumentNullException.ThrowIfNull(mapping.TestIds);
        foreach (var trId in mapping.TrIds)
            ValidateId(trId, nameof(mapping.TrIds), "TR");
        foreach (var testId in mapping.TestIds)
            ValidateId(testId, nameof(mapping.TestIds), "TEST");
    }

    private static void ValidateId(string id, string paramName, string? expectedPrefix = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID is required.", paramName);
        if (expectedPrefix is not null && !IsValidRequirementId(id, expectedPrefix))
            throw new ArgumentException($"Requirement ID '{id}' must match the {expectedPrefix} identifier shape.", paramName);
    }

    private static bool IsValidRequirementId(string id, string expectedPrefix) =>
        RequirementIdShapeRegex.IsMatch(id)
        && id.StartsWith(expectedPrefix + "-", StringComparison.OrdinalIgnoreCase)
        && !id.Contains('*', StringComparison.Ordinal);

    private static bool IdEquals(string left, string right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static RequirementScopeLayerEntry NormalizeLayerEntry(RequirementScopeLayerEntry entry, string workspaceId)
    {
        var key = NormalizeLayerKey(entry.Key);
        if (entry.Order < 1)
            throw new ArgumentException("Requirement scope layer order must be greater than zero.", nameof(entry));
        var name = string.IsNullOrWhiteSpace(entry.Name) ? key : entry.Name.Trim();
        return entry with
        {
            Key = key,
            Name = name,
            Description = string.IsNullOrWhiteSpace(entry.Description) ? null : entry.Description.Trim(),
            ScopeEndLayerKey = NormalizeOptionalLayerKey(entry.ScopeEndLayerKey),
            WorkspaceId = workspaceId
        };
    }

    private static string NormalizeLayerKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Requirement scope layer key is required.", nameof(key));

        var normalized = key.Trim();
        if (!Regex.IsMatch(normalized, @"^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant))
            throw new ArgumentException($"Requirement scope layer key '{key}' is invalid.", nameof(key));
        return normalized;
    }

    private static string? NormalizeOptionalLayerKey(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : NormalizeLayerKey(key);

    private static async Task<RequirementScopeLayerEntity> FindLayerAsync(McpDbContext ctx, string key, CancellationToken ct)
    {
        var normalized = NormalizeLayerKey(key);
        return await ctx.RequirementScopeLayers.FirstOrDefaultAsync(x => x.Key == normalized, ct).ConfigureAwait(false)
            ?? throw new RequirementsNotFoundException($"Requirement scope layer '{normalized}' was not found.");
    }

    private static async Task ValidateLayerSunsetAsync(
        McpDbContext ctx,
        string layerKey,
        int layerOrder,
        string? scopeEndLayerKey,
        bool allowSelf,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(scopeEndLayerKey))
            return;

        var endKey = NormalizeLayerKey(scopeEndLayerKey);
        if (allowSelf && IdEquals(layerKey, endKey))
            return;

        var endLayer = await FindLayerAsync(ctx, endKey, ct).ConfigureAwait(false);
        if (endLayer.Order < layerOrder)
            throw new InvalidOperationException("Requirement scope layer sunset cannot be before the layer being sunset.");
    }

    private static async Task<(string Start, string? End)> ValidateRequirementScopeAsync(
        McpDbContext ctx,
        string? scopeStartLayerKey,
        string? scopeEndLayerKey,
        CancellationToken ct)
    {
        var startKey = string.IsNullOrWhiteSpace(scopeStartLayerKey)
            ? RequirementScopeLayerDefaults.DefaultLayerKey
            : NormalizeLayerKey(scopeStartLayerKey);
        var startLayer = await FindLayerAsync(ctx, startKey, ct).ConfigureAwait(false);
        var endKey = NormalizeOptionalLayerKey(scopeEndLayerKey);
        if (endKey is null)
            return (startKey, null);

        var endLayer = await FindLayerAsync(ctx, endKey, ct).ConfigureAwait(false);
        if (endLayer.Order < startLayer.Order)
            throw new InvalidOperationException("Requirement scope end layer cannot be before its start layer.");
        return (startKey, endKey);
    }

    private static bool IsRequirementEffective(
        RequirementEntity row,
        int currentOrder,
        IReadOnlyDictionary<string, int> layerOrders,
        IReadOnlyDictionary<string, int?> layerEndOrders)
    {
        if (!layerOrders.TryGetValue(row.ScopeStartLayerKey, out var startOrder))
            return false;
        if (startOrder > currentOrder)
            return false;

        int? effectiveEnd = null;
        if (!string.IsNullOrWhiteSpace(row.ScopeEndLayerKey))
        {
            if (!layerOrders.TryGetValue(row.ScopeEndLayerKey, out var requirementEndOrder))
                return false;
            effectiveEnd = requirementEndOrder;
        }

        if (layerEndOrders.TryGetValue(row.ScopeStartLayerKey, out var layerEndOrder) && layerEndOrder.HasValue)
            effectiveEnd = effectiveEnd.HasValue ? Math.Min(effectiveEnd.Value, layerEndOrder.Value) : layerEndOrder.Value;

        return !effectiveEnd.HasValue || effectiveEnd.Value >= currentOrder;
    }

    private static string RequireWorkspaceId(McpDbContext ctx) =>
        string.IsNullOrWhiteSpace(ctx.CurrentWorkspaceId)
            ? throw new InvalidOperationException("Requirements operations require a resolved workspace.")
            : ctx.CurrentWorkspaceId;

    private static string Now() => DateTime.UtcNow.ToString("O");

    private static IReadOnlyList<FrTrMapping> BuildVisibleMappings(
        McpDbContext ctx,
        IReadOnlyList<RequirementTraceabilityLinkEntity> links)
    {
        return links
            .Where(link => !IsSoftDeleted(ctx, link))
            .GroupBy(link => link.FrId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new FrTrMapping(
                group.Key,
                group.Where(link => link.TargetKind == TrKind).Select(link => link.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.Where(link => link.TargetKind == TestKind).Select(link => link.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                group.First().WorkspaceId))
            .ToArray();
    }

    private static async Task RestoreRequirementsCoreAsync(
        McpDbContext ctx,
        RequirementsDatabaseCompensationState state,
        CancellationToken cancellationToken)
    {
        var currentRows = await ctx.Requirements
            .IgnoreQueryFilters(SoftDeleteQueryFilter)
            .Where(row => row.WorkspaceId == state.WorkspaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var snapshotRows = state.Requirements.ToDictionary(
            row => RequirementKey(row.Entity.WorkspaceId, row.Entity.Kind, row.Entity.Id),
            StringComparer.OrdinalIgnoreCase);

        foreach (var current in currentRows)
        {
            if (!snapshotRows.ContainsKey(RequirementKey(current.WorkspaceId, current.Kind, current.Id)))
                MarkSoftDeleted(ctx, current, DateTimeOffset.UtcNow, "requirements_transaction_rollback");
        }

        foreach (var snapshot in state.Requirements)
        {
            var source = snapshot.Entity;
            var current = currentRows.FirstOrDefault(row =>
                string.Equals(row.WorkspaceId, source.WorkspaceId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.Kind, source.Kind, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.Id, source.Id, StringComparison.OrdinalIgnoreCase));
            if (current is null)
            {
                current = CloneRequirement(source);
                ctx.Requirements.Add(current);
            }
            else
            {
                CopyRequirement(source, current);
            }

            // Restore acceptance criteria dependent-side (the [NotMapped] holder does not persist).
            var existingCriteria = await ctx.RequirementAcceptanceCriteria
                .IgnoreQueryFilters(SoftDeleteQueryFilter)
                .Where(c => c.WorkspaceId == source.WorkspaceId && c.RequirementKind == source.Kind && c.RequirementId == source.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            ctx.RequirementAcceptanceCriteria.RemoveRange(existingCriteria);
            ctx.RequirementAcceptanceCriteria.AddRange(CloneCriteria(source.AcceptanceCriteria));

            ApplySoftDeleteState(ctx.Entry(current), snapshot.SoftDelete);
        }
    }

    private static async Task RestoreTraceabilityLinksCoreAsync(
        McpDbContext ctx,
        RequirementsDatabaseCompensationState state,
        CancellationToken cancellationToken)
    {
        var currentLinks = await ctx.RequirementTraceabilityLinks
            .IgnoreQueryFilters(SoftDeleteQueryFilter)
            .Where(row => row.WorkspaceId == state.WorkspaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var snapshotLinks = state.TraceabilityLinks.ToDictionary(
            row => TraceabilityKey(row.Entity.WorkspaceId, row.Entity.FrId, row.Entity.TargetKind, row.Entity.TargetId),
            StringComparer.OrdinalIgnoreCase);

        foreach (var current in currentLinks)
        {
            if (!snapshotLinks.ContainsKey(TraceabilityKey(current.WorkspaceId, current.FrId, current.TargetKind, current.TargetId)))
                MarkSoftDeleted(ctx, current, DateTimeOffset.UtcNow, "requirements_transaction_rollback");
        }

        foreach (var snapshot in state.TraceabilityLinks)
        {
            var source = snapshot.Entity;
            var current = currentLinks.FirstOrDefault(row =>
                string.Equals(row.WorkspaceId, source.WorkspaceId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.FrId, source.FrId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.TargetKind, source.TargetKind, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.TargetId, source.TargetId, StringComparison.OrdinalIgnoreCase));
            if (current is null)
            {
                current = CloneTraceabilityLink(source);
                ctx.RequirementTraceabilityLinks.Add(current);
            }
            else
            {
                CopyTraceabilityLink(source, current);
            }

            ApplySoftDeleteState(ctx.Entry(current), snapshot.SoftDelete);
        }
    }

    private static string RequirementKey(string workspaceId, string kind, string id)
        => $"{workspaceId}\0{kind}\0{id}";

    private static string TraceabilityKey(string workspaceId, string frId, string targetKind, string targetId)
        => $"{workspaceId}\0{frId}\0{targetKind}\0{targetId}";

    private static RequirementEntity CloneRequirement(RequirementEntity source)
    {
        return new RequirementEntity
        {
            WorkspaceId = source.WorkspaceId,
            Kind = source.Kind,
            Id = source.Id,
            Title = source.Title,
            Body = source.Body,
            Priority = source.Priority,
            Status = source.Status,
            Notes = source.Notes,
            AcceptanceCriteria = CloneCriteria(source.AcceptanceCriteria),
            ScopeStartLayerKey = source.ScopeStartLayerKey,
            ScopeEndLayerKey = source.ScopeEndLayerKey,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
        };
    }

    private static void CopyRequirement(RequirementEntity source, RequirementEntity target)
    {
        target.Title = source.Title;
        target.Body = source.Body;
        target.Priority = source.Priority;
        target.Status = source.Status;
        target.Notes = source.Notes;
        target.AcceptanceCriteria.Clear();
        target.AcceptanceCriteria.AddRange(CloneCriteria(source.AcceptanceCriteria));
        target.ScopeStartLayerKey = source.ScopeStartLayerKey;
        target.ScopeEndLayerKey = source.ScopeEndLayerKey;
        target.CreatedAtUtc = source.CreatedAtUtc;
        target.UpdatedAtUtc = source.UpdatedAtUtc;
    }

    private static RequirementTraceabilityLinkEntity CloneTraceabilityLink(RequirementTraceabilityLinkEntity source)
    {
        return new RequirementTraceabilityLinkEntity
        {
            WorkspaceId = source.WorkspaceId,
            SourceKind = source.SourceKind,
            FrId = source.FrId,
            TargetKind = source.TargetKind,
            TargetId = source.TargetId,
            CreatedAtUtc = source.CreatedAtUtc,
        };
    }

    private static void CopyTraceabilityLink(
        RequirementTraceabilityLinkEntity source,
        RequirementTraceabilityLinkEntity target)
    {
        target.SourceKind = source.SourceKind;
        target.CreatedAtUtc = source.CreatedAtUtc;
    }

    private static bool IsSoftDeleted(McpDbContext ctx, object entity)
    {
        var entry = ctx.Entry(entity);
        return entry.Metadata.FindProperty("IsDeleted") is not null
               && entry.Property("IsDeleted").CurrentValue is true;
    }

    private static void MarkSoftDeleted(McpDbContext ctx, object entity, DateTimeOffset deletedAtUtc, string reason)
    {
        var entry = ctx.Entry(entity);
        if (entry.Metadata.FindProperty("IsDeleted") is null)
            return;

        entry.State = EntityState.Modified;
        entry.Property("IsDeleted").CurrentValue = true;
        entry.Property("DeletedAtUtc").CurrentValue = deletedAtUtc;
        entry.Property("DeletedBy").CurrentValue = nameof(RequirementsDatabaseDocumentService);
        entry.Property("DeleteReason").CurrentValue = reason;
    }

    private static void ClearSoftDelete(McpDbContext ctx, object entity)
    {
        var entry = ctx.Entry(entity);
        if (entry.Metadata.FindProperty("IsDeleted") is null)
            return;

        entry.State = EntityState.Modified;
        entry.Property("IsDeleted").CurrentValue = false;
        entry.Property("DeletedAtUtc").CurrentValue = null;
        entry.Property("DeletedBy").CurrentValue = null;
        entry.Property("DeleteReason").CurrentValue = null;
    }

    private static SoftDeleteState ReadSoftDeleteState(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        => new(
            entry.Property("IsDeleted").CurrentValue is true,
            entry.Property("DeletedAtUtc").CurrentValue as DateTimeOffset?,
            entry.Property("DeletedBy").CurrentValue as string,
            entry.Property("DeleteReason").CurrentValue as string);

    private static void ApplySoftDeleteState(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        SoftDeleteState state)
    {
        entry.Property("IsDeleted").CurrentValue = state.IsDeleted;
        entry.Property("DeletedAtUtc").CurrentValue = state.DeletedAtUtc;
        entry.Property("DeletedBy").CurrentValue = state.DeletedBy;
        entry.Property("DeleteReason").CurrentValue = state.DeleteReason;
    }

    private async Task PublishRequirementsChangeSafeAsync(string action, string entityId, CancellationToken ct)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Requirements,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/requirements/{entityId}",
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing requirements change event for {EntityId}", entityId);
        }
    }

    private readonly record struct RequirementBatchValue(string Kind, string Id, string Title, string Body, string Priority, string Status, string? Notes, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria, string ScopeStartLayerKey, string? ScopeEndLayerKey);

    private readonly record struct RequirementDocumentPaths(string Functional, string Technical, string Testing, string Mapping, string Matrix);

    private sealed record RequirementsDatabaseCompensationState(
        string WorkspaceId,
        IReadOnlyList<RequirementEntityCompensationState> Requirements,
        IReadOnlyList<RequirementTraceabilityLinkCompensationState> TraceabilityLinks);

    private sealed record RequirementEntityCompensationState(
        RequirementEntity Entity,
        SoftDeleteState SoftDelete);

    private sealed record RequirementTraceabilityLinkCompensationState(
        RequirementTraceabilityLinkEntity Entity,
        SoftDeleteState SoftDelete);

    private readonly record struct SoftDeleteState(
        bool IsDeleted,
        DateTimeOffset? DeletedAtUtc,
        string? DeletedBy,
        string? DeleteReason);

    private readonly struct DbScope : IAsyncDisposable
    {
        private readonly AsyncServiceScope _scope;

        public DbScope(AsyncServiceScope scope, McpDbContext context)
        {
            _scope = scope;
            Context = context;
        }

        public McpDbContext Context { get; }

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}
