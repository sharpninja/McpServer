using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Validates document management operations
/// (ListDocuments, GetDocumentChunks, DeleteDocument) in <see cref="GraphRagService"/>
/// using an in-memory EF Core database seeded with test documents and chunks.
/// </summary>
public sealed class GraphRagDocumentManagementTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\graphrag-docmgmt";

    private readonly McpDbContext _db;
    private readonly IVectorIndexService _vectorIndexService;
    private readonly string _tempWorkspacePath;

    /// <summary>Initializes in-memory DB, seeds test data, and creates mocks.</summary>
    public GraphRagDocumentManagementTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"DocMgmtTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);

        _vectorIndexService = Substitute.For<IVectorIndexService>();
        _vectorIndexService.RemoveVector(Arg.Any<string>()).Returns(true);

        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), $"graphrag-docmgmt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempWorkspacePath);

        SeedDocuments();
    }

    /// <summary>Disposes DB and cleans up temp directory.</summary>
    public void Dispose()
    {
        _db.Dispose();
        try { if (Directory.Exists(_tempWorkspacePath)) Directory.Delete(_tempWorkspacePath, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// FR-MCP-080: Verifies that ListDocumentsAsync returns paginated results.
    /// </summary>
    [Fact]
    public async Task ListDocumentsAsync_ReturnsPaginatedResults()
    {
        var sut = CreateSut();

        var result = await sut.ListDocumentsAsync(skip: 0, take: 2, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, result.Documents.Count);
        Assert.Equal(3, result.TotalCount);
    }

    /// <summary>
    /// FR-MCP-080: Verifies that ListDocumentsAsync filters by source type.
    /// </summary>
    [Fact]
    public async Task ListDocumentsAsync_FiltersBySourceType()
    {
        var sut = CreateSut();

        var result = await sut.ListDocumentsAsync(sourceType: "repo", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Single(result.Documents);
        Assert.All(result.Documents, d => Assert.Equal("repo", d.SourceType));
    }

    /// <summary>
    /// FR-MCP-080: Verifies that GetDocumentChunksAsync returns chunks ordered by index.
    /// </summary>
    [Fact]
    public async Task GetDocumentChunksAsync_ReturnsChunksOrderedByIndex()
    {
        var sut = CreateSut();

        var result = await sut.GetDocumentChunksAsync("doc-1", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(result);
        Assert.Equal("doc-1", result!.DocumentId);
        Assert.Equal(2, result.TotalChunks);
        Assert.Equal(0, result.Chunks[0].ChunkIndex);
        Assert.Equal(1, result.Chunks[1].ChunkIndex);
    }

    /// <summary>
    /// FR-MCP-080: Verifies that GetDocumentChunksAsync returns null for a nonexistent document.
    /// </summary>
    [Fact]
    public async Task GetDocumentChunksAsync_ReturnsNullForNonexistent()
    {
        var sut = CreateSut();

        var result = await sut.GetDocumentChunksAsync("nonexistent-doc", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(result);
    }

    /// <summary>
    /// FR-MCP-080: Verifies that DeleteDocumentAsync removes the document and its chunks.
    /// </summary>
    [Fact]
    public async Task DeleteDocumentAsync_RemovesDocumentAndChunks()
    {
        var sut = CreateSut();

        var result = await sut.DeleteDocumentAsync("doc-1", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("doc-1", result.DocumentId);
        Assert.Equal(2, result.ChunksRemoved);

        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == "doc-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Null(doc);
        var chunks = await _db.Chunks.Where(c => c.DocumentId == "doc-1").ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Empty(chunks);
    }

    /// <summary>
    /// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Verifies that DeleteDocumentAsync calls RemoveVector for each chunk.
    /// </summary>
    [Fact]
    public async Task DeleteDocumentAsync_CallsRemoveVectorForEachChunk()
    {
        var sut = CreateSut();

        await sut.DeleteDocumentAsync("doc-1", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        _vectorIndexService.Received(1).RemoveVector("doc-1-chunk-0");
        _vectorIndexService.Received(1).RemoveVector("doc-1-chunk-1");
    }

    private void SeedDocuments()
    {
        var now = DateTime.UtcNow;

        _db.Documents.Add(new ContextDocumentEntity
        {
            Id = "doc-1",
            SourceType = "adhoc-text",
            SourceKey = "doc-1-key",
            IngestedAt = now,
            ContentHash = "hash1"
        });
        _db.Documents.Add(new ContextDocumentEntity
        {
            Id = "doc-2",
            SourceType = "adhoc-text",
            SourceKey = "doc-2-key",
            IngestedAt = now.AddMinutes(-1),
            ContentHash = "hash2"
        });
        _db.Documents.Add(new ContextDocumentEntity
        {
            Id = "doc-3",
            SourceType = "repo",
            SourceKey = "doc-3-key",
            IngestedAt = now.AddMinutes(-2),
            ContentHash = "hash3"
        });

        _db.Chunks.AddRange(
            new ContextChunkEntity { Id = "doc-1-chunk-0", DocumentId = "doc-1", Content = "Chunk 0 of doc 1", TokenCount = 5, ChunkIndex = 0 },
            new ContextChunkEntity { Id = "doc-1-chunk-1", DocumentId = "doc-1", Content = "Chunk 1 of doc 1", TokenCount = 5, ChunkIndex = 1 },
            new ContextChunkEntity { Id = "doc-2-chunk-0", DocumentId = "doc-2", Content = "Chunk 0 of doc 2", TokenCount = 5, ChunkIndex = 0 },
            new ContextChunkEntity { Id = "doc-3-chunk-0", DocumentId = "doc-3", Content = "Chunk 0 of doc 3", TokenCount = 5, ChunkIndex = 0 }
        );

        _db.SaveChanges();
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
        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.Dimensions.Returns(384);
        embeddingService.IsAvailable.Returns(true);
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
            embeddingService,
            _vectorIndexService);
    }
}
