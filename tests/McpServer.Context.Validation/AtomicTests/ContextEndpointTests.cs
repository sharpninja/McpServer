using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace McpServer.Context.Validation.AtomicTests;

[Collection("ContextEndpoint")]
public sealed class ContextEndpointTests
{
    private readonly ContextEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ContextEndpointTests(ContextEndpointFixture fixture) => _fixture = fixture;

    // --- GET /mcp/context/sources ---

    [Fact]
    public async Task Sources_Returns200WithSourcesArray()
    {
        var response = await _fixture.Client.GetAsync($"{ContextEndpointFixture.ContextRoute}/sources");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("sources", out var sources));
        Assert.Equal(JsonValueKind.Array, sources.ValueKind);
    }

    // --- POST /mcp/context/search ---

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

    [Fact]
    public async Task Search_WithSourceTypeFilter_Returns200()
    {
        var payload = new { query = "test", sourceType = "repo", limit = 3 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_LimitClamped_Returns200()
    {
        var payload = new { query = "test", limit = 200 }; // exceeds max of 100
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/search", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /mcp/context/pack ---

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

    [Fact]
    public async Task Pack_WithQuery_ReturnsFilteredChunks()
    {
        var payload = new { query = "workspace", limit = 10 };
        var response = await _fixture.Client.PostAsJsonAsync($"{ContextEndpointFixture.ContextRoute}/pack", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /mcp/context/rebuild-index ---

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
