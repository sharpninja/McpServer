using System.Diagnostics;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.RequirementsWikiExportOrchestratorTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>TEST-MCP-DOCFXWIKI-001: Shared requirements wiki export orchestrator delegation coverage.</summary>
/// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/RequirementsWikiExportOrchestratorTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 4 Category=Integration cases and the helpers only they use (AssertPublishedDocFxArtifacts and PassthroughProcessEnvironmentService) moved unchanged with the class attributes; the helpers both halves use come from Services/RequirementsWikiExportOrchestratorTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class RequirementsWikiExportOrchestratorTests
{
    /// <summary>Omitted DocFX configuration does not run the external DocFX workflow runner.</summary>
    /// <remarks>Integration: exercises physical wiki output, staging cleanup or native DocFX tool execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ExportAsync_WhenDocFxSectionIsOmitted_DoesNotRunDocFxRunner()
    {
        using var workspace = new RequirementsWikiWorkspace();
        workspace.SeedCanonicalDocs();
        workspace.WriteWikiConfigWithoutDocFx();
        var docFxRunner = new RecordingDocFxWorkflowRunner();
        var orchestrator = new RequirementsWikiExportOrchestrator(docFxRunner);
        var outputRoot = Path.Combine(workspace.Path, "wiki-output");
        var request = new RequirementsWikiExportRequest(
            outputRoot,
            new DateTimeOffset(2026, 7, 13, 23, 0, 0, TimeSpan.Zero),
            workspace.Path,
            workspace.CreateOptions(),
            [new FrEntry("FR-MCP-DOCFX-001", "FR", "Body")],
            [new TrEntry("TR-MCP-DOCFX-001", "TR", "Body")],
            [new TestEntry("TEST-MCP-DOCFX-001", "Test")],
            [new FrTrMapping("FR-MCP-DOCFX-001", ["TR-MCP-DOCFX-001"], ["TEST-MCP-DOCFX-001"])],
            null);

        var result = await orchestrator.ExportAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, docFxRunner.CallCount);
        Assert.True(result.Success);
        Assert.Contains(result.Files, file => file.RelativePath == "azure/Home.md");
        Assert.Contains(result.Files, file => file.RelativePath == "github/Home.md");
    }

    /// <summary>DocFX artifacts are included in the selected platform manifest and omitted from unselected platforms.</summary>
    /// <remarks>Integration: exercises physical wiki output, staging cleanup or native DocFX tool execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ExportAsync_WithDocFxArtifacts_IncludesArtifactsInSelectedPlatformManifestOnly()
    {
        using var workspace = new RequirementsWikiWorkspace();
        workspace.SeedCanonicalDocs();
        workspace.WriteWikiConfigWithDocFx(platforms: ["github"]);
        var docFxRunner = new RecordingDocFxWorkflowRunner
        {
            Documents =
            [
                new RequirementsRenderedDocument("github/api/index.html", "<html>api</html>", "text/html")
            ]
        };
        var orchestrator = new RequirementsWikiExportOrchestrator(docFxRunner);
        var outputRoot = Path.Combine(workspace.Path, "wiki-output");

        var result = await orchestrator.ExportAsync(workspace.CreateRequest(outputRoot), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, docFxRunner.CallCount);
        Assert.Contains(result.Files, file => file.RelativePath == "github/api/index.html");
        Assert.DoesNotContain(result.Files, file => file.RelativePath == "azure/api/index.html");
        var githubManifest = await File.ReadAllTextAsync(Path.Combine(outputRoot, "github", RequirementsWikiDocumentRenderer.ManifestFileName), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var azureManifest = await File.ReadAllTextAsync(Path.Combine(outputRoot, "azure", RequirementsWikiDocumentRenderer.ManifestFileName), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("api/index.html", githubManifest, StringComparison.Ordinal);
        Assert.DoesNotContain("api/index.html", azureManifest, StringComparison.Ordinal);
    }

    /// <summary>Stale DocFX artifacts from a previous wiki export are removed by the shared writer.</summary>
    /// <remarks>Integration: exercises physical wiki output, staging cleanup or native DocFX tool execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ExportAsync_RemovesStaleDocFxArtifactsThroughWriterCleanup()
    {
        using var workspace = new RequirementsWikiWorkspace();
        workspace.SeedCanonicalDocs();
        workspace.WriteWikiConfigWithDocFx(platforms: ["github"]);
        var outputRoot = Path.Combine(workspace.Path, "wiki-output");
        Directory.CreateDirectory(Path.Combine(outputRoot, "github", "api"));
        var staleArtifact = Path.Combine(outputRoot, "github", "api", "old.html");
        await File.WriteAllTextAsync(staleArtifact, "old", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var docFxRunner = new RecordingDocFxWorkflowRunner
        {
            Documents =
            [
                new RequirementsRenderedDocument("github/api/index.html", "<html>new</html>", "text/html")
            ]
        };
        var orchestrator = new RequirementsWikiExportOrchestrator(docFxRunner);

        await orchestrator.ExportAsync(workspace.CreateRequest(outputRoot), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(File.Exists(staleArtifact));
        Assert.True(File.Exists(Path.Combine(outputRoot, "github", "api", "index.html")));
    }

    /// <summary>Real DocFX 2.78.3 local-tool output is published through both wiki roots and staging is deleted.</summary>
    /// <remarks>Integration: exercises physical wiki output, staging cleanup or native DocFX tool execution (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ExportAsync_WithRealDocFxScratchWorkspace_PublishesArtifactsAndDeletesStaging()
    {
        using var workspace = new RequirementsWikiWorkspace();
        workspace.SeedCanonicalDocs();
        workspace.SeedDocFxProject();
        workspace.WriteWikiConfigWithDocFx(arguments: ["tool", "run", "docfx", "docfx.json"]);
        var outputRoot = Path.Combine(workspace.Path, "wiki-output");
        var processRunner = new ProcessRunner(
            new PassthroughProcessEnvironmentService(),
            Microsoft.Extensions.Options.Options.Create(new ProcessRunnerOptions()),
            NullLogger<ProcessRunner>.Instance);
        var restore = await processRunner.RunAsync(
            new ProcessRunRequest("dotnet", string.Empty, WorkingDirectory: workspace.Path, ArgumentList: ["tool", "restore"]),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(restore.ExitCode == 0, $"dotnet tool restore failed. Stdout: {restore.Stdout} Stderr: {restore.Stderr}");
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);
        var orchestrator = new RequirementsWikiExportOrchestrator(runner);

        var result = await orchestrator.ExportAsync(workspace.CreateRequest(outputRoot), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        AssertPublishedDocFxArtifacts(outputRoot, "azure");
        AssertPublishedDocFxArtifacts(outputRoot, "github");
        Assert.Contains(result.Files, file => file.RelativePath == "azure/api/index.html");
        Assert.Contains(result.Files, file => file.RelativePath == "github/api/index.html");
        Assert.False(Directory.Exists(Path.Combine(workspace.Path, "docs", "docfx", "_site")));
    }

    private static void AssertPublishedDocFxArtifacts(string outputRoot, string platform)
    {
        var indexPath = Path.Combine(outputRoot, platform, "api", "index.html");
        var guidePath = Path.Combine(outputRoot, platform, "api", "guide.html");
        var rawMarkdownPath = Path.Combine(outputRoot, platform, "api", "resources", "raw-note.md");
        Assert.True(File.Exists(indexPath), $"Missing {indexPath}.");
        Assert.True(File.Exists(guidePath), $"Missing {guidePath}.");
        Assert.True(File.Exists(rawMarkdownPath), $"Missing {rawMarkdownPath}.");
        Assert.Contains("Scratch DocFX Home", File.ReadAllText(indexPath), StringComparison.Ordinal);
        Assert.Contains("Scratch DocFX Guide", File.ReadAllText(guidePath), StringComparison.Ordinal);
        Assert.Contains("Raw copied markdown", File.ReadAllText(rawMarkdownPath), StringComparison.Ordinal);
    }

    private sealed class PassthroughProcessEnvironmentService : IProcessEnvironmentService
    {
        public void ApplyGitHubToken(ProcessStartInfo psi, string? token)
        {
        }

        public void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser)
        {
        }

        public void ApplyAll(ProcessStartInfo psi, string? runAsUser, string? gitHubToken)
        {
        }

        public string ResolveExecutable(ProcessStartInfo psi, string fileName) => fileName;
    }
}
