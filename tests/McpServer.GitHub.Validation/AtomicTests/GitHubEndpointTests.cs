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

    /// <summary>
    /// Initializes a new instance of GitHubEndpointTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    public GitHubEndpointTests(GitHubEndpointFixture fixture) => _fixture = fixture;

    // --- GET /mcpserver/gh/issues ---

    /// <summary>
    /// Validates the <c>ListIssues_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task ListIssues_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/issues", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("issues", out var issues));
        Assert.Equal(JsonValueKind.Array, issues.ValueKind);
    }

    /// <summary>
    /// Validates the <c>ListIssues_WithState_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task ListIssues_WithState_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/issues?state=open&limit=5", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- GET /mcpserver/gh/issues/{number} ---

    /// <summary>
    /// Validates the <c>GetIssue_ExistingNumber_Returns200Or404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task GetIssue_ExistingNumber_Returns200Or404()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/issues/1", cancellationToken: TestContext.Current.CancellationToken);
        // May be 200 (found) or 404 (not found) depending on repo state
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {(int)response.StatusCode}");
    }

    // --- POST /mcpserver/gh/issues (create) ---

    /// <summary>
    /// Validates the <c>CreateIssue_MissingTitle_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task CreateIssue_MissingTitle_Returns400()
    {
        var payload = new { body = "no title" };
        var response = await _fixture.Client.PostAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/issues", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- PUT /mcpserver/gh/issues/{number} (update) ---

    /// <summary>
    /// Validates the <c>UpdateIssue_NullBody_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task UpdateIssue_NullBody_Returns400()
    {
        var response = await _fixture.Client.PutAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/issues/99999", (object?)null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- POST /mcpserver/gh/issues/{id}/comments ---

    /// <summary>
    /// Validates the <c>CommentOnIssue_MissingBody_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task CommentOnIssue_MissingBody_Returns400()
    {
        var payload = new { body = "" };
        var response = await _fixture.Client.PostAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/issues/1/comments", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- GET /mcpserver/gh/labels ---

    /// <summary>
    /// Validates the <c>ListLabels_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task ListLabels_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/labels", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("labels", out _));
    }

    // --- GET /mcpserver/gh/pulls ---

    /// <summary>
    /// Validates the <c>ListPulls_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task ListPulls_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/pulls", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("pulls", out var pulls));
        Assert.Equal(JsonValueKind.Array, pulls.ValueKind);
    }

    /// <summary>
    /// Validates the <c>ListPulls_WithState_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task ListPulls_WithState_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{GitHubEndpointFixture.GhRoute}/pulls?state=closed&limit=5", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --- POST /mcpserver/gh/pulls/{id}/comments ---

    /// <summary>
    /// Validates the <c>CommentOnPull_MissingBody_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task CommentOnPull_MissingBody_Returns400()
    {
        var payload = new { body = "" };
        var response = await _fixture.Client.PostAsJsonAsync($"{GitHubEndpointFixture.GhRoute}/pulls/1/comments", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- POST /mcpserver/gh/issues/{number}/close ---

    /// <summary>
    /// Validates the <c>CloseIssue_NonExistent_ReturnsBadRequestOr200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task CloseIssue_NonExistent_ReturnsBadRequestOr200()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/99999/close", null, cancellationToken: TestContext.Current.CancellationToken);
        // May be 200 (gh cli succeeds) or 400 (fails) depending on issue existence
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcpserver/gh/issues/{number}/reopen ---

    /// <summary>
    /// Validates the <c>ReopenIssue_NonExistent_ReturnsBadRequestOr200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task ReopenIssue_NonExistent_ReturnsBadRequestOr200()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/99999/reopen", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcpserver/gh/issues/sync/from-github ---

    /// <summary>
    /// Validates the <c>SyncFromGitHub_Returns200Or400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task SyncFromGitHub_Returns200Or400()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/sync/from-github", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcpserver/gh/issues/sync/to-github ---

    /// <summary>
    /// Validates the <c>SyncToGitHub_Returns200Or400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task SyncToGitHub_Returns200Or400()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/sync/to-github", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcpserver/gh/issues/{number}/sync ---

    /// <summary>
    /// Validates the <c>SyncSingleIssue_Returns200Or400Or404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-081, TEST-MCP-082, TEST-MCP-083, TEST-MCP-084, FR-MCP-063, TR-MCP-GH-001, TR-MCP-GH-004.
    /// Test data: Fixture HTTP calls with issue/pull/label/comment/sync payloads and existing/non-existing identifiers.
    /// Data rationale: These inputs verify GitHub integration contracts and expected status behavior on valid and invalid requests.
    /// </remarks>
    [Fact]
    public async Task SyncSingleIssue_Returns200Or400Or404()
    {
        var response = await _fixture.Client.PostAsync($"{GitHubEndpointFixture.GhRoute}/issues/1/sync", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200, 400, or 404, got {(int)response.StatusCode}");
    }
}
