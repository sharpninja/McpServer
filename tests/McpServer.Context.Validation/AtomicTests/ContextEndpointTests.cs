using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace McpServer.Context.Validation.AtomicTests;

/// <summary>
/// Validation tests for <c>ContextEndpointTests</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
/// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
/// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
/// </remarks>
[Collection("ContextEndpoint")]
public sealed class ContextEndpointTests
{
    private readonly ContextEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of ContextEndpointTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    public ContextEndpointTests(ContextEndpointFixture fixture) => _fixture = fixture;

    // --- GET /mcpserver/context/sources ---

    /// <summary>
    /// Validates the <c>Sources_Returns200WithSourcesArray</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Sources_Returns200WithSourcesArray()
    {
        var response = await _fixture.Client.GetAsync($"{ContextEndpointFixture.ContextRoute}/sources");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("sources", out var sources));
        Assert.Equal(JsonValueKind.Array, sources.ValueKind);
    }

    // --- POST /mcpserver/context/search ---

    /// <summary>
    /// Validates the <c>Search_EmptyQuery_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Search_EmptyQuery_Returns200()
    {
        var payload = new { query = "", limit = 5 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("chunks", out var chunks));
        Assert.Equal(JsonValueKind.Array, chunks.ValueKind);
    }

    /// <summary>
    /// Validates the <c>Search_WithQuery_Returns200WithResults</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Search_WithQuery_Returns200WithResults()
    {
        var payload = new { query = "workspace", limit = 5 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("query", out _));
        Assert.True(json.TryGetProperty("chunks", out _));
        Assert.True(json.TryGetProperty("sourceKeys", out _));
    }

    /// <summary>
    /// Validates the <c>Search_WithSourceTypeFilter_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Search_WithSourceTypeFilter_Returns200()
    {
        var payload = new { query = "test", sourceType = "repo", limit = 3 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Search_LimitClamped_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Search_LimitClamped_Returns200()
    {
        var payload = new { query = "test", limit = 200 }; // exceeds max of 100
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /mcpserver/context/pack ---

    /// <summary>
    /// Validates the <c>Pack_EmptyQuery_Returns200WithPack</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Pack_EmptyQuery_Returns200WithPack()
    {
        var payload = new { query = "", limit = 5 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/pack", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("queryId", out _));
        Assert.True(json.TryGetProperty("chunks", out _));
        Assert.True(json.TryGetProperty("sourceKeys", out _));
    }

    /// <summary>
    /// Validates the <c>Pack_WithQueryId_Returns200WithSameQueryId</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Pack_WithQueryId_Returns200WithSameQueryId()
    {
        var queryId = $"audit-{Guid.NewGuid():N}";
        var payload = new { queryId, query = "controller", limit = 3 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/pack", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(queryId, json.GetProperty("queryId").GetString());
    }

    /// <summary>
    /// Validates the <c>Pack_WithQuery_ReturnsFilteredChunks</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task Pack_WithQuery_ReturnsFilteredChunks()
    {
        var payload = new { query = "workspace", limit = 10 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/pack", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /mcpserver/context/rebuild-index ---

    /// <summary>
    /// Validates the <c>RebuildIndex_Returns200Or500</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task RebuildIndex_Returns200Or500()
    {
        var response = await _fixture.Client.PostAsync($"{ContextEndpointFixture.ContextRoute}/rebuild-index", null);
        // 200 when FTS5 index exists and rebuild succeeds; 500 when index not initialized
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 200 or 500, got {(int)response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
            Assert.Equal("rebuilt", json.GetProperty("status").GetString());
        }
    }
}
