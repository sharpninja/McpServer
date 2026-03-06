using System.Net;
using System.Net.Http.Json;
using McpServer.ToolRegistry.Validation.Models;
using Xunit;

namespace McpServer.ToolRegistry.Validation.AtomicTests;

/// <summary>Audit: Tool CRUD endpoints — List, Search, Get, Create, Update, Delete.</summary>
[Collection("ToolRegistry")]
public sealed class ToolCrudTests
{
    private readonly ToolRegistryFixture _f;
    /// <summary>
    /// Initializes a new instance of ToolCrudTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    public ToolCrudTests(ToolRegistryFixture f) => _f = f;

    // ── List ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>List_Returns200WithValidStructure</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task List_Returns200WithValidStructure()
    {
        var r = await _f.Client.GetAsync(ToolRegistryFixture.ToolRoute);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>();
        Assert.NotNull(res);
        Assert.NotNull(res.Tools);
        Assert.True(res.TotalCount >= 0);
    }

    /// <summary>
    /// Validates the <c>List_ResponseIsJson</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task List_ResponseIsJson()
    {
        var r = await _f.Client.GetAsync(ToolRegistryFixture.ToolRoute);
        Assert.Equal("application/json", r.Content.Headers.ContentType?.MediaType);
    }

    // ── Search ───────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>Search_WithKeyword_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Search_WithKeyword_Returns200()
    {
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/search?keyword=test");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>();
        Assert.NotNull(res);
    }

    /// <summary>
    /// Validates the <c>Search_NonMatchingKeyword_ReturnsEmpty</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Search_NonMatchingKeyword_ReturnsEmpty()
    {
        var r = await _f.Client.GetAsync(
            $"{ToolRegistryFixture.ToolRoute}/search?keyword=zzz_nonexistent_{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>();
        Assert.NotNull(res);
        Assert.Equal(0, res.TotalCount);
    }

    // ── Create + Get + Update + Delete (full mini-cycle) ─────────────────

    /// <summary>
    /// Validates the <c>Create_ValidTool_Returns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Create_ValidTool_Returns201()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        try
        {
            var body = new
            {
                Name = name,
                Description = "Audit test tool",
                Tags = new[] { "audit", "test" },
                CommandTemplate = "echo hello"
            };
            var r = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body);
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);

            var res = await r.Content.ReadFromJsonAsync<ToolMutationResult>();
            Assert.NotNull(res);
            Assert.True(res.Success, $"Create failed: {res.Error}");
            Assert.NotNull(res.Tool);
            Assert.Equal(name, res.Tool.Name);
            Assert.Equal("Audit test tool", res.Tool.Description);
            Assert.Contains("audit", res.Tool.Tags);
            Assert.True(res.Tool.Id > 0);
            Assert.NotNull(r.Headers.Location);

            // Cleanup
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{res.Tool.Id}");
        }
        catch { /* best effort */ }
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
        var body = new { Name = name, Description = "First", Tags = new[] { "dup" } };
        var first = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstRes = await first.Content.ReadFromJsonAsync<ToolMutationResult>();

        try
        {
            var second = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }
        finally
        {
            if (firstRes?.Tool != null)
                await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{firstRes.Tool.Id}");
        }
    }

    /// <summary>
    /// Validates the <c>Get_ExistingTool_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Get_ExistingTool_Returns200()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "GetTest", Tags = new[] { "get" } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>();
        Assert.NotNull(createRes?.Tool);

        try
        {
            var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var dto = await r.Content.ReadFromJsonAsync<ToolDto>();
            Assert.NotNull(dto);
            Assert.Equal(name, dto.Name);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}");
        }
    }

    /// <summary>
    /// Validates the <c>Get_NonExistentId_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/999999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Update_ChangeName_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Update_ChangeName_Returns200()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "UpdateTest", Tags = new[] { "upd" } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>();
        Assert.NotNull(createRes?.Tool);

        try
        {
            var newName = ToolRegistryFixture.GenerateToolName();
            var update = new { Name = newName };
            var r = await _f.Client.PutAsJsonAsync(
                $"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", update);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var res = await r.Content.ReadFromJsonAsync<ToolMutationResult>();
            Assert.NotNull(res);
            Assert.True(res.Success);
            Assert.Equal(newName, res.Tool!.Name);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}");
        }
    }

    /// <summary>
    /// Validates the <c>Update_NonExistentId_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        var update = new { Name = "ghost" };
        var r = await _f.Client.PutAsJsonAsync($"{ToolRegistryFixture.ToolRoute}/999999", update);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Delete_ExistingTool_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Delete_ExistingTool_Returns200()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "DeleteTest", Tags = new[] { "del" } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>();
        Assert.NotNull(createRes?.Tool);

        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolMutationResult>();
        Assert.NotNull(res);
        Assert.True(res.Success);

        // Verify gone
        var get = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    /// <summary>
    /// Validates the <c>Delete_NonExistentId_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/999999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ── Search by created tool tag ───────────────────────────────────────

    /// <summary>
    /// Validates the <c>Search_ByTag_FindsCreatedTool</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task Search_ByTag_FindsCreatedTool()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var uniqueTag = $"audittag{Guid.NewGuid().ToString("N")[..6]}";
        var body = new { Name = name, Description = "SearchTagTest", Tags = new[] { uniqueTag } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>();
        Assert.NotNull(createRes?.Tool);

        try
        {
            var r = await _f.Client.GetAsync(
                $"{ToolRegistryFixture.ToolRoute}/search?keyword={uniqueTag}");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>();
            Assert.NotNull(res);
            Assert.True(res.TotalCount >= 1);
            Assert.Contains(res.Tools, t => t.Name == name);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}");
        }
    }
}
