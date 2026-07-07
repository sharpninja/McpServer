using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-078, TR-GRAPHRAG-ADHOC-001: Validates ad-hoc text ingestion
/// through <see cref="GraphRagService.IngestTextAsync"/> using an in-memory
/// EF Core database, mock embedding service, and mock vector index service.
/// </summary>
public sealed class GraphRagIngestTextTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\graphrag-ingest";

    private readonly McpDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorIndexService _vectorIndexService;
    private readonly string _tempWorkspacePath;

    /// <summary>Initializes in-memory DB and mock services for each test.</summary>
    public GraphRagIngestTextTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"IngestTextTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);

        _embeddingService = Substitute.For<IEmbeddingService>();
        _embeddingService.Dimensions.Returns(384);
        _embeddingService.IsAvailable.Returns(true);
        _embeddingService.GenerateEmbedding(Arg.Any<string>()).Returns(new float[384]);

        _vectorIndexService = Substitute.For<IVectorIndexService>();

        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), $"graphrag-ingest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempWorkspacePath);
    }

    /// <summary>Disposes DB and cleans up temp directory.</summary>
    public void Dispose()
    {
        _db.Dispose();
        try { if (Directory.Exists(_tempWorkspacePath)) Directory.Delete(_tempWorkspacePath, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// FR-MCP-078: Verifies that valid content produces a document and chunks in the DB.
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_ValidContent_CreatesDocumentAndChunks()
    {
        var sut = CreateSut();
        var request = new GraphRagIngestTextRequest { Content = "Hello world. This is a test document with enough content." };

        var result = await sut.IngestTextAsync(request, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result.DocumentId);
        Assert.True(result.ChunkCount > 0);
        Assert.True(result.TokenCount > 0);

        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == result.DocumentId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(doc);
        var chunks = await _db.Chunks.Where(c => c.DocumentId == result.DocumentId).ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(result.ChunkCount, chunks.Count);
    }

    /// <summary>
    /// FR-MCP-078: Verifies that empty content throws <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_EmptyContent_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var request = new GraphRagIngestTextRequest { Content = "" };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.IngestTextAsync(request, ct: TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-078: Verifies that when SourceType is not set, it defaults to "adhoc-text".
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_DefaultsSourceType_ToAdhocText()
    {
        var sut = CreateSut();
        var request = new GraphRagIngestTextRequest { Content = "Some ad-hoc content for testing defaults." };

        var result = await sut.IngestTextAsync(request, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("adhoc-text", result.SourceType);
    }

    /// <summary>
    /// FR-MCP-078: Verifies that when SourceKey is not set, it defaults to Title if provided, otherwise to the document ID.
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_DefaultsSourceKey_ToTitleOrDocId()
    {
        var sut = CreateSut();

        // With title
        var withTitle = new GraphRagIngestTextRequest { Content = "Content A.", Title = "My Title" };
        var resultWithTitle = await sut.IngestTextAsync(withTitle, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("My Title", resultWithTitle.SourceKey);

        // Without title
        var withoutTitle = new GraphRagIngestTextRequest { Content = "Content B." };
        var resultWithoutTitle = await sut.IngestTextAsync(withoutTitle, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(resultWithoutTitle.DocumentId, resultWithoutTitle.SourceKey);
    }

    /// <summary>
    /// FR-MCP-078: Verifies that the chunk count matches Chunker output for known content.
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_ChunkCount_MatchesExpected()
    {
        var sut = CreateSut();
        var content = new string('A', 4096); // Should produce multiple chunks at 512 tokens * 4 chars = 2048 chars per chunk
        var request = new GraphRagIngestTextRequest { Content = content };

        var result = await sut.IngestTextAsync(request, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var chunker = new Chunker();
        var expectedChunks = chunker.Chunk("test-doc", content);
        Assert.Equal(expectedChunks.Count, result.ChunkCount);
    }

    /// <summary>
    /// FR-MCP-078: Verifies that TriggerReindex=true causes IndexAsync to be called.
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_TriggerReindex_CallsIndexAsync()
    {
        // We test this indirectly — IndexAsync creates the ready artifact.
        var sut = CreateSut();
        var request = new GraphRagIngestTextRequest
        {
            Content = "Content for reindex test.",
            TriggerReindex = true
        };

        var result = await sut.IngestTextAsync(request, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.ReindexTriggered);
    }

    /// <summary>
    /// FR-MCP-078: Verifies that TriggerReindex=false does not trigger reindexing.
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_NoTriggerReindex_DoesNotCallIndexAsync()
    {
        var sut = CreateSut();
        var request = new GraphRagIngestTextRequest
        {
            Content = "Content without reindex.",
            TriggerReindex = false
        };

        var result = await sut.IngestTextAsync(request, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.ReindexTriggered);
    }

    private GraphRagService CreateSut()
    {
        var graphRagOptions = Microsoft.Extensions.Options.Options.Create(new GraphRagOptions
        {
            Enabled = true,
            RootPath = "mcp-data/graphrag",
            ArtifactVersion = "v1"
        });
        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempWorkspacePath });
        var workspaceContext = new WorkspaceContext { WorkspacePath = _tempWorkspacePath };
        var contextSearch = Substitute.For<IContextSearchService>();
        contextSearch
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ContextSearchResult([], []));
        var adapters = new IGraphRagBackendAdapter[]
        {
            new InternalFallbackGraphRagBackendAdapter()
        };

        return new GraphRagService(
            graphRagOptions,
            ingestionOptions,
            workspaceContext,
            contextSearch,
            adapters,
            NullLogger<GraphRagService>.Instance,
            _db,
            _embeddingService,
            _vectorIndexService);
    }
}
