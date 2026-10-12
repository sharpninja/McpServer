using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using QBrainAi.Support.Mcp.Storage.Indexing;

namespace QBrainAi.Support.Mcp.Tests.Indexing;

/// <summary>TR-PLANNED-CORE-013: Unit tests for EmbeddingService (stub mode and ONNX integration).</summary>
public sealed class EmbeddingServiceTests : IDisposable
{
    private readonly EmbeddingService _sut = new(new EmbeddingOptions(), NullLogger<EmbeddingService>.Instance);

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void DimensionsReturns384()
    {
        Assert.Equal(384, _sut.Dimensions);
    }

    [Fact]
    public void IsAvailableReturnsFalseWhenModelMissing()
    {
        Assert.False(_sut.IsAvailable);
    }

    [Fact]
    public void GenerateEmbeddingReturnsCorrectDimensions()
    {
        var embedding = _sut.GenerateEmbedding("test input");

        Assert.Equal(384, embedding.Length);
    }

    [Fact]
    public void GenerateEmbeddingsReturnsCorrectCount()
    {
        var texts = new[] { "text1", "text2", "text3" };
        var embeddings = _sut.GenerateEmbeddings(texts);

        Assert.Equal(3, embeddings.Length);
        Assert.All(embeddings, e => Assert.Equal(384, e.Length));
    }

    [Fact]
    public void GenerateEmbeddingEmptyInputHandlesGracefully()
    {
        var embedding = _sut.GenerateEmbedding(string.Empty);

        Assert.Equal(384, embedding.Length);
    }

    [Fact]
    public void GenerateEmbeddingStubModeReturnsZeroVector()
    {
        var embedding = _sut.GenerateEmbedding("test");

        Assert.All(embedding, v => Assert.Equal(0f, v));
    }

    [Fact]
    public void WordPieceTokenizerUsesCaseInsensitiveVocabularyLookup()
    {
        var vocabPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.vocab.txt");
        File.WriteAllLines(vocabPath, ["[PAD]", "[UNK]", "[CLS]", "[SEP]", "hello", "world"]);

        try
        {
            var tokenizer = new WordPieceTokenizer(vocabPath, maxSeqLength: 8);

            var result = tokenizer.Tokenize("HELLO World");

            Assert.Equal([2, 4, 5, 3], result.InputIds);
        }
        finally
        {
            File.Delete(vocabPath);
        }
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbeddingWithModelReturnsNonZeroVector()
    {
        // This test requires the ONNX model to be downloaded
        if (!_sut.IsAvailable)
            return; // Skip in CI

        var embedding = _sut.GenerateEmbedding("hello world");
        Assert.Contains(embedding, v => v != 0f);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbeddingSameInputReturnsDeterministicOutput()
    {
        if (!_sut.IsAvailable)
            return;

        var e1 = _sut.GenerateEmbedding("deterministic test");
        var e2 = _sut.GenerateEmbedding("deterministic test");
        Assert.Equal(e1, e2);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbeddingDifferentInputReturnsDifferentVectors()
    {
        if (!_sut.IsAvailable)
            return;

        var e1 = _sut.GenerateEmbedding("cats and dogs");
        var e2 = _sut.GenerateEmbedding("quantum physics");
        Assert.NotEqual(e1, e2);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbeddingLongInputTruncatesCorrectly()
    {
        if (!_sut.IsAvailable)
            return;

        var longText = string.Join(" ", Enumerable.Repeat("word", 500));
        var embedding = _sut.GenerateEmbedding(longText);
        Assert.Equal(384, embedding.Length);
        Assert.Contains(embedding, v => v != 0f);
    }
}
