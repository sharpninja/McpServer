using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;
using Xunit;
using TxnFailureReason = McpServer.TransactionSecurity.Models.TransactionFailureReason;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: TODO execution file-state mutation transaction gate tests.
/// </summary>
public sealed class TransactionGatedTodoExecutionServiceTests
{
    private const string WorkspacePath = @"F:\GitHub\McpServer";

    /// <summary>File-state TODO execution mutations execute inside the coordinator and return after commit.</summary>
    [Fact]
    public async Task UpdateStatusAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsResult()
    {
        var inner = new RecordingTodoExecutionService();
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var result = await sut.UpdateStatusAsync(
                WorkspacePath,
                "EXEC-TODO-001",
                new UpdateTodoStatusRequest { TargetStatus = TodoExecutionStatus.Implementing },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("EXEC-TODO-001", result.TodoId);
        Assert.Equal(TodoExecutionStatus.Implementing, result.CurrentStatus);
        Assert.Equal(1, inner.UpdateStatusCalls);
        Assert.Equal(1, inner.CaptureCalls);
        Assert.Equal(0, inner.RestoreCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("todo.execution.status.update", coordinator.Request.OperationName);
        Assert.Contains("\"todoId\":\"EXEC-TODO-001\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>A pre-mutation coordinator rejection prevents TODO execution state mutation.</summary>
    [Fact]
    public async Task UpdateStatusAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotMutate()
    {
        var inner = new RecordingTodoExecutionService();
        var coordinator = new CapturingCoordinator
        {
            RejectBeforeMutation = true,
            Status = "rejected",
            Reason = TxnFailureReason.KeyServerUnavailable,
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.UpdateStatusAsync(
                    WorkspacePath,
                    "EXEC-TODO-002",
                    new UpdateTodoStatusRequest { TargetStatus = TodoExecutionStatus.Implementing },
                    CancellationToken.None))
            .ConfigureAwait(true);

        Assert.Contains("did not commit todo.execution.status.update", ex.Message, StringComparison.Ordinal);
        Assert.Equal(0, inner.UpdateStatusCalls);
        Assert.Equal(0, inner.RestoreCalls);
    }

    /// <summary>A post-mutation commit failure restores the pre-mutation TODO execution state snapshot.</summary>
    [Fact]
    public async Task AppendCheckpointAsync_WhenCommitFailsAfterMutation_RestoresStateSnapshot()
    {
        var snapshot = new TodoExecutionStateSnapshot(WorkspacePath, Exists: true, ContentJson: "{\"before\":true}");
        var inner = new RecordingTodoExecutionService { Snapshot = snapshot };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.AppendCheckpointAsync(
                    WorkspacePath,
                    "EXEC-TODO-003",
                    new AppendTodoCheckpointRequest
                    {
                        Kind = TodoCheckpointKind.ImplementationProgress,
                        Summary = "implemented slice",
                    },
                    CancellationToken.None))
            .ConfigureAwait(true);

        Assert.Contains("Rollback completed", ex.Message, StringComparison.Ordinal);
        Assert.Equal(1, inner.AppendCheckpointCalls);
        Assert.Equal(1, inner.RestoreCalls);
        Assert.Same(snapshot, inner.RestoredSnapshot);
    }

    /// <summary>Rollback restore failures are surfaced as coordinator rollback failures.</summary>
    [Fact]
    public async Task SetTestPlanAsync_WhenRollbackFails_ReportsRollbackFailure()
    {
        var inner = new RecordingTodoExecutionService
        {
            RestoreExceptionMessage = "state restore failed",
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.SetTestPlanAsync(
                    WorkspacePath,
                    "EXEC-TODO-004",
                    new SetTodoTestPlanRequest
                    {
                        UnitTestsDefined = true,
                        TestFilePaths = ["tests/TodoExecutionTests.cs"],
                    },
                    CancellationToken.None))
            .ConfigureAwait(true);

        Assert.Contains("Rollback failed", ex.Message, StringComparison.Ordinal);
        Assert.Contains("state restore failed", ex.Message, StringComparison.Ordinal);
        Assert.Equal(1, inner.SetTestPlanCalls);
        Assert.Equal(1, inner.RestoreCalls);
    }

    /// <summary>CreateTodosFromPlan signs and commits the cross-store plan expansion before returning created TODO ids.</summary>
    [Fact]
    public async Task CreateTodosFromPlanAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsCreatedTodos()
    {
        var inner = new RecordingTodoExecutionService();
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(inner, coordinator);

        var result = await sut.CreateTodosFromPlanAsync(
                WorkspacePath,
                new CreateTodosFromPlanRequest
                {
                    PhaseId = "PHASE-001",
                    PlanId = "PLAN-001",
                    Todos =
                    [
                        new PlanTodoInput
                        {
                            Title = "Slice",
                            Goal = "Implement slice",
                            Summary = "Implement the slice",
                        },
                    ],
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("PHASE-001", result.PhaseId);
        Assert.Equal(["EXEC-TODO-001"], result.TodoIds);
        Assert.Equal(1, inner.CreateTodosFromPlanCalls);
        Assert.Equal(1, inner.CaptureCalls);
        Assert.Equal(0, inner.RollbackPlanCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("todo.execution.plan.todos.create", coordinator.Request.OperationName);
        Assert.Contains("\"phaseId\":\"PHASE-001\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>CreateTodosFromPlan rollback deletes created legacy TODOs and restores the execution state snapshot.</summary>
    [Fact]
    public async Task CreateTodosFromPlanAsync_WhenCommitFailsAfterMutation_RollsBackCreatedTodosAndState()
    {
        var snapshot = new TodoExecutionStateSnapshot(WorkspacePath, Exists: true, ContentJson: "{\"before\":true}");
        var inner = new RecordingTodoExecutionService { Snapshot = snapshot };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateSut(inner, coordinator);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.CreateTodosFromPlanAsync(
                    WorkspacePath,
                    new CreateTodosFromPlanRequest
                    {
                        PhaseId = "PHASE-001",
                        PlanId = "PLAN-001",
                        Todos =
                        [
                            new PlanTodoInput
                            {
                                Title = "Slice",
                                Goal = "Implement slice",
                                Summary = "Implement the slice",
                            },
                        ],
                    },
                    CancellationToken.None))
            .ConfigureAwait(true);

        Assert.Contains("Rollback completed", ex.Message, StringComparison.Ordinal);
        Assert.Equal(1, inner.CreateTodosFromPlanCalls);
        Assert.Equal(1, inner.RollbackPlanCalls);
        Assert.Same(snapshot, inner.RolledBackPlanSnapshot);
        Assert.Equal(["EXEC-TODO-001"], inner.RolledBackPlanTodoIds);
    }

    /// <summary>Required transaction mode fails closed before plan expansion when legacy TODO compensation is unavailable.</summary>
    [Fact]
    public async Task CreateTodosFromPlanAsync_WhenPlanCompensationMissingAndRequired_DoesNotMutate()
    {
        var inner = new StateOnlyTodoExecutionService();
        var coordinator = new CapturingCoordinator();
        var sut = new TransactionGatedTodoExecutionService(
            inner,
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.CreateTodosFromPlanAsync(
                    WorkspacePath,
                    new CreateTodosFromPlanRequest
                    {
                        PhaseId = "PHASE-001",
                        PlanId = "PLAN-001",
                        Todos =
                        [
                            new PlanTodoInput
                            {
                                Title = "Slice",
                                Goal = "Implement slice",
                                Summary = "Implement the slice",
                            },
                        ],
                    },
                    CancellationToken.None))
            .ConfigureAwait(true);

        Assert.Contains("does not support cross-store transaction rollback compensation", ex.Message, StringComparison.Ordinal);
        Assert.Equal(0, inner.CreateTodosFromPlanCalls);
        Assert.Null(coordinator.Request);
    }

    private static TransactionGatedTodoExecutionService CreateSut(
        RecordingTodoExecutionService inner,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
        => new(
            inner,
            inner,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));

    private sealed class RecordingTodoExecutionService : ITodoExecutionService, ITodoExecutionPlanCompensation
    {
        public TodoExecutionStateSnapshot Snapshot { get; init; } = new(WorkspacePath, Exists: true, ContentJson: "{\"before\":true}");

        public string? RestoreExceptionMessage { get; init; }

        public int CaptureCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public int UpdateStatusCalls { get; private set; }

        public int AppendCheckpointCalls { get; private set; }

        public int SetTestPlanCalls { get; private set; }

        public int CreateTodosFromPlanCalls { get; private set; }

        public int RollbackPlanCalls { get; private set; }

        public TodoExecutionStateSnapshot? RestoredSnapshot { get; private set; }

        public TodoExecutionStateSnapshot? RolledBackPlanSnapshot { get; private set; }

        public IReadOnlyList<string>? RolledBackPlanTodoIds { get; private set; }

        public Task<TodoExecutionStateSnapshot> CaptureStateAsync(string workspacePath, CancellationToken cancellationToken = default)
        {
            CaptureCalls++;
            return Task.FromResult(Snapshot);
        }

        public Task RestoreStateAsync(TodoExecutionStateSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            RestoredSnapshot = snapshot;
            if (!string.IsNullOrWhiteSpace(RestoreExceptionMessage))
                throw new InvalidOperationException(RestoreExceptionMessage);
            return Task.CompletedTask;
        }

        public Task VerifyPlanTodoCompensationAsync(string workspacePath, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackCreatedPlanTodosAsync(
            string workspacePath,
            IReadOnlyList<string> createdTodoIds,
            TodoExecutionStateSnapshot stateSnapshot,
            CancellationToken cancellationToken = default)
        {
            RollbackPlanCalls++;
            RolledBackPlanTodoIds = createdTodoIds;
            RolledBackPlanSnapshot = stateSnapshot;
            if (!string.IsNullOrWhiteSpace(RestoreExceptionMessage))
                throw new InvalidOperationException(RestoreExceptionMessage);
            return Task.CompletedTask;
        }

        public Task<CreateIterationPhaseResult> CreateIterationPhaseAsync(
            string workspacePath,
            CreateIterationPhaseRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new CreateIterationPhaseResult
            {
                PhaseId = "PHASE-001",
                Status = TodoIterationPhaseStatus.Planning,
            });

        public Task<CreateTodosFromPlanResult> CreateTodosFromPlanAsync(
            string workspacePath,
            CreateTodosFromPlanRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateTodosFromPlanCalls++;
            return Task.FromResult(new CreateTodosFromPlanResult
            {
                PhaseId = request.PhaseId,
                TodoIds = ["EXEC-TODO-001"],
            });
        }

        public Task<ActiveTodoResult?> GetActiveTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
            => Task.FromResult<ActiveTodoResult?>(null);

        public Task<TodoExecutionRecord?> GetTodoAsync(string workspacePath, string todoId, CancellationToken cancellationToken = default)
            => Task.FromResult<TodoExecutionRecord?>(null);

        public Task<ActiveTodoResult?> GetNextReadyTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
            => Task.FromResult<ActiveTodoResult?>(null);

        public Task<ActiveTodoContext?> GetExecutionContextAsync(
            string workspacePath,
            string todoId,
            int requirementSnippetLimit = 5,
            int sessionTurnSummaryLimit = 5,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ActiveTodoContext?>(null);

        public Task<TodoDeltaContext?> GetDeltaContextAsync(
            string workspacePath,
            string todoId,
            string? sinceCheckpointId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<TodoDeltaContext?>(null);

        public Task<SetTodoTestPlanResult> SetTestPlanAsync(
            string workspacePath,
            string todoId,
            SetTodoTestPlanRequest request,
            CancellationToken cancellationToken = default)
        {
            SetTestPlanCalls++;
            return Task.FromResult(new SetTodoTestPlanResult
            {
                TodoId = todoId,
                Status = TodoExecutionStatus.TestReady,
            });
        }

        public Task<UpdateTodoStatusResult> UpdateStatusAsync(
            string workspacePath,
            string todoId,
            UpdateTodoStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            UpdateStatusCalls++;
            return Task.FromResult(new UpdateTodoStatusResult
            {
                TodoId = todoId,
                PreviousStatus = TodoExecutionStatus.TestReady,
                CurrentStatus = request.TargetStatus,
            });
        }

        public Task<AppendTodoCheckpointResult> AppendCheckpointAsync(
            string workspacePath,
            string todoId,
            AppendTodoCheckpointRequest request,
            CancellationToken cancellationToken = default)
        {
            AppendCheckpointCalls++;
            return Task.FromResult(new AppendTodoCheckpointResult
            {
                CheckpointId = "CHK-001",
                TodoId = todoId,
            });
        }

        public Task<RecordTodoValidationResultResult> RecordValidationResultAsync(
            string workspacePath,
            string todoId,
            RecordTodoValidationResultRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RecordTodoValidationResultResult
            {
                TodoId = todoId,
                ValidationState = new TodoValidationState
                {
                    LastResult = request.Result,
                },
            });

        public Task<LinkTodoToSessionTurnsResult> LinkTodoToSessionTurnsAsync(
            string workspacePath,
            string todoId,
            LinkTodoToSessionTurnsRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new LinkTodoToSessionTurnsResult
            {
                TodoId = todoId,
                SessionTurnIds = request.SessionTurnIds ?? [],
            });

        public Task<AdbStepResult> AdbStepAsync(
            string workspacePath,
            AdbStepRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AdbStepResult
            {
                Success = true,
                Action = request.Action,
                TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
            });
    }

    private sealed class StateOnlyTodoExecutionService : ITodoExecutionService, ITodoExecutionStateCompensation
    {
        public int CreateTodosFromPlanCalls { get; private set; }

        public Task<TodoExecutionStateSnapshot> CaptureStateAsync(string workspacePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoExecutionStateSnapshot(workspacePath, Exists: false, ContentJson: null));

        public Task RestoreStateAsync(TodoExecutionStateSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<CreateIterationPhaseResult> CreateIterationPhaseAsync(
            string workspacePath,
            CreateIterationPhaseRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new CreateIterationPhaseResult { PhaseId = "PHASE-001" });

        public Task<CreateTodosFromPlanResult> CreateTodosFromPlanAsync(
            string workspacePath,
            CreateTodosFromPlanRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateTodosFromPlanCalls++;
            return Task.FromResult(new CreateTodosFromPlanResult { PhaseId = request.PhaseId, TodoIds = ["EXEC-TODO-001"] });
        }

        public Task<ActiveTodoResult?> GetActiveTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
            => Task.FromResult<ActiveTodoResult?>(null);

        public Task<TodoExecutionRecord?> GetTodoAsync(string workspacePath, string todoId, CancellationToken cancellationToken = default)
            => Task.FromResult<TodoExecutionRecord?>(null);

        public Task<ActiveTodoResult?> GetNextReadyTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
            => Task.FromResult<ActiveTodoResult?>(null);

        public Task<ActiveTodoContext?> GetExecutionContextAsync(
            string workspacePath,
            string todoId,
            int requirementSnippetLimit = 5,
            int sessionTurnSummaryLimit = 5,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ActiveTodoContext?>(null);

        public Task<TodoDeltaContext?> GetDeltaContextAsync(
            string workspacePath,
            string todoId,
            string? sinceCheckpointId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<TodoDeltaContext?>(null);

        public Task<SetTodoTestPlanResult> SetTestPlanAsync(
            string workspacePath,
            string todoId,
            SetTodoTestPlanRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new SetTodoTestPlanResult { TodoId = todoId });

        public Task<UpdateTodoStatusResult> UpdateStatusAsync(
            string workspacePath,
            string todoId,
            UpdateTodoStatusRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new UpdateTodoStatusResult { TodoId = todoId, CurrentStatus = request.TargetStatus });

        public Task<AppendTodoCheckpointResult> AppendCheckpointAsync(
            string workspacePath,
            string todoId,
            AppendTodoCheckpointRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AppendTodoCheckpointResult { CheckpointId = "CHK-001", TodoId = todoId });

        public Task<RecordTodoValidationResultResult> RecordValidationResultAsync(
            string workspacePath,
            string todoId,
            RecordTodoValidationResultRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RecordTodoValidationResultResult { TodoId = todoId });

        public Task<LinkTodoToSessionTurnsResult> LinkTodoToSessionTurnsAsync(
            string workspacePath,
            string todoId,
            LinkTodoToSessionTurnsRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new LinkTodoToSessionTurnsResult { TodoId = todoId });

        public Task<AdbStepResult> AdbStepAsync(
            string workspacePath,
            AdbStepRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AdbStepResult { Success = true, Action = request.Action });
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public bool RejectBeforeMutation { get; init; }

        public string Status { get; init; } = "committed";

        public TxnFailureReason Reason { get; init; } = TxnFailureReason.None;

        public string? Message { get; init; }

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            if (RejectBeforeMutation)
                return BuildResult(null, rollbackAttempted: false, rollbackSucceeded: false);

            var mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
            if (string.Equals(Status, "committed", StringComparison.OrdinalIgnoreCase))
                return BuildResult(mutationResult, rollbackAttempted: false, rollbackSucceeded: false);

            var rollback = await RunRollbackAsync(mutationResult, cancellationToken).ConfigureAwait(false);
            return BuildResult(mutationResult, rollback.Attempted, rollback.Succeeded, rollback.Error);
        }

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = false,
                Message = "available",
            };

        private TurnTransactionResult BuildResult(
            TurnMutationResult? mutationResult,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string? rollbackError = null)
            => new()
            {
                TransactionId = "txn-test",
                Status = Status,
                Reason = Reason,
                MutationResult = mutationResult,
                MutationApplied = mutationResult is not null,
                Message = Message,
                RollbackAttempted = rollbackAttempted,
                RollbackSucceeded = rollbackSucceeded,
                RollbackError = rollbackError,
            };

        private static async Task<(bool Attempted, bool Succeeded, string? Error)> RunRollbackAsync(
            TurnMutationResult mutationResult,
            CancellationToken cancellationToken)
        {
            if (mutationResult.RollbackAsync is null)
                return (false, false, null);

            try
            {
                await mutationResult.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return (true, true, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                return (true, false, ex.Message);
            }
        }
    }
}
