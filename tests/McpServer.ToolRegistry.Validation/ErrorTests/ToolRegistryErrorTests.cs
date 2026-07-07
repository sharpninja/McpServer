using System.Net;
using System.Net.Http.Json;
using McpServer.ToolRegistry.Validation.Models;
using Xunit;

namespace McpServer.ToolRegistry.Validation.ErrorTests;

/// <summary>Audit: Error and edge-case tests for tool registry endpoints.</summary>
[Collection("ToolRegistry")]
public sealed class ToolRegistryErrorTests
{
    private readonly ToolRegistryFixture _f;
    /// <summary>
    /// Initializes a new instance of ToolRegistryErrorTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    public ToolRegistryErrorTests(ToolRegistryFixture f) => _f = f;

    /// <summary>
    /// Validates the <c>Get_NonExistentTool_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Get_NonExistentTool_Returns404()
    {
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/999999", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Update_NonExistentTool_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Update_NonExistentTool_Returns404()
    {
        var r = await _f.Client.PutAsJsonAsync($"{ToolRegistryFixture.ToolRoute}/999999", new { Name = "ghost" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Delete_NonExistentTool_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Delete_NonExistentTool_Returns404()
    {
        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/999999", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>RemoveBucket_NonExistent_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task RemoveBucket_NonExistent_Returns404()
    {
        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>BrowseBucket_NonExistent_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task BrowseBucket_NonExistent_Returns404()
    {
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/browse", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>SyncBucket_NonExistent_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task SyncBucket_NonExistent_Returns404()
    {
        var r = await _f.Client.PostAsync($"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/sync", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>InstallFromBucket_NonExistent_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task InstallFromBucket_NonExistent_Returns404()
    {
        var r = await _f.Client.PostAsync(
            $"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/install?toolName=foo", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Patch_NotSupported_Returns405</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Patch_NotSupported_Returns405()
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, $"{ToolRegistryFixture.ToolRoute}/1")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        var r = await _f.Client.SendAsync(req, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Get_InvalidIdFormat_Returns404Or400</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Get_InvalidIdFormat_Returns404Or400()
    {
        // {id:int} constraint should reject non-numeric — returns 404 (no matching route).
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/not-a-number", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(
            r.StatusCode == HttpStatusCode.NotFound || r.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404/400 but got {(int)r.StatusCode}.");
    }

    /// <summary>
    /// Validates the <c>Create_DuplicateName_Returns409</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "Dup", Tags = new[] { "dup" } };
        var first = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstRes = await first.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            var second = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            var res = await second.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(res);
            Assert.False(res.Success);
        }
        finally
        {
            if (firstRes?.Tool != null)
                await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{firstRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Validates the <c>AddBucket_Duplicate_Returns409</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task AddBucket_Duplicate_Returns409()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer" };
        var first = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        try
        {
            var second = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Validates the <c>Search_WithWorkspace_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Search_WithWorkspace_Returns200()
    {
        var r = await _f.Client.GetAsync(
            $"{ToolRegistryFixture.ToolRoute}/search?keyword=test&workspace=E%3A%5Cgithub%5CMcpServer", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>List_WithWorkspace_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task List_WithWorkspace_Returns200()
    {
        var r = await _f.Client.GetAsync(
            $"{ToolRegistryFixture.ToolRoute}?workspace=E%3A%5Cgithub%5CMcpServer", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }
}
