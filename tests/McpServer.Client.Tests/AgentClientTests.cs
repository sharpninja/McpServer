using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class AgentClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task ListDefinitionsAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[{"id":"cursor","displayName":"Cursor"}],"totalCount":1}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.ListDefinitionsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("cursor", result.Items[0].Id);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/definitions", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetDefinitionAsync_EncodesRouteSegment()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"copilot/manager","displayName":"Copilot Manager"}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.GetDefinitionAsync("copilot/manager", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("copilot/manager", result.Id);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/definitions/copilot%2Fmanager", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpsertDefinitionAsync_PostsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.UpsertDefinitionAsync(new AgentDefinitionRequest
        {
            Id = "cursor",
            DisplayName = "Cursor",
            DefaultLaunchCommand = "cursor-agent"
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/definitions", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"cursor\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"displayName\":\"Cursor\"", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteDefinitionAsync_UsesDeleteVerb()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.DeleteDefinitionAsync("cursor", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/definitions/cursor", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task SeedDefaultsAsync_PostsSeedEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"seeded":3}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.SeedDefaultsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Seeded);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/definitions/seed", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListWorkspaceAgentsAsync_IncludesWorkspaceQuery()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        _ = await client.ListWorkspaceAgentsAsync("/repo path", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("workspace=%2Frepo%20path", handler.LastRequest.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("/mcpserver/agents", handler.LastRequest.RequestUri.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetWorkspaceAgentAsync_IncludesWorkspaceQuery()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"agentId":"cursor","workspacePath":"/repo","enabled":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.GetWorkspaceAgentAsync("cursor", "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("cursor", result.AgentId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpsertWorkspaceAgentAsync_PostsRouteAndBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.UpsertWorkspaceAgentAsync(
            "cursor",
            new AgentWorkspaceRequest { AgentId = "cursor", Enabled = true, AgentIsolation = "clone" },
            "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("\"agentIsolation\":\"clone\"", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteWorkspaceAgentAsync_UsesDeleteVerb()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.DeleteWorkspaceAgentAsync("cursor", "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task BanAgentAsync_PostsBanRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.BanAgentAsync(
            "cursor",
            new AgentBanRequest { Reason = "policy", Global = false, BannedUntilPr = 17 },
            "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor/ban", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"reason\":\"policy\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"global\":false", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task UnbanAgentAsync_UsesGlobalQueryFlag()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.UnbanAgentAsync("cursor", global: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor/unban", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("global=true", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task LogEventAsync_PostsEventRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.LogEventAsync(
            "cursor",
            new AgentEventRequest
            {
                AgentId = "cursor",
                EventType = 7,
                Details = "init"
            },
            "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor/events", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"eventType\":7", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetEventsAsync_IncludesWorkspaceAndLimitQuery()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[{"id":1,"agentId":"cursor","workspacePath":"/repo","eventType":7}],"totalCount":1}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.GetEventsAsync("cursor", "/repo", 25, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(7, result.Items[0].EventType);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor/events", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("limit=25", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task ValidateAsync_UsesValidateEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"valid":true,"path":"/repo/agents.yaml"}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.ValidateAsync("/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Valid);
        Assert.Equal("/repo/agents.yaml", result.Path);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/validate", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task LaunchAgentAsync_PostsLaunchEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"processId":1234,"agentId":"cursor","workspacePath":"/repo","startedAt":"2026-06-25T12:00:00Z","status":"Running","workDirectory":"/repo"}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.LaunchAgentAsync("cursor", "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor/launch", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Equal(1234, result.ProcessId);
        Assert.Equal(AgentProcessStatus.Running, result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task StopAgentAsync_PostsStopEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.StopAgentAsync("cursor", "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor/stop", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetProcessStatusAsync_GetsProcessStatusEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"processId":1234,"agentId":"cursor","workspacePath":"/repo","startedAt":"2026-06-25T12:00:00Z","status":"Running","workDirectory":"/repo"}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.GetProcessStatusAsync("cursor", "/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/cursor/process-status", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Equal(AgentProcessStatus.Running, result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListRunningAgentsAsync_GetsRunningEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"agents":[{"processId":1234,"agentId":"cursor","workspacePath":"/repo","startedAt":"2026-06-25T12:00:00Z","status":"Running","workDirectory":"/repo"}]}""");
        using var http = new HttpClient(handler);
        var client = new AgentClient(http, DefaultOptions);

        var result = await client.ListRunningAgentsAsync("/repo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agents/running", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("workspace=%2Frepo", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Equal("cursor", Assert.Single(result.Agents).AgentId);
    }
}
