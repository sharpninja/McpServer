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

        await client.ListIssuesAsync(state: "open", limit: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("state=open", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("limit=10", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetIssueAsync_GetsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"number":42,"title":"Bug"}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.GetIssueAsync(42, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(42, result.Number);
        Assert.Contains("/mcpserver/gh/issues/42", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CloseIssueAsync_IncludesReason()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.CloseIssueAsync(1, reason: "completed", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("reason=completed", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("/close", handler.LastRequest.RequestUri.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task SyncFromGitHubAsync_PostsCorrectly()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"synced":5,"skipped":0,"failed":0,"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.SyncFromGitHubAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal(5, result.Synced);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateIssueAsync_PostsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"number":99,"url":"https://github.com/x/y/issues/99"}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.CreateIssueAsync(new Models.GitHubIssueRequest { Title = "Bug" }, cancellationToken: TestContext.Current.CancellationToken);

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

        var result = await client.UpdateIssueAsync(42, new Models.GitHubIssueUpdateRequest { Title = "Updated" }, cancellationToken: TestContext.Current.CancellationToken);

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

        await client.ReopenIssueAsync(7, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/issues/7/reopen", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CommentOnIssueAsync_PostsBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.CommentOnIssueAsync(5, "Nice work!", cancellationToken: TestContext.Current.CancellationToken);

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

        var result = await client.ListLabelsAsync(cancellationToken: TestContext.Current.CancellationToken);

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

        var result = await client.ListPullsAsync(state: "open", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("state=open", handler.LastRequest!.RequestUri!.Query);
        Assert.Single(result.Pulls);
    }

    [Fact]
    public async System.Threading.Tasks.Task CommentOnPullAsync_PostsBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.CommentOnPullAsync(3, "LGTM", cancellationToken: TestContext.Current.CancellationToken);

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

        var result = await client.SyncToGitHubAsync(cancellationToken: TestContext.Current.CancellationToken);

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

        var result = await client.SyncIssueAsync(10, "from-github", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/issues/10/sync", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("direction=from-github", handler.LastRequest.RequestUri.Query);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAuthStatusAsync_GetsCorrectPath()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"workspacePath":"x","authMode":"stored_token","hasStoredToken":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.GetAuthStatusAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("/mcpserver/gh/auth/status", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.True(result.HasStoredToken);
    }

    [Fact]
    public async System.Threading.Tasks.Task SetAuthTokenAsync_PutsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.SetAuthTokenAsync(new Models.GitHubAuthTokenUpsertRequest { AccessToken = "gho_test" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/auth/token", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("gho_test", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListWorkflowRunsAsync_SendsQueryParams()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"runs":[]}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        await client.ListWorkflowRunsAsync(branch: "main", status: "completed", eventName: "push", workflow: "ci", limit: 10, cancellationToken: TestContext.Current.CancellationToken);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("branch=main", query);
        Assert.Contains("status=completed", query);
        Assert.Contains("event=push", query);
        Assert.Contains("workflow=ci", query);
        Assert.Contains("limit=10", query);
    }

    [Fact]
    public async System.Threading.Tasks.Task RerunWorkflowRunAsync_PostsCorrectPath()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new GitHubClient(http, DefaultOptions);

        var result = await client.RerunWorkflowRunAsync(55, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/gh/actions/runs/55/rerun", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }
}
