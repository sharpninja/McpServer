using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-USECASE-018 AC-006; TEST-MCP-106: the per-test state and helpers that both halves of RequirementsDocumentServiceTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; TEST-MCP-106; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from RequirementsDocumentServiceTests (_tempRoot, .ctor, Dispose, CreateService and SeedCanonicalDocs) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class RequirementsDocumentServiceTestsBase : IDisposable
{
    internal readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "mcp-reqdocs-tests-" + Guid.NewGuid().ToString("N")[..8]);

    protected RequirementsDocumentServiceTestsBase()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "docs", "Project"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
    }

    internal RequirementsDocumentService CreateService()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RequirementsOptions
        {
            FunctionalRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Functional-Requirements.md"),
            TechnicalRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Technical-Requirements.md"),
            TestingRequirementsPath = Path.Combine(_tempRoot, "docs", "Project", "Testing-Requirements.md"),
            MappingPath = Path.Combine(_tempRoot, "docs", "Project", "TR-per-FR-Mapping.md"),
            MatrixPath = Path.Combine(_tempRoot, "docs", "Project", "Requirements-Matrix.md")
        });

        return new RequirementsDocumentService(options, NullLogger<RequirementsDocumentService>.Instance);
    }

    internal void SeedCanonicalDocs()
    {
        var projectDir = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), """
            # Functional Requirements (QBrain.AI)

            ## FR-MCP-001 Configurable workspace root and paths

            The server shall support configurable `RepoRoot`, `TodoFilePath`, `DataDirectory`, and index paths.

            **Covered by:** `IngestionOptions`, `IOptions`

            ## FR-MCP-002 TODO management API

            The server shall provide CRUD/query operations for TODO items over REST and STDIO.

            **Covered by:** `TodoController`, `TodoService`
            """);

        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), """
            # Technical Requirements (QBrain.AI)

            ## TR-MCP-CFG-001

            IOptions-based configuration for all filesystem and runtime settings.

            ## TR-MCP-CFG-002

            Port selection from `Mcp:Port` with `PORT` env override.

            ## TR-MCP-WS-004

            **Workspace Controller** — REST API at `/qbrainai/workspace` with Base64URL-encoded path keys.
            """);

        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), """
            # Testing Requirements (QBrain.AI)

            - TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
            - TEST-MCP-002: Given TODO API operations, when create/update/delete/query run, then contracts remain stable.
            """);

        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), """
            # TR per FR Mapping (QBrain.AI)

            | FR | Primary TRs | Tests |
            | --- | --- | --- |
            | FR-MCP-001 | TR-MCP-CFG-001, TR-MCP-CFG-002 | TEST-MCP-001 |
            """);
    }
}
