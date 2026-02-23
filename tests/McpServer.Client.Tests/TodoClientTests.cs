using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class TodoClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7148"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task QueryAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.QueryAsync(keyword: "auth", priority: "high");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("keyword=auth", handler.LastRequest.RequestUri!.Query);
        Assert.Contains("priority=high", handler.LastRequest.RequestUri.Query);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"MVP-001","title":"test","section":"s","priority":"high","done":false}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.GetAsync("MVP-001");

        Assert.Equal("MVP-001", result.Id);
        Assert.Contains("/mcp/todo/MVP-001", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_PostsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.CreateAsync(new Models.TodoCreateRequest
        {
            Id = "NEW-001",
            Title = "New item",
            Section = "test",
            Priority = "high"
        });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("NEW-001", handler.LastRequestBody!);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAsync_PutsJsonBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.UpdateAsync("MVP-001", new Models.TodoUpdateRequest { Done = true });

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_SendsDeleteRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.DeleteAsync("MVP-001");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApiKeyHeader_IsSent()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        await client.QueryAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Contains("test-key", values!);
    }

    [Fact]
    public async System.Threading.Tasks.Task AnalyzeRequirementsAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"copilotResponse":"analysis"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var result = await client.AnalyzeRequirementsAsync("MVP-001");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcp/todo/MVP-001/requirements", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamStatusAsync_YieldsDataLines()
    {
        var sse = "data: Line one\n\ndata: Line two\n\nevent: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var lines = new System.Collections.Generic.List<string>();
        await foreach (var line in client.StreamStatusAsync("MVP-001"))
            lines.Add(line);

        Assert.Equal(2, lines.Count);
        Assert.Equal("Line one", lines[0]);
        Assert.Equal("Line two", lines[1]);
        Assert.Contains("/mcp/todo/MVP-001/prompt/status", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamImplementAsync_YieldsDataLines()
    {
        var sse = "data: impl line\n\nevent: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var lines = new System.Collections.Generic.List<string>();
        await foreach (var line in client.StreamImplementAsync("MVP-001"))
            lines.Add(line);

        Assert.Single(lines);
        Assert.Equal("impl line", lines[0]);
        Assert.Contains("/mcp/todo/MVP-001/prompt/implement", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamPlanAsync_YieldsDataLines()
    {
        var sse = "data: plan step 1\n\ndata: plan step 2\n\ndata: plan step 3\n\nevent: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var lines = new System.Collections.Generic.List<string>();
        await foreach (var line in client.StreamPlanAsync("MVP-001"))
            lines.Add(line);

        Assert.Equal(3, lines.Count);
        Assert.Equal("plan step 1", lines[0]);
        Assert.Contains("/mcp/todo/MVP-001/prompt/plan", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamSse_WithoutApiKey_ThrowsInvalidOperation()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, "", "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, new McpServerClientOptions { BaseUrl = new System.Uri("http://localhost:7148") });

        await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.StreamStatusAsync("MVP-001")) { }
        });
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamSse_ServerError_ThrowsMcpServerException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.InternalServerError, """{"error":"fail"}""", "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        await Assert.ThrowsAsync<McpServerException>(async () =>
        {
            await foreach (var _ in client.StreamStatusAsync("MVP-001")) { }
        });
    }

    [Fact]
    public async System.Threading.Tasks.Task StreamSse_ApiKeyHeader_IsSent()
    {
        var sse = "event: done\ndata: \n\n";
        var handler = new MockHttpHandler(HttpStatusCode.OK, sse, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        await foreach (var _ in client.StreamStatusAsync("MVP-001")) { }

        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Contains("test-key", values!);
    }
}
