using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests the directory walk that both user-profile scans use (marker agent-plugin version resolution and the
/// session-log agent-history scan): a walk never follows a directory link or other reparse point, skips the
/// contents of any directory whose listing fails with an IO or access error, and keeps walking the rest.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-WS-005 / AC-TR-MCP-WS-005-001 and TR-MCP-SESSIONLOG-006 /
/// AC-TR-MCP-SESSIONLOG-006-009 (operator 2026-10-20: "Fix scans in PR 87 (Recommended)"; triage
/// report b0e41c36). Test data: scripted in-memory directory trees (<see cref="ScriptedDirectoryTree"/>) whose
/// listings return fixed entries or throw the exceptions observed on PAYTON-LEGION2 as kingd (an untrusted mount
/// point), an access-denied folder, and a vanished folder; no file system is read. Expected outputs are derived
/// from the acceptance criteria: a directory is yielded when its parent lists it unless it is a link, its contents
/// are walked only when its own listing succeeds, and file links are yielded as files. Negative controls run
/// deliberately wrong walks (one that follows links, one that propagates listing errors as the legacy recursive
/// enumeration did, one that stops at the first error, one that drops an unlistable directory) over the same trees
/// and prove the walk assertion rejects each.
/// </remarks>
public sealed class DirectoryTreeWalkerTests
{
    private const string UntrustedMountPointMessage =
        "The path cannot be traversed because it contains an untrusted mount point.";

    /// <summary>A walk under test: lists through <c>list</c> from <c>root</c> and returns what it yielded.</summary>
    private delegate (List<string> Directories, List<string> Files) WalkFunction(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list,
        string root);

    private static DirectoryListingEntry DirEntry(string path) => new(path, IsDirectory: true, IsLink: false);

    private static DirectoryListingEntry DirLinkEntry(string path) => new(path, IsDirectory: true, IsLink: true);

    private static DirectoryListingEntry FileEntry(string path) => new(path, IsDirectory: false, IsLink: false);

    private static DirectoryListingEntry FileLinkEntry(string path) => new(path, IsDirectory: false, IsLink: true);

    /// <summary>
    /// Scripted cases: the tree, the walk root, and the directories, files and listed directories the acceptance
    /// criteria require. Each case builds a fresh tree so the listing record starts empty.
    /// </summary>
    private static readonly Dictionary<string, (Func<ScriptedDirectoryTree> Tree, string Root, string[] Directories, string[] Files, string[] Listed)> Cases =
        new(StringComparer.Ordinal)
        {
            ["nested-tree"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/p", DirEntry("/p/a"), FileEntry("/p/f1"))
                    .Lists("/p/a", DirEntry("/p/a/b"), FileEntry("/p/a/f2"))
                    .Lists("/p/a/b", FileEntry("/p/a/b/f3")),
                "/p",
                ["/p/a", "/p/a/b"],
                ["/p/f1", "/p/a/f2", "/p/a/b/f3"],
                ["/p", "/p/a", "/p/a/b"]),
            ["untrusted-mount-point"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/p", DirEntry("/p/bad"), DirEntry("/p/good"))
                    .Throws("/p/bad", () => new IOException(UntrustedMountPointMessage))
                    .Lists("/p/good", DirEntry("/p/good/inner"), FileEntry("/p/good/g.md"))
                    .Lists("/p/good/inner", FileEntry("/p/good/inner/i.md")),
                "/p",
                ["/p/bad", "/p/good", "/p/good/inner"],
                ["/p/good/g.md", "/p/good/inner/i.md"],
                ["/p", "/p/bad", "/p/good", "/p/good/inner"]),
            ["access-denied"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/p", DirEntry("/p/locked"), DirEntry("/p/open"))
                    .Throws("/p/locked", () => new UnauthorizedAccessException("Access to the path is denied."))
                    .Lists("/p/open", FileEntry("/p/open/o.md")),
                "/p",
                ["/p/locked", "/p/open"],
                ["/p/open/o.md"],
                ["/p", "/p/locked", "/p/open"]),
            ["vanished-directory"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/p", DirEntry("/p/gone"), DirEntry("/p/here"))
                    .Throws("/p/gone", () => new DirectoryNotFoundException("Could not find a part of the path."))
                    .Lists("/p/here", FileEntry("/p/here/h.md")),
                "/p",
                ["/p/gone", "/p/here"],
                ["/p/here/h.md"],
                ["/p", "/p/gone", "/p/here"]),
            ["directory-link"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/p", DirEntry("/p/real"), DirLinkEntry("/p/link"), FileEntry("/p/f"))
                    .Lists("/p/real", FileEntry("/p/real/x.md"))
                    .Lists("/p/link", FileEntry("/p/link/y.md")),
                "/p",
                ["/p/real"],
                ["/p/f", "/p/real/x.md"],
                ["/p", "/p/real"]),
            ["file-link"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/p", FileLinkEntry("/p/linked.md"), FileEntry("/p/plain.md")),
                "/p",
                [],
                ["/p/linked.md", "/p/plain.md"],
                ["/p"]),
            ["root-unlistable"] = (
                () => new ScriptedDirectoryTree()
                    .Throws("/p", () => new IOException(UntrustedMountPointMessage)),
                "/p",
                [],
                [],
                ["/p"]),
        };

    /// <summary>The scripted case names, for the theories.</summary>
    public static TheoryData<string> CaseNames => new(Cases.Keys.Order(StringComparer.Ordinal));

    /// <summary>
    /// Asserts one walk result against a case: the yielded directories, the yielded files, and the directories
    /// the walk listed, each compared as an ordinal set (the acceptance criteria fix membership, not order).
    /// </summary>
    private static void AssertWalk(
        (string[] Directories, string[] Files, string[] Listed) expected,
        IEnumerable<string> directories,
        IEnumerable<string> files,
        IEnumerable<string> listed)
    {
        Assert.Equal(expected.Directories.Order(StringComparer.Ordinal), directories.Order(StringComparer.Ordinal));
        Assert.Equal(expected.Files.Order(StringComparer.Ordinal), files.Order(StringComparer.Ordinal));
        Assert.Equal(
            expected.Listed.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
            listed.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    }

    /// <summary>Runs one walk over a fresh copy of a case's tree and asserts its result.</summary>
    private static void RunAndAssert(string name, WalkFunction walk)
    {
        var (treeFactory, root, directories, files, listed) = Cases[name];
        var tree = treeFactory();
        var result = walk(tree.List, root);
        AssertWalk((directories, files, listed), result.Directories, result.Files, tree.Listed);
    }

    /// <summary>
    /// Mock gate positive control: each case's required output passes the walk assertion.
    /// </summary>
    /// <param name="name">Scripted case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_RequiredOutput_PassesTheWalkAssertion(string name)
    {
        var (_, _, directories, files, listed) = Cases[name];
        AssertWalk((directories, files, listed), directories, files, listed);
    }

    /// <summary>The theories cover every scripted case.</summary>
    [Fact]
    public void ScriptedTheory_CoversEveryCase() => Assert.Equal(7, Cases.Count);

    /// <summary>
    /// The scripted tree is itself correct: it records every listing in order, returns the scripted entries,
    /// throws the scripted exception, and refuses a directory it was not given, so a walk that lists a link
    /// it should have skipped fails loudly.
    /// </summary>
    [Fact]
    public void ScriptedTree_RecordsListingsAndRefusesUnscriptedDirectories()
    {
        var tree = new ScriptedDirectoryTree()
            .Lists("/p", DirEntry("/p/a"))
            .Throws("/p/a", () => new IOException(UntrustedMountPointMessage));

        Assert.Equal(new[] { DirEntry("/p/a") }, tree.List("/p"));
        Assert.Equal(UntrustedMountPointMessage, Assert.Throws<IOException>(() => tree.List("/p/a")).Message);
        Assert.Throws<InvalidOperationException>(() => tree.List("/p/unscripted"));
        Assert.Equal(new[] { "/p", "/p/a", "/p/unscripted" }, tree.Listed);
    }

    /// <summary>
    /// Mock gate negative controls: each wrong walk is rejected by the walk assertion on the case that exposes it,
    /// inside a passing harness. The legacy recursive enumeration (follows links, propagates listing errors) fails
    /// on the untrusted mount point, the access-denied folder, the vanished folder, the unlistable root and the
    /// directory link; a walk that follows links fails on the directory link; a walk that stops at the first
    /// listing error loses the healthy sibling; a walk that drops an unlistable directory loses its entry.
    /// </summary>
    [Fact]
    public void WrongWalkNegativeControls_AreRejectedInsidePassingHarness()
    {
        foreach (var name in new[] { "untrusted-mount-point", "access-denied", "vanished-directory", "root-unlistable", "directory-link" })
            Assert.NotNull(Record.Exception(() => RunAndAssert(name, LegacyRecursiveWalk)));

        Assert.NotNull(Record.Exception(() => RunAndAssert("directory-link", FollowsLinksWalk)));
        Assert.NotNull(Record.Exception(() => RunAndAssert("untrusted-mount-point", StopsAtFirstErrorWalk)));
        Assert.NotNull(Record.Exception(() => RunAndAssert("untrusted-mount-point", DropsUnlistableDirectoryWalk)));

        // Each wrong walk is wrong only where a case exposes it: on the nested tree, which has no link and no
        // listing error, every one of them yields the required output, so the rejections above come from the
        // behavior under test and not from an assertion that rejects everything.
        foreach (var walk in new WalkFunction[] { LegacyRecursiveWalk, FollowsLinksWalk, StopsAtFirstErrorWalk, DropsUnlistableDirectoryWalk })
            Assert.Null(Record.Exception(() => RunAndAssert("nested-tree", walk)));
    }

    /// <summary>
    /// Mock gate negative control for the error filter: a scripted listing error that is neither an IO nor an
    /// access error escapes the scripted tree, and a walk with a catch-all filter hides it; the production
    /// binding requires the walk to let it propagate.
    /// </summary>
    [Fact]
    public void UnexpectedListingError_EscapesTheScriptedTree()
    {
        var tree = new ScriptedDirectoryTree()
            .Lists("/p", DirEntry("/p/a"))
            .Throws("/p/a", () => new InvalidOperationException("not an IO error"));

        Assert.Throws<InvalidOperationException>(() => LegacyRecursiveWalk(tree.List, "/p"));
        Assert.Null(Record.Exception(() => SwallowsEverythingWalk(tree.List, "/p")));
    }

    /// <summary>
    /// Production binding of the scripted cases: <see cref="DirectoryTreeWalker"/> over each scripted tree yields
    /// the required directories and files, and both its directory walk and its file walk list exactly the required
    /// directories (never a link, never the contents of an unlistable directory).
    /// </summary>
    /// <param name="name">Scripted case name.</param>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ScriptedCase_ProductionWalk_YieldsTheRequiredOutput(string name)
    {
        var (treeFactory, root, directories, files, listed) = Cases[name];
        var directoryTree = treeFactory();
        var fileTree = treeFactory();

        var yieldedDirectories = new DirectoryTreeWalker(directoryTree.List).EnumerateDirectories(root).ToList();
        var yieldedFiles = new DirectoryTreeWalker(fileTree.List).EnumerateFiles(root).ToList();

        AssertWalk((directories, files, listed), yieldedDirectories, yieldedFiles, directoryTree.Listed);
        AssertWalk((directories, files, listed), yieldedDirectories, yieldedFiles, fileTree.Listed);
    }

    /// <summary>
    /// Production binding of the error filter: a listing error that is neither an IO nor an access error
    /// propagates out of <see cref="DirectoryTreeWalker"/> instead of being skipped.
    /// </summary>
    [Fact]
    public void ProductionWalk_UnexpectedListingError_Propagates()
    {
        var tree = new ScriptedDirectoryTree()
            .Lists("/p", DirEntry("/p/a"))
            .Throws("/p/a", () => new InvalidOperationException("not an IO error"));

        Assert.Throws<InvalidOperationException>(() => new DirectoryTreeWalker(tree.List).EnumerateDirectories("/p").ToList());
        Assert.Throws<ArgumentNullException>(() => new DirectoryTreeWalker(null!));
    }

    /// <summary>
    /// Integration: on the real file system, <see cref="DirectoryTreeWalker.ListFileSystem"/> flags a directory
    /// symbolic link and a file symbolic link as links, and <see cref="DirectoryTreeWalker.FileSystem"/> neither
    /// yields nor descends the directory link (whose target lies outside the walked tree) while it yields the file
    /// link as a file. Creating symbolic links depends on the host (on Windows, Developer Mode or the
    /// create-symbolic-link privilege), so this is an integration test.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void FileSystemWalk_RealLinks_AreFlaggedAndNotDescended()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mcp-walk-links-{Guid.NewGuid():N}");
        var outside = root + "-outside";
        var real = Path.Combine(root, "real");
        var realFile = Path.Combine(real, "x.md");
        var directoryLink = Path.Combine(root, "dir-link");
        var fileLink = Path.Combine(root, "file-link.md");
        Directory.CreateDirectory(real);
        Directory.CreateDirectory(outside);
        System.IO.File.WriteAllText(realFile, "x");
        System.IO.File.WriteAllText(Path.Combine(outside, "y.md"), "y");
        try
        {
            Directory.CreateSymbolicLink(directoryLink, outside);
            System.IO.File.CreateSymbolicLink(fileLink, realFile);

            var entries = DirectoryTreeWalker.ListFileSystem(root);
            Assert.Contains(new DirectoryListingEntry(directoryLink, IsDirectory: true, IsLink: true), entries);
            Assert.Contains(new DirectoryListingEntry(fileLink, IsDirectory: false, IsLink: true), entries);
            Assert.Contains(new DirectoryListingEntry(real, IsDirectory: true, IsLink: false), entries);

            Assert.Equal(new[] { real }, DirectoryTreeWalker.FileSystem.EnumerateDirectories(root));
            Assert.Equal(
                new[] { fileLink, realFile }.Order(StringComparer.Ordinal),
                DirectoryTreeWalker.FileSystem.EnumerateFiles(root).Order(StringComparer.Ordinal));
        }
        finally
        {
            if (System.IO.File.Exists(fileLink))
                System.IO.File.Delete(fileLink);
            if (Directory.Exists(directoryLink))
                Directory.Delete(directoryLink);
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }

    // Wrong walks used only as negative controls.

    private static (List<string> Directories, List<string> Files) LegacyRecursiveWalk(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list, string root)
    {
        var directories = new List<string>();
        var files = new List<string>();
        var pending = new Queue<string>([root]);
        while (pending.Count > 0)
        {
            foreach (var entry in list(pending.Dequeue()))
            {
                if (entry.IsDirectory)
                {
                    directories.Add(entry.Path);
                    pending.Enqueue(entry.Path);
                }
                else
                {
                    files.Add(entry.Path);
                }
            }
        }

        return (directories, files);
    }

    private static (List<string> Directories, List<string> Files) FollowsLinksWalk(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list, string root) =>
        ConfigurableWalk(list, root, skipLinks: false, stopAtFirstError: false, yieldUnlistable: true, catchAll: false);

    private static (List<string> Directories, List<string> Files) StopsAtFirstErrorWalk(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list, string root) =>
        ConfigurableWalk(list, root, skipLinks: true, stopAtFirstError: true, yieldUnlistable: true, catchAll: false);

    private static (List<string> Directories, List<string> Files) DropsUnlistableDirectoryWalk(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list, string root) =>
        ConfigurableWalk(list, root, skipLinks: true, stopAtFirstError: false, yieldUnlistable: false, catchAll: false);

    private static (List<string> Directories, List<string> Files) SwallowsEverythingWalk(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list, string root) =>
        ConfigurableWalk(list, root, skipLinks: true, stopAtFirstError: false, yieldUnlistable: true, catchAll: true);

    private static (List<string> Directories, List<string> Files) ConfigurableWalk(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list,
        string root,
        bool skipLinks,
        bool stopAtFirstError,
        bool yieldUnlistable,
        bool catchAll)
    {
        var directories = new List<string>();
        var files = new List<string>();
        var pending = new Queue<string>([root]);
        while (pending.Count > 0)
        {
            var directory = pending.Dequeue();
            IReadOnlyList<DirectoryListingEntry> entries;
            try
            {
                entries = list(directory);
            }
            catch (Exception ex) when (catchAll || ex is IOException or UnauthorizedAccessException)
            {
                if (!yieldUnlistable)
                    directories.Remove(directory);
                if (stopAtFirstError)
                    break;
                continue;
            }

            foreach (var entry in entries)
            {
                if (entry.IsDirectory && entry.IsLink && skipLinks)
                    continue;
                if (entry.IsDirectory)
                {
                    directories.Add(entry.Path);
                    pending.Enqueue(entry.Path);
                }
                else
                {
                    files.Add(entry.Path);
                }
            }
        }

        return (directories, files);
    }

    /// <summary>
    /// Scripted in-memory directory tree: each directory lists fixed entries or throws a fixed exception, every
    /// listing is recorded in order, and an unscripted directory throws <see cref="InvalidOperationException"/>.
    /// </summary>
    private sealed class ScriptedDirectoryTree
    {
        private readonly Dictionary<string, Func<IReadOnlyList<DirectoryListingEntry>>> _listings = new(StringComparer.Ordinal);

        /// <summary>Directories listed so far, in order.</summary>
        public List<string> Listed { get; } = [];

        /// <summary>Scripts <paramref name="directory"/> to list <paramref name="entries"/>.</summary>
        public ScriptedDirectoryTree Lists(string directory, params DirectoryListingEntry[] entries)
        {
            _listings[directory] = () => entries;
            return this;
        }

        /// <summary>Scripts <paramref name="directory"/> to throw the exception <paramref name="error"/> creates.</summary>
        public ScriptedDirectoryTree Throws(string directory, Func<Exception> error)
        {
            _listings[directory] = () => throw error();
            return this;
        }

        /// <summary>Lists one directory as scripted.</summary>
        public IReadOnlyList<DirectoryListingEntry> List(string directory)
        {
            Listed.Add(directory);
            if (!_listings.TryGetValue(directory, out var listing))
                throw new InvalidOperationException($"Unscripted directory listed: {directory}");
            return listing();
        }
    }
}
