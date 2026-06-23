using System.Text.Json;
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

    private static readonly System.Text.RegularExpressions.Regex CanonicalFrIdRegex = new(
        @"^FR-[A-Z0-9]+(-[A-Z0-9]+)*-\d+$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static readonly string[] SoftDeleteQueryFilter = ["SoftDelete"];

    // TR-MCP-REQAC-001: AcceptanceCriterion carries [JsonPropertyName] attributes, so default
    // options already emit/read the canonical {id,text,isSatisfied,evidence} shape used by TODOs.
    private static readonly JsonSerializerOptions s_criteriaJson = new(JsonSerializerDefaults.Web);

    /// <summary>Serializes acceptance criteria to the JSON column value (null when empty).</summary>
    private static string? SerializeCriteria(IReadOnlyList<AcceptanceCriterion>? criteria) =>
        criteria is null || criteria.Count == 0 ? null : JsonSerializer.Serialize(criteria, s_criteriaJson);

    /// <summary>Deserializes the JSON column value to acceptance criteria (empty list when null/blank).</summary>
    private static IReadOnlyList<AcceptanceCriterion> DeserializeCriteria(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<AcceptanceCriterion>>(json, s_criteriaJson) ?? [];

    /// <summary>Maps a stored requirement row to an <see cref="FrEntry"/> including acceptance criteria.</summary>
    private static FrEntry MapFr(RequirementEntity x) =>
        new(x.Id, x.Title, x.Body, x.WorkspaceId, x.Priority, x.Status, x.Notes, DeserializeCriteria(x.AcceptanceCriteriaJson));

    /// <summary>Maps a stored requirement row to a <see cref="TrEntry"/> including acceptance criteria.</summary>
    private static TrEntry MapTr(RequirementEntity x) =>
        new(x.Id, x.Title, x.Body, x.WorkspaceId, x.Priority, x.Status, x.Notes, DeserializeCriteria(x.AcceptanceCriteriaJson));

    /// <summary>Maps a stored requirement row to a <see cref="TestEntry"/> including acceptance criteria.</summary>
    private static TestEntry MapTest(RequirementEntity x) =>
        new(x.Id, x.Body, x.WorkspaceId, x.Title, x.Priority, x.Status, x.Notes, DeserializeCriteria(x.AcceptanceCriteriaJson));

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RequirementsOptions _options;
    private readonly ILogger<RequirementsDatabaseDocumentService> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IChangeEventBus? _eventBus;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Initializes a new DB-backed requirements service.</summary>
    public RequirementsDatabaseDocumentService(
        IServiceScopeFactory scopeFactory,
        IOptions<RequirementsOptions> options,
        ILogger<RequirementsDatabaseDocumentService> logger,
        IHttpContextAccessor? httpContextAccessor = null,
        IChangeEventBus? eventBus = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor;
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public void Dispose() => _writeLock.Dispose();

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
                .Where(x => string.IsNullOrEmpty(x.Id) || !CanonicalFrIdRegex.IsMatch(x.Id))
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
        await AddRequirementAsync(FrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateFrAsync(FrEntry entry, CancellationToken ct = default)
    {
        ValidateFr(entry);
        await UpdateRequirementAsync(FrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, ct).ConfigureAwait(false);
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
        await AddRequirementAsync(TrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateTrAsync(TrEntry entry, CancellationToken ct = default)
    {
        ValidateTr(entry);
        await UpdateRequirementAsync(TrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, ct).ConfigureAwait(false);
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
        await AddRequirementAsync(TestKind, entry.Id, entry.Title, entry.Condition, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateTestAsync(TestEntry entry, CancellationToken ct = default)
    {
        ValidateTest(entry);
        await UpdateRequirementAsync(TestKind, entry.Id, entry.Title, entry.Condition, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria, ct).ConfigureAwait(false);
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
                bool exists = await ctx.Requirements.AnyAsync(x => x.Kind == value.Kind && x.Id == value.Id, ct).ConfigureAwait(false);
                if (!exists)
                {
                    toInsert.Add(value);
                }
                // Idempotent create: pre-existing records are left as-is (mitigates double-submit races from clients/plugins that send the batch twice).
            }

            foreach (var value in toInsert)
            {
                ctx.Requirements.Add(new RequirementEntity
                {
                    WorkspaceId = workspaceId,
                    Kind = value.Kind,
                    Id = value.Id,
                    Title = value.Title,
                    Body = value.Body,
                    Priority = NormalizePriority(value.Priority),
                    Status = NormalizeStatus(value.Status),
                    Notes = value.Notes,
                    AcceptanceCriteriaJson = SerializeCriteria(value.AcceptanceCriteria),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
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
                updates.Add((row, value));
            }

            var now = Now();
            foreach (var (row, value) in updates)
            {
                row.Title = value.Title;
                row.Body = value.Body;
                row.Priority = NormalizePriority(value.Priority);
                row.Status = NormalizeStatus(value.Status);
                row.Notes = value.Notes;
                row.AcceptanceCriteriaJson = SerializeCriteria(value.AcceptanceCriteria);
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
        var all = await GetAllMappingsAsync(ct).ConfigureAwait(false);
        return all.FirstOrDefault(x => IdEquals(x.FrId, frId));
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
            RequirementsDocType.Technical => (RequirementsDocumentRenderer.RenderTechnical(await GetAllTrAsync(ct).ConfigureAwait(false)), "text/markdown"),
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
        var documents = RequirementsWikiDocumentRenderer.RenderWikiFiles(fr, tr, test, mapping, generated, ReadExistingMatrixForWikiExport(outputRootPath));
        return await RequirementsDocumentExportWriter.WriteAsync(
            outputRootPath,
            "wiki",
            "all",
            generated,
            documents,
            [RequirementsWikiDocumentRenderer.AzureFolder, RequirementsWikiDocumentRenderer.GitHubFolder],
            ct).ConfigureAwait(false);
    }

    private async Task AddRequirementAsync(string kind, string id, string title, string body, string priority, string status, string? notes, IReadOnlyList<AcceptanceCriterion>? acceptanceCriteria, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
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
                    AcceptanceCriteriaJson = SerializeCriteria(acceptanceCriteria),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                existing.Title = title;
                existing.Body = body;
                existing.Priority = NormalizePriority(priority);
                existing.Status = NormalizeStatus(status);
                existing.Notes = notes;
                existing.AcceptanceCriteriaJson = SerializeCriteria(acceptanceCriteria);
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

    private async Task UpdateRequirementAsync(string kind, string id, string title, string body, string priority, string status, string? notes, IReadOnlyList<AcceptanceCriterion>? acceptanceCriteria, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var scope = CreateScope();
            var ctx = scope.Context;
            await EnsureBootstrappedAsync(ctx, ct).ConfigureAwait(false);
            var row = await FindRequirementAsync(ctx, kind, id, asTracking: true, ct).ConfigureAwait(false)
                ?? throw new RequirementsNotFoundException($"{kind.ToUpperInvariant()} '{id}' was not found.");
            row.Title = title;
            row.Body = body;
            row.Priority = NormalizePriority(priority);
            row.Status = NormalizeStatus(status);
            row.Notes = notes;
            row.AcceptanceCriteriaJson = SerializeCriteria(acceptanceCriteria);
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
            yield return new RequirementBatchValue(FrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria);
        foreach (var entry in entries.Technical)
            yield return new RequirementBatchValue(TrKind, entry.Id, entry.Title, entry.Body, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria);
        foreach (var entry in entries.Testing)
            yield return new RequirementBatchValue(TestKind, entry.Id, entry.Title, entry.Condition, entry.Priority, entry.Status, entry.Notes, entry.AcceptanceCriteria);
    }

    private static RequirementsBatchEntries NormalizeBatchResult(RequirementsBatchEntries entries, string workspaceId) =>
        new(
            entries.Functional
                .Select(entry => entry with
                {
                    WorkspaceId = workspaceId,
                    Priority = NormalizePriority(entry.Priority),
                    Status = NormalizeStatus(entry.Status)
                })
                .ToArray(),
            entries.Technical
                .Select(entry => entry with
                {
                    WorkspaceId = workspaceId,
                    Priority = NormalizePriority(entry.Priority),
                    Status = NormalizeStatus(entry.Status)
                })
                .ToArray(),
            entries.Testing
                .Select(entry => entry with
                {
                    WorkspaceId = workspaceId,
                    Priority = NormalizePriority(entry.Priority),
                    Status = NormalizeStatus(entry.Status)
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
        return await query.FirstOrDefaultAsync(x => x.Kind == kind && x.Id == id, ct).ConfigureAwait(false);
    }

    private async Task EnsureBootstrappedAsync(McpDbContext ctx, CancellationToken ct)
    {
        if (await ctx.Requirements.AnyAsync(ct).ConfigureAwait(false))
            return;

        var paths = ResolveDocumentPaths(ctx.CurrentWorkspaceId);
        if (!File.Exists(paths.Functional) && !File.Exists(paths.Technical) && !File.Exists(paths.Testing) && !File.Exists(paths.Mapping))
            return;

        var now = Now();
        var workspaceId = RequireWorkspaceId(ctx);
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
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = FrKind, Id = entry.Id, Title = entry.Title, Body = entry.Body, CreatedAtUtc = now, UpdatedAtUtc = now });
        }
        foreach (var entry in RequirementsDocumentParser.ParseTechnical(ReadFileIfExists(paths.Technical)))
        {
            if (!importedRequirements.Add($"{TrKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = TrKind, Id = entry.Id, Title = entry.Title, Body = entry.Body, CreatedAtUtc = now, UpdatedAtUtc = now });
        }
        foreach (var entry in RequirementsDocumentParser.ParseTesting(ReadFileIfExists(paths.Testing)))
        {
            if (!importedRequirements.Add($"{TestKind}\0{entry.Id}"))
                continue;
            ctx.Requirements.Add(new RequirementEntity { WorkspaceId = workspaceId, Kind = TestKind, Id = entry.Id, Title = string.Empty, Body = entry.Condition, CreatedAtUtc = now, UpdatedAtUtc = now });
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

    private static void ValidateFr(FrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Title))
            throw new ArgumentException("FR title is required.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("FR body is required.", nameof(entry));
    }

    private static void ValidateTr(TrEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
        if (string.IsNullOrWhiteSpace(entry.Body))
            throw new ArgumentException("TR body is required.", nameof(entry));
    }

    private static void ValidateTest(TestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateId(entry.Id, nameof(entry.Id));
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
        ValidateId(mapping.FrId, nameof(mapping.FrId));
    }

    private static void ValidateId(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID is required.", paramName);
    }

    private static bool IdEquals(string left, string right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

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
            AcceptanceCriteriaJson = source.AcceptanceCriteriaJson,
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
        target.AcceptanceCriteriaJson = source.AcceptanceCriteriaJson;
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

    private readonly record struct RequirementBatchValue(string Kind, string Id, string Title, string Body, string Priority, string Status, string? Notes, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria);

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
