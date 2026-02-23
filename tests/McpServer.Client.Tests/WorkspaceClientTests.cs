using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class WorkspaceClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7148"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task ListAsync_GetsWorkspaces()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task StartAsync_PostsToCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"isRunning":true,"port":7149}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.StartAsync("abc123");

        Assert.True(result.IsRunning);
        Assert.Contains("/mcp/workspace/abc123/start", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
