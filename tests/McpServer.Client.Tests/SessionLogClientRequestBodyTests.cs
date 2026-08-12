using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// TR-MCP-CLIENT-001: Verifies that every <see cref="SessionLogClient"/> mutation that used to
/// post a compiler-generated anonymous body now posts a registered request DTO, so the shared
/// <c>McpClientJsonContext</c> resolver can produce <c>JsonTypeInfo</c> for it. Fixture is
/// <see cref="MockHttpHandler"/> over an in-memory <see cref="HttpClient"/>: no server is needed
/// because the defect (BUG-TRIAGE-088/090/093/094/095/101) throws during request serialization,
/// before the request is ever dispatched.
/// </summary>
public sealed class SessionLogClientRequestBodyTests
{
    /// <summary>Options pointing the client at a loopback base URL with a dummy API key.</summary>
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    /// <summary>
    /// TR-MCP-CLIENT-001: <see cref="SessionLogClient.OpenSessionAsync"/> must serialize its
    /// title/model body through the source-generated context and reach the server.
    /// Fixture: <see cref="MockHttpHandler"/> returning a 200 open result.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task OpenSessionAsync_SerializesTitleAndModelBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"agent":"ClaudeCode","sessionId":"s1","created":true}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.OpenSessionAsync(
            "ClaudeCode", "s1", title: "Partition 2 sweep", model: "opus-4-8",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/mcpserver/sessionlog/ClaudeCode/s1/open", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"title\":\"Partition 2 sweep\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"model\":\"opus-4-8\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.True(result.Created);
    }

    /// <summary>
    /// TR-MCP-CLIENT-001: <see cref="SessionLogClient.BeginTurnAsync"/> must serialize its
    /// queryTitle/queryText/model body through the source-generated context.
    /// Fixture: <see cref="MockHttpHandler"/> returning a 201 turn-submit result.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task BeginTurnAsync_SerializesQueryTitleTextAndModelBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"turnId":11,"agent":"ClaudeCode","sessionId":"s1","requestId":"r1"}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.BeginTurnAsync(
            "ClaudeCode", "s1", "r1",
            queryTitle: "Fix anonymous bodies", queryText: "audit the client", model: "opus-4-8",
            planFile: "None", todoId: "MCP-SESSIONLOG-002",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/mcpserver/sessionlog/ClaudeCode/s1/r1/begin", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"queryTitle\":\"Fix anonymous bodies\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"queryText\":\"audit the client\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"model\":\"opus-4-8\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"planFile\":\"None\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"todoId\":\"MCP-SESSIONLOG-002\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal(11, result.TurnId);
    }

    /// <summary>
    /// AC-TR-MCP-SESSIONLOG-006-007 / TEST-MCP-SESSIONLOG-006:
    /// <see cref="SessionLogClient.BeginTurnAsync"/> serializes planFile and todoId.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task BeginTurnAsync_SerializesPlanFileAndTodoId()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"turnId":12,"agent":"ClaudeCode","sessionId":"s1","requestId":"r2"}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        await client.BeginTurnAsync(
            "ClaudeCode", "s1", "r2",
            queryTitle: "Plan fields", queryText: "serialize",
            planFile: "docs/plans/foo.md", todoId: "MCP-SESSIONLOG-002",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("\"planFile\":\"docs/plans/foo.md\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"todoId\":\"MCP-SESSIONLOG-002\"", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-MCP-CLIENT-001: <see cref="SessionLogClient.SetSessionTitleAsync"/> must serialize its
    /// title body through the source-generated context instead of an anonymous type.
    /// Fixture: <see cref="MockHttpHandler"/> returning a 200 retitle result.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task SetSessionTitleAsync_SerializesTitleBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"turnId":3,"agent":"ClaudeCode","sessionId":"s1","retitled":true}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.SetSessionTitleAsync(
            "ClaudeCode", "s1", "Agent-refined session title",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/mcpserver/sessionlog/ClaudeCode/s1/title", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"title\":\"Agent-refined session title\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal(3, result.TurnId);
    }

    /// <summary>
    /// TR-MCP-CLIENT-001: <see cref="SessionLogClient.SetTurnTitleAsync"/> must serialize its
    /// title body through the source-generated context instead of an anonymous type.
    /// Fixture: <see cref="MockHttpHandler"/> returning a 200 retitle result.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task SetTurnTitleAsync_SerializesTitleBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"turnId":4,"agent":"ClaudeCode","sessionId":"s1","requestId":"r1","retitled":true}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.SetTurnTitleAsync(
            "ClaudeCode", "s1", "r1", "Agent-refined turn title",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/mcpserver/sessionlog/ClaudeCode/s1/r1/title", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"title\":\"Agent-refined turn title\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal(4, result.TurnId);
    }
}
