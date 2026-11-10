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
/// <item><term>2026-11-07</term><description>FR-MCP-004; TR-MCP-NUKE-019 AC-7: the unit cases no longer depend on the host. Built from default options, the service looked for model.onnx and vocab.txt under the user's local application data, so IsAvailable_ReturnsFalseWhenModelMissing and the stub-mode cases passed only on a host without the model there (MEMORY-BESTPRACTICE-001 rule 4.3: a test that depends on the environment is never a unit test). The new constructor builds the service over a model path in a fresh temporary folder that is never created, so every unit case runs in stub mode on every host; the integration half keeps the default path, where it uses the model when present. The case code is unchanged (ClaudeCode review of slice S9).</description></item>
/// </list>
/// </remarks>
public sealed class EmbeddingServiceTests : EmbeddingServiceTestsBase, IDisposable
{
    /// <summary>
    /// FR-MCP-004; TR-MCP-NUKE-019 AC-7: builds the service under test over a model path in a fresh temporary folder that
    /// holds no model or vocabulary file (the folder is never created), so the unit cases do not depend on whether this
    /// host has the ONNX model at the default path.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: added so the unit half is hermetic (ClaudeCode review of slice S9).</remarks>
    public EmbeddingServiceTests()
        : base(new EmbeddingOptions
        {
            ModelPath = Path.Combine(Path.GetTempPath(), "qbrainai-embedding-absent-" + Guid.NewGuid().ToString("N"), "model.onnx"),
        })
    {
    }

    /// <summary>
    /// FR-MCP-004: the embedding service reports 384 dimensions. Data: the EmbeddingService this class builds over a
    /// model path in a fresh temporary folder that holds no model.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: the data now names the hermetic service the class constructor builds (ClaudeCode review of slice S9).</para>
    /// </remarks>
    [Fact]
    public void Dimensions_Returns384()
    {
        Assert.Equal(384, _sut.Dimensions);
    }

    /// <summary>
    /// FR-MCP-004: an embedding service whose model and vocabulary files are missing reports IsAvailable false, so it
    /// runs in stub mode. Data: the EmbeddingService this class builds over a model path in a fresh temporary folder
    /// that holds no model or vocabulary file.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: the data now names the hermetic service the class constructor builds; before, this case passed only on a host without the model at the default path (ClaudeCode review of slice S9).</para>
    /// </remarks>
    [Fact]
    public void IsAvailable_ReturnsFalseWhenModelMissing()
    {
        Assert.False(_sut.IsAvailable);
    }

    /// <summary>
    /// FR-MCP-004: GenerateEmbedding for "test input" returns 384 values. Data: the EmbeddingService this class builds
    /// over a model path in a fresh temporary folder that holds no model.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: the data now names the hermetic service the class constructor builds (ClaudeCode review of slice S9).</para>
    /// </remarks>
    [Fact]
    public void GenerateEmbedding_ReturnsCorrectDimensions()
    {
        var embedding = _sut.GenerateEmbedding("test input");

        Assert.Equal(384, embedding.Length);
    }

    /// <summary>
    /// FR-MCP-004: GenerateEmbeddings for three texts returns three embeddings of 384 values each. Data: the texts
    /// text1, text2 and text3 and the EmbeddingService this class builds over a model path in a fresh temporary folder
    /// that holds no model.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: the data now names the hermetic service the class constructor builds (ClaudeCode review of slice S9).</para>
    /// </remarks>
    [Fact]
    public void GenerateEmbeddings_ReturnsCorrectCount()
    {
        var texts = new[] { "text1", "text2", "text3" };
        var embeddings = _sut.GenerateEmbeddings(texts);

        Assert.Equal(3, embeddings.Length);
        Assert.All(embeddings, e => Assert.Equal(384, e.Length));
    }

    /// <summary>
    /// FR-MCP-004: GenerateEmbedding for an empty string returns 384 values without throwing. Data: the
    /// EmbeddingService this class builds over a model path in a fresh temporary folder that holds no model.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: the data now names the hermetic service the class constructor builds (ClaudeCode review of slice S9).</para>
    /// </remarks>
    [Fact]
    public void GenerateEmbedding_EmptyInput_HandlesGracefully()
    {
        var embedding = _sut.GenerateEmbedding(string.Empty);

        Assert.Equal(384, embedding.Length);
    }

    /// <summary>
    /// FR-MCP-004: in stub mode GenerateEmbedding for "test" returns only zero values. Data: the EmbeddingService this
    /// class builds over a model path in a fresh temporary folder that holds no model or vocabulary file, so the
    /// service runs in stub mode on every host.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: the data now names the hermetic service the class constructor builds; before, this case passed only on a host without the model at the default path (ClaudeCode review of slice S9).</para>
    /// </remarks>
    [Fact]
    public void GenerateEmbedding_StubMode_ReturnsZeroVector()
    {
        var embedding = _sut.GenerateEmbedding("test");

        Assert.All(embedding, v => Assert.Equal(0f, v));
    }

    /// <summary>
    /// TR-MCP-QUALITY-001 ac-4; FR-MCP-004: WordPieceTokenizer looks up vocabulary entries ignoring case, so "HELLO
    /// World" tokenizes to the ids 2, 4, 5 and 3 ([CLS], hello, world, [SEP]). Data: a six-line vocabulary file ([PAD],
    /// [UNK], [CLS], [SEP], hello, world) written to the temporary folder and deleted afterwards, and a maximum
    /// sequence length of 8.
    /// </summary>
    /// <remarks>2026-11-07, TR-MCP-QUALITY-001 ac-4; FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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
