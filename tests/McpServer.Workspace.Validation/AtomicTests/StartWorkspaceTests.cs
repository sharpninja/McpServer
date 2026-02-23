using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: POST /mcp/workspace/{key}/start — Start the hosted MCP instance.</summary>
[Collection("WorkspaceEndpoint")]
public sealed class StartWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    public StartWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    public async ValueTask InitializeAsync()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditStartTest" };
        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        // Stop first in case it was started, then delete.
        await _fixture.Client.PostAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}/stop", null);
        await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");
    }

    [Fact]
    public async Task Start_RegisteredWorkspace_ReturnsProcessStatus()
    {
        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}/start", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<WorkspaceProcessStatus>();
        Assert.NotNull(status);
        // It may or may not successfully start depending on environment, but should return the DTO.
    }

    [Fact]
    public async Task Start_NonExistentWorkspace_Returns404()
    {
        var fakeKey = WorkspaceEndpointFixture.EncodeKey(@"C:\NonExistent\Path_" + Guid.NewGuid().ToString("N"));

        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{fakeKey}/start", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Start_InvalidKey_Returns400()
    {
        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/!!!invalid!!!/start", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
