using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-CORE-013: Repository file read/list/write with path allowlist and audit.
/// FR-SUPPORT-010: Path allowlist enforced; write operations audited.
/// </summary>
public sealed class RepoFileService : IRepoFileService, IRepoFileCompensation
{
    private static readonly char[] s_trimSlashChars = { '/', '\\' };
    private readonly IngestionOptions _options;
    private readonly WorkspaceContext _workspaceContext;
    private readonly IWriteAuditLog _auditLog;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<RepoFileService> _logger;


    /// <summary>TR-PLANNED-CORE-013, TR-MCP-MT-001: Constructor. Uses WorkspaceContext for workspace-aware path resolution.</summary>
    /// <param name="options">Ingestion options providing default repo root and allowlist.</param>
    /// <param name="workspaceContext">Per-request workspace context for multi-workspace resolution.</param>
    /// <param name="auditLog">Audit log for recording write operations.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventBus">Optional in-process bus for publishing repo change events.</param>
    public RepoFileService(IOptions<IngestionOptions> options, WorkspaceContext workspaceContext,
        IWriteAuditLog auditLog, ILogger<RepoFileService> logger, IChangeEventBus? eventBus = null)
    {
        _logger = logger;
        _options = options?.Value ?? new IngestionOptions();
        _workspaceContext = workspaceContext;
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public async Task<RepoFileReadResult?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        var normalized = NormalizeRelative(relativePath);
        if (!TryResolveFullPath(normalized, out var fullPath)) return null;
        if (!IsAllowed(normalized)) return null;
        if (!File.Exists(fullPath)) return new RepoFileReadResult(normalized, string.Empty, false);
        var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        return new RepoFileReadResult(normalized, content, true);
    }

    /// <inheritdoc />
    public Task<RepoListResult> ListAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        var dir = NormalizeRelative(relativePath ?? ".");
        if (!TryResolveFullPath(dir, out var fullPath) || !Directory.Exists(fullPath))
            return Task.FromResult(new RepoListResult(dir, Array.Empty<RepoListEntry>()));
        if (!CanListPath(dir)) return Task.FromResult(new RepoListResult(dir, Array.Empty<RepoListEntry>()));

        var entries = new List<RepoListEntry>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(fullPath))
        {
            var name = Path.GetFileName(entry);
            if (string.IsNullOrEmpty(name) || name.StartsWith('.')) continue;
            var isDir = Directory.Exists(entry);
            var childRelative = string.IsNullOrEmpty(dir) || dir == "." ? name : dir + "/" + name;
            if (isDir)
            {
                if (!CanListPath(childRelative))
                    continue;
            }
            else if (!IsAllowed(childRelative))
            {
                continue;
            }

            entries.Add(new RepoListEntry(name, isDir));
        }
        return Task.FromResult(new RepoListResult(dir, entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList()));
    }

    /// <inheritdoc />
    public async Task<RepoWriteResult> WriteAsync(string relativePath, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(relativePath))
            return new RepoWriteResult(false, "path is required");
        var normalized = NormalizeRelative(relativePath);
        if (!TryResolveFullPath(normalized, out var fullPath))
            return new RepoWriteResult(false, "path not allowed or invalid");
        if (!IsAllowed(normalized))
            return new RepoWriteResult(false, "path not in allowlist");

        try
        {
            var existedBeforeWrite = File.Exists(fullPath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);
            _auditLog.RecordWrite(normalized, DateTime.UtcNow);
            await PublishChangeSafeAsync(
                existedBeforeWrite ? ChangeEventActions.Updated : ChangeEventActions.Created,
                normalized,
                cancellationToken).ConfigureAwait(false);
            return new RepoWriteResult(true, null);
        }
        catch (IOException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return new RepoWriteResult(false, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return new RepoWriteResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<RepoEditResult> EditAsync(
        string relativePath,
        string oldString,
        string newString,
        bool replaceAll = false,
        int? expectedOccurrences = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(oldString);
        ArgumentNullException.ThrowIfNull(newString);
        if (string.IsNullOrWhiteSpace(relativePath))
            return new RepoEditResult(false, 0, "path is required");
        if (oldString.Length == 0)
            return new RepoEditResult(false, 0, "oldString must not be empty");
        if (string.Equals(oldString, newString, StringComparison.Ordinal))
            return new RepoEditResult(false, 0, "oldString and newString must differ");

        var normalized = NormalizeRelative(relativePath);
        if (!TryResolveFullPath(normalized, out var fullPath))
            return new RepoEditResult(false, 0, "path not allowed or invalid");
        if (!IsAllowed(normalized))
            return new RepoEditResult(false, 0, "path not in allowlist");
        if (!File.Exists(fullPath))
            return new RepoEditResult(false, 0, "file not found");

        try
        {
            var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
            var occurrences = CountOccurrences(content, oldString);
            if (occurrences == 0)
                return new RepoEditResult(false, 0, "oldString not found in file");
            if (expectedOccurrences.HasValue && occurrences != expectedOccurrences.Value)
                return new RepoEditResult(false, 0, $"expected {expectedOccurrences.Value} occurrence(s) but found {occurrences}");
            if (!replaceAll && occurrences > 1)
                return new RepoEditResult(false, 0, $"oldString is ambiguous ({occurrences} matches); set replaceAll or widen oldString to a unique span");

            var (updated, replacements) = replaceAll
                ? (content.Replace(oldString, newString, StringComparison.Ordinal), occurrences)
                : (ReplaceFirst(content, oldString, newString), 1);

            await File.WriteAllTextAsync(fullPath, updated, cancellationToken).ConfigureAwait(false);
            _auditLog.RecordWrite(normalized, DateTime.UtcNow);
            await PublishChangeSafeAsync(ChangeEventActions.Updated, normalized, cancellationToken).ConfigureAwait(false);
            return new RepoEditResult(true, replacements, null);
        }
        catch (IOException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return new RepoEditResult(false, 0, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return new RepoEditResult(false, 0, ex.Message);
        }
    }

    private static int CountOccurrences(string content, string token)
    {
        var count = 0;
        var index = content.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = content.IndexOf(token, index + token.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string ReplaceFirst(string content, string oldString, string newString)
    {
        var index = content.IndexOf(oldString, StringComparison.Ordinal);
        return index < 0
            ? content
            : string.Concat(content.AsSpan(0, index), newString, content.AsSpan(index + oldString.Length));
    }

    /// <inheritdoc />
    public async Task<RepoFileSnapshot?> CaptureForWriteAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var normalized = NormalizeRelative(relativePath);
        if (!TryResolveFullPath(normalized, out var fullPath))
            return null;
        if (!IsAllowed(normalized))
            return null;
        if (!File.Exists(fullPath))
            return new RepoFileSnapshot(normalized, Exists: false, Content: string.Empty, ContentSha256: string.Empty);

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        return new RepoFileSnapshot(normalized, Exists: true, content, ComputeSha256(content));
    }

    /// <inheritdoc />
    public async Task RestoreWriteAsync(
        RepoFileSnapshot snapshot,
        string writtenContent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(writtenContent);

        if (!TryResolveFullPath(snapshot.RelativePath, out var fullPath))
            throw new InvalidOperationException($"Rollback path '{snapshot.RelativePath}' is not allowed or invalid.");
        if (!IsAllowed(snapshot.RelativePath))
            throw new InvalidOperationException($"Rollback path '{snapshot.RelativePath}' is not in the repo allowlist.");

        var expectedWrittenHash = ComputeSha256(writtenContent);
        if (File.Exists(fullPath))
        {
            var currentContent = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
            var currentHash = ComputeSha256(currentContent);
            if (!string.Equals(currentHash, expectedWrittenHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"File '{snapshot.RelativePath}' changed after transactional write; rollback refused.");
        }
        else if (snapshot.Exists)
        {
            throw new InvalidOperationException(
                $"File '{snapshot.RelativePath}' changed after transactional write; rollback refused.");
        }

        if (snapshot.Exists)
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(fullPath, snapshot.Content, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private static string NormalizeRelative(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative)) return ".";
        var s = relative.Replace('\\', '/').TrimStart(s_trimSlashChars);
        return string.IsNullOrEmpty(s) ? "." : s;
    }

    private bool TryResolveFullPath(string relativePath, out string fullPath)
    {
        fullPath = null!;
        if (IsPathTraversal(relativePath))
            return false;

        var repoRoot = GetRepoRoot();
        var candidate = Path.GetFullPath(Path.Combine(repoRoot, relativePath));
        if (!IsPathWithinRoot(repoRoot, candidate))
            return false;

        if (ContainsEscapingReparsePoint(repoRoot, candidate, relativePath))
            return false;

        fullPath = candidate;
        return true;
    }

    private static bool IsPathTraversal(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return true;

        return SplitPathSegments(relativePath)
            .Any(segment => string.Equals(segment, "..", StringComparison.Ordinal));
    }

    private bool IsAllowed(string relativePath)
    {
        var allowlist = _options.RepoAllowlist;
        if (allowlist == null || allowlist.Count == 0) return true;
        return MatchesAllowlist(relativePath, allowlist);
    }

    private bool CanListPath(string relativePath)
    {
        var allowlist = _options.RepoAllowlist;
        if (allowlist == null || allowlist.Count == 0) return true;
        return CanListPath(relativePath, allowlist);
    }

    private static bool MatchesAllowlist(string relativePath, IReadOnlyList<string> patterns)
        => PathGlobMatcher.MatchesAny(relativePath, patterns);

    private static bool CanListPath(string relativePath, IReadOnlyList<string> patterns)
        => PathGlobMatcher.MayMatchDirectoryPrefix(relativePath, patterns);

    private string GetRepoRoot()
    {
        return Path.GetFullPath(_workspaceContext.WorkspacePath ?? _options.RepoRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private bool ContainsEscapingReparsePoint(string repoRoot, string candidatePath, string relativePath)
    {
        var current = repoRoot;
        foreach (var segment in SplitPathSegments(relativePath))
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current))
                break;

            try
            {
                var attributes = File.GetAttributes(current);
                if (!attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;

                var resolved = ResolveReparsePointTarget(current);
                if (resolved is null || !IsPathWithinRoot(repoRoot, resolved.FullName))
                {
                    _logger.LogWarning(
                        "Rejected repo path {RelativePath} because reparse point {ReparsePath} resolves outside repo root {RepoRoot}.",
                        relativePath,
                        current,
                        repoRoot);
                    return true;
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Rejected repo path {RelativePath} because reparse-point validation failed at {ReparsePath}.",
                    relativePath,
                    current);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Rejected repo path {RelativePath} because reparse-point validation could not access {ReparsePath}.",
                    relativePath,
                    current);
                return true;
            }
        }

        return false;
    }

    private static FileSystemInfo? ResolveReparsePointTarget(string path)
    {
        if (Directory.Exists(path))
            return new DirectoryInfo(path).ResolveLinkTarget(returnFinalTarget: true);

        if (File.Exists(path))
            return new FileInfo(path).ResolveLinkTarget(returnFinalTarget: true);

        return null;
    }

    private static bool IsPathWithinRoot(string rootPath, string candidatePath)
    {
        var relative = Path.GetRelativePath(rootPath, candidatePath);
        if (string.IsNullOrWhiteSpace(relative) || string.Equals(relative, ".", StringComparison.Ordinal))
            return true;

        if (Path.IsPathRooted(relative))
            return false;

        return !string.Equals(relative, "..", StringComparison.Ordinal)
               && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
               && !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static string[] SplitPathSegments(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || string.Equals(path, ".", StringComparison.Ordinal))
            return [];

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private async Task PublishChangeSafeAsync(string action, string entityId, CancellationToken cancellationToken)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Repo,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/repo/{entityId}",
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing repo change event for {EntityId}", entityId);
        }
    }
}
