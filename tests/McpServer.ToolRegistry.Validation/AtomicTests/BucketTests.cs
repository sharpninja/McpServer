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
    public BucketTests(ToolRegistryFixture f) => _f = f;

    // ── List Buckets ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListBuckets_Returns200WithValidStructure()
    {
        var r = await _f.Client.GetAsync(ToolRegistryFixture.BucketRoute);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<BucketListResult>();
        Assert.NotNull(res);
        Assert.NotNull(res.Buckets);
        Assert.True(res.TotalCount >= 0);
    }

    // ── Add Bucket ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddBucket_ValidRequest_Returns201()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer", Branch = "main", ManifestPath = "/tools" };

        var r = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var res = await r.Content.ReadFromJsonAsync<BucketMutationResult>();
        Assert.NotNull(res);
        Assert.True(res.Success, $"Add bucket failed: {res.Error}");
        Assert.NotNull(res.Bucket);
        Assert.Equal(name, res.Bucket.Name);
        Assert.Equal("sharpninja", res.Bucket.Owner);
        Assert.Equal("McpServer", res.Bucket.Repo);

        // Cleanup
        await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}");
    }

    [Fact]
    public async Task AddBucket_Duplicate_Returns409()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer" };
        var first = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        try
        {
            var second = await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}");
        }
    }

    // ── Remove Bucket ────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveBucket_Existing_Returns200()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer" };
        await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body);

        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var res = await r.Content.ReadFromJsonAsync<BucketMutationResult>();
        Assert.NotNull(res);
        Assert.True(res.Success);
    }

    [Fact]
    public async Task RemoveBucket_NonExistent_Returns404()
    {
        var r = await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ── Browse Bucket ────────────────────────────────────────────────────

    [Fact]
    public async Task BrowseBucket_NonExistent_Returns404()
    {
        var r = await _f.Client.GetAsync($"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/browse");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task BrowseBucket_Existing_ReturnsResult()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer", ManifestPath = "/tools" };
        await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body);

        try
        {
            var r = await _f.Client.GetAsync($"{ToolRegistryFixture.BucketRoute}/{name}/browse");
            // May be 200 (found manifests) or 404 (no manifests in path).
            Assert.True(
                r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NotFound,
                $"Expected 200/404 but got {(int)r.StatusCode}.");
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}");
        }
    }

    // ── Install from Bucket ──────────────────────────────────────────────

    [Fact]
    public async Task InstallFromBucket_NonExistentBucket_Returns404()
    {
        var r = await _f.Client.PostAsync(
            $"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/install?toolName=foo", null);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ── Sync Bucket ──────────────────────────────────────────────────────

    [Fact]
    public async Task SyncBucket_NonExistentBucket_Returns404()
    {
        var r = await _f.Client.PostAsync(
            $"{ToolRegistryFixture.BucketRoute}/nonexistent-{Guid.NewGuid():N}/sync", null);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task SyncBucket_Existing_Returns200Or404()
    {
        var name = ToolRegistryFixture.GenerateBucketName();
        var body = new { Name = name, Owner = "sharpninja", Repo = "McpServer", ManifestPath = "/tools" };
        await _f.Client.PostAsJsonAsync(ToolRegistryFixture.BucketRoute, body);

        try
        {
            var r = await _f.Client.PostAsync($"{ToolRegistryFixture.BucketRoute}/{name}/sync", null);
            // 200 if manifests found, 404 if manifest path doesn't exist in repo
            Assert.True(
                r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NotFound,
                $"Expected 200/404 but got {(int)r.StatusCode}.");
        }
        finally
        {
            await _f.Client.DeleteAsync($"{ToolRegistryFixture.BucketRoute}/{name}");
        }
    }
}
