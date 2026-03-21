using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.McpAgent.Hosting;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.McpAgent.SessionLog;
using McpServer.Client.Models;
using McpClientServiceCollectionExtensions = McpServer.Client.ServiceCollectionExtensions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-089: Verifies that a .NET host can register the Agent Framework library through
/// <see cref="ServiceCollectionExtensions.AddMcpServerMcpAgent(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{McpAgentOptions})"/>,
/// obtain the hosted-agent and adapter surface from dependency injection, and execute meaningful
/// built-in workflow operations against stable in-memory MCP contracts without custom host glue.
/// </summary>
public sealed class HostedAgentWorkflowIntegrationTests
{
    /// <summary>
    /// TEST-MCP-089: Verifies that a DI-registered hosted agent can expose its ChatClientAgent
    /// metadata, attach the built-in MCP tools to run options, and execute a representative
    /// session-log-plus-TODO workflow through those tools.
    /// The test uses only service registration, factory resolution, run-option tool discovery, and
    /// tool invocation while an in-memory HTTP handler stands in for the MCP server contracts.
    /// </summary>
    [Fact]
    public async Task AddMcpServerMcpAgent_FactoryCreatedHostedAgent_ExecutesBuiltInWorkflowTools()
    {
        using var serviceProvider = CreateServiceProvider(out var handler);
        var hostedAgentFactory = serviceProvider.GetRequiredService<IMcpHostedAgentFactory>();
        var hostedAgent = hostedAgentFactory.CreateHostedAgent();
        using var chatClient = new StubChatClient();
        var chatClientAgent = hostedAgent.CreateChatClientAgent(chatClient);
        var tools = GetAttachedTools(hostedAgent);

        var bootstrapResult = await tools["mcp_session_bootstrap"].InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new SessionLogBootstrapRequest
                {
                    Model = "gpt-5.4",
                    Title = "Hosted workflow acceptance",
                    Workspace = new WorkspaceInfoDto
                    {
                        Project = "McpServer",
                        Branch = "feature/agentframework-tests",
                    },
                },
            },
            CancellationToken.None);

        var beginTurnResult = await tools["mcp_session_turn_begin"].InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new SessionLogTurnCreateRequest
                {
                    QueryTitle = "Protect hosted workflow layer",
                    QueryText = "Exercise the DI-registered MCP workflow tools end-to-end.",
                    Tags = ["agentframework-tests"],
                    ContextList = [@"tests\McpServer.McpAgent.Tests\HostedAgentWorkflowIntegrationTests.cs"],
                },
            },
            CancellationToken.None);

        var queryResult = await tools["mcp_todo_query"].InvokeAsync(
            new AIFunctionArguments
            {
                ["keyword"] = "coverage",
                ["priority"] = "high",
                ["section"] = "agentframework",
                ["id"] = "MCP-AGENTFRAMEWORK-001",
                ["done"] = false,
            },
            CancellationToken.None);

        var updateResult = await tools["mcp_todo_update"].InvokeAsync(
            new AIFunctionArguments
            {
                ["id"] = "MCP-AGENTFRAMEWORK-001",
                ["request"] = new TodoUpdateRequest
                {
                    Title = "Protect hosted workflow layer",
                    Note = "Acceptance coverage updated through the registered hosted-agent tools.",
                    Done = false,
                },
            },
            CancellationToken.None);

        var statusResult = await tools["mcp_todo_status"].InvokeAsync(
            new AIFunctionArguments
            {
                ["id"] = "MCP-AGENTFRAMEWORK-001",
            },
            CancellationToken.None);
        var repoListResult = await tools["mcp_repo_list"].InvokeAsync(
            new AIFunctionArguments
            {
                ["path"] = "src",
            },
            CancellationToken.None);
        var desktopLaunchResult = await tools["mcp_desktop_launch"].InvokeAsync(
            new AIFunctionArguments
            {
                ["executablePath"] = @"C:\Windows\System32\cmd.exe",
                ["arguments"] = "/c exit 0",
                ["createNoWindow"] = true,
                ["waitForExit"] = true,
            },
            CancellationToken.None);
        var powerShellCreateResult = await tools["mcp_powershell_session_create"].InvokeAsync(
            new AIFunctionArguments(),
            CancellationToken.None);
        var powerShellSession = DeserializeJsonResult<PowerShellSessionCreateResult>(powerShellCreateResult);
        var powerShellFirstCommandResult = await tools["mcp_powershell_session_command"].InvokeAsync(
            new AIFunctionArguments
            {
                ["sessionId"] = powerShellSession.SessionId!,
                ["command"] = "$global:WorkflowValue = 7; $global:WorkflowValue",
            },
            CancellationToken.None);
        var powerShellSecondCommandResult = await tools["mcp_powershell_session_command"].InvokeAsync(
            new AIFunctionArguments
            {
                ["sessionId"] = powerShellSession.SessionId!,
                ["command"] = "$global:WorkflowValue",
            },
            CancellationToken.None);
        var powerShellCloseResult = await tools["mcp_powershell_session_close"].InvokeAsync(
            new AIFunctionArguments
            {
                ["sessionId"] = powerShellSession.SessionId!,
            },
            CancellationToken.None);

        var turnRequestId = GetJsonProperty(Assert.IsType<JsonElement>(beginTurnResult), "requestId", "RequestId").GetString();
        Assert.NotNull(turnRequestId);

        var completeTurnResult = await tools["mcp_session_turn_complete"].InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new SessionLogTurnCompleteRequest
                {
                    RequestId = turnRequestId!,
                    Response = "Protected the hosted workflow layer through DI registration and adapter invocation.",
                    Interpretation = "Use the registered hosted-agent tool surface rather than custom host HTTP glue.",
                    FilesModified = [@"tests\McpServer.McpAgent.Tests\HostedAgentWorkflowIntegrationTests.cs"],
                    DesignDecisions = ["Exercise the same DI-registered workflows the sample host would consume."],
                    RequirementsDiscovered = ["TEST-MCP-089"],
                },
            },
            CancellationToken.None);

        var bootstrapJson = Assert.IsType<JsonElement>(bootstrapResult);
        var completedTurnJson = Assert.IsType<JsonElement>(completeTurnResult);
        var todoQuery = DeserializeJsonResult<TodoQueryResult>(queryResult);
        var todoMutation = DeserializeJsonResult<TodoMutationResult>(updateResult);
        var statusText = ReadStringResult(statusResult);
        var repoListing = DeserializeJsonResult<RepoListResult>(repoListResult);
        var desktopLaunch = DeserializeJsonResult<DesktopLaunchResult>(desktopLaunchResult);
        var powerShellFirstCommand = DeserializeJsonResult<PowerShellSessionCommandResult>(powerShellFirstCommandResult);
        var powerShellSecondCommand = DeserializeJsonResult<PowerShellSessionCommandResult>(powerShellSecondCommandResult);
        var powerShellClosed = DeserializeJsonResult<PowerShellSessionCloseResult>(powerShellCloseResult);
        var workflowContext = hostedAgent.SessionLog.Context!;
        Assert.NotNull(workflowContext);
        var completedTurn = Assert.Single(workflowContext.Turns);

        Assert.Equal(hostedAgent.Name, chatClientAgent.Name);
        Assert.Equal(hostedAgent.AgentOptions.Description, chatClientAgent.Description);
        Assert.Equal("Codex-20260309T150105Z-gpt-5-4", GetJsonProperty(bootstrapJson, "sessionId", "SessionId").GetString());
        Assert.Equal("Codex", GetJsonProperty(bootstrapJson, "sourceType", "SourceType").GetString());
        Assert.Equal("req-20260309T150105Z-protect-hosted-workflow-layer", turnRequestId);
        Assert.Equal("completed", GetJsonProperty(completedTurnJson, "status", "Status").GetString());
        Assert.Equal("Codex-20260309T150105Z-gpt-5-4", workflowContext.SessionId);
        Assert.Equal("Codex", workflowContext.SourceType);
        Assert.Equal("Hosted workflow acceptance", workflowContext.Title);
        Assert.Equal("gpt-5.4", workflowContext.Model);
        Assert.Equal("completed", completedTurn.Status);
        Assert.Equal("Protected the hosted workflow layer through DI registration and adapter invocation.", completedTurn.Response);
        Assert.Equal(
            "Use the registered hosted-agent tool surface rather than custom host HTTP glue.",
            completedTurn.Interpretation);
        Assert.Equal(["agentframework-tests"], completedTurn.Tags);
        Assert.Equal([@"tests\McpServer.McpAgent.Tests\HostedAgentWorkflowIntegrationTests.cs"], completedTurn.ContextList);
        Assert.Equal(["TEST-MCP-089"], completedTurn.RequirementsDiscovered);
        Assert.Equal(["Exercise the same DI-registered workflows the sample host would consume."], completedTurn.DesignDecisions);
        Assert.Equal(1, todoQuery.TotalCount);
        Assert.Single(todoQuery.Items);
        Assert.Equal("MCP-AGENTFRAMEWORK-001", todoQuery.Items[0].Id);
        Assert.True(todoMutation.Success);
        Assert.NotNull(todoMutation.Item);
        Assert.Equal("Protect hosted workflow layer", todoMutation.Item!.Title);
        Assert.Equal(
            "Acceptance coverage updated through the registered hosted-agent tools.",
            todoMutation.Item.Note);
        Assert.Equal("Coverage protected\nAwaiting review", statusText);
        Assert.Equal("src", repoListing.Path);
        Assert.True(desktopLaunch.Success);
        Assert.Equal(4242, desktopLaunch.ProcessId);
        Assert.Equal(0, desktopLaunch.ExitCode);
        Assert.True(powerShellSession.Success);
        Assert.Equal(@"E:\github\McpServer", powerShellSession.CurrentLocation);
        Assert.True(powerShellFirstCommand.Success);
        Assert.Equal("7", powerShellFirstCommand.Output);
        Assert.Equal(powerShellSession.SessionId, powerShellFirstCommand.SessionId);
        Assert.True(powerShellSecondCommand.Success);
        Assert.Equal("7", powerShellSecondCommand.Output);
        Assert.True(powerShellClosed.Success);
        Assert.Equal(powerShellSession.SessionId, powerShellClosed.SessionId);
        Assert.Collection(
            repoListing.Entries,
            entry =>
            {
                Assert.Equal("McpServer.McpAgent", entry.Name);
                Assert.True(entry.IsDirectory);
            },
            entry =>
            {
                Assert.Equal("McpServer.Client", entry.Name);
                Assert.True(entry.IsDirectory);
            });

        Assert.Equal(8, handler.Requests.Count);
        Assert.All(
            handler.Requests,
            static request =>
            {
                Assert.Equal("test-key", request.ApiKey);
                Assert.Equal(@"E:\github\McpServer", request.WorkspacePath);
            });

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[0].RequestUri.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[1].RequestUri.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
        Assert.Equal(
            "http://localhost:7147/mcpserver/todo?keyword=coverage&priority=high&section=agentframework&id=MCP-AGENTFRAMEWORK-001&done=false",
            handler.Requests[2].RequestUri.ToString());
        Assert.Equal(HttpMethod.Put, handler.Requests[3].Method);
        Assert.Equal("/mcpserver/todo/MCP-AGENTFRAMEWORK-001", handler.Requests[3].RequestUri.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.Requests[4].Method);
        Assert.Equal("/mcpserver/todo/MCP-AGENTFRAMEWORK-001/prompt/status", handler.Requests[4].RequestUri.AbsolutePath);
        Assert.Contains("text/event-stream", handler.Requests[4].AcceptMediaTypes);
        Assert.Equal(HttpMethod.Get, handler.Requests[5].Method);
        Assert.Equal("http://localhost:7147/mcpserver/repo/list?path=src", handler.Requests[5].RequestUri.ToString());
        Assert.Equal(HttpMethod.Post, handler.Requests[6].Method);
        Assert.Equal("/mcpserver/desktop/launch", handler.Requests[6].RequestUri.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.Requests[7].Method);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[7].RequestUri.AbsolutePath);

        using var updateBody = JsonDocument.Parse(handler.Requests[3].Body!);
        Assert.Equal("Protect hosted workflow layer", updateBody.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "Acceptance coverage updated through the registered hosted-agent tools.",
            updateBody.RootElement.GetProperty("note").GetString());
        Assert.False(updateBody.RootElement.GetProperty("done").GetBoolean());

        using var desktopLaunchBody = JsonDocument.Parse(handler.Requests[6].Body!);
        Assert.Equal(@"C:\Windows\System32\cmd.exe", desktopLaunchBody.RootElement.GetProperty("executablePath").GetString());
        Assert.True(desktopLaunchBody.RootElement.GetProperty("createNoWindow").GetBoolean());
        Assert.True(desktopLaunchBody.RootElement.GetProperty("waitForExit").GetBoolean());

        using var finalSessionBody = JsonDocument.Parse(handler.Requests[7].Body!);
        var finalTurn = finalSessionBody.RootElement.GetProperty("turns")[0];
        Assert.Equal("completed", finalTurn.GetProperty("status").GetString());
        Assert.Equal(
            "Protected the hosted workflow layer through DI registration and adapter invocation.",
            finalTurn.GetProperty("response").GetString());
        Assert.Equal("TEST-MCP-089", finalTurn.GetProperty("requirementsDiscovered")[0].GetString());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that <see cref="IMcpHostedAgentFactory"/> returns isolated hosted
    /// agents whose session-log workflow state does not bleed across conversations.
    /// The test bootstraps two factory-created agents through the same DI registration and asserts
    /// that each tool invocation produces an independent in-memory session context and submit payload.
    /// </summary>
    [Fact]
    public async Task AddMcpServerMcpAgent_HostedAgentFactory_CreatesIsolatedWorkflowContexts()
    {
        using var serviceProvider = CreateServiceProvider(out var handler);
        var factory = serviceProvider.GetRequiredService<IMcpHostedAgentFactory>();
        var firstAgent = factory.CreateHostedAgent();
        var secondAgent = factory.CreateHostedAgent();
        var firstBootstrapTool = GetAttachedTools(firstAgent)["mcp_session_bootstrap"];
        var secondBootstrapTool = GetAttachedTools(secondAgent)["mcp_session_bootstrap"];

        await firstBootstrapTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new SessionLogBootstrapRequest
                {
                    SessionIdSuffix = "first-agent",
                    Title = "First hosted conversation",
                },
            },
            CancellationToken.None);

        await secondBootstrapTool.InvokeAsync(
            new AIFunctionArguments
            {
                ["request"] = new SessionLogBootstrapRequest
                {
                    SessionIdSuffix = "second-agent",
                    Title = "Second hosted conversation",
                },
            },
            CancellationToken.None);

        var firstContext = firstAgent.SessionLog.Context!;
        var secondContext = secondAgent.SessionLog.Context!;
        Assert.NotNull(firstContext);
        Assert.NotNull(secondContext);

        Assert.NotSame(firstAgent, secondAgent);
        Assert.NotSame(firstContext, secondContext);
        Assert.Equal("Codex-20260309T150105Z-first-agent", firstContext.SessionId);
        Assert.Equal("Codex-20260309T150105Z-second-agent", secondContext.SessionId);
        Assert.Equal("First hosted conversation", firstContext.Title);
        Assert.Equal("Second hosted conversation", secondContext.Title);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[0].RequestUri.AbsolutePath);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[1].RequestUri.AbsolutePath);

        using var firstBody = JsonDocument.Parse(handler.Requests[0].Body!);
        using var secondBody = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.Equal("Codex-20260309T150105Z-first-agent", firstBody.RootElement.GetProperty("sessionId").GetString());
        Assert.Equal("Codex-20260309T150105Z-second-agent", secondBody.RootElement.GetProperty("sessionId").GetString());
    }

    private static ServiceProvider CreateServiceProvider(out RecordingHostedWorkflowHttpMessageHandler handler)
    {
        handler = new RecordingHostedWorkflowHttpMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 03, 09, 15, 01, 05, TimeSpan.Zero)));
        services.AddSingleton(handler);
        services.AddMcpServerMcpAgent(options =>
        {
            options.ApiKey = "test-key";
            options.BaseUrl = new Uri("http://localhost:7147");
            options.SourceType = "Codex";
            options.WorkspacePath = @"E:\github\McpServer";
        });
        services.AddHttpClient(McpClientServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider =>
                serviceProvider.GetRequiredService<RecordingHostedWorkflowHttpMessageHandler>());

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private static Dictionary<string, AIFunction> GetAttachedTools(IMcpHostedAgent hostedAgent)
    {
        var runOptions = hostedAgent.CreateRunOptions();
        var tools = runOptions.ChatOptions?.Tools!;
        Assert.NotNull(tools);
        return tools.OfType<AIFunction>().ToDictionary(static tool => tool.Name, StringComparer.Ordinal);
    }

    private static T DeserializeJsonResult<T>(object? result)
    {
        var json = Assert.IsType<JsonElement>(result);
        var value = JsonSerializer.Deserialize<T>(json.GetRawText());
        Assert.NotNull(value);
        return value;
    }

    private static JsonElement GetJsonProperty(JsonElement element, string camelCaseName, string pascalCaseName) =>
        element.TryGetProperty(camelCaseName, out var camelCaseProperty)
            ? camelCaseProperty
            : element.GetProperty(pascalCaseName);

    private static string ReadStringResult(object? result) => result switch
    {
        JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString() ?? string.Empty,
        string text => text,
        _ => throw new InvalidOperationException(
            $"Expected a string or JSON string result but received '{result?.GetType().FullName ?? "<null>"}'."),
    };

    /// <summary>
    /// Captures outbound session-log and TODO requests emitted by the DI-registered hosted agent and
    /// returns deterministic JSON or SSE payloads for the exercised MCP contracts.
    /// </summary>
    private sealed class RecordingHostedWorkflowHttpMessageHandler : HttpMessageHandler
    {
        private long _submitCount;

        /// <summary>
        /// Gets the ordered request log captured during the integration test.
        /// </summary>
        public List<RecordedRequest> Requests { get; } = [];

        /// <summary>
        /// Captures an outbound request and returns the deterministic response for the targeted MCP endpoint.
        /// </summary>
        /// <param name="request">The outbound request emitted by the hosted workflow surface.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the hosted workflow surface.</param>
        /// <returns>A deterministic response matching the requested MCP endpoint.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Requests.Add(
                new RecordedRequest(
                    request.Method,
                    request.RequestUri!,
                    body,
                    request.Headers.TryGetValues("X-Api-Key", out var apiKeys) ? apiKeys.Single() : null,
                    request.Headers.TryGetValues("X-Workspace-Path", out var workspacePaths) ? workspacePaths.Single() : null,
                    request.Headers.Accept.Select(static value => value.MediaType ?? string.Empty).ToArray()));

            var segments = request.RequestUri!.Segments
                .Select(static segment => Uri.UnescapeDataString(segment.Trim('/')))
                .Where(static segment => !string.IsNullOrEmpty(segment))
                .ToArray();

            if (request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath == "/mcpserver/sessionlog")
                return CreateSessionLogResponse(body!);

            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath == "/mcpserver/todo")
                return CreateTodoQueryResponse();

            if (segments is ["mcpserver", "todo", var todoId] && request.Method == HttpMethod.Put)
                return CreateTodoMutationResponse(todoId, body!);

            if (segments is ["mcpserver", "todo", _, "prompt", "status"] && request.Method == HttpMethod.Get)
                return CreateTodoStatusResponse();

            if (segments is ["mcpserver", "repo", "list"] && request.Method == HttpMethod.Get)
                return CreateRepoListResponse(request.RequestUri);

            if (segments is ["mcpserver", "desktop", "launch"] && request.Method == HttpMethod.Post)
                return CreateDesktopLaunchResponse();

            throw new InvalidOperationException(
                $"Unexpected MCP request '{request.Method} {request.RequestUri.AbsolutePath}'.");
        }

        private HttpResponseMessage CreateSessionLogResponse(string body)
        {
            _submitCount++;
            using var document = JsonDocument.Parse(body);
            var sourceType = document.RootElement.GetProperty("sourceType").GetString();
            var sessionId = document.RootElement.GetProperty("sessionId").GetString();

            return CreateJsonResponse(
                HttpStatusCode.Created,
                JsonSerializer.Serialize(
                    new
                    {
                        id = _submitCount,
                        sourceType,
                        sessionId,
                    }));
        }

        private static HttpResponseMessage CreateTodoQueryResponse() => CreateJsonResponse(
            HttpStatusCode.OK,
            """
            {"items":[{"id":"MCP-AGENTFRAMEWORK-001","title":"Protect hosted workflow layer","section":"agentframework","priority":"high","done":false}],"totalCount":1}
            """);

        private static HttpResponseMessage CreateTodoMutationResponse(string todoId, string body)
        {
            using var document = JsonDocument.Parse(body);
            var title = document.RootElement.GetProperty("title").GetString();
            var note = document.RootElement.GetProperty("note").GetString();
            var done = document.RootElement.GetProperty("done").GetBoolean();

            return CreateJsonResponse(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(
                    new
                    {
                        success = true,
                        item = new
                        {
                            id = todoId,
                            title,
                            section = "agentframework",
                            priority = "high",
                            done,
                            note,
                        },
                    }));
        }

        private static HttpResponseMessage CreateTodoStatusResponse() => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "data: Coverage protected\n\ndata: Awaiting review\n\nevent: done\ndata: \n\n",
                Encoding.UTF8,
                "text/event-stream"),
        };

        private static HttpResponseMessage CreateRepoListResponse(Uri requestUri)
        {
            var path = GetQueryParameter(requestUri, "path") ?? string.Empty;
            return CreateJsonResponse(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(
                    new RepoListResult
                    {
                        Path = path,
                        Entries =
                        [
                            new RepoListEntry { Name = "McpServer.McpAgent", IsDirectory = true },
                            new RepoListEntry { Name = "McpServer.Client", IsDirectory = true },
                        ],
                    }));
        }

        private static HttpResponseMessage CreateDesktopLaunchResponse() => CreateJsonResponse(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(
                new DesktopLaunchResult
                {
                    Success = true,
                    ProcessId = 4242,
                    ExitCode = 0
                }));

        private static string? GetQueryParameter(Uri requestUri, string name)
        {
            foreach (var segment in requestUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = segment.Split('=', 2);
                if (!string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.Ordinal))
                {
                    continue;
                }

                return parts.Length == 2
                    ? Uri.UnescapeDataString(parts[1])
                    : string.Empty;
            }

            return null;
        }

        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string body) => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>
    /// Captures a single outbound request emitted by the integration-test hosted workflow surface.
    /// </summary>
    /// <param name="Method">The emitted HTTP method.</param>
    /// <param name="RequestUri">The emitted request URI.</param>
    /// <param name="Body">The serialized request body, when present.</param>
    /// <param name="ApiKey">The emitted API key header, when present.</param>
    /// <param name="WorkspacePath">The emitted workspace-path header, when present.</param>
    /// <param name="AcceptMediaTypes">The emitted accept-header media types.</param>
    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri RequestUri,
        string? Body,
        string? ApiKey,
        string? WorkspacePath,
        IReadOnlyList<string> AcceptMediaTypes);

    /// <summary>
    /// Provides a deterministic clock for hosted workflow integration tests.
    /// </summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        /// <summary>
        /// Initializes the deterministic test clock with a fixed UTC timestamp.
        /// </summary>
        /// <param name="utcNow">The fixed UTC timestamp returned by <see cref="GetUtcNow"/>.</param>
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        /// <summary>
        /// Returns the fixed UTC timestamp configured for the test.
        /// </summary>
        /// <returns>The fixed UTC timestamp.</returns>
        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    /// <summary>
    /// Minimal chat client used to prove that a host can obtain a <see cref="ChatClientAgent"/>
    /// from the DI-registered hosted-agent surface without a live provider.
    /// </summary>
    private sealed class StubChatClient : IChatClient
    {
        /// <summary>
        /// Releases no resources because the stub chat client is stateless.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// This method is not expected to be called during the hosted workflow integration tests.
        /// </summary>
        /// <param name="messages">Ignored.</param>
        /// <param name="options">Ignored.</param>
        /// <param name="cancellationToken">Ignored.</param>
        /// <returns>Never returns successfully.</returns>
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The stub chat client is only used for ChatClientAgent construction in tests.");

        /// <summary>
        /// This method is not expected to be called during the hosted workflow integration tests.
        /// </summary>
        /// <param name="messages">Ignored.</param>
        /// <param name="options">Ignored.</param>
        /// <param name="cancellationToken">Ignored.</param>
        /// <returns>Never returns successfully.</returns>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The stub chat client is only used for ChatClientAgent construction in tests.");

        /// <summary>
        /// Returns no additional services for the hosted workflow integration tests.
        /// </summary>
        /// <param name="serviceType">Requested service type.</param>
        /// <param name="serviceKey">Requested service key.</param>
        /// <returns><see langword="null"/> for all service requests.</returns>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
