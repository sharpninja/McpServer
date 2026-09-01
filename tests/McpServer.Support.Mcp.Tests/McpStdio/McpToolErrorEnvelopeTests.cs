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
/// TEST-MCP-TRIAGEERR-001: MCP tool JSON from the shipped <see cref="FwhMcpTools"/> session-log
/// path exposes <c>code</c>, <c>message</c>, <c>retryable</c>, and <c>details.inner</c>.
/// </summary>
public sealed class McpToolErrorEnvelopeTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly ISessionLogService _sessionLogService = Substitute.For<ISessionLogService>();
    private readonly FwhMcpTools _tools;

    /// <summary>Builds an in-memory tool host with a substituted session-log service.</summary>
    public McpToolErrorEnvelopeTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"McpToolErrorEnvelopeTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
        _tools = CreateTools(_db, _sessionLogService);
    }

    /// <summary>DbUpdateException on complete_turn includes persistence/conflict code and inner text.</summary>
    [Fact]
    public async Task SessionLogCompleteTurn_DbUpdateException_ReturnsFourFieldEnvelopeWithInner()
    {
        var inner = new SqliteException("UNIQUE constraint failed: SessionLogTurns.RequestId", 19);
        _sessionLogService
            .UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns<long>(_ => throw new DbUpdateException(
                "An error occurred while saving the entity changes. See the inner exception for details.",
                inner));

        var json = await _tools.SessionLogCompleteTurn(
            "ClaudeCode",
            "ClaudeCode-20260818T000000Z-envelope",
            "req-20260818T000000Z-001-envelope",
            Path.GetTempPath(),
            turnJson: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("conflict", root.GetProperty("code").GetString());
        Assert.Equal("conflict", root.GetProperty("error").GetString());
        Assert.False(root.GetProperty("retryable").GetBoolean());
        Assert.Equal(inner.Message, root.GetProperty("details").GetProperty("inner").GetString());
        Assert.DoesNotContain("See the inner exception", json, StringComparison.Ordinal);
    }

    /// <summary>KeyNotFound on complete_turn is not_found with retryable false.</summary>
    [Fact]
    public async Task SessionLogCompleteTurn_KeyNotFound_ReturnsNotFoundEnvelope()
    {
        _sessionLogService
            .UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns<long>(_ => throw new KeyNotFoundException("Turn not found: req-missing"));

        var json = await _tools.SessionLogCompleteTurn(
            "ClaudeCode",
            "ClaudeCode-20260818T000000Z-envelope",
            "req-20260818T000000Z-001-missing",
            Path.GetTempPath(),
            turnJson: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("not_found", document.RootElement.GetProperty("code").GetString());
        Assert.False(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Equal("not_found", document.RootElement.GetProperty("details").GetProperty("reason").GetString());
    }

    /// <summary>Ordinary validation failure is not retryable and has a stable code.</summary>
    [Fact]
    public async Task SessionLogCompleteTurn_ArgumentException_ReturnsValidationEnvelope()
    {
        _sessionLogService
            .UpsertTurnAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UnifiedRequestEntryDto>(), Arg.Any<CancellationToken>())
            .Returns<long>(_ => throw new ArgumentException("sourceType is required."));

        var json = await _tools.SessionLogCompleteTurn(
            "ClaudeCode",
            "ClaudeCode-20260818T000000Z-envelope",
            "req-20260818T000000Z-002-envelope",
            Path.GetTempPath(),
            turnJson: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("validation_error", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("sourceType is required.", document.RootElement.GetProperty("message").GetString());
        Assert.False(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.Equal("validation", document.RootElement.GetProperty("details").GetProperty("reason").GetString());
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
