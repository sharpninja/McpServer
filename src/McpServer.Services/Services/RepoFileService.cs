using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Repository file read/list/write with path allowlist and audit.
/// FR-SUPPORT-010: Path allowlist enforced; write operations audited.
/// </summary>
public sealed class RepoFileService : IRepoFileService
{
    private static readonly char[] s_trimSlashChars = { '/', '\\' };
    private readonly IngestionOptions _options;
    private readonly WorkspaceContext _workspaceContext;
    private readonly IWriteAuditLog _auditLog;
    private readonly IChangeEventBus? _eventBus;
    private readonly ILogger<RepoFileService> _logger;


    /// <summary>TR-PLANNED-013, TR-MCP-MT-001: Constructor. Uses WorkspaceContext for workspace-aware path resolution.</summary>
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
        if (!IsAllowed(dir)) return Task.FromResult(new RepoListResult(dir, Array.Empty<RepoListEntry>()));

        var entries = new List<RepoListEntry>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(fullPath))
        {
            var name = Path.GetFileName(entry);
            if (string.IsNullOrEmpty(name) || name.StartsWith('.')) continue;
            var isDir = Directory.Exists(entry);
            var childRelative = string.IsNullOrEmpty(dir) || dir == "." ? name : dir + "/" + name;
            if (!IsAllowed(childRelative)) continue;
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new RepoWriteResult(false, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            return new RepoWriteResult(false, ex.Message);
        }
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
        if (IsPathTraversal(relativePath)) return false;
        var repoRoot = Path.GetFullPath(_workspaceContext.WorkspacePath ?? _options.RepoRoot);
        fullPath = Path.GetFullPath(Path.Combine(repoRoot, relativePath));
        return fullPath.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPathTraversal(string relativePath)
    {
        return relativePath.Contains("..", StringComparison.Ordinal) ||
               Path.IsPathRooted(relativePath);
    }

    private bool IsAllowed(string relativePath)
    {
        var allowlist = _options.RepoAllowlist;
        if (allowlist == null || allowlist.Count == 0) return true;
        return MatchesAllowlist(relativePath, allowlist);
    }

    private static bool MatchesAllowlist(string relativePath, IReadOnlyList<string> patterns)
    {
        foreach (var p in patterns)
        {
            if (p.Contains("**", StringComparison.Ordinal))
            {
                var prefix = p.Replace("**", string.Empty, StringComparison.Ordinal).TrimEnd(s_trimSlashChars);
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
