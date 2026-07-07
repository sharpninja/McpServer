using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>TR-PLANNED-CORE-013, TR-GH-013-006: GitHub controller API tests. Validates gh CLI integration and request validation.</summary>
[Trait("Category", "Integration")]
public sealed class GitHubControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GitHubControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    /// <summary>GET /mcpserver/gh/issues returns 200 and body with issues array (or error when gh not available).</summary>
    [Fact]
    public async Task ListIssues_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/gh/issues?limit=5", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("issues", out var issues));
        Assert.Equal(JsonValueKind.Array, issues.ValueKind);
    }

    /// <summary>GET /mcpserver/gh/pulls returns 200 and body with pulls array (or error when gh not available).</summary>
    [Fact]
    public async Task ListPulls_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/gh/pulls?limit=5", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("pulls", out var pulls));
        Assert.Equal(JsonValueKind.Array, pulls.ValueKind);
    }

    /// <summary>POST /mcpserver/gh/issues without title returns 400.</summary>
    [Fact]
    public async Task CreateIssue_WithoutTitle_ReturnsBadRequest()
    {
        var request = new { title = "", body = (string?)null };
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/gh/issues", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcpserver/gh/issues with null body (missing title) returns 400.</summary>
    [Fact]
    public async Task CreateIssue_NullBody_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/gh/issues", UriKind.Relative), (object?)null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcpserver/gh/issues/{id}/comments without body returns 400.</summary>
    [Fact]
    public async Task CommentOnIssue_WithoutBody_ReturnsBadRequest()
    {
        var request = new { body = "" };
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/gh/issues/1/comments", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcpserver/gh/pulls/{id}/comments without body returns 400.</summary>
    [Fact]
    public async Task CommentOnPull_WithoutBody_ReturnsBadRequest()
    {
        var request = new { body = "" };
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/gh/pulls/1/comments", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>GET /mcpserver/gh/labels returns 200 and body with labels array.</summary>
    [Fact]
    public async Task ListLabels_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/gh/labels", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("labels", out var labels));
        Assert.Equal(JsonValueKind.Array, labels.ValueKind);
    }

    /// <summary>PUT /mcpserver/gh/issues/{number} with null body returns 400.</summary>
    [Fact]
    public async Task UpdateIssue_NullBody_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync(new Uri("/mcpserver/gh/issues/1", UriKind.Relative), (object?)null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>POST /mcpserver/gh/issues/sync/from-github returns 200.</summary>
    [Fact]
    public async Task SyncFromGitHub_ReturnsOk()
    {
        var response = await _client.PostAsync(new Uri("/mcpserver/gh/issues/sync/from-github?limit=5", UriKind.Relative), null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>POST /mcpserver/gh/issues/sync/to-github returns 200.</summary>
    [Fact]
    public async Task SyncToGitHub_ReturnsOk()
    {
        var response = await _client.PostAsync(new Uri("/mcpserver/gh/issues/sync/to-github", UriKind.Relative), null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>GET /mcpserver/gh/actions/runs returns 200 and body with runs array.</summary>
    [Fact]
    public async Task ListWorkflowRuns_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/gh/actions/runs?limit=5", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("runs", out var runs));
        Assert.Equal(JsonValueKind.Array, runs.ValueKind);
    }

    /// <summary>PUT/GET/DELETE auth token endpoints support round-trip token state.</summary>
    [Fact]
    public async Task AuthTokenEndpoints_RoundTrip()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var setResponse = await _client.PutAsJsonAsync(
            new Uri("/mcpserver/gh/auth/token", UriKind.Relative),
            new { accessToken = "gho_test_token", expiresAtUtc = expiresAt }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        var statusResponse = await _client.GetAsync(new Uri("/mcpserver/gh/auth/status", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var statusDoc = JsonDocument.Parse(statusJson);
        Assert.True(statusDoc.RootElement.TryGetProperty("hasStoredToken", out var hasStoredToken));
        Assert.True(hasStoredToken.GetBoolean());

        var deleteResponse = await _client.DeleteAsync(new Uri("/mcpserver/gh/auth/token", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    /// <summary>GET /mcpserver/gh/oauth/config returns bootstrap payload, and authorize URL requires full config.</summary>
    [Fact]
    public async Task OAuthConfig_AndAuthorizeUrlBehavior()
    {
        var configResponse = await _client.GetAsync(new Uri("/mcpserver/gh/oauth/config", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, configResponse.StatusCode);
        var configJson = await configResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var configDoc = JsonDocument.Parse(configJson);
        Assert.True(configDoc.RootElement.TryGetProperty("isConfigured", out _));

        var authorizeResponse = await _client.GetAsync(new Uri("/mcpserver/gh/oauth/authorize-url?state=abc", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(
            authorizeResponse.StatusCode == HttpStatusCode.OK
            || authorizeResponse.StatusCode == HttpStatusCode.BadRequest);
    }
}
