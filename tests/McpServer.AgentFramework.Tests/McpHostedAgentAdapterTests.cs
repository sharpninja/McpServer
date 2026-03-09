using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.AgentFramework.AgentFramework;
using McpServer.AgentFramework.SessionLog;
using McpServer.AgentFramework.Todo;
using McpServer.Client;
using McpServer.Client.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.AgentFramework.Tests;

/// <summary>
/// TEST-MCP-089: Verifies the Microsoft Agent Framework adapter surface exposed by
/// <see cref="McpHostedAgent"/> and <see cref="McpHostedAgentRegistration"/>.
/// </summary>
public sealed class McpHostedAgentAdapterTests
{
    /// <summary>
    /// TEST-MCP-089: Verifies that the adapter registration exposes a stable MCP capability surface,
    /// preserves caller-supplied run options, and wraps chat clients with function invocation support.
    /// The test uses a real hosted-agent wrapper with in-memory MCP transport fixtures so the
    /// assertions stay stable without requiring a live LLM provider.
    /// </summary>
    [Fact]
    public void Registration_CreateRunOptions_AttachesStableToolsAndFunctionInvocation()
    {
        var (hostedAgent, _) = CreateHostedAgent();
        var registration = hostedAgent.Registration;
        var expectedToolNames = new[]
        {
            "mcp_session_bootstrap",
            "mcp_session_update",
            "mcp_session_turn_begin",
            "mcp_session_turn_update",
            "mcp_session_turn_complete",
            "mcp_todo_query",
            "mcp_todo_get",
            "mcp_todo_update",
            "mcp_todo_plan",
            "mcp_todo_status",
            "mcp_todo_implementation",
        };

        var existingTool = AIFunctionFactory.Create(
            (Func<string>)(() => "existing"),
            new AIFunctionFactoryOptions
            {
                Description = "Existing host tool.",
                Name = "existing_host_tool",
            });

        var baseFactoryCalled = false;
        var runOptions = registration.CreateRunOptions(
            new ChatClientAgentRunOptions
            {
                ChatClientFactory = client =>
                {
                    baseFactoryCalled = true;
                    return client;
                },
                ChatOptions = new ChatOptions
                {
                    Tools = [existingTool],
                },
            });

        var wrappedClient = runOptions.ChatClientFactory!(new StubChatClient());
        var chatClientAgent = hostedAgent.CreateChatClientAgent(new StubChatClient());

        Assert.Equal(expectedToolNames, registration.Functions.Select(static function => function.Name));
        Assert.True(baseFactoryCalled);
        Assert.IsType<FunctionInvokingChatClient>(wrappedClient);
        Assert.NotNull(runOptions.ChatOptions);
        Assert.NotNull(runOptions.ChatOptions.ToolMode);
        Assert.False(runOptions.ChatOptions.AllowMultipleToolCalls);
        Assert.NotNull(runOptions.ChatOptions.Tools);
        var attachedTools = runOptions.ChatOptions.Tools;
        Assert.Contains(attachedTools, static tool => tool.Name == "existing_host_tool");
        Assert.Equal(
            ["existing_host_tool", .. expectedToolNames],
            attachedTools.Select(static tool => tool.Name).ToArray());
        Assert.Equal(hostedAgent.Name, chatClientAgent.Name);
        Assert.Equal(hostedAgent.AgentOptions.Description, chatClientAgent.Description);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the session bootstrap adapter function delegates to the existing
    /// session-log workflow and returns the typed workflow context produced by that workflow.
    /// The test uses the same deterministic clock and recording transport used by the workflow tests
    /// so the canonical session identifier and HTTP payload shape stay stable.
    /// </summary>
    [Fact]
    public async Task Registration_Functions_BootstrapSessionThroughExistingWorkflow()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var bootstrapFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_session_bootstrap");

        var result = await bootstrapFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new SessionLogBootstrapRequest
                {
                    Model = "gpt-5.4",
                    Title = "Implement MCP-AGENTFRAMEWORK-001",
                },
            },
            CancellationToken.None);

        var context = Assert.IsType<JsonElement>(result);

        Assert.Equal("Codex-20260309T150105Z-gpt-5-4", GetJsonProperty(context, "sessionId", "SessionId").GetString());
        Assert.Equal("Codex", GetJsonProperty(context, "sourceType", "SourceType").GetString());
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[0].RequestUri.AbsolutePath);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the TODO adapter functions reuse the built-in TODO workflow for
    /// both complex mutation requests and buffered plan retrieval.
    /// The test inspects the emitted HTTP body for the mutation path and asserts the buffered plan
    /// output so the adapter proves it delegates to the existing transport-backed workflow methods.
    /// </summary>
    [Fact]
    public async Task Registration_Functions_UpdateTodoAndBufferPlanThroughExistingWorkflow()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var updateFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_todo_update");
        var planFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_todo_plan");

        var updateResult = await updateFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["id"] = "MCP-AGENT-001",
                ["request"] = new TodoUpdateRequest
                {
                    Done = true,
                    Note = "Updated via MCP tool adapter.",
                    Title = "Updated TODO title",
                },
            },
            CancellationToken.None);

        var planResult = await planFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["id"] = "MCP-AGENT-001",
            },
            CancellationToken.None);

        var mutationJson = Assert.IsType<JsonElement>(updateResult);
        var mutation = JsonSerializer.Deserialize<TodoMutationResult>(mutationJson.GetRawText());
        var planText = planResult switch
        {
            JsonElement json => json.GetString(),
            string text => text,
            null => null,
            _ => throw new InvalidOperationException($"Unexpected plan result type '{planResult.GetType()}'."),
        };

        Assert.NotNull(mutation);
        Assert.True(mutation.Success);
        Assert.NotNull(mutation.Item);
        Assert.Equal("Updated TODO title", mutation.Item!.Title);
        Assert.Equal("Step 1\nStep 2", planText);
        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/todo/MCP-AGENT-001", handler.Requests[0].RequestUri.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("/mcpserver/todo/MCP-AGENT-001/prompt/plan", handler.Requests[1].RequestUri.AbsolutePath);

        using var requestBody = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal("Updated TODO title", requestBody.RootElement.GetProperty("title").GetString());
        Assert.Equal("Updated via MCP tool adapter.", requestBody.RootElement.GetProperty("note").GetString());
        Assert.True(requestBody.RootElement.GetProperty("done").GetBoolean());
    }

    private static (McpHostedAgent HostedAgent, RecordingMcpHttpMessageHandler Handler) CreateHostedAgent()
    {
        var handler = new RecordingMcpHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var client = new McpServerClient(
            httpClient,
            new McpServerClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = new Uri("http://localhost:7147"),
                WorkspacePath = @"E:\github\McpServer",
            });
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 09, 15, 01, 05, TimeSpan.Zero));
        var options = Options.Create(
            new McpAgentFrameworkOptions
            {
                ApiKey = "test-key",
                BaseUrl = new Uri("http://localhost:7147"),
                SourceType = "Codex",
                WorkspacePath = @"E:\github\McpServer",
            });
        var identifiers = new McpSessionIdentifierFactory(options, timeProvider);
        var sessionLog = new SessionLogWorkflow(client, identifiers, timeProvider);
        var todo = new TodoWorkflow(client);
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        return (
            new McpHostedAgent(
                client,
                identifiers,
                new ChatClientAgentOptions
                {
                    Description = "Hosted MCP agent adapter.",
                    Id = "mcpserver-hosted-agent",
                    Name = "McpServerHostedAgent",
                },
                options,
                sessionLog,
                todo,
                serviceProvider),
            handler);
    }

    private static JsonElement GetJsonProperty(JsonElement element, string camelCaseName, string pascalCaseName) =>
        element.TryGetProperty(camelCaseName, out var camelCaseProperty)
            ? camelCaseProperty
            : element.GetProperty(pascalCaseName);

    /// <summary>
    /// TEST-MCP-089: Captures outbound MCP workflow transport calls for the adapter tests and
    /// returns deterministic JSON or SSE payloads that match the requested MCP endpoint.
    /// </summary>
    private sealed class RecordingMcpHttpMessageHandler : HttpMessageHandler
    {
        private long _submitCount;

        /// <summary>
        /// TEST-MCP-089: Gets the ordered request log captured during a test run.
        /// </summary>
        public List<RecordedRequest> Requests { get; } = [];

        /// <summary>
        /// TEST-MCP-089: Captures the outbound request and returns the deterministic response
        /// associated with the targeted MCP workflow endpoint.
        /// </summary>
        /// <param name="request">The outbound request emitted by the adapter-under-test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the adapter.</param>
        /// <returns>A deterministic response matching the targeted endpoint.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body));

            return request.RequestUri!.AbsolutePath switch
            {
                "/mcpserver/sessionlog" => CreateSessionLogResponse(body!),
                "/mcpserver/todo/MCP-AGENT-001" when request.Method == HttpMethod.Put => CreateTodoMutationResponse(body!),
                "/mcpserver/todo/MCP-AGENT-001/prompt/plan" => CreatePlanResponse(),
                _ => throw new InvalidOperationException($"Unexpected MCP request path '{request.RequestUri.AbsolutePath}'."),
            };
        }

        private HttpResponseMessage CreatePlanResponse() => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "data: Step 1\n\ndata: Step 2\n\nevent: done\ndata: \n\n",
                Encoding.UTF8,
                "text/event-stream"),
        };

        private HttpResponseMessage CreateSessionLogResponse(string body)
        {
            _submitCount++;
            using var document = JsonDocument.Parse(body);
            var sourceType = document.RootElement.GetProperty("sourceType").GetString();
            var sessionId = document.RootElement.GetProperty("sessionId").GetString();

            return CreateJsonResponse(
                HttpStatusCode.Created,
                $$"""
                {"id":{{_submitCount}},"sourceType":"{{sourceType}}","sessionId":"{{sessionId}}"}
                """);
        }

        private static HttpResponseMessage CreateTodoMutationResponse(string body)
        {
            using var document = JsonDocument.Parse(body);
            var title = document.RootElement.GetProperty("title").GetString();
            var note = document.RootElement.GetProperty("note").GetString();
            var done = document.RootElement.GetProperty("done").GetBoolean();

            return CreateJsonResponse(
                HttpStatusCode.OK,
                $"{{\"success\":true,\"item\":{{\"id\":\"MCP-AGENT-001\",\"title\":\"{title}\",\"section\":\"agent\",\"priority\":\"high\",\"done\":{done.ToString().ToLowerInvariant()},\"note\":\"{note}\"}}}}");
        }

        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string body) => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>
    /// TEST-MCP-089: Captures a single recorded HTTP request emitted by the hosted-agent adapter.
    /// </summary>
    /// <param name="Method">The emitted HTTP method.</param>
    /// <param name="RequestUri">The emitted request URI.</param>
    /// <param name="Body">The serialized request body, when present.</param>
    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri RequestUri,
        string? Body);

    /// <summary>
    /// TEST-MCP-089: Provides a deterministic clock for the hosted-agent adapter tests.
    /// </summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        /// <summary>
        /// TEST-MCP-089: Initializes the deterministic test clock with a fixed UTC timestamp.
        /// </summary>
        /// <param name="utcNow">The fixed UTC timestamp returned by <see cref="GetUtcNow"/>.</param>
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        /// <summary>
        /// TEST-MCP-089: Returns the fixed UTC timestamp configured for the test.
        /// </summary>
        /// <returns>The fixed UTC timestamp.</returns>
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    /// <summary>
    /// TEST-MCP-089: Minimal chat client used when constructing ChatClientAgent instances and
    /// function-invocation wrappers during adapter tests.
    /// </summary>
    private sealed class StubChatClient : IChatClient
    {
        /// <summary>
        /// TEST-MCP-089: Releases no resources because the stub chat client is stateless.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// TEST-MCP-089: This method is not expected to be called during the adapter tests.
        /// </summary>
        /// <param name="messages">Ignored.</param>
        /// <param name="options">Ignored.</param>
        /// <param name="cancellationToken">Ignored.</param>
        /// <returns>Never returns successfully.</returns>
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The stub chat client is only used for construction-time adapter tests.");

        /// <summary>
        /// TEST-MCP-089: This method is not expected to be called during the adapter tests.
        /// </summary>
        /// <param name="messages">Ignored.</param>
        /// <param name="options">Ignored.</param>
        /// <param name="cancellationToken">Ignored.</param>
        /// <returns>Never returns successfully.</returns>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The stub chat client is only used for construction-time adapter tests.");

        /// <summary>
        /// TEST-MCP-089: Returns no additional services for the adapter tests.
        /// </summary>
        /// <param name="serviceType">Requested service type.</param>
        /// <param name="serviceKey">Requested service key.</param>
        /// <returns><see langword="null"/> for all service requests.</returns>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
