using Xunit;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Indexing;

/// <summary>
/// TEST-MCP-EMBEDDING-001: pure mock gate for <see cref="EmbeddingModelAssertionContract"/> using four correct,
/// four independently wrong and four unavailable supplied responses.
/// </summary>
/// <remarks>
/// 2026-11-09, FR-MCP-004 embedding-vector-contract; TEST-MCP-EMBEDDING-001 positive-fixtures,
/// wrong-response-controls and unavailable-controls: added twelve named theory rows before changing the real
/// EmbeddingServiceTests fixture or bindings. The rows use supplied arrays only.
/// </remarks>
public sealed class EmbeddingModelAssertionContractTests
{
    /// <summary>
    /// Verifies that every correct response passes and that every wrong or unavailable response is rejected by the
    /// assertion for the corresponding original embedding behavior.
    /// </summary>
    /// <param name="behavior">The original embedding behavior being asserted.</param>
    /// <param name="response">The supplied response class: correct, wrong or unavailable.</param>
    [Theory]
    [InlineData("nonzero", "correct")]
    [InlineData("same-input", "correct")]
    [InlineData("different-input", "correct")]
    [InlineData("long-input", "correct")]
    [InlineData("nonzero", "wrong")]
    [InlineData("same-input", "wrong")]
    [InlineData("different-input", "wrong")]
    [InlineData("long-input", "wrong")]
    [InlineData("nonzero", "unavailable")]
    [InlineData("same-input", "unavailable")]
    [InlineData("different-input", "unavailable")]
    [InlineData("long-input", "unavailable")]
    public void SuppliedResponses_AreDiscriminated(string behavior, string response)
    {
        if (behavior == "long-input" && response == "wrong")
        {
            var wrongSizedNonzero = Vector(383, nonzeroIndex: 11);
            var correctlySizedZero = Vector(384);

            Assert.ThrowsAny<XunitException>(
                () => EmbeddingModelAssertionContract.AssertLongInputVector(isAvailable: true, wrongSizedNonzero));
            Assert.ThrowsAny<XunitException>(
                () => EmbeddingModelAssertionContract.AssertLongInputVector(isAvailable: true, correctlySizedZero));
            return;
        }

        var action = CreateAssertion(behavior, response);
        if (response == "correct")
        {
            action();
            return;
        }

        Assert.ThrowsAny<XunitException>(action);
    }

    /// <summary>Creates the assertion action for one supplied-response row.</summary>
    /// <param name="behavior">The original embedding behavior.</param>
    /// <param name="response">The supplied response class.</param>
    /// <returns>The assertion action.</returns>
    private static Action CreateAssertion(string behavior, string response)
    {
        var available = response != "unavailable";
        var wrong = response == "wrong";

        return behavior switch
        {
            "nonzero" => () => EmbeddingModelAssertionContract.AssertNonZeroVector(
                available,
                wrong ? Vector(8) : Vector(8, nonzeroIndex: 3)),
            "same-input" => () => EmbeddingModelAssertionContract.AssertDeterministicOutput(
                available,
                [0.5f, -0.25f, 0.75f],
                wrong ? [0.5f, -0.25f, 0.5f] : [0.5f, -0.25f, 0.75f]),
            "different-input" => () => EmbeddingModelAssertionContract.AssertDistinctVectors(
                available,
                [0.1f, 0.9f],
                wrong ? [0.1f, 0.9f] : [0.9f, 0.1f]),
            "long-input" => () => EmbeddingModelAssertionContract.AssertLongInputVector(
                available,
                Vector(384, nonzeroIndex: 127)),
            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "Unknown embedding behavior."),
        };
    }

    /// <summary>Creates a supplied vector with one optional nonzero value.</summary>
    /// <param name="length">The vector length.</param>
    /// <param name="nonzeroIndex">The optional index to set to one.</param>
    /// <returns>The supplied vector.</returns>
    private static float[] Vector(int length, int? nonzeroIndex = null)
    {
        var vector = new float[length];
        if (nonzeroIndex is not null)
            vector[nonzeroIndex.Value] = 1f;
        return vector;
    }
}
