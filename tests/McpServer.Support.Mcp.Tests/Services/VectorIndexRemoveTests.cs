using McpServer.Support.Mcp.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Validates the RemoveVector method on
/// VectorIndexService, covering successful removal, unknown chunk IDs,
/// and post-removal search behavior.
/// </summary>
public sealed class VectorIndexRemoveTests
{
    /// <summary>
    /// FR-MCP-080: Verifies that RemoveVector returns true for an existing chunk
    /// and decrements the Count.
    /// </summary>
    [Fact]
    public void RemoveVector_ExistingChunk_ReturnsTrueAndDecrementsCount()
    {
        var sut = CreateService();
        sut.AddVector("chunk-1", CreateEmbedding(0.5f));
        sut.AddVector("chunk-2", CreateEmbedding(0.9f));
        Assert.Equal(2, sut.Count);

        var result = sut.RemoveVector("chunk-1");

        Assert.True(result);
        Assert.Equal(1, sut.Count);
    }

    /// <summary>
    /// FR-MCP-080: Verifies that RemoveVector returns false when the chunk ID
    /// does not exist in the index.
    /// </summary>
    [Fact]
    public void RemoveVector_UnknownChunkId_ReturnsFalse()
    {
        var sut = CreateService();
        sut.AddVector("chunk-1", CreateEmbedding(0.5f));

        var result = sut.RemoveVector("nonexistent-chunk");

        Assert.False(result);
        Assert.Equal(1, sut.Count);
    }

    /// <summary>
    /// FR-MCP-080: Verifies that after removing a vector, it is no longer
    /// returned by Search, while other vectors remain searchable.
    /// Uses multiple vectors so the HNSW search returns enough valid results
    /// after filtering out the stale removed entry.
    /// </summary>
    [Fact]
    public void RemoveVector_RemovedChunkNotReturnedBySearch()
    {
        var sut = CreateService();
        sut.AddVector("chunk-a", CreateEmbedding(0.1f));
        sut.AddVector("chunk-b", CreateEmbedding(0.3f));
        sut.AddVector("chunk-keep", CreateEmbedding(0.5f));
        sut.AddVector("chunk-remove", CreateEmbedding(0.9f));

        sut.RemoveVector("chunk-remove");

        var results = sut.Search(CreateEmbedding(0.5f), k: 10);
        Assert.DoesNotContain(results, r => r.ChunkId == "chunk-remove");
        Assert.True(results.Count > 0, "At least one non-removed chunk should be returned");
    }

    /// <summary>
    /// FR-MCP-080: Verifies that removing all vectors in the index leaves it
    /// empty and Search returns no results.
    /// </summary>
    [Fact]
    public void RemoveVector_AllVectorsRemoved_SearchReturnsEmpty()
    {
        var sut = CreateService();
        sut.AddVector("chunk-1", CreateEmbedding(0.3f));
        sut.AddVector("chunk-2", CreateEmbedding(0.7f));

        sut.RemoveVector("chunk-1");
        sut.RemoveVector("chunk-2");

        Assert.Equal(0, sut.Count);
        var results = sut.Search(CreateEmbedding(0.5f), k: 10);
        Assert.Empty(results);
    }

    private static VectorIndexService CreateService()
    {
        var options = new VectorIndexOptions { Enabled = true, MaxElements = 1000 };
        return new VectorIndexService(options, NullLogger<VectorIndexService>.Instance);
    }

    private static float[] CreateEmbedding(float seed)
    {
        var embedding = new float[384];
        for (var i = 0; i < embedding.Length; i++)
            embedding[i] = seed + (i * 0.001f);
        return embedding;
    }
}
