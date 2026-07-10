using System.Text.Json;
using McpServer.SessionLog.Transcripts;
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
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008: validates native MCP transcript ingestion tool inventory and invocation.
/// </summary>
public sealed class TranscriptMcpToolTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly McpDbContext _db;
    private readonly ITranscriptIngestionService _transcriptIngestionService = Substitute.For<ITranscriptIngestionService>();
    private readonly FwhMcpTools _tools;

    /// <summary>Initializes the MCP tool fixture with a substituted transcript ingestion service.</summary>
    public TranscriptMcpToolTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"TranscriptMcpToolTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
        _tools = CreateTools(_db, _transcriptIngestionService);
    }

    /// <summary>Native MCP assembly discovery exposes the exact transcript tool names.</summary>
    [Fact]
    public void FwhMcpTools_ExposesTranscriptToolNames()
    {
        var toolNames = typeof(FwhMcpTools)
            .GetMethods()
            .Select(method => method.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: true)
                .OfType<McpServerToolAttribute>()
                .FirstOrDefault()?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("sessionlog_ingest_path", toolNames);
        Assert.Contains("sessionlog_normalize_path", toolNames);
    }

    /// <summary>sessionlog_ingest_path delegates a workspace-bound request to the shared ingestion service.</summary>
    [Fact]
    public async Task SessionLogIngestPath_DelegatesWorkspaceBoundRequest()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), "mcp-transcript-tool", Guid.NewGuid().ToString("N"));
        var result = CreateResult(workspacePath, "run-tool-ingest");
        TranscriptIngestionRequest? captured = null;
        _transcriptIngestionService
            .IngestPathAsync(Arg.Do<TranscriptIngestionRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns(result);

        var json = await _tools.SessionLogIngestPath(
            workspacePath,
            "transcripts/session.jsonl",
            "Codex",
            source: "Codex",
            recursive: false,
            strict: true,
            persist: true,
            compatibilityProfile: null,
            emitNormalizedProfile: false,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = JsonSerializer.Deserialize<TranscriptIngestRunResponse>(json, JsonOptions);
        Assert.NotNull(response);
        Assert.NotNull(captured);
        Assert.Equal(workspacePath, captured.WorkspacePath);
        Assert.Equal("transcripts/session.jsonl", captured.Path);
        Assert.Equal("Codex", captured.Agent);
        Assert.Equal(TranscriptSourceKind.Codex, captured.SourceKind);
        Assert.False(captured.Recursive);
        Assert.True(captured.Strict);
        Assert.True(captured.Persist);
        Assert.Equal(TranscriptCompatibilityProfile.None, captured.CompatibilityProfile);
        Assert.Equal("run-tool-ingest", response.RunId);
    }

    /// <summary>sessionlog_normalize_path requires a target profile and disables persistence by default.</summary>
    [Fact]
    public async Task SessionLogNormalizePath_DelegatesProfileProjectionWithoutPersistenceByDefault()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), "mcp-transcript-tool", Guid.NewGuid().ToString("N"));
        var result = CreateResult(workspacePath, "run-tool-normalize");
        TranscriptIngestionRequest? captured = null;
        _transcriptIngestionService
            .IngestPathAsync(Arg.Do<TranscriptIngestionRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns(result);

        var json = await _tools.SessionLogNormalizePath(
            workspacePath,
            "transcripts/session.jsonl",
            "Codex",
            "Grok",
            source: "Codex",
            recursive: true,
            strict: true,
            persist: false,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = JsonSerializer.Deserialize<TranscriptIngestRunResponse>(json, JsonOptions);
        Assert.NotNull(response);
        Assert.NotNull(captured);
        Assert.Equal(workspacePath, captured.WorkspacePath);
        Assert.Equal("transcripts/session.jsonl", captured.Path);
        Assert.Equal("Codex", captured.Agent);
        Assert.Equal(TranscriptSourceKind.Codex, captured.SourceKind);
        Assert.True(captured.Recursive);
        Assert.True(captured.Strict);
        Assert.False(captured.Persist);
        Assert.Equal(TranscriptCompatibilityProfile.Grok, captured.CompatibilityProfile);
        Assert.Equal("run-tool-normalize", response.RunId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
    }

    private static Task<TranscriptIngestionResult> CreateResult(string workspacePath, string runId)
    {
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "root",
            "session-1",
            "hash",
            "normalized",
            Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", runId, "session-1.hash.sessionlog.yaml"),
            string.Empty);
        return Task.FromResult(new TranscriptIngestionResult(
            sessions: [],
            diagnostics: [],
            runId: runId,
            artifactRootPath: Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", runId),
            importRecoveryPaths: [],
            persisted: false,
            degraded: false,
            receipts: [receipt]));
    }

    private static FwhMcpTools CreateTools(McpDbContext db, ITranscriptIngestionService transcriptIngestionService)
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
            transcriptIngestionService: transcriptIngestionService);
    }
}
