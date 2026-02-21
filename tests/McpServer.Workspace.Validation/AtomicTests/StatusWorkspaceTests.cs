using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: GET /mcp/workspace/{key}/status — Get process status (public endpoint).</summary>
[Collection("WorkspaceEndpoint")]
public sealed class StatusWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    public StatusWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    public async ValueTask InitializeAsync()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditStatusTest" };
        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");
    }

    [Fact]
    public async Task Status_NotStarted_ReturnsNotRunning()
    {
        var response = await _fixture.Client.GetAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
        Assert.NotNull(status);
        Assert.False(status.IsRunning, "Workspace that was never started should report IsRunning=false.");
    }

    [Fact]
    public async Task Status_InvalidKey_Returns400()
    {
        var response = await _fixture.Client.GetAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/!!!invalid!!!/status");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
