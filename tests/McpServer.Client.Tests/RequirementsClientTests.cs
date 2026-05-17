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
    public async System.Threading.Tasks.Task GenerateAsync_ReturnsWorkspaceExportMetadata()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """
            {"success":true,"format":"wiki","docType":"all","generatedAtUtc":"2026-05-08T12:00:00Z","outputRoot":"F:\\GitHub\\McpServer\\docs\\Project\\wiki","files":[{"relativePath":"azure/Home.md","fullPath":"F:\\GitHub\\McpServer\\docs\\Project\\wiki\\azure\\Home.md","contentType":"text/markdown","lastModifiedUtc":"2026-05-08T12:00:00Z"}]}
            """);
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.GenerateAsync("all", "wiki");

        Assert.Equal("application/json", result.ContentType);
        Assert.NotNull(result.ExportResult);
        Assert.True(result.ExportResult!.Success);
        Assert.Equal("wiki", result.ExportResult.Format);
        Assert.Equal("azure/Home.md", Assert.Single(result.ExportResult.Files).RelativePath);
        Assert.Contains("doc=all", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("format=wiki", handler.LastRequest.RequestUri.Query);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
    }

    [Fact]
    public async System.Threading.Tasks.Task IngestAsync_PostsMarkdownPayload()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"functionalParsed":1,"functionalAdded":0,"functionalUpdated":1,"technicalParsed":0,"technicalAdded":0,"technicalUpdated":0,"testingParsed":0,"testingAdded":0,"testingUpdated":0,"mappingParsed":0,"mappingAdded":0,"mappingUpdated":0}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.IngestAsync(new RequirementsIngestRequest
        {
            FunctionalMarkdown = "# Functional Requirements (MCP Server)\n\n## FR-MCP-001 Sample\n\nBody."
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/ingest", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("functionalMarkdown", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Equal(1, result.FunctionalParsed);
        Assert.Equal(1, result.FunctionalUpdated);
    }

    [Fact]
    public async System.Threading.Tasks.Task IngestAsync_PostsWikiDocumentMap()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"functionalParsed":1,"functionalAdded":1,"functionalUpdated":0,"technicalParsed":0,"technicalAdded":0,"technicalUpdated":0,"testingParsed":0,"testingAdded":0,"testingUpdated":0,"mappingParsed":0,"mappingAdded":0,"mappingUpdated":0,"selectedWikiFormat":"github"}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.IngestAsync(new RequirementsIngestRequest
        {
            SourceFormat = "wiki",
            PreferredWikiFormat = "github",
            Documents = new Dictionary<string, RequirementsIngestDocument>
            {
                ["github/Functional-Requirements.md"] = new()
                {
                    Content = "# Functional Requirements (MCP Server)",
                    LastModifiedUtc = new DateTimeOffset(2026, 5, 8, 12, 0, 0, TimeSpan.Zero)
                }
            }
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("\"sourceFormat\":\"wiki\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"preferredWikiFormat\":\"github\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"documents\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Equal("github", result.SelectedWikiFormat);
    }
}
