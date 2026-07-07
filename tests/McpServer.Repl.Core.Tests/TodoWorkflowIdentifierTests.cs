using System.Net;
using McpServer.Client;
using McpServer.Repl.Core;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Regression coverage for TODO identifier validation performed by the production REPL TODO workflow.
/// </summary>
public sealed class TodoWorkflowIdentifierTests
{
    /// <summary>
    /// Verifies that import-compatible uppercase/digit kebab IDs pass REPL workflow validation.
    /// </summary>
    [Theory]
    [InlineData("PHASE0-REMOTE-001")]
    [InlineData("MCP-TODO-CREATE-001")]
    public async Task GetAsync_ValidImportCompatibleId_DelegatesToClient(string id)
    {
        var handler = new JsonHandler($$"""
            {"id":"{{id}}","title":"Import-compatible TODO","section":"Backlog","priority":"high","done":false}
            """);
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key"
        });
        var sut = new TodoWorkflow(client);

        var item = await sut.GetAsync(id, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(id, item.Id);
        Assert.NotNull(handler.LastRequest);
        Assert.EndsWith($"/mcpserver/todo/{id}", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    /// <summary>
    /// Verifies that missing descriptive segments still fail before transport.
    /// </summary>
    [Fact]
    public async Task GetAsync_TwoSegmentId_ThrowsBeforeHttp()
    {
        var handler = new JsonHandler("""{"id":"MCP-001","title":"Invalid","section":"Backlog","priority":"high","done":false}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key"
        });
        var sut = new TodoWorkflow(client);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await sut.GetAsync("MCP-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        Assert.Contains("Invalid TODO ID format", exception.Message, StringComparison.Ordinal);
        Assert.Null(handler.LastRequest);
    }

    private sealed class JsonHandler : HttpMessageHandler
    {
        private readonly string _json;

        public JsonHandler(string json)
        {
            _json = json;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json)
            });
        }
    }
}
