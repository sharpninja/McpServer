using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: GET /mcpserver/workspace/{key}/status — Get process status (public endpoint).</summary>
[Collection("WorkspaceEndpoint")]
public sealed class StatusWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    /// <summary>Initializes a new instance.</summary>
    public StatusWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    /// <summary>Initializes resources asynchronously.</summary>
    public async ValueTask InitializeAsync()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditStatusTest" };
        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>Disposes resources asynchronously.</summary>
    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Status_AfterCreate_ReturnsProcessStatus()
    {
        var response = await _fixture.Client.GetAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}/status", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<WorkspaceProcessStatus>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(status);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Status_InvalidKey_Returns400()
    {
        var response = await _fixture.Client.GetAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/!!!invalid!!!/status", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
