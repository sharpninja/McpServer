using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.SessionLog.Validation.Models;
using Xunit;

namespace McpServer.SessionLog.Validation.AtomicTests;

/// <summary>
/// Validation tests for <c>QuerySessionLogTests</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
[Collection("SessionLogEndpoint")]
public sealed class QuerySessionLogTests
{
    private readonly SessionLogEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of QuerySessionLogTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public QuerySessionLogTests(SessionLogEndpointFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Validates the <c>Query_NoFilters_Returns200WithResults</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Query_NoFilters_Returns200WithResults()
    {
        var response = await _fixture.Client.GetAsync(SessionLogEndpointFixture.SessionLogRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 0);
        Assert.NotNull(result.Items);
    }

    /// <summary>
    /// Validates the <c>Query_FilterByAgent_Returns200Filtered</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Query_FilterByAgent_Returns200Filtered()
    {
        // First submit a session with known agent
        var sessionId = SessionLogEndpointFixture.GenerateSessionId();
        var payload = new
        {
            sourceType = "QueryAgentTest",
            sessionId,
            title = "Agent filter test",
            model = "test-model",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            entryCount = 0
        };
        await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);

        // Query by agent
        var response = await _fixture.Client.GetAsync($"{SessionLogEndpointFixture.SessionLogRoute}?agent=QueryAgentTest");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
        Assert.All(result.Items!, s => Assert.Equal("QueryAgentTest", s.SourceType));
    }

    /// <summary>
    /// Validates the <c>Query_FilterByModel_Returns200Filtered</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Query_FilterByModel_Returns200Filtered()
    {
        var sessionId = SessionLogEndpointFixture.GenerateSessionId();
        var uniqueModel = $"audit-model-{Guid.NewGuid():N}";
        var payload = new
        {
            sourceType = "AuditTest",
            sessionId,
            title = "Model filter test",
            model = uniqueModel,
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            entryCount = 0
        };
        await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);

        var response = await _fixture.Client.GetAsync($"{SessionLogEndpointFixture.SessionLogRoute}?model={uniqueModel}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 1);
    }

    /// <summary>
    /// Validates the <c>Query_FilterByDateRange_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Query_FilterByDateRange_Returns200()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-1).ToString("o");
        var to = DateTimeOffset.UtcNow.AddDays(1).ToString("o");
        var response = await _fixture.Client.GetAsync(
            $"{SessionLogEndpointFixture.SessionLogRoute}?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(JsonOpts);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Validates the <c>Query_WithPagination_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Query_WithPagination_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{SessionLogEndpointFixture.SessionLogRoute}?limit=2&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.True(result.Items!.Count <= 2);
    }

    /// <summary>
    /// Validates the <c>Query_NonMatchingAgent_ReturnsEmptyResults</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Query_NonMatchingAgent_ReturnsEmptyResults()
    {
        var response = await _fixture.Client.GetAsync(
            $"{SessionLogEndpointFixture.SessionLogRoute}?agent=NonExistentAgent_{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal(0, result!.TotalCount);
    }

    /// <summary>
    /// Validates the <c>Query_FilterByText_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    [Fact]
    public async Task Query_FilterByText_Returns200()
    {
        // Submit a session with unique text
        var uniqueText = $"unique-search-text-{Guid.NewGuid():N}";
        var sessionId = SessionLogEndpointFixture.GenerateSessionId();
        var payload = new
        {
            sourceType = "AuditTest",
            sessionId,
            title = uniqueText,
            model = "test-model",
            started = DateTimeOffset.UtcNow.ToString("o"),
            lastUpdated = DateTimeOffset.UtcNow.ToString("o"),
            status = "completed",
            entryCount = 0
        };
        await _fixture.Client.PostAsJsonAsync(SessionLogEndpointFixture.SessionLogRoute, payload);

        var response = await _fixture.Client.GetAsync(
            $"{SessionLogEndpointFixture.SessionLogRoute}?text={Uri.EscapeDataString(uniqueText)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QueryResult>(JsonOpts);
        Assert.NotNull(result);
        // Text search may use FTS5 — at least verify 200 OK
    }
}
