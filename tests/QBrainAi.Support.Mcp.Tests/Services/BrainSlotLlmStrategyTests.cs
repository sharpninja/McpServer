using System.Text.Json;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.AI;
using QBrainAi.Support.Mcp.Storage.Entities;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-LLMSTRATEGY-001: Per-role completion strategies, shared turn-context identity, and HTTP flattening.
/// </summary>
/// <remarks>
/// Requirement coverage named by requirement evidence: FR-MCP-LLMSTRATEGY-001 (its first criterion, which carries no
/// criterion id, through CreateStrategy_TwoRoles_ResolvesTwoDifferentStrategyTypes).
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>FR-MCP-LLMSTRATEGY-001 (criterion without an id): cites the criterion whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (Factory_CreateStrategy_ExistsWithoutNewAgentSdk) and the helpers only it uses (RepoRoot) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class BrainSlotLlmStrategyTests
{
    /// <summary>
    /// TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001 (its first criterion, which carries no criterion id);
    /// TR-MCP-LLMSTRATEGY-001: the brain-slot chat client factory resolves a non-null completion strategy for each of
    /// two roles, and the two strategies are of different types. Data: a default BrainSlotChatClientFactory, a
    /// Creativity slot of provider kind OpenAICompatible at http://localhost:11434/v1 and a Logic slot of provider kind
    /// OpenAI at https://api.openai.com/v1, both built by the Slot helper, and the credential test-key.
    /// </summary>
    /// <remarks>2026-11-07, TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001; TR-MCP-LLMSTRATEGY-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void CreateStrategy_TwoRoles_ResolvesTwoDifferentStrategyTypes()
    {
        var factory = new BrainSlotChatClientFactory();
        var creativity = Slot(BrainSlotRoles.Creativity, "OpenAICompatible", "http://localhost:11434/v1");
        var logic = Slot(BrainSlotRoles.Logic, "OpenAI", "https://api.openai.com/v1");

        var creativityStrategy = factory.CreateStrategy(creativity, "test-key");
        var logicStrategy = factory.CreateStrategy(logic, "test-key");

        Assert.NotNull(creativityStrategy);
        Assert.NotNull(logicStrategy);
        Assert.NotEqual(creativityStrategy.GetType(), logicStrategy.GetType());
    }

    /// <summary>
    /// TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001 (its second criterion, which carries no criterion id): two
    /// completion strategies called through the IBrainSlotCompletionStrategy seam with one turn context each receive
    /// that same instance (Assert.Same). Data: a BrainSlotTurnContext with original input shared-original, session
    /// sess-1, turn turn-1, transaction txn-1 and committed Creativity evidence draft; two RecordingCompletionStrategy
    /// test doubles that record the context they receive; a Creativity OpenAICompatible slot from the Slot helper.
    /// </summary>
    /// <remarks>2026-11-07, TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CompletionStrategy_ReceivesSharedTurnContextByReference()
    {
        var context = new BrainSlotTurnContext
        {
            OriginalInput = "shared-original",
            SessionId = "sess-1",
            TurnId = "turn-1",
            TransactionId = "txn-1",
        };
        context.SetCommittedEvidence(BrainSlotRoles.Creativity, "draft");
        var first = new RecordingCompletionStrategy();
        var second = new RecordingCompletionStrategy();
        var slot = Slot(BrainSlotRoles.Creativity, "OpenAICompatible", "http://localhost:11434/v1");

        await first.CompleteAsync(slot, "role-prompt-a", context, temperature: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await second.CompleteAsync(slot, "role-prompt-b", context, temperature: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Same(context, first.ReceivedContext);
        Assert.Same(context, second.ReceivedContext);
        Assert.Same(first.ReceivedContext, second.ReceivedContext);
    }

    /// <summary>
    /// TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001 (its third criterion, which carries no criterion id);
    /// TR-MCP-LLMSTRATEGY-001: the OpenAI-compatible request JSON and the OpenAI chat messages built from a slot, a
    /// role prompt and a turn context both carry the role prompt, the original input, the session id and the committed
    /// Creativity evidence (the request JSON also the turn and transaction ids), and the OpenAI options carry the
    /// temperature 0.2. Data: a Logic OpenAICompatible slot from the Slot helper, the role prompt role-prompt-logic,
    /// and a BrainSlotTurnContext with original input original-user-input, session sess-ctx, turn turn-ctx, transaction
    /// txn-ctx and Creativity evidence draft-a.
    /// </summary>
    /// <remarks>2026-11-07, TEST-MCP-LLMSTRATEGY-001; FR-MCP-LLMSTRATEGY-001; TR-MCP-LLMSTRATEGY-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void BuildOpenAiCompatibleRequestJson_WhenTurnContextSupplied_IncludesOriginalInputAndTurnIds()
    {
        var slot = Slot(BrainSlotRoles.Logic, "OpenAICompatible", "http://localhost:11434/v1");
        var context = new BrainSlotTurnContext
        {
            OriginalInput = "original-user-input",
            SessionId = "sess-ctx",
            TurnId = "turn-ctx",
            TransactionId = "txn-ctx",
        };
        context.SetCommittedEvidence(BrainSlotRoles.Creativity, "draft-a");

        using var document = JsonDocument.Parse(BrainSlotChatClientFactory.BuildOpenAiCompatibleRequestJson(slot, "role-prompt-logic", context, 0.2));
        var messages = document.RootElement.GetProperty("messages").EnumerateArray().Select(static m => m.GetProperty("content").GetString() ?? string.Empty).ToArray();
        var joined = string.Join('\n', messages);

        Assert.Contains("role-prompt-logic", joined, StringComparison.Ordinal);
        Assert.Contains("original-user-input", joined, StringComparison.Ordinal);
        Assert.Contains("sessionId=sess-ctx", joined, StringComparison.Ordinal);
        Assert.Contains("turnId=turn-ctx", joined, StringComparison.Ordinal);
        Assert.Contains("transactionId=txn-ctx", joined, StringComparison.Ordinal);
        Assert.Contains("evidence.Creativity=draft-a", joined, StringComparison.Ordinal);

        var chatMessages = BrainSlotChatClientFactory.BuildOpenAiMessages(slot, "role-prompt-logic", context);
        var joinedMessages = string.Join('\n', chatMessages.Select(static m => m.Text));
        Assert.Contains("role-prompt-logic", joinedMessages, StringComparison.Ordinal);
        Assert.Contains("original-user-input", joinedMessages, StringComparison.Ordinal);
        Assert.Contains("sessionId=sess-ctx", joinedMessages, StringComparison.Ordinal);
        Assert.Contains("evidence.Creativity=draft-a", joinedMessages, StringComparison.Ordinal);
        var options = BrainSlotChatClientFactory.BuildOpenAiOptions(slot, 0.2);
        Assert.Equal(0.2f, options.Temperature);
    }

    private static BrainSlotDefinitionEntity Slot(string role, string providerKind, string endpoint)
        => new()
        {
            WorkspaceId = string.Empty,
            SlotId = role.ToLowerInvariant() + "-main",
            Role = role,
            ProviderKind = providerKind,
            ModelId = "model",
            Endpoint = endpoint,
            CredentialReference = "env:TEST_KEY",
            PartyId = "brain-slot:" + role.ToLowerInvariant(),
            Enabled = true,
            TimeoutSeconds = 30,
            MaxOutputTokens = 1024,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

    private sealed class RecordingCompletionStrategy : IBrainSlotCompletionStrategy
    {
        public BrainSlotTurnContext? ReceivedContext { get; private set; }

        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            ReceivedContext = context;
            return Task.FromResult("ok");
        }
    }
}
