using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>Tests for X-Workspace-Path header support in McpServerClient and McpClientBase.</summary>
public sealed class WorkspaceHeaderTests
{
    [Fact]
    public async System.Threading.Tasks.Task Client_SendsXWorkspacePath_WhenSet()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, "{\"items\":[],\"totalCount\":0}");
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"C:\projects\alpha",
        };
        var client = new McpServerClient(http, options);

        await client.Todo.QueryAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.Contains("X-Workspace-Path"));
        var values = handler.LastRequest.Headers.GetValues("X-Workspace-Path").ToList();
        Assert.Single(values);
        Assert.Equal(@"C:\projects\alpha", values[0]);
    }

    [Fact]
    public async System.Threading.Tasks.Task Client_OmitsXWorkspacePath_WhenNull()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, "{\"items\":[],\"totalCount\":0}");
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            // WorkspacePath not set — defaults to null/empty
        };
        var client = new McpServerClient(http, options);

        await client.Todo.QueryAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.False(handler.LastRequest!.Headers.Contains("X-Workspace-Path"));
    }

    [Fact]
    public async System.Threading.Tasks.Task Client_SendsBothApiKeyAndWorkspacePath()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, "{\"items\":[],\"totalCount\":0}");
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"C:\projects\beta",
        };
        var client = new McpServerClient(http, options);

        await client.Todo.QueryAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.Contains("X-Api-Key"));
        Assert.True(handler.LastRequest.Headers.Contains("X-Workspace-Path"));
    }

    [Fact]
    public void WorkspacePath_PropagatedToAllSubClients()
    {
        using var http = new HttpClient();
        var options = new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"C:\initial",
        };
        var client = new McpServerClient(http, options);

        client.WorkspacePath = @"C:\updated";

        Assert.Equal(@"C:\updated", client.Todo.WorkspacePath);
        Assert.Equal(@"C:\updated", client.Context.WorkspacePath);
        Assert.Equal(@"C:\updated", client.Workspace.WorkspacePath);
        Assert.Equal(@"C:\updated", client.Repo.WorkspacePath);
        Assert.Equal(@"C:\updated", client.GitHub.WorkspacePath);
        Assert.Equal(@"C:\updated", client.SessionLog.WorkspacePath);
        Assert.Equal(@"C:\updated", client.Memory.WorkspacePath);
        Assert.Equal(@"C:\updated", client.Tools.WorkspacePath);
        Assert.Equal(@"C:\updated", client.AgentPool.WorkspacePath);
    }

    [Fact]
    public void Options_WorkspacePath_DefaultsToNull()
    {
        var options = new McpServerClientOptions();
        Assert.Null(options.WorkspacePath);
    }
}
