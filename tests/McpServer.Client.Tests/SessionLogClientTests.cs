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
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("Copilot", result.SourceType);
    }

    [Fact]
    public async System.Threading.Tasks.Task QueryAsync_PassesFilters()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"totalCount":0,"limit":10,"offset":0,"items":[]}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        await client.QueryAsync(agent: "Copilot", limit: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("agent=Copilot", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("limit=10", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task QueryAsync_RequestObjectPassesAgentDefinitionFilter()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"totalCount":0,"limit":25,"offset":5,"items":[]}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        await client.QueryAsync(new SessionLogQueryRequest
        {
            Agent = "Codex",
            AgentDefinitionId = "mcpserver-triage",
            Limit = 25,
            Offset = 5,
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("agent=Codex", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("agentDefinitionId=mcpserver-triage", handler.LastRequest.RequestUri.Query);
        Assert.Contains("limit=25", handler.LastRequest.RequestUri.Query);
        Assert.Contains("offset=5", handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async System.Threading.Tasks.Task RepairWorkspaceStampsAsync_PostsRepairEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"repaired":12,"dryRun":true}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.RepairWorkspaceStampsAsync(dryRun: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/sessionlog/repair-workspace-stamps", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("dryRun=true", handler.LastRequest.RequestUri.Query);
        Assert.Equal(12, result.Repaired);
        Assert.True(result.DryRun);
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
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("/Copilot/s1/r1/dialog", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal(2, result.TotalDialogCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpsertTurnAsync_PostsTurn()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"turnId":5,"agent":"Copilot","sessionId":"s1","requestId":"r1"}""");
        using var http = new HttpClient(handler);
        var client = new SessionLogClient(http, DefaultOptions);

        var result = await client.UpsertTurnAsync("Copilot", "s1", new UnifiedRequestEntryDto
        {
            RequestId = "r1",
            QueryText = "structured turn",
            Interpretation = "preserve interpretation",
            Status = "in_progress",
            Tags = ["sessionlog"],
            ContextList = ["src/McpServer.Client/SessionLogClient.cs"],
            Actions =
            [
                new UnifiedActionDto
                {
                    Description = "typed client per-turn append",
                    Type = "session_turn",
                    Status = "completed",
                    FilePath = "src/McpServer.Client/SessionLogClient.cs"
                }
            ]
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/sessionlog/Copilot/s1/turn", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"interpretation\":\"preserve interpretation\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"contextList\":[\"src/McpServer.Client/SessionLogClient.cs\"]", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal(5, result.TurnId);
        Assert.Equal("r1", result.RequestId);
    }
}
