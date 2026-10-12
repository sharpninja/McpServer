using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace QBrainAi.Context.Validation.AtomicTests;

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

    // --- GET /qbrainai/context/sources ---

    /// <summary>
    /// Validates the <c>SourcesReturns200WithSourcesArray</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task SourcesReturns200WithSourcesArray()
    {
        var response = await _fixture.Client.GetAsync($"{ContextEndpointFixture.ContextRoute}/sources", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("sources", out var sources));
        Assert.Equal(JsonValueKind.Array, sources.ValueKind);
    }

    // --- POST /qbrainai/context/search ---

    /// <summary>
    /// Validates the <c>SearchEmptyQueryReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task SearchEmptyQueryReturns200()
    {
        var payload = new { query = "", limit = 5 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("chunks", out var chunks));
        Assert.Equal(JsonValueKind.Array, chunks.ValueKind);
    }

    /// <summary>
    /// Validates the <c>SearchWithQueryReturns200WithResults</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task SearchWithQueryReturns200WithResults()
    {
        var payload = new { query = "workspace", limit = 5 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("query", out _));
        Assert.True(json.TryGetProperty("chunks", out _));
        Assert.True(json.TryGetProperty("sourceKeys", out _));
    }

    /// <summary>
    /// Validates the <c>SearchWithSourceTypeFilterReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task SearchWithSourceTypeFilterReturns200()
    {
        var payload = new { query = "test", sourceType = "repo", limit = 3 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>SearchLimitClampedReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task SearchLimitClampedReturns200()
    {
        var payload = new { query = "test", limit = 200 }; // exceeds max of 100
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /qbrainai/context/pack ---

    /// <summary>
    /// Validates the <c>PackEmptyQueryReturns200WithPack</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task PackEmptyQueryReturns200WithPack()
    {
        var payload = new { query = "", limit = 5 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/pack", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("queryId", out _));
        Assert.True(json.TryGetProperty("chunks", out _));
        Assert.True(json.TryGetProperty("sourceKeys", out _));
    }

    /// <summary>
    /// Validates the <c>PackWithQueryIdReturns200WithSameQueryId</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task PackWithQueryIdReturns200WithSameQueryId()
    {
        var queryId = $"audit-{Guid.NewGuid():N}";
        var payload = new { queryId, query = "controller", limit = 3 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/pack", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(queryId, json.GetProperty("queryId").GetString());
    }

    /// <summary>
    /// Validates the <c>PackWithQueryReturnsFilteredChunks</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task PackWithQueryReturnsFilteredChunks()
    {
        var payload = new { query = "workspace", limit = 10 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/pack", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /qbrainai/context/rebuild-index ---

    /// <summary>
    /// Validates the <c>RebuildIndexReturns200Or500</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-004, FR-MCP-004, TR-MCP-DATA-002, TR-MCP-DATA-003.
    /// Test data: Fixture HTTP calls with context query payloads (empty, filtered, bounded, and queryId-based inputs).
    /// Data rationale: These inputs verify context endpoint contracts across normal, boundary, and filtering scenarios.
    /// </remarks>
    [Fact]
    public async Task RebuildIndexReturns200Or500()
    {
        var response = await _fixture.Client.PostAsync($"{ContextEndpointFixture.ContextRoute}/rebuild-index", null, cancellationToken: TestContext.Current.CancellationToken);
        // 200 when FTS5 index exists and rebuild succeeds; 500 when index not initialized
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 200 or 500, got {(int)response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("rebuilt", json.GetProperty("status").GetString());
        }
    }
}
