using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-SESSIONATTR-001 / TR-MCP-SESSIONATTR-001: rejects filesModified and commit
/// paths that resolve outside the workspace root unless the turn marks them as foreign.
/// Forward-only. Completeness audits filter the prefixes and tags below.
/// </summary>
public static class SessionLogWorkspaceAttributionValidator
{
    /// <summary>Item-level prefix for a path that lives outside the workspace.</summary>
    public const string ForeignPrefix = "foreign:";

    /// <summary>Alternate item-level prefix for a foreign-repo path.</summary>
    public const string ForeignRepoPrefix = "foreign-repo:";

    /// <summary>Alternate item-level prefix for a cross-workspace path.</summary>
    public const string CrossWorkspacePrefix = "cross-workspace:";

    /// <summary>Turn tag that marks filesModified and commits as foreign-repo artifacts.</summary>
    public const string ForeignRepoTag = "foreign-repo";

    /// <summary>Turn tag that marks filesModified and commits as cross-workspace artifacts.</summary>
    public const string CrossWorkspaceTag = "cross-workspace";

    /// <summary>Turn tag that marks filesModified and commits as foreign-workspace artifacts.</summary>
    public const string ForeignWorkspaceTag = "foreign-workspace";

    /// <summary>
    /// Validates filesModified and commit filesChanged on <paramref name="turn"/> against
    /// <paramref name="workspaceRoot"/>. No-op when the workspace root is empty (import or
    /// no ambient workspace).
    /// </summary>
    /// <param name="turn">The incoming turn payload.</param>
    /// <param name="workspaceRoot">Active workspace root, or empty to skip.</param>
    /// <exception cref="ArgumentException">When an unmarked path resolves outside the root.</exception>
    public static void ValidateTurn(UnifiedRequestEntryDto turn, string? workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(turn);
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        ValidatePaths(turn.FilesModified, turn.Tags, workspaceRoot, "filesModified");
        ValidateCommits(turn.Commits, turn.Tags, workspaceRoot);
    }

    /// <summary>
    /// Validates a filesModified (or equivalent) collection against the workspace root.
    /// </summary>
    /// <param name="paths">Paths to check; null skips (omitted merge).</param>
    /// <param name="tags">Turn tags that may mark the whole turn as foreign.</param>
    /// <param name="workspaceRoot">Active workspace root.</param>
    /// <param name="fieldName">Field name for the exception message.</param>
    /// <exception cref="ArgumentException">When an unmarked path resolves outside the root.</exception>
    public static void ValidatePaths(
        IEnumerable<string>? paths,
        IEnumerable<string>? tags,
        string workspaceRoot,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(workspaceRoot);
        ArgumentNullException.ThrowIfNull(fieldName);
        if (paths is null)
            return;

        var turnMarked = IsTurnMarked(tags);
        foreach (var raw in paths)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            if (turnMarked || HasForeignPrefix(raw))
                continue;
            if (IsInsideWorkspace(raw, workspaceRoot))
                continue;

            throw new ArgumentException(
                $"{fieldName} path '{raw}' resolves outside the workspace root. Prefix the path with '{ForeignPrefix}' or add turn tag '{ForeignRepoTag}' or '{CrossWorkspaceTag}'.",
                fieldName);
        }
    }

    /// <summary>
    /// Validates commit filesChanged collections against the workspace root.
    /// </summary>
    /// <param name="commits">Commits to check; null skips.</param>
    /// <param name="tags">Turn tags that may mark the whole turn as foreign.</param>
    /// <param name="workspaceRoot">Active workspace root.</param>
    /// <exception cref="ArgumentException">When an unmarked commit path resolves outside the root.</exception>
    public static void ValidateCommits(
        IEnumerable<SessionLogCommitDto>? commits,
        IEnumerable<string>? tags,
        string workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(workspaceRoot);
        if (commits is null)
            return;

        foreach (var commit in commits)
        {
            if (commit.FilesChanged is null)
                continue;
            ValidatePaths(commit.FilesChanged, tags, workspaceRoot, "commits.filesChanged");
        }
    }

    /// <summary>True when <paramref name="path"/> starts with a documented foreign prefix.</summary>
    /// <param name="path">Candidate path.</param>
    /// <returns>True when the path is explicitly marked foreign.</returns>
    public static bool HasForeignPrefix(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.StartsWith(ForeignPrefix, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(ForeignRepoPrefix, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(CrossWorkspacePrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when turn tags include a documented foreign marker.</summary>
    /// <param name="tags">Turn tags.</param>
    /// <returns>True when the turn is explicitly marked foreign.</returns>
    public static bool IsTurnMarked(IEnumerable<string>? tags)
    {
        if (tags is null)
            return false;

        foreach (var tag in tags)
        {
            if (string.Equals(tag, ForeignRepoTag, StringComparison.OrdinalIgnoreCase)
                || string.Equals(tag, CrossWorkspaceTag, StringComparison.OrdinalIgnoreCase)
                || string.Equals(tag, ForeignWorkspaceTag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideWorkspace(string path, string workspaceRoot)
    {
        string fullRoot;
        string fullPath;
        try
        {
            fullRoot = Path.GetFullPath(workspaceRoot);
            fullPath = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(fullRoot, path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var rootWithSep = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return fullPath.Equals(fullRoot, comparison)
            || fullPath.StartsWith(rootWithSep, comparison);
    }
}
