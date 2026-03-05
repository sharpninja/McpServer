using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>Integration tests for ToolRegistryController endpoints.</summary>
public sealed class ToolRegistryControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public ToolRegistryControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task ListTools_Returns200()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/tools", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task CreateTool_ValidRequest_Returns201()
    {
        var request = new
        {
            name = $"screenshot_{Guid.NewGuid():N}",
            description = "Takes a screenshot of the current screen",
            tags = new[] { "screenshot", "capture", "image" },
            commandTemplate = "powershell -File Take-Screenshot.ps1 -Path {path}"
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ToolMutationResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Tool);
        Assert.Equal(request.description, result.Tool.Description);
        Assert.Contains("screenshot", result.Tool.Tags);
        Assert.Contains("capture", result.Tool.Tags);
    }

    [Fact]
    public async Task CreateTool_DuplicateName_Returns409()
    {
        var name = $"duptool_{Guid.NewGuid():N}";
        var request = new { name, description = "Test", tags = new[] { "test" } };

        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SearchByKeyword_MatchesTag_ReturnsResults()
    {
        var unique = Guid.NewGuid().ToString("N");
        var request = new
        {
            name = $"tool_{unique}",
            description = "A clipboard utility",
            tags = new[] { $"clip_{unique}", "paste" }
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri($"/mcpserver/tools/search?keyword=clip_{unique}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Tools, t => t.Name == $"tool_{unique}");
    }

    [Fact]
    public async Task SearchByKeyword_MatchesName_ReturnsResults()
    {
        var unique = Guid.NewGuid().ToString("N");
        var request = new
        {
            name = $"findme_{unique}",
            description = "Some tool",
            tags = new[] { "tag1" }
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri($"/mcpserver/tools/search?keyword=findme_{unique}", UriKind.Relative)).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.True(result!.TotalCount >= 1);
    }

    [Fact]
    public async Task SearchByKeyword_MatchesDescription_ReturnsResults()
    {
        var unique = Guid.NewGuid().ToString("N");
        var request = new
        {
            name = $"desctool_{unique}",
            description = $"A unique_{unique} tool for testing",
            tags = new[] { "other" }
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri($"/mcpserver/tools/search?keyword=unique_{unique}", UriKind.Relative)).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.True(result!.TotalCount >= 1);
    }

    [Fact]
    public async Task SearchByKeyword_NoMatch_ReturnsEmptyList()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/tools/search?keyword=zzz_nonexistent_zzz", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.Equal(0, result!.TotalCount);
    }

    [Fact]
    public async Task GetTool_Exists_Returns200()
    {
        var name = $"gettool_{Guid.NewGuid():N}";
        var createResp = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative),
            new { name, description = "Test", tags = new[] { "test" } }).ConfigureAwait(true);
        var created = await createResp.Content.ReadFromJsonAsync<ToolMutationResult>().ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri($"/mcpserver/tools/{created!.Tool!.Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTool_NotFound_Returns404()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/tools/99999", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTool_ChangeDescription_Returns200()
    {
        var name = $"uptool_{Guid.NewGuid():N}";
        var createResp = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative),
            new { name, description = "Old desc", tags = new[] { "tag1" } }).ConfigureAwait(true);
        var created = await createResp.Content.ReadFromJsonAsync<ToolMutationResult>().ConfigureAwait(true);

        var updateReq = new { description = "New desc" };
        var response = await _client.PutAsJsonAsync(new Uri($"/mcpserver/tools/{created!.Tool!.Id}", UriKind.Relative), updateReq).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ToolMutationResult>().ConfigureAwait(true);
        Assert.Equal("New desc", result!.Tool!.Description);
    }

    [Fact]
    public async Task UpdateTool_ReplaceTags_Returns200()
    {
        var name = $"tagtool_{Guid.NewGuid():N}";
        var createResp = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative),
            new { name, description = "Test", tags = new[] { "old1", "old2" } }).ConfigureAwait(true);
        var created = await createResp.Content.ReadFromJsonAsync<ToolMutationResult>().ConfigureAwait(true);

        var updateReq = new { tags = new[] { "new1", "new2", "new3" } };
        var response = await _client.PutAsJsonAsync(new Uri($"/mcpserver/tools/{created!.Tool!.Id}", UriKind.Relative), updateReq).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<ToolMutationResult>().ConfigureAwait(true);

        Assert.Equal(3, result!.Tool!.Tags.Count);
        Assert.Contains("new1", result.Tool.Tags);
        Assert.DoesNotContain("old1", result.Tool.Tags);
    }

    [Fact]
    public async Task DeleteTool_Exists_Returns200()
    {
        var name = $"deltool_{Guid.NewGuid():N}";
        var createResp = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative),
            new { name, description = "Test", tags = new[] { "test" } }).ConfigureAwait(true);
        var created = await createResp.Content.ReadFromJsonAsync<ToolMutationResult>().ConfigureAwait(true);

        var response = await _client.DeleteAsync(new Uri($"/mcpserver/tools/{created!.Tool!.Id}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTool_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync(new Uri("/mcpserver/tools/99999", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WorkspaceScopedTool_NotVisibleInGlobalSearch()
    {
        var unique = Guid.NewGuid().ToString("N");
        var request = new
        {
            name = $"scoped_{unique}",
            description = "Workspace-only tool",
            tags = new[] { $"scope_{unique}" },
            workspacePath = Path.Combine(Path.GetTempPath(), $"ws_{unique}")
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);

        // Global search (no workspace param) should NOT see it.
        var response = await _client.GetAsync(new Uri($"/mcpserver/tools/search?keyword=scope_{unique}", UriKind.Relative)).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.Equal(0, result!.TotalCount);
    }

    [Fact]
    public async Task WorkspaceScopedTool_VisibleWhenWorkspaceSpecified()
    {
        var unique = Guid.NewGuid().ToString("N");
        var wsPath = Path.Combine(Path.GetTempPath(), $"ws_{unique}");
        var request = new
        {
            name = $"scoped2_{unique}",
            description = "Workspace-only tool",
            tags = new[] { $"scope2_{unique}" },
            workspacePath = wsPath
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);

        // Search with workspace param should see it.
        var encodedWs = Uri.EscapeDataString(wsPath);
        var response = await _client.GetAsync(new Uri($"/mcpserver/tools/search?keyword=scope2_{unique}&workspace={encodedWs}", UriKind.Relative)).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.True(result!.TotalCount >= 1);
    }

    [Fact]
    public async Task GlobalTool_VisibleInWorkspaceSearch()
    {
        var unique = Guid.NewGuid().ToString("N");
        // Create a global tool.
        var request = new
        {
            name = $"global_{unique}",
            description = "Global tool",
            tags = new[] { $"glob_{unique}" }
        };
        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools", UriKind.Relative), request).ConfigureAwait(true);

        // Search with a workspace param should still see global tools.
        var wsPath = Uri.EscapeDataString(Path.Combine(Path.GetTempPath(), "any_workspace"));
        var response = await _client.GetAsync(new Uri($"/mcpserver/tools/search?keyword=glob_{unique}&workspace={wsPath}", UriKind.Relative)).ConfigureAwait(true);
        var result = await response.Content.ReadFromJsonAsync<ToolSearchResult>().ConfigureAwait(true);
        Assert.True(result!.TotalCount >= 1);
    }

    // ── Bucket endpoints ───────────────────────────────────────────────

    [Fact]
    public async Task ListBuckets_Returns200()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/tools/buckets", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BucketListResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task AddBucket_ValidRequest_Returns201()
    {
        var request = new
        {
            name = $"bucket_{Guid.NewGuid():N}",
            owner = "sharpninja",
            repo = "mcp-tool-bucket"
        };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools/buckets", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BucketMutationResult>().ConfigureAwait(true);
        Assert.True(result!.Success);
        Assert.Equal("main", result.Bucket!.Branch);
    }

    [Fact]
    public async Task AddBucket_DuplicateName_Returns409()
    {
        var name = $"dup_bucket_{Guid.NewGuid():N}";
        var request = new { name, owner = "test", repo = "repo" };

        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools/buckets", UriKind.Relative), request).ConfigureAwait(true);
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/tools/buckets", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoveBucket_Exists_Returns200()
    {
        var name = $"rmbucket_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync(new Uri("/mcpserver/tools/buckets", UriKind.Relative),
            new { name, owner = "test", repo = "repo" }).ConfigureAwait(true);

        var response = await _client.DeleteAsync(new Uri($"/mcpserver/tools/buckets/{name}", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RemoveBucket_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync(new Uri("/mcpserver/tools/buckets/nonexistent_bucket", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
