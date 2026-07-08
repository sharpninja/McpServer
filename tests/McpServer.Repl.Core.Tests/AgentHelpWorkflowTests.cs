using System.Net;
using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-HELP-008: Regression coverage for the production REPL Agent Help workflow.
/// </summary>
public sealed class AgentHelpWorkflowTests
{
    private static readonly McpServerClientOptions Options = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>Verifies that create session posts through the typed Agent Help client.</summary>
    [Fact]
    public async Task CreateSessionAsync_PostsAgentHelpSessionRequest()
    {
        var handler = new JsonHandler("""
            {"sessionId":"help-001","status":"created","executionStrategy":"stub"}
            """);
        using var http = new HttpClient(handler);
        var sut = new AgentHelpWorkflow(new AgentHelpClient(http, Options));

        var result = await sut.CreateSessionAsync(new AgentHelpSessionCreateRequest
        {
            WorkspacePath = "F:\\GitHub\\McpServer",
            Topic = "marker trust",
            AgentSeed = "callerAgent=Codex",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("help-001", result.SessionId);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("/mcpserver/agent-help/session", handler.LastRequest.RequestUri!.OriginalString, StringComparison.Ordinal);
        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal("F:\\GitHub\\McpServer", document.RootElement.GetProperty("workspacePath").GetString());
    }

    /// <summary>Verifies that submit turn posts to the expected REST path.</summary>
    [Fact]
    public async Task SubmitTurnAsync_PostsTurnRequest()
    {
        var handler = new JsonHandler("""
            {"sessionId":"help-001","turnId":"turn-001","status":"completed","latencyMs":1}
            """);
        using var http = new HttpClient(handler);
        var sut = new AgentHelpWorkflow(new AgentHelpClient(http, Options));

        var result = await sut.SubmitTurnAsync(
            "help-001",
            new AgentHelpTurnRequest { UserMessage = "Need help with marker trust." },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("turn-001", result.TurnId);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agent-help/session/help-001/turn", handler.LastRequest.RequestUri!.OriginalString, StringComparison.Ordinal);
    }

    /// <summary>Verifies that get status reads from the expected REST path.</summary>
    [Fact]
    public async Task GetStatusAsync_GetsSessionStatus()
    {
        var handler = new JsonHandler("""
            {"sessionId":"help-001","status":"created","createdUtc":"2026-07-08T00:00:00Z","lastUpdatedUtc":"2026-07-08T00:00:00Z","executionStrategy":"stub"}
            """);
        using var http = new HttpClient(handler);
        var sut = new AgentHelpWorkflow(new AgentHelpClient(http, Options));

        var result = await sut.GetStatusAsync("help-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("help-001", result.SessionId);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agent-help/session/help-001", handler.LastRequest.RequestUri!.OriginalString, StringComparison.Ordinal);
    }

    private sealed class JsonHandler : HttpMessageHandler
    {
        private readonly string _json;

        public JsonHandler(string json)
        {
            _json = json;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}