using McpServer.Support.Mcp.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Indexing;

/// <summary>TR-PLANNED-013: Unit tests for EmbeddingService (stub mode and ONNX integration).</summary>
public sealed class EmbeddingServiceTests : IDisposable
{
    private readonly EmbeddingService _sut = new(new EmbeddingOptions(), NullLogger<EmbeddingService>.Instance);

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void Dimensions_Returns384()
    {
        Assert.Equal(384, _sut.Dimensions);
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenModelMissing()
    {
        Assert.False(_sut.IsAvailable);
    }

    [Fact]
    public void GenerateEmbedding_ReturnsCorrectDimensions()
    {
        var embedding = _sut.GenerateEmbedding("test input");

        Assert.Equal(384, embedding.Length);
    }

    [Fact]
    public void GenerateEmbeddings_ReturnsCorrectCount()
    {
        var texts = new[] { "text1", "text2", "text3" };
        var embeddings = _sut.GenerateEmbeddings(texts);

        Assert.Equal(3, embeddings.Length);
        Assert.All(embeddings, e => Assert.Equal(384, e.Length));
    }

    [Fact]
    public void GenerateEmbedding_EmptyInput_HandlesGracefully()
    {
        var embedding = _sut.GenerateEmbedding(string.Empty);

        Assert.Equal(384, embedding.Length);
    }

    [Fact]
    public void GenerateEmbedding_StubMode_ReturnsZeroVector()
    {
        var embedding = _sut.GenerateEmbedding("test");

        Assert.All(embedding, v => Assert.Equal(0f, v));
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_WithModel_ReturnsNonZeroVector()
    {
        // This test requires the ONNX model to be downloaded
        if (!_sut.IsAvailable)
            return; // Skip in CI

        var embedding = _sut.GenerateEmbedding("hello world");
        Assert.Contains(embedding, v => v != 0f);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_SameInput_ReturnsDeterministicOutput()
    {
        if (!_sut.IsAvailable)
            return;

        var e1 = _sut.GenerateEmbedding("deterministic test");
        var e2 = _sut.GenerateEmbedding("deterministic test");
        Assert.Equal(e1, e2);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_DifferentInput_ReturnsDifferentVectors()
    {
        if (!_sut.IsAvailable)
            return;

        var e1 = _sut.GenerateEmbedding("cats and dogs");
        var e2 = _sut.GenerateEmbedding("quantum physics");
        Assert.NotEqual(e1, e2);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_LongInput_TruncatesCorrectly()
    {
        if (!_sut.IsAvailable)
            return;

        var longText = string.Join(" ", Enumerable.Repeat("word", 500));
        var embedding = _sut.GenerateEmbedding(longText);
        Assert.Equal(384, embedding.Length);
        Assert.Contains(embedding, v => v != 0f);
    }
}
