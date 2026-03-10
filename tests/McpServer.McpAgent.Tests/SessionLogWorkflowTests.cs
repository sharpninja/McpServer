using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using McpServer.McpAgent.SessionLog;
using McpServer.Client;
using McpServer.Client.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.McpAgent.Tests;

/// <summary>
/// TEST-MCP-089: Verifies the built-in session-log workflow lifecycle and its stable
/// interactions with <see cref="SessionLogClient"/>.
/// </summary>
public sealed class SessionLogWorkflowTests
{
    /// <summary>
    /// TEST-MCP-089: Verifies that bootstrapping a workflow generates canonical session identifiers
    /// from the configured source type and model slug, then submits the initial session payload via
    /// the shared <see cref="SessionLogClient"/> surface.
    /// The test uses a recording HTTP handler, a deterministic clock, and a fixed workspace path so
    /// the payload shape can be asserted without a live MCP server.
    /// </summary>
    [Fact]
    public async Task BootstrapAsync_SubmitsCanonicalInitialSessionPayload()
    {
        var (_, workflow, handler) = CreateSut();

        var context = await workflow.BootstrapAsync(new SessionLogBootstrapRequest
        {
            Title = "Resume MCP-AGENTFRAMEWORK-001",
            Model = "gpt-5.4",
            Workspace = new WorkspaceInfoDto
            {
                Project = "McpServer",
                Branch = "feature/session-log",
            },
        });

        Assert.Equal("Codex", context.SourceType);
        Assert.Equal("Codex-20260309T150105Z-gpt-5-4", context.SessionId);
        Assert.Single(handler.Requests);

        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/mcpserver/sessionlog", request.RequestUri.AbsolutePath);
        Assert.Equal("test-key", request.ApiKey);
        Assert.Equal(@"E:\github\McpServer", request.WorkspacePath);

        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("Codex", body.RootElement.GetProperty("sourceType").GetString());
        Assert.Equal("Codex-20260309T150105Z-gpt-5-4", body.RootElement.GetProperty("sessionId").GetString());
        Assert.Equal("Resume MCP-AGENTFRAMEWORK-001", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("gpt-5.4", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("in_progress", body.RootElement.GetProperty("status").GetString());
        Assert.False(body.RootElement.TryGetProperty("entries", out _));
    }

    /// <summary>
    /// TEST-MCP-089: Verifies the end-to-end turn lifecycle for begin, dialog append, action append,
    /// complete, and explicit persist operations.
    /// The test uses a deterministic clock and recording HTTP handler so it can assert both the
    /// in-memory continuation state and the underlying <see cref="SessionLogClient"/> request shapes.
    /// </summary>
    [Fact]
    public async Task TurnLifecycle_AppendsDialogAndActionsThenCompletesAndPersists()
    {
        var (_, workflow, handler) = CreateSut();
        await workflow.BootstrapAsync(new SessionLogBootstrapRequest
        {
            Title = "Implement MCP-AGENTFRAMEWORK-001",
            Model = "gpt-5.4",
        });

        var turn = await workflow.BeginTurnAsync(new SessionLogTurnCreateRequest
        {
            QueryTitle = "Implement MCP Agent session-log workflow",
            QueryText = "Implement the built-in session-log workflow end-to-end.",
            Tags = ["session-log"],
            ContextList = ["plan.md"],
        });

        await workflow.AppendDialogAsync(new SessionLogDialogAppendRequest
        {
            RequestId = turn.RequestId,
            Items =
            [
                new ProcessingDialogItemDto
                {
                    Timestamp = "2026-03-09T15:01:06.0000000+00:00",
                    Role = "model",
                    Content = "Analyzing workflow gaps.",
                    Category = "reasoning",
                },
            ],
        });

        await workflow.AppendActionsAsync(new SessionLogActionAppendRequest
        {
            RequestId = turn.RequestId,
            Actions =
            [
                new UnifiedActionDto
                {
                    Description = "Updated session-log workflow implementation",
                    Type = "edit",
                    Status = "completed",
                    FilePath = @"src\McpServer.McpAgent\SessionLog\SessionLogWorkflow.cs",
                },
                new UnifiedActionDto
                {
                    Description = "Added session-log workflow lifecycle tests",
                    Type = "create",
                    Status = "completed",
                    FilePath = @"tests\McpServer.McpAgent.Tests\SessionLogWorkflowTests.cs",
                },
            ],
        });

        var completedTurn = await workflow.CompleteTurnAsync(new SessionLogTurnCompleteRequest
        {
            RequestId = turn.RequestId,
            Response = "Implemented the workflow lifecycle.",
            Interpretation = "Provide dedicated bootstrap, turn, dialog, action, completion, failure, and persist operations.",
            TokenCount = 321,
            FilesModified =
            [
                @"src\McpServer.McpAgent\SessionLog\SessionLogWorkflow.cs",
                @"tests\McpServer.McpAgent.Tests\SessionLogWorkflowTests.cs",
            ],
            DesignDecisions =
            [
                "Mirror appended dialog items locally so later full-session submits preserve in-host continuation state.",
            ],
            RequirementsDiscovered = ["FR-MCP-066", "TR-MCP-AGENT-007"],
        });

        var persisted = await workflow.PersistAsync();

        Assert.Equal("req-20260309T150105Z-implement-mcp-agent-session-log-workflow", turn.RequestId);
        Assert.Equal("completed", completedTurn.Status);
        Assert.Equal("Implemented the workflow lifecycle.", completedTurn.Response);
        Assert.Single(completedTurn.ProcessingDialog);
        Assert.Equal(2, completedTurn.Actions.Count);
        Assert.Equal(1, completedTurn.Actions[0].Order);
        Assert.Equal(2, completedTurn.Actions[1].Order);
        Assert.Single(persisted.Turns);

        Assert.Equal(6, handler.Requests.Count);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[0].RequestUri.AbsolutePath);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[1].RequestUri.AbsolutePath);
        Assert.Equal(
            $"/mcpserver/sessionlog/Codex/{Uri.EscapeDataString(persisted.SessionId)}/{Uri.EscapeDataString(turn.RequestId)}/dialog",
            handler.Requests[2].RequestUri.AbsolutePath);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[3].RequestUri.AbsolutePath);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[4].RequestUri.AbsolutePath);
        Assert.Equal("/mcpserver/sessionlog", handler.Requests[5].RequestUri.AbsolutePath);

        using var dialogBody = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Equal("Analyzing workflow gaps.", dialogBody.RootElement[0].GetProperty("content").GetString());

        using var finalBody = JsonDocument.Parse(handler.Requests[5].Body!);
        var finalTurn = finalBody.RootElement.GetProperty("entries")[0];
        Assert.Equal("completed", finalTurn.GetProperty("status").GetString());
        Assert.Equal("Implemented the workflow lifecycle.", finalTurn.GetProperty("response").GetString());
        Assert.Equal(321, finalTurn.GetProperty("tokenCount").GetInt32());
        Assert.Equal(2, finalTurn.GetProperty("actions").GetArrayLength());
        Assert.Equal(1, finalTurn.GetProperty("processingDialog").GetArrayLength());
        Assert.Equal(2, finalTurn.GetProperty("filesModified").GetArrayLength());
        Assert.Equal("FR-MCP-066", finalTurn.GetProperty("requirementsDiscovered")[0].GetString());
        Assert.Equal(
            "Mirror appended dialog items locally so later full-session submits preserve in-host continuation state.",
            finalTurn.GetProperty("designDecisions")[0].GetString());
    }

    /// <summary>
    /// TEST-MCP-089: Verifies that failing a turn records a failed status, failure note, and
    /// blockers in the submitted session log.
    /// The test uses the same deterministic workflow fixture as the happy-path test so the failure
    /// behavior can be asserted with stable IDs and payload ordering.
    /// </summary>
    [Fact]
    public async Task FailTurnAsync_RecordsFailureStateInSubmittedSessionLog()
    {
        var (_, workflow, handler) = CreateSut();
        await workflow.BootstrapAsync(new SessionLogBootstrapRequest
        {
            Title = "Investigate build break",
            Model = "gpt-5.4",
        });

        var turn = await workflow.BeginTurnAsync(new SessionLogTurnCreateRequest
        {
            QueryTitle = "Investigate build break",
            QueryText = "Find the failing build step.",
        });

        var failedTurn = await workflow.FailTurnAsync(new SessionLogTurnFailureRequest
        {
            RequestId = turn.RequestId,
            Response = "The build still fails.",
            FailureNote = "Compilation error in SessionLogWorkflow.cs.",
            Blockers = ["dotnet build reports CS1591 in a new public API surface."],
        });

        Assert.Equal("failed", failedTurn.Status);
        Assert.Equal("Compilation error in SessionLogWorkflow.cs.", failedTurn.FailureNote);
        Assert.Single(failedTurn.Blockers);

        Assert.Equal(3, handler.Requests.Count);
        using var body = JsonDocument.Parse(handler.Requests[2].Body!);
        var finalTurn = body.RootElement.GetProperty("entries")[0];
        Assert.Equal("failed", finalTurn.GetProperty("status").GetString());
        Assert.Equal("Compilation error in SessionLogWorkflow.cs.", finalTurn.GetProperty("failureNote").GetString());
        Assert.Equal("dotnet build reports CS1591 in a new public API surface.", finalTurn.GetProperty("blockers")[0].GetString());
    }

    private static (McpServerClient Client, SessionLogWorkflow Workflow, RecordingHttpMessageHandler Handler) CreateSut()
    {
        var handler = new RecordingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"E:\github\McpServer",
        };

        var client = new McpServerClient(httpClient, options);
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 09, 15, 01, 05, TimeSpan.Zero));
        var identifiers = new McpSessionIdentifierFactory(
            Options.Create(new McpAgentOptions
            {
                ApiKey = "test-key",
                BaseUrl = new Uri("http://localhost:7147"),
                SourceType = "Codex",
                WorkspacePath = @"E:\github\McpServer",
            }),
            timeProvider);

        return (client, new SessionLogWorkflow(client, identifiers, timeProvider), handler);
    }

    /// <summary>
    /// TEST-MCP-089: Captures every HTTP request emitted by the workflow and returns a stable
    /// JSON response appropriate for the requested session-log endpoint.
    /// </summary>
    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private long _submitCount;

        /// <summary>
        /// Gets the ordered request log captured during a test run.
        /// </summary>
        public List<RecordedRequest> Requests { get; } = [];

        /// <summary>
        /// Sends a recorded HTTP response for the session-log endpoint under test.
        /// </summary>
        /// <param name="request">The outbound request emitted by the workflow.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the client.</param>
        /// <returns>A synthetic JSON response tailored to the request path.</returns>
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
                    request.Headers.TryGetValues("X-Workspace-Path", out var workspacePaths) ? workspacePaths.Single() : null));

            if (request.RequestUri!.AbsolutePath.EndsWith("/dialog", StringComparison.Ordinal))
            {
                var segments = request.RequestUri.Segments
                    .Select(static segment => Uri.UnescapeDataString(segment.Trim('/')))
                    .Where(static segment => !string.IsNullOrEmpty(segment))
                    .ToArray();

                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    $$"""
                    {"agent":"{{segments[^4]}}","sessionId":"{{segments[^3]}}","requestId":"{{segments[^2]}}","totalDialogCount":1}
                    """);
            }

            _submitCount++;
            using var document = JsonDocument.Parse(body!);
            var sourceType = document.RootElement.GetProperty("sourceType").GetString();
            var sessionId = document.RootElement.GetProperty("sessionId").GetString();

            return CreateJsonResponse(
                HttpStatusCode.Created,
                $$"""
                {"id":{{_submitCount}},"sourceType":"{{sourceType}}","sessionId":"{{sessionId}}"}
                """);
        }

        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string body) => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>
    /// TEST-MCP-089: Captures a single recorded HTTP request for later assertions.
    /// </summary>
    /// <param name="Method">The emitted HTTP method.</param>
    /// <param name="RequestUri">The emitted request URI.</param>
    /// <param name="Body">The serialized request body, when present.</param>
    /// <param name="ApiKey">The emitted API key header, when present.</param>
    /// <param name="WorkspacePath">The emitted workspace-path header, when present.</param>
    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri RequestUri,
        string? Body,
        string? ApiKey,
        string? WorkspacePath);

    /// <summary>
    /// TEST-MCP-089: Provides a deterministic clock for session-log workflow tests.
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
}
