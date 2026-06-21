using System.Text.Json;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Executes tool registry mutations through the turn transaction
/// coordinator and restores the persisted tool/tag graph on rollback.
/// </summary>
public sealed class TransactionGatedToolRegistryService : IToolRegistryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IToolRegistryService _inner;
    private readonly McpDbContext _db;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedToolRegistryService"/> class.</summary>
    /// <param name="inner">Underlying tool registry service.</param>
    /// <param name="db">Scoped database context used for exact graph restoration.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedToolRegistryService(
        IToolRegistryService inner,
        McpDbContext db,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<ToolSearchResult> SearchAsync(string keyword, string? workspacePath = null, CancellationToken ct = default)
        => _inner.SearchAsync(keyword, workspacePath, ct);

    /// <inheritdoc />
    public Task<ToolDto?> GetAsync(int id, CancellationToken ct = default)
        => _inner.GetAsync(id, ct);

    /// <inheritdoc />
    public Task<ToolSearchResult> ListAsync(string? workspacePath = null, CancellationToken ct = default)
        => _inner.ListAsync(workspacePath, ct);

    /// <inheritdoc />
    public Task<ToolMutationResult> CreateAsync(ToolCreateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "tool_registry.create",
            new ToolCreateTransactionPayload(
                request.Name,
                request.Description,
                request.Tags,
                request.ParameterSchema,
                request.CommandTemplate,
                request.WorkspacePath),
            async cancellationToken =>
            {
                var result = await _inner.CreateAsync(request, cancellationToken).ConfigureAwait(false);
                var snapshot = result is { Success: true, Tool: not null }
                    ? await CaptureToolAsync(result.Tool.Id, cancellationToken).ConfigureAwait(false)
                    : null;
                return new MutationExecution(
                    result,
                    snapshot is not null ? rollbackCt => RestoreToolAsync(snapshot, rollbackCt) : null);
            },
            ct);
    }

    /// <inheritdoc />
    public Task<ToolMutationResult> UpdateAsync(int id, ToolUpdateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "tool_registry.update",
            new ToolUpdateTransactionPayload(
                id,
                request.Name,
                request.Description,
                request.Tags,
                request.ParameterSchema,
                request.CommandTemplate,
                request.WorkspacePath),
            async cancellationToken =>
            {
                var before = await CaptureToolAsync(id, cancellationToken).ConfigureAwait(false);
                var result = await _inner.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
                return new MutationExecution(
                    result,
                    result.Success && before is not null
                        ? rollbackCt => RestoreToolAsync(before, rollbackCt)
                        : null);
            },
            ct);
    }

    /// <inheritdoc />
    public Task<ToolMutationResult> DeleteAsync(int id, CancellationToken ct = default)
        => ExecuteMutationAsync(
            "tool_registry.delete",
            new ToolDeleteTransactionPayload(id),
            async cancellationToken =>
            {
                var before = await CaptureToolAsync(id, cancellationToken).ConfigureAwait(false);
                var result = await _inner.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
                return new MutationExecution(
                    result,
                    result.Success && before is not null
                        ? rollbackCt => RestoreToolAsync(before, rollbackCt)
                        : null);
            },
            ct);

    private async Task<ToolMutationResult> ExecuteMutationAsync(
        string operationName,
        object operationBody,
        Func<CancellationToken, Task<MutationExecution>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
        {
            var direct = await mutation(cancellationToken).ConfigureAwait(false);
            return direct.Result;
        }

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            return new ToolMutationResult(
                false,
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message);
        }

        if (RequiresMutationTransactions(status) && _db.Database.ProviderName is null)
            return new ToolMutationResult(false, "Tool registry storage does not support transaction rollback compensation.");

        MutationExecution? execution = null;
        var transaction = BuildTransactionRequest(operationName, operationBody);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    execution = await mutation(ct).ConfigureAwait(false);
                    return ToMutationResult(execution.Value);
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (execution is not null && (!execution.Value.Result.Success || IsTransactionSuccess(result)))
            return execution.Value.Result;

        return ToTransactionFailure(operationName, result);
    }

    private async Task<ToolDefinitionSnapshot?> CaptureToolAsync(int id, CancellationToken cancellationToken)
    {
        _db.ChangeTracker.Clear();
        var entity = await _db.ToolDefinitions
            .IgnoreQueryFilters()
            .Include(tool => tool.Tags)
            .FirstOrDefaultAsync(tool => tool.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return null;

        var snapshot = ToolDefinitionSnapshot.From(entity, ReadSoftDelete(_db.Entry(entity)), entity.Tags
            .OrderBy(tag => tag.Id)
            .Select(tag => ToolDefinitionTagSnapshot.From(tag, ReadSoftDelete(_db.Entry(tag))))
            .ToList());
        _db.ChangeTracker.Clear();
        return snapshot;
    }

    private async Task RestoreToolAsync(ToolDefinitionSnapshot snapshot, CancellationToken cancellationToken)
    {
        _db.ChangeTracker.Clear();
        var entity = await _db.ToolDefinitions
            .IgnoreQueryFilters()
            .Include(tool => tool.Tags)
            .FirstOrDefaultAsync(tool => tool.Id == snapshot.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new ToolDefinitionEntity
            {
                Id = snapshot.Id,
                WorkspaceId = snapshot.WorkspaceId,
                Name = snapshot.Name,
                Description = snapshot.Description,
                ParameterSchema = snapshot.ParameterSchema,
                CommandTemplate = snapshot.CommandTemplate,
                WorkspacePath = snapshot.WorkspacePath,
                BucketName = snapshot.BucketName,
                DateTimeCreated = snapshot.DateTimeCreated,
                DateTimeModified = snapshot.DateTimeModified,
            };
            _db.ToolDefinitions.Add(entity);
        }
        else
        {
            entity.WorkspaceId = snapshot.WorkspaceId;
            entity.Name = snapshot.Name;
            entity.Description = snapshot.Description;
            entity.ParameterSchema = snapshot.ParameterSchema;
            entity.CommandTemplate = snapshot.CommandTemplate;
            entity.WorkspacePath = snapshot.WorkspacePath;
            entity.BucketName = snapshot.BucketName;
            entity.DateTimeCreated = snapshot.DateTimeCreated;
            entity.DateTimeModified = snapshot.DateTimeModified;
        }

        ApplySoftDelete(_db.Entry(entity), snapshot.SoftDelete);
        RestoreTagSnapshots(entity, snapshot.Tags);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _db.ChangeTracker.Clear();
    }

    private void RestoreTagSnapshots(
        ToolDefinitionEntity entity,
        IReadOnlyList<ToolDefinitionTagSnapshot> snapshots)
    {
        var snapshotIds = snapshots.Select(tag => tag.Id).ToHashSet();
        var currentById = entity.Tags.ToDictionary(tag => tag.Id);

        foreach (var snapshot in snapshots)
        {
            if (!currentById.TryGetValue(snapshot.Id, out var tag))
            {
                tag = new ToolDefinitionTagEntity
                {
                    Id = snapshot.Id,
                    ToolDefinitionId = snapshot.ToolDefinitionId,
                    WorkspaceId = snapshot.WorkspaceId,
                    Tag = snapshot.Tag,
                };
                _db.ToolDefinitionTags.Add(tag);
                currentById[snapshot.Id] = tag;
            }
            else
            {
                tag.ToolDefinitionId = snapshot.ToolDefinitionId;
                tag.WorkspaceId = snapshot.WorkspaceId;
                tag.Tag = snapshot.Tag;
            }

            ApplySoftDelete(_db.Entry(tag), snapshot.SoftDelete);
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var extraTag in currentById.Values.Where(tag => !snapshotIds.Contains(tag.Id)))
            MarkSoftDeleted(_db.Entry(extraTag), now, "transaction_rollback_extra_tag");
    }

    private TurnTransactionRequest BuildTransactionRequest(string operationName, object operationBody)
    {
        var sequence = NextSequence();
        return new TurnTransactionRequest
        {
            TurnId = $"{operationName}-{sequence}",
            OperationName = operationName,
            OperationBodyJson = JsonSerializer.Serialize(operationBody, JsonOptions),
            Sequence = sequence,
            Mutating = true,
        };
    }

    private long NextSequence()
    {
        while (true)
        {
            var current = Volatile.Read(ref _lastSequence);
            var next = Math.Max(current + 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (Interlocked.CompareExchange(ref _lastSequence, next, current) == current)
                return next;
        }
    }

    private static TurnMutationResult ToMutationResult(MutationExecution execution)
        => new()
        {
            Success = execution.Result.Success,
            ResultJson = JsonSerializer.Serialize(execution.Result, JsonOptions),
            Error = execution.Result.Error,
            RollbackAsync = execution.Result.Success ? execution.RollbackAsync : null,
        };

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static ToolMutationResult ToTransactionFailure(string operationName, TurnTransactionResult result)
    {
        var transactionId = string.IsNullOrWhiteSpace(result.TransactionId)
            ? "unassigned"
            : result.TransactionId;
        var message = string.IsNullOrWhiteSpace(result.Message)
            ? result.Reason.ToString()
            : result.Message;
        if (result.RollbackAttempted)
        {
            message = result.RollbackSucceeded
                ? $"{message} Rollback completed."
                : $"{message} Rollback failed: {result.RollbackError ?? "unknown error"}.";
        }

        return new ToolMutationResult(
            false,
            $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private static SoftDeleteSnapshot ReadSoftDelete(EntityEntry entry)
        => new(
            ReadShadow<bool>(entry, "IsDeleted"),
            ReadShadow<DateTimeOffset?>(entry, "DeletedAtUtc"),
            ReadShadow<string?>(entry, "DeletedBy"),
            ReadShadow<string?>(entry, "DeleteReason"));

    private static T? ReadShadow<T>(EntityEntry entry, string propertyName)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        return property is null ? default : (T?)property.CurrentValue;
    }

    private static void ApplySoftDelete(EntityEntry entry, SoftDeleteSnapshot snapshot)
    {
        if (entry.State != EntityState.Added)
            entry.State = EntityState.Modified;

        SetShadowValue(entry, "IsDeleted", snapshot.IsDeleted);
        SetShadowValue(entry, "DeletedAtUtc", snapshot.DeletedAtUtc);
        SetShadowValue(entry, "DeletedBy", snapshot.DeletedBy);
        SetShadowValue(entry, "DeleteReason", snapshot.DeleteReason);
    }

    private static void MarkSoftDeleted(EntityEntry entry, DateTimeOffset deletedAtUtc, string reason)
    {
        if (entry.State != EntityState.Added)
            entry.State = EntityState.Modified;

        SetShadowValue(entry, "IsDeleted", true);
        SetShadowValue(entry, "DeletedAtUtc", deletedAtUtc);
        SetShadowValue(entry, "DeletedBy", nameof(TransactionGatedToolRegistryService));
        SetShadowValue(entry, "DeleteReason", reason);
    }

    private static void SetShadowValue(EntityEntry entry, string propertyName, object? value)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (property is not null)
            property.CurrentValue = value;
    }

    private readonly record struct MutationExecution(
        ToolMutationResult Result,
        Func<CancellationToken, Task>? RollbackAsync);

    private sealed record ToolCreateTransactionPayload(
        string Name,
        string Description,
        IReadOnlyList<string> Tags,
        string? ParameterSchema,
        string? CommandTemplate,
        string? WorkspacePath);

    private sealed record ToolUpdateTransactionPayload(
        int Id,
        string? Name,
        string? Description,
        IReadOnlyList<string>? Tags,
        string? ParameterSchema,
        string? CommandTemplate,
        string? WorkspacePath);

    private sealed record ToolDeleteTransactionPayload(int Id);

    private sealed record ToolDefinitionSnapshot(
        int Id,
        string WorkspaceId,
        string Name,
        string Description,
        string? ParameterSchema,
        string? CommandTemplate,
        string? WorkspacePath,
        string? BucketName,
        DateTimeOffset DateTimeCreated,
        DateTimeOffset DateTimeModified,
        SoftDeleteSnapshot SoftDelete,
        IReadOnlyList<ToolDefinitionTagSnapshot> Tags)
    {
        public static ToolDefinitionSnapshot From(
            ToolDefinitionEntity entity,
            SoftDeleteSnapshot softDelete,
            IReadOnlyList<ToolDefinitionTagSnapshot> tags)
            => new(
                entity.Id,
                entity.WorkspaceId,
                entity.Name,
                entity.Description,
                entity.ParameterSchema,
                entity.CommandTemplate,
                entity.WorkspacePath,
                entity.BucketName,
                entity.DateTimeCreated,
                entity.DateTimeModified,
                softDelete,
                tags);
    }

    private sealed record ToolDefinitionTagSnapshot(
        int Id,
        string WorkspaceId,
        int ToolDefinitionId,
        string Tag,
        SoftDeleteSnapshot SoftDelete)
    {
        public static ToolDefinitionTagSnapshot From(ToolDefinitionTagEntity entity, SoftDeleteSnapshot softDelete)
            => new(entity.Id, entity.WorkspaceId, entity.ToolDefinitionId, entity.Tag, softDelete);
    }

    private sealed record SoftDeleteSnapshot(
        bool IsDeleted,
        DateTimeOffset? DeletedAtUtc,
        string? DeletedBy,
        string? DeleteReason);
}
