using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Ingests repository files under allowlist; computes hashes and chunks.
/// FR-SUPPORT-010: Path allowlist and repo root only.
/// </summary>
public sealed class RepoIngestor
{
    private static readonly char[] TrimSlashChars = { '/' };
    private readonly Chunker _chunker;
    private readonly IngestionOptions _options;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="chunker">Chunker for splitting content.</param>
    /// <param name="options">Ingestion options providing repo root and allowlist.</param>
    public RepoIngestor(Chunker chunker, Microsoft.Extensions.Options.IOptions<IngestionOptions> options)
    {
        _chunker = chunker;
        _options = options?.Value ?? new IngestionOptions();
    }

    /// <summary>TR-PLANNED-013: Ingests allowlisted files under RepoRoot; returns documents and chunks.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of ingested documents with their chunks.</returns>
    public async Task<IReadOnlyList<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>> IngestAsync(
        CancellationToken cancellationToken = default)
    {
        var repoRoot = Path.GetFullPath(_options.RepoRoot);
        if (!Directory.Exists(repoRoot))
        {
            return Array.Empty<(ContextDocument, IReadOnlyList<ContextChunk>)>();
        }

        var results = new List<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>();
        var allowlist = _options.RepoAllowlist;

        await foreach (var path in EnumerateAllowlistedFilesAsync(repoRoot, allowlist, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                if (content.Length > _options.MaxFileSizeBytes)
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                if (IsPathTraversal(relativePath))
                {
                    continue;
                }

                var contentHash = ComputeHash(content);
                var documentId = "repo:" + relativePath.Replace("/", "-", StringComparison.Ordinal).Replace(":", "-", StringComparison.Ordinal);
                var doc = new ContextDocument
                {
                    Id = documentId,
                    SourceType = "repo",
                    SourceKey = relativePath,
                    IngestedAt = DateTime.UtcNow,
                    ContentHash = contentHash
                };
                var chunks = _chunker.Chunk(documentId, content);
                results.Add((doc, chunks));
            }
            catch (IOException)
            {
                // Skip unreadable files
            }
            catch (UnauthorizedAccessException)
            {
                // Skip inaccessible files
            }
        }

        return results;
    }

    private static async IAsyncEnumerable<string> EnumerateAllowlistedFilesAsync(
        string repoRoot,
        IReadOnlyList<string>? allowlist,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var file in EnumerateAllFilesAsync(repoRoot, cancellationToken).ConfigureAwait(false))
        {
            var relative = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            if (allowlist != null && allowlist.Count > 0 && !MatchesAllowlist(relative, allowlist))
            {
                continue;
            }
            if (seen.Add(relative))
            {
                yield return file;
            }
        }
    }

    private static async IAsyncEnumerable<string> EnumerateAllFilesAsync(
        string dir,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var path in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(path);
            if (name.StartsWith('.') || name == "mcp.db" ||
                path.Contains("bin", StringComparison.Ordinal) || path.Contains("obj", StringComparison.Ordinal))
            {
                continue;
            }
            yield return path;
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static bool MatchesAllowlist(string relativePath, IReadOnlyList<string> patterns)
    {
        foreach (var p in patterns)
        {
            if (p.Contains("**", StringComparison.Ordinal))
            {
                var prefix = p.Replace("**", string.Empty, StringComparison.Ordinal).TrimEnd(TrimSlashChars);
                if (relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (p.StartsWith("*.", StringComparison.Ordinal))
            {
                var suffix = p[1..];
                if (relativePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (relativePath.StartsWith(p.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsPathTraversal(string relativePath)
    {
        return relativePath.Contains("..", StringComparison.Ordinal) ||
               Path.IsPathRooted(relativePath);
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }
}
