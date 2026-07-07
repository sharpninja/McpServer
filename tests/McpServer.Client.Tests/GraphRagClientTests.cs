using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// Tests for the dedicated GraphRAG client facade.
/// </summary>
public sealed class GraphRagClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>
    /// The dedicated client exposes each GraphRAG controller operation without requiring
    /// callers to route through <see cref="ContextClient"/>.
    /// </summary>
    [Fact]
    public void GraphRagClient_ExposesAllControllerEquivalentMethods()
    {
        var expected = new[]
        {
            nameof(GraphRagClient.StatusAsync),
            nameof(GraphRagClient.IndexAsync),
            nameof(GraphRagClient.QueryAsync),
            nameof(GraphRagClient.IngestTextAsync),
            nameof(GraphRagClient.ListDocumentsAsync),
            nameof(GraphRagClient.GetDocumentChunksAsync),
            nameof(GraphRagClient.DeleteDocumentAsync),
            nameof(GraphRagClient.CreateEntityAsync),
            nameof(GraphRagClient.ListEntitiesAsync),
            nameof(GraphRagClient.GetEntityAsync),
            nameof(GraphRagClient.UpdateEntityAsync),
            nameof(GraphRagClient.DeleteEntityAsync),
            nameof(GraphRagClient.CreateRelationshipAsync),
            nameof(GraphRagClient.ListRelationshipsAsync),
            nameof(GraphRagClient.GetRelationshipAsync),
            nameof(GraphRagClient.UpdateRelationshipAsync),
            nameof(GraphRagClient.DeleteRelationshipAsync),
        }.OrderBy(static name => name).ToArray();

        var actual = typeof(GraphRagClient)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(static method => method.Name)
            .Distinct()
            .OrderBy(static name => name)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// QueryAsync posts the typed query body to the GraphRAG query endpoint.
    /// </summary>
    [Fact]
    public async Task QueryAsync_PostsGraphRagQueryRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"query":"auth","mode":"local","answer":"ok","citations":[],"chunks":[],"sourceKeys":[],"entities":[],"relationships":[],"communities":[]}""");
        using var http = new HttpClient(handler);
        var client = new GraphRagClient(http, DefaultOptions);

        var result = await client.QueryAsync(
            "auth",
            mode: "local",
            maxChunks: 10,
            includeContextChunks: false,
            maxEntities: 5,
            maxRelationships: 4,
            communityDepth: 2,
            responseTokenBudget: 1024, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/graphrag/query", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"query\":\"auth\"", handler.LastRequestBody!);
        Assert.Contains("\"includeContextChunks\":false", handler.LastRequestBody!);
        Assert.Contains("\"maxEntities\":5", handler.LastRequestBody!);
        Assert.Equal("auth", result.Query);
    }

    /// <summary>
    /// ListRelationshipsAsync builds the GraphRAG relationship filter query string.
    /// </summary>
    [Fact]
    public async Task ListRelationshipsAsync_GetsFilteredRelationshipPage()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"relationships":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new GraphRagClient(http, DefaultOptions);

        var result = await client.ListRelationshipsAsync(skip: 3, take: 9, entityId: "entity/1", type: "uses", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/graphrag/relationships", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("skip=3", handler.LastRequest.RequestUri.Query);
        Assert.Contains("take=9", handler.LastRequest.RequestUri.Query);
        Assert.Contains("entityId=entity%2F1", handler.LastRequest.RequestUri.Query);
        Assert.Contains("type=uses", handler.LastRequest.RequestUri.Query);
        Assert.Equal(0, result.TotalCount);
    }

    /// <summary>
    /// CreateEntityAsync posts the typed entity request to the GraphRAG entity endpoint.
    /// </summary>
    [Fact]
    public async Task CreateEntityAsync_PostsEntityRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"entity-1","name":"Dispatcher","entityType":"component"}""");
        using var http = new HttpClient(handler);
        var client = new GraphRagClient(http, DefaultOptions);

        var result = await client.CreateEntityAsync(new GraphEntityRequest
        {
            Name = "Dispatcher",
            EntityType = "component",
            Description = "Routes GraphRAG commands.",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/graphrag/entities", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"name\":\"Dispatcher\"", handler.LastRequestBody!);
        Assert.Contains("\"entityType\":\"component\"", handler.LastRequestBody!);
        Assert.Equal("entity-1", result.Id);
    }

    /// <summary>
    /// DeleteEntityAsync sends a DELETE request and requires only a successful status response.
    /// </summary>
    [Fact]
    public async Task DeleteEntityAsync_SendsDeleteRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, string.Empty);
        using var http = new HttpClient(handler);
        var client = new GraphRagClient(http, DefaultOptions);

        await client.DeleteEntityAsync("entity/1", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/graphrag/entities/entity%2F1", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}
