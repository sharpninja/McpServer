namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Repository file read/list/write with path allowlist.
/// FR-SUPPORT-010: Path allowlist enforced; write audit log.
/// </summary>
public interface IRepoFileService
{
    /// <summary>Read file content by relative path. Returns null if not found or not allowed.</summary>
    /// <param name="relativePath">Relative path from repo root.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The file read result, or <see langword="null"/> if not found or not allowed.</returns>
    Task<RepoFileReadResult?> ReadAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>List directory entries (files and dirs) under relative path. Empty if not allowed.</summary>
    /// <param name="relativePath">Relative path from repo root, or <see langword="null"/> for root.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Directory listing result.</returns>
    Task<RepoListResult> ListAsync(string? relativePath, CancellationToken cancellationToken = default);

    /// <summary>Write content to path. Returns success and records audit. Fails if path not allowed.</summary>
    /// <param name="relativePath">Relative path from repo root.</param>
    /// <param name="content">File content to write.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Write result indicating success or failure with an error message.</returns>
    Task<RepoWriteResult> WriteAsync(string relativePath, string content, CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TXN-001: Captures and restores repository file state for transactional write compensation.
/// </summary>
public interface IRepoFileCompensation
{
    /// <summary>
    /// Captures the file state before a write mutation.
    /// </summary>
    /// <param name="relativePath">Relative path from repo root.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The captured file state, or <see langword="null"/> when the path is invalid or disallowed.</returns>
    Task<RepoFileSnapshot?> CaptureForWriteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the captured file state after a failed transaction commit.
    /// </summary>
    /// <param name="snapshot">Pre-write file snapshot.</param>
    /// <param name="writtenContent">Content written by the rejected transaction.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RestoreWriteAsync(
        RepoFileSnapshot snapshot,
        string writtenContent,
        CancellationToken cancellationToken = default);
}

/// <summary>TR-MCP-TXN-001: Pre-write repository file state used for rollback compensation.</summary>
/// <param name="RelativePath">Normalized relative path from repo root.</param>
/// <param name="Exists">Whether the file existed before the write.</param>
/// <param name="Content">Previous file content, or empty when the file did not exist.</param>
/// <param name="ContentSha256">SHA-256 hash of previous file content, or empty when the file did not exist.</param>
public sealed record RepoFileSnapshot(string RelativePath, bool Exists, string Content, string ContentSha256);

/// <summary>TR-PLANNED-013: Result of repo file read.</summary>
/// <param name="RelativePath">Normalized relative path.</param>
/// <param name="Content">File content (empty string if not found).</param>
/// <param name="Exists">Whether the file exists on disk.</param>
public sealed record RepoFileReadResult(string RelativePath, string Content, bool Exists);

/// <summary>TR-PLANNED-013: Result of repo list.</summary>
/// <param name="Path">Normalized directory path.</param>
/// <param name="Entries">Ordered list of directory entries.</param>
public sealed record RepoListResult(string Path, IReadOnlyList<RepoListEntry> Entries);

/// <summary>TR-PLANNED-013: Single list entry (name, isDirectory).</summary>
/// <param name="Name">File or directory name.</param>
/// <param name="IsDirectory">Whether the entry is a directory.</param>
public sealed record RepoListEntry(string Name, bool IsDirectory);

/// <summary>TR-PLANNED-013: Result of repo write.</summary>
/// <param name="Written">Whether the write succeeded.</param>
/// <param name="Error">Error message when write failed; otherwise <see langword="null"/>.</param>
public sealed record RepoWriteResult(bool Written, string? Error);
