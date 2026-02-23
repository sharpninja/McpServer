using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: POST /mcp/workspace/{key}/stop — Stop the hosted MCP instance.</summary>
[Collection("WorkspaceEndpoint")]
public sealed class StopWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    public StopWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    public async ValueTask InitializeAsync()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditStopTest" };
        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");
    }

    [Fact]
    public async Task Stop_NotRunning_ReturnsStatus()
    {
        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}/stop", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
        Assert.NotNull(status);
        // Stopping a non-running workspace should succeed (idempotent) with IsRunning=false.
        Assert.False(status.IsRunning);
    }

    [Fact]
    public async Task Stop_InvalidKey_Returns400()
    {
        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/!!!invalid!!!/stop", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
