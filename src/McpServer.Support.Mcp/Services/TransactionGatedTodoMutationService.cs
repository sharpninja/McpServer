using System.Text.Json;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-MCP-TXN-001: Executes TODO mutations through the turn transaction coordinator when available.</summary>
public interface ITransactionGatedTodoMutationService
{
    /// <summary>Creates a TODO item under the turn transaction policy.</summary>
    Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a TODO item under the turn transaction policy.</summary>
    Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a TODO item under the turn transaction policy.</summary>
    Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Moves a TODO item to a target workspace under the turn transaction policy.</summary>
    Task<TodoMutationResult> MoveAsync(string id, TodoMoveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Repairs the TODO projection when mutation transactions do not require compensation.</summary>
    Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TXN-001: Shared TODO mutation gate for HTTP controller and MCP tool update entry points.
/// </summary>
public sealed class TransactionGatedTodoMutationService : ITransactionGatedTodoMutationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly TodoCreationService _todoCreationService;
    private readonly TodoUpdateService _todoUpdateService;
    private readonly TodoServiceResolver? _todoServiceResolver;
    private readonly IWorkspaceService? _workspaceService;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedTodoMutationService"/> class.</summary>
    /// <param name="workspaceAccessor">Workspace-aware TODO service accessor.</param>
    /// <param name="todoCreationService">Shared TODO create orchestration service.</param>
    /// <param name="todoUpdateService">Shared TODO update orchestration service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional turn transaction options.</param>
    public TransactionGatedTodoMutationService(
        WorkspaceServiceAccessor workspaceAccessor,
        TodoCreationService todoCreationService,
        TodoUpdateService todoUpdateService,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
        : this(
            workspaceAccessor,
            todoCreationService,
            todoUpdateService,
            null,
            null,
            coordinator,
            transactionOptions)
    {
    }

    /// <summary>Initializes a new move-capable instance of the <see cref="TransactionGatedTodoMutationService"/> class.</summary>
    /// <param name="workspaceAccessor">Workspace-aware TODO service accessor.</param>
    /// <param name="todoCreationService">Shared TODO create orchestration service.</param>
    /// <param name="todoUpdateService">Shared TODO update orchestration service.</param>
    /// <param name="todoServiceResolver">TODO service resolver used for target workspace moves.</param>
    /// <param name="workspaceService">Workspace registry service used to resolve target workspaces.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional turn transaction options.</param>
    [ActivatorUtilitiesConstructor]
    public TransactionGatedTodoMutationService(
        WorkspaceServiceAccessor workspaceAccessor,
        TodoCreationService todoCreationService,
        TodoUpdateService todoUpdateService,
        TodoServiceResolver? todoServiceResolver,
        IWorkspaceService? workspaceService,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _workspaceAccessor = workspaceAccessor ?? throw new ArgumentNullException(nameof(workspaceAccessor));
        _todoCreationService = todoCreationService ?? throw new ArgumentNullException(nameof(todoCreationService));
        _todoUpdateService = todoUpdateService ?? throw new ArgumentNullException(nameof(todoUpdateService));
        _todoServiceResolver = todoServiceResolver;
        _workspaceService = workspaceService;
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> CreateAsync(
        TodoCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "todo.create",
            new TodoCreateTransactionPayload(request),
            request.Id,
            async ct =>
            {
                var todoService = _workspaceAccessor.GetTodoService();
                var compensation = GetUsableCompensation(todoService);
                var result = await _todoCreationService.CreateAsync(request, ct).ConfigureAwait(false);
                var createdId = result.Item?.Id ?? request.Id;

                return new MutationExecution(
                    result,
                    compensation is not null && MayHaveAppliedMutation(result)
                        ? rollbackCt => DeleteCreatedOrThrowAsync(compensation, createdId, rollbackCt)
                        : null);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> UpdateAsync(
        string id,
        TodoUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "todo.update",
            new TodoUpdateTransactionPayload(id, request),
            id,
            async ct =>
            {
                var todoService = _workspaceAccessor.GetTodoService();
                var compensation = GetUsableCompensation(todoService);
                TodoCompensationSnapshot? snapshot;
                TodoMutationResult result;
                if (compensation is null || TodoUpdateService.IsIssueTodoId(id))
                {
                    snapshot = compensation is null
                        ? null
                        : await compensation.CaptureForRestoreAsync(id, ct).ConfigureAwait(false);
                    result = await _todoUpdateService.UpdateAsync(id, request, ct).ConfigureAwait(false);
                }
                else
                {
                    var update = await compensation.UpdateWithRestorePointAsync(id, request, ct).ConfigureAwait(false);
                    snapshot = update.Snapshot;
                    result = update.Result;
                }

                return new MutationExecution(
                    result,
                    compensation is not null && snapshot is not null && MayHaveAppliedMutation(result)
                        ? rollbackCt => RestoreOrThrowAsync(compensation, snapshot, rollbackCt)
                        : null);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return ExecuteMutationAsync(
            "todo.delete",
            new TodoDeleteTransactionPayload(id),
            id,
            async ct =>
            {
                var todoService = _workspaceAccessor.GetTodoService();
                var compensation = GetUsableCompensation(todoService);
                TodoCompensationSnapshot? snapshot;
                TodoMutationResult result;
                if (compensation is null)
                {
                    snapshot = null;
                    result = await todoService.DeleteAsync(id, ct).ConfigureAwait(false);
                }
                else
                {
                    var delete = await compensation.DeleteWithRestorePointAsync(id, ct).ConfigureAwait(false);
                    snapshot = delete.Snapshot;
                    result = delete.Result;
                }

                return new MutationExecution(
                    result,
                compensation is not null && snapshot is not null && MayHaveAppliedMutation(result)
                        ? rollbackCt => RestoreOrThrowAsync(compensation, snapshot, rollbackCt)
                        : null);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> MoveAsync(
        string id,
        TodoMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteMutationAsync(
            "todo.move",
            new TodoMoveTransactionPayload(id, request),
            id,
            ct => ExecuteMoveAsync(id, request, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
    {
        var todoService = _workspaceAccessor.GetTodoService();
        if (_coordinator is null)
            return await todoService.RepairProjectionAsync(cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
            return ProjectionRepairUnavailable(status.Message);

        if (RequiresMutationTransactions(status))
        {
            return ProjectionRepairUnavailable(
                "TODO projection repair is not transaction compensated while required turn transactions are active.");
        }

        return await todoService.RepairProjectionAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<TodoMutationResult> ExecuteMutationAsync(
        string operationName,
        object operationBody,
        string id,
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
            return TransactionUnavailable(status.Message);

        var compensation = GetUsableCompensation(_workspaceAccessor.GetTodoService());
        var requiresMutationTransactions = RequiresMutationTransactions(status);
        if (requiresMutationTransactions && IsIssueBackedTodoMutation(id))
        {
            return new TodoMutationResult(
                false,
                $"ISSUE-backed TODO {IssueOperationLabel(operationName)} have external GitHub side effects and are not yet transaction-compensated.",
                FailureKind: TodoMutationFailureKind.Conflict);
        }

        if (requiresMutationTransactions && compensation is null)
        {
            return new TodoMutationResult(
                false,
                "The active TODO provider does not support transaction rollback compensation.",
                FailureKind: TodoMutationFailureKind.Conflict);
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

        if (execution is not null && (IsTransactionSuccess(result) || ShouldReturnLocalFailure(result)))
            return execution.Value.Result;

        return ToTransactionFailure(operationName, id, result);
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
            RollbackAsync = execution.RollbackAsync is null
                ? null
                : async ct => await execution.RollbackAsync(ct).ConfigureAwait(false),
        };

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static async Task<TodoMutationResult> RestoreOrThrowAsync(
        ITodoCompensationService compensation,
        TodoCompensationSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var result = await compensation.RestoreAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? "TODO rollback compensation failed.");
        return result;
    }

    private static async Task<TodoMutationResult> DeleteCreatedOrThrowAsync(
        ITodoCompensationService compensation,
        string id,
        CancellationToken cancellationToken)
    {
        var result = await compensation.DeleteCreatedAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? "TODO create rollback compensation failed.");
        return result;
    }

    private static bool MayHaveAppliedMutation(TodoMutationResult result)
        => result.Success ||
           result is { Item: not null, FailureKind: TodoMutationFailureKind.ExternalSyncFailed or TodoMutationFailureKind.ProjectionFailed };

    private static bool IsIssueBackedTodoMutation(string id)
        => TodoCreationService.IsNewGitHubIssueRequestId(id) || TodoUpdateService.IsIssueTodoId(id);

    private static string IssueOperationLabel(string operationName)
        => operationName switch
        {
            "todo.create" => "creates",
            "todo.delete" => "deletes",
            "todo.move" => "moves",
            _ => "updates",
        };

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldReturnLocalFailure(TurnTransactionResult result)
        => result.MutationResult?.Success == false &&
           (!result.RollbackAttempted || result.RollbackSucceeded);

    private static TodoMutationResult TransactionUnavailable(string? message)
        => new(
            false,
            string.IsNullOrWhiteSpace(message)
                ? "Turn transaction coordinator is degraded."
                : message,
            FailureKind: TodoMutationFailureKind.Conflict);

    private static TodoProjectionRepairResult ProjectionRepairUnavailable(string? message)
    {
        var error = string.IsNullOrWhiteSpace(message)
            ? "Turn transaction coordinator is degraded."
            : message;
        return new TodoProjectionRepairResult(
            false,
            error,
            new TodoProjectionStatusResult(
                "turn-transaction-gate",
                "turn-transaction-gate",
                "TODO.yaml",
                ProjectionTargetExists: false,
                ProjectionConsistent: false,
                RepairRequired: true,
                DateTimeOffset.UtcNow.ToString("O"),
                Message: error));
    }

    private static TodoMutationResult ToTransactionFailure(string operationName, string id, TurnTransactionResult result)
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

        return new TodoMutationResult(
            false,
            $"Turn transaction coordinator did not commit {operationName} for TODO '{id}' in transaction '{transactionId}': {message}",
            FailureKind: TodoMutationFailureKind.Conflict);
    }

    private readonly record struct MutationExecution(
        TodoMutationResult Result,
        Func<CancellationToken, Task<TodoMutationResult>>? RollbackAsync);

    private sealed record TodoCreateTransactionPayload(TodoCreateRequest Request);

    private sealed record TodoDeleteTransactionPayload(string Id);

    private sealed record TodoMoveTransactionPayload(string Id, TodoMoveRequest Request);

    private sealed record TodoUpdateTransactionPayload(string Id, TodoUpdateRequest Request);

    private async Task<MutationExecution> ExecuteMoveAsync(
        string id,
        TodoMoveRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TargetWorkspacePath))
            return Failure("Request body with targetWorkspacePath is required.", TodoMutationFailureKind.Validation);

        if (_workspaceService is null || _todoServiceResolver is null)
        {
            return Failure(
                "TODO move transaction gating requires workspace resolution services.",
                TodoMutationFailureKind.Conflict);
        }

        var sourceService = _workspaceAccessor.GetTodoService();
        var sourceItem = await sourceService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (sourceItem is null)
            return Failure($"Item with id '{id}' not found in source workspace.", TodoMutationFailureKind.NotFound);

        var targetWorkspace = await _workspaceService.GetAsync(request.TargetWorkspacePath, cancellationToken).ConfigureAwait(false);
        if (targetWorkspace is null)
            return Failure($"Target workspace '{request.TargetWorkspacePath}' not found.", TodoMutationFailureKind.Validation);

        var targetService = _todoServiceResolver.Resolve(new WorkspaceContext
        {
            WorkspacePath = targetWorkspace.WorkspacePath,
            WorkspaceName = targetWorkspace.Name,
            DataDirectory = targetWorkspace.DataDirectory,
            TodoFilePath = targetWorkspace.TodoPath,
        });

        var targetCompensation = GetUsableCompensation(targetService);
        var requiresCompensation = _coordinator is not null && RequiresMutationTransactions(_coordinator.GetStatus());
        if (requiresCompensation && targetCompensation is null)
        {
            return Failure(
                "The target TODO provider does not support transaction rollback compensation.",
                TodoMutationFailureKind.Conflict);
        }

        var createResult = await targetService.CreateAsync(ToCreateRequest(sourceItem), cancellationToken).ConfigureAwait(false);
        if (!createResult.Success)
        {
            return Failure(
                $"Failed to create in target workspace: {createResult.Error}",
                createResult.FailureKind == TodoMutationFailureKind.None ? TodoMutationFailureKind.Conflict : createResult.FailureKind);
        }

        var sourceCompensation = GetUsableCompensation(sourceService);
        TodoCompensationSnapshot? sourceSnapshot = null;
        TodoMutationResult deleteResult;
        if (sourceCompensation is not null)
        {
            var delete = await sourceCompensation.DeleteWithRestorePointAsync(id, cancellationToken).ConfigureAwait(false);
            sourceSnapshot = delete.Snapshot;
            deleteResult = delete.Result;
        }
        else
        {
            deleteResult = await sourceService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        }

        var rollback = BuildMoveRollback(
            sourceCompensation,
            sourceSnapshot,
            targetCompensation,
            createResult.Item?.Id ?? sourceItem.Id);
        if (!deleteResult.Success)
        {
            return new MutationExecution(
                new TodoMutationResult(
                    false,
                    $"Failed to delete from source workspace after target creation succeeded: {deleteResult.Error}",
                    createResult.Item,
                    // The item now exists in the target while the source copy could not be removed: the move
                    // partially applied and the persisted state is inconsistent. Classify as ProjectionFailed so
                    // callers surface a server-side 500 (not a client 409 conflict) and the coordinator treats the
                    // mutation as possibly-applied for rollback.
                    deleteResult.FailureKind == TodoMutationFailureKind.None ? TodoMutationFailureKind.ProjectionFailed : deleteResult.FailureKind),
                rollback);
        }

        if (sourceCompensation is not null && sourceSnapshot is null)
        {
            return new MutationExecution(
                new TodoMutationResult(
                    false,
                    $"Source TODO provider did not supply a restore snapshot for move '{id}'.",
                    createResult.Item,
                    TodoMutationFailureKind.Conflict),
                rollback);
        }

        return new MutationExecution(
            new TodoMutationResult(true, Item: createResult.Item ?? sourceItem),
            rollback);
    }

    private static MutationExecution Failure(string error, TodoMutationFailureKind failureKind)
        => new(new TodoMutationResult(false, error, FailureKind: failureKind), null);

    private static ITodoCompensationService? GetUsableCompensation(ITodoService todoService)
    {
        if (todoService is not ITodoCompensationService compensation)
            return null;

        return todoService is ITodoCompensationCapability { SupportsRollbackCompensation: false }
            ? null
            : compensation;
    }

    private static Func<CancellationToken, Task<TodoMutationResult>>? BuildMoveRollback(
        ITodoCompensationService? sourceCompensation,
        TodoCompensationSnapshot? sourceSnapshot,
        ITodoCompensationService? targetCompensation,
        string targetId)
    {
        if (targetCompensation is null && (sourceCompensation is null || sourceSnapshot is null))
            return null;

        return rollbackCt => RollbackMoveOrThrowAsync(
            sourceCompensation,
            sourceSnapshot,
            targetCompensation,
            targetId,
            rollbackCt);
    }

    private static async Task<TodoMutationResult> RollbackMoveOrThrowAsync(
        ITodoCompensationService? sourceCompensation,
        TodoCompensationSnapshot? sourceSnapshot,
        ITodoCompensationService? targetCompensation,
        string targetId,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        TodoMutationResult? sourceRestore = null;

        if (targetCompensation is not null)
        {
            var targetDelete = await targetCompensation.DeleteCreatedAsync(targetId, cancellationToken).ConfigureAwait(false);
            if (!targetDelete.Success)
                errors.Add(targetDelete.Error ?? $"Target rollback delete for TODO '{targetId}' failed.");
        }

        if (sourceCompensation is not null && sourceSnapshot is not null)
        {
            sourceRestore = await sourceCompensation.RestoreAsync(sourceSnapshot, cancellationToken).ConfigureAwait(false);
            if (!sourceRestore.Success)
                errors.Add(sourceRestore.Error ?? "Source rollback restore failed.");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        return sourceRestore ?? new TodoMutationResult(true);
    }

    private static TodoCreateRequest ToCreateRequest(TodoFlatItem item)
        => new()
        {
            Id = item.Id,
            Title = item.Title,
            Section = item.Section,
            Priority = item.Priority,
            Estimate = item.Estimate,
            Description = item.Description,
            TechnicalDetails = item.TechnicalDetails,
            ImplementationTasks = item.ImplementationTasks,
            Note = item.Note,
            Remaining = item.Remaining,
            DependsOn = item.DependsOn,
            FunctionalRequirements = item.FunctionalRequirements,
            TechnicalRequirements = item.TechnicalRequirements,
        };
}
