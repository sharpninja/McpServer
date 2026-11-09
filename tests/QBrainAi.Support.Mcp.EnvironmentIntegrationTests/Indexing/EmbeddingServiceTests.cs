using QBrainAi.Support.Mcp.Indexing;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Indexing;

/// <summary>FR-MCP-004: model-backed integration assertions for <see cref="EmbeddingService"/>.</summary>
/// <remarks>
/// Requirement coverage: FR-MCP-004 embedding-vector-contract; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-QUALITY-001 ac-4: cites the criterion whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TR-PLANNED-CORE-013; TR-MCP-QUALITY-001 ac-4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Indexing/EmbeddingServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 4 Category=Integration cases moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class EmbeddingServiceTestsBase in Indexing/EmbeddingServiceTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</description></item>
/// <item><term>2026-11-09</term><description>FR-MCP-004 embedding-vector-contract; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001: replaces the unavailable-model early returns with a test-owned fixture that must load the product model and vocabulary sources, and routes all four outcomes through the assertion contract exercised by positive, wrong-response and unavailable controls.</description></item>
/// </list>
/// </remarks>
public sealed class EmbeddingServiceTests : IClassFixture<EmbeddingModelFixture>
{
    private readonly EmbeddingModelFixture _fixture;

    /// <summary>Creates a model-backed test instance and writes immutable fixture artifact evidence to test output.</summary>
    /// <param name="fixture">The required available service and test-owned artifacts.</param>
    /// <param name="output">The xUnit output sink captured by the test result.</param>
    public EmbeddingServiceTests(EmbeddingModelFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        foreach (var artifact in fixture.Artifacts)
            output.WriteLine($"EMBEDDING_ARTIFACT source={artifact.Source} length={artifact.Length} sha256={artifact.Sha256}");
    }

    /// <summary>
    /// FR-MCP-004: the embedding of "hello world" from the required test-owned ONNX model has 384 values and at least
    /// one nonzero value. Data: <see cref="EmbeddingModelFixture"/> downloads the product model and vocabulary sources.
    /// </summary>
    /// <remarks>2026-11-09, FR-MCP-004; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001: unavailability is a fixture failure, not a successful early return.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_WithModel_ReturnsNonZeroVector()
    {
        var embedding = _fixture.Service.GenerateEmbedding("hello world");
        EmbeddingModelAssertionContract.AssertNonZeroVector(isAvailable: true, embedding);
    }

    /// <summary>
    /// FR-MCP-004: two embeddings of the same text "deterministic test" from the required test-owned model are equal.
    /// Data: <see cref="EmbeddingModelFixture"/> downloads the product model and vocabulary sources.
    /// </summary>
    /// <remarks>2026-11-09, FR-MCP-004; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001: the shared contract also rejects unavailable controls.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_SameInput_ReturnsDeterministicOutput()
    {
        var first = _fixture.Service.GenerateEmbedding("deterministic test");
        var second = _fixture.Service.GenerateEmbedding("deterministic test");
        EmbeddingModelAssertionContract.AssertDeterministicOutput(isAvailable: true, first, second);
    }

    /// <summary>
    /// FR-MCP-004: embeddings of "cats and dogs" and "quantum physics" from the required test-owned model differ.
    /// Data: <see cref="EmbeddingModelFixture"/> downloads the product model and vocabulary sources.
    /// </summary>
    /// <remarks>2026-11-09, FR-MCP-004; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001: the shared contract also rejects equal wrong-response controls.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_DifferentInput_ReturnsDifferentVectors()
    {
        var first = _fixture.Service.GenerateEmbedding("cats and dogs");
        var second = _fixture.Service.GenerateEmbedding("quantum physics");
        EmbeddingModelAssertionContract.AssertDistinctVectors(isAvailable: true, first, second);
    }

    /// <summary>
    /// FR-MCP-004: a 500-word input to the required test-owned model yields exactly 384 values with at least one nonzero
    /// value. Data: the word "word" repeated 500 times and <see cref="EmbeddingModelFixture"/>.
    /// </summary>
    /// <remarks>2026-11-09, FR-MCP-004; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001: the shared contract independently rejects wrong length and all-zero controls.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void GenerateEmbedding_LongInput_TruncatesCorrectly()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 500));
        var embedding = _fixture.Service.GenerateEmbedding(longText);
        EmbeddingModelAssertionContract.AssertLongInputVector(isAvailable: true, embedding);
    }
}
