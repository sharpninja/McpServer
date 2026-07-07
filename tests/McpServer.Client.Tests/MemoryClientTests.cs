using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>Tests for typed memory REST client routing and payload contracts.</summary>
public sealed class MemoryClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
        WorkspacePath = @"E:\github\McpServer",
    };

    /// <summary>ListAsync builds the expected query string and deserializes memory results.</summary>
    [Fact]
    public async System.Threading.Tasks.Task ListAsync_SendsExpectedQuery()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"items":[{"id":"MEMORY-OPERATOR-001","category":"OPERATOR","scope":"Global","text":"global memory","version":1,"createdAtUtc":"2026-06-08T00:00:00Z","updatedAtUtc":"2026-06-08T00:00:00Z"}],"totalCount":1}
            """);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.ListAsync(MemoryScope.Global, "operator notes", "global", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Single(result.Items);
        Assert.Equal(MemoryScope.Global, result.Items[0].Scope);
        Assert.Equal("http://localhost:7147/mcpserver/memory?scope=Global&category=operator%20notes&keyword=global", handler.LastRequest!.RequestUri!.OriginalString);
        Assert.True(handler.LastRequest.Headers.Contains("X-Workspace-Path"));
    }

    /// <summary>AddAsync posts the structured memory add request and returns the mutation result.</summary>
    [Fact]
    public async System.Threading.Tasks.Task AddAsync_PostsRequestBody()
    {
        var response = new MemoryMutationResult
        {
            Success = true,
            Memory = new MemoryItem
            {
                Id = "MEMORY-OPERATOR-001",
                Category = "OPERATOR",
                Scope = MemoryScope.Workspace,
                WorkspacePath = @"E:\github\McpServer",
                Text = "workspace memory",
                Version = 1,
                CreatedAtUtc = DateTimeOffset.Parse("2026-06-08T00:00:00Z"),
                UpdatedAtUtc = DateTimeOffset.Parse("2026-06-08T00:00:00Z"),
            },
        };
        var handler = new MockHttpHandler(HttpStatusCode.Created, JsonSerializer.Serialize(response));
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.AddAsync(new MemoryAddRequest
        {
            Category = "operator",
            Scope = MemoryScope.Workspace,
            Text = "workspace memory",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/memory", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("workspace memory", handler.LastRequestBody, StringComparison.Ordinal);
    }

    /// <summary>UpdateAsync and RemoveAsync use the id route segment.</summary>
    [Fact]
    public async System.Threading.Tasks.Task UpdateAndRemoveAsync_UseIdRoutes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        await client.UpdateAsync("MEMORY-OPERATOR-001", new MemoryUpdateRequest { Text = "updated" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/memory/MEMORY-OPERATOR-001", handler.LastRequest.RequestUri!.ToString());

        await client.RemoveAsync("MEMORY-OPERATOR-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/memory/MEMORY-OPERATOR-001", handler.LastRequest.RequestUri!.ToString());
    }
}
