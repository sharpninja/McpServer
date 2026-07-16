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
/// TR-MCP-SESSIONLOG-001 / TEST-MCP-SESSIONLOG-001 (BUG-TRIAGE-070/075): the session-log lifecycle
/// MCP tools SHALL return a structured {error} for every failure - including a malformed turnJson -
/// instead of throwing an uncaught exception that the MCP SDK surfaces as the opaque
/// "An error occurred invoking sessionlog_complete_turn". Uses an in-memory McpDbContext and a
/// substituted ISessionLogService.
/// </summary>
public sealed class SessionLogLifecycleToolErrorTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly ISessionLogService _sessionLogService = Substitute.For<ISessionLogService>();
    private readonly FwhMcpTools _tools;

    /// <summary>Initializes the fixture with an in-memory DB and a substituted session-log service.</summary>
    public SessionLogLifecycleToolErrorTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"SessionLogLifecycleToolErrorTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
        _tools = CreateTools(_db, _sessionLogService);
    }

    /// <summary>AC1: a malformed turnJson yields a structured {error} result rather than a thrown exception.</summary>
    [Fact]
    public async Task SessionLogCompleteTurn_MalformedTurnJson_ReturnsStructuredError()
    {
        var json = await _tools.SessionLogCompleteTurn(
            "ClaudeCode",
            "ClaudeCode-20260716T000000Z-plugin-session",
            "req-20260716T000000Z-prompt-0001",
            Path.GetTempPath(),
            turnJson: "{ this is not valid json ]",
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("error", out var error), json);
        Assert.False(string.IsNullOrWhiteSpace(error.GetString()));
        Assert.False(document.RootElement.TryGetProperty("success", out _));
    }

    /// <summary>AC1 (fail-turn parity): sessionlog_fail_turn also returns a structured {error} for malformed turnJson.</summary>
    [Fact]
    public async Task SessionLogFailTurn_MalformedTurnJson_ReturnsStructuredError()
    {
        var json = await _tools.SessionLogFailTurn(
            "ClaudeCode",
            "ClaudeCode-20260716T000000Z-plugin-session",
            "req-20260716T000000Z-prompt-0002",
            Path.GetTempPath(),
            turnJson: "not-json",
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("error", out _), json);
    }

    /// <summary>AC3: a null turnJson (no payload) still completes successfully through the service.</summary>
    [Fact]
    public async Task SessionLogCompleteTurn_NullTurnJson_ReturnsSuccess()
    {
        _sessionLogService
            .UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(42L);

        var json = await _tools.SessionLogCompleteTurn(
            "ClaudeCode",
            "ClaudeCode-20260716T000000Z-plugin-session",
            "req-20260716T000000Z-prompt-0003",
            Path.GetTempPath(),
            turnJson: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("success", out var success) && success.GetBoolean(), json);
    }

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();

    private static FwhMcpTools CreateTools(McpDbContext db, ISessionLogService sessionLogService)
    {
        var workspaceContext = new WorkspaceContext { WorkspacePath = Path.GetTempPath() };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext
        {
            RequestServices = Substitute.For<IServiceProvider>()
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
