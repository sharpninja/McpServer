using System.Text.Json;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using TxnFailureReason = McpServer.TransactionSecurity.Models.TransactionFailureReason;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-161: Server-side TODO update mutation transaction gate tests.
/// </summary>
public sealed class TransactionGatedTodoMutationServiceTests
{
    /// <summary>TODO update executes inside the coordinator and returns after commit.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsResult()
    {
        var todo = new RecordingTodoService
        {
            Existing = Item("TODO-TXN-001", "Before"),
            UpdateResult = new TodoMutationResult(true, Item: Item("TODO-TXN-001", "After")),
        };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator);

        var result = await sut.UpdateAsync(
                "TODO-TXN-001",
                new TodoUpdateRequest { Title = "After" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("After", result.Item?.Title);
        Assert.Equal(1, todo.UpdateCalls);
        Assert.Equal(1, todo.CompensatedUpdateCalls);
        Assert.Equal(0, todo.CaptureCalls);
        Assert.Equal(0, todo.RestoreCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("todo.update", coordinator.Request.OperationName);
        Assert.Contains("\"id\":\"TODO-TXN-001\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>A pre-mutation coordinator rejection prevents TODO update execution.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotUpdate()
    {
        var todo = new RecordingTodoService { Existing = Item("TODO-TXN-002", "Before") };
        var coordinator = new CapturingCoordinator
        {
            RejectBeforeMutation = true,
            Status = "rejected",
            Reason = TxnFailureReason.KeyServerUnavailable,
        };
        var sut = CreateSut(todo, coordinator);

        var result = await sut.UpdateAsync(
                "TODO-TXN-002",
                new TodoUpdateRequest { Title = "After" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(0, todo.UpdateCalls);
        Assert.Equal(0, todo.RestoreCalls);
    }

    /// <summary>A post-mutation commit failure invokes TODO store compensation.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCommitFailsAfterMutation_RestoresSnapshot()
    {
        var before = Item("TODO-TXN-003", "Before");
        var todo = new RecordingTodoService
        {
            Existing = before,
            UpdateResult = new TodoMutationResult(true, Item: Item("TODO-TXN-003", "After")),
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateSut(todo, coordinator);

        var result = await sut.UpdateAsync(
                "TODO-TXN-003",
                new TodoUpdateRequest { Title = "After" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(1, todo.UpdateCalls);
        Assert.Equal(1, todo.CompensatedUpdateCalls);
        Assert.Equal(1, todo.RestoreCalls);
        Assert.Same(before, todo.RestoredState);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>Rollback compensation failures are surfaced as failed rollback results instead of success.</summary>
    [Fact]
    public async Task UpdateAsync_WhenRollbackReturnsFailure_ReportsRollbackFailure()
    {
        var before = Item("TODO-TXN-004", "Before");
        var todo = new RecordingTodoService
        {
            Existing = before,
            UpdateResult = new TodoMutationResult(true, Item: Item("TODO-TXN-004", "After")),
            RestoreResult = new TodoMutationResult(false, "restore failed", FailureKind: TodoMutationFailureKind.Conflict),
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateSut(todo, coordinator);

        var result = await sut.UpdateAsync(
                "TODO-TXN-004",
                new TodoUpdateRequest { Title = "After" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(1, todo.RestoreCalls);
        Assert.Contains("Rollback failed", result.Error, StringComparison.Ordinal);
        Assert.Contains("restore failed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>External-sync/projection failures with a mutated item are rolled back during abort.</summary>
    [Fact]
    public async Task UpdateAsync_WhenLocalFailureMayHaveAppliedMutation_RestoresSnapshot()
    {
        var before = Item("TODO-TXN-005", "Before");
        var todo = new RecordingTodoService
        {
            Existing = before,
            UpdateResult = new TodoMutationResult(
                false,
                "Updated locally but external sync failed.",
                Item("TODO-TXN-005", "After"),
                TodoMutationFailureKind.ExternalSyncFailed),
        };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator);

        var result = await sut.UpdateAsync(
                "TODO-TXN-005",
                new TodoUpdateRequest { Title = "After" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.ExternalSyncFailed, result.FailureKind);
        Assert.Equal(1, todo.UpdateCalls);
        Assert.Equal(1, todo.RestoreCalls);
        Assert.Same(before, todo.RestoredState);
    }

    /// <summary>Partial local failures surface rollback failure details when compensation cannot restore.</summary>
    [Fact]
    public async Task UpdateAsync_WhenLocalFailureRollbackFails_ReportsRollbackFailure()
    {
        var before = Item("TODO-TXN-006", "Before");
        var todo = new RecordingTodoService
        {
            Existing = before,
            UpdateResult = new TodoMutationResult(
                false,
                "Updated locally but projection failed.",
                Item("TODO-TXN-006", "After"),
                TodoMutationFailureKind.ProjectionFailed),
            RestoreResult = new TodoMutationResult(false, "restore failed", FailureKind: TodoMutationFailureKind.Conflict),
        };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator);

        var result = await sut.UpdateAsync(
                "TODO-TXN-006",
                new TodoUpdateRequest { Title = "After" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(1, todo.RestoreCalls);
        Assert.Contains("Rollback failed", result.Error, StringComparison.Ordinal);
        Assert.Contains("restore failed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>ISSUE-backed updates are rejected while transaction gating is required because GitHub side effects are not compensated yet.</summary>
    [Fact]
    public async Task UpdateAsync_WhenIssueBackedTodoAndGatingRequired_RejectsBeforeMutation()
    {
        var todo = new RecordingTodoService { Existing = Item("ISSUE-42", "Before") };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.UpdateAsync(
                "ISSUE-42",
                new TodoUpdateRequest { Title = "After" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("ISSUE-backed TODO updates", result.Error, StringComparison.Ordinal);
        Assert.Equal(0, todo.UpdateCalls);
        Assert.Null(coordinator.Request);
    }

    /// <summary>TODO create executes inside the coordinator and returns only after commit.</summary>
    [Fact]
    public async Task CreateAsync_WhenCoordinatorCommits_BuildsTransactionAndReturnsResult()
    {
        var created = Item("TODO-TXN-CREATE-001", "Created");
        var todo = new RecordingTodoService
        {
            CreateResult = new TodoMutationResult(true, Item: created),
        };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator);

        var result = await sut.CreateAsync(
                new TodoCreateRequest
                {
                    Id = "TODO-TXN-CREATE-001",
                    Title = "Created",
                    Section = "Backlog",
                    Priority = "high",
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Same(created, result.Item);
        Assert.Equal(1, todo.CreateCalls);
        Assert.Equal(0, todo.DeleteCreatedCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("todo.create", coordinator.Request.OperationName);
        Assert.Contains("\"id\":\"TODO-TXN-CREATE-001\"", coordinator.Request.OperationBodyJson, StringComparison.Ordinal);
    }

    /// <summary>A pre-mutation coordinator rejection prevents TODO create execution.</summary>
    [Fact]
    public async Task CreateAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotCreate()
    {
        var todo = new RecordingTodoService();
        var coordinator = new CapturingCoordinator
        {
            RejectBeforeMutation = true,
            Status = "rejected",
            Reason = TxnFailureReason.KeyServerUnavailable,
        };
        var sut = CreateSut(todo, coordinator);

        var result = await sut.CreateAsync(
                new TodoCreateRequest
                {
                    Id = "TODO-TXN-CREATE-002",
                    Title = "Created",
                    Section = "Backlog",
                    Priority = "high",
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(0, todo.CreateCalls);
    }

    /// <summary>A post-mutation commit failure removes the locally-created TODO item.</summary>
    [Fact]
    public async Task CreateAsync_WhenCommitFailsAfterMutation_DeletesCreatedItem()
    {
        var created = Item("TODO-TXN-CREATE-003", "Created");
        var todo = new RecordingTodoService
        {
            CreateResult = new TodoMutationResult(true, Item: created),
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateSut(todo, coordinator);

        var result = await sut.CreateAsync(
                new TodoCreateRequest
                {
                    Id = "TODO-TXN-CREATE-003",
                    Title = "Created",
                    Section = "Backlog",
                    Priority = "high",
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(1, todo.CreateCalls);
        Assert.Equal(1, todo.DeleteCreatedCalls);
        Assert.Null(todo.Existing);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>ISSUE-backed creates are rejected while required transaction gating lacks GitHub side-effect compensation.</summary>
    [Fact]
    public async Task CreateAsync_WhenIssueBackedTodoAndGatingRequired_RejectsBeforeMutation()
    {
        var todo = new RecordingTodoService();
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.CreateAsync(
                new TodoCreateRequest
                {
                    Id = TodoCreationService.NewGitHubIssueTodoId,
                    Title = "Issue backed",
                    Section = "Backlog",
                    Priority = "high",
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("ISSUE-backed TODO creates", result.Error, StringComparison.Ordinal);
        Assert.Equal(0, todo.CreateCalls);
        Assert.Null(coordinator.Request);
    }

    /// <summary>A federated decorator over a non-compensating provider is rejected before required-gated create mutation.</summary>
    [Fact]
    public async Task CreateAsync_WhenFederatedProviderWrapsUnsupportedInnerAndGatingRequired_RejectsBeforeMutation()
    {
        var inner = Substitute.For<ITodoService>();
        var federated = new FederatedTodoService(
            inner,
            new FederationRegistry(Microsoft.Extensions.Options.Options.Create(new FederationOptions())),
            Substitute.For<IFederationDataClient>(),
            NullLogger<FederatedTodoService>.Instance);
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(federated, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.CreateAsync(
                new TodoCreateRequest
                {
                    Id = "TODO-TXN-CREATE-FED-001",
                    Title = "Created",
                    Section = "Backlog",
                    Priority = "high",
                },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("active TODO provider does not support transaction rollback compensation", result.Error, StringComparison.Ordinal);
        await inner.DidNotReceiveWithAnyArgs().CreateAsync(default!, default).ConfigureAwait(true);
        Assert.Null(coordinator.Request);
    }

    /// <summary>A post-mutation commit failure restores the TODO deleted by the local provider.</summary>
    [Fact]
    public async Task DeleteAsync_WhenCommitFailsAfterMutation_RestoresSnapshot()
    {
        var before = Item("TODO-TXN-DELETE-001", "Before");
        var todo = new RecordingTodoService
        {
            Existing = before,
            DeleteResult = new TodoMutationResult(true),
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateSut(todo, coordinator);

        var result = await sut.DeleteAsync("TODO-TXN-DELETE-001", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(1, todo.DeleteCalls);
        Assert.Equal(1, todo.CompensatedDeleteCalls);
        Assert.Equal(1, todo.RestoreCalls);
        Assert.Same(before, todo.RestoredState);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>ISSUE-backed deletes are rejected while required transaction gating lacks GitHub side-effect compensation.</summary>
    [Fact]
    public async Task DeleteAsync_WhenIssueBackedTodoAndGatingRequired_RejectsBeforeMutation()
    {
        var todo = new RecordingTodoService { Existing = Item("ISSUE-42", "Before") };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.DeleteAsync("ISSUE-42", CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("ISSUE-backed TODO deletes", result.Error, StringComparison.Ordinal);
        Assert.Equal(0, todo.DeleteCalls);
        Assert.Null(coordinator.Request);
    }

    /// <summary>A pre-mutation coordinator rejection prevents TODO move source and target mutations.</summary>
    [Fact]
    public async Task MoveAsync_WhenCoordinatorRejectsBeforeMutation_DoesNotCreateTargetOrDeleteSource()
    {
        var source = new RecordingTodoService { Existing = Item("TODO-TXN-MOVE-001", "Move me") };
        var target = new RecordingTodoService();
        var coordinator = new CapturingCoordinator
        {
            RejectBeforeMutation = true,
            Status = "rejected",
            Reason = TxnFailureReason.KeyServerUnavailable,
        };
        var sut = CreateMoveSut(source, target, coordinator);

        var result = await sut.MoveAsync(
                "TODO-TXN-MOVE-001",
                new TodoMoveRequest { TargetWorkspacePath = TargetWorkspacePath },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(0, target.CreateCalls);
        Assert.Equal(0, source.DeleteCalls);
    }

    /// <summary>TODO move creates the target item and deletes the source only after transaction commit approval.</summary>
    [Fact]
    public async Task MoveAsync_WhenCoordinatorCommits_CreatesTargetDeletesSourceAndReturnsMovedItem()
    {
        var sourceItem = Item("TODO-TXN-MOVE-002", "Move me");
        var movedItem = sourceItem with { Title = "Move me" };
        var source = new RecordingTodoService
        {
            Existing = sourceItem,
            DeleteResult = new TodoMutationResult(true),
        };
        var target = new RecordingTodoService
        {
            CreateResult = new TodoMutationResult(true, Item: movedItem),
        };
        var coordinator = new CapturingCoordinator();
        var sut = CreateMoveSut(source, target, coordinator);

        var result = await sut.MoveAsync(
                "TODO-TXN-MOVE-002",
                new TodoMoveRequest { TargetWorkspacePath = TargetWorkspacePath },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Same(movedItem, result.Item);
        Assert.Null(source.Existing);
        Assert.Same(movedItem, target.Existing);
        Assert.Equal(1, target.CreateCalls);
        Assert.Equal(1, source.CompensatedDeleteCalls);
        Assert.NotNull(coordinator.Request);
        Assert.Equal("todo.move", coordinator.Request.OperationName);
    }

    /// <summary>TODO move rollback removes the target item and restores the source snapshot.</summary>
    [Fact]
    public async Task MoveAsync_WhenCommitFailsAfterMutation_RemovesTargetAndRestoresSource()
    {
        var sourceItem = Item("TODO-TXN-MOVE-003", "Move me");
        var movedItem = sourceItem with { Title = "Move me" };
        var source = new RecordingTodoService
        {
            Existing = sourceItem,
            DeleteResult = new TodoMutationResult(true),
        };
        var target = new RecordingTodoService
        {
            CreateResult = new TodoMutationResult(true, Item: movedItem),
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateMoveSut(source, target, coordinator);

        var result = await sut.MoveAsync(
                "TODO-TXN-MOVE-003",
                new TodoMoveRequest { TargetWorkspacePath = TargetWorkspacePath },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Same(sourceItem, source.Existing);
        Assert.Null(target.Existing);
        Assert.Equal(1, target.DeleteCreatedCalls);
        Assert.Equal(1, source.RestoreCalls);
        Assert.Contains("Rollback completed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>TODO move reports rollback failure when either target cleanup or source restore fails.</summary>
    [Fact]
    public async Task MoveAsync_WhenRollbackFails_ReturnsConflictWithRollbackFailure()
    {
        var sourceItem = Item("TODO-TXN-MOVE-004", "Move me");
        var movedItem = sourceItem with { Title = "Move me" };
        var source = new RecordingTodoService
        {
            Existing = sourceItem,
            DeleteResult = new TodoMutationResult(true),
            RestoreResult = new TodoMutationResult(false, "source restore failed", FailureKind: TodoMutationFailureKind.Conflict),
        };
        var target = new RecordingTodoService
        {
            CreateResult = new TodoMutationResult(true, Item: movedItem),
            DeleteCreatedResult = new TodoMutationResult(false, "target cleanup failed", FailureKind: TodoMutationFailureKind.Conflict),
        };
        var coordinator = new CapturingCoordinator
        {
            Status = "rejected",
            Reason = TxnFailureReason.SubscriberUnavailable,
            Message = "Subscriber unavailable.",
        };
        var sut = CreateMoveSut(source, target, coordinator);

        var result = await sut.MoveAsync(
                "TODO-TXN-MOVE-004",
                new TodoMoveRequest { TargetWorkspacePath = TargetWorkspacePath },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Equal(1, target.DeleteCreatedCalls);
        Assert.Equal(1, source.RestoreCalls);
        Assert.Contains("Rollback failed", result.Error, StringComparison.Ordinal);
        Assert.Contains("target cleanup failed", result.Error, StringComparison.Ordinal);
        Assert.Contains("source restore failed", result.Error, StringComparison.Ordinal);
    }

    /// <summary>ISSUE-backed TODO moves are rejected while GitHub side-effect compensation is deferred.</summary>
    [Fact]
    public async Task MoveAsync_WhenIssueBackedTodoAndGatingRequired_RejectsBeforeMutation()
    {
        var source = new RecordingTodoService { Existing = Item("ISSUE-42", "Move me") };
        var target = new RecordingTodoService();
        var coordinator = new CapturingCoordinator();
        var sut = CreateMoveSut(source, target, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.MoveAsync(
                "ISSUE-42",
                new TodoMoveRequest { TargetWorkspacePath = TargetWorkspacePath },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("ISSUE-backed TODO moves", result.Error, StringComparison.Ordinal);
        Assert.Equal(0, target.CreateCalls);
        Assert.Equal(0, source.DeleteCalls);
        Assert.Null(coordinator.Request);
    }

    /// <summary>TODO move rejects unsupported target providers before mutating when mutation gating is required.</summary>
    [Fact]
    public async Task MoveAsync_WhenTargetProviderUnsupportedAndGatingRequired_ReturnsConflictBeforeMutation()
    {
        var source = new RecordingTodoService { Existing = Item("TODO-TXN-MOVE-005", "Move me") };
        var target = Substitute.For<ITodoService>();
        var coordinator = new CapturingCoordinator();
        var sut = CreateMoveSut(source, target, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.MoveAsync(
                "TODO-TXN-MOVE-005",
                new TodoMoveRequest { TargetWorkspacePath = TargetWorkspacePath },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, result.FailureKind);
        Assert.Contains("target TODO provider does not support transaction rollback compensation", result.Error, StringComparison.Ordinal);
        await target.DidNotReceiveWithAnyArgs().CreateAsync(default!, default).ConfigureAwait(true);
        Assert.Equal(0, source.DeleteCalls);
    }

    /// <summary>TODO projection repair fails closed while required transaction gating is active.</summary>
    [Fact]
    public async Task RepairProjectionAsync_WhenRequiredTransactionsActive_FailsClosedWithoutRepair()
    {
        var todo = new RecordingTodoService();
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.RepairProjectionAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("not transaction compensated", result.Error, StringComparison.Ordinal);
        Assert.Equal(0, todo.RepairProjectionCalls);
    }

    /// <summary>TODO projection repair fails closed when the coordinator is degraded.</summary>
    [Fact]
    public async Task RepairProjectionAsync_WhenCoordinatorDegraded_FailsClosedWithoutRepair()
    {
        var todo = new RecordingTodoService();
        var coordinator = new CapturingCoordinator
        {
            StatusResponse = new TurnTransactionStatusResponse
            {
                Enabled = true,
                Degraded = true,
                Message = "transaction gate unavailable",
            },
        };
        var sut = CreateSut(todo, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });

        var result = await sut.RepairProjectionAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("transaction gate unavailable", result.Error, StringComparison.Ordinal);
        Assert.Equal(0, todo.RepairProjectionCalls);
    }

    /// <summary>TODO projection repair delegates when mutation transactions are not required.</summary>
    [Fact]
    public async Task RepairProjectionAsync_WhenTransactionsNotRequired_DelegatesToTodoService()
    {
        var todo = new RecordingTodoService
        {
            RepairProjectionResult = ProjectionRepair(success: true),
        };
        var coordinator = new CapturingCoordinator();
        var sut = CreateSut(todo, coordinator, new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await sut.RepairProjectionAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(1, todo.RepairProjectionCalls);
    }

    /// <summary>DI resolution selects the constructor that has TODO move workspace resolution dependencies.</summary>
    [Fact]
    public async Task MoveAsync_WhenResolvedFromServiceProvider_UsesMoveCapableConstructor()
    {
        var source = new RecordingTodoService();
        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new McpServer.Support.Mcp.Ingestion.IngestionOptions { RepoRoot = "." });
        var factory = Substitute.For<ITodoServiceFactory>();
        var resolver = new TodoServiceResolver(source, ingestionOptions, factory);
        var httpContextAccessor = Substitute.For<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((Microsoft.AspNetCore.Http.HttpContext?)null);
        var accessor = new WorkspaceServiceAccessor(resolver, httpContextAccessor, ingestionOptions);
        var services = new ServiceCollection()
            .AddSingleton(accessor)
            .AddSingleton(new TodoCreationService(accessor, Substitute.For<IGitHubCliService>(), NullLogger<TodoCreationService>.Instance))
            .AddSingleton(new TodoUpdateService(accessor, null, NullLogger<TodoUpdateService>.Instance))
            .AddSingleton(resolver)
            .AddSingleton(Substitute.For<IWorkspaceService>())
            .AddSingleton<ITurnTransactionCoordinator>(new CapturingCoordinator())
            .AddSingleton(Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }))
            .AddScoped<ITransactionGatedTodoMutationService, TransactionGatedTodoMutationService>();

        await using var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<ITransactionGatedTodoMutationService>();

        var result = await sut.MoveAsync(
                "TODO-TXN-MOVE-DI-001",
                new TodoMoveRequest { TargetWorkspacePath = TargetWorkspacePath },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.NotFound, result.FailureKind);
        Assert.Contains("not found in source workspace", result.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("requires workspace resolution services", result.Error, StringComparison.Ordinal);
    }

    private static TransactionGatedTodoMutationService CreateSut(
        ITodoService todo,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
    {
        var accessor = TestWorkspaceAccessorHelper.Create(todo);
        var creation = new TodoCreationService(accessor, Substitute.For<IGitHubCliService>(), NullLogger<TodoCreationService>.Instance);
        var update = new TodoUpdateService(accessor, null, NullLogger<TodoUpdateService>.Instance);
        return new TransactionGatedTodoMutationService(
            accessor,
            creation,
            update,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
    }

    private const string TargetWorkspacePath = @"F:\GitHub\McpServer.Target";

    private static TransactionGatedTodoMutationService CreateMoveSut(
        RecordingTodoService source,
        ITodoService target,
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? options = null)
    {
        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new McpServer.Support.Mcp.Ingestion.IngestionOptions { RepoRoot = "." });
        var factory = Substitute.For<ITodoServiceFactory>();
        factory.CreateForWorkspace(Arg.Any<string>(), Arg.Any<WorkspaceContext>()).Returns(target);
        var resolver = new TodoServiceResolver(source, ingestionOptions, factory);
        var httpContextAccessor = Substitute.For<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((Microsoft.AspNetCore.Http.HttpContext?)null);
        var accessor = new WorkspaceServiceAccessor(resolver, httpContextAccessor, ingestionOptions);
        var creation = new TodoCreationService(accessor, Substitute.For<IGitHubCliService>(), NullLogger<TodoCreationService>.Instance);
        var update = new TodoUpdateService(accessor, null, NullLogger<TodoUpdateService>.Instance);
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.GetAsync(TargetWorkspacePath, Arg.Any<CancellationToken>())
            .Returns(TargetWorkspace());

        return new TransactionGatedTodoMutationService(
            accessor,
            creation,
            update,
            resolver,
            workspaceService,
            coordinator,
            Microsoft.Extensions.Options.Options.Create(options ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
    }

    private static WorkspaceDto TargetWorkspace()
        => new()
        {
            WorkspacePath = TargetWorkspacePath,
            Name = "target",
            TodoPath = "docs/todo.yaml",
            StatusPrompt = "status",
            ImplementPrompt = "implement",
            PlanPrompt = "plan",
        };

    private static TodoFlatItem Item(string id, string title)
        => new()
        {
            Id = id,
            Title = title,
            Section = "Backlog",
            Priority = "high",
            Done = false,
        };

    private static TodoProjectionRepairResult ProjectionRepair(bool success, string? error = null)
        => new(
            success,
            error,
            new TodoProjectionStatusResult(
                "test",
                "test.db",
                "TODO.yaml",
                ProjectionTargetExists: false,
                ProjectionConsistent: success,
                RepairRequired: !success,
                DateTimeOffset.UtcNow.ToString("O"),
                Message: error));

    private sealed class RecordingTodoService : ITodoService, ITodoCompensationService
    {
        public TodoFlatItem? Existing { get; set; }

        public TodoMutationResult UpdateResult { get; set; } = new(true);

        public TodoMutationResult CreateResult { get; set; } = new(true);

        public TodoMutationResult DeleteResult { get; set; } = new(true);

        public TodoMutationResult DeleteCreatedResult { get; set; } = new(true);

        public TodoMutationResult RestoreResult { get; set; } = new(true);

        public int CreateCalls { get; private set; }

        public int UpdateCalls { get; private set; }

        public int DeleteCalls { get; private set; }

        public int CompensatedUpdateCalls { get; private set; }

        public int CompensatedDeleteCalls { get; private set; }

        public int DeleteCreatedCalls { get; private set; }

        public int CaptureCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public int RepairProjectionCalls { get; private set; }

        public object? RestoredState { get; private set; }

        public TodoProjectionRepairResult RepairProjectionResult { get; init; } = ProjectionRepair(success: true);

        public Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoQueryResult(Existing is null ? [] : [Existing], Existing is null ? 0 : 1));

        public Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Existing is not null && string.Equals(Existing.Id, id, StringComparison.OrdinalIgnoreCase) ? Existing : null);

        public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            if (CreateResult.Item is not null)
                Existing = CreateResult.Item;
            return Task.FromResult(CreateResult);
        }

        public Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            if (UpdateResult.Item is not null)
                Existing = UpdateResult.Item;
            return Task.FromResult(UpdateResult);
        }

        public Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            if (DeleteResult.Success && Existing is not null && string.Equals(Existing.Id, id, StringComparison.OrdinalIgnoreCase))
                Existing = null;
            return Task.FromResult(DeleteResult);
        }

        public Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
            => Task.FromResult(new TodoAuditQueryResult([], 0));

        public Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
        {
            RepairProjectionCalls++;
            return Task.FromResult(RepairProjectionResult);
        }

        public Task<TodoCompensationSnapshot?> CaptureForRestoreAsync(string id, CancellationToken cancellationToken = default)
        {
            CaptureCalls++;
            return Task.FromResult<TodoCompensationSnapshot?>(Existing is null
                ? null
                : new TodoCompensationSnapshot { Provider = "test", State = Existing });
        }

        public async Task<TodoCompensatedMutationResult> UpdateWithRestorePointAsync(
            string id,
            TodoUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            CompensatedUpdateCalls++;
            var snapshot = Existing is null
                ? null
                : new TodoCompensationSnapshot { Provider = "test", State = Existing };
            var result = await UpdateAsync(id, request, cancellationToken).ConfigureAwait(true);
            return new TodoCompensatedMutationResult
            {
                Result = result,
                Snapshot = snapshot,
            };
        }

        public async Task<TodoCompensatedMutationResult> DeleteWithRestorePointAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            CompensatedDeleteCalls++;
            var snapshot = Existing is null
                ? null
                : new TodoCompensationSnapshot { Provider = "test", State = Existing };
            var result = await DeleteAsync(id, cancellationToken).ConfigureAwait(true);
            return new TodoCompensatedMutationResult
            {
                Result = result,
                Snapshot = snapshot,
            };
        }

        public Task<TodoMutationResult> DeleteCreatedAsync(string id, CancellationToken cancellationToken = default)
        {
            DeleteCreatedCalls++;
            if (DeleteCreatedResult.Success && Existing is not null && string.Equals(Existing.Id, id, StringComparison.OrdinalIgnoreCase))
                Existing = null;
            return Task.FromResult(DeleteCreatedResult);
        }

        public Task<TodoMutationResult> RestoreAsync(TodoCompensationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            RestoredState = snapshot.State;
            if (RestoreResult.Success)
                Existing = (TodoFlatItem)snapshot.State;
            return Task.FromResult(RestoreResult.Success
                ? RestoreResult with { Item = Existing }
                : RestoreResult);
        }
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? Request { get; private set; }

        public bool RejectBeforeMutation { get; init; }

        public string Status { get; init; } = "committed";

        public TxnFailureReason Reason { get; init; } = TxnFailureReason.None;

        public string? Message { get; init; }

        public TurnTransactionStatusResponse? StatusResponse { get; init; }

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            if (RejectBeforeMutation)
                return BuildResult(null, rollbackAttempted: false, rollbackSucceeded: false);

            var mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
            if (!mutationResult.Success)
            {
                var rollback = await RunRollbackAsync(mutationResult, cancellationToken).ConfigureAwait(false);
                return new TurnTransactionResult
                {
                    TransactionId = "txn-test",
                    Status = "aborted",
                    Reason = TxnFailureReason.Aborted,
                    MutationResult = mutationResult,
                    MutationApplied = true,
                    Message = mutationResult.Error,
                    RollbackAttempted = rollback.Attempted,
                    RollbackSucceeded = rollback.Succeeded,
                    RollbackError = rollback.Error,
                };
            }

            if (string.Equals(Status, "committed", StringComparison.OrdinalIgnoreCase))
                return BuildResult(mutationResult, rollbackAttempted: false, rollbackSucceeded: false);

            var failedRollback = await RunRollbackAsync(mutationResult, cancellationToken).ConfigureAwait(false);
            return BuildResult(mutationResult, failedRollback.Attempted, failedRollback.Succeeded, failedRollback.Error);
        }

        public TurnTransactionStatusResponse GetStatus()
            => StatusResponse ?? new()
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
