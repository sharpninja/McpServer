using System.Net;
using System.Text;
using System.Text.Json;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core.Tests;

/// <summary>Regression tests for YAML-to-client binding through the production REPL passthrough.</summary>
public sealed class GenericClientPassthroughYamlBindingTests
{
    /// <summary>Nested object-keyed YAML lists must survive binding into typed client request models.</summary>
    [Fact]
    public async Task ClientCreateFrAsync_NestedYamlAcceptanceCriteria_SendsCriteriaToRestClient()
    {
        var handler = new CapturingHttpHandler(
            """
            {
              "id":"FR-MCP-901",
              "title":"FR",
              "body":"Body",
              "workspaceId":"F:\\GitHub\\McpServer",
              "priority":"medium",
              "status":"pending",
              "acceptanceCriteria":[
                {
                  "id":"AC-MCP-901",
                  "text":"Criteria survives binding",
                  "isSatisfied":true,
                  "evidence":"Captured by test"
                }
              ]
            }
            """);
        using var http = new HttpClient(handler);
        var client = new McpServerClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer"
        });
        var passthrough = new GenericClientPassthrough(client);
        var dispatcher = new ReplCommandDispatcher(passthrough);
        var protocol = new AgentStdioProtocol(new YamlSerializer(), dispatcher);
        var input =
            """
            type: request
            payload:
              requestId: req-repl-ac-001
              method: client.Requirements.CreateFrAsync
              params:
                request:
                  id: FR-MCP-901
                  title: FR
                  body: Body
                  priority: medium
                  acceptanceCriteria:
                    - id: AC-MCP-901
                      text: Criteria survives binding
                      isSatisfied: true
                      evidence: Captured by test

            """;

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await protocol.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.True(output.Contains("type: result", StringComparison.Ordinal), output);
        Assert.Contains("\"acceptanceCriteria\"", handler.LastRequestBody);
        Assert.Contains("\"id\":\"AC-MCP-901\"", handler.LastRequestBody);
        Assert.Contains("\"isSatisfied\":true", handler.LastRequestBody);
    }

    /// <summary>Session log DTO fields and numeric YAML scalars must survive production passthrough binding.</summary>
    [Fact]
    public async Task ClientSessionLogSubmitAsync_StructuredTurnWithYamlNumbers_SendsCompleteDto()
    {
        var handler = new CapturingHttpHandler(
            """
            {
              "id": 42,
              "sourceType": "ClaudeCode",
              "sessionId": "ClaudeCode-20260610T154039Z-structured-binding"
            }
            """);
        using var http = new HttpClient(handler);
        var client = new McpServerClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer"
        });
        var passthrough = new GenericClientPassthrough(client);
        var dispatcher = new ReplCommandDispatcher(passthrough);
        var protocol = new AgentStdioProtocol(new YamlSerializer(), dispatcher);
        var input =
            """
            type: request
            payload:
              requestId: req-repl-sessionlog-structured-001
              method: client.SessionLog.SubmitAsync
              params:
                sessionLog:
                  sourceType: ClaudeCode
                  sessionId: ClaudeCode-20260610T154039Z-structured-binding
                  title: Scratch repro session
                  model: claude-fable-5
                  status: in_progress
                  turnCount: 1
                  turns:
                    - requestId: req-20260610T154039Z-scratch-turn-one
                      queryTitle: Repro turn
                      queryText: Structured fields repro
                      response: testing structured fields
                      interpretation: structured DTO sub-field repro
                      status: in_progress
                      tokenCount: 123
                      tags:
                        - repro
                      contextList:
                        - src/McpServer.Repl.Core/GenericClientPassthrough.cs
                      actions:
                        - order: 1
                          description: repro action
                          type: edit
                          status: completed
                          filePath: src/test.cs

            """;

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await protocol.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.True(output.Contains("type: result", StringComparison.Ordinal), output);
        using var document = JsonDocument.Parse(handler.LastRequestBody);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("turnCount").GetInt32());
        var turn = root.GetProperty("turns")[0];
        Assert.Equal("structured DTO sub-field repro", turn.GetProperty("interpretation").GetString());
        Assert.Equal(123, turn.GetProperty("tokenCount").GetInt32());
        Assert.Equal("repro", turn.GetProperty("tags")[0].GetString());
        Assert.Equal("src/McpServer.Repl.Core/GenericClientPassthrough.cs", turn.GetProperty("contextList")[0].GetString());
        var action = turn.GetProperty("actions")[0];
        Assert.Equal(1, action.GetProperty("order").GetInt32());
        Assert.Equal("repro action", action.GetProperty("description").GetString());
        Assert.Equal("src/test.cs", action.GetProperty("filePath").GetString());
    }

    /// <summary>
    /// TEST-MCP-REPL-011: Generic client passthrough emits results from real async
    /// client methods whose runtime task type derives from <see cref="Task{TResult}"/>.
    /// </summary>
    [Fact]
    public async Task ClientTriageQueryRunsAsync_AsyncTaskSubclass_EmitsResultPayload()
    {
        var handler = new CapturingHttpHandler(JsonSerializer.Serialize(new TriageRunQueryResult
        {
            Items =
            [
                new TriageResearchRunDetail
                {
                    RunId = "triage-run-stdout",
                    GroupId = "triage-group-stdout",
                    Status = "completed",
                    AgentStdout = "agent stdout",
                    AgentStderr = "agent stderr",
                    AgentExitCode = 0,
                },
            ],
            TotalCount = 1,
        }));
        using var http = new HttpClient(handler);
        var client = new McpServerClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer"
        });
        var passthrough = new GenericClientPassthrough(client);
        var dispatcher = new ReplCommandDispatcher(passthrough);
        var protocol = new AgentStdioProtocol(new YamlSerializer(), dispatcher);
        var input = JsonSerializer.Serialize(new
        {
            type = "request",
            payload = new
            {
                requestId = "req-triage-runs-001",
                method = "client.Triage.QueryRunsAsync",
                @params = new
                {
                    groupId = "triage-group-stdout",
                    workspacePath = @"F:\GitHub\McpServer",
                },
            },
        });

        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        await protocol.RunAsync(reader, writer, CancellationToken.None);

        var output = writer.ToString();
        Assert.True(output.Contains("type: result", StringComparison.Ordinal), output);
        Assert.Contains("triage-run-stdout", output, StringComparison.Ordinal);
        Assert.Contains("agent stdout", output, StringComparison.Ordinal);
        Assert.Contains("agent stderr", output, StringComparison.Ordinal);
        Assert.Contains("agentExitCode: 0", output, StringComparison.Ordinal);
    }

    private sealed class CapturingHttpHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public CapturingHttpHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
