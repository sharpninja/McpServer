using System.Reflection;
using System.Text.Json;
using QBrainAi.Cqrs.Mvvm;
using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Services.AgentHelp;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using NSubstitute;
using QBrainAi.Tests.Shared;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-HANDOFF-006: native handoff MCP tools and public-surface inventory.
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (PublicSurfaces_ExposeIngestGetAndApprove and PluginSync_HandoffSkill_MatchesCoreArtifact) and the helpers only they use (FindRepoRoot and FindPrimaryRepoRoot) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_db and Dispose) moved once into the abstract base class HandoffMcpToolTestsBase in McpStdio/HandoffMcpToolTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class HandoffMcpToolTests : HandoffMcpToolTestsBase, IDisposable
{
    /// <summary>TEST-HANDOFF-006: handoff_ingest maps sourceKind/mode and calls IHandoffIngestionService.</summary>
    [Fact]
    public async Task HandoffIngest_DelegatesToSharedService()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        service.IngestAsync(Arg.Any<HandoffIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true });
        var tools = CreateTools(_db, service);

        var json = await tools.HandoffIngest(
            @"Q:\__mcp_unit_test__\QBrainAi",
            "Content",
            content: "handoff",
            mode: "DraftOnly",
            cancellationToken: TestContext.Current.CancellationToken);

        var result = JsonSerializer.Deserialize<HandoffIngestionResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.True(result.Success);
        await service.Received(1).IngestAsync(
            Arg.Is<HandoffIngestionRequest>(request => request != null && request.SourceKind == HandoffSourceKind.Content && request.Mode == HandoffIngestionMode.DraftOnly),
            Arg.Any<CancellationToken>());
    }

    /// <summary>P1-7: native MCP rejects numeric mode 999 and never calls the service.</summary>
    [Fact]
    public async Task HandoffIngest_NumericMode999_ReturnsInvalidMode()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        var tools = CreateTools(_db, service);
        var json = await tools.HandoffIngest(
            @"Q:\__mcp_unit_test__\QBrainAi",
            "Content",
            content: "handoff",
            mode: "999",
            cancellationToken: TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<HandoffIngestionResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.False(result!.Success);
        Assert.Equal(HandoffErrorCodes.InvalidMode, result.ErrorCode);
        await service.DidNotReceiveWithAnyArgs().IngestAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-006: invalid mode is rejected instead of coerced to DraftOnly.</summary>
    [Fact]
    public async Task HandoffIngest_InvalidMode_ReturnsError()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        var tools = CreateTools(_db, service);

        var json = await tools.HandoffIngest(
            @"Q:\__mcp_unit_test__\QBrainAi",
            "Content",
            content: "handoff",
            mode: "NotAMode",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("invalid_mode", json, StringComparison.Ordinal);
        await service.DidNotReceiveWithAnyArgs().IngestAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static FwhMcpTools CreateTools(McpDbContext db, IHandoffIngestionService handoffService)
    {
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = TestWorkspacePaths.UnusedRepoRoot });
        var workspaceContext = new WorkspaceContext { WorkspacePath = TestWorkspacePaths.UnusedRepoRoot };
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
            agentHelpService: Substitute.For<IAgentHelpConversationService>(),
            handoffIngestionService: handoffService);
    }
}
