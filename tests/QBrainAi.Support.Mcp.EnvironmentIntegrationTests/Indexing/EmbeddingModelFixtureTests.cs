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
/// 2026-11-09, TR-MCP-EMBEDDING-001 test-owned-fixture: added a deterministic transient-lock control after the first
/// Windows full-lane gate exposed delayed release of the fixture-owned ONNX file during class cleanup.
/// 2026-11-09, TR-MCP-EMBEDDING-001 test-owned-fixture; R15-F4: adds persistent-IO and non-IO controls so the bounded
/// cleanup policy proves its final failure is surfaced and non-transient failures are never retried.
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

    /// <summary>
    /// Verifies that fixture cleanup retries a transient file lock and then removes only its supplied owned root.
    /// Data: a unique empty root and a delete delegate that throws <see cref="IOException"/> twice before deleting it.
    /// </summary>
    /// <remarks>
    /// 2026-11-09, TR-MCP-EMBEDDING-001 test-owned-fixture and no-user-cache-mutation: the Windows negative control for
    /// the class-cleanup failure observed in full-lane run ug-r15p3-final-win-lane-1.
    /// </remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteOwnedRootAsync_RetriesTransientFileLockBeforeDeletingRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"qbrainai-embedding-delete-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var attempts = 0;

        await EmbeddingModelFixture.DeleteOwnedRootAsync(
            root,
            (path, recursive) =>
            {
                attempts++;
                if (attempts < 3)
                    throw new IOException("Simulated delayed ONNX file-handle release.");
                Directory.Delete(path, recursive);
            },
            _ => ValueTask.CompletedTask);

        Assert.Equal(3, attempts);
        Assert.False(Directory.Exists(root));
    }

    /// <summary>Verifies that the twentieth persistent <see cref="IOException"/> is surfaced after exactly twenty attempts.</summary>
    /// <remarks>2026-11-09, TR-MCP-EMBEDDING-001 test-owned-fixture; R15-F4: required bounded-final-failure negative control.</remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteOwnedRootAsync_PersistentIOException_ThrowsAfterBoundedAttempts()
    {
        var root = Path.Combine(Path.GetTempPath(), $"qbrainai-embedding-delete-persistent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var attempts = 0;
        try
        {
            var failure = await Assert.ThrowsAsync<IOException>(() => EmbeddingModelFixture.DeleteOwnedRootAsync(
                root,
                (_, _) =>
                {
                    attempts++;
                    throw new IOException("Persistent simulated ONNX file-handle lock.");
                },
                _ => ValueTask.CompletedTask).AsTask());

            Assert.Contains("Persistent simulated", failure.Message, StringComparison.Ordinal);
            Assert.Equal(20, attempts);
            Assert.True(Directory.Exists(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Verifies that a non-IO cleanup failure is surfaced immediately without a retry delay.</summary>
    /// <remarks>2026-11-09, TR-MCP-EMBEDDING-001 test-owned-fixture; R15-F4: required non-transient-failure negative control.</remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteOwnedRootAsync_NonIOException_IsNotRetried()
    {
        var root = Path.Combine(Path.GetTempPath(), $"qbrainai-embedding-delete-nonio-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var attempts = 0;
        var delays = 0;
        try
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => EmbeddingModelFixture.DeleteOwnedRootAsync(
                root,
                (_, _) =>
                {
                    attempts++;
                    throw new InvalidOperationException("Non-transient simulated cleanup failure.");
                },
                _ =>
                {
                    delays++;
                    return ValueTask.CompletedTask;
                }).AsTask());

            Assert.Contains("Non-transient simulated", failure.Message, StringComparison.Ordinal);
            Assert.Equal(1, attempts);
            Assert.Equal(0, delays);
            Assert.True(Directory.Exists(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
