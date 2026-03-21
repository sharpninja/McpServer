using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class VoiceClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task CreateSessionAsync_PostsPayload()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"sessionId":"voice-1","status":"idle","language":"en-US"}""");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var result = await client.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            DeviceId = "device-1",
            Language = "en-US"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/voice/session", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"deviceId\":\"device-1\"", handler.LastRequestBody!);
        Assert.Equal("voice-1", result.SessionId);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateSessionAsync_SerializesExecutionStrategy()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"sessionId":"voice-1","status":"idle","language":"en-US","executionStrategy":"hosted-mcp-agent"}""");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var result = await client.CreateSessionAsync(new VoiceSessionCreateRequest
        {
            DeviceId = "device-1",
            ExecutionStrategy = "hosted-mcp-agent",
        });

        Assert.Contains("\"executionStrategy\":\"hosted-mcp-agent\"", handler.LastRequestBody!);
        Assert.Equal("hosted-mcp-agent", result.ExecutionStrategy);
    }

    [Fact]
    public async System.Threading.Tasks.Task SubmitTurnAsync_PostsToTurnEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"sessionId":"voice-1","turnId":"t1","status":"completed"}""");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var result = await client.SubmitTurnAsync("voice-1", new VoiceTurnRequest { UserTranscriptText = "hello" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/voice/session/voice-1/turn", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("t1", result.TurnId);
    }

    [Fact]
    public async System.Threading.Tasks.Task InterruptAsync_PostsInterruptEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"sessionId":"voice-1","interrupted":true,"status":"idle"}""");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var result = await client.InterruptAsync("voice-1");

        Assert.True(result.Interrupted);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/voice/session/voice-1/interrupt", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetStatusAsync_GetsStatusEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"sessionId":"voice-1","status":"idle","language":"en-US","createdUtc":"2026-01-01T00:00:00Z","lastUpdatedUtc":"2026-01-01T00:00:00Z","isTurnActive":false,"turnCounter":0,"transcriptCount":0}""");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var result = await client.GetStatusAsync("voice-1");

        Assert.Equal("voice-1", result.SessionId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/voice/session/voice-1", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTranscriptAsync_GetsTranscriptEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"sessionId":"voice-1","items":[{"timestampUtc":"2026-01-01T00:00:00Z","role":"user","category":"transcript","text":"hello"}]}""");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var result = await client.GetTranscriptAsync("voice-1");

        Assert.Single(result.Items);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/voice/session/voice-1/transcript", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteSessionAsync_ReturnsTrueOnNoContent()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, string.Empty, "text/plain");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var deleted = await client.DeleteSessionAsync("voice-1");

        Assert.True(deleted);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/voice/session/voice-1", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteSessionAsync_ReturnsFalseOnNotFound()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NotFound, """{"error":"not found"}""");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var deleted = await client.DeleteSessionAsync("missing");

        Assert.False(deleted);
    }

    [Fact]
    public async System.Threading.Tasks.Task SubmitTurnStreamingAsync_ParsesSseEvents()
    {
        var payload = string.Join(
            "\n",
            "data: {\"type\":\"chunk\",\"text\":\"hello\"}",
            string.Empty,
            "data: {\"type\":\"done\",\"turnId\":\"t1\",\"status\":\"completed\"}",
            string.Empty);

        var handler = new MockHttpHandler(HttpStatusCode.OK, payload, "text/event-stream");
        using var http = new HttpClient(handler);
        var client = new VoiceClient(http, DefaultOptions);

        var events = new List<VoiceTurnStreamEvent>();
        await foreach (var evt in client.SubmitTurnStreamingAsync("voice-1", new VoiceTurnRequest { UserTranscriptText = "hello" }))
            events.Add(evt);

        Assert.Equal(2, events.Count);
        Assert.Equal("chunk", events[0].Type);
        Assert.Equal("done", events[1].Type);
        Assert.Contains("/mcpserver/voice/session/voice-1/turn/stream", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
