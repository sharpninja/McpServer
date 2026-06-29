using System.Net;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Text.Json;
using McpServer.Support.Mcp;
using McpServer.Support.Mcp.IntegrationTests;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>Integration tests for requirements management endpoints.</summary>
public sealed class RequirementsControllerTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly RequirementsWebFactory _factory;

    public RequirementsControllerTests()
    {
        _factory = new RequirementsWebFactory();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _factory.GetFullWorkspaceApiKey());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task FunctionalCrud_AndGenerateEndpoint_WorkEndToEnd()
    {
        var createBody = new
        {
            id = "FR-MCP-999",
            title = "Requirements test entry",
            body = "The server shall support end-to-end requirements CRUD integration tests."
        };

        var createResponse = await _client.PostAsJsonAsync("/mcpserver/requirements/fr", createBody).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getResponse = await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-999").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var generated = await _client.GetAsync("/mcpserver/requirements/generate?doc=functional").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
        Assert.Equal("text/markdown", generated.Content.Headers.ContentType?.MediaType);
        var generatedMarkdown = await generated.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("FR-MCP-999 Requirements test entry", generatedMarkdown);

        var generatedMatrix = await _client.GetAsync("/mcpserver/requirements/generate?doc=matrix").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, generatedMatrix.StatusCode);
        Assert.Equal("text/markdown", generatedMatrix.Content.Headers.ContentType?.MediaType);
        var matrixMarkdown = await generatedMatrix.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("| FR-MCP-999 | Tracked | Functional-Requirements.md |", matrixMarkdown);

        var deleteResponse = await _client.DeleteAsync("/mcpserver/requirements/fr/FR-MCP-999").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getDeletedResponse = await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-999").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    /// <summary>Batch creation accepts a YAML-equivalent records array shape and persists mixed requirement kinds atomically.</summary>
    [Fact]
    public async Task BatchCreateEndpoint_AcceptsMixedRecordsArray()
    {
        var payload = new
        {
            records = new object[]
            {
                new
                {
                    kind = "fr",
                    id = "FR-MCP-910",
                    title = "Batch FR",
                    description = "The server shall create FR records from a batch payload.",
                    priority = "high"
                },
                new
                {
                    kind = "tr",
                    id = "TR-MCP-BATCH-910",
                    title = "Batch TR",
                    description = "The server shall create TR records from a batch payload.",
                    priority = "high"
                },
                new
                {
                    kind = "test",
                    id = "TEST-MCP-910",
                    title = "Batch TEST",
                    description = "The server shall create TEST records from a batch payload.",
                    priority = "high"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcpserver/requirements/batch", payload).ConfigureAwait(true);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Batch create failed ({response.StatusCode}): {body}");
        using var result = JsonDocument.Parse(body);
        Assert.True(result.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(3, result.RootElement.GetProperty("total").GetInt32());

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-910").ConfigureAwait(true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/mcpserver/requirements/tr/TR-MCP-BATCH-910").ConfigureAwait(true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/mcpserver/requirements/test/TEST-MCP-910").ConfigureAwait(true)).StatusCode);
    }

    /// <summary>BUG-TRIAGE-010: single FR/TR/TEST create and update endpoints persist structured acceptance criteria after a fresh read.</summary>
    [Fact]
    public async Task SingleRequirementEndpoints_PersistAcceptanceCriteriaAfterFreshRead()
    {
        const string frId = "FR-MCP-AC-991";
        const string trId = "TR-MCP-AC-991";
        const string testId = "TEST-MCP-AC-991";

        await AssertSuccessAsync(await _client.PostAsJsonAsync(
            "/mcpserver/requirements/fr",
            new
            {
                id = frId,
                title = "AC FR",
                body = "FR body",
                notes = "keep-fr-notes",
                acceptanceCriteria = Criteria($"{frId}-AC001", "FR create criteria", false, "fr-create")
            }).ConfigureAwait(true), HttpStatusCode.Created).ConfigureAwait(true);
        await AssertSuccessAsync(await _client.PostAsJsonAsync(
            "/mcpserver/requirements/tr",
            new
            {
                id = trId,
                title = "AC TR",
                body = "TR body",
                notes = "keep-tr-notes",
                acceptanceCriteria = Criteria($"{trId}-AC001", "TR create criteria", false, "tr-create")
            }).ConfigureAwait(true), HttpStatusCode.Created).ConfigureAwait(true);
        await AssertSuccessAsync(await _client.PostAsJsonAsync(
            "/mcpserver/requirements/test",
            new
            {
                id = testId,
                condition = "TEST condition",
                title = "AC TEST",
                notes = "keep-test-notes",
                acceptanceCriteria = Criteria($"{testId}-AC001", "TEST create criteria", false, "test-create")
            }).ConfigureAwait(true), HttpStatusCode.Created).ConfigureAwait(true);

        var createdFr = await GetJsonAsync<FrEntry>($"/mcpserver/requirements/fr/{frId}").ConfigureAwait(true);
        var createdTr = await GetJsonAsync<TrEntry>($"/mcpserver/requirements/tr/{trId}").ConfigureAwait(true);
        var createdTest = await GetJsonAsync<TestEntry>($"/mcpserver/requirements/test/{testId}").ConfigureAwait(true);
        AssertCriterion(createdFr.AcceptanceCriteria, $"{frId}-AC001", "FR create criteria", false, "fr-create");
        AssertCriterion(createdTr.AcceptanceCriteria, $"{trId}-AC001", "TR create criteria", false, "tr-create");
        AssertCriterion(createdTest.AcceptanceCriteria, $"{testId}-AC001", "TEST create criteria", false, "test-create");

        await AssertSuccessAsync(await _client.PutAsJsonAsync(
            $"/mcpserver/requirements/fr/{frId}",
            new { acceptanceCriteria = Criteria($"{frId}-AC002", "FR update criteria", true, "fr-update") }).ConfigureAwait(true)).ConfigureAwait(true);
        await AssertSuccessAsync(await _client.PutAsJsonAsync(
            $"/mcpserver/requirements/tr/{trId}",
            new { acceptanceCriteria = Criteria($"{trId}-AC002", "TR update criteria", true, "tr-update") }).ConfigureAwait(true)).ConfigureAwait(true);
        await AssertSuccessAsync(await _client.PutAsJsonAsync(
            $"/mcpserver/requirements/test/{testId}",
            new { acceptanceCriteria = Criteria($"{testId}-AC002", "TEST update criteria", true, "test-update") }).ConfigureAwait(true)).ConfigureAwait(true);

        var updatedFr = await GetJsonAsync<FrEntry>($"/mcpserver/requirements/fr/{frId}").ConfigureAwait(true);
        var updatedTr = await GetJsonAsync<TrEntry>($"/mcpserver/requirements/tr/{trId}").ConfigureAwait(true);
        var updatedTest = await GetJsonAsync<TestEntry>($"/mcpserver/requirements/test/{testId}").ConfigureAwait(true);
        Assert.Equal("keep-fr-notes", updatedFr.Notes);
        Assert.Equal("keep-tr-notes", updatedTr.Notes);
        Assert.Equal("keep-test-notes", updatedTest.Notes);
        AssertCriterion(updatedFr.AcceptanceCriteria, $"{frId}-AC002", "FR update criteria", true, "fr-update");
        AssertCriterion(updatedTr.AcceptanceCriteria, $"{trId}-AC002", "TR update criteria", true, "tr-update");
        AssertCriterion(updatedTest.AcceptanceCriteria, $"{testId}-AC002", "TEST update criteria", true, "test-update");
    }

    /// <summary>BUG-TRIAGE-010: FR/TR/TEST batch endpoints persist structured acceptance criteria after create and update.</summary>
    [Fact]
    public async Task BatchRequirementEndpoints_PersistAcceptanceCriteriaAfterFreshRead()
    {
        const string frId = "FR-MCP-ACB-991";
        const string trId = "TR-MCP-ACB-991";
        const string testId = "TEST-MCP-ACB-991";

        await AssertBatchSuccessAsync(await _client.PostAsJsonAsync(
            "/mcpserver/requirements/fr/batch",
            new
            {
                records = new[]
                {
                    new
                    {
                        id = frId,
                        title = "Batch AC FR",
                        body = "Batch FR body",
                        notes = "keep-fr-batch-notes",
                        acceptanceCriteria = Criteria($"{frId}-AC001", "FR batch create criteria", false, "fr-batch-create")
                    }
                }
            }).ConfigureAwait(true)).ConfigureAwait(true);
        await AssertBatchSuccessAsync(await _client.PostAsJsonAsync(
            "/mcpserver/requirements/tr/batch",
            new
            {
                records = new[]
                {
                    new
                    {
                        id = trId,
                        title = "Batch AC TR",
                        body = "Batch TR body",
                        notes = "keep-tr-batch-notes",
                        acceptanceCriteria = Criteria($"{trId}-AC001", "TR batch create criteria", false, "tr-batch-create")
                    }
                }
            }).ConfigureAwait(true)).ConfigureAwait(true);
        await AssertBatchSuccessAsync(await _client.PostAsJsonAsync(
            "/mcpserver/requirements/test/batch",
            new
            {
                records = new[]
                {
                    new
                    {
                        id = testId,
                        condition = "Batch TEST condition",
                        title = "Batch AC TEST",
                        notes = "keep-test-batch-notes",
                        acceptanceCriteria = Criteria($"{testId}-AC001", "TEST batch create criteria", false, "test-batch-create")
                    }
                }
            }).ConfigureAwait(true)).ConfigureAwait(true);

        AssertCriterion((await GetJsonAsync<FrEntry>($"/mcpserver/requirements/fr/{frId}").ConfigureAwait(true)).AcceptanceCriteria, $"{frId}-AC001", "FR batch create criteria", false, "fr-batch-create");
        AssertCriterion((await GetJsonAsync<TrEntry>($"/mcpserver/requirements/tr/{trId}").ConfigureAwait(true)).AcceptanceCriteria, $"{trId}-AC001", "TR batch create criteria", false, "tr-batch-create");
        AssertCriterion((await GetJsonAsync<TestEntry>($"/mcpserver/requirements/test/{testId}").ConfigureAwait(true)).AcceptanceCriteria, $"{testId}-AC001", "TEST batch create criteria", false, "test-batch-create");

        await AssertBatchSuccessAsync(await _client.PutAsJsonAsync(
            "/mcpserver/requirements/fr/batch",
            new { records = new[] { new { id = frId, acceptanceCriteria = Criteria($"{frId}-AC002", "FR batch update criteria", true, "fr-batch-update") } } }).ConfigureAwait(true)).ConfigureAwait(true);
        await AssertBatchSuccessAsync(await _client.PutAsJsonAsync(
            "/mcpserver/requirements/tr/batch",
            new { records = new[] { new { id = trId, acceptanceCriteria = Criteria($"{trId}-AC002", "TR batch update criteria", true, "tr-batch-update") } } }).ConfigureAwait(true)).ConfigureAwait(true);
        await AssertBatchSuccessAsync(await _client.PutAsJsonAsync(
            "/mcpserver/requirements/test/batch",
            new { records = new[] { new { id = testId, acceptanceCriteria = Criteria($"{testId}-AC002", "TEST batch update criteria", true, "test-batch-update") } } }).ConfigureAwait(true)).ConfigureAwait(true);

        var updatedFr = await GetJsonAsync<FrEntry>($"/mcpserver/requirements/fr/{frId}").ConfigureAwait(true);
        var updatedTr = await GetJsonAsync<TrEntry>($"/mcpserver/requirements/tr/{trId}").ConfigureAwait(true);
        var updatedTest = await GetJsonAsync<TestEntry>($"/mcpserver/requirements/test/{testId}").ConfigureAwait(true);
        Assert.Equal("keep-fr-batch-notes", updatedFr.Notes);
        Assert.Equal("keep-tr-batch-notes", updatedTr.Notes);
        Assert.Equal("keep-test-batch-notes", updatedTest.Notes);
        AssertCriterion(updatedFr.AcceptanceCriteria, $"{frId}-AC002", "FR batch update criteria", true, "fr-batch-update");
        AssertCriterion(updatedTr.AcceptanceCriteria, $"{trId}-AC002", "TR batch update criteria", true, "tr-batch-update");
        AssertCriterion(updatedTest.AcceptanceCriteria, $"{testId}-AC002", "TEST batch update criteria", true, "test-batch-update");
    }

    /// <summary>Batch creation rejects duplicate incoming IDs before writing any record.</summary>
    [Fact]
    public async Task BatchCreateEndpoint_DuplicateIncomingIdsRejectsWholeBatch()
    {
        var payload = new
        {
            records = new object[]
            {
                new
                {
                    id = "FR-MCP-911",
                    title = "First duplicate",
                    description = "This record must not be committed.",
                    priority = "high"
                },
                new
                {
                    id = "FR-MCP-911",
                    title = "Second duplicate",
                    description = "This record makes the batch invalid.",
                    priority = "high"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcpserver/requirements/fr/batch", payload).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(true));
        Assert.False(result.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("Duplicate FR ID", result.RootElement.GetProperty("errors")[0].GetProperty("error").GetString(), StringComparison.Ordinal);

        var getResponse = await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-911").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GenerateAll_WritesCanonicalDocumentsToWorkspace()
    {
        var response = await _client.GetAsync("/mcpserver/requirements/generate?doc=all").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var export = await response.Content.ReadFromJsonAsync<RequirementsDocumentExportResult>().ConfigureAwait(true);
        Assert.NotNull(export);
        var names = export!.Files.Select(e => e.RelativePath).OrderBy(static n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            new[]
            {
                "Functional-Requirements.md",
                "Requirements-Matrix.md",
                "TR-per-FR-Mapping.md",
                "Technical-Requirements.md",
                "Testing-Requirements.md"
            },
            names);
        Assert.All(export.Files, file => Assert.True(File.Exists(file.FullPath), file.FullPath));
    }

    [Fact]
    public async Task GenerateWiki_WritesAzureAndGitHubWikiFiles()
    {
        var response = await _client.GetAsync("/mcpserver/requirements/generate?doc=all&format=wiki").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(true);
        using var archive = new ZipArchive(responseStream, ZipArchiveMode.Read, leaveOpen: false);
        var names = archive.Entries.Select(e => e.FullName).OrderBy(static n => n, StringComparer.Ordinal).ToArray();

        Assert.Contains("azure/.mcp-requirements-manifest.json", names);
        Assert.Contains("azure/.order", names);
        Assert.Contains("azure/Requirements-Matrix.md", names);
        Assert.Contains("github/.mcp-requirements-manifest.json", names);
        Assert.Contains("github/Requirements-Matrix.md", names);
        Assert.Contains("github/_Sidebar.md", names);
        Assert.Contains("github/_Footer.md", names);

        var testingEntry = archive.GetEntry("github/Testing-Requirements.md");
        Assert.NotNull(testingEntry);
        using var testingReader = new StreamReader(testingEntry!.Open());
        var testingMarkdown = await testingReader.ReadToEndAsync().ConfigureAwait(true);
        Assert.Contains("## TEST-MCP", testingMarkdown, StringComparison.Ordinal);
        Assert.Contains("### TEST-MCP-", testingMarkdown, StringComparison.Ordinal);
        Assert.DoesNotContain("| ID | Requirement |", testingMarkdown, StringComparison.Ordinal);
        Assert.DoesNotContain("- TEST-MCP-", testingMarkdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IngestEndpoint_ParsesAndUpsertsMarkdownPayload()
    {
        var payload = new
        {
            functionalMarkdown = """
                # Functional Requirements (MCP Server)

                ## FR-MCP-001 Seed Entry Updated

                Updated FR body.

                ## FR-MCP-777 New Entry

                New FR body.
                """,
            technicalMarkdown = """
                # Technical Requirements (MCP Server)

                ## TR-MCP-001

                **Updated Title** — Updated TR body.

                ## TR-MCP-777

                **New Title** — New TR body.
                """,
            testingMarkdown = """
                # Testing Requirements (MCP Server)

                - TEST-MCP-001: Updated test condition.
                - TEST-MCP-777: New test condition.
                """,
            mappingMarkdown = """
                # TR per FR Mapping (MCP Server)

                | FR | Primary TRs |
                | --- | --- |
                | FR-MCP-001 | TR-MCP-001 |
                | FR-MCP-777 | TR-MCP-777 |
                """
        };

        var ingestResponse = await _client.PostAsJsonAsync("/mcpserver/requirements/ingest", payload).ConfigureAwait(true);
        var ingestBody = await ingestResponse.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.True(ingestResponse.StatusCode == HttpStatusCode.OK,
            $"Requirements ingest failed ({ingestResponse.StatusCode}): {ingestBody}");

        var fr = await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-777").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, fr.StatusCode);

        var tr = await _client.GetAsync("/mcpserver/requirements/tr/TR-MCP-777").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, tr.StatusCode);

        var test = await _client.GetAsync("/mcpserver/requirements/test/TEST-MCP-777").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, test.StatusCode);

        var mapping = await _client.GetAsync("/mcpserver/requirements/mapping/FR-MCP-777").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, mapping.StatusCode);
    }

    [Fact]
    public async Task IngestEndpoint_SelectsNewerWikiSourceAndAuthoritativelySyncs()
    {
        var older = new DateTimeOffset(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2026, 5, 8, 12, 0, 0, TimeSpan.Zero);
        var payload = new
        {
            sourceFormat = "wiki",
            documents = new Dictionary<string, object?>
            {
                ["azure/.mcp-requirements-manifest.json"] = WikiDoc("""{"generatedAtUtc":"2026-05-07T12:00:00Z"}""", older),
                ["azure/Functional-Requirements.md"] = WikiDoc("""
                    # Functional Requirements (MCP Server)

                    ## FR-MCP-101 Azure Entry

                    Azure body.
                    """, older),
                ["azure/Technical-Requirements.md"] = WikiDoc("""
                    # Technical Requirements (MCP Server)

                    ## TR-MCP-101

                    Azure TR body.
                    """, older),
                ["azure/Testing-Requirements.md"] = WikiDoc("""
                    # Testing Requirements (MCP Server)

                    - TEST-MCP-101: Azure test.
                    """, older),
                ["azure/TR-per-FR-Mapping.md"] = WikiDoc("""
                    # TR per FR Mapping (MCP Server)

                    | FR | Primary TRs | Tests |
                    | --- | --- | --- |
                    | FR-MCP-101 | TR-MCP-101 | TEST-MCP-101 |
                    """, older),
                ["github/.mcp-requirements-manifest.json"] = WikiDoc("""{"generatedAtUtc":"2026-05-08T12:00:00Z"}""", newer),
                ["github/Functional-Requirements.md"] = WikiDoc("""
                    # Functional Requirements (MCP Server)

                    ## FR-MCP-888 GitHub Entry

                    GitHub body.
                    """, newer),
                ["github/Technical-Requirements.md"] = WikiDoc("""
                    # Technical Requirements (MCP Server)

                    ## TR-MCP-888

                    GitHub TR body.
                    """, newer),
                ["github/Testing-Requirements.md"] = WikiDoc("""
                    # Testing Requirements (MCP Server)

                    - TEST-MCP-888: GitHub test.
                    """, newer),
                ["github/TR-per-FR-Mapping.md"] = WikiDoc("""
                    # TR per FR Mapping (MCP Server)

                    | FR | Primary TRs | Tests |
                    | --- | --- | --- |
                    | FR-MCP-888 | TR-MCP-888 | TEST-MCP-888 |
                    """, newer),
            }
        };

        var ingestResponse = await _client.PostAsJsonAsync("/mcpserver/requirements/ingest", payload).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, ingestResponse.StatusCode);

        using var body = JsonDocument.Parse(await ingestResponse.Content.ReadAsStringAsync().ConfigureAwait(true));
        Assert.Equal("github", body.RootElement.GetProperty("selectedWikiFormat").GetString());
        Assert.True(body.RootElement.GetProperty("functionalDeleted").GetInt32() >= 1);

        var selectedFr = await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-888").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, selectedFr.StatusCode);

        var deletedSeedFr = await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-001").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, deletedSeedFr.StatusCode);
    }

    private static object WikiDoc(string content, DateTimeOffset lastModifiedUtc)
        => new
        {
            content,
            lastModifiedUtc
        };

    private async Task<T> GetJsonAsync<T>(string path)
    {
        var response = await _client.GetAsync(path).ConfigureAwait(true);
        await AssertSuccessAsync(response).ConfigureAwait(true);
        return await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(true)
               ?? throw new InvalidOperationException($"Endpoint '{path}' returned an empty JSON body.");
    }

    private static async Task AssertSuccessAsync(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Equal(expected, response.StatusCode);
        Assert.True(response.IsSuccessStatusCode, $"Request failed ({response.StatusCode}): {body}");
    }

    private static async Task AssertBatchSuccessAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(body);
        Assert.True(result.RootElement.GetProperty("success").GetBoolean(), body);
    }

    private static object[] Criteria(string id, string text, bool isSatisfied, string evidence) =>
    [
        new
        {
            id,
            text,
            isSatisfied,
            evidence
        }
    ];

    private static void AssertCriterion(
        IReadOnlyList<AcceptanceCriterion>? criteria,
        string expectedId,
        string expectedText,
        bool expectedSatisfied,
        string expectedEvidence)
    {
        var criterion = Assert.Single(criteria ?? []);
        Assert.Equal(expectedId, criterion.Id);
        Assert.Equal(expectedText, criterion.Text);
        Assert.Equal(expectedSatisfied, criterion.IsSatisfied);
        Assert.Equal(expectedEvidence, criterion.Evidence);
    }

    /// <summary>WebApplicationFactory that seeds a temporary requirements docs workspace.</summary>
    public sealed class RequirementsWebFactory : WebApplicationFactory<McpApiEntryPoint>, IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "mcp-reqctrl-tests-" + Guid.NewGuid().ToString("N")[..8]);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            SeedWorkspaceFiles();
            var databasePath = Path.Combine(_tempDir, "mcp.db");

            builder.UseEnvironment("Test");
            builder.UseContentRoot(CustomWebApplicationFactory.ResolveContentRoot());
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DataFolder", _tempDir },
                    { "Mcp:DataSource", databasePath },
                    { "Mcp:Database:Provider", "sqlite" },
                    { "Mcp:Database:Sqlite:DataSource", databasePath },
                    { "Mcp:UseInMemoryDatabaseForTests", "false" },
                    { "Mcp:RepoRoot", _tempDir },
                    { "Mcp:Workspaces:0:WorkspacePath", _tempDir },
                    { "Mcp:Workspaces:0:Name", Path.GetFileName(_tempDir) },
                    { "Mcp:Workspaces:0:TodoPath", "docs/Project/TODO.yaml" },
                    { "Mcp:Workspaces:0:IsPrimary", "true" },
                    { "Mcp:Workspaces:0:IsEnabled", "true" }
                });
            });
            builder.ConfigureServices(services =>
            {
                ConfigureTestDatabase(services, databasePath);
                services.RemoveAll<IWorkspaceProjectionWriter>();
                services.AddSingleton<IWorkspaceProjectionWriter, NoOpWorkspaceProjectionWriter>();
                services.AddHostedService<TestDatabaseInitializer>();
            });
        }

        private static void ConfigureTestDatabase(IServiceCollection services, string databasePath)
        {
            var connectionString = $"Data Source={databasePath}";
            var providerOptions = McpDatabaseProviderFactory.CreateOptions("sqlite", connectionString);

            services.RemoveAll<McpDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<McpDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<McpDbContext>>();
            services.RemoveAll<McpDatabaseProviderOptions>();
            services.RemoveAll<McpDatabaseRuntimeOptions>();
            services.AddSingleton(providerOptions);
            services.AddSingleton(new McpDatabaseRuntimeOptions(
                providerOptions,
                new McpDatabaseEncryptionOptions(
                    enabled: false,
                    sqliteKey: null,
                    sqliteSeeToolPath: null,
                    postgreSqlKeyProvider: null,
                    postgreSqlPrincipalKey: null,
                    sqlServerCertificateName: null,
                    sqlServerDatabaseEncryptionKeyName: null)));
            services.AddDbContext<McpDbContext>(options =>
            {
                McpDatabaseProviderFactory.Configure(options, providerOptions);
                options.EnableSensitiveDataLogging();
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        }

        private void SeedWorkspaceFiles()
        {
            var projectDir = Path.Combine(_tempDir, "docs", "Project");
            Directory.CreateDirectory(projectDir);

            File.WriteAllText(Path.Combine(projectDir, "TODO.yaml"), """
                mvp-app:
                  high-priority:
                    - id: TEST-BOOT-001
                      title: Seed TODO for test host startup
                      done: false
                """);

            File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), """
                # Functional Requirements (MCP Server)

                ## FR-MCP-001 Seed Entry

                Seed FR body.
                """);

            File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), """
                # Technical Requirements (MCP Server)

                ## TR-MCP-001

                Seed TR body.
                """);

            File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), """
                # Testing Requirements (MCP Server)

                - TEST-MCP-001: Seed test requirement.
                """);

            File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), """
                # TR per FR Mapping (MCP Server)

                | FR | Primary TRs |
                | --- | --- |
                | FR-MCP-001 | TR-MCP-001 |
                """);
        }

        public string GetFullWorkspaceApiKey()
        {
            var tokenService = Services.GetRequiredService<WorkspaceTokenService>();
            return tokenService.GetToken(_tempDir)
                   ?? throw new InvalidOperationException("Workspace full API key was not generated for test host.");
        }

        public new void Dispose()
        {
            base.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private sealed class TestDatabaseInitializer : IHostedService
        {
            private readonly IServiceProvider _services;

            public TestDatabaseInitializer(IServiceProvider services)
            {
                _services = services;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
                await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
