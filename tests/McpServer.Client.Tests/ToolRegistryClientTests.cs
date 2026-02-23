using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class ToolRegistryClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7148"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task SearchAsync_IncludesKeyword()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"tools":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        await client.SearchAsync("lint");

        Assert.Contains("keyword=lint", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task InstallFromBucketAsync_IncludesToolName()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        await client.InstallFromBucketAsync("default", "my-tool", workspace: "/path");

        Assert.Contains("toolName=my-tool", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("workspace=", handler.LastRequest.RequestUri.Query);
        Assert.Contains("/install", handler.LastRequest.RequestUri.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task SyncBucketAsync_PostsCorrectly()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"updated":2,"added":1,"unchanged":5}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.SyncBucketAsync("default");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(2, result.Updated);
        Assert.Equal(1, result.Added);
    }
}
