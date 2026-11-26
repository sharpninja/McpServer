using System.IO.Compression;
using System.Text.Json;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.RequirementsWikiExportOrchestratorTestsSupport;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>Verifies paired wiki export artifacts through the file writer and ZIP endpoint.</summary>
/// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiFooterCoverage, ac-WikiFooterValidation,
/// ac-WikiGroupNavigation, ac-WikiGroupInventory, and ac-WikiGroupRoundTrip.</remarks>
[Trait("Category", "Integration")]
public sealed class WikiExportEnhancementsIntegrationTests
{
    private const string GroupPath = "Functional-Requirements--FR-MCP-WIKI.md";
    private const string FooterText = "Shared footer for the test workspace.";
    private const string FooterEnd = "<!-- mcp-wiki-footer:end -->";

    /// <summary>Checks directory, manifest, endpoint ZIP, and obsolete child cleanup in one repeatable export.</summary>
    [Fact]
    public async Task ExportAndArchiveKeepTheSamePagesAndRemoveObsoleteGroups()
    {
        using var workspace = new RequirementsWikiWorkspace();
        Configure(workspace, docFx: true);
        var cancellation = TestContext.Current.CancellationToken;
        var output = Path.Combine(workspace.Path, "wiki-output");
        var request = workspace.CreateRequest(output) with
        {
            Functional = [new("FR-MCP-WIKI-001", "One", "First body"), new("FR-MCP-WIKI-002", "Two", "Second body")],
        };
        var orchestrator = new RequirementsWikiExportOrchestrator(new RecordingDocFxWorkflowRunner
        {
            Documents = [
                new("azure/Api/Guide.md", "# DocFX page", "text/markdown"),
                new("azure/Api/Code.html", "<html>DocFX site</html>", "text/html"),
                new("github/Api/Guide.md", "# DocFX page", "text/markdown"),
            ],
        });
        var export = await orchestrator.ExportAsync(request, cancellation).ConfigureAwait(true);
        var repeated = await orchestrator.ExportAsync(request, cancellation).ConfigureAwait(true);
        var paths = export.Files.Select(static file => file.RelativePath.Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(paths, repeated.Files.Select(static file => file.RelativePath.Replace('\\', '/')).Order(StringComparer.Ordinal));
        foreach (var platform in new[] { "azure", "github" })
        {
            Assert.Contains(platform + "/" + GroupPath, paths, StringComparer.Ordinal);
            var manifest = await File.ReadAllTextAsync(Path.Combine(output, platform, ".mcp-requirements-manifest.json"), cancellation).ConfigureAwait(true);
            using var manifestJson = JsonDocument.Parse(manifest);
            Assert.Equal(GroupPath, Assert.Single(manifestJson.RootElement.GetProperty("groupDocuments").EnumerateArray()).GetString());
            Assert.Contains(manifestJson.RootElement.GetProperty("documents").EnumerateArray(), entry => entry.GetString() == GroupPath);
            Assert.Contains(manifestJson.RootElement.GetProperty("documents").EnumerateArray(), entry => entry.GetString() == "Api/Guide.md");
        }

        string[] pages = ["Home.md", "Functional-Requirements.md", "Technical-Requirements.md", "Testing-Requirements.md",
            "TR-per-FR-Mapping.md", "Requirements-Matrix.md", "Guide.md", "Api/Guide.md", GroupPath];
        foreach (var page in pages)
        {
            var inline = File.ReadAllText(Path.Combine(output, "azure", page));
            Assert.EndsWith(FooterEnd, inline.TrimEnd(), StringComparison.Ordinal);
            Assert.Contains(FooterText, inline, StringComparison.Ordinal);
            Assert.Equal(inline.IndexOf(FooterText, StringComparison.Ordinal), inline.LastIndexOf(FooterText, StringComparison.Ordinal));
            Assert.DoesNotContain(FooterText, File.ReadAllText(Path.Combine(output, "github", page)), StringComparison.Ordinal);
        }
        Assert.Equal(FooterText, File.ReadAllText(Path.Combine(output, "github", "_Footer.md")).Trim());
        Assert.Equal("<html>DocFX site</html>", File.ReadAllText(Path.Combine(output, "azure", "Api", "Code.html")));
        Assert.Equal("# Original guide", await File.ReadAllTextAsync(Path.Combine(workspace.Path, "docs", "guide.md"), cancellation).ConfigureAwait(true));
        var requirements = Substitute.For<IRequirementsDocumentService>();
        requirements.GenerateWikiAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Task.FromResult(export));
        var controller = new RequirementsController(requirements, MsOptions.Create(workspace.CreateOptions()),
            new WorkspaceContext { WorkspacePath = workspace.Path }, Substitute.For<ITodoExecutionService>(),
            NullLogger<RequirementsController>.Instance, transactionCoordinator: null, transactionOptions: null);
        var response = await controller.GenerateAsync("all", "wiki", cancellation).ConfigureAwait(true);
        using var archive = new ZipArchive(new MemoryStream(Assert.IsType<FileContentResult>(response).FileContents));
        Assert.Equal(paths, archive.Entries.Select(static entry => entry.FullName).Order(StringComparer.Ordinal));

        var single = await orchestrator.ExportAsync(request with { Functional = [request.Functional[0]] }, cancellation).ConfigureAwait(true);
        Assert.DoesNotContain(single.Files, file => file.RelativePath.EndsWith(GroupPath, StringComparison.Ordinal));
        foreach (var platform in new[] { "azure", "github" })
        {
            Assert.False(File.Exists(Path.Combine(output, platform, GroupPath)));
            var parent = await File.ReadAllTextAsync(Path.Combine(output, platform, "Functional-Requirements.md"), cancellation).ConfigureAwait(true);
            Assert.DoesNotContain(GroupPath[..^3], parent, StringComparison.Ordinal);
        }
    }

    /// <summary>Checks invalid footer declarations without mutating a previous export.</summary>
    [Theory]
    [InlineData("missing.md")]
    [InlineData("guide.txt")]
    [InlineData("../escape.md")]
    [InlineData("empty.md")]
    [InlineData("delimiter.md")]
    public async Task InvalidFooterNeverChangesOutput(string name)
    {
        using var workspace = new RequirementsWikiWorkspace();
        Configure(workspace, name);
        var docs = Path.Combine(workspace.Path, "docs");
        File.WriteAllText(Path.Combine(docs, "guide.txt"), "Wrong extension");
        File.WriteAllText(Path.Combine(docs, "empty.md"), "  ");
        File.WriteAllText(Path.Combine(docs, "delimiter.md"), "<!-- mcp-wiki-footer:begin -->");
        var output = Directory.CreateDirectory(Path.Combine(workspace.Path, "wiki-output", "github")).FullName;
        var existing = Path.Combine(output, "Home.md");
        File.WriteAllText(existing, "previous");
        var request = workspace.CreateRequest(Path.GetDirectoryName(output)!);
        var orchestrator = new RequirementsWikiExportOrchestrator(new RecordingDocFxWorkflowRunner());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ExportAsync(request, TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Contains("footer.template", error.Message, StringComparison.Ordinal);
        Assert.Equal("previous", File.ReadAllText(existing));
        Assert.Single(Directory.EnumerateFiles(output));
    }

    /// <summary>Exports an actual file store and a disposable SQLite store and re-ingests their wiki documents.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiFooterRoundTrip, ac-WikiGroupRoundTrip, and ac-WikiGroupInventory.</remarks>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanonicalStoreRoundTripDoesNotMutateRequirements(bool useDatabase)
    {
        using var workspace = new RequirementsWikiWorkspace();
        using var database = new RequirementsDatabaseWikiFixture();
        var cancellation = TestContext.Current.CancellationToken;
        Configure(workspace);
        workspace.SeedCanonicalDocs();
        // Seed the existing canonical coverage line so the round-trip measures footer and group effects.
        File.AppendAllText(workspace.CreateOptions().TechnicalRequirementsPath, "\n**Covered by:** FR-MCP-DOCFX-001\n");
        var footer = Path.Combine(workspace.Path, "docs", "footer.md");
        File.WriteAllText(footer, "## FR-MCP-FOOTER-001 Extra heading\n\n- [x] Footer wording");
        var orchestrator = new RequirementsWikiExportOrchestrator(new RecordingDocFxWorkflowRunner());
        database.SetWorkspace(workspace.Path);
        IRequirementsDocumentService service = useDatabase
            ? database.CreateService(orchestrator)
            : new RequirementsDocumentService(MsOptions.Create(workspace.CreateOptions()),
                NullLogger<RequirementsDocumentService>.Instance, wikiExportOrchestrator: orchestrator);

        var first = new FrEntry("FR-MCP-WIKI-001", "First", "Keep the first body", AcceptanceCriteria:
            [new() { Id = "ac-verified", Text = "Verify state", IsSatisfied = true, Evidence = "asserted" }]);
        await service.AddFrAsync(first, cancellation).ConfigureAwait(true);
        await service.AddFrAsync(new FrEntry("FR-MCP-WIKI-002", "Second", "Keep the second body"), cancellation).ConfigureAwait(true);
        var functionalBefore = await service.GetAllFrAsync(cancellation).ConfigureAwait(true);
        Assert.Contains(functionalBefore, entry => entry.Id == first.Id && Assert.Single(entry.AcceptanceCriteria!).IsSatisfied);
        var before = JsonSerializer.Serialize(functionalBefore);
        var mappingsBefore = JsonSerializer.Serialize(await service.GetAllMappingsAsync(cancellation).ConfigureAwait(true));
        var output = Path.Combine(workspace.Path, "wiki-output");
        var timestamp = new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);
        var export = await service.GenerateWikiAsync(output, timestamp, cancellation).ConfigureAwait(true);
        Assert.True(export.Success);

        var documents = new Dictionary<string, RequirementsIngestDocument>(StringComparer.Ordinal);
        foreach (var file in export.Files)
        {
            var path = file.RelativePath.Replace('\\', '/');
            documents.Add(path, new RequirementsIngestDocument
            {
                Content = await File.ReadAllTextAsync(Path.Combine(output, path), cancellation).ConfigureAwait(true),
                LastModifiedUtc = path == "github/" + GroupPath ? timestamp.AddDays(1) : timestamp,
            });
        }
        Assert.Contains("Verify state", documents["azure/" + GroupPath].Content!, StringComparison.Ordinal);
        var controller = new RequirementsController(service, MsOptions.Create(workspace.CreateOptions()),
            new WorkspaceContext { WorkspacePath = workspace.Path }, Substitute.For<ITodoExecutionService>(),
            NullLogger<RequirementsController>.Instance, transactionCoordinator: null, transactionOptions: null);
        var response = await controller.IngestAsync(new RequirementsIngestRequest
        {
            SourceFormat = "wiki",
            PreferredWikiFormat = "azure",
            Documents = documents,
        }, cancellation).ConfigureAwait(true);
        var imported = Assert.IsType<RequirementsIngestResult>(Assert.IsType<OkObjectResult>(response.Result).Value);
        Assert.Equal("azure", imported.SelectedWikiFormat);
        Assert.Equal(3, functionalBefore.Count);
        Assert.Equal(functionalBefore.Count, imported.FunctionalParsed);
        Assert.Equal(0, imported.FunctionalAdded);
        Assert.Equal(0, imported.FunctionalUpdated);
        Assert.Equal(0, imported.FunctionalDeleted);
        Assert.Equal(before, JsonSerializer.Serialize(await service.GetAllFrAsync(cancellation).ConfigureAwait(true)));
        Assert.Equal(mappingsBefore, JsonSerializer.Serialize(await service.GetAllMappingsAsync(cancellation).ConfigureAwait(true)));
        Assert.Equal(0, imported.MappingAdded + imported.MappingUpdated + imported.MappingDeleted);
        Assert.Equal(0, imported.TechnicalAdded + imported.TechnicalUpdated + imported.TechnicalDeleted);
        Assert.Equal(0, imported.TestingAdded + imported.TestingUpdated + imported.TestingDeleted);
    }

    /// <summary>Checks that a contained footer declaration cannot traverse an external symbolic link.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiFooterValidation.</remarks>
    [Fact]
    public async Task EscapingFooterSymlinkNeverChangesOutput()
    {
        using var workspace = new RequirementsWikiWorkspace();
        using var sibling = new RequirementsWikiWorkspace();
        Configure(workspace, "external.md");
        var external = Path.Combine(sibling.Path, "docs", "footer.md");
        File.WriteAllText(external, FooterText);
        File.CreateSymbolicLink(Path.Combine(workspace.Path, "docs", "external.md"), external);
        var output = Path.Combine(workspace.Path, "wiki-output");
        var orchestrator = new RequirementsWikiExportOrchestrator(new RecordingDocFxWorkflowRunner());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ExportAsync(workspace.CreateRequest(output), TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Contains("footer.template", error.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(output));
    }

    /// <summary>Rejects a group route that conflicts with an existing page before changing disk output.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiGroupInventory.</remarks>
    [Theory]
    [InlineData(GroupPath)]
    [InlineData("Other/" + GroupPath)]
    public async Task GroupRouteCollisionNeverChangesOutput(string target)
    {
        using var workspace = new RequirementsWikiWorkspace();
        Configure(workspace, guideTarget: target);
        var output = Path.Combine(workspace.Path, "wiki-output");
        var oldHome = Path.Combine(output, "github", "Home.md");
        Directory.CreateDirectory(Path.GetDirectoryName(oldHome)!);
        File.WriteAllText(oldHome, "previous");
        var request = workspace.CreateRequest(output) with
        {
            Functional = [new("FR-MCP-WIKI-001", "One", "First body"), new("FR-MCP-WIKI-002", "Two", "Second body")],
        };
        var orchestrator = new RequirementsWikiExportOrchestrator(new RecordingDocFxWorkflowRunner());
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ExportAsync(request, TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Contains(GroupPath[..^3], error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("previous", File.ReadAllText(oldHome));
        Assert.Single(Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories));
    }

    private static void Configure(RequirementsWikiWorkspace workspace, string footer = "footer.md",
        string guideTarget = "Guide.md", bool docFx = false)
    {
        var docs = Path.Combine(workspace.Path, "docs");
        File.WriteAllText(Path.Combine(docs, "footer.md"), FooterText);
        File.WriteAllText(Path.Combine(docs, "guide.md"), "# Original guide");
        var extension = "";
        if (docFx)
        {
            Directory.CreateDirectory(Path.Combine(docs, "docfx"));
            extension = """
                docfx:
                  workflows:
                  - id: api
                    executable: dotnet
                    arguments: [docfx, docfx.json]
                    workingDirectory: docs/docfx
                    outputRoot: docs/docfx/_site
                    targetRoot: Api
                    platforms: [azure, github]
                """;
        }
        File.WriteAllText(Path.Combine(docs, "wiki.yaml"), $$"""
            schema: mcp-wiki-export/v1
            {{extension}}
            footer:
              template: docs/{{footer}}
            requirementGroups:
              enabled: true
            documents:
            - id: home
              title: Home
              source: generated:home
              target: Home.md
            - id: functional
              title: Functional Requirements
              source: generated:functional
              target: Functional-Requirements.md
            - id: guide
              title: Guide
              source: docs/guide.md
              target: {{guideTarget}}
            - id: technical
              title: Technical Requirements
              source: generated:technical
              target: Technical-Requirements.md
            - id: testing
              title: Testing Requirements
              source: generated:testing
              target: Testing-Requirements.md
            - id: mapping
              title: TR per FR Mapping
              source: generated:mapping
              target: TR-per-FR-Mapping.md
            - id: matrix
              title: Requirements Matrix
              source: generated:matrix
              target: Requirements-Matrix.md
            navigation:
            - document: home
            - document: functional
            - document: guide
            - document: technical
            - document: testing
            - document: mapping
            - document: matrix
            """);
    }
}
