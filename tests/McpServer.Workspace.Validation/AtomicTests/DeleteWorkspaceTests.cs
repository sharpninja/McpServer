using System.Net;
using System.Net.Http.Json;
using McpServer.Workspace.Validation.Models;
using Xunit;

namespace McpServer.Workspace.Validation.AtomicTests;

/// <summary>Audit: DELETE /mcpserver/workspace/{key} — Delete a workspace registration.</summary>
[Collection("WorkspaceEndpoint")]
public sealed class DeleteWorkspaceTests
{
    private readonly WorkspaceEndpointFixture _fixture;

    /// <summary>Initializes a new instance.</summary>
    public DeleteWorkspaceTests(WorkspaceEndpointFixture fixture) => _fixture = fixture;

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Delete_ExistingWorkspace_Returns200()
    {
        // Create a workspace to delete.
        var testPath = WorkspaceEndpointFixture.GenerateTestWorkspacePath();
        var testKey = WorkspaceEndpointFixture.EncodeKey(testPath);

        var createBody = new { WorkspacePath = testPath, Name = "AuditDeleteTest" };
        var createResponse = await _fixture.Client.PostAsJsonAsync(WorkspaceEndpointFixture.WorkspaceRoute, createBody, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Now delete it.
        var deleteResponse = await _fixture.Client.DeleteAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{testKey}", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var result = await deleteResponse.Content.ReadFromJsonAsync<WorkspaceMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success but got error: {result.Error}");

        // Verify it's gone.
        var getResponse = await _fixture.Client.GetAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{testKey}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Delete_NonExistentWorkspace_Returns404()
    {
        var fakeKey = WorkspaceEndpointFixture.EncodeKey(@"C:\NonExistent\Path_" + Guid.NewGuid().ToString("N"));

        var response = await _fixture.Client.DeleteAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/{fakeKey}", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    /// <summary>Test method.</summary>
    [Fact]
    public async Task Delete_InvalidKey_Returns400()
    {
        var response = await _fixture.Client.DeleteAsync(
            $"{WorkspaceEndpointFixture.WorkspaceRoute}/!!!invalid!!!", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
