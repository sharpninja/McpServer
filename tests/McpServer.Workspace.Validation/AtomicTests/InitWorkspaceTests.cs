using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: POST /mcpserver/workspace/{key}/init — Initialize workspace data files.</summary>
[Collection("WorkspaceEndpoint")]
public sealed class InitWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    /// <summary>Initializes a new instance.</summary>
    public InitWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    /// <summary>Initializes resources asynchronously.</summary>
    public async ValueTask InitializeAsync()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditInitTest" };
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
    public async Task Init_RegisteredWorkspace_ReturnsResult()
    {
        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}/init", null, cancellationToken: TestContext.Current.CancellationToken);

        // Init may succeed (200) or fail if the directory doesn't physically exist (422).
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 200 or 422 but got {(int)response.StatusCode}.");

        var result = await response.Content.ReadFromJsonAsync<WorkspaceInitResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(result.Success);
        }
        else
        {
            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Init_InvalidKey_Returns400()
    {
        var response = await _fixture.Client.PostAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/!!!invalid!!!/init", null, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
