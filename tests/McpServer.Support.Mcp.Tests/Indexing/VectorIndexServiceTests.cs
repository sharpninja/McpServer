using McpServer.Support.Mcp.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Indexing;

/// <summary>TR-PLANNED-013: Unit tests for VectorIndexService (HNSW).</summary>
public sealed class VectorIndexServiceTests : IDisposable
{
    private readonly VectorIndexService _sut = new(new VectorIndexOptions { MaxElements = 1000 }, NullLogger<VectorIndexService>.Instance);

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void WhenAddAndSearchThenRoundTrips()
    {
        var embedding = new float[384];
        embedding[0] = 1f;
        _sut.AddVector("chunk1", embedding);

        var query = new float[384];
        query[0] = 1f;
        var results = _sut.Search(query, 5);

        Assert.Single(results);
        Assert.Equal("chunk1", results[0].ChunkId);
        Assert.True(results[0].Distance < 0.01f, "Same vector should have near-zero distance");
    }

    [Fact]
    public void WhenEmptyIndexThenSearchReturnsEmpty()
    {
        var results = _sut.Search(new float[384], 5);

        Assert.Empty(results);
    }

    [Fact]
    public void WhenMultipleVectorsThenReturnsKNearest()
    {
        var close = new float[384]; close[0] = 1f;
        var far = new float[384]; far[1] = 1f;
        var medium = new float[384]; medium[0] = 0.7f; medium[1] = 0.7f;

        _sut.AddVector("close", close);
        _sut.AddVector("far", far);
        _sut.AddVector("medium", medium);

        var query = new float[384]; query[0] = 1f;
        var results = _sut.Search(query, 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("close", results[0].ChunkId);
    }

    [Fact]
    public void Count_ReflectsAddedVectors()
    {
        Assert.Equal(0, _sut.Count);

        var v1 = new float[384]; v1[0] = 1f;
        var v2 = new float[384]; v2[1] = 1f;
        _sut.AddVector("a", v1);
        _sut.AddVector("b", v2);

        Assert.Equal(2, _sut.Count);
    }

    [Fact]
    public async Task RebuildAsync_ClearsIndex()
    {
        var v = new float[384]; v[0] = 1f;
        _sut.AddVector("chunk1", v);
        Assert.Equal(1, _sut.Count);

        await _sut.RebuildAsync().ConfigureAwait(true);

        Assert.Equal(0, _sut.Count);
    }

    [Fact]
    public async Task SaveAsync_LoadAsync_PersistsAndRestoresIndex()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hnsw_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var indexPath = Path.Combine(tempDir, "test.idx");

        try
        {
            var v1 = new float[384]; v1[0] = 1f;
            var v2 = new float[384]; v2[1] = 1f;
            _sut.AddVector("chunk1", v1);
            _sut.AddVector("chunk2", v2);

            await _sut.SaveAsync(indexPath).ConfigureAwait(true);

            var sut2 = new VectorIndexService(new VectorIndexOptions { MaxElements = 1000 }, NullLogger<VectorIndexService>.Instance);
            await sut2.LoadAsync(indexPath).ConfigureAwait(true);

            Assert.Equal(2, sut2.Count);
            var query = new float[384]; query[0] = 1f;
            var results = sut2.Search(query, 1);
            Assert.Single(results);
            Assert.Equal("chunk1", results[0].ChunkId);

            sut2.Dispose();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Search_KLargerThanIndex_ReturnsAll()
    {
        var v1 = new float[384]; v1[0] = 1f;
        var v2 = new float[384]; v2[1] = 1f;
        _sut.AddVector("a", v1);
        _sut.AddVector("b", v2);

        var query = new float[384]; query[0] = 1f;
        var results = _sut.Search(query, 100);

        Assert.Equal(2, results.Count);
    }
}
