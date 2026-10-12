using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace QBrainAi.Client.Tests;

public sealed class RepoClientTests
{
    private static readonly QBrainAiClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task ReadFileAsyncEncodesPath()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"path":"src/main.cs","content":"code","exists":true}""");
        using var http = new HttpClient(handler);
        var client = new RepoClient(http, DefaultOptions);

        var result = await client.ReadFileAsync("src/main.cs", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Exists);
        Assert.Contains("path=", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task WriteFileAsyncPostsContent()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"path":"test.txt","written":true}""");
        using var http = new HttpClient(handler);
        var client = new RepoClient(http, DefaultOptions);

        var result = await client.WriteFileAsync("test.txt", "content", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Written);
        Assert.Contains("test.txt", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListAsyncWithoutPathOmitsQueryString()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"path":null,"entries":[]}""");
        using var http = new HttpClient(handler);
        var client = new RepoClient(http, DefaultOptions);

        await client.ListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain("path=", handler.LastRequest!.RequestUri!.ToString());
    }
}
