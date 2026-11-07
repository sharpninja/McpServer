using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>Unit tests for <see cref="RequirementsDocumentService"/> document parsing, rendering, and persistence behavior.</summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; TEST-MCP-106; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/RequirementsDocumentServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 6 Category=Integration cases and the helpers only they use (SeedGroupedTestingDocs) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class RequirementsDocumentServiceTestsBase in Services/RequirementsDocumentServiceTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class RequirementsDocumentServiceTests : RequirementsDocumentServiceTestsBase, IDisposable
{
    /// <remarks>Integration: writes and reads physical requirement or wiki exports and their native file attributes (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
    /// <remarks>Integration: writes and reads physical requirement or wiki exports and their native file attributes (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    /// <remarks>Integration: writes and reads physical requirement or wiki exports and their native file attributes (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task GenerateAllAsync_PreservesExistingMatrixRowsAndAppendsMissingIds()
    {
        SeedCanonicalDocs();
        var projectMatrixPath = Path.Combine(_tempRoot, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName);
        await File.WriteAllTextAsync(projectMatrixPath, """
            # Requirements Matrix (QBrain.AI)

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

    /// <remarks>Integration: writes and reads physical requirement or wiki exports and their native file attributes (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
    /// <remarks>Integration: writes and reads physical requirement or wiki exports and their native file attributes (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    /// <remarks>Integration: writes and reads physical requirement or wiki exports and their native file attributes (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    private void SeedGroupedTestingDocs()
    {
        var projectDir = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), """
            # Functional Requirements (QBrain.AI)

            ## FR-MCP-001 Configurable workspace root and paths

            The server shall support configurable paths.
            """);

        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), """
            # Technical Requirements (QBrain.AI)

            ## TR-MCP-CFG-001

            Configuration paths are resolved through options.
            """);

        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), """
            # Testing Requirements (QBrain.AI)

            - TEST-MCP-001: Given configurable RepoRoot/Todo paths, when service starts, then path resolution is correct.
              **Acceptance Criteria:**
              - [ ] Path resolution is covered by a focused assertion.
              - [x] Validation evidence is retained. (evidence: RequirementsDocumentServiceTests)
            - TEST-MCP-REPL-021: Given `TryResolveWithDiagnostics` with a workspace path containing no marker file, when called, then the error message enumerates every directory walked.
            - TEST-SUPPORT-016: Given a `SessionLogService`, when `SubmitAsync` persists a session, then child entities keep the workspace id.
            """);

        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), """
            # TR per FR Mapping (QBrain.AI)

            | FR | Primary TRs | Tests |
            | --- | --- | --- |
            | FR-MCP-001 | TR-MCP-CFG-001 | TEST-MCP-001 |
            """);
    }
}
