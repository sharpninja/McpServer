using QBrainAi.Support.Mcp.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Indexing;

/// <summary>TR-PLANNED-CORE-013: Unit tests for EmbeddingService (stub mode and ONNX integration).</summary>
/// <remarks>
/// Requirement coverage named by requirement evidence: TR-MCP-QUALITY-001 ac-4.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-QUALITY-001 ac-4: cites the criterion whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TR-PLANNED-CORE-013; TR-MCP-QUALITY-001 ac-4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 4 Category=Integration cases (GenerateEmbedding_WithModel_ReturnsNonZeroVector, GenerateEmbedding_SameInput_ReturnsDeterministicOutput, GenerateEmbedding_DifferentInput_ReturnsDifferentVectors and GenerateEmbedding_LongInput_TruncatesCorrectly) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_sut and Dispose) moved once into the abstract base class EmbeddingServiceTestsBase in Indexing/EmbeddingServiceTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class EmbeddingServiceTests : EmbeddingServiceTestsBase, IDisposable
{
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

    [Fact]
    public void WordPieceTokenizer_UsesCaseInsensitiveVocabularyLookup()
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
}
