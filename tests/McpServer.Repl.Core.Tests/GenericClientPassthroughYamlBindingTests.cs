using System.Net;
using System.Text;
using McpServer.Client;

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
