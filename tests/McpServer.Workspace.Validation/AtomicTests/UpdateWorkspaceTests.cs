using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: PUT /mcpserver/workspace/{key} — Update a workspace registration.</summary>
[Collection("WorkspaceEndpoint")]
public sealed class UpdateWorkspaceTests : IAsyncLifetime
{
    private readonly WorkspaceEndpointFixture _fixture;
    private readonly string _testPath;
    private readonly string _testKey;

    public UpdateWorkspaceTests(WorkspaceEndpointFixture fixture)
    {
        _fixture = fixture;
        _testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        _testKey = WorkspaceEndpointFixture.EncodeKey(_testPath);
    }

    public async ValueTask InitializeAsync()
    {
        var body = new { WorkspacePath = _testPath, Name = "AuditUpdateOriginal" };
        var response = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        await _fixture.Client.DeleteAsync($"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}");
    }

    [Fact]
    public async Task Update_ChangeName_Returns200()
    {
        var body = new { Name = "AuditUpdateRenamed" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");
        Assert.NotNull(result.Workspace);
        Assert.Equal("AuditUpdateRenamed", result.Workspace.Name);
    }

    [Fact]
    public async Task Update_ChangeTodoPath_Returns200()
    {
        var body = new { TodoPath = "custom/todo.yaml" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{_testKey}", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Equal("custom/todo.yaml", result.Workspace.TodoPath);
    }

    [Fact]
    public async Task Update_NonExistentKey_Returns404()
    {
        var fakeKey = WorkspaceEndpointFixture.EncodeKey(@"C:\NonExistent\Path_" + Guid.NewGuid().ToString("N"));
        var body = new { Name = "Ghost" };
        var response = await _fixture.Client.PutAsJsonAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{fakeKey}", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
