using System.Net;
using System.Net.Http.Json;
using System.IO.Compression;
using System.Text.Json;
using McpServer.Support.Mcp;
using McpServer.Support.Mcp.IntegrationTests;
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
        Assert.Contains("| ID | Requirement |", testingMarkdown, StringComparison.Ordinal);
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
