using System.Reflection;
using System.Text.Json;
using McpServer.Cqrs.Mvvm;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-HANDOFF-006: native handoff MCP tools and public-surface inventory.
/// </summary>
public sealed class HandoffMcpToolTests : IDisposable
{
    private readonly McpDbContext _db = new(
        new DbContextOptionsBuilder<McpDbContext>().UseInMemoryDatabase("handoff-tools-" + Guid.NewGuid().ToString("N")).Options,
        new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" });

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>TEST-HANDOFF-006: handoff_ingest maps sourceKind/mode and calls IHandoffIngestionService.</summary>
    [Fact]
    public async Task HandoffIngest_DelegatesToSharedService()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        service.IngestAsync(Arg.Any<HandoffIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true });
        var tools = CreateTools(_db, service);

        var json = await tools.HandoffIngest(
            @"F:\GitHub\McpServer",
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
            @"F:\GitHub\McpServer",
            "Content",
            content: "handoff",
            mode: "999",
            cancellationToken: TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<HandoffIngestionResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.False(result!.Success);
        Assert.Equal(HandoffErrorCodes.InvalidMode, result.ErrorCode);
        await service.DidNotReceiveWithAnyArgs().IngestAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>TEST-HANDOFF-006: native tools, Director aliases, and plugin skill exist.</summary>
    [Fact]
    public void PublicSurfaces_ExposeIngestGetAndApprove()
    {
        var toolNames = typeof(FwhMcpTools)
            .GetMethods()
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("handoff_ingest", toolNames);
        Assert.Contains("handoff_get", toolNames);
        Assert.Contains("handoff_approve", toolNames);

        var aliases = typeof(HandoffIngestDirectorCommand).Assembly
            .GetTypes()
            .Select(type => type.GetCustomAttribute<ViewModelCommandAttribute>()?.Alias)
            .Where(alias => alias is not null)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("handoff-ingest", aliases);
        Assert.Contains("handoff-get", aliases);
        Assert.Contains("handoff-approve", aliases);

        var executor = Substitute.For<IHandoffDirectorExecutor>();
        Assert.NotNull(new HandoffIngestDirectorCommand(executor).PrimaryCommand);
        Assert.NotNull(new HandoffGetDirectorCommand(executor).PrimaryCommand);
        Assert.NotNull(new HandoffApproveDirectorCommand(executor).PrimaryCommand);

        var skill = Path.Combine(FindRepoRoot(), "plugins", "core", "skills", "handoff", "SKILL.md");
        Assert.True(File.Exists(skill), skill);
        var skillText = File.ReadAllText(skill);
        Assert.Contains("workflow.handoff.ingest", skillText, StringComparison.Ordinal);
    }

    /// <summary>P2-6: plugin-sync artifact matches the canonical core handoff skill bytes.</summary>
    [Fact]
    public void PluginSync_HandoffSkill_MatchesCoreArtifact()
    {
        var root = FindRepoRoot();
        var core = Path.Combine(root, "plugins", "core", "skills", "handoff", "SKILL.md");
        var grok = Path.Combine(root, "..", "mcpserver-grok-plugin", "skills", "handoff", "SKILL.md");
        Assert.True(File.Exists(core), core);
        Assert.True(File.Exists(grok), grok);
        Assert.Equal(
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(core))),
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(grok))));
    }

    /// <summary>TEST-HANDOFF-006: invalid mode is rejected instead of coerced to DraftOnly.</summary>
    [Fact]
    public async Task HandoffIngest_InvalidMode_ReturnsError()
    {
        var service = Substitute.For<IHandoffIngestionService>();
        var tools = CreateTools(_db, service);

        var json = await tools.HandoffIngest(
            @"F:\GitHub\McpServer",
            "Content",
            content: "handoff",
            mode: "NotAMode",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("invalid_mode", json, StringComparison.Ordinal);
        await service.DidNotReceiveWithAnyArgs().IngestAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static FwhMcpTools CreateTools(McpDbContext db, IHandoffIngestionService handoffService)
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
            agentHelpService: Substitute.For<IAgentHelpConversationService>(),
            handoffIngestionService: handoffService);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing McpServer.sln.");
    }
}
