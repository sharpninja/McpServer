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

    /// <summary>
    /// TEST-MCP-MEMORY-004 / triage-report-a6fb8ae08ce348799d0db61ae2e0734a:
    /// Live recall JSON uses <c>items</c> and <c>rankingMode</c>. MemoryClient must surface those
    /// rows as non-empty plugin-facing <c>hits</c>, and must keep <c>rankingMode</c>, instead of
    /// dropping them on <see cref="MemorySurfaceResult"/>.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task RecallAsync_LiveItemsJson_SurfacesHits()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, LiveRecallItemsJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.RecallAsync(
            new MemoryRecallRequest { Query = "MEMORY-FACT-003" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:7147/mcpserver/memory/recall", handler.LastRequest.RequestUri!.ToString());
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
        Assert.NotNull(result.Hits);
        Assert.NotEmpty(result.Hits!);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.Hits!).Id);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.Items!).Id);
        Assert.Equal(0.87, result.Hits![0].Score);
        Assert.Equal("hybrid", result.RankingMode);
        Assert.False(result.RerankApplied);
        Assert.Contains("Operator fact from Legion recall proof.", result.Hits[0].Content, StringComparison.Ordinal);

        var pluginFacing = JsonSerializer.Serialize(result, McpClientJsonContext.Default.MemoryRecallResult);
        using var document = JsonDocument.Parse(pluginFacing);
        Assert.True(document.RootElement.TryGetProperty("hits", out var hits));
        Assert.Equal(JsonValueKind.Array, hits.ValueKind);
        Assert.NotEqual(0, hits.GetArrayLength());
        Assert.Equal("MEMORY-FACT-003", hits[0].GetProperty("id").GetString());
        Assert.Equal("hybrid", document.RootElement.GetProperty("rankingMode").GetString());
        Assert.True(document.RootElement.TryGetProperty("items", out var items));
        Assert.Equal("MEMORY-FACT-003", items[0].GetProperty("id").GetString());
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: A <c>hits</c> array on the wire also deserializes and stays non-empty.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task RecallAsync_HitsJson_SurfacesItems()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, LiveRecallHitsJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.RecallAsync(
            new MemoryRecallRequest { Query = "MEMORY-FACT-003" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.RankedHits).Id);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.Hits!).Id);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.Items!).Id);
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: Direct deserialize of live API JSON into the old surface DTO
    /// still drops ranked rows, which is why recall must use <see cref="MemoryRecallResult"/>.
    /// </summary>
    [Fact]
    public void MemorySurfaceResult_LiveItemsJson_DropsHits()
    {
        var dropped = JsonSerializer.Deserialize(LiveRecallItemsJson, McpClientJsonContext.Default.MemorySurfaceResult);
        Assert.NotNull(dropped);
        Assert.Equal(200, dropped!.StatusCode);
        var surfaceJson = JsonSerializer.Serialize(dropped, McpClientJsonContext.Default.MemorySurfaceResult);
        using var document = JsonDocument.Parse(surfaceJson);
        Assert.False(document.RootElement.TryGetProperty("hits", out _));
        Assert.False(document.RootElement.TryGetProperty("items", out _));
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: Live explore JSON uses <c>items</c> neighbors. The client must
    /// keep those rows on <c>items</c>, <c>neighbors</c>, and plugin-facing <c>hits</c>.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ExploreAsync_LiveItemsJson_SurfacesHits()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, LiveExploreItemsJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-FACT-003" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("http://localhost:7147/mcpserver/memory/explore", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("MEMORY-FACT-004", Assert.Single(result.Items!).Id);
        Assert.Equal("MEMORY-FACT-004", Assert.Single(result.Hits!).Id);
        Assert.Equal("MEMORY-FACT-004", Assert.Single(result.Neighbors!).Id);
        Assert.Equal("related", result.Hits![0].EdgeType);

        var pluginFacing = JsonSerializer.Serialize(result, McpClientJsonContext.Default.MemoryExploreResult);
        using var document = JsonDocument.Parse(pluginFacing);
        Assert.Equal("MEMORY-FACT-004", document.RootElement.GetProperty("hits")[0].GetProperty("id").GetString());
        Assert.Equal("MEMORY-FACT-004", document.RootElement.GetProperty("items")[0].GetProperty("id").GetString());
        Assert.Equal("MEMORY-FACT-004", document.RootElement.GetProperty("neighbors")[0].GetProperty("id").GetString());
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: Live consolidate JSON uses <c>plan</c>. The client must surface
    /// that list as non-empty <c>plan</c>, <c>items</c>, and <c>hits</c>.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ConsolidateAsync_LivePlanJson_SurfacesHits()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, LiveConsolidatePlanJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.ConsolidateAsync(
            new MemoryConsolidateRequest { DryRun = true },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.Plan!).SurvivorId);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.Items!).SurvivorId);
        Assert.Equal("MEMORY-FACT-003", Assert.Single(result.Hits!).SurvivorId);
        Assert.Contains("MEMORY-FACT-004", result.Plan![0].CandidateIds);
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: Live versions JSON uses <c>items</c> snapshots. The client must
    /// keep those rows on <c>items</c> and plugin-facing <c>hits</c>.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ListVersionsAsync_LiveItemsJson_SurfacesHits()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, LiveVersionsItemsJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.ListVersionsAsync("MEMORY-FACT-003", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.EndsWith("/mcpserver/memory/MEMORY-FACT-003/versions", handler.LastRequest!.RequestUri!.OriginalString, StringComparison.Ordinal);
        Assert.Equal(1, Assert.Single(result.Items!).VersionNumber);
        Assert.Equal(1, Assert.Single(result.Hits!).VersionNumber);
        Assert.Contains("Operator fact from Legion recall proof.", result.Hits![0].Content, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: Remember returns a memory object, not a ranked list. The typed
    /// DTO must keep <c>memory</c> instead of collapsing to <see cref="MemorySurfaceResult"/>.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task RememberAsync_LiveMemoryJson_KeepsMemory()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, LiveRememberJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.RememberAsync(
            new MemoryRememberRequest { Content = "Operator fact from Legion recall proof." },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(201, result.StatusCode);
        Assert.Equal("MEMORY-FACT-003", result.MemoryId);
        Assert.Equal("MEMORY-FACT-003", result.Memory!.Id);
        Assert.Contains("Operator fact from Legion recall proof.", result.Memory.Text, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(LiveRememberJson);
        Assert.False(document.RootElement.TryGetProperty("items", out _));
        Assert.False(document.RootElement.TryGetProperty("hits", out _));
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: Promote keeps the created memory row from live HTTP.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task PromoteAsync_LiveMemoryJson_KeepsMemory()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, LivePromoteJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.PromoteAsync(
            new MemoryPromoteRequest { SourceKind = "sessionlog", SourceRef = "turn-1" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("MEMORY-FACT-003", result.Memory!.Id);
        Assert.Equal("sessionlog", result.SourceKind);
        using var document = JsonDocument.Parse(LivePromoteJson);
        Assert.False(document.RootElement.TryGetProperty("items", out _));
        Assert.False(document.RootElement.TryGetProperty("hits", out _));
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: Revert keeps the restored memory row from live HTTP.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task RevertAsync_LiveMemoryJson_KeepsMemory()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, LiveRevertJson);
        using var http = new HttpClient(handler);
        var client = new MemoryClient(http, DefaultOptions);

        var result = await client.RevertAsync("MEMORY-FACT-003", 1, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("MEMORY-FACT-003", result.Memory!.Id);
        Assert.Equal(1, result.Memory.Version);
        using var document = JsonDocument.Parse(LiveRevertJson);
        Assert.False(document.RootElement.TryGetProperty("items", out _));
        Assert.False(document.RootElement.TryGetProperty("hits", out _));
    }

    /// <summary>
    /// TEST-MCP-MEMORY-004: No MemoryClient additive method maps a live body onto
    /// <see cref="MemorySurfaceResult"/> alone.
    /// </summary>
    [Fact]
    public void MemoryClient_AdditiveMethods_DoNotReturnMemorySurfaceResult()
    {
        foreach (var name in new[]
        {
            "RememberAsync", "RecallAsync", "ExploreAsync", "ConsolidateAsync",
            "PromoteAsync", "ListVersionsAsync", "RevertAsync",
        })
        {
            var method = typeof(MemoryClient).GetMethods()
                .Single(item => item.Name == name && item.GetParameters().Length >= 1);
            Assert.True(method.ReturnType.IsGenericType, name);
            Assert.NotEqual(typeof(MemorySurfaceResult), method.ReturnType.GetGenericArguments()[0]);
        }
    }

    private const string LiveRecallItemsJson =
        """
        {"statusCode":200,"items":[{"id":"MEMORY-FACT-003","score":0.87,"title":"Legion fact","content":"Operator fact from Legion recall proof.","type":"fact","tags":["legion"],"scope":"Workspace","matchKind":"hybrid"}],"failureKind":"None","rerankApplied":false,"rankingMode":"hybrid"}
        """;

    private const string LiveRecallHitsJson =
        """
        {"statusCode":200,"hits":[{"id":"MEMORY-FACT-003","score":0.87,"content":"Operator fact from Legion recall proof."}]}
        """;

    private const string LiveExploreItemsJson =
        """
        {"statusCode":200,"items":[{"id":"MEMORY-FACT-004","weight":0.8,"edgeType":"related","depth":1}],"failureKind":"None","seedId":"MEMORY-FACT-003","hebbianApplied":false}
        """;

    private const string LiveConsolidatePlanJson =
        """
        {"statusCode":200,"plan":[{"candidateIds":["MEMORY-FACT-003","MEMORY-FACT-004"],"similarityScore":0.91,"survivorId":"MEMORY-FACT-003"}],"mergedAwayIds":[],"dryRunApplied":true,"failureKind":"None"}
        """;

    private const string LiveVersionsItemsJson =
        """
        {"statusCode":200,"items":[{"versionNumber":1,"title":"Legion fact","content":"Operator fact from Legion recall proof.","createdAtUtc":"2026-09-19T19:00:00Z"}]}
        """;

    private const string LiveRememberJson =
        """
        {"statusCode":201,"memoryId":"MEMORY-FACT-003","memory":{"id":"MEMORY-FACT-003","category":"FACT","scope":"Workspace","text":"Operator fact from Legion recall proof.","version":1,"createdAtUtc":"2026-09-19T19:00:00Z","updatedAtUtc":"2026-09-19T19:00:00Z"},"failureKind":"None"}
        """;

    private const string LivePromoteJson =
        """
        {"statusCode":200,"memory":{"id":"MEMORY-FACT-003","category":"FACT","scope":"Workspace","text":"Promoted fact.","version":1,"createdAtUtc":"2026-09-19T19:00:00Z","updatedAtUtc":"2026-09-19T19:00:00Z"},"sourceKind":"sessionlog","sourceRef":"turn-1","failureKind":"None"}
        """;

    private const string LiveRevertJson =
        """
        {"statusCode":200,"memory":{"id":"MEMORY-FACT-003","category":"FACT","scope":"Workspace","text":"Restored fact.","version":1,"createdAtUtc":"2026-09-19T19:00:00Z","updatedAtUtc":"2026-09-19T19:00:00Z"},"failureKind":"None"}
        """;
}
