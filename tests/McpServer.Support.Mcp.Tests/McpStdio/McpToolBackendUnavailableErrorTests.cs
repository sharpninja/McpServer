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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): when the storage backend is unreachable the MCP tools
/// SHALL return the stable machine-readable payload <c>{"error":"backend_unavailable", ...}</c>
/// instead of echoing raw storage-provider text (raw SqlClient/SQLite messages, the
/// EnableRetryOnFailure hint). Representative surface: <c>sessionlog_complete_turn</c>.
/// Fixture: an in-memory McpDbContext plus a substituted <see cref="ISessionLogService"/> whose
/// UpsertTurnAsync throws a connection-class storage exception (SQLite error 14, CANTOPEN).
/// </summary>
public sealed class McpToolBackendUnavailableErrorTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly ISessionLogService _sessionLogService = Substitute.For<ISessionLogService>();
    private readonly FwhMcpTools _tools;

    /// <summary>Initializes the fixture with an in-memory DB and a substituted session-log service.</summary>
    public McpToolBackendUnavailableErrorTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"McpToolBackendUnavailableErrorTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
        _tools = CreateTools(_db, _sessionLogService);
    }

    /// <summary>
    /// AC (TR-MCP-HEALTH-003): a connection-class storage failure surfaces as the typed
    /// <c>backend_unavailable</c> error, not the raw provider message. Red on the pre-fix tools:
    /// the payload echoes the untyped raw SQLite text, which this test quotes in its failure
    /// message.
    /// </summary>
    [Fact]
    public async Task SessionLogCompleteTurn_StorageUnreachable_ReturnsTypedBackendUnavailableError()
    {
        _sessionLogService
            .UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns<long>(_ => throw new SqliteException("unable to open database file", 14));

        var json = await _tools.SessionLogCompleteTurn(
            "ClaudeCode",
            "ClaudeCode-20260720T000000Z-storage-outage",
            "req-20260720T000000Z-001-storage-outage",
            Path.GetTempPath(),
            turnJson: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        var error = document.RootElement.GetProperty("error").GetString();
        Assert.True(
            error == "backend_unavailable",
            $"Expected the typed backend_unavailable error; actual tool payload: {json}");
        Assert.Equal("backend_unavailable", document.RootElement.GetProperty("code").GetString());
        Assert.True(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("message").GetString()));
        Assert.Equal("backend_unavailable", document.RootElement.GetProperty("details").GetProperty("reason").GetString());
        Assert.DoesNotContain("SQLite Error", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Guard (TR-MCP-HEALTH-003): non-storage failures keep the existing untyped shape so
    /// ordinary tool errors are not reclassified. Passes before and after the fix by design.
    /// </summary>
    [Fact]
    public async Task SessionLogCompleteTurn_OrdinaryFailure_KeepsUntypedErrorMessage()
    {
        _sessionLogService
            .UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns<long>(_ => throw new InvalidOperationException("turn validation failed"));

        var json = await _tools.SessionLogCompleteTurn(
            "ClaudeCode",
            "ClaudeCode-20260720T000000Z-storage-outage",
            "req-20260720T000000Z-002-ordinary-failure",
            Path.GetTempPath(),
            turnJson: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("internal_server_error", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("turn validation failed", document.RootElement.GetProperty("message").GetString());
        Assert.False(document.RootElement.GetProperty("retryable").GetBoolean());
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
