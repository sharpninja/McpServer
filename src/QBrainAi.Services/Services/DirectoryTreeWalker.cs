namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-WS-005 / AC-TR-MCP-WS-005-001, TR-MCP-SESSIONLOG-006 / AC-TR-MCP-SESSIONLOG-006-009:
/// walks a directory tree under the user profile breadth first without following directory links or other
/// reparse points, and skips the contents of any directory whose listing fails with an IO or access error
/// (an untrusted mount point, an access-denied or vanished folder), so a scan never fails on what it cannot
/// traverse.
/// </summary>
/// <remarks>
/// A directory is yielded when its parent lists it, unless it is a link; its contents are walked only when its own
/// listing succeeds. File links are yielded as files. Listing errors other than <see cref="IOException"/> and
/// <see cref="UnauthorizedAccessException"/> propagate.
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

    /// <summary>Creates a walker that lists each directory through <paramref name="list"/>.</summary>
    /// <param name="list">Lists one directory, or throws when it cannot be listed.</param>
    internal DirectoryTreeWalker(Func<string, IReadOnlyList<DirectoryListingEntry>> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        _list = list;
    }

    /// <summary>The walker over the real file system.</summary>
    internal static DirectoryTreeWalker FileSystem { get; } = new(ListFileSystem);

    /// <summary>Every directory below <paramref name="root"/> that the walk reaches.</summary>
    /// <param name="root">Directory to walk; it is listed but not yielded.</param>
    /// <returns>The reached directories, breadth first.</returns>
    internal IEnumerable<string> EnumerateDirectories(string root) => Walk(root, yieldDirectories: true);

    /// <summary>Every file below <paramref name="root"/> that the walk reaches.</summary>
    /// <param name="root">Directory to walk.</param>
    /// <returns>The reached files, breadth first.</returns>
    internal IEnumerable<string> EnumerateFiles(string root) => Walk(root, yieldDirectories: false);

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
