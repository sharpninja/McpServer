using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Integration tests for WorkspaceController endpoints.</summary>
public sealed class WorkspaceControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public WorkspaceControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    public void Dispose() => _client.Dispose();

    /// <summary>Deletes a workspace created during a test to prevent config pollution.</summary>
    private async Task CleanupWorkspaceAsync(string path)
    {
        var key = EncodeKey(Path.GetFullPath(path));
        await _client.PostAsync(new Uri($"/mcpserver/workspace/{key}/stop", UriKind.Relative), null).ConfigureAwait(true);
        var deleteResponse = await _client.DeleteAsync(new Uri($"/mcpserver/workspace/{key}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ListWorkspaces_Returns200WithValidResult()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/workspace", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceListResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task CreateWorkspace_ValidRequest_Returns201()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_test_{Guid.NewGuid():N}");
        var request = new { workspacePath = path, name = "test-ws" };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Workspace);
        Assert.Equal("test-ws", result.Workspace.Name);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateWorkspace_NoName_CreatesWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_auto_{Guid.NewGuid():N}");
        var request = new { workspacePath = path };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.NotNull(result.Workspace);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateWorkspace_NoName_DerivesFromPath()
    {
        var folderName = $"MyProject_{Guid.NewGuid():N}";
        var path = Path.Combine(Path.GetTempPath(), folderName);
        var request = new { workspacePath = path };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), request).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>().ConfigureAwait(true);
        Assert.Equal(folderName, result!.Workspace!.Name);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateWorkspace_NoTodoPath_DefaultsToDocsTodoYaml()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_todo_{Guid.NewGuid():N}");
        var request = new { workspacePath = path };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), request).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>().ConfigureAwait(true);
        Assert.Equal("docs/todo.yaml", result!.Workspace!.TodoPath);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    [Fact]
    public async Task CreateWorkspace_Duplicate_Returns409()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_dup_{Guid.NewGuid():N}");
        var request = new { workspacePath = path };

        await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), request).ConfigureAwait(true);
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    [Fact]
    public async Task GetWorkspace_ValidKey_Returns200()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_get_{Guid.NewGuid():N}");
        await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), new { workspacePath = path }).ConfigureAwait(true);

        var key = EncodeKey(Path.GetFullPath(path));
        var response = await _client.GetAsync(new Uri($"/mcpserver/workspace/{key}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    [Fact]
    public async Task GetWorkspace_InvalidKey_Returns404()
    {
        var key = EncodeKey("C:\\nonexistent\\path");
        var response = await _client.GetAsync(new Uri($"/mcpserver/workspace/{key}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkspace_ChangeName_Returns200()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_upd_{Guid.NewGuid():N}");
        await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), new { workspacePath = path }).ConfigureAwait(true);

        var key = EncodeKey(Path.GetFullPath(path));
        var updateRequest = new { name = "renamed-ws" };
        var response = await _client.PutAsJsonAsync(new Uri($"/mcpserver/workspace/{key}", UriKind.Relative), updateRequest).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<WorkspaceMutationResult>().ConfigureAwait(true);
        Assert.Equal("renamed-ws", result!.Workspace!.Name);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    [Fact]
    public async Task DeleteWorkspace_Exists_Returns200()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_del_{Guid.NewGuid():N}");
        await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), new { workspacePath = path }).ConfigureAwait(true);

        var key = EncodeKey(Path.GetFullPath(path));
        var response = await _client.DeleteAsync(new Uri($"/mcpserver/workspace/{key}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkspace_NotFound_Returns404()
    {
        var key = EncodeKey("C:\\missing\\workspace");
        var response = await _client.DeleteAsync(new Uri($"/mcpserver/workspace/{key}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_StoppedProcess_ReturnsNotRunning()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ws_stat_{Guid.NewGuid():N}");
        await _client.PostAsJsonAsync(new Uri("/mcpserver/workspace", UriKind.Relative), new { workspacePath = path }).ConfigureAwait(true);

        // Creation auto-starts the workspace (FR-MCP-021); stop it first so we can verify "not running" status.
        var key = EncodeKey(Path.GetFullPath(path));
        await _client.PostAsync(new Uri($"/mcpserver/workspace/{key}/stop", UriKind.Relative), null).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri($"/mcpserver/workspace/{key}/status", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<WorkspaceProcessStatus>().ConfigureAwait(true);
        Assert.NotNull(status);
        Assert.False(status.IsRunning);

        await CleanupWorkspaceAsync(path).ConfigureAwait(true);
    }

    private static string EncodeKey(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path.Trim());
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
