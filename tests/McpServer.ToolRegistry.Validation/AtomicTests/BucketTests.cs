using System.Net;
using System.Net.Http.Json;
using McpServer.ToolRegistry.Validation.Models;
using Xunit;

namespace McpServer.ToolRegistry.Validation.AtomicTests;

/// <summary>Audit: Bucket management endpoints — List, Add, Remove, Browse, Install, Sync.</summary>
[Collection("ToolRegistry")]
public sealed class BucketTests
{
    private readonly ToolRegistryFixture _f;
    /// <summary>
    /// Initializes a new instance of BucketTests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    public BucketTests(ToolRegistryFixture f) => _f = f;

    // ── List Buckets ─────────────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>ListBuckets_Returns200WithValidStructure</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task ListBuckets_Returns200WithValidStructure()
    {
        var r = await _f.Client.GetAsync(ToolRegistryFixture.BucketRoute, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<BucketListResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(res);
        Assert.NotNull(res.Buckets);
        Assert.True(res.TotalCount >= 0);
    }

    // ── Add Bucket ───────────────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>AddBucket_ValidRequest_Returns201</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task AddBucket_ValidRequest_Returns201()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer", Branch = "main", ManifestPath = "/tools" };

        var r = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var res = await r.Content.ReadFromJsonAsync<BucketMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(res);
        Assert.True(res.Success, $"Add bucket failed: {res.Error}");
        Assert.NotNull(res.Bucket);
        Assert.Equal(name, res.Bucket.Name);
        Assert.Equal("sharpninja", res.Bucket.Owner);
        Assert.Equal("McpServer", res.Bucket.Repo);

        // Cleanup
        await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}", cancellationToken: TestContext.Current.CancellationToken);
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

    // ── Remove Bucket ────────────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>RemoveBucket_Existing_Returns200</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task RemoveBucket_Existing_Returns200()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer" };
        await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body, cancellationToken: TestContext.Current.CancellationToken);

        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<BucketMutationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(res);
        Assert.True(res.Success);
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

    // ── Browse Bucket ────────────────────────────────────────────────────

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
    /// Validates the <c>BrowseBucket_Existing_ReturnsResult</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task BrowseBucket_Existing_ReturnsResult()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer", ManifestPath = "/tools" };
        await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body, cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            var r = await _f.Client.GetAsync($"{ToolRegistryFixture.BucketRoute}/{name}/browse", cancellationToken: TestContext.Current.CancellationToken);
            // May be 200 (found manifests) or 404 (no manifests in path).
            Assert.True(
                r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NotFound,
                $"Expected 200/404 but got {(int)r.StatusCode}.");
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    // ── Install from Bucket ──────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>InstallFromBucket_NonExistentBucket_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task InstallFromBucket_NonExistentBucket_Returns404()
    {
        var r = await _f.Client.PostAsync(
            $"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/install?toolName=foo", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ── Sync Bucket ──────────────────────────────────────────────────────

    /// <summary>
    /// Validates the <c>SyncBucket_NonExistentBucket_Returns404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task SyncBucket_NonExistentBucket_Returns404()
    {
        var r = await _f.Client.PostAsync(
            $"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/sync", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    /// <summary>
    /// Validates the <c>SyncBucket_Existing_Returns200Or404</c> scenario.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-008, FR-MCP-012, TR-MCP-TR-001, TR-MCP-TR-002, TR-MCP-TR-003.
    /// Test data: Generated tool/bucket names and CRUD/search/browse/sync payload objects for registry endpoints.
    /// Data rationale: These inputs verify tool-registry bucket/tool lifecycle endpoints and search/sync behavior.
    /// </remarks>
    [Fact]
    public async Task SyncBucket_Existing_Returns200Or404()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer", ManifestPath = "/tools" };
        await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body, cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            var r = await _f.Client.PostAsync($"{ToolRegistryFixture.BucketRoute}/{name}/sync", null, cancellationToken: TestContext.Current.CancellationToken);
            // 200 if manifests found, 404 if manifest path doesn't exist in repo
            Assert.True(
                r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NotFound,
                $"Expected 200/404 but got {(int)r.StatusCode}.");
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}", cancellationToken: TestContext.Current.CancellationToken);
        }
    }
}
