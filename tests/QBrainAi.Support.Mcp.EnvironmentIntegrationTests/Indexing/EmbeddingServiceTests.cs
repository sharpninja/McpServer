using QBrainAi.Support.Mcp.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Indexing;

/// <summary>TR-PLANNED-CORE-013: Unit tests for EmbeddingService (stub mode and ONNX integration).</summary>
/// <remarks>
/// Requirement coverage named by requirement evidence: TR-MCP-QUALITY-001 ac-4.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-QUALITY-001 ac-4: cites the criterion whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TR-PLANNED-CORE-013; TR-MCP-QUALITY-001 ac-4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Indexing/EmbeddingServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 4 Category=Integration cases moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class EmbeddingServiceTestsBase in Indexing/EmbeddingServiceTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</description></item>
/// </list>
/// </remarks>
public sealed class EmbeddingServiceTests : EmbeddingServiceTestsBase, IDisposable
{
    /// <summary>
    /// FR-MCP-004: when the ONNX model is loaded, the embedding of "hello world" holds at least one nonzero value; when
    /// the service reports the model unavailable the case returns without asserting. Data: the EmbeddingService that
    /// the linked base class EmbeddingServiceTestsBase builds from default EmbeddingOptions (no model path set).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-MCP-004: when the ONNX model is loaded, two embeddings of the same text "deterministic test" are equal; when
    /// the service reports the model unavailable the case returns without asserting. Data: the EmbeddingService that
    /// the linked base class EmbeddingServiceTestsBase builds from default EmbeddingOptions (no model path set).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-MCP-004: when the ONNX model is loaded, the embeddings of "cats and dogs" and "quantum physics" differ; when
    /// the service reports the model unavailable the case returns without asserting. Data: the EmbeddingService that
    /// the linked base class EmbeddingServiceTestsBase builds from default EmbeddingOptions (no model path set).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-MCP-004: when the ONNX model is loaded, a 500-word input still yields a 384-value embedding with at least one
    /// nonzero value; when the service reports the model unavailable the case returns without asserting. Data: the text
    /// "word" repeated 500 times and the EmbeddingService that the linked base class EmbeddingServiceTestsBase builds
    /// from default EmbeddingOptions (no model path set).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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
