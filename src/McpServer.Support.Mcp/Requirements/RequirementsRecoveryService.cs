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
    private static readonly JsonSerializerOptions ResultJsonOptions = new(JsonSerializerDefaults.Web);

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
        return BuildResult(payload, existing, applied: false, replay: false, createdAtUtc: string.Empty);
    }

    /// <inheritdoc />
    public async Task<RequirementsRecoveryResult> ApplyAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        var payload = Validate(request);
        var prior = await FindRunAsync(payload.Key, cancellationToken).ConfigureAwait(false);
        if (prior is not null)
            return ResolveExisting(prior, payload.Hash);

        await using var transaction = await _db.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var raced = await FindRunAsync(payload.Key, cancellationToken).ConfigureAwait(false);
            if (raced is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                _db.ChangeTracker.Clear();
                return ResolveExisting(raced, payload.Hash);
            }

            var existing = await LoadExistingAsync(payload.Items, cancellationToken).ConfigureAwait(false);
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
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            _db.ChangeTracker.Clear();
            var raced = await FindRunAsync(payload.Key, cancellationToken).ConfigureAwait(false);
            if (raced is not null)
                return ResolveExisting(raced, payload.Hash);

            throw new RequirementsRecoveryConflictException(
                $"Requirements recovery idempotency key '{payload.Key}' conflicts with a stored payload.");
        }
        catch
        {
            if (_db.Database.CurrentTransaction is not null)
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RequirementsRecoveryResult> GetAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(idempotencyKey);
        RequireWorkspace();
        var run = await FindRunAsync(key, cancellationToken).ConfigureAwait(false);
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
                row.Title = item.Title;
                row.Body = item.Body;
                row.Priority = item.Priority;
                row.Status = item.Status;
                row.UpdatedAtUtc = timestamp;
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
        var rows = await _db.Requirements
            .Where(row => ids.Contains(row.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.ToDictionary(row => MapKey(row.Kind, row.Id), StringComparer.Ordinal);
    }

    private Task<RequirementsRecoveryRunEntity?> FindRunAsync(string key, CancellationToken cancellationToken)
        => _db.RequirementsRecoveryRuns.FirstOrDefaultAsync(row => row.IdempotencyKey == key, cancellationToken);

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

            var title = item.Title?.Trim() ?? string.Empty;
            var body = item.Body?.Trim() ?? string.Empty;
            if (title.Length == 0 || body.Length == 0)
                throw new ArgumentException($"Requirement '{id}' requires title and body.");

            var priority = string.IsNullOrWhiteSpace(item.Priority) ? "medium" : item.Priority.Trim();
            var status = string.IsNullOrWhiteSpace(item.Status) ? "pending" : item.Status.Trim();
            var mapKey = MapKey(kind, id);
            if (!seen.Add(mapKey))
                throw new ArgumentException($"Duplicate requirement '{kind}:{id}' in the recovery payload.");

            normalized.Add(new NormalizedItem(kind, id, title, body, priority, status));
        }

        return normalized;
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
        var builder = new StringBuilder();
        foreach (var item in items
            .OrderBy(item => item.Kind, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            builder.Append(item.Kind).Append('\n')
                .Append(item.Id).Append('\n')
                .Append(item.Title).Append('\n')
                .Append(item.Body).Append('\n')
                .Append(item.Priority).Append('\n')
                .Append(item.Status).Append('\n')
                .Append('\u001e');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
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
}
