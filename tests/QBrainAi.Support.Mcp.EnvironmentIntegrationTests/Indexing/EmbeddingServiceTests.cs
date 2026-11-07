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
