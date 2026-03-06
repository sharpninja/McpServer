using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Ingests external docs from cached path (e.g. docs/external).
/// FR-SUPPORT-010: Only cached copies under ExternalDocsPath are indexed.
/// </summary>
public sealed class ExternalDocsIngestor
{
    private readonly Chunker _chunker;
    private readonly IngestionOptions _options;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<ExternalDocsIngestor> _logger;


    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="chunker">Chunker for splitting content.</param>
    /// <param name="options">Ingestion options providing external docs path.</param>
    /// <param name="workspaceContext">Resolved workspace context for per-workspace ingestion.</param>
    /// <param name="logger">Logger instance.</param>
    public ExternalDocsIngestor(Chunker chunker, Microsoft.Extensions.Options.IOptions<IngestionOptions> options,
        WorkspaceContext workspaceContext,
        ILogger<ExternalDocsIngestor> logger)
    {
        _logger = logger;
        _chunker = chunker;
        _options = options?.Value ?? new IngestionOptions();
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
    }

    /// <summary>FR-SUPPORT-010: Ingests all files under ExternalDocsPath; returns documents and chunks.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of ingested documents with their chunks.</returns>
    public async Task<IReadOnlyList<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>> IngestAsync(
        CancellationToken cancellationToken = default)
    {
        var repoRoot = ResolveRepoRoot();
        var externalPath = ResolveExternalDocsDirectory(repoRoot);
        var sourceRoot = IsUnderPath(externalPath, repoRoot) ? repoRoot : externalPath;
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

                var relativePath = Path.GetRelativePath(sourceRoot, path).Replace('\\', '/');
                if (relativePath.Contains("..", StringComparison.Ordinal)) continue;

                var contentHash = ComputeHash(content);
                var documentId = BuildWorkspaceScopedDocumentId("external-doc", repoRoot, relativePath);
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
            catch (IOException ex)
            {
                _logger.LogWarning("{ExceptionDetail}", ex.ToString());
                // Skip unreadable
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("{ExceptionDetail}", ex.ToString());
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

    private string ResolveRepoRoot()
    {
        var candidate = _workspaceContext.WorkspacePath;
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = _options.RepoRoot;
        return Path.GetFullPath(candidate);
    }

    private string ResolveExternalDocsDirectory(string repoRoot)
    {
        var externalDocsPath = !string.IsNullOrWhiteSpace(_workspaceContext.ExternalDocsPath)
            ? _workspaceContext.ExternalDocsPath!
            : _options.ExternalDocsPath;

        return Path.IsPathRooted(externalDocsPath)
            ? Path.GetFullPath(externalDocsPath)
            : Path.GetFullPath(Path.Combine(repoRoot, externalDocsPath.TrimStart('.', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
    }

    private static bool IsUnderPath(string candidatePath, string rootPath)
    {
        var normalizedCandidate = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (normalizedCandidate.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return true;
        var rootWithSep = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildWorkspaceScopedDocumentId(string sourcePrefix, string workspaceRoot, string relativePath)
    {
        var scope = ComputeHash(workspaceRoot).Substring(0, 16).ToLowerInvariant();
        var normalizedPath = relativePath.Replace("/", "-", StringComparison.Ordinal).Replace(":", "-", StringComparison.Ordinal);
        return $"{sourcePrefix}:{scope}:{normalizedPath}";
    }
}
