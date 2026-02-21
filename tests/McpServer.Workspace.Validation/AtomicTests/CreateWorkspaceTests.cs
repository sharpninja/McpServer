using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: POST /mcp/workspace — Create (register) a new workspace.</summary>
[Collection("WorkspaceEndpoint")]
public sealed class CreateWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    public CreateWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        // Clean up: delete the test workspace if it was created.
        await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditCreateTest" };

        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.NotNull(result.Workspace);
        Assert.Equal(_testPath, result.Workspace.WorkspacePath);
        Assert.Equal("AuditCreateTest", result.Workspace.Name);
        Assert.True(result.Workspace.WorkspacePort >= 7148, "Auto-assigned port should be >= 7148.");
        Assert.Equal("docs/todo.yaml", result.Workspace.TodoPath);

        // Location header should be set
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(_testKey, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Create_DuplicatePath_Returns409()
    {
        var body = new { WorkspacePath = _testPath, Name = "First" };
        var first = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second create with same path should conflict.
        var second = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var result = await second.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Create_NoName_DerivesFromPath()
    {
        var body = new { WorkspacePath = _testPath };

        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
        Assert.NotNull(result);
        Assert.NotNull(result.Workspace);
        // Name should be derived from the last segment of the path.
        Assert.False(string.IsNullOrWhiteSpace(result.Workspace.Name));
    }

    [Fact]
    public async Task Create_NullBody_Returns400()
    {
        var response = await _fixture.Client.PostAsync(
            WorkspaceEndpointFixture.WorkspaceRoute,
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Empty object missing required WorkspacePath → should be BadRequest or validation error
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422 but got {(int)response.StatusCode}.");
    }
}
