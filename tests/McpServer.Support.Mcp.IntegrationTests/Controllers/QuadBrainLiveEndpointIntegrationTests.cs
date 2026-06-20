using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-QBLIVEINT-001: Drives the REAL Quad-Brain orchestration loop (FR-MCP-134, FR-MCP-QBOPENAI-001,
/// FR-MCP-QBEXEC-001) through the OpenAI-compatible endpoint (POST /v1/chat/completions). Unlike the other
/// endpoint tests, <see cref="IQuadBrainOrchestrationService"/> is NOT replaced: the real orchestration runs
/// over four seeded slots with the real registry, invocation service, and key server. Only the per-brain LLM
/// call and the transaction-commit machinery (independently covered by the ACID suite) are faked.
/// </summary>
public sealed class QuadBrainLiveEndpointIntegrationTests
{
    private const string Endpoint = "v1/chat/completions";

    /// <summary>A plain Arbiter decision from the real loop is returned as the assistant message.</summary>
    [Fact]
    public async Task ChatCompletions_RealOrchestration_ReturnsArbiterContent()
    {
        var factory = new RecordingChatClientFactory("the live arbiter decision");
        using var app = BuildFactory(factory);
        using var seedClient = SeedClient(app);
        await SeedQuadAsync(seedClient, BrainSlotRoles.All).ConfigureAwait(true);
        using var client = Authorized(app);

        var body = await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Equal("the live arbiter decision", choice.Message.Content);
        Assert.Contains(BrainSlotRoles.ArbiterOfTruth, factory.InvokedRoles);
        Assert.Equal(4, factory.InvokedRoles.Count);
    }

    /// <summary>A tool_calls payload elected by the real Arbiter surfaces as an OpenAI tool call.</summary>
    [Fact]
    public async Task ChatCompletions_RealOrchestration_ArbiterToolCall_ReturnedAsToolCall()
    {
        var factory = new RecordingChatClientFactory(
            "{\"tool_calls\":[{\"name\":\"write_file\",\"arguments\":{\"path\":\"a.cs\"}}]}");
        using var app = BuildFactory(factory);
        using var seedClient = SeedClient(app);
        await SeedQuadAsync(seedClient, BrainSlotRoles.All).ConfigureAwait(true);
        using var client = Authorized(app);

        var body = await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        Assert.Equal("write_file", Assert.Single(choice.Message.ToolCalls!).Function.Name);
    }

    /// <summary>With no slots seeded the real loop rejects (QuadNotReady); the endpoint returns no decision.</summary>
    [Fact]
    public async Task ChatCompletions_RealOrchestration_QuadNotReady_ReturnsEmptyDecision()
    {
        var factory = new RecordingChatClientFactory("unused");
        using var app = BuildFactory(factory);
        using var client = Authorized(app);

        var body = await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.True(string.IsNullOrEmpty(choice.Message.Content));
        Assert.Empty(factory.InvokedRoles);
    }

    private static CustomWebApplicationFactory BuildFactory(RecordingChatClientFactory chatClientFactory)
        => new(
            services =>
            {
                services.RemoveAll<IBrainSlotChatClientFactory>();
                services.AddSingleton<IBrainSlotChatClientFactory>(chatClientFactory);
                services.RemoveAll<IBrainSlotCredentialResolver>();
                services.AddSingleton<IBrainSlotCredentialResolver, StubCredentialResolver>();
                services.RemoveAll<ITurnTransactionCoordinator>();
                services.AddSingleton<ITurnTransactionCoordinator, CommittingTurnTransactionCoordinator>();
            },
            new Dictionary<string, string?>
            {
                ["Mcp:BrainSlots:ExecutionEnabled"] = "true",
                ["Mcp:TurnTransactions:Enabled"] = "true",
                ["Mcp:TurnTransactions:RequiredForMutations"] = "true",
            });

    private static async Task SeedQuadAsync(HttpClient client, IReadOnlyList<string> roles)
    {
        foreach (var role in roles)
        {
            var response = await client.PutAsJsonAsync(
                new Uri($"mcpserver/brain-slots/{role.ToLowerInvariant()}-main", UriKind.Relative),
                new UpsertBrainSlotRequest
                {
                    Role = role,
                    ProviderKind = "OpenAI",
                    ModelId = "gpt-test",
                    CredentialReference = "env:BRAIN_SLOT_TEST_KEY",
                    Enabled = true,
                    TimeoutSeconds = 30,
                    MaxOutputTokens = 1024,
                }).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static HttpClient SeedClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);
        return client;
    }

    private static HttpClient Authorized(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);
        if (client.DefaultRequestHeaders.TryGetValues("X-Api-Key", out var keys))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", keys.First());
        return client;
    }

    private static OpenAiChatCompletionRequest SimpleRequest()
        => new() { Model = "qbagent", Messages = [new OpenAiChatMessage { Role = "user", Content = "do it" }] };

    private static async Task<OpenAiChatCompletionResponse> PostAsync(HttpClient client, OpenAiChatCompletionRequest request)
    {
        var response = await client.PostAsJsonAsync(new Uri(Endpoint, UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OpenAiChatCompletionResponse>().ConfigureAwait(true);
        Assert.NotNull(body);
        return body!;
    }

    private sealed class RecordingChatClientFactory(string arbiterOutput) : IBrainSlotChatClientFactory
    {
        private readonly object _gate = new();

        public List<string> InvokedRoles { get; } = [];

        public IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential)
            => new RecordingChatClient(this, arbiterOutput);

        private void Record(string role)
        {
            lock (_gate)
                InvokedRoles.Add(role);
        }

        private sealed class RecordingChatClient(RecordingChatClientFactory owner, string arbiterOutput) : IBrainSlotChatClient
        {
            public Task<string> CompleteAsync(BrainSlotDefinitionEntity slot, string input, CancellationToken cancellationToken = default)
            {
                owner.Record(slot.Role);
                var output = string.Equals(slot.Role, BrainSlotRoles.ArbiterOfTruth, StringComparison.Ordinal)
                    ? arbiterOutput
                    : slot.Role + " evidence";
                return Task.FromResult(output);
            }
        }
    }

    private sealed class StubCredentialResolver : IBrainSlotCredentialResolver
    {
        public Task<string?> ResolveAsync(string credentialReference, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("resolved-secret");

        public bool IsSupportedReference(string credentialReference)
            => !string.IsNullOrWhiteSpace(credentialReference);
    }

    private sealed class CommittingTurnTransactionCoordinator : ITurnTransactionCoordinator
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
}
