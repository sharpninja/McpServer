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

        var fr = await service.GetAllFrAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var tr = await service.GetAllTrAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var test = await service.GetAllTestAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var mapping = await service.GetAllMappingsAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        Assert.Single(mapping[0].TestIds);
        Assert.Equal("TEST-MCP-001", mapping[0].TestIds[0]);

        var functionalDoc = await service.GenerateDocumentAsync(RequirementsDocType.Functional, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("text/markdown", functionalDoc.MimeType);
        Assert.Contains("# Functional Requirements (MCP Server)", functionalDoc.Content);
        Assert.Contains("## FR-MCP-001 Configurable workspace root and paths", functionalDoc.Content);

        var technicalDoc = await service.GenerateDocumentAsync(RequirementsDocType.Technical, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("## TR-MCP-WS-004", technicalDoc.Content);
        Assert.Contains("**Workspace Controller** — REST API", technicalDoc.Content);

        var matrixDoc = await service.GenerateDocumentAsync(RequirementsDocType.Matrix, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("# Requirements Matrix (MCP Server)", matrixDoc.Content);
        Assert.Contains("| FR-MCP-001 | Tracked | Functional-Requirements.md |", matrixDoc.Content);
        Assert.Contains("| TR-MCP-CFG-001 | Tracked | Technical-Requirements.md |", matrixDoc.Content);
        Assert.Contains("| TEST-MCP-001 | Tracked | Testing-Requirements.md |", matrixDoc.Content);
    }

    [Fact]
    public async Task GenerateAllAsync_WritesCanonicalFilesToWorkspace()
    {
        SeedCanonicalDocs();
        var service = CreateService();
        var outputRoot = Path.Combine(_tempRoot, "export", "canonical");
        var generatedAt = new DateTimeOffset(2026, 5, 8, 12, 0, 0, TimeSpan.Zero);

        var result = await service.GenerateAllAsync(outputRoot, generatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var names = result.Files.Select(e => e.RelativePath).OrderBy(static n => n, StringComparer.Ordinal).ToArray();

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
        Assert.True(result.Success);
        Assert.Equal("markdown", result.Format);
        Assert.Equal(Path.GetFullPath(outputRoot), result.OutputRoot);
        Assert.Contains("FR-MCP-001", File.ReadAllText(Path.Combine(outputRoot, RequirementsDocumentRenderer.FunctionalFileName)));
        Assert.Contains("TEST-MCP-001", File.ReadAllText(Path.Combine(outputRoot, RequirementsDocumentRenderer.MatrixFileName)));
        Assert.Equal(generatedAt.UtcDateTime, File.GetLastWriteTimeUtc(Path.Combine(outputRoot, RequirementsDocumentRenderer.FunctionalFileName)));
    }

    /// <summary>
    /// MCP-REQEXPORT-READONLY-001: Requirements markdown exports are projections and are
    /// marked read-only so agents do not accidentally edit them as the source of truth.
    /// </summary>
    [Fact]
    public async Task GenerateAllAsync_MarksExportedMarkdownFilesReadOnly()
    {
        SeedCanonicalDocs();
        var service = CreateService();
        var outputRoot = Path.Combine(_tempRoot, "export", "canonical-readonly");

        var result = await service.GenerateAllAsync(outputRoot, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.All(result.Files, file =>
        {
            var attributes = File.GetAttributes(file.FullPath);
            Assert.True(
                attributes.HasFlag(FileAttributes.ReadOnly),
                $"{file.RelativePath} should be marked read-only.");
        });
    }

    [Fact]
    public async Task GenerateAllAsync_PreservesExistingMatrixRowsAndAppendsMissingIds()
    {
        SeedCanonicalDocs();
        var projectMatrixPath = Path.Combine(_tempRoot, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName);
        await File.WriteAllTextAsync(projectMatrixPath, """
            # Requirements Matrix (MCP Server)

            | Requirement | Status | Source Files |
            | --- | --- | --- |
            | FR-MCP-001 | Complete | ExistingSource |
            """, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var service = CreateService();
        var outputRoot = Path.Combine(_tempRoot, "export", "canonical-with-matrix");

        await service.GenerateAllAsync(outputRoot, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var matrix = await File.ReadAllTextAsync(Path.Combine(outputRoot, RequirementsDocumentRenderer.MatrixFileName), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("| FR-MCP-001 | Complete | ExistingSource |", matrix);
        Assert.Contains("| FR-MCP-002 | Tracked | Functional-Requirements.md |", matrix);
        Assert.Contains("| TR-MCP-CFG-001 | Tracked | Technical-Requirements.md |", matrix);
        Assert.Contains("| TEST-MCP-001 | Tracked | Testing-Requirements.md |", matrix);
    }

    [Fact]
    public async Task GenerateWikiAsync_WritesAzureAndGitHubFoldersWithMetadata()
    {
        SeedCanonicalDocs();
        var service = CreateService();
        var outputRoot = Path.Combine(_tempRoot, "docs", "Project", "wiki");
        Directory.CreateDirectory(Path.Combine(outputRoot, "azure"));
        File.WriteAllText(Path.Combine(outputRoot, "azure", "Old.md"), "stale");

        var generatedAt = new DateTimeOffset(2026, 5, 8, 12, 0, 0, TimeSpan.Zero);
        var result = await service.GenerateWikiAsync(outputRoot, generatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var names = result.Files.Select(e => e.RelativePath).OrderBy(static n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(
            new[]
            {
                "azure/.mcp-requirements-manifest.json",
                "azure/.order",
                "azure/Functional-Requirements.md",
                "azure/Home.md",
                "azure/Requirements-Matrix.md",
                "azure/TR-per-FR-Mapping.md",
                "azure/Technical-Requirements.md",
                "azure/Testing-Requirements.md",
                "github/.mcp-requirements-manifest.json",
                "github/Functional-Requirements.md",
                "github/Home.md",
                "github/Requirements-Matrix.md",
                "github/TR-per-FR-Mapping.md",
                "github/Technical-Requirements.md",
                "github/Testing-Requirements.md",
                "github/_Footer.md",
                "github/_Sidebar.md"
            },
            names);

        var manifest = File.ReadAllText(Path.Combine(outputRoot, "azure", ".mcp-requirements-manifest.json"));
        Assert.Contains("\"platform\": \"azure\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"generatedAtUtc\": \"2026-05-08T12:00:00+00:00\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"Requirements-Matrix.md\"", manifest, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(outputRoot, "azure", "Old.md")));
        Assert.Equal(generatedAt.UtcDateTime, File.GetLastWriteTimeUtc(Path.Combine(outputRoot, "github", "_Sidebar.md")));
        Assert.All(result.Files, file => Assert.True(File.GetAttributes(file.FullPath).HasFlag(FileAttributes.ReadOnly)));
    }

    /// <summary>
    /// TEST-MCP-106: Wiki export temporarily clears read-only files for replacement/deletion,
    /// then leaves exported files read-only after generation completes.
    /// </summary>
    [Fact]
    public async Task GenerateWikiAsync_ReplacesExistingReadOnlyFilesAndRestoresReadOnly()
    {
        SeedCanonicalDocs();
        var service = CreateService();
        var outputRoot = Path.Combine(_tempRoot, "docs", "Project", "wiki-readonly");
        var existingExport = Path.Combine(outputRoot, "github", RequirementsDocumentRenderer.FunctionalFileName);
        var staleExport = Path.Combine(outputRoot, "azure", "Old.md");
        Directory.CreateDirectory(Path.GetDirectoryName(existingExport)!);
        Directory.CreateDirectory(Path.GetDirectoryName(staleExport)!);
        await File.WriteAllTextAsync(existingExport, "stale generated export", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(staleExport, "stale export", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        File.SetAttributes(existingExport, File.GetAttributes(existingExport) | FileAttributes.ReadOnly);
        File.SetAttributes(staleExport, File.GetAttributes(staleExport) | FileAttributes.ReadOnly);

        var result = await service.GenerateWikiAsync(outputRoot, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var rewritten = await File.ReadAllTextAsync(existingExport, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("FR-MCP-001", rewritten, StringComparison.Ordinal);
        Assert.False(File.Exists(staleExport));
        Assert.All(result.Files, file => Assert.True(
            File.GetAttributes(file.FullPath).HasFlag(FileAttributes.ReadOnly),
            $"{file.RelativePath} should be read-only after export."));
    }

    [Fact]
    public async Task GenerateWikiAsync_RendersTestingRequirementsAsGroupedSectionsWithAcceptanceCriteria()
    {
        SeedGroupedTestingDocs();
        var service = CreateService();
        var outputRoot = Path.Combine(_tempRoot, "docs", "Project", "wiki");
        var generatedAt = new DateTimeOffset(2026, 5, 22, 16, 30, 0, TimeSpan.Zero);

        await service.GenerateWikiAsync(outputRoot, generatedAt, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var githubTesting = await File.ReadAllTextAsync(
            Path.Combine(outputRoot, "github", RequirementsDocumentRenderer.TestingFileName), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var azureTesting = await File.ReadAllTextAsync(
            Path.Combine(outputRoot, "azure", RequirementsDocumentRenderer.TestingFileName), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(githubTesting, azureTesting);
        Assert.Contains("## TEST-MCP", githubTesting);
        Assert.Contains("### TEST-MCP-001", githubTesting);
        Assert.Contains("Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.", githubTesting);
        Assert.Contains("**Acceptance Criteria:**", githubTesting);
        Assert.Contains("- [ ] Path resolution is covered by a focused assertion.", githubTesting);
        Assert.Contains("- [x] Validation evidence is retained. (evidence: RequirementsDocumentServiceTests)", githubTesting);
        Assert.Contains("## TEST-MCP-REPL", githubTesting);
        Assert.Contains("### TEST-MCP-REPL-021", githubTesting);
        Assert.Contains("Given `TryResolveWithDiagnostics` with a workspace path containing no marker file, when called, then the error message enumerates every directory walked.", githubTesting);
        Assert.Contains("## TEST-SUPPORT", githubTesting);
        Assert.Contains("### TEST-SUPPORT-016", githubTesting);
        Assert.Contains("Given a `SessionLogService`, when `SubmitAsync` persists a session, then child entities keep the workspace id.", githubTesting);
        Assert.DoesNotContain("| ID | Requirement |", githubTesting);
        Assert.DoesNotContain("- TEST-MCP-001:", githubTesting);
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
        var content = await File.ReadAllTextAsync(testingPath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var parsed = RequirementsDocumentParser.ParseTesting(content);

        Assert.Equal(20, parsed.Count);
        Assert.DoesNotContain(Directory.GetFiles(Path.GetDirectoryName(testingPath)!, "*.tmp"), _ => true);
    }

    [Fact]
    public async Task DeleteTrAsync_RemovesTrFromMappingRows()
    {
        SeedCanonicalDocs();
        var service = CreateService();

        await service.DeleteTrAsync("TR-MCP-CFG-002", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var mapping = await service.GetMappingAsync("FR-MCP-001", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(mapping);
        Assert.Single(mapping!.TrIds);
        Assert.Equal("TR-MCP-CFG-001", mapping.TrIds[0]);
    }

    [Fact]
    public void ParseMapping_AcceptsLegacyTwoColumnRows()
    {
        var parsed = RequirementsDocumentParser.ParseMapping("""
            # TR per FR Mapping (MCP Server)

            | FR | Primary TRs |
            | --- | --- |
            | FR-MCP-001 | TR-MCP-CFG-001, TR-MCP-CFG-002 |
            """);

        var mapping = Assert.Single(parsed);
        Assert.Equal("FR-MCP-001", mapping.FrId);
        Assert.Equal(2, mapping.TrIds.Count);
        Assert.Empty(mapping.TestIds);
    }

    [Fact]
    public void ParseTesting_AcceptsWikiGroupedTableRows()
    {
        var parsed = RequirementsDocumentParser.ParseTesting("""
            # Testing Requirements (MCP Server)

            ## TEST-MCP

            | ID | Requirement |
            | --- | --- |
            | TEST-MCP-001 | Given A \| B, when C, then D. |

            ## TEST-MCP-REPL

            | ID | Requirement |
            | --- | --- |
            | TEST-MCP-REPL-021 | Given marker diagnostics, when no marker exists, then searched paths are listed. |
            """);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("TEST-MCP-001", parsed[0].Id);
        Assert.Equal("Given A | B, when C, then D.", parsed[0].Condition);
        Assert.Equal("TEST-MCP-REPL-021", parsed[1].Id);
    }

    [Fact]
    public void ParseTesting_AcceptsWikiGroupedSectionsWithAcceptanceCriteria()
    {
        var parsed = RequirementsDocumentParser.ParseTesting("""
            # Testing Requirements (MCP Server)

            ## TEST-MCP

            ### TEST-MCP-AC-001

            Given exported requirements, when the wiki is generated, then descriptions are readable.

            **Acceptance Criteria:**
            - [ ] First criterion
            - [x] Second criterion (evidence: parser-test)
            """);

        var entry = Assert.Single(parsed);
        Assert.Equal("TEST-MCP-AC-001", entry.Id);
        Assert.Equal("Given exported requirements, when the wiki is generated, then descriptions are readable.", entry.Condition);
        Assert.Equal(2, entry.AcceptanceCriteria?.Count);
        Assert.False(entry.AcceptanceCriteria![0].IsSatisfied);
        Assert.Equal("First criterion", entry.AcceptanceCriteria[0].Text);
        Assert.True(entry.AcceptanceCriteria[1].IsSatisfied);
        Assert.Equal("Second criterion", entry.AcceptanceCriteria[1].Text);
        Assert.Equal("parser-test", entry.AcceptanceCriteria[1].Evidence);
    }

    private RequirementsDocumentService CreateService()
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

    private void SeedEmptyDocs()
    {
        var projectDir = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), "# Functional Requirements (MCP Server)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), "# Technical Requirements (MCP Server)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), "# Testing Requirements (MCP Server)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), "# TR per FR Mapping (MCP Server)\n\n| FR | Primary TRs | Tests |\n| --- | --- | --- |\n");
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

            **Workspace Controller** — REST API at `/mcpserver/workspace` with Base64URL-encoded path keys.
            """);

        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), """
            # Testing Requirements (MCP Server)

            - TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
            - TEST-MCP-002: Given TODO API operations, when create/update/delete/query run, then contracts remain stable.
            """);

        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), """
            # TR per FR Mapping (MCP Server)

            | FR | Primary TRs | Tests |
            | --- | --- | --- |
            | FR-MCP-001 | TR-MCP-CFG-001, TR-MCP-CFG-002 | TEST-MCP-001 |
            """);
    }

    private void SeedGroupedTestingDocs()
    {
        var projectDir = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), """
            # Functional Requirements (MCP Server)

            ## FR-MCP-001 Configurable workspace root and paths

            The server shall support configurable paths.
            """);

        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), """
            # Technical Requirements (MCP Server)

            ## TR-MCP-CFG-001

            Configuration paths are resolved through options.
            """);

        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), """
            # Testing Requirements (MCP Server)

            - TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
              **Acceptance Criteria:**
              - [ ] Path resolution is covered by a focused assertion.
              - [x] Validation evidence is retained. (evidence: RequirementsDocumentServiceTests)
            - TEST-MCP-REPL-021: Given `TryResolveWithDiagnostics` with a workspace path containing no marker file, when called, then the error message enumerates every directory walked.
            - TEST-SUPPORT-016: Given a `SessionLogService`, when `SubmitAsync` persists a session, then child entities keep the workspace id.
            """);

        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), """
            # TR per FR Mapping (MCP Server)

            | FR | Primary TRs | Tests |
            | --- | --- | --- |
            | FR-MCP-001 | TR-MCP-CFG-001 | TEST-MCP-001 |
            """);
    }
}
