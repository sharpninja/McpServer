using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class TodoClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7148"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task QueryAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.QueryAsync(keyword: "auth", priority: "high");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("keyword=auth", handler.LastRequest.RequestUri!.Query);
        Assert.Contains("priority=high", handler.LastRequest.RequestUri.Query);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"MVP-001","title":"test","section":"s","priority":"high","done":false}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetAsync("MVP-001");

        Assert.Equal("MVP-001", result.Id);
        Assert.Contains("/mcp/todo/MVP-001", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_PostsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.CreateAsync(new Models.TodoCreateRequest
        {
            Id = "NEW-001",
            Title = "New item",
            Section = "test",
            Priority = "high"
        });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("NEW-001", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAsync_PutsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.UpdateAsync("MVP-001", new Models.TodoUpdateRequest { Done = true });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_SendsDeleteRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.DeleteAsync("MVP-001");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApiKeyHeader_IsSent()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        await client.QueryAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Contains("test-key", values!);
    }
}
