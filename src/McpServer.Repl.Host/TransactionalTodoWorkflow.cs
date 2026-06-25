using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Repl.Host;

/// <summary>
/// TR-MCP-TXN-001: Decorates REPL TODO create/update/delete workflow mutations with turn transaction gating.
/// </summary>
public sealed class TransactionalTodoWorkflow : ITodoWorkflow
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITodoWorkflow _inner;
    private readonly TodoClient _client;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;
    private long _lastSequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Initializes a new instance of the <see cref="TransactionalTodoWorkflow"/> class.</summary>
    /// <param name="inner">Inner TODO workflow that owns workflow semantics and selection state.</param>
    /// <param name="client">Typed TODO client used for rollback snapshots and compensation.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional turn transaction enforcement options.</param>
    public TransactionalTodoWorkflow(
        ITodoWorkflow inner,
        TodoClient client,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<ITodoQueryResult> QueryAsync(
        string? keyword = null,
        string? priority = null,
        string? section = null,
        string? id = null,
        bool? done = null,
        CancellationToken cancellationToken = default)
        => _inner.QueryAsync(keyword, priority, section, id, done, cancellationToken);

    /// <inheritdoc />
    public Task<ITodoItem> GetAsync(string id, CancellationToken cancellationToken = default)
        => _inner.GetAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task SelectAsync(string id, CancellationToken cancellationToken = default)
        => _inner.SelectAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task<ITodoMutationResult> CreateAsync(
        ITodoCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_coordinator is null)
            return await _inner.CreateAsync(request, cancellationToken).ConfigureAwait(false);

        ITodoMutationResult? mutationResult = null;
        var transaction = BuildTransactionRequest(
            "workflow.todo.create",
            new TodoCreateTransactionPayload(MapCreateRequest(request)));
        var result = await ExecuteAsync(
                transaction,
                async ct =>
                {
                    mutationResult = await _inner.CreateAsync(request, ct).ConfigureAwait(false);
                    return new TurnMutationResult
                    {
                        Success = mutationResult.Success,
                        ResultJson = SerializeMutationResult(mutationResult),
                        RollbackAsync = mutationResult.Success
                            ? rollbackCt => RollbackCreateAsync(mutationResult, rollbackCt)
                            : null,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (mutationResult is not null && (!mutationResult.Success || IsTransactionSuccess(result)))
            return mutationResult;

        throw BuildFailure("workflow.todo.create", result);
    }

    /// <inheritdoc />
    public async Task<ITodoMutationResult> UpdateAsync(
        string id,
        ITodoUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_coordinator is null)
            return await _inner.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);

        return await UpdateCoreAsync(
                "workflow.todo.update",
                id,
                request,
                (_, updateRequest, ct) => _inner.UpdateAsync(id, updateRequest, ct),
                selectedOperation: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ITodoMutationResult> UpdateAsync(
        ITodoUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_coordinator is null)
            return await _inner.UpdateAsync(request, cancellationToken).ConfigureAwait(false);

        var selection = _inner.CurrentSelection()
            ?? throw new InvalidOperationException("No TODO is currently selected");
        return await UpdateCoreAsync(
                "workflow.todo.updateSelected",
                selection.Id,
                request,
                (_, updateRequest, ct) => _inner.UpdateAsync(updateRequest, ct),
                selectedOperation: true,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_coordinator is null)
        {
            await _inner.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return;
        }

        await DeleteCoreAsync(
                "workflow.todo.delete",
                id,
                (_, ct) => _inner.DeleteAsync(id, ct),
                selectedOperation: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_coordinator is null)
        {
            await _inner.DeleteAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var selection = _inner.CurrentSelection()
            ?? throw new InvalidOperationException("No TODO is currently selected");
        await DeleteCoreAsync(
                "workflow.todo.deleteSelected",
                selection.Id,
                (_, ct) => _inner.DeleteAsync(ct),
                selectedOperation: true,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ITodoRequirementsAnalysis> AnalyzeRequirementsAsync(string id, CancellationToken cancellationToken = default)
        => _inner.AnalyzeRequirementsAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task StreamStatusAsync(
        string id,
        Func<IStreamingEvent, Task> eventCallback,
        CancellationToken cancellationToken = default)
        => _inner.StreamStatusAsync(id, eventCallback, cancellationToken);

    /// <inheritdoc />
    public Task StreamPlanAsync(
        string id,
        Func<IStreamingEvent, Task> eventCallback,
        CancellationToken cancellationToken = default)
        => _inner.StreamPlanAsync(id, eventCallback, cancellationToken);

    /// <inheritdoc />
    public Task StreamImplementAsync(
        string id,
        Func<IStreamingEvent, Task> eventCallback,
        CancellationToken cancellationToken = default)
        => _inner.StreamImplementAsync(id, eventCallback, cancellationToken);

    /// <inheritdoc />
    public Task<ITodoProjectionStatus> GetProjectionStatusAsync(string id, CancellationToken cancellationToken = default)
        => _inner.GetProjectionStatusAsync(id, cancellationToken);

    /// <inheritdoc />
    public async Task RepairProjectionAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_coordinator is not null)
        {
            var status = _coordinator.GetStatus();
            if (status.Degraded)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(status.Message)
                        ? "Turn transaction coordinator is degraded."
                        : status.Message);
            }

            if (RequiresMutationTransactions(status))
            {
                throw new InvalidOperationException(
                    "TODO projection repair is not transaction compensated while required turn transactions are active.");
            }
        }

        await _inner.RepairProjectionAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ITodoSelectionState? CurrentSelection()
        => _inner.CurrentSelection();

    private async Task<ITodoMutationResult> UpdateCoreAsync(
        string operationName,
        string id,
        ITodoUpdateRequest request,
        Func<string, ITodoUpdateRequest, CancellationToken, Task<ITodoMutationResult>> update,
        bool selectedOperation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ITodoMutationResult? mutationResult = null;
        TodoFlatItem? snapshot = null;
        var transaction = BuildTransactionRequest(
            operationName,
            new TodoUpdateTransactionPayload(id, selectedOperation, MapUpdateRequest(request)));
        var result = await ExecuteAsync(
                transaction,
                async ct =>
                {
                    snapshot = await _client.GetAsync(id, ct).ConfigureAwait(false);
                    mutationResult = await update(id, request, ct).ConfigureAwait(false);
                    return new TurnMutationResult
                    {
                        Success = mutationResult.Success,
                        ResultJson = SerializeMutationResult(mutationResult),
                        RollbackAsync = mutationResult.Success
                            ? rollbackCt => RollbackUpdateAsync(snapshot, selectedOperation, rollbackCt)
                            : null,
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (mutationResult is not null && (!mutationResult.Success || IsTransactionSuccess(result)))
            return mutationResult;

        throw BuildFailure(operationName, result);
    }

    private async Task DeleteCoreAsync(
        string operationName,
        string id,
        Func<string, CancellationToken, Task> delete,
        bool selectedOperation,
        CancellationToken cancellationToken)
    {
        var deleteExecuted = false;
        TodoFlatItem? snapshot = null;
        var transaction = BuildTransactionRequest(
            operationName,
            new TodoDeleteTransactionPayload(id, selectedOperation));
        var result = await ExecuteAsync(
                transaction,
                async ct =>
                {
                    snapshot = await _client.GetAsync(id, ct).ConfigureAwait(false);
                    await delete(id, ct).ConfigureAwait(false);
                    deleteExecuted = true;
                    return new TurnMutationResult
                    {
                        Success = true,
                        ResultJson = JsonSerializer.Serialize(new { Success = true, Id = id }, JsonOptions),
                        RollbackAsync = rollbackCt => RollbackDeleteAsync(snapshot, selectedOperation, rollbackCt),
                    };
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (deleteExecuted && IsTransactionSuccess(result))
            return;

        throw BuildFailure(operationName, result);
    }

    private async Task RollbackDeleteAsync(
        TodoFlatItem snapshot,
        bool selectedOperation,
        CancellationToken cancellationToken)
    {
        var rollback = await _client.CreateAsync(ToCreateRequest(snapshot), cancellationToken).ConfigureAwait(false);
        if (!rollback.Success)
            throw new InvalidOperationException(rollback.Error ?? $"Rollback recreate for TODO '{snapshot.Id}' failed.");

        if (selectedOperation)
            await ReselectAsync(snapshot.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TurnTransactionResult> ExecuteAsync(
        TurnTransactionRequest request,
        Func<CancellationToken, Task<TurnMutationResult>> mutation,
        CancellationToken cancellationToken)
    {
        if (_coordinator is null)
            throw new InvalidOperationException("Turn transaction coordinator is not available.");

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? string.Empty,
                Status = "degraded",
                Reason = status.LastReason,
                Degraded = true,
                Message = string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message,
            };
        }

        return await _coordinator.ExecuteAsync(request, mutation, cancellationToken).ConfigureAwait(false);
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

    private bool RequiresMutationTransactions(McpServer.TransactionSecurity.Models.TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);

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

    private async Task RollbackCreateAsync(ITodoMutationResult result, CancellationToken cancellationToken)
    {
        var id = result.Item.Id;
        var rollback = await _client.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!rollback.Success && rollback.FailureKind != TodoMutationFailureKind.NotFound)
            throw new InvalidOperationException(rollback.Error ?? $"Rollback delete for TODO '{id}' failed.");
    }

    private async Task RollbackUpdateAsync(
        TodoFlatItem snapshot,
        bool selectedOperation,
        CancellationToken cancellationToken)
    {
        var rollback = await _client.UpdateAsync(snapshot.Id, ToUpdateRequest(snapshot), cancellationToken).ConfigureAwait(false);
        if (!rollback.Success)
            throw new InvalidOperationException(rollback.Error ?? $"Rollback update for TODO '{snapshot.Id}' failed.");

        if (selectedOperation)
            await ReselectAsync(snapshot.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReselectAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _inner.SelectAsync(id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException($"Rollback restored TODO '{id}' but selection refresh failed.", ex);
        }
    }

    private static TodoCreateRequest MapCreateRequest(ITodoCreateRequest request)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Section = request.Section,
            Priority = request.Priority,
            Estimate = request.Estimate,
            Description = request.Description?.ToList(),
            TechnicalDetails = request.TechnicalDetails?.ToList(),
            ImplementationTasks = request.ImplementationTasks?.Select(ToFlatTask).ToList(),
            Note = request.Note,
            Remaining = request.Remaining,
            DependsOn = request.DependsOn?.ToList(),
            FunctionalRequirements = request.FunctionalRequirements?.ToList(),
            TechnicalRequirements = request.TechnicalRequirements?.ToList(),
        };

    private static TodoUpdateRequest MapUpdateRequest(ITodoUpdateRequest request)
        => new()
        {
            Title = request.Title,
            Priority = request.Priority,
            Section = request.Section,
            Done = request.Done,
            Estimate = request.Estimate,
            Description = request.Description?.ToList(),
            TechnicalDetails = request.TechnicalDetails?.ToList(),
            ImplementationTasks = request.ImplementationTasks?.Select(ToFlatTask).ToList(),
            Note = request.Note,
            CompletedDate = request.CompletedDate,
            DoneSummary = request.DoneSummary,
            Remaining = request.Remaining,
            DependsOn = request.DependsOn?.ToList(),
            FunctionalRequirements = request.FunctionalRequirements?.ToList(),
            TechnicalRequirements = request.TechnicalRequirements?.ToList(),
        };

    private static TodoUpdateRequest ToUpdateRequest(TodoFlatItem item)
        => new()
        {
            Title = item.Title,
            Priority = item.Priority,
            Section = item.Section,
            Done = item.Done,
            Estimate = item.Estimate,
            Description = item.Description?.ToList(),
            TechnicalDetails = item.TechnicalDetails?.ToList(),
            ImplementationTasks = item.ImplementationTasks?.Select(CloneTask).ToList(),
            Note = item.Note,
            CompletedDate = item.CompletedDate,
            DoneSummary = item.DoneSummary,
            Remaining = item.Remaining,
            Reference = item.Reference,
            Phase = item.Phase,
            DependsOn = item.DependsOn?.ToList(),
            FunctionalRequirements = item.FunctionalRequirements?.ToList(),
            TechnicalRequirements = item.TechnicalRequirements?.ToList(),
        };

    private static TodoCreateRequest ToCreateRequest(TodoFlatItem item)
        => new()
        {
            Id = item.Id,
            Title = item.Title,
            Section = item.Section,
            Priority = item.Priority,
            Estimate = item.Estimate,
            Description = item.Description?.ToList(),
            TechnicalDetails = item.TechnicalDetails?.ToList(),
            ImplementationTasks = item.ImplementationTasks?.Select(CloneTask).ToList(),
            Note = item.Note,
            Remaining = item.Remaining,
            DependsOn = item.DependsOn?.ToList(),
            FunctionalRequirements = item.FunctionalRequirements?.ToList(),
            TechnicalRequirements = item.TechnicalRequirements?.ToList(),
        };

    private static TodoFlatTask ToFlatTask(ITodoSubtask task)
        => new()
        {
            Task = task.Task,
            Done = task.Done,
        };

    private static TodoFlatTask CloneTask(TodoFlatTask task)
        => new()
        {
            Task = task.Task,
            Done = task.Done,
        };

    private static string SerializeMutationResult(ITodoMutationResult result)
    {
        if (!result.Success)
            return JsonSerializer.Serialize(new { result.Success }, JsonOptions);

        return JsonSerializer.Serialize(new
        {
            result.Success,
            Item = ToFlatItem(result.Item),
        }, JsonOptions);
    }

    private static TodoFlatItem ToFlatItem(ITodoItem item)
        => new()
        {
            Id = item.Id,
            Title = item.Title,
            Section = item.Section,
            Priority = item.Priority,
            Done = item.Done,
            Estimate = item.Estimate,
            Note = item.Note,
            Description = item.Description.ToList(),
            TechnicalDetails = item.TechnicalDetails.ToList(),
            ImplementationTasks = item.ImplementationTasks.Select(ToFlatTask).ToList(),
            CompletedDate = item.CompletedDate,
            DoneSummary = item.DoneSummary,
            Remaining = item.Remaining,
            PriorityNote = item.PriorityNote,
            Reference = item.Reference,
            DependsOn = item.DependsOn.ToList(),
            FunctionalRequirements = item.FunctionalRequirements.ToList(),
            TechnicalRequirements = item.TechnicalRequirements.ToList(),
        };

    private static bool IsTransactionSuccess(TurnTransactionResult result)
        => string.Equals(result.Status, "committed", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(result.Status, "bypassed", StringComparison.OrdinalIgnoreCase);

    private static InvalidOperationException BuildFailure(string operationName, TurnTransactionResult result)
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
            $"Turn transaction coordinator did not commit {operationName} '{transactionId}': {message}");
    }

    private sealed record TodoCreateTransactionPayload(TodoCreateRequest Request);

    private sealed record TodoDeleteTransactionPayload(string Id, bool SelectedOperation);

    private sealed record TodoUpdateTransactionPayload(string Id, bool SelectedOperation, TodoUpdateRequest Request);
}
