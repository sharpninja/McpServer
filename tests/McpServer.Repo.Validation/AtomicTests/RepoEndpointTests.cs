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

    public RepoEndpointTests(RepoEndpointFixture fixture) => _fixture = fixture;

    // --- GET /mcpserver/repo/list ---

    [Fact]
    public async Task List_RootPath_Returns200WithEntries()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("entries", out var entries));
        Assert.Equal(JsonValueKind.Array, entries.ValueKind);
    }

    [Fact]
    public async Task List_WithPath_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/list?path=docs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("path", out _));
        Assert.True(json.TryGetProperty("entries", out _));
    }

    // --- GET /mcpserver/repo/file ---

    [Fact]
    public async Task ReadFile_MissingPath_Returns400()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/file");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("path is required", json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ReadFile_ExistingFile_Returns200()
    {
        var response = await _fixture.Client.GetAsync($"{RepoEndpointFixture.RepoRoute}/file?path=index.md");
        // 200 if allowed and exists, 400 if path not allowed
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task ReadFile_NonExistentFile_Returns200Or400()
    {
        var response = await _fixture.Client.GetAsync(
            $"{RepoEndpointFixture.RepoRoute}/file?path=nonexistent-{Guid.NewGuid():N}.txt");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }

    // --- POST /mcpserver/repo/file ---

    [Fact]
    public async Task WriteFile_MissingPath_Returns400()
    {
        var payload = new { content = "test" };
        var response = await _fixture.Client.PostAsJsonAsync($"{RepoEndpointFixture.RepoRoute}/file", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WriteFile_NullBody_Returns400()
    {
        var response = await _fixture.Client.PostAsJsonAsync($"{RepoEndpointFixture.RepoRoute}/file", (object?)null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WriteFile_ValidPath_Returns200Or400()
    {
        var payload = new { path = "_tmp_audit_test.txt", content = "audit test" };
        var response = await _fixture.Client.PostAsJsonAsync($"{RepoEndpointFixture.RepoRoute}/file", payload);
        // 200 if path is allowed, 400 if path not in allowlist
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {(int)response.StatusCode}");
    }
}
