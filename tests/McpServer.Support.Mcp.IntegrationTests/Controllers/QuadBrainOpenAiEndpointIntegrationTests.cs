using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-QBINT-001: Integration tests for the QuadBrain OpenAI-compatible endpoint (POST /v1/chat/completions)
/// exercising FR-MCP-QBOPENAI-001 and FR-MCP-QBEXEC-001 end to end through the real ASP.NET pipeline with the
/// orchestration and internal-tool executor replaced by deterministic test doubles.
/// </summary>
public sealed class QuadBrainOpenAiEndpointIntegrationTests
{
    private const string Endpoint = "v1/chat/completions";

    /// <summary>A request without a token is rejected with 401.</summary>
    [Fact]
    public async Task ChatCompletions_NoToken_Returns401()
    {
        var orchestration = new FakeOrchestration { Output = "hi" };
        using var factory = BuildFactory(orchestration, new FakeExecutor());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(new Uri(Endpoint, UriKind.Relative), SimpleRequest()).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>A plain Arbiter decision is returned as the assistant message.</summary>
    [Fact]
    public async Task ChatCompletions_Authorized_ReturnsArbiterContent()
    {
        var orchestration = new FakeOrchestration { Output = "the arbiter decision" };
        using var factory = BuildFactory(orchestration, new FakeExecutor());
        using var client = Authorized(factory);

        var body = await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Equal("the arbiter decision", choice.Message.Content);
    }

    /// <summary>An external tool elected by QuadBrain is returned to the agent as a tool call.</summary>
    [Fact]
    public async Task ChatCompletions_ExternalTool_ReturnedAsToolCall()
    {
        var orchestration = new FakeOrchestration
        {
            Output = "{\"tool_calls\":[{\"name\":\"edit_local_file\",\"arguments\":{\"path\":\"a.txt\"}}]}",
        };
        using var factory = BuildFactory(orchestration, new FakeExecutor());
        using var client = Authorized(factory);

        var body = await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("tool_calls", choice.FinishReason);
        Assert.Equal("edit_local_file", Assert.Single(choice.Message.ToolCalls!).Function.Name);
    }

    /// <summary>An MCP-internal tool that executes server-side is stripped from the response.</summary>
    [Fact]
    public async Task ChatCompletions_InternalToolExecuted_IsStripped()
    {
        var orchestration = new FakeOrchestration
        {
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{\"id\":\"X\"}}]}",
        };
        using var factory = BuildFactory(orchestration, new FakeExecutor("mcp_todo_update"));
        using var client = Authorized(factory);

        var body = await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
    }

    /// <summary>An internal tool failure is surfaced as a note, not a tool call.</summary>
    [Fact]
    public async Task ChatCompletions_InternalToolFailure_BecomesNote()
    {
        var orchestration = new FakeOrchestration
        {
            Output = "{\"tool_calls\":[{\"name\":\"mcp_todo_update\",\"arguments\":{}}]}",
        };
        using var factory = BuildFactory(orchestration, new FakeExecutor(failed: "mcp_todo_update"));
        using var client = Authorized(factory);

        var body = await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        var choice = Assert.Single(body.Choices);
        Assert.Equal("stop", choice.FinishReason);
        Assert.Null(choice.Message.ToolCalls);
        Assert.Contains("mcp_todo_update", choice.Message.Content!, StringComparison.Ordinal);
    }

    private static CustomWebApplicationFactory BuildFactory(
        IQuadBrainOrchestrationService orchestration,
        IQuadBrainInternalToolExecutor executor)
        => new(services =>
        {
            services.RemoveAll<IQuadBrainOrchestrationService>();
            services.AddSingleton(orchestration);
            services.RemoveAll<IQuadBrainInternalToolExecutor>();
            services.AddSingleton(executor);
        });

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

    private sealed class FakeOrchestration : IQuadBrainOrchestrationService
    {
        public string Output { get; set; } = string.Empty;

        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new QuadBrainOrchestrationResponse { Status = "committed", Output = Output });

        public Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
            AotReconciliationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
            QuadBrainWeightUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeExecutor(string? handled = null, string? failed = null) : IQuadBrainInternalToolExecutor
    {
        public Task<InternalToolExecutionOutcome> TryExecuteAsync(
            OpenAiToolCall toolCall, string? turnId, CancellationToken cancellationToken = default)
        {
            if (failed is not null && toolCall.Function.Name == failed)
                return Task.FromResult(InternalToolExecutionOutcome.Fail("transaction rejected"));
            if (handled is not null && toolCall.Function.Name == handled)
                return Task.FromResult(InternalToolExecutionOutcome.Ok());
            return Task.FromResult(InternalToolExecutionOutcome.Unhandled);
        }
    }
}
