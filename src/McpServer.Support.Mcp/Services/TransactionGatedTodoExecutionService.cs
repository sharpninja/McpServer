using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Executes file-state TODO execution mutations through the turn transaction coordinator.
/// </summary>
public sealed class TransactionGatedTodoExecutionService : ITodoExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITodoExecutionService _inner;
    private readonly ITodoExecutionStateCompensation? _compensation;
    private readonly ITodoExecutionPlanCompensation? _planCompensation;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedTodoExecutionService"/> class.</summary>
    /// <param name="inner">Underlying TODO execution service.</param>
    /// <param name="compensation">Optional file-state rollback compensation service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction options.</param>
    public TransactionGatedTodoExecutionService(
        ITodoExecutionService inner,
        ITodoExecutionStateCompensation? compensation = null,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _compensation = compensation;
        _planCompensation = compensation as ITodoExecutionPlanCompensation ?? inner as ITodoExecutionPlanCompensation;
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<CreateIterationPhaseResult> CreateIterationPhaseAsync(
        string workspacePath,
        CreateIterationPhaseRequest request,
        CancellationToken cancellationToken = default)
        => ExecuteFileStateMutationAsync(
            "todo.execution.phase.create",
            workspacePath,
            new TodoExecutionCreatePhasePayload(workspacePath, request),
            ct => _inner.CreateIterationPhaseAsync(workspacePath, request, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<CreateTodosFromPlanResult> CreateTodosFromPlanAsync(
        string workspacePath,
        CreateTodosFromPlanRequest request,
        CancellationToken cancellationToken = default)
        => ExecutePlanMutationAsync(
            "todo.execution.plan.todos.create",
            workspacePath,
            new TodoExecutionCreateTodosFromPlanPayload(workspacePath, request),
            ct => _inner.CreateTodosFromPlanAsync(workspacePath, request, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<ActiveTodoResult?> GetActiveTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
        => _inner.GetActiveTodoAsync(workspacePath, cancellationToken);

    /// <inheritdoc />
    public Task<TodoExecutionRecord?> GetTodoAsync(string workspacePath, string todoId, CancellationToken cancellationToken = default)
        => _inner.GetTodoAsync(workspacePath, todoId, cancellationToken);

    /// <inheritdoc />
    public Task<ActiveTodoResult?> GetNextReadyTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
        => _inner.GetNextReadyTodoAsync(workspacePath, cancellationToken);

    /// <inheritdoc />
    public Task<ActiveTodoContext?> GetExecutionContextAsync(
        string workspacePath,
        string todoId,
        int requirementSnippetLimit = 5,
        int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default)
        => _inner.GetExecutionContextAsync(workspacePath, todoId, requirementSnippetLimit, sessionTurnSummaryLimit, cancellationToken);

    /// <inheritdoc />
    public Task<TodoDeltaContext?> GetDeltaContextAsync(
        string workspacePath,
        string todoId,
        string? sinceCheckpointId,
        CancellationToken cancellationToken = default)
        => _inner.GetDeltaContextAsync(workspacePath, todoId, sinceCheckpointId, cancellationToken);

    /// <inheritdoc />
    public Task<SetTodoTestPlanResult> SetTestPlanAsync(
        string workspacePath,
        string todoId,
        SetTodoTestPlanRequest request,
        CancellationToken cancellationToken = default)
        => ExecuteFileStateMutationAsync(
            "todo.execution.testPlan.set",
            workspacePath,
            new TodoExecutionTodoPayload<SetTodoTestPlanRequest>(workspacePath, todoId, request),
            ct => _inner.SetTestPlanAsync(workspacePath, todoId, request, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<UpdateTodoStatusResult> UpdateStatusAsync(
        string workspacePath,
        string todoId,
        UpdateTodoStatusRequest request,
        CancellationToken cancellationToken = default)
        => ExecuteFileStateMutationAsync(
            "todo.execution.status.update",
            workspacePath,
            new TodoExecutionTodoPayload<UpdateTodoStatusRequest>(workspacePath, todoId, request),
            ct => _inner.UpdateStatusAsync(workspacePath, todoId, request, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<AppendTodoCheckpointResult> AppendCheckpointAsync(
        string workspacePath,
        string todoId,
        AppendTodoCheckpointRequest request,
        CancellationToken cancellationToken = default)
        => ExecuteFileStateMutationAsync(
            "todo.execution.checkpoint.append",
            workspacePath,
            new TodoExecutionTodoPayload<AppendTodoCheckpointRequest>(workspacePath, todoId, request),
            ct => _inner.AppendCheckpointAsync(workspacePath, todoId, request, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<RecordTodoValidationResultResult> RecordValidationResultAsync(
        string workspacePath,
        string todoId,
        RecordTodoValidationResultRequest request,
        CancellationToken cancellationToken = default)
        => ExecuteFileStateMutationAsync(
            "todo.execution.validation.record",
            workspacePath,
            new TodoExecutionTodoPayload<RecordTodoValidationResultRequest>(workspacePath, todoId, request),
            ct => _inner.RecordValidationResultAsync(workspacePath, todoId, request, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<LinkTodoToSessionTurnsResult> LinkTodoToSessionTurnsAsync(
        string workspacePath,
        string todoId,
        LinkTodoToSessionTurnsRequest request,
        CancellationToken cancellationToken = default)
        => ExecuteFileStateMutationAsync(
            "todo.execution.sessionTurns.link",
            workspacePath,
            new TodoExecutionTodoPayload<LinkTodoToSessionTurnsRequest>(workspacePath, todoId, request),
            ct => _inner.LinkTodoToSessionTurnsAsync(workspacePath, todoId, request, ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<AdbStepResult> AdbStepAsync(
        string workspacePath,
        AdbStepRequest request,
        CancellationToken cancellationToken = default)
        => _inner.AdbStepAsync(workspacePath, request, cancellationToken);

    private async Task<T> ExecuteFileStateMutationAsync<T>(
        string operationName,
        string workspacePath,
        object operationBody,
        Func<CancellationToken, Task<T>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
            return await mutation(cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message);

        var requiresMutationTransactions = RequiresMutationTransactions(status);
        if (requiresMutationTransactions && _compensation is null)
            throw new InvalidOperationException("TODO execution state provider does not support transaction rollback compensation.");

        T? mutationResult = default;
        var hasMutationResult = false;
        var transaction = BuildTransactionRequest(operationName, operationBody);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    TodoExecutionStateSnapshot? snapshot = null;
                    if (_compensation is not null)
                        snapshot = await _compensation.CaptureStateAsync(workspacePath, ct).ConfigureAwait(false);

                    mutationResult = await mutation(ct).ConfigureAwait(false);
                    hasMutationResult = true;
                    return new TurnMutationResult
                    {
                        Success = true,
                        ResultJson = JsonSerializer.Serialize(mutationResult, JsonOptions),
                        RollbackAsync = snapshot is null
                            ? null
                            : rollbackCt => RestoreStateOrThrowAsync(snapshot, rollbackCt),
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (hasMutationResult && IsTransactionSuccess(result))
            return mutationResult!;

        throw ToTransactionFailure(operationName, result);
    }

    private async Task<CreateTodosFromPlanResult> ExecutePlanMutationAsync(
        string operationName,
        string workspacePath,
        object operationBody,
        Func<CancellationToken, Task<CreateTodosFromPlanResult>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
            return await mutation(cancellationToken).ConfigureAwait(false);

        var status = _coordinator.GetStatus();
        if (status.Degraded)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message);

        var requiresMutationTransactions = RequiresMutationTransactions(status);
        if (requiresMutationTransactions && _planCompensation is null)
            throw new InvalidOperationException("TODO execution plan provider does not support cross-store transaction rollback compensation.");

        if (requiresMutationTransactions && _planCompensation is not null)
            await _planCompensation.VerifyPlanTodoCompensationAsync(workspacePath, cancellationToken).ConfigureAwait(false);

        CreateTodosFromPlanResult? mutationResult = null;
        var hasMutationResult = false;
        var transaction = BuildTransactionRequest(operationName, operationBody);
        var result = await _coordinator.ExecuteAsync(
                transaction,
                async ct =>
                {
                    TodoExecutionStateSnapshot? snapshot = null;
                    if (_planCompensation is not null)
                    {
                        try
                        {
                            if (!requiresMutationTransactions)
                                await _planCompensation.VerifyPlanTodoCompensationAsync(workspacePath, ct).ConfigureAwait(false);

                            snapshot = await _planCompensation.CaptureStateAsync(workspacePath, ct).ConfigureAwait(false);
                        }
                        catch (InvalidOperationException) when (!requiresMutationTransactions)
                        {
                            snapshot = null;
                        }
                    }

                    mutationResult = await mutation(ct).ConfigureAwait(false);
                    hasMutationResult = true;
                    return new TurnMutationResult
                    {
                        Success = true,
                        ResultJson = JsonSerializer.Serialize(mutationResult, JsonOptions),
                        RollbackAsync = snapshot is null || _planCompensation is null
                            ? null
                            : rollbackCt => _planCompensation.RollbackCreatedPlanTodosAsync(
                                workspacePath,
                                mutationResult.TodoIds,
                                snapshot,
                                rollbackCt),
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (hasMutationResult && IsTransactionSuccess(result))
            return mutationResult!;

        throw ToTransactionFailure(operationName, result);
    }

    private async Task RestoreStateOrThrowAsync(TodoExecutionStateSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (_compensation is null)
            throw new InvalidOperationException("TODO execution state provider does not support transaction rollback compensation.");

        await _compensation.RestoreStateAsync(snapshot, cancellationToken).ConfigureAwait(false);
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

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static InvalidOperationException ToTransactionFailure(string operationName, TurnTransactionResult result)
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

        return new InvalidOperationException(
            $"Turn transaction coordinator did not commit {operationName} in transaction '{transactionId}': {message}");
    }

    private sealed record TodoExecutionCreatePhasePayload(string WorkspacePath, CreateIterationPhaseRequest Request);

    private sealed record TodoExecutionCreateTodosFromPlanPayload(string WorkspacePath, CreateTodosFromPlanRequest Request);

    private sealed record TodoExecutionTodoPayload<TRequest>(string WorkspacePath, string TodoId, TRequest Request);
}
