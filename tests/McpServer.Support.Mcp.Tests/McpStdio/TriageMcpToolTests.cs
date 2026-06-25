using System.Text.Json;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-PLUGIN-TRIAGE-001: verifies the plugin-facing STDIO MCP triage tools
/// delegate to the triage service and preserve the shared JSON contracts.
/// </summary>
public sealed class TriageMcpToolTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly McpDbContext _db;
    private readonly ITriageService _triageService = Substitute.For<ITriageService>();
    private readonly FwhMcpTools _tools;

    /// <summary>Initializes the MCP tool fixture with substituted triage service dependencies.</summary>
    public TriageMcpToolTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"TriageMcpToolTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
        _tools = CreateTools(_db, _triageService);
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>
    /// TEST-MCP-PLUGIN-TRIAGE-001: triage_report forwards the report contract and returns
    /// accepted queue state immediately.
    /// </summary>
    [Fact]
    public async Task TriageReport_DelegatesToTriageService()
    {
        _triageService.SubmitReportAsync(Arg.Any<TriageReportRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageReportSubmitResult
            {
                Success = true,
                ReportId = "triage-report-001",
                GroupId = "triage-group-001",
                Status = "collecting",
                QuietDeadlineUtc = DateTimeOffset.Parse("2026-06-25T05:15:00Z"),
                WorkspacePath = @"F:\GitHub\McpServer",
            });

        var json = await _tools.TriageReport(
            @"F:\GitHub\McpServer",
            "mcpserver-codex-plugin masks method_not_found",
            "The plugin wrapper hides triage errors.",
            component: "mcpserver-codex-plugin",
            severity: "high",
            dedupeKey: "plugin-triage-wrapper",
            errorSignature: "method_not_found",
            affectedPaths: @"F:\GitHub\mcpserver-codex-plugin\lib\repl-invoke.sh,F:\GitHub\mcpserver-codex-plugin\skills\triage\SKILL.md",
            reporterAgent: "Codex").ConfigureAwait(true);

        var result = JsonSerializer.Deserialize<TriageReportSubmitResult>(json, JsonOptions);
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("triage-report-001", result.ReportId);
        await _triageService.Received(1).SubmitReportAsync(
            Arg.Is<TriageReportRequest>(request => request != null
                && request.WorkspacePath == @"F:\GitHub\McpServer"
                && request.Title == "mcpserver-codex-plugin masks method_not_found"
                && request.Component == "mcpserver-codex-plugin"
                && request.AffectedPaths != null
                && request.AffectedPaths.Count == 2
                && request.ReporterAgent == "Codex"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-PLUGIN-TRIAGE-001: triage_status can inspect a single report.</summary>
    [Fact]
    public async Task TriageStatus_WithReportId_ReturnsReport()
    {
        _triageService.GetReportAsync("triage-report-001", Arg.Any<CancellationToken>())
            .Returns(new TriageReportDetail
            {
                ReportId = "triage-report-001",
                GroupId = "triage-group-001",
                Status = "grouped",
                Title = "bug",
                Summary = "details",
            });

        var json = await _tools.TriageStatus(@"F:\GitHub\McpServer", reportId: "triage-report-001").ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<TriageReportDetail>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal("triage-report-001", result!.ReportId);
        await _triageService.Received(1).GetReportAsync("triage-report-001", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-PLUGIN-TRIAGE-001: triage_status without ids queries triage groups.</summary>
    [Fact]
    public async Task TriageStatus_WithoutIds_ReturnsGroupQuery()
    {
        _triageService.QueryGroupsAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new TriageGroupQueryResult
            {
                TotalCount = 1,
                Items =
                [
                    new TriageGroupDetail
                    {
                        GroupId = "triage-group-001",
                        Status = "collecting",
                        ReportCount = 1,
                    },
                ],
            });

        var json = await _tools.TriageStatus(@"F:\GitHub\McpServer").ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<TriageGroupQueryResult>(json, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalCount);
        Assert.Equal("triage-group-001", result.Items[0].GroupId);
        await _triageService.Received(1).QueryGroupsAsync(null, null, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static FwhMcpTools CreateTools(McpDbContext db, ITriageService triageService)
    {
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var workspaceContext = new WorkspaceContext { WorkspacePath = "." };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var chunker = new Chunker();
        var repoIngestor = new RepoIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<RepoIngestor>.Instance);
        var sessionLogIngestor = new SessionLogIngestor(chunker, ingestionOptions, workspaceContext, Substitute.For<ISessionLogService>(), NullLogger<SessionLogIngestor>.Instance);
        var externalDocsIngestor = new ExternalDocsIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<ExternalDocsIngestor>.Instance);
        var gitHubIngestor = new GitHubIngestor(chunker, gitHubCliService, NullLogger<GitHubIngestor>.Instance);
        var issueIngestor = new IssueIngestor(chunker, gitHubCliService, NullLogger<IssueIngestor>.Instance);
        var coordinator = new IngestionCoordinator(
            db,
            repoIngestor,
            sessionLogIngestor,
            externalDocsIngestor,
            gitHubIngestor,
            issueIngestor,
            Substitute.For<IWebsiteIngestor>(),
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            null,
            workspaceContext,
            NullLogger<IngestionCoordinator>.Instance);
        var todoService = Substitute.For<ITodoService>();
        var todoServiceResolver = new TodoServiceResolver(todoService, ingestionOptions, Substitute.For<ITodoServiceFactory>());
        var workspaceAccessor = new WorkspaceServiceAccessor(todoServiceResolver, httpContextAccessor, ingestionOptions);
        var desktopLaunchService = new DesktopLaunchService(
            Substitute.For<IConfiguration>(),
            MsOptions.Options.Create(new DesktopLaunchOptions()),
            Substitute.For<IProcessRunner>(),
            NullLogger<DesktopLaunchService>.Instance);
        var todoCreationService = new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance);
        var todoUpdateService = new TodoUpdateService(workspaceAccessor, null, NullLogger<TodoUpdateService>.Instance);

        return new FwhMcpTools(
            db,
            Substitute.For<IRepoFileService>(),
            coordinator,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IContextSearchService>(),
            Substitute.For<IGraphRagService>(),
            workspaceAccessor,
            Substitute.For<ITodoPromptService>(),
            Substitute.For<ISessionLogService>(),
            Substitute.For<IMemoryService>(),
            gitHubCliService,
            Substitute.For<IRequirementsDocumentService>(),
            desktopLaunchService,
            httpContextAccessor,
            workspaceContext,
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IWorkspacePolicyService>(),
            todoServiceResolver,
            todoCreationService,
            todoUpdateService,
            Substitute.For<ITodoExecutionService>(),
            Substitute.For<IPromptTemplateService>(),
            NullLogger<FwhMcpTools>.Instance,
            triageService: triageService);
    }
}
