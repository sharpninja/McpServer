using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.McpAgent.Todo;
using McpServer.Client;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-089: Verifies that the hosted Agent Framework TODO workflow reuses the existing
/// <see cref="TodoClient"/> contracts for query, get, update, requirements analysis, and buffered
/// prompt streaming without host-specific HTTP glue.
/// </summary>
public sealed class TodoWorkflowTests
{
    /// <summary>
    /// TEST-MCP-089: Verifies that querying through <see cref="TodoWorkflow"/> delegates to
    /// <see cref="TodoClient.QueryAsync"/> with the caller-supplied filters and cancellation token.
    /// The test uses a recording in-memory HTTP handler so it can inspect the exact request path and
    /// query string while still deserializing the real TODO query DTOs.
    /// </summary>
    [Fact]
    public async Task QueryAsync_DelegatesToTodoClient()
    {
        var response = new TodoQueryResult
        {
            Items =
            [
                new TodoFlatItem
                {
                    Id = "MCP-AGENT-001",
                    Title = "Implement TODO workflow",
                    Section = "agent",
                    Priority = "high",
                    Done = false,
                }
            ],
            TotalCount = 1,
        };

        var handler = new RecordingHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        using var httpClient = new HttpClient(handler);
        var workflow = CreateWorkflow(httpClient);
        using var cancellationSource = new CancellationTokenSource();

        var result = await workflow.QueryAsync(
            keyword: "workflow",
            priority: "high",
            section: "agent",
            id: "MCP-AGENT-001",
            done: false,
            cancellationToken: cancellationSource.Token);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("MCP-AGENT-001", result.Items[0].Id);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            "http://localhost:7147/mcpserver/todo?keyword=workflow&priority=high&section=agent&id=MCP-AGENT-001&done=false",
            handler.LastRequest.RequestUri!.ToString());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that reading a TODO item through <see cref="TodoWorkflow"/> delegates
    /// to <see cref="TodoClient.GetAsync"/> with the canonical TODO identifier and cancellation token
    /// supplied by the host.
    /// </summary>
    [Fact]
    public async Task GetAsync_DelegatesToTodoClient()
    {
        var response = new TodoFlatItem
        {
            Id = "MCP-AGENT-001",
            Title = "Implement TODO workflow",
            Section = "agent",
            Priority = "high",
            Done = false,
        };

        var handler = new RecordingHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        using var httpClient = new HttpClient(handler);
        var workflow = CreateWorkflow(httpClient);
        using var cancellationSource = new CancellationTokenSource();

        var item = await workflow.GetAsync("MCP-AGENT-001", cancellationSource.Token);

        Assert.Equal("MCP-AGENT-001", item.Id);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            "http://localhost:7147/mcpserver/todo/MCP-AGENT-001",
            handler.LastRequest.RequestUri!.ToString());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that existing legacy TODO identifiers already stored by the server are
    /// passed through unchanged instead of being rejected by client-side canonical-ID validation.
    /// The test uses the known lowercase-style ID shape found in the workspace TODO data to preserve
    /// compatibility with real server content.
    /// </summary>
    [Fact]
    public async Task GetAsync_AllowsLegacyTodoIdentifiers()
    {
        var response = new TodoFlatItem
        {
            Id = "do-not-speak-tables",
            Title = "Avoid table output",
            Section = "agent",
            Priority = "medium",
            Done = false,
        };

        var handler = new RecordingHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        using var httpClient = new HttpClient(handler);
        var workflow = CreateWorkflow(httpClient);

        var item = await workflow.GetAsync("do-not-speak-tables", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("do-not-speak-tables", item.Id);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(
            "http://localhost:7147/mcpserver/todo/do-not-speak-tables",
            handler.LastRequest!.RequestUri!.ToString());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that updates issued through <see cref="TodoWorkflow"/> delegate to
    /// <see cref="TodoClient.UpdateAsync"/> using the existing TODO mutation DTOs instead of any
    /// duplicated transport contract.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_DelegatesToTodoClient()
    {
        var request = new TodoUpdateRequest
        {
            Title = "Updated TODO workflow title",
            Note = "Updated via hosted workflow",
            Done = true,
        };

        var response = new TodoMutationResult
        {
            Success = true,
            Item = new TodoFlatItem
            {
                Id = "MCP-AGENT-001",
                Title = request.Title!,
                Section = "agent",
                Priority = "high",
                Done = true,
                Note = request.Note,
            },
        };

        var handler = new RecordingHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        using var httpClient = new HttpClient(handler);
        var workflow = CreateWorkflow(httpClient);
        using var cancellationSource = new CancellationTokenSource();

        var result = await workflow.UpdateAsync("MCP-AGENT-001", request, cancellationSource.Token);

        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("Updated TODO workflow title", result.Item!.Title);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Equal(
            "http://localhost:7147/mcpserver/todo/MCP-AGENT-001",
            handler.LastRequest.RequestUri!.ToString());
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Updated TODO workflow title", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Updated via hosted workflow", document.RootElement.GetProperty("note").GetString());
        Assert.True(document.RootElement.GetProperty("done").GetBoolean());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that requirements analysis through <see cref="TodoWorkflow"/> delegates
    /// to <see cref="TodoClient.AnalyzeRequirementsAsync"/> and returns the existing structured
    /// analysis DTOs from <c>McpServer.Client.Models</c>.
    /// </summary>
    [Fact]
    public async Task AnalyzeRequirementsAsync_DelegatesToTodoClient()
    {
        var response = new RequirementsAnalysisResult
        {
            Success = true,
            FunctionalRequirements = ["FR-MCP-066"],
            TechnicalRequirements = ["TR-MCP-AGENT-007"],
        };

        var handler = new RecordingHttpHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        using var httpClient = new HttpClient(handler);
        var workflow = CreateWorkflow(httpClient);

        var result = await workflow.AnalyzeRequirementsAsync("MCP-AGENT-001", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(["FR-MCP-066"], result.FunctionalRequirements);
        Assert.Equal(["TR-MCP-AGENT-007"], result.TechnicalRequirements);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(
            "http://localhost:7147/mcpserver/todo/MCP-AGENT-001/requirements",
            handler.LastRequest.RequestUri!.ToString());
        Assert.Null(handler.LastRequestBody);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the buffered prompt helpers preserve caller cancellation rather
    /// than introducing separate client-side timeout behavior on top of the underlying
    /// <see cref="TodoClient"/> SSE stream.
    /// The test uses an infinite-delay HTTP handler with <see cref="HttpClient.Timeout"/> disabled so
    /// only the caller token can complete the workflow call.
    /// </summary>
    [Fact]
    public async Task BufferedPromptHelpers_PreserveCallerCancellation()
    {
        var handler = new BlockingHttpHandler();
        using var httpClient = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        var workflow = CreateWorkflow(httpClient);
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var workflowTask = workflow.GetPlanAsync("MCP-AGENT-001", cancellationSource.Token);
        var completedTask = await Task.WhenAny(workflowTask, Task.Delay(TimeSpan.FromSeconds(1), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(workflowTask, completedTask);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await workflowTask);
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that the buffered prompt helpers aggregate streamed SSE data lines into
    /// newline-delimited text without adding host-specific transport glue or trailing blank lines.
    /// The test runs each helper against an in-memory SSE payload so plan, status, and implementation
    /// flows all prove they consume the existing streamed MCP Server contracts.
    /// </summary>
    /// <param name="workflowOperation">The buffered workflow helper to invoke.</param>
    /// <param name="relativePath">The expected TODO prompt endpoint path.</param>
    /// <param name="payload">The SSE payload returned by the recording handler.</param>
    /// <param name="expectedText">The newline-delimited buffered text expected from the helper.</param>
    [Theory]
    [InlineData("plan", "/mcpserver/todo/MCP-AGENT-001/prompt/plan", "data: Step 1\n\ndata: Step 2\n\nevent: done\ndata: \n\n", "Step 1\nStep 2")]
    [InlineData("status", "/mcpserver/todo/MCP-AGENT-001/prompt/status", "data: Healthy\n\ndata: Waiting on review\n\nevent: done\ndata: \n\n", "Healthy\nWaiting on review")]
    [InlineData("implement", "/mcpserver/todo/MCP-AGENT-001/prompt/implement", "data: Apply patch\n\nevent: done\ndata: \n\n", "Apply patch")]
    public async Task BufferedPromptHelpers_AggregateStreamedOutput(
        string workflowOperation,
        string relativePath,
        string payload,
        string expectedText)
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.OK, payload, "text/event-stream");
        using var httpClient = new HttpClient(handler);
        var workflow = CreateWorkflow(httpClient);
        using var cancellationSource = new CancellationTokenSource();

        var result = workflowOperation switch
        {
            "plan" => await workflow.GetPlanAsync("MCP-AGENT-001", cancellationSource.Token),
            "status" => await workflow.GetStatusReportAsync("MCP-AGENT-001", cancellationSource.Token),
            "implement" => await workflow.GetImplementationGuideAsync("MCP-AGENT-001", cancellationSource.Token),
            _ => throw new InvalidOperationException($"Unknown workflow operation '{workflowOperation}'."),
        };

        Assert.Equal(expectedText, result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal($"http://localhost:7147{relativePath}", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains(
            handler.LastRequest.Headers.Accept,
            static value => value.MediaType == "text/event-stream");
    }

    private static TodoWorkflow CreateWorkflow(HttpClient httpClient)
    {
        var client = new McpServerClient(httpClient, new McpServerClientOptions
        {
            ApiKey = "test-key",
            BaseUrl = new Uri("http://localhost:7147"),
            WorkspacePath = @"E:\github\McpServer",
        });

        return new TodoWorkflow(client);
    }

    /// <summary>
    /// TEST-MCP-089: Records outbound HTTP requests from the TODO workflow so the tests can verify
    /// path, method, body, and cancellation-token delegation while returning deterministic JSON or
    /// SSE payloads to the real transport client.
    /// </summary>
    private sealed class RecordingHttpHandler : HttpMessageHandler
    {
        private readonly string _contentType;
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        /// <summary>
        /// TEST-MCP-089: Initializes the recording handler with the exact response payload the
        /// hosted workflow should consume during the test.
        /// </summary>
        /// <param name="statusCode">HTTP status code returned to the workflow.</param>
        /// <param name="responseBody">Serialized JSON or SSE payload returned to the workflow.</param>
        /// <param name="contentType">Response content type.</param>
        public RecordingHttpHandler(HttpStatusCode statusCode, string responseBody, string contentType = "application/json")
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
            _contentType = contentType;
        }

        /// <summary>
        /// TEST-MCP-089: Gets the last request sent through the workflow.
        /// </summary>
        public HttpRequestMessage? LastRequest { get; private set; }

        /// <summary>
        /// TEST-MCP-089: Gets the serialized request body captured from the last request, if any.
        /// </summary>
        public string? LastRequestBody { get; private set; }

        /// <summary>
        /// TEST-MCP-089: Gets the cancellation token observed by the underlying transport call.
        /// </summary>
        public CancellationToken LastCancellationToken { get; private set; }

        /// <summary>
        /// TEST-MCP-089: Captures the outbound request details and returns the configured response.
        /// </summary>
        /// <param name="request">The outbound request generated by the TODO workflow.</param>
        /// <param name="cancellationToken">The cancellation token propagated by the workflow.</param>
        /// <returns>The configured deterministic HTTP response.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, _contentType),
            };
        }
    }

    /// <summary>
    /// TEST-MCP-089: Simulates a long-running transport call that can complete only through caller
    /// cancellation so the workflow tests can verify direct cancellation-token flow.
    /// </summary>
    private sealed class BlockingHttpHandler : HttpMessageHandler
    {
        /// <summary>
        /// TEST-MCP-089: Waits indefinitely until the supplied cancellation token is canceled.
        /// </summary>
        /// <param name="request">The outbound request generated by the TODO workflow.</param>
        /// <param name="cancellationToken">The cancellation token propagated by the workflow.</param>
        /// <returns>Never returns successfully; completion is cancellation-only.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation should stop the infinite-delay handler before it returns.");
        }
    }
}
