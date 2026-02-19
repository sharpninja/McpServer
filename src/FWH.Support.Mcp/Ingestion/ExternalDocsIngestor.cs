using System.Security.Cryptography;
using System.Text;
using FWH.Support.Mcp.Indexing;
using FWH.Support.Mcp.Models;

namespace FWH.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Ingests external docs from cached path (e.g. docs/external).
/// FR-SUPPORT-010: Only cached copies under ExternalDocsPath are indexed.
/// </summary>
public sealed class ExternalDocsIngestor
{
    private readonly Chunker _chunker;
    private readonly IngestionOptions _options;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="chunker">Chunker for splitting content.</param>
    /// <param name="options">Ingestion options providing external docs path.</param>
    public ExternalDocsIngestor(Chunker chunker, Microsoft.Extensions.Options.IOptions<IngestionOptions> options)
    {
        _chunker = chunker;
        _options = options?.Value ?? new IngestionOptions();
    }

    /// <summary>FR-SUPPORT-010: Ingests all files under ExternalDocsPath; returns documents and chunks.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of ingested documents with their chunks.</returns>
    public async Task<IReadOnlyList<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>> IngestAsync(
        CancellationToken cancellationToken = default)
    {
        var repoRoot = Path.GetFullPath(_options.RepoRoot);
        var externalPath = Path.Combine(repoRoot, _options.ExternalDocsPath.TrimStart('.', Path.DirectorySeparatorChar));
        if (!Directory.Exists(externalPath))
        {
            return Array.Empty<(ContextDocument, IReadOnlyList<ContextChunk>)>();
        }

        var results = new List<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>();

        foreach (var path in Directory.EnumerateFiles(externalPath, "*.*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                if (content.Length > _options.MaxFileSizeBytes) continue;

                var relativePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                if (relativePath.Contains("..", StringComparison.Ordinal)) continue;

                var contentHash = ComputeHash(content);
                var documentId = "external-doc:" + relativePath.Replace("/", "-", StringComparison.Ordinal).Replace(":", "-", StringComparison.Ordinal);
                var doc = new ContextDocument
                {
                    Id = documentId,
                    SourceType = "external-doc",
                    SourceKey = relativePath,
                    IngestedAt = DateTime.UtcNow,
                    ContentHash = contentHash
                };
                var chunks = _chunker.Chunk(documentId, content);
                results.Add((doc, chunks));
            }
            catch (IOException)
            {
                // Skip unreadable
            }
            catch (UnauthorizedAccessException)
            {
                // Skip inaccessible
            }
        }

        return results;
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }
}
