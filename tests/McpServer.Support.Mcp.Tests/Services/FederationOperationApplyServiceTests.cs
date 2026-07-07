using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.FederationAdapters;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for applying synchronized federation operations through adapters.</summary>
public sealed class FederationOperationApplyServiceTests
{
    /// <summary>Transactional apply gates adapter mutations through the turn transaction coordinator.</summary>
    [Fact]
    public async Task TransactionalApplyAsync_CommitsAdapterApplyThroughTurnCoordinator()
    {
        var adapter = new CapturingAdapter("todo");
        var inner = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));
        var coordinator = new CapturingTurnTransactionCoordinator();
        var sut = new TurnTransactionFederationOperationApplyService(inner, coordinator);

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-transactional-apply",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-TURNTRANSACTIONS-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-TURNTRANSACTIONS-001",
            Method = "workflow.todo.update",
            BodyBase64 = Convert.ToBase64String("{\"remaining\":\"next\"}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        Assert.Equal("{\"remaining\":\"next\"}", adapter.PayloadJson);
        Assert.NotNull(coordinator.LastRequest);
        Assert.Equal("op-transactional-apply", coordinator.LastRequest.TransactionId);
        Assert.Equal("workflow.todo.update", coordinator.LastRequest.OperationName);
        Assert.True(coordinator.LastRequest.Mutating);
        Assert.Contains("PLAN-TURNTRANSACTIONS-001", coordinator.LastRequest.OperationBodyJson, StringComparison.Ordinal);
        Assert.Contains("\"domain\":\"todo\"", coordinator.LastRequest.OperationBodyJson, StringComparison.Ordinal);
        Assert.True(coordinator.MutationRan);
    }

    /// <summary>Transactional apply does not run the adapter when the coordinator rejects before mutation.</summary>
    [Fact]
    public async Task TransactionalApplyAsync_WhenCoordinatorRejects_DoesNotApplyAdapter()
    {
        var adapter = new CapturingAdapter("todo");
        var inner = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));
        var coordinator = new CapturingTurnTransactionCoordinator
        {
            PreMutationStatus = "rejected",
            PreMutationReason = TransactionFailureReason.KeyServerUnavailable,
            PreMutationMessage = "keyserver unavailable",
        };
        var sut = new TurnTransactionFederationOperationApplyService(inner, coordinator);

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-rejected-apply",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-TURNTRANSACTIONS-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-TURNTRANSACTIONS-001",
            BodyBase64 = Convert.ToBase64String("{\"remaining\":\"next\"}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Contains("keyserver unavailable", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(adapter.PayloadJson);
        Assert.False(coordinator.MutationRan);
    }

    /// <summary>Transactional apply does not return an applied result while the coordinator commit remains pending.</summary>
    [Fact]
    public async Task TransactionalApplyAsync_WhenCoordinatorCommitIsPending_DoesNotReturnApplyResult()
    {
        var adapter = new CapturingAdapter("todo");
        var inner = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));
        var coordinator = new CapturingTurnTransactionCoordinator { HoldAfterMutation = true };
        var sut = new TurnTransactionFederationOperationApplyService(inner, coordinator);
        var operation = new FederationOperationRequest
        {
            OperationId = "op-pending-apply",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-TURNTRANSACTIONS-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-TURNTRANSACTIONS-001",
            BodyBase64 = Convert.ToBase64String("{\"remaining\":\"next\"}"u8.ToArray()),
        };

        var applyTask = sut.ApplyAsync(operation, CancellationToken.None).AsTask();
        await coordinator.WaitForMutationAsync().ConfigureAwait(true);

        Assert.True(coordinator.MutationRan);
        Assert.Equal("{\"remaining\":\"next\"}", adapter.PayloadJson);
        Assert.False(applyTask.IsCompleted);

        coordinator.ReleaseCoordinator();
        var result = await applyTask.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Applied);
    }

    /// <summary>Transactional apply blocks federation mutations when the coordinator is already degraded.</summary>
    [Fact]
    public async Task TransactionalApplyAsync_WhenCoordinatorIsDegraded_DoesNotApplyAdapter()
    {
        var adapter = new CapturingAdapter("todo");
        var inner = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));
        var coordinator = new CapturingTurnTransactionCoordinator
        {
            Degraded = true,
            DegradedMessage = "turn transaction coordinator is degraded",
        };
        var sut = new TurnTransactionFederationOperationApplyService(inner, coordinator);

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-degraded-apply",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-TURNTRANSACTIONS-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-TURNTRANSACTIONS-001",
            BodyBase64 = Convert.ToBase64String("{\"remaining\":\"next\"}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Contains("degraded", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(adapter.PayloadJson);
        Assert.False(coordinator.MutationRan);
        Assert.Null(coordinator.LastRequest);
    }

    /// <summary>Apply decodes BodyBase64 before passing payload JSON to the adapter.</summary>
    [Fact]
    public async Task ApplyAsync_DecodesBodyBase64BeforeAdapterApply()
    {
        var adapter = new CapturingAdapter("todo");
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "PUT",
            Path = "/mcpserver/todo/PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"title\":\"Updated\"}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(result.Applied);
        Assert.Equal("{\"title\":\"Updated\"}", adapter.PayloadJson);
        Assert.Equal("PUT", adapter.HttpMethod);
        Assert.Equal("/mcpserver/todo/PLAN-FEDERATION-001", adapter.Path);
    }

    /// <summary>Invalid base64 bodies return a conflict result instead of calling the adapter.</summary>
    [Fact]
    public async Task ApplyAsync_InvalidBodyBase64DoesNotCallAdapter()
    {
        var adapter = new CapturingAdapter("todo");
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            BodyBase64 = "not-base64",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Null(adapter.PayloadJson);
    }

    /// <summary>Local-only domains are rejected before adapter apply is attempted.</summary>
    [Fact]
    public async Task ApplyAsync_LocalOnlyDomainReturnsConflict()
    {
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry(
            [new LocalOnlyFederationStateAdapter("marker_state", "host-specific trust material")]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "marker_state",
            ResourceId = "AGENTS-README-FIRST.yaml",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Contains("local-only", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Echo operations are acknowledged as already applied and do not call adapter apply.</summary>
    [Fact]
    public async Task ApplyAsync_EchoOperationSuppressesApply()
    {
        var adapter = new CapturingAdapter("todo") { Echo = true, Version = "v3" };
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            SourceOperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"title\":\"Updated\"}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.AlreadyApplied);
        Assert.Equal("v3", result.Version);
        Assert.Null(adapter.PayloadJson);
    }

    /// <summary>Snapshot-only replicated domains return an explicit conflict instead of silent success.</summary>
    [Fact]
    public async Task ApplyAsync_SnapshotOnlyDomainReturnsConflict()
    {
        var adapter = new SnapshotOnlyAdapter("session_log");
        var sut = new FederationOperationApplyService(new FederationStateAdapterRegistry([adapter]));

        var result = await sut.ApplyAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "session_log",
            ResourceId = "Codex/session",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.False(result.Applied);
        Assert.True(result.Conflict);
        Assert.Contains("requires signed operation envelopes", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingAdapter : IFederationStateAdapter
    {
        public CapturingAdapter(string domain)
        {
            Domain = domain;
        }

        public string Domain { get; }

        public bool IsLocalOnly => false;

        public string? PayloadJson { get; private set; }

        public string? HttpMethod { get; private set; }

        public string? Path { get; private set; }

        public bool Echo { get; set; }

        public string Version { get; set; } = "v1";

        public ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
            => new(new FederationStateSnapshot { Domain = Domain, ResourceId = resourceId, Version = "v1" });

        public ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
        {
            PayloadJson = operation.PayloadJson;
            HttpMethod = operation.HttpMethod;
            Path = operation.Path;
            return new ValueTask<FederationApplyResult>(new FederationApplyResult { Applied = true, Version = "v2" });
        }

        public ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
            => new(Version);

        public string GetIdempotencyKey(FederationStateOperation operation)
            => operation.OperationId;

        public bool IsEcho(FederationStateOperation operation)
            => Echo;
    }

    private sealed class SnapshotOnlyAdapter : FederationStateAdapterBase
    {
        public SnapshotOnlyAdapter(string domain)
            : base(domain)
        {
        }

        public override ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
            => new(new FederationStateSnapshot { Domain = Domain, ResourceId = resourceId, Version = "v1" });

        public override ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
            => new("v1");
    }

    private sealed class CapturingTurnTransactionCoordinator : ITurnTransactionCoordinator
    {
        private readonly TaskCompletionSource _mutationCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseCoordinator = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TurnTransactionRequest? LastRequest { get; private set; }

        public bool MutationRan { get; private set; }

        public string? PreMutationStatus { get; init; }

        public TransactionFailureReason PreMutationReason { get; init; }

        public string? PreMutationMessage { get; init; }

        public bool Degraded { get; init; }

        public string? DegradedMessage { get; init; }

        public bool HoldAfterMutation { get; init; }

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            if (!string.IsNullOrWhiteSpace(PreMutationStatus))
            {
                return new TurnTransactionResult
                {
                    TransactionId = request.TransactionId ?? string.Empty,
                    Status = PreMutationStatus,
                    Reason = PreMutationReason,
                    MutationApplied = false,
                    Message = PreMutationMessage,
                };
            }

            MutationRan = true;
            var mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
            _mutationCompleted.TrySetResult();
            if (HoldAfterMutation)
                await _releaseCoordinator.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? string.Empty,
                Status = mutationResult.Success ? "committed" : "aborted",
                Reason = mutationResult.Success ? TransactionFailureReason.None : TransactionFailureReason.Aborted,
                MutationApplied = true,
                MutationResult = mutationResult,
                Message = mutationResult.Error,
            };
        }

        public Task WaitForMutationAsync()
            => _mutationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        public void ReleaseCoordinator()
            => _releaseCoordinator.TrySetResult();

        public TurnTransactionStatusResponse GetStatus()
            => new()
            {
                Enabled = true,
                Degraded = Degraded,
                Message = DegradedMessage ?? "available",
            };
    }
}
