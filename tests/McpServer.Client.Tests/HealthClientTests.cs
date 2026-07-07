using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class HealthClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147")
    };

    [Fact]
    public async System.Threading.Tasks.Task GetAsync_GetsHealthWithoutApiKey()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"status":"Healthy","version":"1.0.0","checks":[]}""");
        using var http = new HttpClient(handler);
        var client = new HealthClient(http, DefaultOptions);

        var result = await client.GetAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/health", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.False(handler.LastRequest.Headers.Contains("X-Api-Key"));
        Assert.Equal("Healthy", result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAliveAndReadyAsync_GetExpectedEndpoints()
    {
        var aliveHandler = new MockHttpHandler(HttpStatusCode.OK, """{"status":"Healthy","checks":[]}""");
        using var aliveHttp = new HttpClient(aliveHandler);
        var aliveClient = new HealthClient(aliveHttp, DefaultOptions);

        var alive = await aliveClient.GetAliveAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", alive.Status);
        Assert.Contains("/alive", aliveHandler.LastRequest!.RequestUri!.AbsolutePath);

        var readyHandler = new MockHttpHandler(HttpStatusCode.OK, """{"status":"Healthy","checks":[{"name":"db","status":"Healthy"}]}""");
        using var readyHttp = new HttpClient(readyHandler);
        var readyClient = new HealthClient(readyHttp, DefaultOptions);

        var ready = await readyClient.GetReadyAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", ready.Status);
        Assert.Contains("/ready", readyHandler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("db", Assert.Single(ready.Checks).Name);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetServerStartupAsync_GetsStartupEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"serverStartedAtUtc":"2026-06-25T12:00:00Z","nowUtc":"2026-06-25T12:01:00Z","processId":4242,"workspace":null,"port":7147}""");
        using var http = new HttpClient(handler);
        var client = new HealthClient(http, DefaultOptions);

        var result = await client.GetServerStartupAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/server-startup-utc", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(4242, result.ProcessId);
        Assert.Equal(7147, result.Port);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetMarkerFileTimestampAsync_EncodesRepoPath()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"repoPath":"F:\\GitHub\\McpServer","markerPath":"F:\\GitHub\\McpServer\\AGENTS-README-FIRST.yaml","exists":true,"lastWriteTimeUtc":"2026-06-25T12:00:00Z","creationTimeUtc":"2026-06-25T11:59:00Z","length":1024}""");
        using var http = new HttpClient(handler);
        var client = new HealthClient(http, DefaultOptions);

        var result = await client.GetMarkerFileTimestampAsync(@"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/marker-file-timestamp", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("repoPath=F%3A%5CGitHub%5CMcpServer", handler.LastRequest.RequestUri.Query);
        Assert.True(result.Exists);
        Assert.Equal(1024, result.Length);
    }
}
