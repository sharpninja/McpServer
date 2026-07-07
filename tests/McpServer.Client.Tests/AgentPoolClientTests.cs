using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class AgentPoolClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task GetAgentsAsync_SendsCorrectRequest()
    {
        var json = """[{"agentName":"planner","lifecycle":"idle"}]""";
        var handler = new MockHttpHandler(HttpStatusCode.OK, json);
        using var http = new HttpClient(handler);
        var client = new AgentPoolClient(http, DefaultOptions);

        var result = await client.GetAgentsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("planner", result[0].AgentName);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agent-pool/agents", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StartAgentAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new AgentPoolClient(http, DefaultOptions);

        var result = await client.StartAgentAsync("planner", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agent-pool/agents/planner/start", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task EnqueueOneShotAsync_PostsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"jobId":"job-1","agentName":"planner"}""");
        using var http = new HttpClient(handler);
        var client = new AgentPoolClient(http, DefaultOptions);

        var result = await client.EnqueueOneShotAsync(new AgentPoolOneShotRequest
        {
            PromptText = "Write tests",
            Context = AgentPoolOneShotContext.AdHoc
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("job-1", result.JobId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agent-pool/queue/one-shot", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("Write tests", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    [Fact]
    public async System.Threading.Tasks.Task ResolvePromptAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"promptText":"resolved"}""");
        using var http = new HttpClient(handler);
        var client = new AgentPoolClient(http, DefaultOptions);

        var result = await client.ResolvePromptAsync(new AgentPoolOneShotRequest
        {
            PromptText = "raw prompt",
            Context = AgentPoolOneShotContext.AdHoc
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("resolved", result.PromptText);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/agent-pool/queue/resolve", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamNotificationsAsync_ParsesSseEvents()
    {
        var sse = """
data: {"eventType":"queued","jobId":"job-1","agentName":"planner"}

data: {"eventType":"completed","jobId":"job-1","agentName":"planner"}

event: done
data: 

""";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new AgentPoolClient(http, DefaultOptions);

        var events = new List<AgentPoolNotificationEvent>();
        await foreach (var evt in client.StreamNotificationsAsync(cancellationToken: TestContext.Current.CancellationToken))
            events.Add(evt);

        Assert.Equal(2, events.Count);
        Assert.Equal("queued", events[0].EventType);
        Assert.Equal("completed", events[1].EventType);
        Assert.Contains("/mcpserver/agent-pool/notifications", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamJobAsync_ParsesJobEvents()
    {
        var sse = """
data: {"jobId":"job-42","eventType":"snapshot","status":"queued"}

event: done
data: 

""";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new AgentPoolClient(http, DefaultOptions);

        var events = new List<AgentPoolJobStreamEvent>();
        await foreach (var evt in client.StreamJobAsync("job-42", cancellationToken: TestContext.Current.CancellationToken))
            events.Add(evt);

        Assert.Single(events);
        Assert.Equal("job-42", events[0].JobId);
        Assert.Equal("snapshot", events[0].EventType);
        Assert.Contains("/mcpserver/agent-pool/jobs/job-42/stream", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
