using McpServer.Support.Mcp.Indexing;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Indexing;

/// <summary>TR-PLANNED-013: Unit tests for Chunker.</summary>
public sealed class ChunkerTests
{
    private readonly Chunker _sut = new();

    [Fact]
    public void WhenContentFitsInOneChunkThenReturnsSingleChunk()
    {
        var chunks = _sut.Chunk("doc1", "Hello world");

        Assert.Single(chunks);
        Assert.Equal("doc1", chunks[0].DocumentId);
        Assert.Equal("Hello world", chunks[0].Content);
        Assert.Equal(0, chunks[0].ChunkIndex);
    }

    [Fact]
    public void WhenContentExceedsMaxTokensThenReturnsMultipleChunks()
    {
        var longContent = new string('A', 5000);
        var chunks = _sut.Chunk("doc2", longContent);

        Assert.True(chunks.Count > 1, "Expected multiple chunks for 5000 chars");
        for (var i = 0; i < chunks.Count; i++)
        {
            Assert.Equal(i, chunks[i].ChunkIndex);
            Assert.Equal("doc2", chunks[i].DocumentId);
        }
    }

    [Fact]
    public void WhenContentIsEmptyThenReturnsEmptyList()
    {
        var chunks = _sut.Chunk("doc3", "");
        Assert.Empty(chunks);
    }

    [Fact]
    public void WhenContentIsWhitespaceThenReturnsEmptyList()
    {
        var chunks = _sut.Chunk("doc4", "   \n\t  ");
        Assert.Empty(chunks);
    }

    [Fact]
    public void WhenSameInputThenChunkIdsAreStable()
    {
        var chunks1 = _sut.Chunk("doc5", "Deterministic content");
        var chunks2 = _sut.Chunk("doc5", "Deterministic content");

        Assert.Equal(chunks1[0].Id, chunks2[0].Id);
    }

    [Fact]
    public void WhenDifferentInputThenChunkIdsAreDifferent()
    {
        var chunks1 = _sut.Chunk("doc6", "Content A");
        var chunks2 = _sut.Chunk("doc6", "Content B");

        Assert.NotEqual(chunks1[0].Id, chunks2[0].Id);
    }

    [Fact]
    public void EstimateTokenCount_ReturnsApproximation()
    {
        var count = Chunker.EstimateTokenCount("Hello World!");
        Assert.Equal(3, count);
    }

    [Fact]
    public void EstimateTokenCount_EmptyString_ReturnsZero()
    {
        Assert.Equal(0, Chunker.EstimateTokenCount(""));
    }
}
