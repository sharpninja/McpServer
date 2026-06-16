using System.Net;
using System.IO;
using System.Text;
using System.Text.Json;
using McpServer.McpAgent.Hosting;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.McpAgent.SessionLog;
using McpServer.McpAgent.Todo;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-089: Verifies the Microsoft Agent Framework adapter surface exposed by
/// <see cref="McpHostedAgent"/> and <see cref="McpHostedAgentRegistration"/>.
/// </summary>
public sealed class McpHostedAgentAdapterTests
{
    private static readonly string TestWorkspacePath =
        Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
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
            "mcp_session_query_history",
            "mcp_todo_query",
            "mcp_todo_get",
            "mcp_todo_update",
            "mcp_todo_create",
            "mcp_todo_delete",
            "mcp_todo_plan",
            "mcp_todo_status",
            "mcp_todo_implementation",
            "mcp_repo_read",
            "mcp_repo_list",
            "mcp_repo_write",
            "mcp_desktop_launch",
            "mcp_powershell_session_create",
            "mcp_powershell_session_command",
            "mcp_powershell_session_close",
            "mcp_requirements_list_fr",
            "mcp_requirements_list_tr",
            "mcp_requirements_list_test",
            "mcp_requirements_get_fr",
            "mcp_requirements_get_tr",
            "mcp_requirements_get_test",
            "mcp_quadbrain_coding_execute",
            "mcp_client_invoke",
            "mcp_graphrag_ingest_text",
            "mcp_graphrag_list_documents",
            "mcp_graphrag_get_document_chunks",
            "mcp_graphrag_delete_document",
            "mcp_graphrag_create_entity",
            "mcp_graphrag_list_entities",
            "mcp_graphrag_get_entity",
            "mcp_graphrag_update_entity",
            "mcp_graphrag_delete_entity",
            "mcp_graphrag_create_relationship",
            "mcp_graphrag_list_relationships",
            "mcp_graphrag_get_relationship",
            "mcp_graphrag_update_relationship",
            "mcp_graphrag_delete_relationship",
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
        Assert.NotNull(hostedAgent.PowerShellSessions);
        Assert.True(baseFactoryCalled);
        var invokingClient = Assert.IsType<FunctionInvokingChatClient>(wrappedClient);
        Assert.False(invokingClient.AllowConcurrentInvocation);
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
    /// TEST-MCP-186: Verifies that the ACID profile filters the model-visible MCP tool surface,
    /// rejects unreviewed host tools by default, and preserves serialized function invocation.
    /// </summary>
    [Fact]
    public void Registration_CreateRunOptions_AcidProfileFiltersToolsAndRejectsHostTools()
    {
        var (hostedAgent, _) = CreateHostedAgent(configureOptions: static options =>
            options.UseAcidTightlyCoupledProfile());
        var existingTool = AIFunctionFactory.Create(
            (Func<string>)(() => "existing"),
            new AIFunctionFactoryOptions
            {
                Description = "Existing host tool.",
                Name = "existing_host_tool",
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            hostedAgent.CreateRunOptions(
                new ChatClientAgentRunOptions
                {
                    ChatOptions = new ChatOptions
                    {
                        Tools = [existingTool],
                    },
                }));
        var runOptions = hostedAgent.CreateRunOptions();
        var wrappedClient = runOptions.ChatClientFactory!(new StubChatClient());
        var invokingClient = Assert.IsType<FunctionInvokingChatClient>(wrappedClient);
        Assert.NotNull(runOptions.ChatOptions?.Tools);
        var toolNames = runOptions.ChatOptions.Tools
            .Select(static tool => tool.Name)
            .ToArray();

        Assert.Equal(McpAgentExecutionProfile.AcidTightlyCoupled, hostedAgent.ExecutionProfile);
        Assert.Contains("reject caller-supplied host tools", exception.Message, StringComparison.Ordinal);
        Assert.False(invokingClient.AllowConcurrentInvocation);
        Assert.False(runOptions.ChatOptions!.AllowMultipleToolCalls);
        Assert.Equal(McpAcidAgentDefinition.Instance.AllowedToolNames, toolNames);
        Assert.Contains("mcp_quadbrain_coding_execute", toolNames);
        Assert.DoesNotContain("mcp_client_invoke", toolNames);
        Assert.DoesNotContain("mcp_powershell_session_command", toolNames);
        Assert.DoesNotContain("mcp_repo_write", toolNames);
        Assert.DoesNotContain("mcp_graphrag_ingest_text", toolNames);
    }

    /// <summary>
    /// TEST-MCP-187: Verifies that the ACID runtime exposes a typed host-callable coding-agent path
    /// that executes through the same Quad Brain orchestration endpoint as the model-visible tool.
    /// </summary>
    [Fact]
    public async Task AcidRuntime_ExecuteCodingTaskAsync_RoutesThroughQuadBrainOrchestration()
    {
        var (hostedAgent, handler) = CreateHostedAgent(configureOptions: static options =>
            options.UseAcidTightlyCoupledProfile());
        using var chatClient = new StubChatClient();
        var runtime = hostedAgent.CreateAcidTightlyCoupledRuntime(chatClient);

        var response = await runtime.ExecuteCodingTaskAsync(
            new McpQuadBrainCodingAgentRequest
            {
                Prompt = "Implement a transactional rollback guard for a C# repository method.",
                TaskKind = "implementation",
                TurnId = "turn-acid-coding",
                AdmitCuriosityToGraphRag = true,
                Metadata = new Dictionary<string, string>
                {
                    ["repo"] = "McpServer",
                    ["language"] = "csharp",
                },
            },
            CancellationToken.None);

        var request = Assert.Single(handler.Requests, static request =>
            request.RequestUri.AbsolutePath == "/mcpserver/brain-slots/orchestrate");
        using var body = JsonDocument.Parse(request.Body!);
        var metadata = body.RootElement.GetProperty("metadata");

        Assert.Equal("Committed", response.Status);
        Assert.Equal("Quad Brain coding result for implementation", response.Output);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Implement a transactional rollback guard for a C# repository method.", body.RootElement.GetProperty("input").GetString());
        Assert.Equal("turn-acid-coding", body.RootElement.GetProperty("turnId").GetString());
        Assert.True(body.RootElement.GetProperty("admitCuriosityToGraphRag").GetBoolean());
        Assert.Equal("McpServer", metadata.GetProperty("repo").GetString());
        Assert.Equal("csharp", metadata.GetProperty("language").GetString());
        Assert.Equal("Microsoft.AgentFramework", metadata.GetProperty("codingAgent.surface").GetString());
        Assert.Equal("implementation", metadata.GetProperty("codingAgent.taskKind").GetString());
        Assert.Equal(nameof(McpAgentExecutionProfile.AcidTightlyCoupled), metadata.GetProperty("codingAgent.executionProfile").GetString());
        Assert.Equal(McpHostedAgentDefaults.AcidSourceType, metadata.GetProperty("codingAgent.sourceType").GetString());
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

    /// <summary>
    /// TEST-MCP-089: Verifies that the hosted-agent adapter exposes repository read, list, and write
    /// tools that delegate to the existing MCP repository client rather than introducing custom
    /// transport logic.
    /// The test uses deterministic in-memory HTTP responses so the adapter can prove repo-relative
    /// path handling and response serialization without relying on a live MCP server.
    /// </summary>
    [Fact]
    public async Task Registration_Functions_RepoOperations_ReuseExistingRepoClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var readFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_repo_read");
        var listFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_repo_list");
        var writeFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_repo_write");

        var readResult = await readFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["path"] = "README.md",
            },
            CancellationToken.None);
        var listResult = await listFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["path"] = "src",
            },
            CancellationToken.None);
        var writeResult = await writeFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["path"] = @"docs\agent-output.txt",
                ["content"] = "Generated by hosted agent tooling.",
            },
            CancellationToken.None);

        var read = DeserializeJsonResult<RepoFileReadResult>(readResult);
        var listing = DeserializeJsonResult<RepoListResult>(listResult);
        var write = DeserializeJsonResult<RepoWriteResult>(writeResult);

        Assert.Equal("README.md", read.Path);
        Assert.Equal("Hosted agent README", read.Content);
        Assert.True(read.Exists);
        Assert.Equal("src", listing.Path);
        Assert.Collection(
            listing.Entries,
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
        Assert.Equal(@"docs\agent-output.txt", write.Path);
        Assert.True(write.Written);

        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/repo/file", handler.Requests[0].RequestUri.AbsolutePath);
        Assert.Equal("http://localhost:7147/mcpserver/repo/file?path=README.md", handler.Requests[0].RequestUri.ToString());
        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("/mcpserver/repo/list", handler.Requests[1].RequestUri.AbsolutePath);
        Assert.Equal("http://localhost:7147/mcpserver/repo/list?path=src", handler.Requests[1].RequestUri.ToString());
        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.Equal("/mcpserver/repo/file", handler.Requests[2].RequestUri.AbsolutePath);

        using var requestBody = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Equal(@"docs\agent-output.txt", requestBody.RootElement.GetProperty("path").GetString());
        Assert.Equal("Generated by hosted agent tooling.", requestBody.RootElement.GetProperty("content").GetString());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the hosted-agent adapter exposes a desktop-launch tool that
    /// reuses the authenticated MCP desktop-launch endpoint instead of introducing host-specific
    /// process-spawning logic.
    /// The test uses a deterministic in-memory HTTP response so launch payload serialization and
    /// endpoint routing can be asserted without starting a real local process.
    /// </summary>
    [Fact]
    public async Task Registration_Functions_DesktopLaunch_ReusesExistingDesktopClient()
    {
        var (hostedAgent, handler) = CreateHostedAgent("desktop-secret");
        var launchFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_desktop_launch");

        var launchResult = await launchFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["executablePath"] = @"C:\Windows\System32\cmd.exe",
                ["arguments"] = "/c exit 0",
                ["workingDirectory"] = @"C:\Windows\System32",
                ["environmentVariables"] = new Dictionary<string, string> { ["TEST_ENV"] = "true" },
                ["createNoWindow"] = true,
                ["windowStyle"] = "Hidden",
                ["waitForExit"] = true,
                ["timeoutMs"] = 5000
            },
            CancellationToken.None);

        var launch = DeserializeJsonResult<DesktopLaunchResult>(launchResult);

        Assert.True(launch.Success);
        Assert.Equal(4242, launch.ProcessId);
        Assert.Equal(0, launch.ExitCode);
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("/mcpserver/desktop/launch", handler.Requests[0].RequestUri.AbsolutePath);
        Assert.Equal("desktop-secret", handler.Requests[0].DesktopLaunchToken);

        using var requestBody = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal(@"C:\Windows\System32\cmd.exe", requestBody.RootElement.GetProperty("executablePath").GetString());
        Assert.Equal("/c exit 0", requestBody.RootElement.GetProperty("arguments").GetString());
        Assert.Equal("Hidden", requestBody.RootElement.GetProperty("windowStyle").GetString());
        Assert.True(requestBody.RootElement.GetProperty("createNoWindow").GetBoolean());
        Assert.True(requestBody.RootElement.GetProperty("waitForExit").GetBoolean());
        Assert.Equal("true", requestBody.RootElement.GetProperty("environmentVariables").GetProperty("TEST_ENV").GetString());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the hosted-agent adapter exposes local PowerShell session tools
    /// that host stateful runspaces directly inside the current .NET agent process.
    /// The test uses simple variable assignment and reuse commands so session persistence is proven
    /// without depending on any external shell executable or HTTP endpoint.
    /// </summary>
    [Fact]
    public async Task Registration_Functions_PowerShellSessionCommands_RunInsidePersistentLocalSession()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var createFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_powershell_session_create");
        var commandFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_powershell_session_command");
        var closeFunction = hostedAgent.Registration.Functions.Single(static function =>
            function.Name == "mcp_powershell_session_close");

        var createResult = await createFunction.InvokeAsync(
            new AIFunctionArguments(),
            CancellationToken.None);

        var createdSession = DeserializeJsonResult<PowerShellSessionCreateResult>(createResult);
        Assert.True(createdSession.Success);
        Assert.NotNull(createdSession.SessionId);
        Assert.Equal(TestWorkspacePath, createdSession.CurrentLocation);

        var firstCommandResult = await commandFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["sessionId"] = createdSession.SessionId!,
                ["command"] = "$global:Answer = 42; $global:Answer",
            },
            CancellationToken.None);
        var secondCommandResult = await commandFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["sessionId"] = createdSession.SessionId!,
                ["command"] = "$global:Answer",
            },
            CancellationToken.None);
        var closeResult = await closeFunction.InvokeAsync(
            new AIFunctionArguments
            {
                ["sessionId"] = createdSession.SessionId!,
            },
            CancellationToken.None);

        var firstCommand = DeserializeJsonResult<PowerShellSessionCommandResult>(firstCommandResult);
        var secondCommand = DeserializeJsonResult<PowerShellSessionCommandResult>(secondCommandResult);
        var closedSession = DeserializeJsonResult<PowerShellSessionCloseResult>(closeResult);

        Assert.True(firstCommand.Success);
        Assert.Equal("42", firstCommand.Output);
        Assert.Equal(createdSession.SessionId, firstCommand.SessionId);
        Assert.Equal(TestWorkspacePath, firstCommand.CurrentLocation);
        Assert.True(secondCommand.Success);
        Assert.Equal("42", secondCommand.Output);
        Assert.Equal(createdSession.SessionId, secondCommand.SessionId);
        Assert.True(closedSession.Success);
        Assert.Equal(createdSession.SessionId, closedSession.SessionId);
        Assert.Empty(handler.Requests);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that host applications can execute direct interactive PowerShell
    /// commands through <see cref="IMcpHostedAgent.PowerShellSessions"/> without routing the command
    /// through the model-facing MCP tool adapter surface.
    /// The test uses <c>Read-Host</c> to prove that the host-facing manager can accept interactive
    /// input, preserve session state, and keep the runspace local to the hosted agent instance.
    /// </summary>
    [Fact]
    public async Task PowerShellSessions_ExecuteInteractiveCommand_PreservesHostLocalSessionState()
    {
        var (hostedAgent, handler) = CreateHostedAgent();
        var createdSession = hostedAgent.PowerShellSessions.CreateSession(TestWorkspacePath);
        Assert.True(createdSession.Success);
        Assert.NotNull(createdSession.SessionId);

        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        var interactiveResult = await hostedAgent.PowerShellSessions.ExecuteInteractiveCommandAsync(
            createdSession.SessionId!,
            "$global:PromptValue = Read-Host 'Name'; $global:PromptValue",
            static _ => "LocalShell",
            outputWriter,
            errorWriter,
            CancellationToken.None);
        var persistedResult = await hostedAgent.PowerShellSessions.ExecuteCommandAsync(
            createdSession.SessionId!,
            "$global:PromptValue",
            CancellationToken.None);
        var closeResult = hostedAgent.PowerShellSessions.CloseSession(createdSession.SessionId!);

        Assert.True(interactiveResult.Success);
        Assert.Equal("LocalShell", interactiveResult.Output);
        Assert.Contains("Name", outputWriter.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, errorWriter.ToString());
        Assert.True(persistedResult.Success);
        Assert.Equal("LocalShell", persistedResult.Output);
        Assert.True(closeResult.Success);
        Assert.Empty(handler.Requests);
    }

    private static (McpHostedAgent HostedAgent, RecordingMcpHttpMessageHandler Handler) CreateHostedAgent(
        string? desktopLaunchToken = null,
        Action<McpAgentOptions>? configureOptions = null)
    {
        var handler = new RecordingMcpHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var client = new McpServerClient(
            httpClient,
            new McpServerClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = new Uri("http://localhost:7147"),
                DesktopLaunchToken = desktopLaunchToken,
                WorkspacePath = TestWorkspacePath,
            });
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 09, 15, 01, 05, TimeSpan.Zero));
        var configuredOptions = new McpAgentOptions
        {
            ApiKey = "test-key",
            BaseUrl = new Uri("http://localhost:7147"),
            DesktopLaunchToken = desktopLaunchToken,
            SourceType = "Codex",
            WorkspacePath = TestWorkspacePath,
        };
        configureOptions?.Invoke(configuredOptions);
        var options = Options.Create(configuredOptions);
        var identifiers = new McpSessionIdentifierFactory(options, timeProvider);
        var sessionLog = new McpServer.McpAgent.SessionLog.SessionLogWorkflow(client, identifiers, timeProvider);
        var todo = new McpServer.McpAgent.Todo.TodoWorkflow(client);
        var requirements = new RequirementsWorkflow(client.Requirements);
        var clientPassthrough = new GenericClientPassthrough(client);
        var replSessionLogAdapter = new SessionLogClientAdapter(client.SessionLog);
        var replSessionLog = new McpServer.Repl.Core.SessionLogWorkflow(replSessionLogAdapter, timeProvider);
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        return (
            new McpHostedAgent(
                client,
                identifiers,
                new ChatClientAgentOptions
                {
                    Description = configuredOptions.Description,
                    Id = configuredOptions.AgentId,
                    Name = configuredOptions.AgentName,
                },
                options,
                sessionLog,
                todo,
                requirements,
                clientPassthrough,
                replSessionLog,
                serviceProvider),
            handler);
    }

    private static JsonElement GetJsonProperty(JsonElement element, string camelCaseName, string pascalCaseName) =>
        element.TryGetProperty(camelCaseName, out var camelCaseProperty)
            ? camelCaseProperty
            : element.GetProperty(pascalCaseName);

    private static T DeserializeJsonResult<T>(object? result)
    {
        var json = Assert.IsType<JsonElement>(result);
        var value = JsonSerializer.Deserialize<T>(json.GetRawText());
        Assert.NotNull(value);
        return value;
    }

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

            var desktopLaunchToken = request.Headers.TryGetValues("X-Desktop-Launch-Token", out var tokenValues)
                ? tokenValues.SingleOrDefault()
                : null;

            Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, body, desktopLaunchToken));

            return request.RequestUri!.AbsolutePath switch
            {
                "/mcpserver/sessionlog" => CreateSessionLogResponse(body!),
                "/mcpserver/todo/MCP-AGENT-001" when request.Method == HttpMethod.Put => CreateTodoMutationResponse(body!),
                "/mcpserver/todo/MCP-AGENT-001/prompt/plan" => CreatePlanResponse(),
                "/mcpserver/repo/file" when request.Method == HttpMethod.Get => CreateRepoReadResponse(request.RequestUri!),
                "/mcpserver/repo/list" when request.Method == HttpMethod.Get => CreateRepoListResponse(request.RequestUri!),
                "/mcpserver/repo/file" when request.Method == HttpMethod.Post => CreateRepoWriteResponse(body!),
                "/mcpserver/desktop/launch" when request.Method == HttpMethod.Post => CreateDesktopLaunchResponse(),
                "/mcpserver/brain-slots/orchestrate" when request.Method == HttpMethod.Post => CreateQuadBrainOrchestrationResponse(body!),
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

        private static HttpResponseMessage CreateRepoReadResponse(Uri requestUri)
        {
            var path = GetQueryParameter(requestUri, "path") ?? string.Empty;
            return CreateJsonResponse(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(
                    new RepoFileReadResult
                    {
                        Path = path,
                        Content = "Hosted agent README",
                        Exists = true,
                    }));
        }

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

        private static HttpResponseMessage CreateRepoWriteResponse(string body)
        {
            using var document = JsonDocument.Parse(body);
            var path = document.RootElement.GetProperty("path").GetString();
            return CreateJsonResponse(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(
                    new RepoWriteResult
                    {
                        Path = path,
                        Written = true,
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

        private static HttpResponseMessage CreateQuadBrainOrchestrationResponse(string body)
        {
            using var document = JsonDocument.Parse(body);
            var taskKind = document.RootElement
                .GetProperty("metadata")
                .GetProperty("codingAgent.taskKind")
                .GetString();

            return CreateJsonResponse(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(
                    new QuadBrainOrchestrationResponse
                    {
                        Status = "Committed",
                        Reason = "Committed",
                        Output = $"Quad Brain coding result for {taskKind}",
                        TransactionId = "txn-quad-coding",
                        DiffgramId = "diff-quad-coding",
                        StartedAtUtc = new DateTimeOffset(2026, 03, 09, 15, 01, 05, TimeSpan.Zero),
                        CompletedAtUtc = new DateTimeOffset(2026, 03, 09, 15, 01, 06, TimeSpan.Zero),
                        RoleResults =
                        [
                            new QuadBrainRoleResult
                            {
                                Role = "ArbiterOfTruth",
                                SlotId = "brain-slot:aot",
                                Status = "Committed",
                                Reason = "Committed",
                                ModelId = "grok-build",
                                TransactionId = "txn-aot",
                                DiffgramId = "diff-aot",
                                Output = $"Quad Brain coding result for {taskKind}",
                                OrchestrationWeight = 1,
                                WeightVersion = 1,
                            },
                        ],
                    }));
        }

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
    /// TEST-MCP-089: Captures a single recorded HTTP request emitted by the hosted-agent adapter.
    /// </summary>
    /// <param name="Method">The emitted HTTP method.</param>
    /// <param name="RequestUri">The emitted request URI.</param>
    /// <param name="Body">The serialized request body, when present.</param>
    /// <param name="DesktopLaunchToken">The privileged desktop-launch header, when present.</param>
    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri RequestUri,
        string? Body,
        string? DesktopLaunchToken);

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
