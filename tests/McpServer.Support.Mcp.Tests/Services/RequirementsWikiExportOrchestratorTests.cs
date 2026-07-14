using System.Diagnostics;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-DOCFXWIKI-001: Shared requirements wiki export orchestrator delegation coverage.</summary>
public sealed class RequirementsWikiExportOrchestratorTests
{
    /// <summary>File-backed wiki generation delegates rendering and writing to the shared orchestrator.</summary>
    [Fact]
    public async Task FileBackedGenerateWikiAsync_DelegatesToSharedOrchestrator()
    {
        using var workspace = new RequirementsWikiWorkspace();
        workspace.SeedCanonicalDocs();
        var orchestrator = new RecordingWikiExportOrchestrator();
        var service = new RequirementsDocumentService(
            Microsoft.Extensions.Options.Options.Create(workspace.CreateOptions()),
            NullLogger<RequirementsDocumentService>.Instance,
            wikiExportOrchestrator: orchestrator);
        var outputRoot = Path.Combine(workspace.Path, "wiki-output");
        var generatedAt = new DateTimeOffset(2026, 7, 13, 22, 0, 0, TimeSpan.Zero);

        var result = await service.GenerateWikiAsync(outputRoot, generatedAt, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var request = Assert.Single(orchestrator.Requests);
        Assert.Equal(Path.GetFullPath(outputRoot), Path.GetFullPath(request.OutputRootPath));
        Assert.Equal(generatedAt, request.GeneratedAtUtc);
        Assert.Equal(workspace.Path, request.WorkspacePath);
        Assert.Contains(request.Functional, item => item.Id == "FR-MCP-DOCFX-001");
        Assert.Contains(request.Technical, item => item.Id == "TR-MCP-DOCFX-001");
        Assert.Contains(request.Testing, item => item.Id == "TEST-MCP-DOCFX-001");
        Assert.Contains(request.Mappings, item => item.FrId == "FR-MCP-DOCFX-001");
        Assert.Contains("Existing matrix row", request.ExistingMatrixMarkdown, StringComparison.Ordinal);
        Assert.Same(orchestrator.Result, result);
    }

    /// <summary>DB-backed wiki generation delegates scoped requirements and workspace context to the shared orchestrator.</summary>
    [Fact]
    public async Task DatabaseGenerateWikiAsync_DelegatesToSharedOrchestrator()
    {
        using var fixture = new RequirementsDatabaseWikiFixture();
        var workspaceA = fixture.CreateWorkspace("a");
        var workspaceB = fixture.CreateWorkspace("b");
        var orchestrator = new RecordingWikiExportOrchestrator();
        var service = fixture.CreateService(orchestrator);

        fixture.SetWorkspace(workspaceA);
        await service.AddFrAsync(new FrEntry("FR-MCP-DOCFX-001", "A FR", "A body"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await service.AddTrAsync(new TrEntry("TR-MCP-DOCFX-001", "A TR", "A body"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await service.AddTestAsync(new TestEntry("TEST-MCP-DOCFX-001", "A test"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-DOCFX-001", ["TR-MCP-DOCFX-001"], ["TEST-MCP-DOCFX-001"]), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        fixture.SetWorkspace(workspaceB);
        await service.AddFrAsync(new FrEntry("FR-MCP-DOCFX-001", "B FR", "B body"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        fixture.SetWorkspace(workspaceA);
        var outputRoot = Path.Combine(workspaceA, "wiki-output");
        var generatedAt = new DateTimeOffset(2026, 7, 13, 22, 30, 0, TimeSpan.Zero);

        var result = await service.GenerateWikiAsync(outputRoot, generatedAt, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var request = Assert.Single(orchestrator.Requests);
        Assert.Equal(workspaceA, request.WorkspacePath);
        Assert.Contains(request.Functional, item => item.Title == "A FR");
        Assert.DoesNotContain(request.Functional, item => item.Title == "B FR");
        Assert.Contains(request.Technical, item => item.Id == "TR-MCP-DOCFX-001");
        Assert.Contains(request.Testing, item => item.Id == "TEST-MCP-DOCFX-001");
        Assert.Contains(request.Mappings, item => item.FrId == "FR-MCP-DOCFX-001");
        Assert.Same(orchestrator.Result, result);
    }

    /// <summary>Omitted DocFX configuration does not run the external DocFX workflow runner.</summary>
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

    /// <summary>DocFX output cannot publish over a configured requirements wiki document target.</summary>
    [Fact]
    public async Task ExportAsync_WhenDocFxArtifactCollidesWithConfiguredDocument_ThrowsBeforeWriting()
    {
        using var workspace = new RequirementsWikiWorkspace();
        workspace.SeedCanonicalDocs();
        workspace.WriteWikiConfigWithDocFx(platforms: ["github"]);
        var outputRoot = Path.Combine(workspace.Path, "wiki-output");
        Directory.CreateDirectory(Path.Combine(outputRoot, "github"));
        var existingHome = Path.Combine(outputRoot, "github", "Home.md");
        await File.WriteAllTextAsync(existingHome, "prior home", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var docFxRunner = new RecordingDocFxWorkflowRunner
        {
            Documents =
            [
                new RequirementsRenderedDocument("github/Home.md", "docfx home", "text/markdown")
            ]
        };
        var orchestrator = new RequirementsWikiExportOrchestrator(docFxRunner);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ExportAsync(workspace.CreateRequest(outputRoot), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("prior home", await File.ReadAllTextAsync(existingHome, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>Stale DocFX artifacts from a previous wiki export are removed by the shared writer.</summary>
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

    /// <summary>A failing DocFX workflow leaves existing wiki output unchanged because the writer is not invoked.</summary>
    [Fact]
    public async Task ExportAsync_WhenDocFxRunnerFails_LeavesPriorOutputUnchanged()
    {
        using var workspace = new RequirementsWikiWorkspace();
        workspace.SeedCanonicalDocs();
        workspace.WriteWikiConfigWithDocFx(platforms: ["github"]);
        var outputRoot = Path.Combine(workspace.Path, "wiki-output");
        Directory.CreateDirectory(Path.Combine(outputRoot, "github"));
        var existingHome = Path.Combine(outputRoot, "github", "Home.md");
        await File.WriteAllTextAsync(existingHome, "prior home", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var docFxRunner = new RecordingDocFxWorkflowRunner { Failure = new InvalidOperationException("docfx failed") };
        var orchestrator = new RequirementsWikiExportOrchestrator(docFxRunner);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ExportAsync(workspace.CreateRequest(outputRoot), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("docfx failed", ex.Message, StringComparison.Ordinal);
        Assert.Equal("prior home", await File.ReadAllTextAsync(existingHome, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.False(File.Exists(Path.Combine(outputRoot, "github", RequirementsWikiDocumentRenderer.ManifestFileName)));
    }

    /// <summary>Real DocFX 2.78.3 local-tool output is published through both wiki roots and staging is deleted.</summary>
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

    private sealed class RecordingWikiExportOrchestrator : IRequirementsWikiExportOrchestrator
    {
        public RequirementsDocumentExportResult Result { get; } = new()
        {
            Success = true,
            Format = "wiki",
            DocType = "all",
            OutputRoot = "recorded"
        };

        public List<RequirementsWikiExportRequest> Requests { get; } = [];

        public Task<RequirementsDocumentExportResult> ExportAsync(RequirementsWikiExportRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(Result);
        }
    }

    private sealed class RecordingDocFxWorkflowRunner : IRequirementsDocFxWorkflowRunner
    {
        public int CallCount { get; private set; }

        public IReadOnlyList<RequirementsRenderedDocument> Documents { get; init; } = [];

        public Exception? Failure { get; init; }

        public Task<IReadOnlyList<RequirementsRenderedDocument>> RunAsync(RequirementsWikiExportConfig config, CancellationToken ct = default)
        {
            CallCount++;
            if (Failure is not null)
                throw Failure;

            return Task.FromResult(Documents);
        }
    }

    private sealed class RequirementsWikiWorkspace : IDisposable
    {
        public RequirementsWikiWorkspace()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcp-wiki-orchestrator-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs", "Project"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs"));
        }

        public string Path { get; }

        public RequirementsOptions CreateOptions() => new()
        {
            FunctionalRequirementsPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.FunctionalFileName),
            TechnicalRequirementsPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TechnicalFileName),
            TestingRequirementsPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TestingFileName),
            MappingPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MappingFileName),
            MatrixPath = System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName),
            WikiConfigPath = System.IO.Path.Combine(Path, "docs", "wiki.yaml")
        };

        public void SeedCanonicalDocs()
        {
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.FunctionalFileName), """
                # Functional Requirements (MCP Server)

                ## FR-MCP-DOCFX-001 DocFX wiki export

                Export DocFX output into wiki artifacts.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TechnicalFileName), """
                # Technical Requirements (MCP Server)

                ## TR-MCP-DOCFX-001

                Shared orchestrator merges generated and DocFX wiki documents.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.TestingFileName), """
                # Testing Requirements (MCP Server)

                ### TEST-MCP-DOCFX-001

                Given DocFX wiki export, when generated, then shared orchestration is used.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MappingFileName), """
                # TR per FR Mapping (MCP Server)

                | FR | Primary TRs | Tests |
                | --- | --- | --- |
                | FR-MCP-DOCFX-001 | TR-MCP-DOCFX-001 | TEST-MCP-DOCFX-001 |
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "Project", RequirementsDocumentRenderer.MatrixFileName), """
                # Requirements Matrix (MCP Server)

                | ID | Status | Source |
                | --- | --- | --- |
                | EXISTING | Existing matrix row | Manual |
                """);
        }

        public void WriteWikiConfigWithoutDocFx()
        {
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "wiki.yaml"), """
                schema: mcp-wiki-export/v1
                documents:
                - id: home
                  title: Home
                  source: generated:home
                  target: Home.md
                navigation:
                - document: home
                """);
        }

        public RequirementsWikiExportRequest CreateRequest(string outputRoot) =>
            new(
                outputRoot,
                new DateTimeOffset(2026, 7, 13, 23, 0, 0, TimeSpan.Zero),
                Path,
                CreateOptions(),
                [new FrEntry("FR-MCP-DOCFX-001", "DocFX wiki export", "Export DocFX output into wiki artifacts.")],
                [new TrEntry("TR-MCP-DOCFX-001", "Shared orchestrator", "Shared orchestrator merges generated and DocFX wiki documents.")],
                [new TestEntry("TEST-MCP-DOCFX-001", "Given DocFX wiki export, when generated, then shared orchestration is used.")],
                [new FrTrMapping("FR-MCP-DOCFX-001", ["TR-MCP-DOCFX-001"], ["TEST-MCP-DOCFX-001"])],
                null);

        public void SeedDocFxProject()
        {
            Directory.CreateDirectory(System.IO.Path.Combine(Path, ".config"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs", "docfx", "resources"));
            File.WriteAllText(System.IO.Path.Combine(Path, ".config", "dotnet-tools.json"), """
                {
                  "version": 1,
                  "isRoot": true,
                  "tools": {
                    "docfx": {
                      "version": "2.78.3",
                      "commands": [
                        "docfx"
                      ]
                    }
                  }
                }
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "docfx.json"), """
                {
                  "build": {
                    "content": [
                      {
                        "files": [
                          "index.md",
                          "guide.md"
                        ]
                      }
                    ],
                    "resource": [
                      {
                        "files": [
                          "resources/raw-note.md"
                        ]
                      }
                    ],
                    "dest": "_site",
                    "globalMetadata": {
                      "_appTitle": "MCP DocFX Scratch"
                    }
                  }
                }
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "index.md"), """
                # Scratch DocFX Home

                This page was built by DocFX.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "guide.md"), """
                # Scratch DocFX Guide

                This guide page was built by DocFX.
                """);
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "docfx", "resources", "raw-note.md"), """
                # Raw copied markdown

                Copied as a DocFX resource.
                """);
        }

        public void WriteWikiConfigWithDocFx(IReadOnlyList<string>? platforms = null, IReadOnlyList<string>? arguments = null)
        {
            platforms ??= ["github", "azure"];
            arguments ??= ["docfx", "docfx.json"];
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs", "docfx"));
            var platformLines = string.Join(Environment.NewLine, platforms.Select(platform => $"    - {platform}"));
            var argumentLines = string.Join(Environment.NewLine, arguments.Select(argument => $"    - {argument}"));
            File.WriteAllText(System.IO.Path.Combine(Path, "docs", "wiki.yaml"), $$"""
                schema: mcp-wiki-export/v1
                docfx:
                  workflows:
                  - id: api
                    executable: dotnet
                    arguments:
                {{argumentLines}}
                    workingDirectory: docs/docfx
                    outputRoot: docs/docfx/_site
                    targetRoot: api
                    platforms:
                {{platformLines}}
                    timeoutSeconds: 120
                documents:
                - id: home
                  title: Home
                  source: generated:home
                  target: Home.md
                  platforms:
                  - github
                  - azure
                navigation:
                - document: home
                """);
        }

        public void Dispose()
        {
            ClearReadOnly(Path);
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }

        private static void ClearReadOnly(string root)
        {
            if (!Directory.Exists(root))
                return;

            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(file);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
            }
        }
    }

    private sealed class RequirementsDatabaseWikiFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _requestScope;
        private readonly DefaultHttpContext _httpContext;
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcp-wiki-db-orchestrator-tests-" + Guid.NewGuid().ToString("N"));

        public RequirementsDatabaseWikiFixture()
        {
            var services = new ServiceCollection();
            _connection.Open();
            services.AddDbContext<McpDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<WorkspaceContext>();
            services.AddHttpContextAccessor();
            services.AddSingleton<IOptions<RequirementsOptions>>(Microsoft.Extensions.Options.Options.Create(new RequirementsOptions()));
            services.AddSingleton(NullLogger<RequirementsDatabaseDocumentService>.Instance);
            _provider = services.BuildServiceProvider();
            using (var schemaScope = _provider.CreateScope())
                schemaScope.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();

            _requestScope = _provider.CreateScope();
            _httpContext = new DefaultHttpContext { RequestServices = _requestScope.ServiceProvider };
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
        }

        public string CreateWorkspace(string name)
        {
            var path = System.IO.Path.Combine(_root, name);
            Directory.CreateDirectory(System.IO.Path.Combine(path, "docs", "Project"));
            return path;
        }

        public void SetWorkspace(string path)
        {
            var context = _requestScope.ServiceProvider.GetRequiredService<WorkspaceContext>();
            context.WorkspaceName = System.IO.Path.GetFileName(path);
            context.WorkspacePath = path;
        }

        public RequirementsDatabaseDocumentService CreateService(IRequirementsWikiExportOrchestrator orchestrator) =>
            new(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                _provider.GetRequiredService<IOptions<RequirementsOptions>>(),
                NullLogger<RequirementsDatabaseDocumentService>.Instance,
                _provider.GetRequiredService<IHttpContextAccessor>(),
                wikiExportOrchestrator: orchestrator);

        public void Dispose()
        {
            _requestScope.Dispose();
            _provider.Dispose();
            _connection.Dispose();
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
    }
}
