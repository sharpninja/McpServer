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

/// <summary>Tests for full Quad-Brain orchestration, AoT reconciliation, and weight updates. TEST-MCP-181 through TEST-MCP-183.</summary>
public sealed class QuadBrainOrchestrationServiceTests
{
    private const string Workspace = @"F:\GitHub\McpServer";

    /// <summary>Full orchestration invokes Left, Right, Curiosity, then Arbiter and returns the committed final output.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenQuadReady_ReturnsCommittedAotDecision()
    {
        using var db = CreateDbContext();
        var registry = Substitute.For<IBrainSlotRegistryService>();
        var invocation = Substitute.For<IBrainSlotInvocationService>();
        var slots = BrainSlotRoles.All.Select(role => Slot(role)).ToDictionary(slot => slot.Role, StringComparer.Ordinal);
        registry.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new BrainSlotStatusResponse
            {
                QuadReady = true,
                RoleReadiness = BrainSlotRoles.All.ToDictionary(role => role, _ => true, StringComparer.Ordinal),
            });
        foreach (var pair in slots)
        {
            registry.GetEnabledEntityForRoleAsync(pair.Key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<BrainSlotDefinitionEntity?>(pair.Value));
        }

        invocation.InvokeAsync(Arg.Any<string>(), Arg.Any<BrainSlotInvokeRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var slotId = (string)call[0]!;
                var slot = slots.Values.Single(item => item.SlotId == slotId);
                return Task.FromResult(new BrainSlotInvokeResponse
                {
                    Status = "committed",
                    Reason = BrainSlotReasonCodes.None,
                    SlotId = slot.SlotId,
                    Role = slot.Role,
                    ModelId = slot.ModelId,
                    TransactionId = "txn-" + slot.Role,
                    DiffgramId = "diff-" + slot.Role,
                    Output = slot.Role == BrainSlotRoles.ArbiterOfTruth ? "final decision" : slot.Role + " evidence",
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    CompletedAtUtc = DateTimeOffset.UtcNow,
                });
            });
        var service = CreateService(db, registry, invocation);

        var response = await service.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-1",
            AdmitCuriosityToGraphRag = true,
        }).ConfigureAwait(true);

        Assert.Equal("committed", response.Status);
        Assert.Equal("final decision", response.Output);
        Assert.Equal("txn-ArbiterOfTruth", response.TransactionId);
        Assert.Equal(4, response.RoleResults.Count);
        Assert.Equal([BrainSlotRoles.LeftHemisphere, BrainSlotRoles.RightHemisphere, BrainSlotRoles.CuriosityEngine, BrainSlotRoles.ArbiterOfTruth], response.RoleResults.Select(item => item.Role).ToArray());
        var calledSlotIds = invocation.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IBrainSlotInvocationService.InvokeAsync))
            .Select(call => (string)call.GetArguments()[0]!)
            .ToArray();
        Assert.Equal(["lefthemisphere-main", "righthemisphere-main", "curiosityengine-main", "arbiteroftruth-main"], calledSlotIds);
    }

    /// <summary>Full orchestration rejects before provider calls when the workspace is not quad-ready.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenNotQuadReady_DoesNotInvokeAnySlot()
    {
        using var db = CreateDbContext();
        var registry = Substitute.For<IBrainSlotRegistryService>();
        var invocation = Substitute.For<IBrainSlotInvocationService>();
        registry.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new BrainSlotStatusResponse
            {
                QuadReady = false,
                MissingRoles = [BrainSlotRoles.ArbiterOfTruth],
            });
        var service = CreateService(db, registry, invocation);

        var response = await service.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-1",
        }).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.QuadNotReady, response.Reason);
        Assert.Empty(response.RoleResults);
        await invocation.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, default).ConfigureAwait(true);
    }

    /// <summary>Approved weight updates persist role weights, increment versions, and write audit rows.</summary>
    [Fact]
    public async Task ExecuteWeightUpdateAsync_WhenApproved_PersistsWeightsAndAudits()
    {
        using var db = CreateDbContext();
        var slot = Slot(BrainSlotRoles.LeftHemisphere);
        slot.OrchestrationWeight = 1.0;
        slot.WeightVersion = 7;
        db.BrainSlotDefinitions.Add(slot);
        await db.SaveChangesAsync().ConfigureAwait(true);
        var coordinator = new FakeTurnTransactionCoordinator();
        var service = CreateService(db, Substitute.For<IBrainSlotRegistryService>(), Substitute.For<IBrainSlotInvocationService>(), coordinator);

        var response = await service.ExecuteWeightUpdateAsync(new QuadBrainWeightUpdateRequest
        {
            RoleWeights = new Dictionary<string, double> { [BrainSlotRoles.LeftHemisphere] = 1.5 },
            ExpectedVersions = new Dictionary<string, int> { [BrainSlotRoles.LeftHemisphere] = 7 },
            ReasonText = "AoT-approved safety gate adjustment",
            ProposedBy = "Codex",
            AotApproved = true,
            AdminApproved = true,
            SafetyGatesPassed = true,
            TurnId = "turn-weight",
        }).ConfigureAwait(true);

        var persisted = await db.BrainSlotDefinitions.SingleAsync(item => item.SlotId == slot.SlotId).ConfigureAwait(true);
        Assert.Equal("committed", response.Status);
        Assert.Equal("brain-slot.weight-update", coordinator.LastRequest!.OperationName);
        Assert.Equal(1.5, persisted.OrchestrationWeight);
        Assert.Equal(8, persisted.WeightVersion);
        Assert.Single(response.Snapshots);
        Assert.Contains(db.DataAuditLogs, audit => audit.Action == "weight_update" && audit.EntityKey == slot.SlotId);
    }

    /// <summary>Weight updates without all required approvals are rejected without mutation.</summary>
    [Fact]
    public async Task ExecuteWeightUpdateAsync_WhenApprovalMissing_DoesNotMutate()
    {
        using var db = CreateDbContext();
        var slot = Slot(BrainSlotRoles.LeftHemisphere);
        db.BrainSlotDefinitions.Add(slot);
        await db.SaveChangesAsync().ConfigureAwait(true);
        var service = CreateService(db, Substitute.For<IBrainSlotRegistryService>(), Substitute.For<IBrainSlotInvocationService>());

        var response = await service.ExecuteWeightUpdateAsync(new QuadBrainWeightUpdateRequest
        {
            RoleWeights = new Dictionary<string, double> { [BrainSlotRoles.LeftHemisphere] = 2.0 },
            ReasonText = "missing approvals",
            AotApproved = true,
            AdminApproved = false,
            SafetyGatesPassed = true,
        }).ConfigureAwait(true);

        var persisted = await db.BrainSlotDefinitions.SingleAsync(item => item.SlotId == slot.SlotId).ConfigureAwait(true);
        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.WeightUpdateRejected, response.Reason);
        Assert.Equal(1.0, persisted.OrchestrationWeight);
        Assert.Equal(0, persisted.WeightVersion);
        Assert.DoesNotContain(db.DataAuditLogs, audit => audit.Action == "weight_update");
    }

    private static QuadBrainOrchestrationService CreateService(
        McpDbContext db,
        IBrainSlotRegistryService registry,
        IBrainSlotInvocationService invocation,
        ITurnTransactionCoordinator? coordinator = null)
        => new(
            db,
            registry,
            invocation,
            Monitor(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }),
            NullLogger<QuadBrainOrchestrationService>.Instance,
            coordinator ?? new FakeTurnTransactionCoordinator());

    private static McpDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase("quad-brain-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new McpDbContext(options, new WorkspaceContext { WorkspacePath = Workspace });
    }

    private static BrainSlotDefinitionEntity Slot(string role)
        => new()
        {
            WorkspaceId = Workspace,
            SlotId = role.ToLowerInvariant() + "-main",
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
            OrchestrationWeight = 1.0,
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

    private sealed class FakeTurnTransactionCoordinator : ITurnTransactionCoordinator
    {
        public TurnTransactionRequest? LastRequest { get; private set; }

        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            var mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
            return new TurnTransactionResult
            {
                TransactionId = request.TransactionId ?? "txn-weight",
                Status = "committed",
                DiffgramId = "diff-weight",
                MutationApplied = true,
                MutationResult = mutationResult,
            };
        }

        public TurnTransactionStatusResponse GetStatus()
            => new() { Enabled = true, Degraded = false };
    }
}
