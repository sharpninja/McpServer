using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.SessionLog.Validation.Models;
using Xunit;

namespace McpServer.SessionLog.Validation.AtomicTests;

/// <summary>
/// Validation tests for <c>AppendDialogTests</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
[Collection("SessionLogEndpoint")]
public sealed class AppendDialogTests
{
    private readonly SessionLogEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of AppendDialogTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public AppendDialogTests(SessionLogEndpointFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Validates the <c>AppendDialog_ToExistingEntry_Returns200WithCount</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task AppendDialog_ToExistingEntry_Returns200WithCount()
    {
        // First create a session with an entry
        var sessionId = SessionLogEndpointFixture.GenerateSessionId("DialogTest");
        var requestId = SessionLogEndpointFixture.GenerateRequestId("append-dialog-existing-entry");
        var payload = new
        {
            sourceType = "DialogTest",
            sessionId,
            title = "Dialog append test",
            model = "test-model",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "in_progress",
            entryCount = 1,
            entries = new[]
            {
                new
                {
                    requestId,
                    timestamp = DateTimeOffset.UtcNow.ToString("o"),
                    queryText = "Dialog append base entry",
                    queryTitle = "Dialog test",
                    response = "Pending",
                    status = "in_progress"
                }
            }
        };

        var submitResponse = await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);
        Assert.Equal(HttpStatusCode.Created, submitResponse.StatusCode);

        // Now append dialog items
        var dialogItems = new[]
        {
            new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "model", content = "Analyzing the problem...", category = "reasoning" },
            new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "tool", content = "read_file returned content", category = "tool_result" }
        };

        var dialogRoute = $"{SessionLogEndpointFixture.SessionLogRoute}/DialogTest/{sessionId}/{requestId}/dialog";
        var response = await _fixture.Client.PostAsJsonAsync(dialogRoute, dialogItems);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DialogAppendResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("DialogTest", result!.Agent);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(requestId, result.RequestId);
        Assert.Equal(2, result.TotalDialogCount);
    }

    /// <summary>
    /// Validates the <c>AppendDialog_MultipleAppends_AccumulatesCount</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task AppendDialog_MultipleAppends_AccumulatesCount()
    {
        var sessionId = SessionLogEndpointFixture.GenerateSessionId("DialogAccumTest");
        var requestId = SessionLogEndpointFixture.GenerateRequestId("append-dialog-accumulates");
        var payload = new
        {
            sourceType = "DialogAccumTest",
            sessionId,
            title = "Dialog accumulate test",
            model = "test-model",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "in_progress",
            entryCount = 1,
            entries = new[]
            {
                new
                {
                    requestId,
                    timestamp = DateTimeOffset.UtcNow.ToString("o"),
                    queryText = "Accumulate dialog test",
                    status = "in_progress"
                }
            }
        };
        await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);

        var dialogRoute = $"{SessionLogEndpointFixture.SessionLogRoute}/DialogAccumTest/{sessionId}/{requestId}/dialog";

        // First append
        var items1 = new[] { new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "model", content = "Step 1", category = "reasoning" } };
        var r1 = await _fixture.Client.PostAsJsonAsync(dialogRoute, items1);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var res1 = await r1.Content.ReadFromJsonAsync<DialogAppendResult>(JsonOpts);
        Assert.Equal(1, res1!.TotalDialogCount);

        // Second append
        var items2 = new[] { new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "tool", content = "Step 2", category = "tool_call" } };
        var r2 = await _fixture.Client.PostAsJsonAsync(dialogRoute, items2);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var res2 = await r2.Content.ReadFromJsonAsync<DialogAppendResult>(JsonOpts);
        Assert.Equal(2, res2!.TotalDialogCount);
    }

    /// <summary>
    /// Validates the <c>AppendDialog_NonExistentSession_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task AppendDialog_NonExistentSession_Returns404()
    {
        var sessionId = SessionLogEndpointFixture.GenerateSessionId("NoSuchAgent", "missing-session");
        var requestId = SessionLogEndpointFixture.GenerateRequestId("missing-request");
        var dialogRoute = $"{SessionLogEndpointFixture.SessionLogRoute}/NoSuchAgent/{sessionId}/{requestId}/dialog";
        var items = new[] { new { timestamp = DateTimeOffset.UtcNow.ToString("o"), role = "model", content = "test", category = "reasoning" } };
        var response = await _fixture.Client.PostAsJsonAsync(dialogRoute, items);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
