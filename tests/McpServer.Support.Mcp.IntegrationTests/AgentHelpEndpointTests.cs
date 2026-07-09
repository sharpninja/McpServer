using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// TEST-MCP-HELP-005: Agent Help HTTP endpoint integration tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AgentHelpEndpointTests : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AgentHelpEndpointTests()
    {
        _factory = new CustomWebApplicationFactory(ConfigureAgentHelpStrategy);
        _client = _factory.CreateAuthenticatedClient();
    }

    private static void ConfigureAgentHelpStrategy(IServiceCollection services)
    {
        services.RemoveAll<IAgentExecutionStrategyResolver>();
        services.AddSingleton<IAgentExecutionStrategyResolver>(
            new FakeAgentExecutionStrategyResolver(
                new FakeAgentExecutionStrategy(
                    AgentExecutionStrategyNames.GrokCli,
                    new AgentCliResult
                    {
                        State = AgentCliResultState.Success,
                        Body = "FINAL ANSWER:\nAgent Help endpoint response.",
                    })));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task CreateSession_ReturnsSessionId()
    {
        using var response = await _client.PostAsJsonAsync(
            "/mcpserver/agent-help/session",
            new AgentHelpSessionCreateRequest { WorkspacePath = _factory.WorkspacePath },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AgentHelpSessionCreateResponse>(
            s_jsonOptions,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.SessionId));
        Assert.StartsWith("help-", body.SessionId, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitTurnStreaming_BenignMessage_EmitsChunkAndDone()
    {
        var sessionId = await CreateSessionAsync().ConfigureAwait(true);
        var benign = ReadFixture("bypass/normal-help-request.txt");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/mcpserver/agent-help/session/{Uri.EscapeDataString(sessionId)}/turn/stream");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = JsonContent.Create(new AgentHelpTurnRequest { UserMessage = benign });

        using var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/event-stream", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);

        var events = await ReadSseEventsAsync(response, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains(events, e => string.Equals(GetEventType(e), "chunk", StringComparison.Ordinal));
        Assert.Contains(events, e => string.Equals(GetEventType(e), "done", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SubmitTurnStreaming_InjectionMessage_TerminatesSessionAndWritesGuardrailViolationEvidence()
    {
        var sessionId = await CreateSessionAsync().ConfigureAwait(true);
        var injection = ReadFixture("injection/ignore-previous-instructions.txt");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/mcpserver/agent-help/session/{Uri.EscapeDataString(sessionId)}/turn/stream");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = JsonContent.Create(new AgentHelpTurnRequest { UserMessage = injection });

        using var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await ReadSseEventsAsync(response, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains(events, e => string.Equals(GetEventType(e), "session_terminated", StringComparison.Ordinal));

        using var statusResponse = await _client.GetAsync(
            $"/mcpserver/agent-help/session/{Uri.EscapeDataString(sessionId)}",
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var status = await statusResponse.Content.ReadFromJsonAsync<AgentHelpSessionStatusDto>(
            s_jsonOptions,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(status);
        Assert.True(status!.Terminated);
        Assert.Equal("terminated_guardrail", status.Status);

        var dataRoot = Path.Combine(_factory.WorkspacePath, ".mcpServer");
        var transcriptPath = Path.Combine(dataRoot, "agent-help", "transcripts", $"{sessionId}.jsonl");
        Assert.True(File.Exists(transcriptPath));
        var transcriptJson = await File.ReadAllTextAsync(transcriptPath, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains("\"category\":\"guardrail_violation\"", transcriptJson, StringComparison.Ordinal);

        var incidentDir = Path.Combine(dataRoot, "agent-help", "incidents");
        Assert.True(Directory.Exists(incidentDir));
        Assert.NotEmpty(Directory.GetFiles(incidentDir, "*.json"));
    }

    [Fact]
    public async Task GetTranscript_ReturnsPersistedEntries()
    {
        var sessionId = await CreateSessionAsync().ConfigureAwait(true);
        var benign = ReadFixture("bypass/normal-help-request.txt");

        using var turnResponse = await _client.PostAsJsonAsync(
            $"/mcpserver/agent-help/session/{Uri.EscapeDataString(sessionId)}/turn",
            new AgentHelpTurnRequest { UserMessage = benign },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, turnResponse.StatusCode);
        var turn = await turnResponse.Content.ReadFromJsonAsync<AgentHelpTurnResponse>(
            s_jsonOptions,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(turn);
        Assert.Equal("completed", turn!.Status);
        Assert.Equal("Agent Help endpoint response.", turn.AssistantDisplayText);
        Assert.Null(turn.Error);

        using var statusResponse = await _client.GetAsync(
            $"/mcpserver/agent-help/session/{Uri.EscapeDataString(sessionId)}",
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var status = await statusResponse.Content.ReadFromJsonAsync<AgentHelpSessionStatusDto>(
            s_jsonOptions,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(status);
        Assert.Equal("idle", status!.Status);
        Assert.False(status.IsTurnActive);
        Assert.Null(status.LastError);

        using var transcriptResponse = await _client.GetAsync(
            $"/mcpserver/agent-help/session/{Uri.EscapeDataString(sessionId)}/transcript",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, transcriptResponse.StatusCode);
        var transcript = await transcriptResponse.Content.ReadFromJsonAsync<AgentHelpTranscriptResponse>(
            s_jsonOptions,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(transcript);
        Assert.Equal(sessionId, transcript!.SessionId);
        Assert.Contains(transcript.Items, item => string.Equals(item.Role, "user", StringComparison.OrdinalIgnoreCase));
        var assistant = Assert.Single(
            transcript.Items,
            item => string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Agent Help endpoint response.", assistant.Text);
        Assert.DoesNotContain("FINAL ANSWER:", assistant.Text, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> CreateSessionAsync()
    {
        using var response = await _client.PostAsJsonAsync(
            "/mcpserver/agent-help/session",
            new AgentHelpSessionCreateRequest
            {
                WorkspacePath = _factory.WorkspacePath,
                Topic = "integration-test",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AgentHelpSessionCreateResponse>(
            s_jsonOptions,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        return body!.SessionId;
    }

    private static string ReadFixture(string relativePath)
    {
        var solutionRoot = CustomWebApplicationFactory.ResolveSolutionRoot();
        var path = Path.Combine(
            solutionRoot,
            "tests",
            "McpServer.Support.Mcp.Tests",
            "Fixtures",
            "AgentHelp",
            relativePath);
        return File.ReadAllText(path).Trim();
    }

    private static async Task<List<JsonElement>> ReadSseEventsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        var events = new List<JsonElement>();
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break;

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var json = line["data: ".Length..];
            using var doc = JsonDocument.Parse(json);
            events.Add(doc.RootElement.Clone());
        }

        return events;
    }

    private static string? GetEventType(JsonElement element)
        => element.TryGetProperty("type", out var type) ? type.GetString() : null;

    private sealed class FakeAgentExecutionStrategyResolver(IAgentExecutionStrategy strategy)
        : IAgentExecutionStrategyResolver
    {
        public IAgentExecutionStrategy Resolve(string? strategyName) => strategy;
    }

    private sealed class FakeAgentExecutionStrategy(string name, AgentCliResult result) : IAgentExecutionStrategy
    {
        public string Name { get; } = name;

        public ValueTask<IAgentExecutionSession> CreateSessionAsync(
            AgentExecutionSessionRequest request,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IAgentExecutionSession>(new FakeAgentExecutionSession(result));
    }

    private sealed class FakeAgentExecutionSession(AgentCliResult result) : IAgentExecutionSession
    {
        public bool IsAlive => false;

        public int? ProcessId => null;

        public Task<AgentCliResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public async IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(result.Body))
                yield return result.Body;

            await Task.CompletedTask.ConfigureAwait(false);
        }

        public Task<AgentCliResult> SendAsync(string prompt, CancellationToken cancellationToken = default)
            => Task.FromResult(result);

        public async IAsyncEnumerable<string> SendStreamingAsync(
            string prompt,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(result.Body))
                yield return result.Body;

            await Task.CompletedTask.ConfigureAwait(false);
        }

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EndAsync(TimeSpan timeout) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
