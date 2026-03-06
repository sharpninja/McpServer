using System.Net;
using System.Net.Http.Json;
using McpServer.ToolRegistry.Validation.Models;
using Xunit;

namespace McpServer.ToolRegistry.Validation.SequenceTests;

/// <summary>Audit: Full tool + bucket lifecycle as a single scripted flow.</summary>
[Collection("ToolRegistry")]
public sealed class ToolLifecycleSequenceTests
{
    private readonly ToolRegistryFixture _f;
    private readonly ITestOutputHelper _out;

    /// <summary>
    /// Initializes a new instance of ToolLifecycleSequenceTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    public ToolLifecycleSequenceTests(ToolRegistryFixture f, ITestOutputHelper o) { _f = f; _out = o; }

    /// <summary>
    /// Validates the <c>FullToolLifecycle_CreateThroughDelete</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task FullToolLifecycle_CreateThroughDelete()
    {
        var toolName = ToolRegistryFixture.GenerateToolName();
        var uniqueTag = $"lifecycle{Guid.NewGuid().ToString("N")[..6]}";
        int toolId = 0;

        try
        {
            // 1. List baseline
            _out.WriteLine("1. GET /mcpserver/tools — baseline");
            var list = await _f.Client.GetFromJsonAsync<ToolSearchResult>(ToolRegistryFixture.ToolRoute);
            Assert.NotNull(list);
            var baseline = list.TotalCount;
            _out.WriteLine($"  Baseline: {baseline}");

            // 2. Create
            _out.WriteLine("2. POST /mcpserver/tools — create");
            var createBody = new
            {
                Name = toolName, Description = "Lifecycle audit tool",
                Tags = new[] { uniqueTag, "lifecycle" },
                CommandTemplate = "echo {{param1}}",
                ParameterSchema = "{\"type\":\"object\",\"properties\":{\"param1\":{\"type\":\"string\"}}}"
            };
            var create = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.ToolRoute, createBody);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var createRes = await create.Content.ReadFromJsonAsync<ToolMutationResult>();
            Assert.NotNull(createRes); Assert.True(createRes.Success);
            toolId = createRes.Tool!.Id;
            _out.WriteLine($"  Created id={toolId}");

            // 3. List +1
            _out.WriteLine("3. GET /mcpserver/tools — verify +1");
            var list2 = await _f.Client.GetFromJsonAsync<ToolSearchResult>(ToolRegistryFixture.ToolRoute);
            Assert.Equal(baseline + 1, list2!.TotalCount);

            // 4. Get by ID
            _out.WriteLine("4. GET /mcpserver/tools/{id}");
            var get = await _f.Client.GetFromJsonAsync<ToolDto>($"{ToolRegistryFixture.ToolRoute}/{toolId}");
            Assert.NotNull(get);
            Assert.Equal(toolName, get.Name);
            Assert.Contains(uniqueTag, get.Tags);

            // 5. Search by tag
            _out.WriteLine("5. GET /mcpserver/tools/search?keyword={tag}");
            var search = await _f.Client.GetFromJsonAsync<ToolSearchResult>(
                $"{ToolRegistryFixture.ToolRoute}/search?keyword={uniqueTag}");
            Assert.NotNull(search);
            Assert.Contains(search.Tools, t => t.Id == toolId);

            // 6. Update
            _out.WriteLine("6. PUT /mcpserver/tools/{id} — update description");
            var upd = await _f.Client.PutAsJsonAsync(
                $"{ToolRegistryFixture.ToolRoute}/{toolId}",
                new { Description = "Updated lifecycle tool" });
            Assert.Equal(HttpStatusCode.OK, upd.StatusCode);
            var updRes = await upd.Content.ReadFromJsonAsync<ToolMutationResult>();
            Assert.True(updRes!.Success);
            Assert.Equal("Updated lifecycle tool", updRes.Tool!.Description);

            // 7. Get verify update
            _out.WriteLine("7. GET /mcpserver/tools/{id} — verify update");
            var get2 = await _f.Client.GetFromJsonAsync<ToolDto>($"{ToolRegistryFixture.ToolRoute}/{toolId}");
            Assert.Equal("Updated lifecycle tool", get2!.Description);

            // 8. Delete
            _out.WriteLine("8. DELETE /mcpserver/tools/{id}");
            var del = await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{toolId}");
            Assert.Equal(HttpStatusCode.OK, del.StatusCode);
            toolId = 0;

            // 9. Verify 404
            _out.WriteLine("9. GET /mcpserver/tools/{id} — verify 404");
            var gone = await _f.Client.GetAsync($"{ToolRegistryFixture.ToolRoute}/{createRes.Tool.Id}");
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

            // 10. List restored
            _out.WriteLine("10. GET /mcpserver/tools — verify count restored");
            var listFinal = await _f.Client.GetFromJsonAsync<ToolSearchResult>(ToolRegistryFixture.ToolRoute);
            Assert.Equal(baseline, listFinal!.TotalCount);

            _out.WriteLine("✅ Tool lifecycle complete!");
        }
        catch
        {
            if (toolId > 0) try { await _f.Client.DeleteAsync($"{ToolRegistryFixture.ToolRoute}/{toolId}"); } catch { }
            throw;
        }
    }

    /// <summary>
    /// Validates the <c>FullBucketLifecycle_AddThroughRemove</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task FullBucketLifecycle_AddThroughRemove()
    {
        var bucketName = ToolRegistryFixture.GenerateBucketName();

        try
        {
            // 1. List baseline
            _out.WriteLine("1. GET /mcpserver/tools/buckets — baseline");
            var list = await _f.Client.GetFromJsonAsync<BucketListResult>(ToolRegistryFixture.BucketRoute);
            Assert.NotNull(list);
            var baseline = list.TotalCount;

            // 2. Add
            _out.WriteLine("2. POST /mcpserver/tools/buckets — add");
            var body = new { Name = bucketName, Owner = "sharpninja", Repo = "McpServer", Branch = "main", ManifestPath = "/tools" };
            var add = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body);
            Assert.Equal(HttpStatusCode.Created, add.StatusCode);

            // 3. List +1
            _out.WriteLine("3. GET /mcpserver/tools/buckets — verify +1");
            var list2 = await _f.Client.GetFromJsonAsync<BucketListResult>(ToolRegistryFixture.BucketRoute);
            Assert.Equal(baseline + 1, list2!.TotalCount);

            // 4. Sync (200 if manifests exist, 404 if path has no manifests)
            _out.WriteLine("4. POST /mcpserver/tools/buckets/{name}/sync");
            var sync = await _f.Client.PostAsync($"{ToolRegistryFixture.BucketRoute}/{bucketName}/sync", null);
            Assert.True(
                sync.StatusCode == HttpStatusCode.OK || sync.StatusCode == HttpStatusCode.NotFound,
                $"Expected 200/404 but got {(int)sync.StatusCode}.");

            // 5. Browse
            _out.WriteLine("5. GET /mcpserver/tools/buckets/{name}/browse");
            var browse = await _f.Client.GetAsync($"{ToolRegistryFixture.BucketRoute}/{bucketName}/browse");
            Assert.True(browse.StatusCode == HttpStatusCode.OK || browse.StatusCode == HttpStatusCode.NotFound);

            // 6. Remove
            _out.WriteLine("6. DELETE /mcpserver/tools/buckets/{name}");
            var rem = await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{bucketName}");
            Assert.Equal(HttpStatusCode.OK, rem.StatusCode);

            // 7. List restored
            _out.WriteLine("7. GET /mcpserver/tools/buckets — verify count restored");
            var listFinal = await _f.Client.GetFromJsonAsync<BucketListResult>(ToolRegistryFixture.BucketRoute);
            Assert.Equal(baseline, listFinal!.TotalCount);

            _out.WriteLine("✅ Bucket lifecycle complete!");
        }
        catch
        {
            try { await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{bucketName}"); } catch { }
            throw;
        }
    }
}
