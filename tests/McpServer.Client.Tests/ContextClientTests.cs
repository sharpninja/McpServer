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
}
