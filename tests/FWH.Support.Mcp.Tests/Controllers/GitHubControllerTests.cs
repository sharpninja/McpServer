using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FWH.Support.Mcp.Tests.Controllers;

/// <summary>TR-PLANNED-013, TR-GH-013-006: GitHub controller API tests. Validates gh CLI integration and request validation.</summary>
public sealed class GitHubControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GitHubControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>GET /mcp/gh/issues returns 200 and body with issues array (or error when gh not available).</summary>
    [Fact]
    public async Task ListIssues_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcp/gh/issues?limit=5", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("issues", out var issues));
        Assert.Equal(JsonValueKind.Array, issues.ValueKind);
    }

    /// <summary>GET /mcp/gh/pulls returns 200 and body with pulls array (or error when gh not available).</summary>
    [Fact]
    public async Task ListPulls_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcp/gh/pulls?limit=5", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("pulls", out var pulls));
        Assert.Equal(JsonValueKind.Array, pulls.ValueKind);
    }

    /// <summary>POST /mcp/gh/issues without title returns 400.</summary>
    [Fact]
    public async Task CreateIssue_WithoutTitle_ReturnsBadRequest()
    {
        var request = new { title = "", body = (string?)null };
        var response = await _client.PostAsJsonAsync(new Uri("/mcp/gh/issues", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcp/gh/issues with null body (missing title) returns 400.</summary>
    [Fact]
    public async Task CreateIssue_NullBody_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcp/gh/issues", UriKind.Relative), (object?)null).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcp/gh/issues/{id}/comments without body returns 400.</summary>
    [Fact]
    public async Task CommentOnIssue_WithoutBody_ReturnsBadRequest()
    {
        var request = new { body = "" };
        var response = await _client.PostAsJsonAsync(new Uri("/mcp/gh/issues/1/comments", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcp/gh/pulls/{id}/comments without body returns 400.</summary>
    [Fact]
    public async Task CommentOnPull_WithoutBody_ReturnsBadRequest()
    {
        var request = new { body = "" };
        var response = await _client.PostAsJsonAsync(new Uri("/mcp/gh/pulls/1/comments", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>GET /mcp/gh/labels returns 200 and body with labels array.</summary>
    [Fact]
    public async Task ListLabels_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcp/gh/labels", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("labels", out var labels));
        Assert.Equal(JsonValueKind.Array, labels.ValueKind);
    }

    /// <summary>PUT /mcp/gh/issues/{number} with null body returns 400.</summary>
    [Fact]
    public async Task UpdateIssue_NullBody_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync(new Uri("/mcp/gh/issues/1", UriKind.Relative), (object?)null).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcp/gh/issues/sync/from-github returns 200.</summary>
    [Fact]
    public async Task SyncFromGitHub_ReturnsOk()
    {
        var response = await _client.PostAsync(new Uri("/mcp/gh/issues/sync/from-github?limit=5", UriKind.Relative), null).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>POST /mcp/gh/issues/sync/to-github returns 200.</summary>
    [Fact]
    public async Task SyncToGitHub_ReturnsOk()
    {
        var response = await _client.PostAsync(new Uri("/mcp/gh/issues/sync/to-github", UriKind.Relative), null).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
