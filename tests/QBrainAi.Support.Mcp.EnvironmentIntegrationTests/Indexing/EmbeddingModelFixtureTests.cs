using Microsoft.Extensions.Logging.Abstractions;
using QBrainAi.Support.Mcp.Indexing;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Indexing;

/// <summary>
/// TR-MCP-EMBEDDING-001 unavailable-is-failure: isolated negative controls proving missing and corrupt test-owned
/// artifacts cannot satisfy fixture availability.
/// </summary>
/// <remarks>
/// 2026-11-09, TR-MCP-EMBEDDING-001 unavailable-is-failure and no-user-cache-mutation: added before rebinding the
/// four original embedding cases. Each row supplies an explicit path under its own temporary root and sets
/// AutoDownload=false, so the product's default model cache is neither read nor written.
/// </remarks>
public sealed class EmbeddingModelFixtureTests
{
    /// <summary>
    /// Verifies that the fixture guard rejects both absent artifacts and an invalid ONNX file paired with a test
    /// vocabulary, using only the unique directory created by the row.
    /// </summary>
    /// <param name="fault">The test-owned artifact fault: missing or corrupt.</param>
    [Theory]
    [InlineData("missing")]
    [InlineData("corrupt")]
    [Trait("Category", "Integration")]
    public void RequireAvailable_RejectsMissingOrCorruptArtifacts(string fault)
    {
        var root = Path.Combine(Path.GetTempPath(), $"qbrainai-embedding-unavailable-{Guid.NewGuid():N}");
        var modelPath = Path.Combine(root, "model.onnx");
        try
        {
            if (fault == "corrupt")
            {
                Directory.CreateDirectory(root);
                File.WriteAllBytes(modelPath, [0x01, 0x02, 0x03, 0x04]);
                File.WriteAllLines(
                    Path.Combine(root, "vocab.txt"),
                    ["[PAD]", "[UNK]", "[CLS]", "[SEP]", "hello", "world"]);
            }
            else if (fault != "missing")
            {
                throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown embedding fixture fault.");
            }

            using var service = new EmbeddingService(
                new EmbeddingOptions
                {
                    Enabled = true,
                    ModelPath = modelPath,
                    AutoDownload = false,
                    Dimensions = 384,
                },
                NullLogger<EmbeddingService>.Instance);

            Assert.False(service.IsAvailable);
            Assert.Throws<InvalidOperationException>(() => EmbeddingModelFixture.RequireAvailable(service));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
