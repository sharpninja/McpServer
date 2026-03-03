using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Integration tests for requirements management endpoints.</summary>
public sealed class RequirementsControllerTests : IClassFixture<RequirementsControllerTests.RequirementsWebFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly RequirementsWebFactory _factory;

    public RequirementsControllerTests(RequirementsWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", factory.GetFullWorkspaceApiKey());
    }

    public void Dispose() => _client.Dispose();

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

        var deleteResponse = await _client.DeleteAsync("/mcpserver/requirements/fr/FR-MCP-999").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getDeletedResponse = await _client.GetAsync("/mcpserver/requirements/fr/FR-MCP-999").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task GenerateAll_ReturnsZipWithFourDocuments()
    {
        var response = await _client.GetAsync("/mcpserver/requirements/generate?doc=all").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(true);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var names = zip.Entries.Select(e => e.FullName).OrderBy(static n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            new[]
            {
                "Functional-Requirements.md",
                "TR-per-FR-Mapping.md",
                "Technical-Requirements.md",
                "Testing-Requirements.md"
            },
            names);
    }

    /// <summary>WebApplicationFactory that seeds a temporary requirements docs workspace.</summary>
    public sealed class RequirementsWebFactory : WebApplicationFactory<McpApiEntryPoint>, IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "mcp-reqctrl-tests-" + Guid.NewGuid().ToString("N")[..8]);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            SeedWorkspaceFiles();

            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Mcp:DataSource", ":memory:" },
                    { "Mcp:RepoRoot", _tempDir }
                });
            });
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

        private new void Dispose()
        {
            base.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }
    }
}
