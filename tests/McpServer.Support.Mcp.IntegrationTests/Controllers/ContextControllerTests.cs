using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>TR-PLANNED-CORE-013: Context controller API tests.</summary>
[Trait("Category", "Integration")]
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

    /// <summary>
    /// Regression test for website ingest HttpClient registration.
    /// </summary>
    [Fact]
    public void WebsiteIngestorHttpClient_CanBeCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient(WebsiteIngestor.HttpClientName);
        Assert.NotNull(client);
    }

    /// <summary>GET /mcpserver/context/sources returns 200 and array.</summary>
    [Fact]
    public async Task GetSources_ReturnsOk()
    {
        var response = await _client.GetAsync(new Uri("/mcpserver/context/sources", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("sources", out var sources));
        Assert.Equal(JsonValueKind.Array, sources.ValueKind);
    }

    /// <summary>POST /mcpserver/context/pack returns 200 and ContextPack with empty chunks when query matches no content.</summary>
    [Fact]
    public async Task GetPack_ReturnsOk()
    {
        // Use a query that cannot match any chunk (avoids dependence on DB state / test order).
        var request = new { queryId = "test-1", query = "xyznonexistentquery123", limit = 10 };
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/context/pack", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();
        var pack = await response.Content.ReadFromJsonAsync<ContextPack>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(pack);
        Assert.Equal("test-1", pack.QueryId);
        Assert.NotNull(pack.Chunks);
        Assert.Empty(pack.Chunks);
        Assert.NotNull(pack.SourceKeys);
        Assert.Empty(pack.SourceKeys);
    }

    /// <summary>POST /mcpserver/context/search returns 200.</summary>
    [Fact]
    public async Task Search_ReturnsOk()
    {
        var request = new { query = "test", limit = 5 };
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/context/search", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhenGraphRagEnabled_ReturnsGraphMetadata()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Mcp:GraphRag:Enabled"] = "true",
                    ["Mcp:GraphRag:EnhanceContextSearch"] = "true"
                });
            });
        });
        using var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var response = await client.PostAsJsonAsync(new Uri("/mcpserver/context/search", UriKind.Relative), new { query = "test", limit = 5 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("graphRag", out var graphRag));
        Assert.True(graphRag.TryGetProperty("backend", out _));
    }

    [Fact]
    public async Task Search_WhenGraphRagEnabledAndQueryEmpty_UsesLegacyPathReason()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Mcp:GraphRag:Enabled"] = "true",
                    ["Mcp:GraphRag:EnhanceContextSearch"] = "true"
                });
            });
        });
        using var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var response = await client.PostAsJsonAsync(
            new Uri("/mcpserver/context/search", UriKind.Relative),
            new { query = string.Empty, limit = 5 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("graphRag", out var graphRag));
        Assert.True(graphRag.TryGetProperty("backend", out var backend));
        Assert.Equal("context-search", backend.GetString());
        Assert.True(graphRag.TryGetProperty("reason", out var reason));
        Assert.Equal("empty_query_forces_legacy_path", reason.GetString());
    }

    [Fact]
    public async Task Search_WhenGraphRagEnabledAndSourceTypeProvided_UsesLegacyPathReason()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Mcp:GraphRag:Enabled"] = "true",
                    ["Mcp:GraphRag:EnhanceContextSearch"] = "true"
                });
            });
        });
        using var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var response = await client.PostAsJsonAsync(
            new Uri("/mcpserver/context/search", UriKind.Relative),
            new { query = "test", limit = 5, sourceType = "repo" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("graphRag", out var graphRag));
        Assert.True(graphRag.TryGetProperty("reason", out var reason));
        Assert.Equal("sourceType_filter_forces_legacy_path", reason.GetString());
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
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var request = new { queryId = "det-query-1", query = "content", limit = 10 };
        var response1 = await _client.PostAsJsonAsync(new Uri("/mcpserver/context/pack", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var response2 = await _client.PostAsJsonAsync(new Uri("/mcpserver/context/pack", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        var pack1 = await response1.Content.ReadFromJsonAsync<ContextPack>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var pack2 = await response2.Content.ReadFromJsonAsync<ContextPack>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(pack1);
        Assert.NotNull(pack2);
        Assert.Equal(pack1.QueryId, pack2.QueryId);
        var ids1 = pack1.Chunks!.Select(c => c.Id).ToList();
        var ids2 = pack2.Chunks!.Select(c => c.Id).ToList();
        Assert.True(ids1.SequenceEqual(ids2), "Chunk IDs must be in the same order for same request (deterministic pack).");
    }

    [Fact]
    public async Task IngestWebsite_MissingUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/context/ingest-website", UriKind.Relative), new { includeSubpages = true }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IngestWebsite_Success_UpsertsSourcesAndDeduplicatesBySourceKey()
    {
        var fakeIngestor = new StubWebsiteIngestor();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWebsiteIngestor>();
                services.AddScoped<IWebsiteIngestor>(_ => fakeIngestor);
            });
        });

        using var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var request = new { url = "https://example.com/docs", maxPages = 1, maxDepth = 0, maxBytesPerPage = 12000 };
        var response1 = await client.PostAsJsonAsync(new Uri("/mcpserver/context/ingest-website", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response1.EnsureSuccessStatusCode();

        var response2 = await client.PostAsJsonAsync(new Uri("/mcpserver/context/ingest-website", UriKind.Relative), request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response2.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            var docs = await db.Documents.IgnoreQueryFilters().Where(d => d.SourceType == "external-web" && d.SourceKey == "https://example.com/docs").ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Single(docs);
            Assert.Equal("HASH-2", docs[0].ContentHash);
        }

        var sourcesResponse = await client.GetAsync(new Uri("/mcpserver/context/sources", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        sourcesResponse.EnsureSuccessStatusCode();
        var sourcesJson = await sourcesResponse.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("https://example.com/docs", sourcesJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IngestWebsite_WhenFetcherReturnsErrors_ReturnsPartialFailureStatus()
    {
        var fakeIngestor = new ErrorWebsiteIngestor();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWebsiteIngestor>();
                services.AddScoped<IWebsiteIngestor>(_ => fakeIngestor);
            });
        });

        using var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var response = await client.PostAsJsonAsync(
            new Uri("/mcpserver/context/ingest-website", UriKind.Relative),
            new { url = "https://example.com/error", maxPages = 1 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("partial-failure", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("error", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestWebsiteStream_ReturnsSsePayloadWithResultEvent()
    {
        var fakeIngestor = new StubWebsiteIngestor();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWebsiteIngestor>();
                services.AddScoped<IWebsiteIngestor>(_ => fakeIngestor);
            });
        });

        using var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var response = await client.PostAsJsonAsync(
            new Uri("/mcpserver/context/ingest-website/stream", UriKind.Relative),
            new { url = "https://example.com/docs", maxPages = 1 }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("event: started", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("event: result", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"documentsIngested\":1", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubWebsiteIngestor : IWebsiteIngestor
    {
        private readonly string _documentId = "external-web:test-doc-" + Guid.NewGuid().ToString("N");
        private readonly string _chunkIdSuffix = Guid.NewGuid().ToString("N");
        private int _count;

        public Task<IReadOnlyList<WebsiteIngestPage>> IngestAsync(
            WebsiteIngestRequest request,
            Func<WebsiteIngestPage, Task>? onPageFetched = null,
            CancellationToken cancellationToken = default)
        {
            _count++;
            var doc = new ContextDocument
            {
                Id = _documentId,
                SourceType = "external-web",
                SourceKey = "https://example.com/docs",
                IngestedAt = DateTime.UtcNow,
                ContentHash = _count == 1 ? "HASH-1" : "HASH-2"
            };

            IReadOnlyList<ContextChunk> chunks =
            [
                new ContextChunk
                {
                    Id = _count == 1 ? $"chunk-1-{_chunkIdSuffix}" : $"chunk-2-{_chunkIdSuffix}",
                    DocumentId = _documentId,
                    Content = _count == 1 ? "first" : "second",
                    TokenCount = 1,
                    ChunkIndex = 0
                }
            ];

            IReadOnlyList<WebsiteIngestPage> pages =
            [
                new WebsiteIngestPage
                {
                    Url = request.Url,
                    Document = doc,
                    Chunks = chunks,
                    Outcome = new WebsiteIngestUrlResult
                    {
                        Url = request.Url,
                        Status = "ingested",
                        SourceKey = "https://example.com/docs",
                        ChunksWritten = 1
                    }
                }
            ];

            return Task.FromResult(pages);
        }
    }

    private sealed class ErrorWebsiteIngestor : IWebsiteIngestor
    {
        public Task<IReadOnlyList<WebsiteIngestPage>> IngestAsync(
            WebsiteIngestRequest request,
            Func<WebsiteIngestPage, Task>? onPageFetched = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WebsiteIngestPage> pages =
            [
                new WebsiteIngestPage
                {
                    Url = request.Url,
                    Outcome = new WebsiteIngestUrlResult
                    {
                        Url = request.Url,
                        Status = "error",
                        Message = "blocked"
                    }
                }
            ];

            return Task.FromResult(pages);
        }
    }
}
