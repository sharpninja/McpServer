using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class SessionLogClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task SubmitAsync_PostsSessionLog()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"id":1,"sourceType":"Copilot","sessionId":"s1"}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.SubmitAsync(new UnifiedSessionLogDto
        {
            SourceType = "Copilot",
            SessionId = "s1",
            Title = "Test"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("Copilot", result.SourceType);
    }

    [Fact]
    public async System.Threading.Tasks.Task QueryAsync_PassesFilters()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"totalCount":0,"limit":10,"offset":0,"items":[]}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        await client.QueryAsync(agent: "Copilot", limit: 10);

        Assert.Contains("agent=Copilot", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("limit=10", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task AppendDialogAsync_PostsItems()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"agent":"Copilot","sessionId":"s1","requestId":"r1","totalDialogCount":2}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.AppendDialogAsync("Copilot", "s1", "r1", new List<ProcessingDialogItemDto>
        {
            new() { Role = "model", Content = "Thinking...", Category = "reasoning" }
        });

        Assert.Contains("/Copilot/s1/r1/dialog", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal(2, result.TotalDialogCount);
    }
}
