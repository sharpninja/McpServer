using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using McpServer.Cqrs;
using McpServer.TransactionSecurity;
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

    /// <summary>Remembers a multi-layer memory under the turn transaction policy.</summary>
    Task<MemoryRememberResult> RememberAsync(MemoryRememberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Promotes a source into memory under the turn transaction policy.</summary>
    Task<MemoryPromoteResult> PromoteAsync(MemoryPromoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Applies consolidate/sleep merge under the turn transaction policy.</summary>
    Task<MemoryConsolidateResult> ConsolidateAsync(MemoryConsolidateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reverts a memory to snapshot N under the turn transaction policy.</summary>
    Task<MemoryRevertResult> RevertAsync(string id, int versionNumber, CancellationToken cancellationToken = default);
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
    private readonly IDispatcher? _dispatcher;
    private readonly WorkspaceContext? _workspaceContext;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedMemoryService"/> class.</summary>
    /// <param name="memoryService">Memory service that performs durable mutations.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="db">Optional scoped database context used for exact rollback restoration.</param>
    /// <param name="dispatcher">Optional CQRS dispatcher used by additive S5 verbs.</param>
    /// <param name="workspaceContext">Optional workspace context used for CQRS commands.</param>
    public TransactionGatedMemoryService(
        IMemoryService memoryService,
        ITurnTransactionCoordinator? coordinator = null,
        McpDbContext? db = null,
        IDispatcher? dispatcher = null,
        WorkspaceContext? workspaceContext = null)
    {
        _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        _coordinator = coordinator;
        _db = db;
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
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

    /// <inheritdoc />
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public Task<MemoryRememberResult> RememberAsync(
        MemoryRememberRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteGatedAsync(
            "memory.remember",
            request,
            async ct =>
            {
                var dispatched = await RequireDispatcher().SendAsync(
                    new RememberMemoryCommand(GetWorkspacePath(), request),
                    ct).ConfigureAwait(false);
                return dispatched.IsSuccess && dispatched.Value is not null
                    ? dispatched.Value
                    : new MemoryRememberResult(500, Error: dispatched.Error ?? "Remember failed.");
            },
            error => new MemoryRememberResult(
                409,
                FailureKind: MemoryMutationFailureKind.Conflict,
                Error: error),
            cancellationToken);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public Task<MemoryPromoteResult> PromoteAsync(
        MemoryPromoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteGatedAsync(
            "memory.promote",
            request,
            async ct =>
            {
                var dispatched = await RequireDispatcher().SendAsync(
                    new PromoteMemoryCommand(GetWorkspacePath(), request),
                    ct).ConfigureAwait(false);
                return dispatched.IsSuccess && dispatched.Value is not null
                    ? dispatched.Value
                    : new MemoryPromoteResult(500, Error: dispatched.Error ?? "Promote failed.");
            },
            error => new MemoryPromoteResult(
                409,
                FailureKind: MemoryMutationFailureKind.Conflict,
                Error: error),
            cancellationToken);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public Task<MemoryConsolidateResult> ConsolidateAsync(
        MemoryConsolidateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteGatedAsync(
            "memory.consolidate",
            request,
            async ct =>
            {
                var dispatched = await RequireDispatcher().SendAsync(
                    new ConsolidateMemoryCommand(GetWorkspacePath(), request),
                    ct).ConfigureAwait(false);
                return dispatched.IsSuccess && dispatched.Value is not null
                    ? dispatched.Value
                    : new MemoryConsolidateResult(500, Error: dispatched.Error ?? "Consolidate failed.");
            },
            error => new MemoryConsolidateResult(
                409,
                FailureKind: MemoryMutationFailureKind.Conflict,
                Error: error),
            cancellationToken);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("CQRS dispatcher uses reflection over handler types.")]
    public Task<MemoryRevertResult> RevertAsync(
        string id,
        int versionNumber,
        CancellationToken cancellationToken = default)
        => ExecuteGatedAsync(
            "memory.revert",
            new MemoryRevertTransactionPayload(id, versionNumber),
            async ct =>
            {
                var dispatched = await RequireDispatcher().SendAsync(
                    new RevertMemoryCommand(GetWorkspacePath(), id, versionNumber),
                    ct).ConfigureAwait(false);
                return dispatched.IsSuccess && dispatched.Value is not null
                    ? dispatched.Value
                    : new MemoryRevertResult(500, Error: dispatched.Error ?? "Revert failed.");
            },
            error => new MemoryRevertResult(
                409,
                FailureKind: MemoryMutationFailureKind.Conflict,
                Error: error),
            cancellationToken);

    private async Task<T> ExecuteGatedAsync<T>(
        string operationName,
        object operationBody,
        Func<CancellationToken, Task<T>> mutation,
        Func<string, T> onReject,
        CancellationToken cancellationToken)
    {
        if (TurnTransactionKeyserverScope.ShouldBypassCoordinator(_coordinator, operationName))
            return await mutation(cancellationToken).ConfigureAwait(false);

        var status = _coordinator!.GetStatus();
        if (status.Degraded)
        {
            return onReject(string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message);
        }

        var produced = default(T);
        var producedSet = false;
        var result = await _coordinator.ExecuteAsync(
                BuildTransactionRequest(operationName, operationBody),
                async ct =>
                {
                    produced = await mutation(ct).ConfigureAwait(false);
                    producedSet = true;
                    return new TurnMutationResult { Success = true, ResultJson = "{}" };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (producedSet && IsTransactionSuccess(result))
            return produced!;

        var transactionId = string.IsNullOrWhiteSpace(result.TransactionId) ? "unassigned" : result.TransactionId;
        var message = string.IsNullOrWhiteSpace(result.Message) ? result.Reason.ToString() : result.Message;
        return onReject($"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private IDispatcher RequireDispatcher()
        => _dispatcher ?? throw new InvalidOperationException("CQRS dispatcher is not registered for gated memory verbs.");

    private string GetWorkspacePath()
        => _workspaceContext?.WorkspacePath ?? string.Empty;

    private async Task<MemoryMutationResult> ExecuteMutationAsync(
        string operationName,
        object operationBody,
        Func<CancellationToken, Task<MutationExecution>> mutation,
        CancellationToken cancellationToken)
    {
        if (TurnTransactionKeyserverScope.ShouldBypassCoordinator(_coordinator, operationName))
        {
            var direct = await mutation(cancellationToken).ConfigureAwait(false);
            return direct.Result;
        }

        var status = _coordinator!.GetStatus();
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

    private sealed record MemoryRevertTransactionPayload(string Id, int VersionNumber);

    private static string ToEntityScope(MemoryScope scope)
        => scope == MemoryScope.Global ? MemoryEntity.GlobalScope : MemoryEntity.WorkspaceScope;

    private static void SetShadowValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, object? value)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (property is not null)
            property.CurrentValue = value;
    }
}
