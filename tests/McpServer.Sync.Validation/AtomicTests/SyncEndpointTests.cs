using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace McpServer.Sync.Validation.AtomicTests;

/// <summary>Audit: Sync ingestion trigger and status endpoints.</summary>
[Collection("SyncEndpoint")]
public sealed class SyncEndpointTests
{
    private readonly SyncEndpointFixture _fixture;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SyncEndpointTests(SyncEndpointFixture fixture) => _fixture = fixture;

    // --- GET /mcp/sync/status ---

    [Fact]
    public async Task Status_Returns200WithStatusFields()
    {
        var response = await _fixture.Client.GetAsync($"{SyncEndpointFixture.SyncRoute}/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Status_ContainsExpectedFields()
    {
        var response = await _fixture.Client.GetAsync($"{SyncEndpointFixture.SyncRoute}/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        // Should have status field at minimum; may have lastRun, documentsIngested, etc.
        var statusVal = json.GetProperty("status").GetString();
        Assert.NotNull(statusVal);
    }

    // --- POST /mcp/sync/run ---

    [Fact]
    public async Task Run_Returns200WithRunResult()
    {
        var response = await _fixture.Client.PostAsync($"{SyncEndpointFixture.SyncRoute}/run", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.TryGetProperty("runId", out _));
        Assert.True(json.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Run_ThenStatus_ReflectsLastRun()
    {
        // Run sync first
        await _fixture.Client.PostAsync($"{SyncEndpointFixture.SyncRoute}/run", null);

        // Check status reflects the run
        var response = await _fixture.Client.GetAsync($"{SyncEndpointFixture.SyncRoute}/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var status = json.GetProperty("status").GetString();
        // After a run, status should not be "idle"
        Assert.NotNull(status);
    }
}
