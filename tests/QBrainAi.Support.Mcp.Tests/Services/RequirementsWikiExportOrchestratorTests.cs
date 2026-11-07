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

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-DOCFXWIKI-001: Shared requirements wiki export orchestrator delegation coverage.</summary>
/// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 4 Category=Integration cases (ExportAsync_WhenDocFxSectionIsOmitted_DoesNotRunDocFxRunner, ExportAsync_WithDocFxArtifacts_IncludesArtifactsInSelectedPlatformManifestOnly, ExportAsync_RemovesStaleDocFxArtifactsThroughWriterCleanup and ExportAsync_WithRealDocFxScratchWorkspace_PublishesArtifactsAndDeletesStaging) and the helpers only they use (AssertPublishedDocFxArtifacts and PassthroughProcessEnvironmentService) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (RecordingDocFxWorkflowRunner and RequirementsWikiWorkspace) moved once into Services/RequirementsWikiExportOrchestratorTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
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
        Assert.Equal(ExpectedWorkspaceKey.Of(workspaceA), request.WorkspacePath);
        Assert.Contains(request.Functional, item => item.Title == "A FR");
        Assert.DoesNotContain(request.Functional, item => item.Title == "B FR");
        Assert.Contains(request.Technical, item => item.Id == "TR-MCP-DOCFX-001");
        Assert.Contains(request.Testing, item => item.Id == "TEST-MCP-DOCFX-001");
        Assert.Contains(request.Mappings, item => item.FrId == "FR-MCP-DOCFX-001");
        Assert.Same(orchestrator.Result, result);
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
