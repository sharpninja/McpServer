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

    /// <summary>TEST-MCP-QBLIVE-001: Normal orchestration invokes Creativity, Logic, then Arbiter and returns the committed final output.</summary>
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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("committed", response.Status);
        Assert.Equal("final decision", response.Output);
        Assert.Equal("txn-ArbiterOfTruth", response.TransactionId);
        Assert.Equal(3, response.RoleResults.Count);
        Assert.Equal([BrainSlotRoles.Creativity, BrainSlotRoles.Logic, BrainSlotRoles.ArbiterOfTruth], response.RoleResults.Select(item => item.Role).ToArray());
        var calledSlotIds = invocation.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IBrainSlotInvocationService.InvokeAsync))
            .Select(call => (string)call.GetArguments()[0]!)
            .ToArray();
        Assert.Equal(["creativity-main", "logic-main", "arbiteroftruth-main"], calledSlotIds);
    }

    /// <summary>TEST-MCP-QBLIVE-001: Creativity and Logic roles run in parallel, and AoT waits for both responses.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_StartsLogicBeforeCreativityCompletesAndGatesAotOnBoth()
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

        var creativityStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logicStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var arbiterStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var creativityCompletion = new TaskCompletionSource<BrainSlotInvokeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logicCompletion = new TaskCompletionSource<BrainSlotInvokeResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        invocation.InvokeAsync(Arg.Any<string>(), Arg.Any<BrainSlotInvokeRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var slotId = (string)call[0]!;
                var slot = slots.Values.Single(item => item.SlotId == slotId);
                return slot.Role switch
                {
                    BrainSlotRoles.Creativity => StartAndWait(creativityStarted, creativityCompletion.Task),
                    BrainSlotRoles.Logic => StartAndWait(logicStarted, logicCompletion.Task),
                    BrainSlotRoles.ArbiterOfTruth => StartAndWait(
                        arbiterStarted,
                        Task.FromResult(Response(slot, "final decision"))),
                    _ => Task.FromResult(Response(slot, slot.Role + " evidence")),
                };
            });
        var service = CreateService(db, registry, invocation);

        var orchestration = service.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-parallel",
        }, cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            await creativityStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            await logicStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.False(arbiterStarted.Task.IsCompleted);

            creativityCompletion.SetResult(Response(slots[BrainSlotRoles.Creativity], "creativity evidence"));
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.False(arbiterStarted.Task.IsCompleted);

            logicCompletion.SetResult(Response(slots[BrainSlotRoles.Logic], "logic evidence"));
            await arbiterStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var response = await orchestration.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("committed", response.Status);
            Assert.Equal("final decision", response.Output);
            Assert.Equal(
                [BrainSlotRoles.Creativity, BrainSlotRoles.Logic, BrainSlotRoles.ArbiterOfTruth],
                response.RoleResults.Select(item => item.Role).ToArray());
        }
        finally
        {
            creativityCompletion.TrySetResult(Response(slots[BrainSlotRoles.Creativity], "creativity evidence"));
            logicCompletion.TrySetResult(Response(slots[BrainSlotRoles.Logic], "logic evidence"));
        }

        static Task<BrainSlotInvokeResponse> StartAndWait(
            TaskCompletionSource<bool> started,
            Task<BrainSlotInvokeResponse> completion)
        {
            started.TrySetResult(true);
            return completion;
        }

        static BrainSlotInvokeResponse Response(BrainSlotDefinitionEntity slot, string output)
            => new()
            {
                Status = "committed",
                Reason = BrainSlotReasonCodes.None,
                SlotId = slot.SlotId,
                Role = slot.Role,
                ModelId = slot.ModelId,
                TransactionId = "txn-" + slot.Role,
                DiffgramId = "diff-" + slot.Role,
                Output = output,
                StartedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = DateTimeOffset.UtcNow,
            };
    }

    /// <summary>TEST-MCP-QBLIVE-001: Quad roles carry role-specific descriptions and Logic uses provider temperature.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_BuildsRoleSpecificPromptInstructions()
    {
        using var db = CreateDbContext();
        var registry = Substitute.For<IBrainSlotRegistryService>();
        var invocation = Substitute.For<IBrainSlotInvocationService>();
        var slots = BrainSlotRoles.All.Select(role => Slot(role)).ToDictionary(slot => slot.Role, StringComparer.Ordinal);
        var prompts = new Dictionary<string, string>(StringComparer.Ordinal);
        var temperatures = new Dictionary<string, double?>(StringComparer.Ordinal);
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
                var request = (BrainSlotInvokeRequest)call[1]!;
                var slot = slots.Values.Single(item => item.SlotId == slotId);
                lock (prompts)
                {
                    prompts[slot.Role] = request.Input;
                    temperatures[slot.Role] = request.Temperature;
                }
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

        await service.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-prompt-contract",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains("creativity", prompts[BrainSlotRoles.Creativity], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("temperature", prompts[BrainSlotRoles.Creativity], StringComparison.OrdinalIgnoreCase);
        Assert.Null(temperatures[BrainSlotRoles.Creativity]);
        Assert.Contains("logical reasoning", prompts[BrainSlotRoles.Logic], StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0.0, temperatures[BrainSlotRoles.Logic]);
        Assert.Contains("arbiter of truth for code tasks", prompts[BrainSlotRoles.ArbiterOfTruth], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("enforcer of rules for all tasks", prompts[BrainSlotRoles.ArbiterOfTruth], StringComparison.OrdinalIgnoreCase);
        Assert.Null(temperatures[BrainSlotRoles.ArbiterOfTruth]);
    }

    /// <summary>TEST-MCP-QBLIVE-001: Curiosity is invoked only when both roles fail to produce valid committed output.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenBothHemispheresProduceNoValidOutput_InvokesCuriosityWithoutReturningIt()
    {
        using var db = CreateDbContext();
        var registry = Substitute.For<IBrainSlotRegistryService>();
        var invocation = Substitute.For<IBrainSlotInvocationService>();
        var slots = BrainSlotRoles.All.Select(role => Slot(role)).ToDictionary(slot => slot.Role, StringComparer.Ordinal);
        var capturedRequests = new List<(string SlotId, BrainSlotInvokeRequest Request)>();
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
                var request = (BrainSlotInvokeRequest)call[1]!;
                capturedRequests.Add((slotId, request));
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
                    Output = slot.Role == BrainSlotRoles.CuriosityEngine ? "frustration and research context" : string.Empty,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    CompletedAtUtc = DateTimeOffset.UtcNow,
                });
            });
        var service = CreateService(db, registry, invocation);

        var response = await service.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-curiosity",
            AdmitCuriosityToGraphRag = true,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Null(response.Output);
        Assert.Equal([BrainSlotRoles.Creativity, BrainSlotRoles.Logic, BrainSlotRoles.CuriosityEngine], response.RoleResults.Select(item => item.Role).ToArray());
        Assert.Equal(["creativity-main", "logic-main", "curiosityengine-main"], capturedRequests.Select(item => item.SlotId).ToArray());
        Assert.True(capturedRequests.Single(item => item.SlotId == "curiosityengine-main").Request.AdmitToGraphRag);
        Assert.Contains(
            "curious researcher",
            capturedRequests.Single(item => item.SlotId == "curiosityengine-main").Request.Input,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TEST-MCP-QBLIVE-001: Arbiter semantic rejection triggers a bounded voting/reconciliation round.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenArbiterRejectsInitialEvidence_RunsVotingRound()
    {
        using var db = CreateDbContext();
        var registry = Substitute.For<IBrainSlotRegistryService>();
        var invocation = Substitute.For<IBrainSlotInvocationService>();
        var slots = BrainSlotRoles.All.Select(role => Slot(role)).ToDictionary(slot => slot.Role, StringComparer.Ordinal);
        var invocationCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var votingPrompts = new List<(string Role, string Prompt, double? Temperature)>();
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
                var request = (BrainSlotInvokeRequest)call[1]!;
                var slot = slots.Values.Single(item => item.SlotId == slotId);
                invocationCounts.TryGetValue(slot.Role, out var count);
                invocationCounts[slot.Role] = ++count;
                if (count > 1)
                    votingPrompts.Add((slot.Role, request.Input, request.Temperature));
                var output = slot.Role switch
                {
                    BrainSlotRoles.ArbiterOfTruth when count == 1 => "REJECT: both role responses are not valid enough.",
                    BrainSlotRoles.ArbiterOfTruth => "final decision after voting",
                    _ when count > 1 => slot.Role + " vote",
                    _ => slot.Role + " evidence",
                };
                return Task.FromResult(new BrainSlotInvokeResponse
                {
                    Status = "committed",
                    Reason = BrainSlotReasonCodes.None,
                    SlotId = slot.SlotId,
                    Role = slot.Role,
                    ModelId = slot.ModelId,
                    TransactionId = "txn-" + slot.Role + "-" + count,
                    DiffgramId = "diff-" + slot.Role + "-" + count,
                    Output = output,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    CompletedAtUtc = DateTimeOffset.UtcNow,
                });
            });
        var service = CreateService(db, registry, invocation);

        var response = await service.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-vote",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("committed", response.Status);
        Assert.Equal("final decision after voting", response.Output);
        Assert.Equal(
            [BrainSlotRoles.Creativity, BrainSlotRoles.Logic, BrainSlotRoles.ArbiterOfTruth, BrainSlotRoles.Creativity, BrainSlotRoles.Logic, BrainSlotRoles.ArbiterOfTruth],
            response.RoleResults.Select(item => item.Role).ToArray());
        Assert.False(invocationCounts.ContainsKey(BrainSlotRoles.CuriosityEngine));
        Assert.DoesNotContain("temperature", votingPrompts.Single(item => item.Role == BrainSlotRoles.Creativity).Prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Null(votingPrompts.Single(item => item.Role == BrainSlotRoles.Creativity).Temperature);
        Assert.Equal(0.0, votingPrompts.Single(item => item.Role == BrainSlotRoles.Logic).Temperature);
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
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.QuadNotReady, response.Reason);
        Assert.Empty(response.RoleResults);
        await invocation.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>Approved weight updates persist role weights, increment versions, and write audit rows.</summary>
    [Fact]
    public async Task ExecuteWeightUpdateAsync_WhenApproved_PersistsWeightsAndAudits()
    {
        using var db = CreateDbContext();
        var slot = Slot(BrainSlotRoles.Creativity);
        slot.OrchestrationWeight = 1.0;
        slot.WeightVersion = 7;
        db.BrainSlotDefinitions.Add(slot);
        await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var coordinator = new FakeTurnTransactionCoordinator();
        var service = CreateService(db, Substitute.For<IBrainSlotRegistryService>(), Substitute.For<IBrainSlotInvocationService>(), coordinator);

        var response = await service.ExecuteWeightUpdateAsync(new QuadBrainWeightUpdateRequest
        {
            RoleWeights = new Dictionary<string, double> { [BrainSlotRoles.Creativity] = 1.5 },
            ExpectedVersions = new Dictionary<string, int> { [BrainSlotRoles.Creativity] = 7 },
            ReasonText = "AoT-approved safety gate adjustment",
            ProposedBy = "Codex",
            AotApproved = true,
            AdminApproved = true,
            SafetyGatesPassed = true,
            TurnId = "turn-weight",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var persisted = await db.BrainSlotDefinitions.SingleAsync(item => item.SlotId == slot.SlotId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
        var slot = Slot(BrainSlotRoles.Creativity);
        db.BrainSlotDefinitions.Add(slot);
        await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var service = CreateService(db, Substitute.For<IBrainSlotRegistryService>(), Substitute.For<IBrainSlotInvocationService>());

        var response = await service.ExecuteWeightUpdateAsync(new QuadBrainWeightUpdateRequest
        {
            RoleWeights = new Dictionary<string, double> { [BrainSlotRoles.Creativity] = 2.0 },
            ReasonText = "missing approvals",
            AotApproved = true,
            AdminApproved = false,
            SafetyGatesPassed = true,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var persisted = await db.BrainSlotDefinitions.SingleAsync(item => item.SlotId == slot.SlotId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
            // TR-MCP-QUAD-001: brain-slot definitions are global (stored under the global workspace "").
            WorkspaceId = string.Empty,
            SlotId = role.ToLowerInvariant() + "-main",
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
