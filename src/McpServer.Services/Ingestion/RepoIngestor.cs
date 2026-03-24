using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Ingests repository files under allowlist; computes hashes and chunks.
/// FR-SUPPORT-010: Path allowlist and repo root only.
/// </summary>
public sealed class RepoIngestor
{
    private readonly Chunker _chunker;
    private readonly IngestionOptions _options;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<RepoIngestor> _logger;


    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="chunker">Chunker for splitting content.</param>
    /// <param name="options">Ingestion options providing repo root and allowlist.</param>
    /// <param name="workspaceContext">Resolved workspace context for per-workspace ingestion.</param>
    /// <param name="logger">Logger instance.</param>
    public RepoIngestor(Chunker chunker, Microsoft.Extensions.Options.IOptions<IngestionOptions> options,
        WorkspaceContext workspaceContext,
        ILogger<RepoIngestor> logger)
    {
        _logger = logger;
        _chunker = chunker;
        _options = options?.Value ?? new IngestionOptions();
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>TR-PLANNED-013: Ingests allowlisted files under RepoRoot; returns documents and chunks.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of ingested documents with their chunks.</returns>
    public async Task<IReadOnlyList<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>> IngestAsync(
        CancellationToken cancellationToken = default)
    {
        var repoRoot = ResolveRepoRoot();
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
                var documentId = BuildWorkspaceScopedDocumentId("repo", repoRoot, relativePath);
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
            catch (IOException ex)
            {
                _logger.LogError("{ExceptionDetail}", ex.ToString());
                // Skip unreadable files
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            var relativePath = Path.GetRelativePath(dir, path);
            if (name.StartsWith('.') || name == "mcp.db" ||
                IsBuildArtifactPath(relativePath))
            {
                continue;
            }
            yield return path;
        }
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static bool IsBuildArtifactPath(string relativePath)
    {
        var segments = relativePath
            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Any(static segment =>
            segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesAllowlist(string relativePath, IReadOnlyList<string> patterns)
    {
        var normalizedPath = relativePath.Replace('\\', '/');
        foreach (var p in patterns)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }

            var pattern = p.Replace('\\', '/').TrimStart('/');
            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                var suffix = pattern[1..];
                if (normalizedPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (GlobMatches(normalizedPath, pattern))
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

    private static bool GlobMatches(string path, string pattern)
    {
        var regexPattern = GlobToRegex(pattern);
        return Regex.IsMatch(
            path,
            regexPattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string GlobToRegex(string pattern)
    {
        var sb = new StringBuilder("^");
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '*')
            {
                var isDoubleStar = i + 1 < pattern.Length && pattern[i + 1] == '*';
                if (isDoubleStar)
                {
                    var followedBySlash = i + 2 < pattern.Length &&
                        (pattern[i + 2] == '/' || pattern[i + 2] == '\\');
                    if (followedBySlash)
                    {
                        sb.Append("(?:.*/)?");
                        i += 2;
                    }
                    else
                    {
                        sb.Append(".*");
                        i += 1;
                    }
                }
                else
                {
                    sb.Append("[^/]*");
                }
            }
            else if (c == '?')
            {
                sb.Append("[^/]");
            }
            else if (c == '/' || c == '\\')
            {
                sb.Append('/');
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }

        sb.Append('$');
        return sb.ToString();
    }

    private string ResolveRepoRoot()
    {
        var candidate = _workspaceContext.WorkspacePath;
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = _options.RepoRoot;
        return Path.GetFullPath(candidate);
    }

    private static string BuildWorkspaceScopedDocumentId(string sourcePrefix, string workspaceRoot, string relativePath)
    {
        var scope = ComputeHash(workspaceRoot).Substring(0, 16).ToLowerInvariant();
        var normalizedPath = relativePath.Replace("/", "-", StringComparison.Ordinal).Replace(":", "-", StringComparison.Ordinal);
        return $"{sourcePrefix}:{scope}:{normalizedPath}";
    }
}
