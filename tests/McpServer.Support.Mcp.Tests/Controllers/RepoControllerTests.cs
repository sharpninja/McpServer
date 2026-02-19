using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>TR-PLANNED-013: Repo controller API tests (path allowlist, read/list/write).</summary>
public sealed class RepoControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RepoControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>GET /mcp/repo/list returns 200 and entries array.</summary>
    [Fact]
    public async Task List_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcp/repo/list", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("\"path\"", json, StringComparison.Ordinal);
        Assert.Contains("\"entries\"", json, StringComparison.Ordinal);
    }

    /// <summary>GET /mcp/repo/file without path returns 400.</summary>
    [Fact]
    public async Task ReadFile_WithoutPath_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(new Uri("/mcp/repo/file", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcp/repo/file without body returns 400.</summary>
    [Fact]
    public async Task WriteFile_WithoutPath_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcp/repo/file", UriKind.Relative), new { }).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
