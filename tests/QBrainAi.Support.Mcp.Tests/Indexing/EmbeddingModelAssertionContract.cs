using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Indexing;

/// <summary>
/// FR-MCP-004 embedding-vector-contract; TEST-MCP-EMBEDDING-001: shared assertions for the four source-pinned
/// embedding outcomes. The contract consumes only supplied availability and vectors.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-004 embedding-vector-contract; TEST-MCP-EMBEDDING-001; TR-MCP-EMBEDDING-001
/// model-backed-assertions: added for the PR87 embedding test-only slice so the pure discriminator and the real
/// integration binding execute one assertion definition without disk, environment, network, native inference or
/// clock access.
/// </remarks>
internal static class EmbeddingModelAssertionContract
{
    /// <summary>Asserts that an available model produced a vector containing at least one nonzero value.</summary>
    /// <param name="isAvailable">Whether the service reports that its model is available.</param>
    /// <param name="embedding">The supplied embedding.</param>
    internal static void AssertNonZeroVector(bool isAvailable, float[] embedding)
    {
        AssertModelAvailable(isAvailable);
        Assert.NotNull(embedding);
        Assert.Contains(embedding, value => value != 0f);
    }

    /// <summary>Asserts that an available model produced equal vectors for the same input.</summary>
    /// <param name="isAvailable">Whether the service reports that its model is available.</param>
    /// <param name="first">The first supplied embedding.</param>
    /// <param name="second">The second supplied embedding.</param>
    internal static void AssertDeterministicOutput(bool isAvailable, float[] first, float[] second)
    {
        AssertModelAvailable(isAvailable);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first, second);
    }

    /// <summary>Asserts that an available model produced distinct vectors for the two different inputs.</summary>
    /// <param name="isAvailable">Whether the service reports that its model is available.</param>
    /// <param name="first">The first supplied embedding.</param>
    /// <param name="second">The second supplied embedding.</param>
    internal static void AssertDistinctVectors(bool isAvailable, float[] first, float[] second)
    {
        AssertModelAvailable(isAvailable);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
    }

    /// <summary>Asserts that an available model produced a nonzero 384-value vector for the long input.</summary>
    /// <param name="isAvailable">Whether the service reports that its model is available.</param>
    /// <param name="embedding">The supplied long-input embedding.</param>
    internal static void AssertLongInputVector(bool isAvailable, float[] embedding)
    {
        AssertModelAvailable(isAvailable);
        Assert.NotNull(embedding);
        Assert.Equal(384, embedding.Length);
        Assert.Contains(embedding, value => value != 0f);
    }

    /// <summary>
    /// TR-MCP-EMBEDDING-001 unavailable-is-failure: requires model availability before any vector assertion.
    /// </summary>
    /// <param name="isAvailable">Whether the service reports that its model is available.</param>
    private static void AssertModelAvailable(bool isAvailable) =>
        Assert.True(isAvailable, "The embedding model must be available; unavailable fixtures cannot pass.");
}
