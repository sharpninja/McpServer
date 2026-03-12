using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

public sealed class GraphRagControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GraphRagControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    [Fact]
    public async Task Status_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/graphrag/status", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("enabled", out _));
        Assert.True(doc.RootElement.TryGetProperty("graphRoot", out _));
        Assert.True(doc.RootElement.TryGetProperty("indexCorpus", out _));
        Assert.True(doc.RootElement.TryGetProperty("queryCorpus", out _));
        Assert.True(doc.RootElement.TryGetProperty("inputPath", out _));
        Assert.True(doc.RootElement.TryGetProperty("inputDocumentCount", out _));
    }

    [Fact]
    public async Task Index_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/graphrag/index", UriKind.Relative), new { force = true }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("isIndexed", out var isIndexed));
        Assert.True(isIndexed.GetBoolean());
    }

    [Fact]
    public async Task Query_WithoutQuery_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/graphrag/query", UriKind.Relative), new { }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Query_WithInvalidMaxChunks_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/graphrag/query", UriKind.Relative), new
        {
            query = "auth",
            maxChunks = 0
        }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Query_ReturnsCorpusDiagnostics()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/graphrag/query", UriKind.Relative), new
        {
            query = "auth",
            maxChunks = 5
        }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("queryCorpus", out _));
        Assert.True(doc.RootElement.TryGetProperty("visibilityNote", out _));
    }
}
