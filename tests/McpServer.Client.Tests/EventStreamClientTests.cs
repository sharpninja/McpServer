using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class EventStreamClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task SubscribeAsync_ParsesSseDataPayloads()
    {
        var sse = string.Join(
            "\n",
            "event: todo",
            "data: {\"category\":\"todo\",\"action\":\"updated\",\"entityId\":\"MVP-MCP-003\",\"resourceUri\":\"mcp://workspace/todo/MVP-MCP-003\",\"timestamp\":\"2026-03-03T00:00:00Z\"}",
            string.Empty,
            "event: done",
            string.Empty);

        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new EventStreamClient(http, DefaultOptions);

        var events = new List<McpServer.Client.Models.ChangeEvent>();
        await foreach (var evt in client.SubscribeAsync(cancellationToken: TestContext.Current.CancellationToken))
            events.Add(evt);

        Assert.Single(events);
        Assert.Equal("todo", events[0].Category);
        Assert.Equal("updated", events[0].Action);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/events", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task SubscribeAsync_WithCategory_AddsQueryParameter()
    {
        var sse = string.Join(
            "\n",
            "event: repo",
            "data: {\"category\":\"repo\",\"action\":\"created\",\"timestamp\":\"2026-03-03T00:00:00Z\"}",
            string.Empty);

        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new EventStreamClient(http, DefaultOptions);

        var events = new List<McpServer.Client.Models.ChangeEvent>();
        await foreach (var evt in client.SubscribeAsync("repo", cancellationToken: TestContext.Current.CancellationToken))
            events.Add(evt);

        Assert.Single(events);
        Assert.Contains("category=repo", handler.LastRequest!.RequestUri!.Query);
    }
}
