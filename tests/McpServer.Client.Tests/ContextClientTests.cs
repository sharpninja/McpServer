using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class ContextClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task SearchAsync_PostsSearchRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"query":"auth","chunks":[],"sourceKeys":[]}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.SearchAsync("auth", limit: 5);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/context/search", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("auth", handler.LastRequestBody!);
        Assert.Equal("auth", result.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task PackAsync_PostsPackRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"queryId":"q1","chunks":[],"sourceKeys":[]}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.PackAsync("auth", queryId: "q1");

        Assert.Equal("q1", result.QueryId);
        Assert.Contains("q1", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListSourcesAsync_GetsSourcesList()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"sources":[{"sourceKey":"k","sourceType":"repo"}]}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.ListSourcesAsync();

        Assert.Single(result.Sources);
        Assert.Equal("repo", result.Sources[0].SourceType);
    }

    [Fact]
    public async System.Threading.Tasks.Task RebuildIndexAsync_PostsCorrectly()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"status":"completed"}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.RebuildIndexAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/context/rebuild-index", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("completed", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task GraphRagStatusAsync_GetsStatus()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"workspacePath":"E:/repo","graphRoot":"E:/repo/mcp-data/graphrag","state":"ready","isInitialized":true,"isIndexed":true,"artifactVersion":"v1","backend":"internal-fallback"}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.GraphRagStatusAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/graphrag/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.IsInitialized);
        Assert.True(result.IsIndexed);
        Assert.Equal("ready", result.State);
        Assert.Equal("v1", result.ArtifactVersion);
    }

    [Fact]
    public async System.Threading.Tasks.Task GraphRagIndexAsync_PostsRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"isInitialized":true,"isIndexed":true,"backend":"internal-fallback"}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.GraphRagIndexAsync(force: true);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/graphrag/index", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"force\":true", handler.LastRequestBody!);
        Assert.True(result.IsIndexed);
    }

    [Fact]
    public async System.Threading.Tasks.Task GraphRagQueryAsync_PostsQueryRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"query":"auth","mode":"local","answer":"ok","citations":[],"chunks":[],"sourceKeys":[],"entities":["AuthService"],"relationships":["AuthService -> TokenStore"],"communities":["auth (1)"],"fallbackUsed":true,"fallbackReason":"graphrag_not_indexed","backend":"internal-fallback"}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.GraphRagQueryAsync("auth", mode: "local", maxChunks: 10, maxEntities: 5, maxRelationships: 5, communityDepth: 2, responseTokenBudget: 1024);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/graphrag/query", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"query\":\"auth\"", handler.LastRequestBody!);
        Assert.Contains("\"maxEntities\":5", handler.LastRequestBody!);
        Assert.Contains("\"maxRelationships\":5", handler.LastRequestBody!);
        Assert.Contains("\"communityDepth\":2", handler.LastRequestBody!);
        Assert.Contains("\"responseTokenBudget\":1024", handler.LastRequestBody!);
        Assert.Equal("auth", result.Query);
        Assert.Equal("local", result.Mode);
        Assert.Equal("graphrag_not_indexed", result.FallbackReason);
        Assert.Single(result.Entities);
    }

    [Fact]
    public async System.Threading.Tasks.Task IngestWebsiteAsync_PostsTypedRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"runId":"r1","status":"completed","documentsIngested":1,"chunksWritten":2,"urlResults":[{"url":"https://example.com","status":"ingested","chunksWritten":2}],"graphRagIndexed":false}""");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var result = await client.IngestWebsiteAsync("https://example.com", includeSubpages: true, maxPages: 5);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/context/ingest-website", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"url\":\"https://example.com\"", handler.LastRequestBody!);
        Assert.Contains("\"includeSubpages\":true", handler.LastRequestBody!);
        Assert.Equal("completed", result.Status);
        Assert.Single(result.UrlResults);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamIngestWebsiteAsync_PostsRequestAndYieldsDataLines()
    {
        var sse = "data: {\"event\":\"started\"}\n\ndata: {\"event\":\"completed\"}\n\nevent: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new ContextClient(http, DefaultOptions);

        var lines = new System.Collections.Generic.List<string>();
        await foreach (var line in client.StreamIngestWebsiteAsync(new Models.WebsiteIngestRequest
        {
            Url = "https://example.com",
            IncludeSubpages = true,
            MaxPages = 2
        }))
        {
            lines.Add(line);
        }

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/context/ingest-website/stream", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"url\":\"https://example.com\"", handler.LastRequestBody!);
        Assert.Contains("\"includeSubpages\":true", handler.LastRequestBody!);
        Assert.Equal(2, lines.Count);
        Assert.Contains("started", lines[0]);
        Assert.Contains("completed", lines[1]);
    }
}
