using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-QBINT-001: Integration tests for the QuadBrain OpenAI-compatible endpoint (POST /v1/chat/completions)
/// exercising FR-MCP-QBOPENAI-001 and FR-MCP-QBEXEC-001 end to end through the real ASP.NET pipeline with the
/// orchestration and internal-tool executor replaced by deterministic test doubles.
/// </summary>
[Trait("Category", "Integration")]
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

    /// <summary>FR-MCP-QBOPENAI-001 (G-016): an orchestration failure maps to an OpenAI-compatible 500 error envelope.</summary>
    [Fact]
    public async Task ChatCompletions_OrchestrationThrows_ReturnsOpenAiErrorEnvelope()
    {
        using var factory = BuildFactory(new ThrowingOrchestration(), new FakeExecutor());
        using var client = Authorized(factory);

        var response = await client.PostAsJsonAsync(new Uri(Endpoint, UriKind.Relative), SimpleRequest()).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("server_error", json, StringComparison.Ordinal);
        Assert.Contains("boom", json, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QUAD-SESSION-001: the X-Session-Id header attaches the run to its session (reaches orchestration).</summary>
    [Fact]
    public async Task ChatCompletions_WithSessionHeader_AttachesSessionToOrchestration()
    {
        var orchestration = new CapturingOrchestration { Output = "ok" };
        using var factory = BuildFactory(orchestration, new FakeExecutor());
        using var client = Authorized(factory);
        client.DefaultRequestHeaders.Add("X-Session-Id", "sess-42");

        await PostAsync(client, SimpleRequest()).ConfigureAwait(true);

        Assert.NotNull(orchestration.LastRequest);
        Assert.Equal("sess-42", orchestration.LastRequest!.Metadata["sessionId"]);
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-015): <c>stream:true</c> returns OpenAI-style SSE chunks and a terminal DONE event.</summary>
    [Fact]
    public async Task ChatCompletions_StreamTrue_ReturnsServerSentEvents()
    {
        var orchestration = new FakeOrchestration { Output = "streamed arbiter answer" };
        using var factory = BuildFactory(orchestration, new FakeExecutor());
        using var client = Authorized(factory);
        var request = SimpleRequest();
        request.Stream = true;

        var response = await client.PostAsJsonAsync(new Uri(Endpoint, UriKind.Relative), request).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("data:", body, StringComparison.Ordinal);
        Assert.Contains("chat.completion.chunk", body, StringComparison.Ordinal);
        Assert.Contains("streamed arbiter answer", body, StringComparison.Ordinal);
        Assert.Contains("data: [DONE]", body, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-QBOPENAI-001 (G-050): <c>/v1</c> requests resolve workspace context from the presented token and do not leak across workspaces.</summary>
    [Fact]
    public async Task ChatCompletions_TokenScopedWorkspaces_DoNotLeakWorkspaceContext()
    {
        var secondaryWorkspacePath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-qb-openai-secondary-{Guid.NewGuid():N}",
            "workspace");
        var secondaryDataPath = Path.Combine(Path.GetTempPath(), $"mcp-qb-openai-secondary-data-{Guid.NewGuid():N}");
        SeedMinimalWorkspaceFiles(secondaryWorkspacePath);
        Directory.CreateDirectory(secondaryDataPath);

        try
        {
            var capture = new WorkspaceCapture();
            var overrides = new Dictionary<string, string?>
            {
                { "Mcp:Workspaces:1:WorkspacePath", secondaryWorkspacePath },
                { "Mcp:Workspaces:1:Name", "qb-openai-secondary" },
                { "Mcp:Workspaces:1:TodoPath", Path.Combine(secondaryWorkspacePath, "docs", "Project", "TODO.yaml") },
                { "Mcp:Workspaces:1:DataDirectory", secondaryDataPath },
                { "Mcp:Workspaces:1:IsPrimary", "false" },
                { "Mcp:Workspaces:1:IsEnabled", "true" },
            };
            using var factory = new CustomWebApplicationFactory(
                services =>
                {
                    services.RemoveAll<IQuadBrainOrchestrationService>();
                    services.AddSingleton(capture);
                    services.AddScoped<IQuadBrainOrchestrationService, WorkspaceCapturingOrchestration>();
                    services.RemoveAll<IQuadBrainInternalToolExecutor>();
                    services.AddSingleton<IQuadBrainInternalToolExecutor>(new FakeExecutor());
                },
                overrides);
            using var primaryClient = factory.CreateClient();
            using var secondaryClient = factory.CreateClient();
            AddOpenAiBearer(primaryClient, factory.Services, factory.WorkspacePath);
            AddOpenAiBearer(secondaryClient, factory.Services, secondaryWorkspacePath);

            var primary = await PostAsync(primaryClient, SimpleRequest()).ConfigureAwait(true);
            var secondary = await PostAsync(secondaryClient, SimpleRequest()).ConfigureAwait(true);

            Assert.Equal("workspace:" + factory.WorkspacePath, Assert.Single(primary.Choices).Message.Content);
            Assert.Equal("workspace:" + secondaryWorkspacePath, Assert.Single(secondary.Choices).Message.Content);
            Assert.Contains(factory.WorkspacePath, capture.WorkspacePaths);
            Assert.Contains(secondaryWorkspacePath, capture.WorkspacePaths);
        }
        finally
        {
            TryDeleteDirectory(secondaryWorkspacePath);
            TryDeleteDirectory(secondaryDataPath);
            TryDeleteDirectory(Path.GetDirectoryName(secondaryWorkspacePath));
        }
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

    private static void AddOpenAiBearer(HttpClient client, IServiceProvider services, string workspacePath)
    {
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var token = tokenService.GetToken(workspacePath) ?? tokenService.GenerateToken(workspacePath);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Remove(WorkspaceResolutionMiddleware.WorkspacePathHeader);
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

    private sealed class CapturingOrchestration : IQuadBrainOrchestrationService
    {
        public string Output { get; set; } = string.Empty;

        public QuadBrainOrchestrationRequest? LastRequest { get; private set; }

        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new QuadBrainOrchestrationResponse { Status = "committed", Output = Output });
        }

        public Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
            AotReconciliationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
            QuadBrainWeightUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingOrchestration : IQuadBrainOrchestrationService
    {
        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");

        public Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
            AotReconciliationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
            QuadBrainWeightUpdateRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class WorkspaceCapture
    {
        private readonly object _gate = new();

        public List<string> WorkspacePaths { get; } = [];

        public void Add(string? workspacePath)
        {
            lock (_gate)
                WorkspacePaths.Add(workspacePath ?? string.Empty);
        }
    }

    private sealed class WorkspaceCapturingOrchestration(WorkspaceContext context, WorkspaceCapture capture) : IQuadBrainOrchestrationService
    {
        public Task<QuadBrainOrchestrationResponse> ExecuteFullOrchestrationAsync(
            QuadBrainOrchestrationRequest request,
            CancellationToken cancellationToken = default)
        {
            capture.Add(context.WorkspacePath);
            return Task.FromResult(new QuadBrainOrchestrationResponse
            {
                Status = "committed",
                Output = "workspace:" + context.WorkspacePath,
            });
        }

        public Task<AotReconciliationResponse> ExecuteAotReconciliationAsync(
            AotReconciliationRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuadBrainWeightUpdateResponse> ExecuteWeightUpdateAsync(
            QuadBrainWeightUpdateRequest request,
            CancellationToken cancellationToken = default)
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

    private static void SeedMinimalWorkspaceFiles(string workspacePath)
    {
        var projectPath = Path.Combine(workspacePath, "docs", "Project");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "TODO.yaml"), """
            mvp-app:
              high-priority: []
            """);
    }

    private static void TryDeleteDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
