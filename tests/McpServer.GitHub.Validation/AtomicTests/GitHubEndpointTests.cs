using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace McpServer.GitHub.Validation.AtomicTests;

/// <summary>Audit: GitHub endpoints — issues, pulls, labels, comments, sync.</summary>
[Collection("GitHubEndpoint")]
public sealed class GitHubEndpointTests
{
    private readonly GitHubEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public GitHubEndpointTests(GitHubEndpointFixture fixture) => _fixture = fixture;

    // --- GET /mcp/gh/issues ---

    [Fact]
    public async Task ListIssues_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/issues");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("issues", out var issues));
        Assert.Equal(JsonValueKind.Array, issues.ValueKind);
    }

    [Fact]
    public async Task ListIssues_WithState_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/issues?state=open&limit=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GET /mcp/gh/issues/{number} ---

    [Fact]
    public async Task GetIssue_ExistingNumber_Returns200Or404()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/issues/1");
        // May be 200 (found) or 404 (not found) depending on repo state
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {(int)response.StatusCode}");
    }

    // --- POST /mcp/gh/issues (create) ---

    [Fact]
    public async Task CreateIssue_MissingTitle_Returns400()
    {
        var payload = new { body = "no title" };
        var response = await _fixture.Client.PostAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/issues", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- PUT /mcp/gh/issues/{number} (update) ---

    [Fact]
    public async Task UpdateIssue_NullBody_Returns400()
    {
        var response = await _fixture.Client.PutAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/issues/99999", (object?)null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- POST /mcp/gh/issues/{id}/comments ---

    [Fact]
    public async Task CommentOnIssue_MissingBody_Returns400()
    {
        var payload = new { body = "" };
        var response = await _fixture.Client.PostAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/issues/1/comments", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- GET /mcp/gh/labels ---

    [Fact]
    public async Task ListLabels_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/labels");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("labels", out _));
    }

    // --- GET /mcp/gh/pulls ---

    [Fact]
    public async Task ListPulls_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/pulls");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("pulls", out var pulls));
        Assert.Equal(JsonValueKind.Array, pulls.ValueKind);
    }

    [Fact]
    public async Task ListPulls_WithState_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/pulls?state=closed&limit=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /mcp/gh/pulls/{id}/comments ---

    [Fact]
    public async Task CommentOnPull_MissingBody_Returns400()
    {
        var payload = new { body = "" };
        var response = await _fixture.Client.PostAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/pulls/1/comments", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- POST /mcp/gh/issues/{number}/close ---

    [Fact]
    public async Task CloseIssue_NonExistent_ReturnsBadRequestOr200()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/99999/close", null);
        // May be 200 (gh cli succeeds) or 400 (fails) depending on issue existence
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcp/gh/issues/{number}/reopen ---

    [Fact]
    public async Task ReopenIssue_NonExistent_ReturnsBadRequestOr200()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/99999/reopen", null);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcp/gh/issues/sync/from-github ---

    [Fact]
    public async Task SyncFromGitHub_Returns200Or400()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/sync/from-github", null);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcp/gh/issues/sync/to-github ---

    [Fact]
    public async Task SyncToGitHub_Returns200Or400()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/sync/to-github", null);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcp/gh/issues/{number}/sync ---

    [Fact]
    public async Task SyncSingleIssue_Returns200Or400Or404()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/1/sync", null);
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200, 400, or 404, got {(int)response.StatusCode}");
    }
}
