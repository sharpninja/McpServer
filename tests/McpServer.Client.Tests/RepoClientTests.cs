using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class RepoClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task ReadFileAsync_EncodesPath()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"path":"src/main.cs","content":"code","exists":true}""");
        using var http = new HttpClient(handler);
        var client = new RepoClient(http, DefaultOptions);

        var result = await client.ReadFileAsync("src/main.cs");

        Assert.True(result.Exists);
        Assert.Contains("path=", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task WriteFileAsync_PostsContent()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"path":"test.txt","written":true}""");
        using var http = new HttpClient(handler);
        var client = new RepoClient(http, DefaultOptions);

        var result = await client.WriteFileAsync("test.txt", "content");

        Assert.True(result.Written);
        Assert.Contains("test.txt", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListAsync_WithoutPath_OmitsQueryString()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"path":null,"entries":[]}""");
        using var http = new HttpClient(handler);
        var client = new RepoClient(http, DefaultOptions);

        await client.ListAsync();

        Assert.DoesNotContain("path=", handler.LastRequest!.RequestUri!.ToString());
    }
}
