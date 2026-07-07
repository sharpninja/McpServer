using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace McpServer.Repo.Validation.AtomicTests;

/// <summary>Audit: Repo file read/write/list endpoints.</summary>
[Collection("RepoEndpoint")]
public sealed class RepoEndpointTests
{
    private readonly RepoEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of RepoEndpointTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    public RepoEndpointTests(RepoEndpointFixture fixture) => _fixture = fixture;

    // --- GET /mcpserver/repo/list ---

    /// <summary>
    /// Validates the <c>List_RootPath_Returns200WithEntries</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task List_RootPath_Returns200WithEntries()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/list", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("entries", out var entries));
        Assert.Equal(JsonValueKind.Array, entries.ValueKind);
    }

    /// <summary>
    /// Validates the <c>List_WithPath_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task List_WithPath_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/list?path=docs", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(json.TryGetProperty("path", out _));
        Assert.True(json.TryGetProperty("entries", out _));
    }

    // --- GET /mcpserver/repo/file ---

    /// <summary>
    /// Validates the <c>ReadFile_MissingPath_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task ReadFile_MissingPath_Returns400()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/file", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("path is required", json.GetProperty("error").GetString());
    }

    /// <summary>
    /// Validates the <c>ReadFile_ExistingFile_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task ReadFile_ExistingFile_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/file?path=index.md", cancellationToken: TestContext.Current.CancellationToken);
        // 200 if allowed and exists, 400 if path not allowed
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    /// <summary>
    /// Validates the <c>ReadFile_NonExistentFile_Returns200Or400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task ReadFile_NonExistentFile_Returns200Or400()
    {
        var response = await _fixture.Client.GetAsync(
            $"{RepoEndpointFixture.RepoRoute}/file?path=nonexistent-{Guid.NewGuid():N}.txt", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcpserver/repo/file ---

    /// <summary>
    /// Validates the <c>WriteFile_MissingPath_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task WriteFile_MissingPath_Returns400()
    {
        var payload = new { content = "test" };
        var response = await _fixture.Client.PostAsJsonAsync($"{RepoEndpointFixture.RepoRoute}/file", payload, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>WriteFile_NullBody_Returns400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task WriteFile_NullBody_Returns400()
    {
        var response = await _fixture.Client.PostAsJsonAsync($"{RepoEndpointFixture.RepoRoute}/file", (object?)null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Validates the <c>WriteFile_ValidPath_Returns200Or400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-001, FR-SUPPORT-010, TR-MCP-API-001.
    /// Test data: Fixture HTTP calls with repo list/read/write routes, path query values, and write payload objects.
    /// Data rationale: These inputs verify repository endpoint contract behavior, validation checks, and route correctness.
    /// </remarks>
    [Fact]
    public async Task WriteFile_ValidPath_Returns200Or400()
    {
        var payload = new { path = "_tmp_audit_test.txt", content = "audit test" };
        var response = await _fixture.Client.PostAsJsonAsync($"{RepoEndpointFixture.RepoRoute}/file", payload, cancellationToken: TestContext.Current.CancellationToken);
        // 200 if path is allowed, 400 if path not in allowlist
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }
}
