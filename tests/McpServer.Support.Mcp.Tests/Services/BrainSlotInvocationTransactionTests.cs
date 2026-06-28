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
        }).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.ExecutionDisabled, response.Reason);
        Assert.Null(response.Output);
        fixture.ChatClientFactory.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    /// <summary>Non-Curiosity roles cannot request GraphRAG admission and do not call the provider.</summary>
    [Fact]
    public async Task InvokeAsync_WhenNonCuriosityRequestsGraphRag_ReturnsDeferredFeatureDisabled()
    {
        using var fixture = InvocationFixture.Create(executionEnabled: true, Slot(BrainSlotRoles.LeftHemisphere));

        var response = await fixture.Service.InvokeAsync("slot-1", new BrainSlotInvokeRequest
        {
            Input = "analyze",
            TurnId = "turn-1",
            AdmitToGraphRag = true,
        }).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.DeferredFeatureDisabled, response.Reason);
        Assert.Null(response.Output);
        fixture.ChatClientFactory.DidNotReceiveWithAnyArgs().Create(default!, default!);
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
        }).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.CommitFailed, response.Reason);
        Assert.Equal("txn-fail", response.TransactionId);
        Assert.Null(response.Output);
        await fixture.ContextAdmission.DidNotReceiveWithAnyArgs().AdmitAsync(default!, default!, default!, default)
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
        }).ConfigureAwait(true);

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
                BrainSlotRoles.LeftHemisphere => "brain-slot:left-hemisphere",
                BrainSlotRoles.RightHemisphere => "brain-slot:right-hemisphere",
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

    private sealed class FakeBrainSlotChatClient(string output) : IBrainSlotChatClient
    {
        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
            => Task.FromResult(output);
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

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
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
            IBrainSlotContextAdmissionService contextAdmission)
        {
            Db = db;
            Service = service;
            ChatClientFactory = chatClientFactory;
            ContextAdmission = contextAdmission;
        }

        public McpDbContext Db { get; }

        public BrainSlotInvocationService Service { get; }

        public IBrainSlotChatClientFactory ChatClientFactory { get; }

        public IBrainSlotContextAdmissionService ContextAdmission { get; }

        public static InvocationFixture Create(
            bool executionEnabled,
            BrainSlotDefinitionEntity slot,
            ITurnTransactionCoordinator? coordinator = null,
            string output = "provider output")
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
            chatClientFactory.Create(slot, "resolved-secret")
                .Returns(new FakeBrainSlotChatClient(output));
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
                coordinator ?? new FakeTurnTransactionCoordinator());
            return new InvocationFixture(db, service, chatClientFactory, contextAdmission);
        }

        public void Dispose() => Db.Dispose();
    }
}
