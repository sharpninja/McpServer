using System.IO.Compression;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Unit tests for <see cref="RequirementsDocumentService"/> document parsing, rendering, and persistence behavior.</summary>
public sealed class RequirementsDocumentServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "mcp-reqdocs-tests-" + Guid.NewGuid().ToString("N")[..8]);

    public RequirementsDocumentServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "docs", "Project"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task Constructor_ParsesCanonicalDocs_AndGeneratesMarkdown()
    {
        SeedCanonicalDocs();
        var service = CreateService();

        var fr = await service.GetAllFrAsync().ConfigureAwait(true);
        var tr = await service.GetAllTrAsync().ConfigureAwait(true);
        var test = await service.GetAllTestAsync().ConfigureAwait(true);
        var mapping = await service.GetAllMappingsAsync().ConfigureAwait(true);

        Assert.Equal(2, fr.Count);
        Assert.Equal("FR-MCP-001", fr[0].Id);
        Assert.Equal("Configurable workspace root and paths", fr[0].Title);

        Assert.Equal(3, tr.Count);
        Assert.Equal("Workspace Controller", tr[2].Title);
        Assert.Contains("REST API", tr[2].Body);

        Assert.Equal(2, test.Count);
        Assert.Single(mapping);
        Assert.Equal("FR-MCP-001", mapping[0].FrId);
        Assert.Equal(2, mapping[0].TrIds.Count);

        var functionalDoc = await service.GenerateDocumentAsync(RequirementsDocType.Functional).ConfigureAwait(true);
        Assert.Equal("text/markdown", functionalDoc.MimeType);
        Assert.Contains("# Functional Requirements (MCP Server)", functionalDoc.Content);
        Assert.Contains("## FR-MCP-001 Configurable workspace root and paths", functionalDoc.Content);

        var technicalDoc = await service.GenerateDocumentAsync(RequirementsDocType.Technical).ConfigureAwait(true);
        Assert.Contains("## TR-MCP-WS-004", technicalDoc.Content);
        Assert.Contains("**Workspace Controller** — REST API", technicalDoc.Content);
    }

    [Fact]
    public async Task GenerateAllAsync_ReturnsZipWithCanonicalFiles()
    {
        SeedCanonicalDocs();
        var service = CreateService();

        await using var zipStream = await service.GenerateAllAsync().ConfigureAwait(true);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);
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

    [Fact]
    public async Task ConcurrentMutations_ProduceValidTestingDocument()
    {
        SeedEmptyDocs();
        var service = CreateService();

        var tasks = Enumerable.Range(1, 20)
            .Select(i => service.AddTestAsync(new TestEntry($"TEST-MCP-{100 + i:000}", $"Condition {i}")))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(true);

        var testingPath = Path.Combine(_tempRoot, "docs", "Project", "Testing-Requirements.md");
        var content = await File.ReadAllTextAsync(testingPath).ConfigureAwait(true);
        var parsed = RequirementsDocumentParser.ParseTesting(content);

        Assert.Equal(20, parsed.Count);
        Assert.DoesNotContain(Directory.GetFiles(Path.GetDirectoryName(testingPath)!, "*.tmp"), _ => true);
    }

    [Fact]
    public async Task DeleteTrAsync_RemovesTrFromMappingRows()
    {
        SeedCanonicalDocs();
        var service = CreateService();

        await service.DeleteTrAsync("TR-MCP-CFG-002").ConfigureAwait(true);
        var mapping = await service.GetMappingAsync("FR-MCP-001").ConfigureAwait(true);

        Assert.NotNull(mapping);
        Assert.Single(mapping!.TrIds);
        Assert.Equal("TR-MCP-CFG-001", mapping.TrIds[0]);
    }

    private RequirementsDocumentService CreateService()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RequirementsOptions
        {
            FunctionalRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Functional-Requirements.md"),
            TechnicalRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Technical-Requirements.md"),
            TestingRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Testing-Requirements.md"),
            MappingPath = Path.Combine(_tempRoot, "docs", "Project", "TR-per-FR-Mapping.md")
        });

        return new RequirementsDocumentService(options, NullLogger<RequirementsDocumentService>.Instance);
    }

    private void SeedEmptyDocs()
    {
        var projectDir = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), "# Functional Requirements (MCP Server)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), "# Technical Requirements (MCP Server)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), "# Testing Requirements (MCP Server)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), "# TR per FR Mapping (MCP Server)\n\n| FR | Primary TRs |\n| --- | --- |\n");
    }

    private void SeedCanonicalDocs()
    {
        var projectDir = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), """
            # Functional Requirements (MCP Server)

            ## FR-MCP-001 Configurable workspace root and paths

            The server shall support configurable `RepoRoot`, `TodoFilePath`, `DataDirectory`, and index paths.

            **Covered by:** `IngestionOptions`, `IOptions`

            ## FR-MCP-002 TODO management API

            The server shall provide CRUD/query operations for TODO items over REST and STDIO.

            **Covered by:** `TodoController`, `TodoService`
            """);

        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), """
            # Technical Requirements (MCP Server)

            ## TR-MCP-CFG-001

            IOptions-based configuration for all filesystem and runtime settings.

            ## TR-MCP-CFG-002

            Port selection from `Mcp:Port` with `PORT` env override.

            ## TR-MCP-WS-004

            **Workspace Controller** — REST API at `/mcp/workspace` with Base64URL-encoded path keys.
            """);

        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), """
            # Testing Requirements (MCP Server)

            - TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
            - TEST-MCP-002: Given TODO API operations, when create/update/delete/query run, then contracts remain stable.
            """);

        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), """
            # TR per FR Mapping (MCP Server)

            | FR | Primary TRs |
            | --- | --- |
            | FR-MCP-001 | TR-MCP-CFG-001, TR-MCP-CFG-002 |
            """);
    }
}
