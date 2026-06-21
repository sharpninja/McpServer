using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-CORE-013: Splits content into fixed-size chunks for full-text and vector indexing.
/// </summary>
public sealed class Chunker
{
    private const int DefaultMaxTokensPerChunk = 512;
    private const int ApproxCharsPerToken = 4;

    private readonly int _maxTokensPerChunk;

    /// <summary>TR-PLANNED-CORE-013: Creates a chunker with optional max tokens per chunk.</summary>
    /// <param name="maxTokensPerChunk">Maximum estimated tokens per chunk (default 512).</param>
    public Chunker(int maxTokensPerChunk = DefaultMaxTokensPerChunk)
    {
        _maxTokensPerChunk = maxTokensPerChunk > 0 ? maxTokensPerChunk : DefaultMaxTokensPerChunk;
    }

    /// <summary>TR-PLANNED-CORE-013: Chunks text and returns chunk records with stable IDs.</summary>
    /// <param name="documentId">Parent document ID.</param>
    /// <param name="content">Full text to chunk.</param>
    /// <returns>Ordered list of context chunks.</returns>
    public IReadOnlyList<ContextChunk> Chunk(string documentId, string content)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<ContextChunk>();
        }

        var maxChars = _maxTokensPerChunk * ApproxCharsPerToken;
        var list = new List<ContextChunk>();
        var start = 0;
        var index = 0;

        while (start < content.Length)
        {
            var length = Math.Min(maxChars, content.Length - start);
            var chunkContent = content.Substring(start, length).Trim();
            if (chunkContent.Length == 0)
            {
                start += length;
                continue;
            }

            var chunkId = DeriveChunkId(documentId, index, chunkContent);
            var tokenCount = EstimateTokenCount(chunkContent);
            list.Add(new ContextChunk
            {
                Id = chunkId,
                DocumentId = documentId,
                Content = chunkContent,
                TokenCount = tokenCount,
                ChunkIndex = index
            });
            index++;
            start += length;
        }

        return list;
    }

    /// <summary>TR-PLANNED-CORE-013: Estimates token count (approx 4 chars per token).</summary>
    /// <param name="text">The text to estimate tokens for.</param>
    /// <returns>Estimated token count.</returns>
    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling((double)text.Length / ApproxCharsPerToken);
    }

    private static string DeriveChunkId(string documentId, int chunkIndex, string content)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(documentId + ":" + chunkIndex + ":" + content)));
        return documentId + "-chunk-" + chunkIndex + "-" + hash.AsSpan(0, 8).ToString();
    }
}
