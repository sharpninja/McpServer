using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-REQRECOVERY-001 / TR-MCP-REQRECOVERY-001: writes requirement rows directly inside one
/// serializable transaction. It does not call per-item repository saves, so a failure cannot
/// leave a subset of the payload committed.
/// </summary>
public sealed class RequirementsRecoveryService : IRequirementsRecoveryService
{
    private static readonly Regex FrIdPattern = new(@"^FR-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}$", RegexOptions.Compiled);
    private static readonly Regex TrIdPattern = new(@"^TR-[A-Z0-9]+(?:-[A-Z0-9]+)+\-\d{3}$", RegexOptions.Compiled);
    private static readonly Regex TestIdPattern = new(@"^TEST-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}$", RegexOptions.Compiled);
    private static readonly Regex KeyPattern = new(@"^[A-Za-z0-9._:-]{1,128}$", RegexOptions.Compiled);
    /// <summary>Matches <see cref="RequirementEntity.Title"/> <c>[StringLength(1024)]</c>.</summary>
    private const int TitleMaxLength = 1024;
    /// <summary>Matches <see cref="RequirementEntity.Id"/> <c>[StringLength(128)]</c>.</summary>
    private const int IdMaxLength = 128;
    /// <summary>Matches <see cref="RequirementEntity.Priority"/> <c>[StringLength(32)]</c>.</summary>
    private const int PriorityMaxLength = 32;
    /// <summary>Matches <see cref="RequirementEntity.Status"/> <c>[StringLength(64)]</c>.</summary>
    private const int StatusMaxLength = 64;
    /// <summary>Named soft-delete query filter on durable entities (see McpDbContext).</summary>
    private static readonly string[] SoftDeleteQueryFilter = ["SoftDelete"];
    /// <summary>Retries after requirement-key unique races (recovery is upsert).</summary>
    private const int UniqueRaceMaxAttempts = 3;
    private static readonly JsonSerializerOptions ResultJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions HashJsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly McpDbContext _db;
    private readonly WorkspaceContext _workspace;
    private readonly TimeSpan _commandBudget;

    /// <summary>Initializes the recovery service for the active workspace.</summary>
    /// <param name="db">Workspace-filtered database context.</param>
    /// <param name="workspace">Active workspace. Recovery refuses an unresolved workspace.</param>
    /// <param name="commandBudget">
    /// Optional SaveChanges budget. Null uses <see cref="StorageCommandBudget.Default"/>.
    /// Expiry is <see cref="StorageCommandBudgetExceededException"/> and classifies as HTTP 503.
    /// </param>
    public RequirementsRecoveryService(McpDbContext db, WorkspaceContext workspace, TimeSpan? commandBudget = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _commandBudget = commandBudget ?? StorageCommandBudget.Default;
    }

    /// <inheritdoc />
    public async Task<RequirementsRecoveryResult> PlanAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        var payload = Validate(request);
        var existing = await LoadExistingAsync(payload.Items, cancellationToken).ConfigureAwait(false);
        RejectAcceptanceCriteriaRemoval(payload.Items, existing);
        return BuildResult(payload, existing, applied: false, replay: false, createdAtUtc: string.Empty);
    }

    /// <inheritdoc />
    public async Task<RequirementsRecoveryResult> ApplyAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        var payload = Validate(request);
        var prior = await FindRunAsync(payload.WorkspaceId, payload.Key, cancellationToken).ConfigureAwait(false);
        if (prior is not null)
            return ResolveExisting(prior, payload.Hash);

        // Recovery is upsert. Unique races on the requirement PK (concurrent applies with
        // different idempotency keys) reload and retry instead of fabricating an
        // idempotency-key payload conflict. Unique races on the recovery-run key still
        // resolve to Replay / Conflict via FindRunAsync.
        for (var attempt = 1; attempt <= UniqueRaceMaxAttempts; attempt++)
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var raced = await FindRunAsync(payload.WorkspaceId, payload.Key, cancellationToken).ConfigureAwait(false);
                if (raced is not null)
                {
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    _db.ChangeTracker.Clear();
                    return ResolveExisting(raced, payload.Hash);
                }

                var existing = await LoadExistingAsync(payload.Items, cancellationToken).ConfigureAwait(false);
                RejectAcceptanceCriteriaRemoval(payload.Items, existing);
                var createdAt = DateTimeOffset.UtcNow.ToString("o");
                var result = BuildResult(payload, existing, applied: true, replay: false, createdAtUtc: createdAt);
                ApplyItems(payload, existing, createdAt);
                _db.RequirementsRecoveryRuns.Add(new RequirementsRecoveryRunEntity
                {
                    WorkspaceId = payload.WorkspaceId,
                    IdempotencyKey = payload.Key,
                    PayloadHash = payload.Hash,
                    Status = "applied",
                    ResultJson = JsonSerializer.Serialize(result, ResultJsonOptions),
                    CreatedAtUtc = createdAt,
                });

                await StorageCommandBudget.ExecuteAsync(
                    ct => _db.SaveChangesAsync(ct),
                    cancellationToken,
                    _commandBudget).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                if (_db.Database.CurrentTransaction is not null)
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                _db.ChangeTracker.Clear();
                var raced = await FindRunAsync(payload.WorkspaceId, payload.Key, cancellationToken).ConfigureAwait(false);
                if (raced is not null)
                    return ResolveExisting(raced, payload.Hash);

                if (attempt < UniqueRaceMaxAttempts)
                    continue;

                // Preserve the actual unique failure rather than fabricating a same-key payload conflict.
                throw;
            }
            catch
            {
                if (_db.Database.CurrentTransaction is not null)
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        throw new InvalidOperationException("Requirements recovery apply exhausted unique-race retries.");
    }

    /// <inheritdoc />
    public async Task<RequirementsRecoveryResult> GetAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(idempotencyKey);
        var workspaceId = RequireWorkspace();
        var run = await FindRunAsync(workspaceId, key, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            throw new RequirementsRecoveryNotFoundException(
                $"Requirements recovery run '{key}' was not found.");
        }

        return Deserialize(run);
    }

    private RequirementsRecoveryResult ResolveExisting(RequirementsRecoveryRunEntity run, string hash)
    {
        if (!string.Equals(run.PayloadHash, hash, StringComparison.Ordinal))
        {
            throw new RequirementsRecoveryConflictException(
                $"Requirements recovery idempotency key '{run.IdempotencyKey}' conflicts with a stored payload.");
        }

        var stored = Deserialize(run);
        stored.Replay = true;
        return stored;
    }

    private void ApplyItems(NormalizedPayload payload, Dictionary<string, RequirementEntity> existing, string timestamp)
    {
        foreach (var item in payload.Items)
        {
            var mapKey = MapKey(item.Kind, item.Id);
            if (existing.TryGetValue(mapKey, out var row))
            {
                var wasDeleted = IsSoftDeleted(row);
                row.Title = item.Title;
                row.Body = item.Body;
                row.Priority = item.Priority;
                row.Status = item.Status;
                row.UpdatedAtUtc = timestamp;
                // Ordinary create path revives soft-deleted rows by clearing deletion metadata.
                // Always clear: tracked stale shadow values must not leave IsDeleted=1 in the store.
                if (wasDeleted)
                    row.CreatedAtUtc = timestamp;
                ClearSoftDelete(row);

                continue;
            }

            _db.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = payload.WorkspaceId,
                Kind = item.Kind,
                Id = item.Id,
                Title = item.Title,
                Body = item.Body,
                Priority = item.Priority,
                Status = item.Status,
                // FR-MCP-REQRECOVERY-001 / RequirementEntity default: recovery creates rows at the
                // product-requirements root layer. Item request has no ScopeStartLayerKey field.
                ScopeStartLayerKey = "layer-1",
                CreatedAtUtc = timestamp,
                UpdatedAtUtc = timestamp,
            });
        }
    }

    private async Task<Dictionary<string, RequirementEntity>> LoadExistingAsync(
        IReadOnlyList<NormalizedItem> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(item => item.Id).Distinct(StringComparer.Ordinal).ToArray();
        // SoftDelete filter would hide deleted rows and cause ApplyItems to insert ? PK 409.
        // Match RequirementsDatabaseDocumentService.AddRequirementAsync revive path.
        var rows = await _db.Requirements
            .IgnoreQueryFilters(SoftDeleteQueryFilter)
            .Where(row => ids.Contains(row.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.ToDictionary(row => MapKey(row.Kind, row.Id), StringComparer.Ordinal);
    }

    private Task<RequirementsRecoveryRunEntity?> FindRunAsync(string workspaceId, string key, CancellationToken cancellationToken)
    {
        // Explicit WorkspaceId predicate keeps the lookup correct even when global query filters
        // are disabled in tests (IgnoreQueryFilters / InMemory). Query filter already scopes by workspace.
        return _db.RequirementsRecoveryRuns.FirstOrDefaultAsync(
            row => row.WorkspaceId == workspaceId && row.IdempotencyKey == key,
            cancellationToken);
    }

    private static RequirementsRecoveryResult Deserialize(RequirementsRecoveryRunEntity run)
    {
        var result = JsonSerializer.Deserialize<RequirementsRecoveryResult>(run.ResultJson, ResultJsonOptions)
            ?? throw new InvalidOperationException("Stored requirements recovery result is empty.");
        result.IdempotencyKey = run.IdempotencyKey;
        result.PayloadHash = run.PayloadHash;
        result.Status = run.Status;
        result.Applied = true;
        result.CreatedAtUtc = run.CreatedAtUtc;
        return result;
    }

    private static RequirementsRecoveryResult BuildResult(
        NormalizedPayload payload,
        IReadOnlyDictionary<string, RequirementEntity> existing,
        bool applied,
        bool replay,
        string createdAtUtc)
    {
        return new RequirementsRecoveryResult
        {
            IdempotencyKey = payload.Key,
            PayloadHash = payload.Hash,
            Status = applied ? "applied" : "planned",
            Applied = applied,
            Replay = replay,
            CreatedAtUtc = createdAtUtc,
            Items = payload.Items.Select(item => new RequirementsRecoveryItemResult
            {
                Kind = item.Kind,
                Id = item.Id,
                Action = existing.ContainsKey(MapKey(item.Kind, item.Id)) ? "update" : "create",
            }).ToList(),
        };
    }

    private NormalizedPayload Validate(RequirementsRecoveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var workspaceId = RequireWorkspace();
        var key = NormalizeKey(request.IdempotencyKey);
        var items = NormalizeItems(request.Items);
        return new NormalizedPayload(workspaceId, key, Hash(items), items);
    }

    private string RequireWorkspace()
    {
        if (string.IsNullOrWhiteSpace(_workspace.WorkspacePath))
            throw new ArgumentException("Workspace is not resolved.");

        var workspaceId = _workspace.WorkspacePath.Trim();
        if (!string.Equals(_db.CurrentWorkspaceId, workspaceId, StringComparison.Ordinal))
            _db.OverrideWorkspaceId(workspaceId);
        return workspaceId;
    }

    private static string NormalizeKey(string? idempotencyKey)
    {
        var key = idempotencyKey?.Trim() ?? string.Empty;
        if (!KeyPattern.IsMatch(key))
            throw new ArgumentException("idempotencyKey must be 1-128 characters from [A-Za-z0-9._:-].");
        return key;
    }

    private static List<NormalizedItem> NormalizeItems(IReadOnlyList<RequirementsRecoveryItemRequest>? items)
    {
        if (items is null || items.Count == 0)
            throw new ArgumentException("items must contain at least one requirement.");

        var normalized = new List<NormalizedItem>(items.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (item is null)
                throw new ArgumentException("items contains a null requirement.");

            var kind = item.Kind?.Trim().ToLowerInvariant() ?? string.Empty;
            if (kind is not ("fr" or "tr" or "test"))
                throw new ArgumentException("item.kind must be one of: fr, tr, test.");

            var id = item.Id?.Trim() ?? string.Empty;
            if (!IsValidId(kind, id))
                throw new ArgumentException($"Requirement id '{id}' is not valid for kind '{kind}'.");
            if (id.Length > IdMaxLength)
                throw new ArgumentException($"Requirement id '{id}' exceeds {IdMaxLength} characters.");

            var title = item.Title?.Trim() ?? string.Empty;
            var body = item.Body?.Trim() ?? string.Empty;
            // Canonical TEST rows may have empty Title (ValidateTest only requires condition/body).
            if (body.Length == 0)
                throw new ArgumentException($"Requirement '{id}' requires body.");
            if (body.StartsWith("Placeholder requirement backfilled for TODO link", StringComparison.Ordinal))
                throw new ArgumentException($"Requirement '{id}' body is a placeholder.");
            if (kind is not "test" && title.Length == 0)
                throw new ArgumentException($"Requirement '{id}' requires title.");
            if (title.Length > TitleMaxLength)
                throw new ArgumentException($"Requirement '{id}' title exceeds {TitleMaxLength} characters.");

            var priority = string.IsNullOrWhiteSpace(item.Priority) ? "medium" : item.Priority.Trim().ToLowerInvariant();
            var status = string.IsNullOrWhiteSpace(item.Status) ? "pending" : item.Status.Trim().ToLowerInvariant();
            if (priority.Length > PriorityMaxLength)
                throw new ArgumentException($"Requirement '{id}' priority exceeds {PriorityMaxLength} characters.");
            if (status.Length > StatusMaxLength)
                throw new ArgumentException($"Requirement '{id}' status exceeds {StatusMaxLength} characters.");
            var mapKey = MapKey(kind, id);
            if (!seen.Add(mapKey))
                throw new ArgumentException($"Duplicate requirement '{kind}:{id}' in the recovery payload.");

            normalized.Add(new NormalizedItem(kind, id, title, body, priority, status));
        }

        return normalized;
    }

    private static void RejectAcceptanceCriteriaRemoval(
        IReadOnlyList<NormalizedItem> items,
        IReadOnlyDictionary<string, RequirementEntity> existing)
    {
        foreach (var item in items)
        {
            if (!existing.TryGetValue(MapKey(item.Kind, item.Id), out var row))
                continue;
            var prior = row.Body ?? string.Empty;
            if (prior.Contains("## Acceptance Criteria", StringComparison.Ordinal)
                && !item.Body.Contains("## Acceptance Criteria", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Requirement '{item.Id}' removes acceptance criteria.");
            }
        }
    }

    private static bool IsValidId(string kind, string id) => kind switch
    {
        "fr" => FrIdPattern.IsMatch(id),
        "tr" => TrIdPattern.IsMatch(id),
        "test" => TestIdPattern.IsMatch(id),
        _ => false,
    };

    private static string Hash(IReadOnlyList<NormalizedItem> items)
    {
        // Canonical JSON (ordered fields, no indent) so newline-bearing title/body cannot collide.
        var payload = items
            .OrderBy(item => item.Kind, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => new HashField(item.Kind, item.Id, item.Title, item.Body, item.Priority, item.Status))
            .ToArray();
        var json = JsonSerializer.Serialize(payload, HashJsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private bool IsSoftDeleted(RequirementEntity entity)
    {
        var entry = _db.Entry(entity);
        return entry.Metadata.FindProperty("IsDeleted") is not null
               && entry.Property("IsDeleted").CurrentValue is true;
    }

    private void ClearSoftDelete(RequirementEntity entity)
    {
        var entry = _db.Entry(entity);
        if (entry.Metadata.FindProperty("IsDeleted") is null)
            return;

        entry.Property("IsDeleted").CurrentValue = false;
        entry.Property("DeletedAtUtc").CurrentValue = null;
        entry.Property("DeletedBy").CurrentValue = null;
        entry.Property("DeleteReason").CurrentValue = null;
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var text = current.Message;
            if (text.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || text.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string MapKey(string kind, string id) => kind + "\n" + id;

    private sealed record NormalizedPayload(string WorkspaceId, string Key, string Hash, IReadOnlyList<NormalizedItem> Items);

    private readonly record struct NormalizedItem(string Kind, string Id, string Title, string Body, string Priority, string Status);

    /// <summary>Stable hash DTO; property declaration order is the JSON field order.</summary>
    private sealed record HashField(string kind, string id, string title, string body, string priority, string status);
}
