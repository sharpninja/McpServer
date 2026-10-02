using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Overlay G8: wiki export through shipped <see cref="RequirementsDocumentService.GenerateWikiAsync"/>
/// without --include-dump. TEST-MCP-WIKIEXPORT default no-flag behavior.
/// </summary>
public sealed class DocsSyncG8OverlayTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "g8-wiki-" + Guid.NewGuid().ToString("N"));

    /// <summary>Creates an isolated docs tree for wiki export.</summary>
    public DocsSyncG8OverlayTests()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "docs", "Project"));
        var project = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(project, "Functional-Requirements.md"), "# Functional Requirements (MCP Server)\n\n## FR-MCP-G8-001 Overlay wiki\n\nBody.\n");
        File.WriteAllText(Path.Combine(project, "Technical-Requirements.md"), "# Technical Requirements (MCP Server)\n\n## TR-MCP-G8-001\n\nBody.\n");
        File.WriteAllText(Path.Combine(project, "Testing-Requirements.md"), "# Testing Requirements (MCP Server)\n\n- TEST-MCP-G8-001: wiki export.\n");
        File.WriteAllText(Path.Combine(project, "TR-per-FR-Mapping.md"), "# TR per FR Mapping (MCP Server)\n\n| FR | Primary TRs | Tests |\n| --- | --- | --- |\n| FR-MCP-G8-001 | TR-MCP-G8-001 | TEST-MCP-G8-001 |\n");
        File.WriteAllText(Path.Combine(project, "Requirements-Matrix.md"), "# Requirements Matrix (MCP Server)\n");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    /// <summary>G8: default GenerateWikiAsync writes Azure and GitHub wiki folders and does not write mcp-wiki-dump.json.</summary>
    [Fact]
    public async Task G8_GenerateWikiAsync_Default_WritesWikiFoldersWithoutDump()
    {
        var service = new RequirementsDocumentService(
            MsOptions.Options.Create(new RequirementsOptions
            {
                FunctionalRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Functional-Requirements.md"),
                TechnicalRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Technical-Requirements.md"),
                TestingRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Testing-Requirements.md"),
                MappingPath = Path.Combine(_tempRoot, "docs", "Project", "TR-per-FR-Mapping.md"),
                MatrixPath = Path.Combine(_tempRoot, "docs", "Project", "Requirements-Matrix.md"),
            }),
            NullLogger<RequirementsDocumentService>.Instance);

        var outputRoot = Path.Combine(_tempRoot, "docs", "Project", "wiki");
        var result = await service.GenerateWikiAsync(outputRoot, ct: TestContext.Current.CancellationToken);
        Assert.True(result.Success);
        Assert.Contains(result.Files, file => file.RelativePath.Replace('\\', '/') == "azure/Functional-Requirements.md");
        Assert.Contains(result.Files, file => file.RelativePath.Replace('\\', '/') == "github/Home.md");
        Assert.False(File.Exists(Path.Combine(outputRoot, WikiDumpDefaults.FileName)));
        Assert.False(File.Exists(Path.Combine(outputRoot, "azure", WikiDumpDefaults.FileName)));
        Assert.False(File.Exists(Path.Combine(outputRoot, "github", WikiDumpDefaults.FileName)));
    }
}
