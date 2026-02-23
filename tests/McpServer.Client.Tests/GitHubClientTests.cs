using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class GitHubClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7148")
    };

    [Fact]
    public async System.Threading.Tasks.Task ListIssuesAsync_SendsCorrectQueryParams()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"issues":[]}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.ListIssuesAsync(state: "open", limit: 10);

        Assert.Contains("state=open", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("limit=10", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetIssueAsync_GetsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"number":42,"title":"Bug"}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.GetIssueAsync(42);

        Assert.Equal(42, result.Number);
        Assert.Contains("/mcp/gh/issues/42", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CloseIssueAsync_IncludesReason()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.CloseIssueAsync(1, reason: "completed");

        Assert.Contains("reason=completed", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("/close", handler.LastRequest.RequestUri.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task SyncFromGitHubAsync_PostsCorrectly()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"synced":5,"skipped":0,"failed":0,"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.SyncFromGitHubAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(5, result.Synced);
    }
}
