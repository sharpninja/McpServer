using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-WS-005 / FR-MCP-018 ac-1, TR-MCP-SESSIONLOG-006 / AC-FR-MCP-SESSIONLOGCTX-001-008:
/// walks a directory tree under the user profile breadth first without following directory links or other
/// reparse points, and skips the contents of any directory whose listing fails with an IO or access error
/// (an untrusted mount point, an access-denied or vanished folder), so a scan never fails on what it cannot
/// traverse.
/// </summary>
/// <remarks>
/// A directory is yielded when its parent lists it, unless it is a link; its contents are walked only when its own
/// listing succeeds. File links are yielded as files. Listing errors other than <see cref="IOException"/> and
/// <see cref="UnauthorizedAccessException"/> propagate. A scan root below the user profile or home is walked only
/// when <see cref="IsReachable"/> holds for it (hostile validation round 5 finding R5-F1; operator 2026-10-21:
/// "Fix both, strengthen ACs").
/// </remarks>
internal sealed class DirectoryTreeWalker
{
    private static readonly EnumerationOptions s_singleLevel = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = false,
        AttributesToSkip = 0,
        ReturnSpecialDirectories = false,
    };

    private readonly Func<string, IReadOnlyList<DirectoryListingEntry>> _list;
    private readonly WorkspacePathPlatform? _platform;

    /// <summary>Creates a walker that lists each directory through <paramref name="list"/>.</summary>
    /// <param name="list">Lists one directory, or throws when it cannot be listed.</param>
    /// <param name="platform">Explicit path policy; when absent the boundary's syntax selects it.</param>
    internal DirectoryTreeWalker(Func<string, IReadOnlyList<DirectoryListingEntry>> list, WorkspacePathPlatform? platform = null)
    {
        ArgumentNullException.ThrowIfNull(list);
        _list = list;
        _platform = platform;
    }

    /// <summary>The walker over the real file system.</summary>
    internal static DirectoryTreeWalker FileSystem => new(ListFileSystem,
        OperatingSystem.IsWindows() ? WorkspacePathPlatform.Windows : WorkspacePathPlatform.CaseSensitive);

    /// <summary>Every directory below <paramref name="root"/> that the walk reaches.</summary>
    /// <param name="root">Directory to walk; it is listed but not yielded.</param>
    /// <returns>The reached directories, breadth first.</returns>
    internal IEnumerable<string> EnumerateDirectories(string root) => Walk(root, yieldDirectories: true);

    /// <summary>Every file below <paramref name="root"/> that the walk reaches.</summary>
    /// <param name="root">Directory to walk.</param>
    /// <returns>The reached files, breadth first.</returns>
    internal IEnumerable<string> EnumerateFiles(string root) => Walk(root, yieldDirectories: false);

    /// <summary>
    /// FR-MCP-018 ac-1, AC-FR-MCP-SESSIONLOGCTX-001-008: whether <paramref name="root"/> may be walked from
    /// <paramref name="boundary"/>: the root and every folder between the boundary and it must each be listed by
    /// its parent as a directory that is not a link or other reparse point. The boundary itself is trusted (a user
    /// profile or home that is itself a link is still scanned) and its parent is never listed; a root that is the
    /// boundary is reachable without any listing.
    /// </summary>
    /// <remarks>
    /// A folder whose listing fails with an <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>
    /// cannot be traversed, so the root is not reachable; any other listing error propagates. A root that is not
    /// below the boundary, or that a parent does not list as a directory, is not reachable.
    /// </remarks>
    /// <param name="boundary">The trusted folder the scan starts from (the user profile or home).</param>
    /// <param name="root">The scan root below <paramref name="boundary"/>.</param>
    /// <returns>True when the root may be walked.</returns>
    internal bool IsReachable(string boundary, string root)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(root);
        var platform = _platform ?? WorkspaceIdentityPath.DetectPlatform(boundary);
        var comparison = platform == WorkspacePathPlatform.Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string Canonical(string path) => WorkspaceIdentityPath.NormalizeLexicalPath(path, platform);
        var target = Canonical(root);
        var current = Canonical(boundary);
        while (!string.Equals(current, target, comparison))
        {
            IReadOnlyList<DirectoryListingEntry> entries;
            try
            {
                entries = _list(current);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            var found = entries
                .Select(entry => entry with { Path = Canonical(entry.Path) })
                .Where(entry => LeadsTo(entry.Path, target, comparison))
                .Select(entry => (DirectoryListingEntry?)entry)
                .FirstOrDefault();
            if (found is not { } next || !next.IsDirectory || next.IsLink || next.Path.Length <= current.Length)
                return false;
            current = next.Path;
        }

        return true;
    }

    /// <summary>True when <paramref name="path"/> is <paramref name="target"/> or a folder above it.</summary>
    private static bool LeadsTo(string path, string target, StringComparison comparison) =>
        target.StartsWith(path, comparison)
        && (target.Length == path.Length
            || path.EndsWith('/')
            || target[path.Length] == '/');

    /// <summary>Lists one real directory with each entry's directory and link flags.</summary>
    /// <param name="directory">Directory to list.</param>
    /// <returns>The entries of <paramref name="directory"/>.</returns>
    internal static IReadOnlyList<DirectoryListingEntry> ListFileSystem(string directory) =>
        new DirectoryInfo(directory)
            .EnumerateFileSystemInfos("*", s_singleLevel)
            .Select(info => new DirectoryListingEntry(
                info.FullName,
                info is DirectoryInfo,
                (info.Attributes & FileAttributes.ReparsePoint) != 0))
            .ToList();

    private IEnumerable<string> Walk(string root, bool yieldDirectories)
    {
        var pending = new Queue<string>();
        pending.Enqueue(root);
        while (pending.Count > 0)
        {
            foreach (var entry in TryList(pending.Dequeue()))
            {
                if (entry.IsDirectory)
                {
                    if (entry.IsLink)
                        continue;
                    pending.Enqueue(entry.Path);
                    if (yieldDirectories)
                        yield return entry.Path;
                }
                else if (!yieldDirectories)
                {
                    yield return entry.Path;
                }
            }
        }
    }

    private IReadOnlyList<DirectoryListingEntry> TryList(string directory)
    {
        try
        {
            return _list(directory);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}
