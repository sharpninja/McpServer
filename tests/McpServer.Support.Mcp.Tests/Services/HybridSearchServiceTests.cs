using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013: Unit tests for HybridSearchService RRF blending and degradation.</summary>
public sealed class HybridSearchServiceTests : IAsyncLifetime
{
    private const string WorkspacePath = @"E:\tests\hybrid-search";

    private McpDbContext _db = null!;
    private IContextSearchService _fts5 = null!;
    private IVectorIndexService _vectorIndex = null!;
    private IEmbeddingService _embedding = null!;

    public async ValueTask InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"hybrid_test_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        await _db.Database.EnsureCreatedAsync().ConfigureAwait(true);
        _db.OverrideWorkspaceId(WorkspacePath);

        // Seed data
        var doc = new ContextDocumentEntity
        {
            Id = "doc1",
            SourceType = "repo",
            SourceKey = "test.md",
            IngestedAt = DateTime.UtcNow,
            ContentHash = "abc123"
        };
        _db.Documents.Add(doc);
        _db.Chunks.Add(new ContextChunkEntity
        {
            Id = "chunk1",
            DocumentId = "doc1",
            Content = "hello world test content",
            TokenCount = 5,
            ChunkIndex = 0
        });
        _db.Chunks.Add(new ContextChunkEntity
        {
            Id = "chunk2",
            DocumentId = "doc1",
            Content = "another piece of text",
            TokenCount = 5,
            ChunkIndex = 1
        });
        await _db.SaveChangesAsync().ConfigureAwait(true);

        _fts5 = Substitute.For<IContextSearchService>();
        _vectorIndex = Substitute.For<IVectorIndexService>();
        _embedding = Substitute.For<IEmbeddingService>();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync().ConfigureAwait(true);
    }

    [Fact]
    public async Task SearchAsync_BothModes_ReturnsRRFBlendedResults()
    {
        var fts5Result = new ContextSearchResult(
            [new ScoredChunk { ChunkId = "chunk1", DocumentId = "doc1", Content = "hello world", Score = -1.0, Snippet = "<b>hello</b>" }],
            ["test.md"]);
        _fts5.SearchAsync("hello", Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(fts5Result);

        _embedding.IsAvailable.Returns(true);
        _embedding.GenerateEmbedding("hello").Returns(new float[384]);
        _vectorIndex.Count.Returns(1);
        _vectorIndex.Search(Arg.Any<float[]>(), Arg.Any<int>())
            .Returns(new List<(string ChunkId, float Distance)> { ("chunk1", 0.1f), ("chunk2", 0.5f) });

        var sut = new HybridSearchService(_fts5, _vectorIndex, _embedding, _db, NullLogger<HybridSearchService>.Instance);
        var result = await sut.SearchAsync("hello", 10).ConfigureAwait(true);

        Assert.NotEmpty(result.Chunks);
        Assert.Equal("chunk1", result.Chunks[0].ChunkId);
    }

    [Fact]
    public async Task SearchAsync_Fts5Fails_ReturnsVectorOnly()
    {
        _fts5.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("FTS5 failed"));

        _embedding.IsAvailable.Returns(true);
        _embedding.GenerateEmbedding(Arg.Any<string>()).Returns(new float[384]);
        _vectorIndex.Count.Returns(1);
        _vectorIndex.Search(Arg.Any<float[]>(), Arg.Any<int>())
            .Returns(new List<(string ChunkId, float Distance)> { ("chunk1", 0.1f) });

        var sut = new HybridSearchService(_fts5, _vectorIndex, _embedding, _db, NullLogger<HybridSearchService>.Instance);
        var result = await sut.SearchAsync("test", 10).ConfigureAwait(true);

        Assert.NotEmpty(result.Chunks);
    }

    [Fact]
    public async Task SearchAsync_VectorUnavailable_ReturnsFts5Only()
    {
        var fts5Result = new ContextSearchResult(
            [new ScoredChunk { ChunkId = "chunk1", DocumentId = "doc1", Content = "hello", Score = -1.0 }],
            ["test.md"]);
        _fts5.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(fts5Result);

        _embedding.IsAvailable.Returns(false);

        var sut = new HybridSearchService(_fts5, _vectorIndex, _embedding, _db, NullLogger<HybridSearchService>.Instance);
        var result = await sut.SearchAsync("hello", 10).ConfigureAwait(true);

        Assert.NotEmpty(result.Chunks);
        Assert.Equal("chunk1", result.Chunks[0].ChunkId);
    }

    [Fact]
    public async Task SearchAsync_BothFail_FallsBackToLinq()
    {
        _fts5.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("FTS5 failed"));

        _embedding.IsAvailable.Returns(false);

        var sut = new HybridSearchService(_fts5, _vectorIndex, _embedding, _db, NullLogger<HybridSearchService>.Instance);
        var result = await sut.SearchAsync("hello", 10).ConfigureAwait(true);

        Assert.NotEmpty(result.Chunks);
    }

    [Fact]
    public async Task SearchAsync_PreservesFts5Snippets()
    {
        var fts5Result = new ContextSearchResult(
            [new ScoredChunk { ChunkId = "chunk1", DocumentId = "doc1", Content = "hello world", Score = -1.0, Snippet = "<b>hello</b> world" }],
            ["test.md"]);
        _fts5.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(fts5Result);

        _embedding.IsAvailable.Returns(false);

        var sut = new HybridSearchService(_fts5, _vectorIndex, _embedding, _db, NullLogger<HybridSearchService>.Instance);
        var result = await sut.SearchAsync("hello", 10).ConfigureAwait(true);

        Assert.Equal("<b>hello</b> world", result.Chunks[0].Snippet);
    }

    [Fact]
    public async Task RebuildAsync_CallsBothServices()
    {
        var sut = new HybridSearchService(_fts5, _vectorIndex, _embedding, _db, NullLogger<HybridSearchService>.Instance);

        await sut.RebuildAsync().ConfigureAwait(true);

        await _fts5.Received(1).RebuildAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _vectorIndex.Received(1).RebuildAsync(Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }
}
