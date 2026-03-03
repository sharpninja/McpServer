using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class RequirementsClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task ListFrAsync_GetsFrCollection()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"id":"FR-MCP-001","title":"Title","body":"Body"}]""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.ListFrAsync();

        Assert.Single(result);
        Assert.Equal("FR-MCP-001", result[0].Id);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/fr", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetFrAsync_EncodesIdAndDeserializes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"FR/MCP/001","title":"Title","body":"Body"}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.GetFrAsync("FR/MCP/001");

        Assert.Equal("FR/MCP/001", result.Id);
        Assert.Contains("/mcpserver/requirements/fr/FR%2FMCP%2F001", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTrAsync_PostsBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"id":"TR-MCP-001","title":"TR","body":"Body"}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.CreateTrAsync(new CreateTrRequest
        {
            Id = "TR-MCP-001",
            Title = "TR",
            Body = "Body"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/tr", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"id\":\"TR-MCP-001\"", handler.LastRequestBody!);
        Assert.Equal("TR-MCP-001", result.Id);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTestAsync_PutsBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"TEST-MCP-001","condition":"Updated condition"}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.UpdateTestAsync("TEST-MCP-001", new UpdateTestRequest { Condition = "Updated condition" });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/test/TEST-MCP-001", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"condition\":\"Updated condition\"", handler.LastRequestBody!);
        Assert.Equal("Updated condition", result.Condition);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteTestAsync_UsesDeleteEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.DeleteTestAsync("TEST-MCP-007");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/test/TEST-MCP-007", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpsertMappingAsync_PutsMappingPayload()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"frId":"FR-MCP-001","trIds":["TR-MCP-001","TR-MCP-002"]}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.UpsertMappingAsync("FR-MCP-001", new UpsertFrTrMappingRequest
        {
            TrIds = ["TR-MCP-001", "TR-MCP-002"]
        });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/mapping/FR-MCP-001", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"trIds\":[\"TR-MCP-001\",\"TR-MCP-002\"]", handler.LastRequestBody!);
        Assert.Equal(2, result.TrIds.Count);
    }

    [Fact]
    public async System.Threading.Tasks.Task GenerateAsync_ReturnsBinaryAndContentType()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, "ZIPDATA", "application/zip");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.GenerateAsync("all");

        Assert.Equal("application/zip", result.ContentType);
        Assert.NotEmpty(result.Content);
        Assert.Contains("doc=all", handler.LastRequest!.RequestUri!.Query);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
    }
}
