using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class GitHubClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
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
        Assert.Contains("/mcpserver/gh/issues/42", handler.LastRequest!.RequestUri!.AbsolutePath);
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

    [Fact]
    public async System.Threading.Tasks.Task CreateIssueAsync_PostsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"number":99,"url":"https://github.com/x/y/issues/99"}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.CreateIssueAsync(new Models.GitHubIssueRequest { Title = "Bug" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/issues", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(99, result.Number);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateIssueAsync_PutsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.UpdateIssueAsync(42, new Models.GitHubIssueUpdateRequest { Title = "Updated" });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/issues/42", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task ReopenIssueAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.ReopenIssueAsync(7);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/issues/7/reopen", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CommentOnIssueAsync_PostsBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.CommentOnIssueAsync(5, "Nice work!");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/issues/5/comments", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("Nice work!", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListLabelsAsync_GetsLabels()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"labels":[{"name":"bug","color":"d73a4a"}]}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.ListLabelsAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/labels", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result.Labels!);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListPullsAsync_GetsPulls()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"pulls":[{"number":1,"title":"PR","state":"open"}]}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.ListPullsAsync(state: "open");

        Assert.Contains("state=open", handler.LastRequest!.RequestUri!.Query);
        Assert.Single(result.Pulls);
    }

    [Fact]
    public async System.Threading.Tasks.Task CommentOnPullAsync_PostsBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.CommentOnPullAsync(3, "LGTM");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/pulls/3/comments", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("LGTM", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task SyncToGitHubAsync_PostsCorrectly()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"synced":2,"skipped":0,"failed":0,"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.SyncToGitHubAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/to-github", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(2, result.Synced);
    }

    [Fact]
    public async System.Threading.Tasks.Task SyncIssueAsync_PostsWithDirection()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"todoId":"GH-10"}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.SyncIssueAsync(10, "from-github");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/issues/10/sync", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("direction=from-github", handler.LastRequest.RequestUri.Query);
        Assert.True(result.Success);
    }
}
