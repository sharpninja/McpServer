using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>TR-PLANNED-013: Context controller API tests.</summary>
public sealed class ContextControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ContextControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    /// <summary>GET /mcp/context/sources returns 200 and array.</summary>
    [Fact]
    public async Task GetSources_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcp/context/sources", UriKind.Relative)).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("sources", out var sources));
        Assert.Equal(JsonValueKind.Array, sources.ValueKind);
    }

    /// <summary>POST /mcp/context/pack returns 200 and ContextPack with empty chunks when query matches no content.</summary>
    [Fact]
    public async Task GetPack_ReturnsOk()
    {
        // Use a query that cannot match any chunk (avoids dependence on DB state / test order).
        var request = new { queryId = "test-1", query = "xyznonexistentquery123", limit = 10 };
        var response = await _client.PostAsJsonAsync(new Uri("/mcp/context/pack", UriKind.Relative), request).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();
        var pack = await response.Content.ReadFromJsonAsync<ContextPack>().ConfigureAwait(true);
        Assert.NotNull(pack);
        Assert.Equal("test-1", pack.QueryId);
        Assert.NotNull(pack.Chunks);
        Assert.Empty(pack.Chunks);
        Assert.NotNull(pack.SourceKeys);
        Assert.Empty(pack.SourceKeys);
    }

    /// <summary>POST /mcp/context/search returns 200.</summary>
    [Fact]
    public async Task Search_ReturnsOk()
    {
        var request = new { query = "test", limit = 5 };
        var response = await _client.PostAsJsonAsync(new Uri("/mcp/context/search", UriKind.Relative), request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>FR-SUPPORT-010: Same request produces identical chunk IDs and order (deterministic context pack).</summary>
    [Fact]
    public async Task GetPack_CalledTwiceWithSameRequest_ReturnsIdenticalChunkIdsAndOrder()
    {
        var docId = "det-test-doc-" + Guid.NewGuid().ToString("N");
        var chunkIdA = "det-chunk-a-" + Guid.NewGuid().ToString("N");
        var chunkIdB = "det-chunk-b-" + Guid.NewGuid().ToString("N");
        using (var scope = _factory.Server.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            db.Documents.Add(new ContextDocumentEntity
            {
                Id = docId,
                SourceType = "repo",
                SourceKey = "test/file.txt",
                ContentHash = "abc",
                IngestedAt = DateTime.UtcNow
            });
            db.Chunks.Add(new ContextChunkEntity
            {
                Id = chunkIdA,
                DocumentId = docId,
                Content = "deterministic pack test content",
                TokenCount = 5,
                ChunkIndex = 0
            });
            db.Chunks.Add(new ContextChunkEntity
            {
                Id = chunkIdB,
                DocumentId = docId,
                Content = "more content for pack",
                TokenCount = 4,
                ChunkIndex = 1
            });
            await db.SaveChangesAsync().ConfigureAwait(true);
        }

        var request = new { queryId = "det-query-1", query = "content", limit = 10 };
        var response1 = await _client.PostAsJsonAsync(new Uri("/mcp/context/pack", UriKind.Relative), request).ConfigureAwait(true);
        var response2 = await _client.PostAsJsonAsync(new Uri("/mcp/context/pack", UriKind.Relative), request).ConfigureAwait(true);
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        var pack1 = await response1.Content.ReadFromJsonAsync<ContextPack>().ConfigureAwait(true);
        var pack2 = await response2.Content.ReadFromJsonAsync<ContextPack>().ConfigureAwait(true);
        Assert.NotNull(pack1);
        Assert.NotNull(pack2);
        Assert.Equal(pack1.QueryId, pack2.QueryId);
        var ids1 = pack1.Chunks!.Select(c => c.Id).ToList();
        var ids2 = pack2.Chunks!.Select(c => c.Id).ToList();
        Assert.True(ids1.SequenceEqual(ids2), "Chunk IDs must be in the same order for same request (deterministic pack).");
    }
}
