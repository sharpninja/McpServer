using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Ingests GitHub issues and PRs via IGitHubCliService into ContextDocument/Chunk pairs.
/// FR-SUPPORT-010: Indexes GitHub context for hybrid search.
/// </summary>
public sealed class GitHubIngestor
{
    private readonly Chunker _chunker;
    private readonly IGitHubCliService _github;
    private readonly ILogger<GitHubIngestor> _logger;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public GitHubIngestor(Chunker chunker, IGitHubCliService github, ILogger<GitHubIngestor> logger)
    {
        _chunker = chunker;
        _github = github;
        _logger = logger;
    }

    /// <summary>TR-PLANNED-013: Ingest all issues and PRs into document/chunk pairs.</summary>
    public async Task<IReadOnlyList<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>> IngestAsync(CancellationToken ct = default)
    {
        var results = new List<(ContextDocument, IReadOnlyList<ContextChunk>)>();

        try
        {
            var issuesResult = await _github.ListIssuesAsync("all", 100, ct).ConfigureAwait(false);
            if (issuesResult.Success)
            {
                foreach (var issue in issuesResult.Issues)
                {
                    var content = FormatIssue(issue);
                    var sourceKey = $"issue/{issue.Number}";
                    var docId = DeriveDocumentId(sourceKey);
                    var hash = ComputeHash(content);
                    var doc = new ContextDocument
                    {
                        Id = docId,
                        SourceType = "github-issue",
                        SourceKey = sourceKey,
                        IngestedAt = DateTime.UtcNow,
                        ContentHash = hash
                    };
                    var chunks = _chunker.Chunk(docId, content);
                    results.Add((doc, chunks));
                }
                _logger.LogInformation("GitHubIngestor: ingested {Count} issues", issuesResult.Issues.Count);
            }
            else
            {
                _logger.LogWarning("GitHubIngestor: failed to list issues: {Error}", issuesResult.Error);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "GitHubIngestor: error listing issues");
        }

        try
        {
            var pullsResult = await _github.ListPullsAsync("all", 100, ct).ConfigureAwait(false);
            if (pullsResult.Success)
            {
                foreach (var pr in pullsResult.Pulls)
                {
                    var content = FormatPull(pr);
                    var sourceKey = $"pr/{pr.Number}";
                    var docId = DeriveDocumentId(sourceKey);
                    var hash = ComputeHash(content);
                    var doc = new ContextDocument
                    {
                        Id = docId,
                        SourceType = "github-pr",
                        SourceKey = sourceKey,
                        IngestedAt = DateTime.UtcNow,
                        ContentHash = hash
                    };
                    var chunks = _chunker.Chunk(docId, content);
                    results.Add((doc, chunks));
                }
                _logger.LogInformation("GitHubIngestor: ingested {Count} PRs", pullsResult.Pulls.Count);
            }
            else
            {
                _logger.LogWarning("GitHubIngestor: failed to list PRs: {Error}", pullsResult.Error);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "GitHubIngestor: error listing PRs");
        }

        return results;
    }

    private static string FormatIssue(GitHubIssueItem issue) =>
        $"# Issue #{issue.Number}: {issue.Title}\nState: {issue.State ?? "unknown"}\n\n{issue.Url ?? ""}";

    private static string FormatPull(GitHubPullItem pr) =>
        $"# PR #{pr.Number}: {pr.Title}\nState: {pr.State ?? "unknown"}\n\n{pr.Url ?? ""}";

    private static string DeriveDocumentId(string sourceKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("github:" + sourceKey)))[..32];

    private static string ComputeHash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
