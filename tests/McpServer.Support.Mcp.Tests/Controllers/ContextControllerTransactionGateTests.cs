using System.Text;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-161: Verifies context rebuild and website ingest endpoints fail closed while required turn transactions are active.
/// </summary>
public sealed class ContextControllerTransactionGateTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly IContextSearchService _searchService = Substitute.For<IContextSearchService>();
    private readonly IGraphRagService _graphRagService = Substitute.For<IGraphRagService>();
    private readonly IWebsiteIngestor _websiteIngestor = Substitute.For<IWebsiteIngestor>();
    private readonly WorkspaceContext _workspaceContext = new() { WorkspacePath = @"F:\GitHub\McpServer" };

    /// <summary>Initializes the in-memory context database used by these controller tests.</summary>
    public ContextControllerTransactionGateTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"ContextControllerTransactionGateTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();
    }

    /// <summary>Disposes the in-memory database.</summary>
    public void Dispose()
    {
        _db.Dispose();
    }

    /// <summary>rebuild-index returns conflict before invoking the search index rebuild path.</summary>
    [Fact]
    public async Task RebuildIndexAsync_WhenTransactionsRequired_ReturnsConflictWithoutRebuild()
    {
        var controller = CreateController(new CapturingCoordinator(enabled: true));

        var result = await controller.RebuildIndexAsync(CancellationToken.None).ConfigureAwait(true);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("not transaction compensated", conflict.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        await _searchService.DidNotReceive()
            .RebuildAsync(Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>ingest-website returns conflict before fetching pages or writing context rows.</summary>
    [Fact]
    public async Task IngestWebsiteAsync_WhenTransactionsRequired_ReturnsConflictWithoutCallingWebsiteIngestor()
    {
        var controller = CreateController(new CapturingCoordinator(enabled: true));

        var result = await controller.IngestWebsiteAsync(new WebsiteIngestRequest { Url = "https://example.test/docs" }, CancellationToken.None)
            .ConfigureAwait(true);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Contains("not transaction compensated", conflict.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        await _websiteIngestor.DidNotReceive()
            .IngestAsync(Arg.Any<WebsiteIngestRequest>(), Arg.Any<Func<WebsiteIngestPage, Task>?>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>streaming website ingest returns HTTP 409 before the SSE stream starts when the coordinator is degraded.</summary>
    [Fact]
    public async Task IngestWebsiteStreamAsync_WhenCoordinatorDegraded_ReturnsConflictWithoutCallingWebsiteIngestor()
    {
        var controller = CreateController(new CapturingCoordinator(enabled: true, degraded: true, message: "txn degraded"));
        var httpContext = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        await controller.IngestWebsiteStreamAsync(new WebsiteIngestRequest { Url = "https://example.test/docs" }, CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        responseBody.Position = 0;
        var body = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.Contains("txn degraded", body, StringComparison.OrdinalIgnoreCase);
        await _websiteIngestor.DidNotReceive()
            .IngestAsync(Arg.Any<WebsiteIngestRequest>(), Arg.Any<Func<WebsiteIngestPage, Task>?>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>ingest-website delegates to the existing ingestion path when transaction gating is not required.</summary>
    [Fact]
    public async Task IngestWebsiteAsync_WhenTransactionsNotRequired_DelegatesToIngestionCoordinator()
    {
        _websiteIngestor
            .IngestAsync(Arg.Any<WebsiteIngestRequest>(), Arg.Any<Func<WebsiteIngestPage, Task>?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WebsiteIngestPage>>([CreatePage()]));
        var controller = CreateController(
            new CapturingCoordinator(enabled: true),
            new TurnTransactionOptions { Enabled = true, RequiredForMutations = false });

        var result = await controller.IngestWebsiteAsync(new WebsiteIngestRequest { Url = "https://example.test/docs" }, CancellationToken.None)
            .ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var ingestResult = Assert.IsType<WebsiteIngestResult>(ok.Value);
        Assert.Equal(1, ingestResult.DocumentsIngested);
        await _websiteIngestor.Received(1)
            .IngestAsync(Arg.Any<WebsiteIngestRequest>(), Arg.Any<Func<WebsiteIngestPage, Task>?>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private ContextController CreateController(
        ITurnTransactionCoordinator coordinator,
        TurnTransactionOptions? transactionOptions = null)
    {
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var chunker = new Chunker();
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var sessionLogService = Substitute.For<ISessionLogService>();
        var ingestionCoordinator = new IngestionCoordinator(
            _db,
            new RepoIngestor(chunker, ingestionOptions, _workspaceContext, NullLogger<RepoIngestor>.Instance),
            new SessionLogIngestor(chunker, ingestionOptions, _workspaceContext, sessionLogService, NullLogger<SessionLogIngestor>.Instance),
            new ExternalDocsIngestor(chunker, ingestionOptions, _workspaceContext, NullLogger<ExternalDocsIngestor>.Instance),
            new GitHubIngestor(chunker, gitHubCliService, NullLogger<GitHubIngestor>.Instance),
            new IssueIngestor(chunker, gitHubCliService, NullLogger<IssueIngestor>.Instance),
            _websiteIngestor,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            null,
            _workspaceContext,
            NullLogger<IngestionCoordinator>.Instance);

        return new ContextController(
            _db,
            _searchService,
            _graphRagService,
            ingestionCoordinator,
            MsOptions.Options.Create(new GraphRagOptions()),
            coordinator,
            MsOptions.Options.Create(transactionOptions ?? new TurnTransactionOptions { Enabled = true, RequiredForMutations = true }));
    }

    private static WebsiteIngestPage CreatePage()
    {
        const string documentId = "website-example";
        return new WebsiteIngestPage
        {
            Url = "https://example.test/docs",
            Outcome = new WebsiteIngestUrlResult
            {
                Url = "https://example.test/docs",
                Status = "ingested",
                SourceKey = "https://example.test/docs",
                ChunksWritten = 1,
            },
            Document = new ContextDocument
            {
                Id = documentId,
                SourceType = "website",
                SourceKey = "https://example.test/docs",
                IngestedAt = DateTime.UtcNow,
                ContentHash = "hash",
            },
            Chunks =
            [
                new ContextChunk
                {
                    Id = $"{documentId}-chunk-0",
                    DocumentId = documentId,
                    Content = "Website content",
                    TokenCount = 2,
                    ChunkIndex = 0,
                },
            ],
        };
    }

    private sealed class CapturingCoordinator : ITurnTransactionCoordinator
    {
        private readonly TurnTransactionStatusResponse _status;

        public CapturingCoordinator(bool enabled, bool degraded = false, string message = "")
        {
            _status = new TurnTransactionStatusResponse
            {
                Enabled = enabled,
                Degraded = degraded,
                Message = message,
            };
        }

        public Task<TurnTransactionResult> ExecuteAsync(
            TurnTransactionRequest request,
            Func<CancellationToken, Task<TurnMutationResult>> mutation,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public TurnTransactionStatusResponse GetStatus() => _status;
    }
}
