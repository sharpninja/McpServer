using System.Text.Json;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-MCP-TXN-001: Executes memory mutations through the turn transaction coordinator when available.</summary>
public interface ITransactionGatedMemoryService
{
    /// <summary>Adds a memory item under the turn transaction policy.</summary>
    Task<MemoryMutationResult> AddAsync(MemoryAddRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a memory item under the turn transaction policy.</summary>
    Task<MemoryMutationResult> UpdateAsync(string id, MemoryUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a memory item under the turn transaction policy.</summary>
    Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TXN-001: Shared memory mutation gate for HTTP controller and MCP tool entry points.
/// </summary>
public sealed class TransactionGatedMemoryService : ITransactionGatedMemoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryService _memoryService;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly McpDbContext? _db;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedMemoryService"/> class.</summary>
    /// <param name="memoryService">Memory service that performs durable mutations.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="db">Optional scoped database context used for exact rollback restoration.</param>
    public TransactionGatedMemoryService(
        IMemoryService memoryService,
        ITurnTransactionCoordinator? coordinator = null,
        McpDbContext? db = null)
    {
        _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        _coordinator = coordinator;
        _db = db;
    }

    /// <inheritdoc />
    public Task<MemoryMutationResult> AddAsync(
        MemoryAddRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "memory.add",
            request,
            async ct =>
            {
                var result = await _memoryService.AddAsync(request, ct).ConfigureAwait(false);
                return new MutationExecution(
                    result,
                    result is { Success: true, Memory: not null }
                        ? rollbackCt => RestoreMemoryAsync(result.Memory, rollbackCt)
                        : null);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryMutationResult> UpdateAsync(
        string id,
        MemoryUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "memory.update",
            new MemoryUpdateTransactionPayload(id, request),
            async ct =>
            {
                var previous = await _memoryService.GetAsync(id, ct).ConfigureAwait(false);
                var result = await _memoryService.UpdateAsync(id, request, ct).ConfigureAwait(false);
                return new MutationExecution(
                    result,
                    result.Success && previous is not null
                        ? rollbackCt => RestoreMemoryAsync(previous, rollbackCt)
                        : null);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<MemoryMutationResult> RemoveAsync(string id, CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(
            "memory.remove",
            new MemoryRemoveTransactionPayload(id),
            async ct =>
            {
                var previous = await _memoryService.GetAsync(id, ct).ConfigureAwait(false);
                var result = await _memoryService.RemoveAsync(id, ct).ConfigureAwait(false);
                return new MutationExecution(
                    result,
                    result.Success && previous is not null
                        ? rollbackCt => RestoreRemovedMemoryAsync(previous, rollbackCt)
                        : null);
            },
            cancellationToken);

    private async Task<MemoryMutationResult> ExecuteMutationAsync(
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
            return new MemoryMutationResult(
                false,
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message,
                FailureKind: MemoryMutationFailureKind.Conflict);
        }

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

    private async Task RestoreMemoryAsync(MemoryItem previous, CancellationToken cancellationToken)
    {
        if (_db is not null)
        {
            await RestoreMemoryExactAsync(previous, cancellationToken).ConfigureAwait(false);
            return;
        }

        var rollback = await _memoryService.UpdateAsync(
                previous.Id,
                new MemoryUpdateRequest
                {
                    Category = previous.Category,
                    Scope = previous.Scope,
                    Text = previous.Text,
                    UpdatedBy = previous.UpdatedBy,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (rollback.Success)
            return;

        if (rollback.FailureKind == MemoryMutationFailureKind.NotFound)
        {
            await RestoreRemovedMemoryAsync(previous, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException(rollback.Error ?? $"Rollback update for memory '{previous.Id}' failed.");
    }

    private async Task RestoreRemovedMemoryAsync(MemoryItem previous, CancellationToken cancellationToken)
    {
        if (_db is not null)
        {
            await RestoreMemoryExactAsync(previous, cancellationToken).ConfigureAwait(false);
            return;
        }

        var rollback = await _memoryService.AddAsync(
                new MemoryAddRequest
                {
                    Id = previous.Id,
                    Category = previous.Category,
                    Scope = previous.Scope,
                    Text = previous.Text,
                    UpdatedBy = previous.UpdatedBy,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (!rollback.Success)
            throw new InvalidOperationException(rollback.Error ?? $"Rollback restore for memory '{previous.Id}' failed.");
    }

    private async Task RestoreMemoryExactAsync(MemoryItem previous, CancellationToken cancellationToken)
    {
        if (_db is null)
            throw new InvalidOperationException("A database context is required for exact memory rollback restoration.");

        var entity = await _db.Memories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(memory => memory.Id == previous.Id, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            entity = new MemoryEntity
            {
                Id = previous.Id,
                Category = previous.Category,
                Scope = ToEntityScope(previous.Scope),
                WorkspaceId = previous.WorkspacePath,
                Text = previous.Text,
                Version = previous.Version,
                CreatedAtUtc = previous.CreatedAtUtc,
                UpdatedAtUtc = previous.UpdatedAtUtc,
                UpdatedBy = previous.UpdatedBy,
            };
            _db.Memories.Add(entity);
        }
        else
        {
            entity.Category = previous.Category;
            entity.Scope = ToEntityScope(previous.Scope);
            entity.WorkspaceId = previous.WorkspacePath;
            entity.Text = previous.Text;
            entity.Version = previous.Version;
            entity.CreatedAtUtc = previous.CreatedAtUtc;
            entity.UpdatedAtUtc = previous.UpdatedAtUtc;
            entity.UpdatedBy = previous.UpdatedBy;
        }

        var entry = _db.Entry(entity);
        SetShadowValue(entry, "IsDeleted", false);
        SetShadowValue(entry, "DeletedAtUtc", null);
        SetShadowValue(entry, "DeletedBy", null);
        SetShadowValue(entry, "DeleteReason", null);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static TurnMutationResult ToMutationResult(MutationExecution execution)
        => new()
        {
            Success = execution.Result.Success,
            ResultJson = JsonSerializer.Serialize(execution.Result, JsonOptions),
            Error = execution.Result.Error,
            RollbackAsync = execution.Result.Success ? execution.RollbackAsync : null,
        };

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static MemoryMutationResult ToTransactionFailure(string operationName, TurnTransactionResult result)
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

        return new MemoryMutationResult(
            false,
            $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}",
            FailureKind: MemoryMutationFailureKind.Conflict);
    }

    private readonly record struct MutationExecution(
        MemoryMutationResult Result,
        Func<CancellationToken, Task>? RollbackAsync);

    private sealed record MemoryUpdateTransactionPayload(string Id, MemoryUpdateRequest Request);

    private sealed record MemoryRemoveTransactionPayload(string Id);

    private static string ToEntityScope(MemoryScope scope)
        => scope == MemoryScope.Global ? MemoryEntity.GlobalScope : MemoryEntity.WorkspaceScope;

    private static void SetShadowValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, object? value)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (property is not null)
            property.CurrentValue = value;
    }
}
