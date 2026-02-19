namespace FWH.Support.Mcp.Services;

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
