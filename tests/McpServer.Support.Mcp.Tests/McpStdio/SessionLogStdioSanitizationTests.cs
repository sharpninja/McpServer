using System.Text.Json;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
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
/// TEST-MCP-SESSIONLOGSAN-001 / FR-MCP-SESSIONLOGSAN-001 S17: stdio sessionlog_query JSON
/// must not leak raw secrets when ISessionLogService is the sanitizing decorator.
/// </summary>
public sealed class SessionLogStdioSanitizationTests : IDisposable
{
    private const string Secret = "sk-test-sessionlog-secret-001";

    private readonly McpDbContext _db;

    /// <summary>In-memory DB for FwhMcpTools workspace wiring.</summary>
    public SessionLogStdioSanitizationTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogStdioSanitizationTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    /// <summary>
    /// S17: sessionlog_query JSON-RPC payload omits the raw provider token after sanitizer projection.
    /// </summary>
    [Fact]
    public async Task SessionLogQuery_StdioJson_OmitsRawSecret()
    {
        var inner = Substitute.For<ISessionLogService>();
        inner.QueryAsync(Arg.Any<SessionLogQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SessionLogQueryResult
            {
                TotalCount = 1,
                Limit = 20,
                Offset = 0,
                Items =
                [
                    new UnifiedSessionLogDto
                    {
                        SourceType = "SanitizerStdio",
                        SessionId = "SanitizerStdio-20260821T000000Z-s17",
                        Title = Secret,
                        Model = Secret,
                    },
                ],
            });
        var sanitizer = new SessionLogSanitizer(MsOptions.Options.Create(new SessionLogSanitizationOptions()));
        var tools = CreateTools(_db, new SessionLogSanitizingService(inner, sanitizer));

        var json = await tools.SessionLogQuery(
            Path.GetTempPath(),
            agent: "SanitizerStdio",
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.DoesNotContain(Secret, json, StringComparison.Ordinal);
        Assert.Contains("REDACTED", json, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("totalCount").GetInt32());
    }

    private static FwhMcpTools CreateTools(McpDbContext db, ISessionLogService sessionLogService)
    {
        var workspaceContext = new WorkspaceContext { WorkspacePath = Path.GetTempPath() };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext
        {
            RequestServices = Substitute.For<IServiceProvider>(),
        });
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions());
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
            sessionLogService,
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
            transcriptIngestionService: Substitute.For<McpServer.SessionLog.Transcripts.ITranscriptIngestionService>());
    }
}
