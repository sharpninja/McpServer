using System.Net;
using System.Text;
using McpServer.Client;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>Regression tests for requirement metadata propagation through the production workflow.</summary>
public sealed class RequirementsWorkflowMetadataTests
{
    private static readonly McpServerClientOptions Options = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async Task UpdateFrAsync_SendsAndReturnsPriorityStatusAndNotes()
    {
        var handler = new CapturingHttpHandler("""{"id":"FR-MCP-901","title":"FR","body":"Body","priority":"high","status":"in_progress","notes":"metadata"}""");
        using var http = new HttpClient(handler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));
        var request = Substitute.For<IFrUpdateRequest>();
        request.Id.Returns("FR-MCP-901");
        request.Title.Returns("FR");
        request.Description.Returns("Body");
        request.Priority.Returns("high");
        request.Status.Returns("in_progress");
        request.Notes.Returns("metadata");

        var result = await workflow.UpdateFrAsync(request);

        Assert.Contains("\"priority\":\"high\"", handler.LastRequestBody);
        Assert.Contains("\"status\":\"in_progress\"", handler.LastRequestBody);
        Assert.Contains("\"notes\":\"metadata\"", handler.LastRequestBody);
        Assert.Equal("high", result.Item.Priority);
        Assert.Equal("in_progress", result.Item.Status);
        Assert.Equal("metadata", result.Item.Notes);
    }

    [Fact]
    public async Task UpdateTrAndTestAsync_SendAndReturnPriorityStatusAndNotes()
    {
        var trHandler = new CapturingHttpHandler("""{"id":"TR-MCP-REQ-901","title":"TR","body":"Body","priority":"high","status":"completed","notes":"tr metadata"}""");
        using var trHttp = new HttpClient(trHandler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(trHttp, Options));
        var trRequest = Substitute.For<ITrUpdateRequest>();
        trRequest.Id.Returns("TR-MCP-REQ-901");
        trRequest.Title.Returns("TR");
        trRequest.Description.Returns("Body");
        trRequest.Priority.Returns("high");
        trRequest.Status.Returns("completed");
        trRequest.Notes.Returns("tr metadata");

        var trResult = await workflow.UpdateTrAsync(trRequest);

        Assert.Contains("\"priority\":\"high\"", trHandler.LastRequestBody);
        Assert.Contains("\"status\":\"completed\"", trHandler.LastRequestBody);
        Assert.Contains("\"notes\":\"tr metadata\"", trHandler.LastRequestBody);
        Assert.Equal("high", trResult.Item.Priority);
        Assert.Equal("completed", trResult.Item.Status);
        Assert.Equal("tr metadata", trResult.Item.Notes);

        var testHandler = new CapturingHttpHandler("""{"id":"TEST-MCP-901","title":"TEST","condition":"Condition","priority":"high","status":"completed","notes":"test metadata"}""");
        using var testHttp = new HttpClient(testHandler);
        var testWorkflow = new RequirementsWorkflow(new RequirementsClient(testHttp, Options));
        var testRequest = Substitute.For<ITestUpdateRequest>();
        testRequest.Id.Returns("TEST-MCP-901");
        testRequest.Title.Returns("TEST");
        testRequest.Description.Returns("Condition");
        testRequest.Priority.Returns("high");
        testRequest.Status.Returns("completed");
        testRequest.Notes.Returns("test metadata");

        var testResult = await testWorkflow.UpdateTestAsync(testRequest);

        Assert.Contains("\"priority\":\"high\"", testHandler.LastRequestBody);
        Assert.Contains("\"status\":\"completed\"", testHandler.LastRequestBody);
        Assert.Contains("\"notes\":\"test metadata\"", testHandler.LastRequestBody);
        Assert.Equal("high", testResult.Item.Priority);
        Assert.Equal("completed", testResult.Item.Status);
        Assert.Equal("test metadata", testResult.Item.Notes);
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
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
