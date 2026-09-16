using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for external brain-slot invocation transaction admission. TEST-MCP-178 and TEST-MCP-179.</summary>
public sealed class BrainSlotInvocationTransactionTests
{
    /// <summary>Execution is fail-closed when Mcp:BrainSlots:ExecutionEnabled is false.</summary>
    [Fact]
    public async Task InvokeAsync_WhenExecutionDisabled_DoesNotCallProvider()
    {
        using var fixture = InvocationFixture.Create(executionEnabled: false, Slot(BrainSlotRoles.CuriosityEngine));

        var response = await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "find gaps",
            TurnId = "turn-1",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.ExecutionDisabled, response.Reason);
        Assert.Null(response.Output);
        fixture.ChatClientFactory.DidNotReceiveWithAnyArgs().CreateStrategy(default!, default!);
    }

    /// <summary>Non-Curiosity roles cannot request GraphRAG admission and do not call the provider.</summary>
    [Fact]
    public async Task InvokeAsync_WhenNonCuriosityRequestsGraphRag_ReturnsDeferredFeatureDisabled()
    {
        using var fixture = InvocationFixture.Create(executionEnabled: true, Slot(BrainSlotRoles.Creativity));

        var response = await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "analyze",
            TurnId = "turn-1",
            AdmitToGraphRag = true,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, response.Reason);
        Assert.Null(response.Output);
        fixture.ChatClientFactory.DidNotReceiveWithAnyArgs().CreateStrategy(default!, default!);
    }

    /// <summary>Provider output is discarded from the response when subscriber commit fails.</summary>
    [Fact]
    public async Task InvokeAsync_WhenCommitFails_DiscardsProviderOutput()
    {
        var coordinator = new FakeTurnTransactionCoordinator
        {
            Result = new TurnTransactionResult
            {
                TransactionId = "txn-fail",
                Status = "rejected",
                Message = "subscriber rejected",
            },
        };
        using var fixture = InvocationFixture.Create(
            executionEnabled: true,
            Slot(BrainSlotRoles.CuriosityEngine),
            coordinator,
            output: "provider output");

        var response = await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "find gaps",
            TurnId = "turn-1",
            AdmitToGraphRag = true,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.CommitFailed, response.Reason);
        Assert.Equal("txn-fail", response.TransactionId);
        Assert.Null(response.Output);
        await fixture.ContextAdmission.DidNotReceiveWithAnyArgs().AdmitAsync(default!, default!, default!, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    /// <summary>Committed Curiosity output is returned and admitted only after the transaction commit succeeds.</summary>
    [Fact]
    public async Task InvokeAsync_WhenCommittedCuriosityAdmissionRequested_AdmitsAfterCommit()
    {
        var coordinator = new FakeTurnTransactionCoordinator
        {
            Result = new TurnTransactionResult
            {
                TransactionId = "txn-commit",
                Status = "committed",
                DiffgramId = "diffgram-1",
            },
        };
        using var fixture = InvocationFixture.Create(
            executionEnabled: true,
            Slot(BrainSlotRoles.CuriosityEngine),
            coordinator,
            output: "committed provider output");

        var response = await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "find gaps",
            TurnId = "turn-1",
            AdmitToGraphRag = true,
            Metadata = new Dictionary<string, string> { ["source"] = "test" },
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("committed", response.Status);
        Assert.Equal("txn-commit", response.TransactionId);
        Assert.Equal("diffgram-1", response.DiffgramId);
        Assert.Equal("committed provider output", response.Output);
        Assert.NotNull(coordinator.LastRequest);
        Assert.Equal("brain-slot.invoke", coordinator.LastRequest!.OperationName);
        Assert.Equal("brain-slot:curiosity-engine", coordinator.LastRequest.PublisherPartyId);
        Assert.Contains("\"outputSha256\"", coordinator.LastRequest.OperationBodyJson, StringComparison.Ordinal);
        Assert.DoesNotContain("committed provider output", coordinator.LastRequest.OperationBodyJson, StringComparison.Ordinal);
        await fixture.ContextAdmission.Received(1).AdmitAsync(
                Arg.Any<BrainSlotDefinitionEntity>(),
                "committed provider output",
                "txn-commit",
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>FR-MCP-LLMSTRATEGY-001: invocation passes the same TurnContext instance into the strategy.</summary>
    [Fact]
    public async Task InvokeAsync_WhenTurnContextSupplied_PassesSameInstanceToStrategy()
    {
        var context = new BrainSlotTurnContext
        {
            OriginalInput = "shared-original",
            SessionId = "sess-1",
            TurnId = "turn-1",
            TransactionId = "txn-up",
        };
        context.SetCommittedEvidence(BrainSlotRoles.Creativity, "draft");
        var recorder = new RecordingCompletionStrategy();
        using var fixture = InvocationFixture.Create(
            executionEnabled: true,
            Slot(BrainSlotRoles.Logic),
            strategy: recorder);

        await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "role-prompt",
            TurnId = "turn-1",
            TurnContext = context,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Same(context, recorder.ReceivedContext);
        Assert.Equal("role-prompt", recorder.ReceivedInput);
    }

    /// <summary>Provider timeout becomes ProviderFailed and does not admit.</summary>
    [Fact]
    public async Task InvokeAsync_WhenStrategyTimesOut_ReturnsProviderFailedWithoutAdmission()
    {
        var slot = Slot(BrainSlotRoles.CuriosityEngine);
        slot.TimeoutSeconds = 1;
        using var fixture = InvocationFixture.Create(
            executionEnabled: true,
            slot,
            strategy: new HangingCompletionStrategy());

        var response = await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "find gaps",
            TurnId = "turn-1",
            AdmitToGraphRag = true,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.ProviderFailed, response.Reason);
        Assert.Equal(0, fixture.Coordinator.ExecuteCount);
        await fixture.ContextAdmission.DidNotReceiveWithAnyArgs().AdmitAsync(default!, default!, default!, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Caller cancellation propagates and does not admit.</summary>
    [Fact]
    public async Task InvokeAsync_WhenCallerCancels_DoesNotAdmit()
    {
        var hanging = new HangingCompletionStrategy();
        using var fixture = InvocationFixture.Create(
            executionEnabled: true,
            Slot(BrainSlotRoles.CuriosityEngine),
            strategy: hanging);
        using var cts = new CancellationTokenSource();
        var invoke = fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "find gaps",
            TurnId = "turn-1",
            AdmitToGraphRag = true,
        }, cancellationToken: cts.Token);
        await hanging.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        await cts.CancelAsync().ConfigureAwait(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await invoke.ConfigureAwait(true)).ConfigureAwait(true);
        Assert.Equal(0, fixture.Coordinator.ExecuteCount);
        await fixture.ContextAdmission.DidNotReceiveWithAnyArgs().AdmitAsync(default!, default!, default!, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Null TurnContext falls back to OriginalInput from the invoke request.</summary>
    [Fact]
    public async Task InvokeAsync_WhenTurnContextMissing_BuildsFallbackFromRequest()
    {
        var recorder = new RecordingCompletionStrategy();
        using var fixture = InvocationFixture.Create(
            executionEnabled: true,
            Slot(BrainSlotRoles.Logic),
            strategy: recorder);

        await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "role-prompt",
            TurnId = "turn-fallback",
            Metadata = new Dictionary<string, string>
            {
                ["sessionId"] = "sess-fb",
                ["transactionId"] = "txn-fb",
            },
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(recorder.ReceivedContext);
        Assert.Equal("role-prompt", recorder.ReceivedContext!.OriginalInput);
        Assert.Equal("turn-fallback", recorder.ReceivedContext.TurnId);
        Assert.Equal("sess-fb", recorder.ReceivedContext.SessionId);
        Assert.Equal("txn-fb", recorder.ReceivedContext.TransactionId);
    }

    private static BrainSlotDefinitionEntity Slot(string role)
        => new()
        {
            WorkspaceId = @"F:\GitHub\McpServer",
            SlotId = "slot-1",
            Role = role,
            ProviderKind = "OpenAI",
            ModelId = "gpt-test",
            CredentialReference = "env:BRAIN_SLOT_TEST_KEY",
            PartyId = role switch
            {
                BrainSlotRoles.Creativity => "brain-slot:creativity",
                BrainSlotRoles.Logic => "brain-slot:logic",
                BrainSlotRoles.CuriosityEngine => "brain-slot:curiosity-engine",
                BrainSlotRoles.ArbiterOfTruth => "brain-slot:arbiter-of-truth",
                _ => "brain-slot:unknown",
            },
            Enabled = true,
            TimeoutSeconds = 30,
            MaxOutputTokens = 1024,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

    private static IOptionsMonitor<T> Monitor<T>(T value) where T : class
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        monitor.Get(Arg.Any<string?>()).Returns(value);
        return monitor;
    }

    private sealed class FakeCompletionStrategy(string output) : IBrainSlotCompletionStrategy
    {
        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
            => Task.FromResult(output);
    }

    private sealed class RecordingCompletionStrategy : IBrainSlotCompletionStrategy
    {
        public BrainSlotTurnContext? ReceivedContext { get; private set; }

        public string? ReceivedInput { get; private set; }

        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            ReceivedContext = context;
            ReceivedInput = input;
            return Task.FromResult("ok");
        }
    }

    private sealed class HangingCompletionStrategy : IBrainSlotCompletionStrategy
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return "never";
        }
    }

    private sealed class FakeTurnTransactionCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionResult Result { get; init; } = new()
        {
            TransactionId = "txn-commit",
            Status = "committed",
            DiffgramId = "diffgram-1",
        };

        public TurnTransactionRequest? LastRequest { get; private set; }

        public int ExecuteCount { get; private set; }

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            LastRequest = request;
            var mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
            Result.MutationResult = mutationResult;
            Result.MutationApplied = mutationResult.Success;
            return Result;
        }

        public TurnTransactionStatusResponse GetStatus()
            => new() { Enabled = true, Degraded = false };
    }

    private sealed class InvocationFixture : IDisposable
    {
        private InvocationFixture(
            McpDbContext db,
            BrainSlotInvocationService service,
            IBrainSlotChatClientFactory chatClientFactory,
            IBrainSlotContextAdmissionService contextAdmission,
            FakeTurnTransactionCoordinator coordinator)
        {
            Db = db;
            Service = service;
            ChatClientFactory = chatClientFactory;
            ContextAdmission = contextAdmission;
            Coordinator = coordinator;
        }

        public McpDbContext Db { get; }

        public BrainSlotInvocationService Service { get; }

        public IBrainSlotChatClientFactory ChatClientFactory { get; }

        public IBrainSlotContextAdmissionService ContextAdmission { get; }

        public FakeTurnTransactionCoordinator Coordinator { get; }

        public static InvocationFixture Create(
            bool executionEnabled,
            BrainSlotDefinitionEntity slot,
            ITurnTransactionCoordinator? coordinator = null,
            string output = "provider output",
            IBrainSlotCompletionStrategy? strategy = null)
        {
            var workspace = new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" };
            var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
                .UseInMemoryDatabase("brain-slot-invoke-" + Guid.NewGuid().ToString("N"))
                .Options;
            var db = new McpDbContext(dbOptions, workspace);
            var registry = Substitute.For<IBrainSlotRegistryService>();
            registry.GetEntityAsync(slot.SlotId, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<BrainSlotDefinitionEntity?>(slot));
            var resolver = Substitute.For<IBrainSlotCredentialResolver>();
            resolver.ResolveAsync(slot.CredentialReference, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("resolved-secret"));
            var chatClientFactory = Substitute.For<IBrainSlotChatClientFactory>();
            chatClientFactory.CreateStrategy(slot, "resolved-secret")
                .Returns(strategy ?? new FakeCompletionStrategy(output));
            var contextAdmission = Substitute.For<IBrainSlotContextAdmissionService>();
            contextAdmission.AdmitAsync(Arg.Any<BrainSlotDefinitionEntity>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("doc-1"));
            var partyRegistry = Substitute.For<IKeyServerPartyRegistry>();
            partyRegistry.GetPartyKeyAsync(slot.PartyId, slot.PartyId + ":signing:1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<PartyKeyDescriptor?>(new PartyKeyDescriptor
                {
                    PartyId = slot.PartyId,
                    KeyId = slot.PartyId + ":signing:1",
                    Purpose = "signing",
                    Status = "active",
                }));
            var resolvedCoordinator = coordinator as FakeTurnTransactionCoordinator ?? new FakeTurnTransactionCoordinator();
            var service = new BrainSlotInvocationService(
                db,
                registry,
                resolver,
                chatClientFactory,
                contextAdmission,
                partyRegistry,
                Monitor(new BrainSlotOptions { ExecutionEnabled = executionEnabled, DefaultTimeoutSeconds = 30, MaxTimeoutSeconds = 300 }),
                Monitor(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }),
                NullLogger<BrainSlotInvocationService>.Instance,
                coordinator ?? resolvedCoordinator);
            return new InvocationFixture(db, service, chatClientFactory, contextAdmission, resolvedCoordinator);
        }

        public void Dispose() => Db.Dispose();
    }
}
