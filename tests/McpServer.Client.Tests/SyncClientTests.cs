using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class SyncClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7148"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task RunAsync_PostsToSyncRun()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK,
            """{"runId":"r1","status":"completed","documentsIngested":5,"chunksWritten":20,"sessionLogsImported":1,"issuesSynced":3}""");
        using var http = new HttpClient(handler);
        var client = new SyncClient(http, DefaultOptions);

        var result = await client.RunAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(5, result.DocumentsIngested);
        Assert.Equal(20, result.ChunksWritten);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetStatusAsync_GetsStatus()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"status":"idle"}""");
        using var http = new HttpClient(handler);
        var client = new SyncClient(http, DefaultOptions);

        var result = await client.GetStatusAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("idle", result.Status);
    }
}
