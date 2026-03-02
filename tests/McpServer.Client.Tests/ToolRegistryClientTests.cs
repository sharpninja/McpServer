using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class ToolRegistryClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
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

    [Fact]
    public async System.Threading.Tasks.Task ListAsync_GetsTools()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"tools":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListAsync_WithWorkspace_IncludesQueryParam()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"tools":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        await client.ListAsync(workspace: "/proj");

        Assert.Contains("workspace=", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAsync_GetsToolById()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":5,"name":"lint","description":"Linter","tags":[]}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.GetAsync(5);

        Assert.Contains("/mcpserver/tools/5", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("lint", result.Name);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_PostsTool()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.CreateAsync(new Models.ToolCreateRequest { Name = "new-tool", Description = "d" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/tools", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAsync_PutsTool()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.UpdateAsync(3, new Models.ToolUpdateRequest { Description = "updated" });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/tools/3", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_DeletesTool()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.DeleteAsync(3);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/tools/3", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListBucketsAsync_GetsBuckets()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"buckets":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.ListBucketsAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/tools/buckets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddBucketAsync_PostsBucket()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.AddBucketAsync(new Models.BucketAddRequest { Name = "b", Owner = "o", Repo = "r", Branch = "main", ManifestPath = "m" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteBucketAsync_DeletesCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.DeleteBucketAsync("default", uninstallTools: true);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/tools/buckets/default", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("uninstallTools=true", handler.LastRequest.RequestUri.Query);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task BrowseBucketAsync_GetsTools()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"tools":[{"name":"t","description":"d","tags":[],"manifestFile":"m"}]}""");
        using var http = new HttpClient(handler);
        var client = new ToolRegistryClient(http, DefaultOptions);

        var result = await client.BrowseBucketAsync("default");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/tools/buckets/default/browse", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
        Assert.Single(result.Tools!);
    }
}
