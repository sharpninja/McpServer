using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-GH-013-004: Ingests GitHub issues with full detail (title + body + comments) into the context store.
/// FR-SUPPORT-013: Indexes GitHub issue content for semantic search.
/// </summary>
public sealed class IssueIngestor(
    Chunker chunker,
    IGitHubCliService github,
    ILogger<IssueIngestor> logger)
{
    /// <summary>TR-GH-013-004: Ingest all open issues with full detail into document/chunk pairs.</summary>
    public async Task<IReadOnlyList<(ContextDocument Doc, IReadOnlyList<ContextChunk> Chunks)>> IngestAsync(CancellationToken ct = default)
    {
        var results = new List<(ContextDocument, IReadOnlyList<ContextChunk>)>();

        try
        {
            var issuesResult = await github.ListIssuesAsync("all", 100, ct).ConfigureAwait(false);
            if (!issuesResult.Success)
            {
                logger.LogWarning("IssueIngestor: failed to list issues: {Error}", issuesResult.Error);
                return results;
            }

            foreach (var issueItem in issuesResult.Issues)
            {
                try
                {
                    var detailResult = await github.GetIssueAsync(issueItem.Number, ct).ConfigureAwait(false);
                    if (!detailResult.Success || detailResult.Issue is null)
                    {
                        logger.LogWarning("IssueIngestor: failed to get issue #{Number}: {Error}", issueItem.Number, detailResult.ErrorMessage);
                        continue;
                    }

                    var content = FormatIssueContent(detailResult.Issue);
                    var sourceKey = $"issue:{issueItem.Number}";
                    var docId = DeriveDocumentId(sourceKey);
                    var hash = ComputeHash(content);

                    var doc = new ContextDocument
                    {
                        Id = docId,
                        SourceType = "issue",
                        SourceKey = sourceKey,
                        IngestedAt = DateTime.UtcNow,
                        ContentHash = hash
                    };
                    var chunks = chunker.Chunk(docId, content);
                    results.Add((doc, chunks));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "IssueIngestor: error processing issue #{Number}", issueItem.Number);
                }
            }

            logger.LogInformation("IssueIngestor: ingested {Count} issues with full detail", results.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "IssueIngestor: error listing issues");
        }

        return results;
    }

    internal static string FormatIssueContent(GitHubIssueDetail issue)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Issue #{issue.Number}: {issue.Title}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(issue.Body))
        {
            sb.AppendLine(issue.Body);
            sb.AppendLine();
        }

        if (issue.Comments.Count > 0)
        {
            sb.AppendLine("## Comments");
            sb.AppendLine();
            foreach (var comment in issue.Comments)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"### {comment.Author ?? "unknown"} ({comment.CreatedAt ?? "?"})");
                sb.AppendLine(comment.Body ?? "");
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string DeriveDocumentId(string sourceKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("issue:" + sourceKey)))[..32];

    private static string ComputeHash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
