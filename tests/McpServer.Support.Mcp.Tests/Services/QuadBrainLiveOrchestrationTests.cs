using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBLIVE-001: Exercises the real four-role Quad-Brain loop (FR-MCP-134, FR-MCP-135) end to end.
/// The real <see cref="QuadBrainOrchestrationService"/>, <see cref="BrainSlotInvocationService"/>, and
/// <see cref="BrainSlotRegistryService"/> run over an in-memory database with the real in-memory key server
/// (so party signing keys are provisioned exactly as in production). Only the per-brain LLM call
/// (<see cref="IBrainSlotChatClientFactory"/>) and the transaction-commit machinery
/// (a committing <see cref="FakeTurnTransactionCoordinator"/>, independently covered by the ACID suite) are faked.
/// </summary>
public sealed class QuadBrainLiveOrchestrationTests
{
    private const string WorkspacePath = @"F:\GitHub\McpServer";

    /// <summary>All four roles are invoked in order and the committed Arbiter decision is returned.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WithRealServicesAndFakeBrains_CommitsArbiterDecision()
    {
        using var harness = await LiveQuadHarness.CreateAsync(executionEnabled: true, arbiterOutput: "final decision").ConfigureAwait(true);

        var response = await harness.Orchestration.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-1",
        }).ConfigureAwait(true);

        Assert.Equal("committed", response.Status);
        Assert.Equal("final decision", response.Output);
        Assert.Equal(4, response.RoleResults.Count);
        Assert.Equal(
            [BrainSlotRoles.LeftHemisphere, BrainSlotRoles.RightHemisphere, BrainSlotRoles.CuriosityEngine, BrainSlotRoles.ArbiterOfTruth],
            response.RoleResults.Select(result => result.Role).ToArray());
        Assert.Equal(
            [BrainSlotRoles.LeftHemisphere, BrainSlotRoles.RightHemisphere, BrainSlotRoles.CuriosityEngine, BrainSlotRoles.ArbiterOfTruth],
            harness.Factory.InvokedRoles.ToArray());
    }

    /// <summary>A tool_calls payload from the Arbiter is returned verbatim as the orchestration output.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenArbiterEmitsToolCalls_ReturnsToolCallJsonAsOutput()
    {
        const string toolCalls = "{\"tool_calls\":[{\"name\":\"write_file\",\"arguments\":{\"path\":\"a.cs\"}}]}";
        using var harness = await LiveQuadHarness.CreateAsync(executionEnabled: true, arbiterOutput: toolCalls).ConfigureAwait(true);

        var response = await harness.Orchestration.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "do the task",
            TurnId = "turn-2",
        }).ConfigureAwait(true);

        Assert.Equal("committed", response.Status);
        Assert.Equal(toolCalls, response.Output);
    }

    /// <summary>When a role commits no usable output mid-loop, orchestration rejects before reaching the Arbiter.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenARoleProducesNoOutput_RejectsBeforeArbiter()
    {
        using var harness = await LiveQuadHarness.CreateAsync(
            executionEnabled: true, arbiterOutput: "final decision", emptyOutputRole: BrainSlotRoles.CuriosityEngine).ConfigureAwait(true);

        var response = await harness.Orchestration.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-partial",
        }).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        // Left + Right + Curiosity were invoked; the Arbiter is never reached because Curiosity did not commit output.
        Assert.Contains(BrainSlotRoles.CuriosityEngine, harness.Factory.InvokedRoles);
        Assert.DoesNotContain(BrainSlotRoles.ArbiterOfTruth, harness.Factory.InvokedRoles);
    }

    /// <summary>With only three roles enabled the loop rejects without producing a decision.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenOnlyThreeRolesSeeded_RejectsQuadNotReady()
    {
        using var harness = await LiveQuadHarness.CreateAsync(
            executionEnabled: true,
            arbiterOutput: "final decision",
            seedRoles: [BrainSlotRoles.LeftHemisphere, BrainSlotRoles.RightHemisphere, BrainSlotRoles.CuriosityEngine]).ConfigureAwait(true);

        var response = await harness.Orchestration.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-3",
        }).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.QuadNotReady, response.Reason);
        Assert.Empty(harness.Factory.InvokedRoles);
    }

    /// <summary>When execution is disabled no brain is called and the loop rejects.</summary>
    [Fact]
    public async Task ExecuteFullOrchestrationAsync_WhenExecutionDisabled_RejectsWithoutCallingBrains()
    {
        using var harness = await LiveQuadHarness.CreateAsync(executionEnabled: false, arbiterOutput: "final decision").ConfigureAwait(true);

        var response = await harness.Orchestration.ExecuteFullOrchestrationAsync(new QuadBrainOrchestrationRequest
        {
            Input = "decide this",
            TurnId = "turn-4",
        }).ConfigureAwait(true);

        Assert.Equal("rejected", response.Status);
        Assert.Equal(BrainSlotReasonCodes.ExecutionDisabled, response.Reason);
        Assert.Empty(harness.Factory.InvokedRoles);
    }

    private sealed class LiveQuadHarness : IDisposable
    {
        private readonly McpDbContext _db;
        private readonly InMemoryKeyServerService _keyServer;

        private LiveQuadHarness(
            McpDbContext db,
            InMemoryKeyServerService keyServer,
            QuadBrainOrchestrationService orchestration,
            RecordingChatClientFactory factory)
        {
            _db = db;
            _keyServer = keyServer;
            Orchestration = orchestration;
            Factory = factory;
        }

        public QuadBrainOrchestrationService Orchestration { get; }

        public RecordingChatClientFactory Factory { get; }

        public static async Task<LiveQuadHarness> CreateAsync(
            bool executionEnabled,
            string arbiterOutput,
            IReadOnlyList<string>? seedRoles = null,
            string? emptyOutputRole = null)
        {
            var workspace = new WorkspaceContext { WorkspacePath = WorkspacePath };
            var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
                .UseInMemoryDatabase("quad-live-" + Guid.NewGuid().ToString("N"))
                .Options;
            var db = new McpDbContext(dbOptions, workspace);
            var keyServer = new InMemoryKeyServerService(
                new StaticOptionsMonitor<KeyServerOptions>(new KeyServerOptions()),
                new TransactionManifestCanonicalizer());
            var resolver = new StubCredentialResolver();
            var brainOptions = new StaticOptionsMonitor<BrainSlotOptions>(new BrainSlotOptions
            {
                ExecutionEnabled = executionEnabled,
                DefaultTimeoutSeconds = 30,
                MaxTimeoutSeconds = 300,
            });
            var txnOptions = new StaticOptionsMonitor<TurnTransactionOptions>(new TurnTransactionOptions
            {
                Enabled = true,
                RequiredForMutations = true,
            });
            var coordinator = new FakeTurnTransactionCoordinator();

            var registry = new BrainSlotRegistryService(
                db, keyServer, resolver, brainOptions, NullLogger<BrainSlotRegistryService>.Instance);
            var factory = new RecordingChatClientFactory(arbiterOutput, emptyOutputRole);
            var contextAdmission = new StubContextAdmissionService();
            var invocation = new BrainSlotInvocationService(
                db, registry, resolver, factory, contextAdmission, keyServer,
                brainOptions, txnOptions, NullLogger<BrainSlotInvocationService>.Instance, coordinator);
            var orchestration = new QuadBrainOrchestrationService(
                db, registry, invocation, txnOptions, NullLogger<QuadBrainOrchestrationService>.Instance, coordinator);

            foreach (var role in seedRoles ?? BrainSlotRoles.All)
            {
                await registry.UpsertAsync(role.ToLowerInvariant() + "-main", new UpsertBrainSlotRequest
                {
                    Role = role,
                    ProviderKind = "OpenAI",
                    ModelId = "gpt-test",
                    CredentialReference = "env:BRAIN_SLOT_TEST_KEY",
                    Enabled = true,
                    TimeoutSeconds = 30,
                    MaxOutputTokens = 1024,
                }).ConfigureAwait(false);
            }

            return new LiveQuadHarness(db, keyServer, orchestration, factory);
        }

        public void Dispose()
        {
            _db.Dispose();
            _keyServer.Dispose();
        }
    }

    private sealed class RecordingChatClientFactory(string arbiterOutput, string? emptyOutputRole = null) : IBrainSlotChatClientFactory
    {
        public List<string> InvokedRoles { get; } = [];

        public IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential)
            => new RecordingChatClient(this, arbiterOutput, emptyOutputRole);

        private sealed class RecordingChatClient(RecordingChatClientFactory owner, string arbiterOutput, string? emptyOutputRole)
            : IBrainSlotChatClient
        {
            public Task<string> CompleteAsync(BrainSlotDefinitionEntity slot, string input, CancellationToken cancellationToken = default)
            {
                owner.InvokedRoles.Add(slot.Role);
                if (string.Equals(slot.Role, emptyOutputRole, StringComparison.Ordinal))
                    return Task.FromResult(string.Empty);
                var output = string.Equals(slot.Role, BrainSlotRoles.ArbiterOfTruth, StringComparison.Ordinal)
                    ? arbiterOutput
                    : slot.Role + " evidence";
                return Task.FromResult(output);
            }
        }
    }

    private sealed class StubContextAdmissionService : IBrainSlotContextAdmissionService
    {
        public Task<string?> AdmitAsync(BrainSlotDefinitionEntity slot, string output, string transactionId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class StubCredentialResolver : IBrainSlotCredentialResolver
    {
        public Task<string?> ResolveAsync(string credentialReference, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("resolved-secret");

        public bool IsSupportedReference(string credentialReference)
            => !string.IsNullOrWhiteSpace(credentialReference);
    }

    private sealed class FakeTurnTransactionCoordinator : ITurnTransactionCoordinator
    {
        public async Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
        {
            var mutationResult = await mutation(cancellationToken).ConfigureAwait(false);
            return new TurnTransactionResult
            {
                TransactionId = "txn-" + Guid.NewGuid().ToString("N"),
                Status = "committed",
                DiffgramId = "diffgram-1",
                MutationResult = mutationResult,
                MutationApplied = mutationResult.Success,
            };
        }

        public TurnTransactionStatusResponse GetStatus()
            => new() { Enabled = true, Degraded = false };
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
