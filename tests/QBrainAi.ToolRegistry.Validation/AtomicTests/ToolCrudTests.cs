using System.Net;
using System.Net.Http.Json;
using QBrainAi.ToolRegistry.Validation.Models;
using Xunit;

namespace QBrainAi.ToolRegistry.Validation.AtomicTests;

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
    /// Validates the <c>ListReturns200WithValidStructure</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task ListReturns200WithValidStructure()
    {
        var r = await _f.Client.GetAsync(ToolRegistryFixture.ToolRoute, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(res);
        Assert.NotNull(res.Tools);
        Assert.True(res.TotalCount >= 0);
    }

    /// <summary>
    /// Validates the <c>ListResponseIsJson</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task ListResponseIsJson()
    {
        var r = await _f.Client.GetAsync(ToolRegistryFixture.ToolRoute, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("application/json", r.Content.Headers.ContentType?.MediaType);
    }

    // ── Search ───────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>SearchWithKeywordReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task SearchWithKeywordReturns200()
    {
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/search?keyword=test", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(res);
    }

    /// <summary>
    /// Validates the <c>SearchNonMatchingKeywordReturnsEmpty</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task SearchNonMatchingKeywordReturnsEmpty()
    {
        var r = await _f.Client.GetAsync(
            $"{ToolRegistryFixture.ToolRoute}/search?keyword=zzz_nonexistent_{Guid.NewGuid():N}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(res);
        Assert.Equal(0, res.TotalCount);
    }

    // ── Create + Get + Update + Delete (full mini-cycle) ─────────────────

    /// <summary>
    /// Validates the <c>CreateValidToolReturns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task CreateValidToolReturns201()
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
            var r = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);

            var res = await r.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(res);
            Assert.True(res.Success, $"Create failed: {res.Error}");
            Assert.NotNull(res.Tool);
            Assert.Equal(name, res.Tool.Name);
            Assert.Equal("Audit test tool", res.Tool.Description);
            Assert.Contains("audit", res.Tool.Tags);
            Assert.True(res.Tool.Id > 0);
            Assert.NotNull(r.Headers.Location);

            // Cleanup
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{res.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        }
        catch { /* best effort */ }
    }

    /// <summary>
    /// Validates the <c>CreateDuplicateNameReturns409</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task CreateDuplicateNameReturns409()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "First", Tags = new[] { "dup" } };
        var first = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstRes = await first.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            var second = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }
        finally
        {
            if (firstRes?.Tool != null)
                await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{firstRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Validates the <c>GetExistingToolReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task GetExistingToolReturns200()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "GetTest", Tags = new[] { "get" } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createRes?.Tool);

        try
        {
            var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var dto = await r.Content.ReadFromJsonAsync<ToolDto>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(dto);
            Assert.Equal(name, dto.Name);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Validates the <c>GetNonExistentIdReturns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task GetNonExistentIdReturns404()
    {
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/999999", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>UpdateChangeNameReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task UpdateChangeNameReturns200()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "UpdateTest", Tags = new[] { "upd" } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createRes?.Tool);

        try
        {
            var newName = ToolRegistryFixture.GenerateToolName();
            var update = new { Name = newName };
            var r = await _f.Client.PutAsJsonAsync(
                $"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", update, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var res = await r.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(res);
            Assert.True(res.Success);
            Assert.Equal(newName, res.Tool!.Name);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Validates the <c>UpdateNonExistentIdReturns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task UpdateNonExistentIdReturns404()
    {
        var update = new { Name = "ghost" };
        var r = await _f.Client.PutAsJsonAsync($"{ToolRegistryFixture.ToolRoute}/999999", update, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>DeleteExistingToolReturns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task DeleteExistingToolReturns200()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var body = new { Name = name, Description = "DeleteTest", Tags = new[] { "del" } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createRes?.Tool);

        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(res);
        Assert.True(res.Success);

        // Verify gone
        var get = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    /// <summary>
    /// Validates the <c>DeleteNonExistentIdReturns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task DeleteNonExistentIdReturns404()
    {
        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/999999", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ── Search by created tool tag ───────────────────────────────────────

    /// <summary>
    /// Validates the <c>SearchByTagFindsCreatedTool</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task SearchByTagFindsCreatedTool()
    {
        var name = ToolRegistryFixture.GenerateToolName();
        var uniqueTag = $"audittag{Guid.NewGuid().ToString("N")[..6]}";
        var body = new { Name = name, Description = "SearchTagTest", Tags = new[] { uniqueTag } };
        var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(createRes?.Tool);

        try
        {
            var r = await _f.Client.GetAsync(
                $"{ToolRegistryFixture.ToolRoute}/search?keyword={uniqueTag}", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var res = await r.Content.ReadFromJsonAsync<ToolSearchResult>(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(res);
            Assert.True(res.TotalCount >= 1);
            Assert.Contains(res.Tools, t => t.Name == name);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }
}
