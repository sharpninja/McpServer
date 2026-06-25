using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// TEST-MCP-TRIAGE-001 and TEST-MCP-REPL-TRIAGE-001: client contract tests for the
/// triage REST surface and the McpServerClient facade used by REPL passthrough.
/// </summary>
public sealed class TriageClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>TEST-MCP-TRIAGE-001: SubmitReportAsync posts the shared triage report contract.</summary>
    [Fact]
    public async Task SubmitReportAsync_PostsReportContract()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.Accepted,
            """{"success":true,"reportId":"triage-report-001","groupId":"triage-group-001","status":"collecting","quietDeadlineUtc":"2026-06-25T05:15:00Z","workspacePath":"F:\\GitHub\\McpServer"}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.SubmitReportAsync(new TriageReportRequest
        {
            Title = "Wrapper hides error",
            Summary = "client.triage should expose accepted queue state.",
            Component = "mcpserver-codex-plugin",
        });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/reports", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"component\":\"mcpserver-codex-plugin\"", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-TRIAGE-001: GetReportAsync reads a report by id.</summary>
    [Fact]
    public async Task GetReportAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"reportId":"triage-report-001","groupId":"triage-group-001","status":"collecting","title":"bug","summary":"details"}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.GetReportAsync("triage-report-001");

        Assert.Equal("triage-report-001", result.ReportId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/reports/triage-report-001", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>TEST-MCP-TRIAGE-002: QueryGroupsAsync supports status and workspace filters.</summary>
    [Fact]
    public async Task QueryGroupsAsync_SendsFilters()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.QueryGroupsAsync(status: "failed", workspacePath: "F:\\GitHub\\McpServer");

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/groups", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("status=failed", handler.LastRequest.RequestUri.Query);
        Assert.Contains("workspacePath=F%3A%5CGitHub%5CMcpServer", handler.LastRequest.RequestUri.Query);
    }

    /// <summary>TEST-MCP-TRIAGE-002: GetGroupAsync reads a group by id.</summary>
    [Fact]
    public async Task GetGroupAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"groupId":"triage-group-001","status":"collecting","reportCount":1,"quietDeadlineUtc":"2026-06-25T05:15:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.GetGroupAsync("triage-group-001");

        Assert.Equal("triage-group-001", result.GroupId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/groups/triage-group-001", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>TEST-MCP-TRIAGE-002: FlushGroupAsync posts the flush command.</summary>
    [Fact]
    public async Task FlushGroupAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"groupId":"triage-group-001","status":"queued","reportCount":1,"quietDeadlineUtc":"2026-06-25T05:00:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.FlushGroupAsync("triage-group-001");

        Assert.Equal("queued", result.Status);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/groups/triage-group-001/flush", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>TEST-MCP-TRIAGE-005: RetryGroupAsync posts the retry command.</summary>
    [Fact]
    public async Task RetryGroupAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"groupId":"triage-group-001","status":"collecting","reportCount":1,"quietDeadlineUtc":"2026-06-25T05:00:00Z"}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.RetryGroupAsync("triage-group-001");

        Assert.Equal("collecting", result.Status);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/groups/triage-group-001/retry", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>TEST-TRIAGE-001: GetDashboardAsync reads queue buckets and run history for a workspace.</summary>
    [Fact]
    public async Task GetDashboardAsync_SendsWorkspaceFilter()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"triageQueue":[],"reportGroupQueue":[],"runHistory":[],"totalGroupCount":0,"totalRunCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.GetDashboardAsync("F:\\GitHub\\McpServer");

        Assert.Equal(0, result.TotalGroupCount);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/dashboard", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("workspacePath=F%3A%5CGitHub%5CMcpServer", handler.LastRequest.RequestUri.Query);
    }

    /// <summary>TEST-TRIAGE-001: QueryRunsAsync sends status, group, and workspace filters.</summary>
    [Fact]
    public async Task QueryRunsAsync_SendsFilters()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.QueryRunsAsync(
            status: "failed",
            groupId: "triage-group-001",
            workspacePath: "F:\\GitHub\\McpServer");

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/runs", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("status=failed", handler.LastRequest.RequestUri.Query);
        Assert.Contains("groupId=triage-group-001", handler.LastRequest.RequestUri.Query);
        Assert.Contains("workspacePath=F%3A%5CGitHub%5CMcpServer", handler.LastRequest.RequestUri.Query);
    }

    /// <summary>TEST-TRIAGE-001: GetRunAsync reads AI triage run result details by id.</summary>
    [Fact]
    public async Task GetRunAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"runId":"triage-run-001","groupId":"triage-group-001","status":"completed","startedUtc":"2026-06-25T05:00:00Z","responseJson":"{\"title\":\"Fix\"}"}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.GetRunAsync("triage-run-001");

        Assert.Equal("triage-run-001", result.RunId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/runs/triage-run-001", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("Fix", result.ResponseJson, StringComparison.Ordinal);
    }

    /// <summary>TEST-TRIAGE-002: QueryCreatedTodosAsync reads TODO ids created by triage.</summary>
    [Fact]
    public async Task QueryCreatedTodosAsync_SendsWorkspaceFilter()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"items":[{"todoId":"BUG-TRIAGE-001","createdAtUtc":"2026-06-25T05:03:00Z","workspacePath":"F:\\GitHub\\McpServer","groupId":"triage-group-001","runId":"triage-run-001","groupStatus":"completed","runStatus":"completed"}],"totalCount":1}""");
        using var http = new HttpClient(handler);
        var client = new TriageClient(http, DefaultOptions);

        var result = await client.QueryCreatedTodosAsync("F:\\GitHub\\McpServer");

        var item = Assert.Single(result.Items);
        Assert.Equal("BUG-TRIAGE-001", item.TodoId);
        Assert.Equal(new DateTimeOffset(2026, 6, 25, 5, 3, 0, TimeSpan.Zero), item.CreatedAtUtc);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/triage/todos", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("workspacePath=F%3A%5CGitHub%5CMcpServer", handler.LastRequest.RequestUri.Query);
    }

    /// <summary>TEST-MCP-REPL-TRIAGE-001: McpServerClient exposes Triage for generic client passthrough.</summary>
    [Fact]
    public void McpServerClient_ExposesTriageAndPropagatesWorkspacePath()
    {
        using var http = new HttpClient(new MockHttpHandler(HttpStatusCode.OK, "{}"));
        var client = new McpServerClient(http, DefaultOptions);

        client.WorkspacePath = "F:\\GitHub\\McpServer";

        Assert.NotNull(client.Triage);
        Assert.Equal("F:\\GitHub\\McpServer", client.Triage.WorkspacePath);
    }
}
