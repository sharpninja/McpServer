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

        var result = await client.ListFrAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("FR-MCP-001", result[0].Id);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/fr", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListRequirementsAsync_EncodesFilters()
    {
        var frHandler = new MockHttpHandler(HttpStatusCode.OK, """[]""");
        using var frHttp = new HttpClient(frHandler);
        var client = new RequirementsClient(frHttp, DefaultOptions);

        await client.ListFrAsync("MCP QA", "in_progress");

        Assert.Contains("area=MCP%20QA", frHandler.LastRequest!.RequestUri!.Query);
        Assert.Contains("status=in_progress", frHandler.LastRequest.RequestUri.Query);

        var trHandler = new MockHttpHandler(HttpStatusCode.OK, """[]""");
        using var trHttp = new HttpClient(trHandler);
        var trClient = new RequirementsClient(trHttp, DefaultOptions);

        await trClient.ListTrAsync("MCP", "REQ", "completed");

        Assert.Equal("/mcpserver/requirements/tr", trHandler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("area=MCP", trHandler.LastRequest.RequestUri.Query);
        Assert.Contains("subarea=REQ", trHandler.LastRequest.RequestUri.Query);
        Assert.Contains("status=completed", trHandler.LastRequest.RequestUri.Query);

        var testHandler = new MockHttpHandler(HttpStatusCode.OK, """[]""");
        using var testHttp = new HttpClient(testHandler);
        var testClient = new RequirementsClient(testHttp, DefaultOptions);

        await testClient.ListTestAsync("MCP", "completed");

        Assert.Equal("/mcpserver/requirements/test", testHandler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("area=MCP", testHandler.LastRequest.RequestUri.Query);
        Assert.Contains("status=completed", testHandler.LastRequest.RequestUri.Query);
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

    /// <summary>
    /// Verifies FR batch creation posts the records array to the atomic FR batch endpoint and deserializes the batch result.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CreateFrBatchAsync_PostsRecordsArray()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"operation":"create","kind":"fr","total":1,"items":[{"kind":"fr","id":"FR-MCP-910","fr":{"id":"FR-MCP-910","title":"Batch","body":"Body"}}],"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.CreateFrBatchAsync(new CreateFrBatchRequest
        {
            Records =
            [
                new CreateFrBatchRecord
                {
                    Id = "FR-MCP-910",
                    Title = "Batch",
                    Description = "Body",
                    Priority = "high"
                }
            ]
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/fr/batch", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"records\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"description\":\"Body\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.True(result.Success);
        Assert.Equal("FR-MCP-910", Assert.Single(result.Items).Id);
    }

    /// <summary>
    /// Verifies mixed batch updates post to the atomic mixed batch endpoint with kind-tagged records.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task UpdateBatchAsync_PostsMixedRecordsArray()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"operation":"update","total":1,"items":[{"kind":"test","id":"TEST-MCP-910","test":{"id":"TEST-MCP-910","condition":"Updated"}}],"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.UpdateBatchAsync(new UpdateRequirementsBatchRequest
        {
            Records =
            [
                new UpdateRequirementBatchRecord
                {
                    Kind = "test",
                    Id = "TEST-MCP-910",
                    Description = "Updated"
                }
            ]
        });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/batch", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"kind\":\"test\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"description\":\"Updated\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.True(result.Success);
        Assert.Equal("test", Assert.Single(result.Items).Kind);
    }

    /// <summary>
    /// Verifies batch update requests preserve structured acceptance criteria in all update record shapes.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task UpdateBatchRequests_PostAcceptanceCriteriaArrays()
    {
        async System.Threading.Tasks.Task AssertRequestAsync(
            Func<RequirementsClient, System.Threading.Tasks.Task<RequirementsBatchResult>> send,
            string expectedPath,
            string expectedId)
        {
            var handler = new MockHttpHandler(
                HttpStatusCode.OK,
                """{"success":true,"operation":"update","total":1,"items":[],"errors":[]}""");
            using var http = new HttpClient(handler);
            var client = new RequirementsClient(http, DefaultOptions);

            var result = await send(client);

            Assert.True(result.Success);
            Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
            Assert.Contains(expectedPath, handler.LastRequest.RequestUri!.AbsolutePath);
            Assert.Contains($"\"id\":\"{expectedId}\"", handler.LastRequestBody!, StringComparison.Ordinal);
            Assert.Contains("\"acceptanceCriteria\"", handler.LastRequestBody!, StringComparison.Ordinal);
            Assert.Contains("\"text\":\"Batch criterion\"", handler.LastRequestBody!, StringComparison.Ordinal);
            Assert.Contains("\"isSatisfied\":false", handler.LastRequestBody!, StringComparison.Ordinal);
        }

        await AssertRequestAsync(
            client => client.UpdateFrBatchAsync(new UpdateFrBatchRequest
            {
                Records =
                [
                    new UpdateFrBatchRecord
                    {
                        Id = "FR-MCP-920",
                        Description = "Updated",
                        AcceptanceCriteria =
                        [
                            new AcceptanceCriterion
                            {
                                Id = "FR-MCP-920-AC001",
                                Text = "Batch criterion",
                                IsSatisfied = false
                            }
                        ]
                    }
                ]
            }),
            "/mcpserver/requirements/fr/batch",
            "FR-MCP-920");

        await AssertRequestAsync(
            client => client.UpdateTrBatchAsync(new UpdateTrBatchRequest
            {
                Records =
                [
                    new UpdateTrBatchRecord
                    {
                        Id = "TR-MCP-920",
                        Description = "Updated",
                        AcceptanceCriteria =
                        [
                            new AcceptanceCriterion
                            {
                                Id = "TR-MCP-920-AC001",
                                Text = "Batch criterion",
                                IsSatisfied = false
                            }
                        ]
                    }
                ]
            }),
            "/mcpserver/requirements/tr/batch",
            "TR-MCP-920");

        await AssertRequestAsync(
            client => client.UpdateTestBatchAsync(new UpdateTestBatchRequest
            {
                Records =
                [
                    new UpdateTestBatchRecord
                    {
                        Id = "TEST-MCP-920",
                        Description = "Updated",
                        AcceptanceCriteria =
                        [
                            new AcceptanceCriterion
                            {
                                Id = "TEST-MCP-920-AC001",
                                Text = "Batch criterion",
                                IsSatisfied = false
                            }
                        ]
                    }
                ]
            }),
            "/mcpserver/requirements/test/batch",
            "TEST-MCP-920");

        await AssertRequestAsync(
            client => client.UpdateBatchAsync(new UpdateRequirementsBatchRequest
            {
                Records =
                [
                    new UpdateRequirementBatchRecord
                    {
                        Kind = "fr",
                        Id = "FR-MCP-921",
                        Description = "Updated",
                        AcceptanceCriteria =
                        [
                            new AcceptanceCriterion
                            {
                                Id = "FR-MCP-921-AC001",
                                Text = "Batch criterion",
                                IsSatisfied = false
                            }
                        ]
                    }
                ]
            }),
            "/mcpserver/requirements/batch",
            "FR-MCP-921");
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
    public async System.Threading.Tasks.Task UpdateFrAsync_PutsMetadataBody()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"id":"FR-MCP-001","title":"Title","body":"Body","priority":"high","status":"in_progress","notes":"Reviewed"}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.UpdateFrAsync("FR-MCP-001", new UpdateFrRequest
        {
            Title = "Title",
            Body = "Body",
            Priority = "high",
            Status = "in_progress",
            Notes = "Reviewed"
        });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("\"priority\":\"high\"", handler.LastRequestBody!);
        Assert.Contains("\"status\":\"in_progress\"", handler.LastRequestBody!);
        Assert.Contains("\"notes\":\"Reviewed\"", handler.LastRequestBody!);
        Assert.Equal("high", result.Priority);
        Assert.Equal("in_progress", result.Status);
        Assert.Equal("Reviewed", result.Notes);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTrAndTestAsync_PutMetadataBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"TR-MCP-REQ-001","title":"TR","body":"Body","priority":"high","status":"completed","notes":"TR notes"}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var tr = await client.UpdateTrAsync("TR-MCP-REQ-001", new UpdateTrRequest
        {
            Title = "TR",
            Body = "Body",
            Priority = "high",
            Status = "completed",
            Notes = "TR notes"
        });

        Assert.Contains("\"priority\":\"high\"", handler.LastRequestBody!);
        Assert.Contains("\"status\":\"completed\"", handler.LastRequestBody!);
        Assert.Contains("\"notes\":\"TR notes\"", handler.LastRequestBody!);
        Assert.Equal("high", tr.Priority);
        Assert.Equal("completed", tr.Status);
        Assert.Equal("TR notes", tr.Notes);

        var testHandler = new MockHttpHandler(HttpStatusCode.OK, """{"id":"TEST-MCP-001","condition":"Condition","priority":"high","status":"completed","notes":"TEST notes"}""");
        using var testHttp = new HttpClient(testHandler);
        var testClient = new RequirementsClient(testHttp, DefaultOptions);

        var test = await testClient.UpdateTestAsync("TEST-MCP-001", new UpdateTestRequest
        {
            Condition = "Condition",
            Priority = "high",
            Status = "completed",
            Notes = "TEST notes"
        });

        Assert.Contains("\"priority\":\"high\"", testHandler.LastRequestBody!);
        Assert.Contains("\"status\":\"completed\"", testHandler.LastRequestBody!);
        Assert.Contains("\"notes\":\"TEST notes\"", testHandler.LastRequestBody!);
        Assert.Equal("high", test.Priority);
        Assert.Equal("completed", test.Status);
        Assert.Equal("TEST notes", test.Notes);
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
    public async System.Threading.Tasks.Task CopyFrAcceptanceCriteriaFromTodoAsync_PostsCopyEndpoint()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"id":"FR-MCP-001","title":"Title","body":"Body","acceptanceCriteria":[{"id":"AC-1","text":"Copied","isSatisfied":false}]}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var result = await client.CopyFrAcceptanceCriteriaFromTodoAsync(
            "FR/MCP/001",
            new CopyAcceptanceCriteriaFromTodoRequest { TodoId = "TODO-001" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/fr/FR%2FMCP%2F001/acceptance-criteria/copy-from-todo", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"todoId\":\"TODO-001\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Equal("Copied", Assert.Single(result.AcceptanceCriteria!).Text);
    }

    [Fact]
    public async System.Threading.Tasks.Task CopyTrAndTestAcceptanceCriteriaFromTodoAsync_PostExpectedKindRoutes()
    {
        var trHandler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"id":"TR-MCP-001","title":"TR","body":"Body","acceptanceCriteria":[]}""");
        using var trHttp = new HttpClient(trHandler);
        var trClient = new RequirementsClient(trHttp, DefaultOptions);

        await trClient.CopyTrAcceptanceCriteriaFromTodoAsync(
            "TR-MCP-001",
            new CopyAcceptanceCriteriaFromTodoRequest { TodoId = "TODO-001" });

        Assert.Equal(HttpMethod.Post, trHandler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/tr/TR-MCP-001/acceptance-criteria/copy-from-todo", trHandler.LastRequest.RequestUri!.AbsolutePath);

        var testHandler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"id":"TEST-MCP-001","condition":"Condition","acceptanceCriteria":[]}""");
        using var testHttp = new HttpClient(testHandler);
        var testClient = new RequirementsClient(testHttp, DefaultOptions);

        await testClient.CopyTestAcceptanceCriteriaFromTodoAsync(
            "TEST-MCP-001",
            new CopyAcceptanceCriteriaFromTodoRequest { TodoId = "TODO-001" });

        Assert.Equal(HttpMethod.Post, testHandler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/test/TEST-MCP-001/acceptance-criteria/copy-from-todo", testHandler.LastRequest.RequestUri!.AbsolutePath);
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

    /// <summary>
    /// Verifies RepairFrPlaceholdersAsync posts to the fr/repair endpoint and parses the purged count.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task RepairFrPlaceholdersAsync_PostsToRepairEndpointAndReturnsPurgedCount()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"purged":3}""");
        using var http = new HttpClient(handler);
        var client = new RequirementsClient(http, DefaultOptions);

        var purged = await client.RepairFrPlaceholdersAsync();

        Assert.Equal(3, purged);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/requirements/fr/repair", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}
