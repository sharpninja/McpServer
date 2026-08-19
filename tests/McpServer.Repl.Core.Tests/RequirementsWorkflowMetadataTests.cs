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

    /// <summary>TEST-MCP-TRIAGEREQ-001: list-shaped legacy TR ids are get-able.</summary>
    [Fact]
    public async Task GetTrAsync_LegacyId_DoesNotRejectCanonicalFormat()
    {
        var handler = new CapturingHttpHandler("""{"id":"TR-066","title":"Legacy","body":"Body"}""");
        using var http = new HttpClient(handler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));

        var item = await workflow.GetTrAsync("TR-066", TestContext.Current.CancellationToken);

        Assert.Equal("TR-066", item.Id);
        Assert.Contains("/tr/TR-066", handler.LastRequestUri, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TEST-MCP-TRIAGEREQ-001: update of a listed legacy TR id is accepted.</summary>
    [Fact]
    public async Task UpdateTrAsync_LegacyId_DoesNotRejectCanonicalFormat()
    {
        var handler = new CapturingHttpHandler("""{"id":"TR-066","title":"Updated","body":"Body"}""");
        using var http = new HttpClient(handler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));
        var request = Substitute.For<ITrUpdateRequest>();
        request.Id.Returns("TR-066");
        request.Title.Returns("Updated");
        request.Description.Returns("Body");

        var result = await workflow.UpdateTrAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("TR-066", result.Item.Id);
        Assert.Contains("/tr/TR-066", handler.LastRequestUri, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TEST-MCP-TRIAGEREQ-001: delete of a listed legacy TR id is accepted.</summary>
    [Fact]
    public async Task DeleteTrAsync_LegacyId_DoesNotRejectCanonicalFormat()
    {
        var handler = new CapturingHttpHandler("""{"ok":true}""");
        using var http = new HttpClient(handler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));

        await workflow.DeleteTrAsync("TR-066", TestContext.Current.CancellationToken);

        Assert.Contains("/tr/TR-066", handler.LastRequestUri, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TEST-MCP-TRIAGEREQ-001: create still rejects non-canonical TR ids.</summary>
    [Fact]
    public async Task CreateTrAsync_LegacyId_StillRejected()
    {
        var handler = new CapturingHttpHandler("""{"id":"TR-066"}""");
        using var http = new HttpClient(handler);
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));
        var request = Substitute.For<ITrCreateRequest>();
        request.Id.Returns("TR-066");
        request.Title.Returns("Legacy");
        request.Description.Returns("Body");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            workflow.CreateTrAsync(request, TestContext.Current.CancellationToken));
    }

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

        var result = await workflow.UpdateFrAsync(request, cancellationToken: TestContext.Current.CancellationToken);

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

        var trResult = await workflow.UpdateTrAsync(trRequest, cancellationToken: TestContext.Current.CancellationToken);

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

        var testResult = await testWorkflow.UpdateTestAsync(testRequest, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("\"priority\":\"high\"", testHandler.LastRequestBody);
        Assert.Contains("\"status\":\"completed\"", testHandler.LastRequestBody);
        Assert.Contains("\"notes\":\"test metadata\"", testHandler.LastRequestBody);
        Assert.Equal("high", testResult.Item.Priority);
        Assert.Equal("completed", testResult.Item.Status);
        Assert.Equal("test metadata", testResult.Item.Notes);
    }

    [Fact]
    public async Task HierarchicalRequirementIds_AreAcceptedByWorkflowValidators()
    {
        var frHandler = new CapturingHttpHandler("""{"id":"FR-MCP-MEMORY-001","title":"FR","body":"Body","priority":"high","status":"completed"}""");
        using var frHttp = new HttpClient(frHandler);
        var frWorkflow = new RequirementsWorkflow(new RequirementsClient(frHttp, Options));
        var frRequest = Substitute.For<IFrUpdateRequest>();
        frRequest.Id.Returns("FR-MCP-MEMORY-001");
        frRequest.Title.Returns("FR");
        frRequest.Description.Returns("Body");
        frRequest.Priority.Returns("high");
        frRequest.Status.Returns("completed");

        var frResult = await frWorkflow.UpdateFrAsync(frRequest, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("FR-MCP-MEMORY-001", frResult.Item.Id);
        Assert.Contains("\"status\":\"completed\"", frHandler.LastRequestBody);

        var trHandler = new CapturingHttpHandler("""{"id":"TR-MCP-MEMORY-001","title":"TR","body":"Body","priority":"high","status":"completed"}""");
        using var trHttp = new HttpClient(trHandler);
        var trWorkflow = new RequirementsWorkflow(new RequirementsClient(trHttp, Options));
        var trRequest = Substitute.For<ITrUpdateRequest>();
        trRequest.Id.Returns("TR-MCP-MEMORY-001");
        trRequest.Title.Returns("TR");
        trRequest.Description.Returns("Body");
        trRequest.Priority.Returns("high");
        trRequest.Status.Returns("completed");

        var trResult = await trWorkflow.UpdateTrAsync(trRequest, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("TR-MCP-MEMORY-001", trResult.Item.Id);
        Assert.Contains("\"status\":\"completed\"", trHandler.LastRequestBody);

        var testHandler = new CapturingHttpHandler("""{"id":"TEST-MCP-MEMORY-001","title":"TEST","condition":"Condition","priority":"high","status":"completed"}""");
        using var testHttp = new HttpClient(testHandler);
        var testWorkflow = new RequirementsWorkflow(new RequirementsClient(testHttp, Options));
        var testRequest = Substitute.For<ITestUpdateRequest>();
        testRequest.Id.Returns("TEST-MCP-MEMORY-001");
        testRequest.Title.Returns("TEST");
        testRequest.Description.Returns("Condition");
        testRequest.Priority.Returns("high");
        testRequest.Status.Returns("completed");

        var testResult = await testWorkflow.UpdateTestAsync(testRequest, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("TEST-MCP-MEMORY-001", testResult.Item.Id);
        Assert.Contains("\"status\":\"completed\"", testHandler.LastRequestBody);
    }

    [Fact]
    public async Task CreateMappingAsync_AllowsLegacyStoredIdsThroughMappingWorkflow()
    {
        using var http = new HttpClient(new LegacyMappingHttpHandler());
        var workflow = new RequirementsWorkflow(new RequirementsClient(http, Options));
        var request = Substitute.For<IMappingCreateRequest>();
        request.FrId.Returns("FR-MCP-LIVE-CODEX-20260603T2014Z");
        request.TrIds.Returns(["TR-MCP-MT-003A"]);
        request.TrId.Returns((string?)null);
        request.TestIds.Returns(["TEST-SUPPORT-010B-1"]);
        request.TestId.Returns((string?)null);
        request.Notes.Returns("legacy mapping");

        var result = await workflow.CreateMappingAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(result.Item);
        Assert.Equal("FR-MCP-LIVE-CODEX-20260603T2014Z", result.Item.FrId);
        Assert.Equal("TR-MCP-MT-003A", result.Item.TrId);
        Assert.Equal("TEST-SUPPORT-010B-1", result.Item.TestId);
    }

    private sealed class LegacyMappingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (request.Method == HttpMethod.Get && path.Contains("/mcpserver/requirements/fr/FR-MCP-LIVE-CODEX-20260603T2014Z", StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.OK, """{"id":"FR-MCP-LIVE-CODEX-20260603T2014Z","title":"Legacy FR","body":"Legacy FR"}"""));
            if (request.Method == HttpMethod.Get && path.Contains("/mcpserver/requirements/tr/TR-MCP-MT-003A", StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.OK, """{"id":"TR-MCP-MT-003A","title":"Legacy TR","body":"Legacy TR"}"""));
            if (request.Method == HttpMethod.Get && path.Contains("/mcpserver/requirements/test/TEST-SUPPORT-010B-1", StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.OK, """{"id":"TEST-SUPPORT-010B-1","title":"Legacy TEST","condition":"Legacy TEST"}"""));
            if (request.Method == HttpMethod.Get && path.Contains("/mcpserver/requirements/mapping/FR-MCP-LIVE-CODEX-20260603T2014Z", StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.NotFound, """{"error":"not found"}"""));
            if (request.Method == HttpMethod.Put && path.Contains("/mcpserver/requirements/mapping/FR-MCP-LIVE-CODEX-20260603T2014Z", StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.OK, """{"frId":"FR-MCP-LIVE-CODEX-20260603T2014Z","trIds":["TR-MCP-MT-003A"],"testIds":["TEST-SUPPORT-010B-1"]}"""));

            return Task.FromResult(Json(HttpStatusCode.BadRequest, """{"error":"unexpected request"}"""));
        }

        private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private sealed class CapturingHttpHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public CapturingHttpHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public string LastRequestBody { get; private set; } = string.Empty;

        public string LastRequestUri { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString() ?? string.Empty;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
