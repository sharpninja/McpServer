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
/// and prove the walk assertion rejects each. The reachability cases (hostile validation round 5 finding R5-F1;
/// operator 2026-10-21: "Fix both, strengthen ACs" and "Approve, strengthen AC-006/008 (Recommended)") script the
/// folders between a boundary (the user profile or home) and a scan root: a scan root is walked only when it and
/// every folder between the boundary and it are listed by their parents as directories that are not links, and the
/// boundary itself is trusted. Their negative controls run wrong checks (the unchecked root, a check that follows
/// links, a check of the root alone, a check that also rejects a linked boundary, a prefix match without a
/// separator, a check that propagates listing errors, a check that also lists the root) and prove the reachability
/// assertion rejects each.
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
    /// Production binding of the scripted reachability cases: <see cref="DirectoryTreeWalker.IsReachable"/> over
    /// each scripted tree gives the required answer and never lists a directory link below the boundary.
    /// </summary>
    /// <param name="name">Scripted reachability case name.</param>
    [Theory]
    [MemberData(nameof(ReachCaseNames))]
    public void ScriptedReachCase_ProductionCheck_GivesTheRequiredAnswer(string name) =>
        RunReachAndAssert(name, (list, boundary, root) => new DirectoryTreeWalker(list).IsReachable(boundary, root));

    /// <summary>
    /// Production binding of the reachability error filter: a listing error that is neither an IO nor an access
    /// error propagates out of <see cref="DirectoryTreeWalker.IsReachable"/> instead of being taken as unreachable.
    /// </summary>
    [Fact]
    public void ProductionReach_UnexpectedListingError_Propagates()
    {
        var tree = new ScriptedDirectoryTree()
            .Lists("/p", DirEntry("/p/.codex"))
            .Throws("/p/.codex", () => new InvalidOperationException("not an IO error"));

        Assert.Throws<InvalidOperationException>(() => new DirectoryTreeWalker(tree.List).IsReachable("/p", "/p/.codex/plugins"));
        Assert.Throws<ArgumentNullException>(() => new DirectoryTreeWalker(tree.List).IsReachable(null!, "/p"));
        Assert.Throws<ArgumentNullException>(() => new DirectoryTreeWalker(tree.List).IsReachable("/p", null!));
    }

    /// <summary>
    /// Integration: on the real file system, <see cref="DirectoryTreeWalker.FileSystem"/> reaches a real folder below
    /// a boundary, does not reach a scan root that is a directory symbolic link or one reached through a linked
    /// ancestor (each link's target lies outside the boundary), and still reaches a root below a boundary that is
    /// itself a directory symbolic link (AC-TR-MCP-WS-005-001, AC-TR-MCP-SESSIONLOG-006-009; hostile validation
    /// round 5 finding R5-F1). Creating symbolic links depends on the host (on Windows, Developer Mode or the
    /// create-symbolic-link privilege), so this is an integration test.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void FileSystemReach_RealRootAndAncestorLinks_AreNotReachable()
    {
        var boundary = Path.Combine(Path.GetTempPath(), $"mcp-reach-links-{Guid.NewGuid():N}");
        var outside = boundary + "-outside";
        var linkedBoundary = boundary + "-boundary-link";
        var realRoot = Path.Combine(boundary, "real", "plugins", "cache");
        var rootLink = Path.Combine(boundary, "real", "plugins", "root-link");
        var ancestorLink = Path.Combine(boundary, "ancestor-link");
        Directory.CreateDirectory(realRoot);
        Directory.CreateDirectory(Path.Combine(outside, "cache"));
        try
        {
            Directory.CreateSymbolicLink(rootLink, Path.Combine(outside, "cache"));
            Directory.CreateSymbolicLink(ancestorLink, outside);
            Directory.CreateSymbolicLink(linkedBoundary, boundary);
            Assert.True(Directory.Exists(rootLink));
            Assert.True(Directory.Exists(Path.Combine(ancestorLink, "cache")));

            Assert.True(DirectoryTreeWalker.FileSystem.IsReachable(boundary, realRoot));
            Assert.False(DirectoryTreeWalker.FileSystem.IsReachable(boundary, rootLink));
            Assert.False(DirectoryTreeWalker.FileSystem.IsReachable(boundary, Path.Combine(ancestorLink, "cache")));
            Assert.True(DirectoryTreeWalker.FileSystem.IsReachable(linkedBoundary, Path.Combine(linkedBoundary, "real", "plugins", "cache")));
        }
        finally
        {
            foreach (var link in new[] { linkedBoundary, ancestorLink, rootLink })
            {
                if (Directory.Exists(link))
                    Directory.Delete(link);
            }

            Directory.Delete(boundary, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
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

    // Reachability of a scan root below a boundary (hostile validation round 5 finding R5-F1).

    /// <summary>A reachability check under test: lists through <c>list</c> from <c>boundary</c> down to <c>root</c>.</summary>
    private delegate bool ReachFunction(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list,
        string boundary,
        string root);

    /// <summary>
    /// Scripted reachability cases: the tree, the boundary (the user profile or home), the scan root, and whether
    /// the root may be walked. Expected values come from the strengthened criteria AC-TR-MCP-WS-005-001 and
    /// AC-TR-MCP-SESSIONLOG-006-009 (operator 2026-10-21: "Fix both, strengthen ACs"): the root and every folder
    /// between the boundary and it must each be listed by its parent as a directory that is not a link; a folder
    /// whose listing fails with an IO or access error cannot be traversed; the boundary itself is trusted, so a
    /// linked profile or home is still scanned; a root that is the boundary is reachable without any listing.
    /// </summary>
    private static readonly Dictionary<string, (Func<ScriptedDirectoryTree> Tree, string Boundary, string Root, bool Reachable)> ReachCases =
        new(StringComparer.Ordinal)
        {
            ["reach-normal"] = (() => CodexTree(rootIsLink: false), "/p", "/p/.codex/plugins/cache", true),
            ["reach-root-link"] = (() => CodexTree(rootIsLink: true), "/p", "/p/.codex/plugins/cache", false),
            ["reach-top-ancestor-link"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"))
                    .Lists("/p", DirLinkEntry("/p/.codex"), FileEntry("/p/.bashrc")),
                "/p",
                "/p/.codex/plugins/cache",
                false),
            ["reach-middle-ancestor-link"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"))
                    .Lists("/p", DirEntry("/p/.codex"))
                    .Lists("/p/.codex", DirLinkEntry("/p/.codex/plugins")),
                "/p",
                "/p/.codex/plugins/cache",
                false),
            ["reach-boundary-link"] = (
                () => CodexTree(rootIsLink: false).Lists("/", DirLinkEntry("/p")),
                "/p",
                "/p/.codex/plugins/cache",
                true),
            ["reach-parent-unlistable"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"))
                    .Lists("/p", DirEntry("/p/.codex"))
                    .Throws("/p/.codex", () => new IOException(UntrustedMountPointMessage)),
                "/p",
                "/p/.codex/plugins/cache",
                false),
            ["reach-parent-access-denied"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"))
                    .Lists("/p", DirEntry("/p/.codex"))
                    .Throws("/p/.codex", () => new UnauthorizedAccessException("Access to the path is denied.")),
                "/p",
                "/p/.codex/plugins/cache",
                false),
            ["reach-root-absent"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"))
                    .Lists("/p", DirEntry("/p/.codex"))
                    .Lists("/p/.codex", DirEntry("/p/.codex/plugins"))
                    .Lists("/p/.codex/plugins", DirEntry("/p/.codex/plugins/other")),
                "/p",
                "/p/.codex/plugins/cache",
                false),
            ["reach-root-is-file"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"))
                    .Lists("/p", DirEntry("/p/.codex"))
                    .Lists("/p/.codex", DirEntry("/p/.codex/plugins"))
                    .Lists("/p/.codex/plugins", FileEntry("/p/.codex/plugins/cache")),
                "/p",
                "/p/.codex/plugins/cache",
                false),
            ["reach-root-unlistable"] = (
                () => CodexTree(rootIsLink: false).Throws("/p/.codex/plugins/cache", () => new IOException(UntrustedMountPointMessage)),
                "/p",
                "/p/.codex/plugins/cache",
                true),
            ["reach-root-is-boundary"] = (() => new ScriptedDirectoryTree().Lists("/", DirEntry("/p")), "/p", "/p", true),
            ["reach-outside-boundary"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"), DirEntry("/q"))
                    .Lists("/p", DirEntry("/p/.codex")),
                "/p",
                "/q/.codex",
                false),
            ["reach-prefix-sibling-link"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/p"))
                    .Lists("/p", DirLinkEntry("/p/.cod"), DirEntry("/p/.codex"))
                    .Lists("/p/.codex", DirEntry("/p/.codex/plugins")),
                "/p",
                "/p/.codex/plugins",
                true),
            ["reach-trailing-separator"] = (() => CodexTree(rootIsLink: false), "/p", "/p/.codex/plugins/cache/", true),
            ["reach-history-root"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/h"))
                    .Lists("/h", DirEntry("/h/.claude"), DirEntry("/h/.codex")),
                "/h",
                "/h/.claude",
                true),
            ["reach-history-root-link"] = (
                () => new ScriptedDirectoryTree()
                    .Lists("/", DirEntry("/h"))
                    .Lists("/h", DirLinkEntry("/h/.claude"), DirEntry("/h/.codex")),
                "/h",
                "/h/.claude",
                false),
        };

    /// <summary>The scripted reachability case names, for the theories.</summary>
    public static TheoryData<string> ReachCaseNames => new(ReachCases.Keys.Order(StringComparer.Ordinal));

    /// <summary>
    /// The Codex cache layout <c>/p/.codex/plugins/cache</c> under the boundary <c>/p</c>, with the cache root
    /// listed as a link when <paramref name="rootIsLink"/>.
    /// </summary>
    private static ScriptedDirectoryTree CodexTree(bool rootIsLink) =>
        new ScriptedDirectoryTree()
            .Lists("/", DirEntry("/p"))
            .Lists("/p", DirEntry("/p/.codex"), FileEntry("/p/.bashrc"))
            .Lists("/p/.codex", DirEntry("/p/.codex/plugins"))
            .Lists("/p/.codex/plugins", rootIsLink ? DirLinkEntry("/p/.codex/plugins/cache") : DirEntry("/p/.codex/plugins/cache"))
            .Lists("/p/.codex/plugins/cache", DirEntry("/p/.codex/plugins/cache/mcpserver"));

    /// <summary>
    /// Asserts one reachability result against a case: the answer equals the required one, and the check never
    /// listed a directory link below the boundary (it never descends into a link to find the root).
    /// </summary>
    private static void AssertReach(string boundary, bool expected, bool actual, ScriptedDirectoryTree tree)
    {
        Assert.Equal(expected, actual);
        Assert.DoesNotContain(tree.Listed, directory => !string.Equals(directory, boundary, StringComparison.Ordinal) && tree.IsLink(directory));
    }

    /// <summary>Runs one reachability check over a fresh copy of a case's tree and asserts its result.</summary>
    private static void RunReachAndAssert(string name, ReachFunction reach)
    {
        var (treeFactory, boundary, root, reachable) = ReachCases[name];
        var tree = treeFactory();
        AssertReach(boundary, reachable, reach(tree.List, boundary, root), tree);
    }

    /// <summary>
    /// Mock gate positive control: each reachability case's required answer passes the reachability assertion on a
    /// tree that has listed nothing.
    /// </summary>
    /// <param name="name">Scripted reachability case name.</param>
    [Theory]
    [MemberData(nameof(ReachCaseNames))]
    public void ScriptedReachCase_RequiredAnswer_PassesTheReachAssertion(string name)
    {
        var (treeFactory, boundary, _, reachable) = ReachCases[name];
        AssertReach(boundary, reachable, reachable, treeFactory());
    }

    /// <summary>The reachability theories cover every scripted case.</summary>
    [Fact]
    public void ScriptedReachTheory_CoversEveryCase() => Assert.Equal(16, ReachCases.Count);

    /// <summary>
    /// Mock gate negative controls: each wrong reachability check is rejected by the reachability assertion on the
    /// case that exposes it, inside a passing harness. The unchecked root (the behavior before the fix) fails on a
    /// linked root, a linked ancestor and a linked history root; a check that follows links fails on a linked root
    /// and a linked ancestor; a check of the root alone fails on a linked ancestor; a check that also rejects a
    /// linked boundary fails on a linked profile; a prefix match without a separator takes the linked sibling
    /// <c>.cod</c> for <c>.codex</c>; a check that propagates listing errors fails on an unlistable parent; a check
    /// that also lists the root fails when only the root cannot be listed.
    /// </summary>
    [Fact]
    public void WrongReachNegativeControls_AreRejectedInsidePassingHarness()
    {
        foreach (var name in new[] { "reach-root-link", "reach-top-ancestor-link", "reach-history-root-link" })
            Assert.NotNull(Record.Exception(() => RunReachAndAssert(name, UncheckedReach)));

        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-root-link", FollowsLinksReach)));
        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-middle-ancestor-link", FollowsLinksReach)));
        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-top-ancestor-link", RootOnlyReach)));
        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-middle-ancestor-link", RootOnlyReach)));
        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-boundary-link", ChecksBoundaryReach)));
        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-prefix-sibling-link", PrefixMatchReach)));
        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-parent-unlistable", PropagatesListingErrorsReach)));
        Assert.NotNull(Record.Exception(() => RunReachAndAssert("reach-root-unlistable", ListsRootReach)));

        // Each wrong check is wrong only where a case exposes it: on the normal layout, which has no link and no
        // listing error, every one of them gives the required answer, so the rejections above come from the
        // behavior under test and not from an assertion that rejects everything.
        foreach (var reach in new ReachFunction[] { UncheckedReach, FollowsLinksReach, RootOnlyReach, ChecksBoundaryReach, PrefixMatchReach, PropagatesListingErrorsReach, ListsRootReach })
            Assert.Null(Record.Exception(() => RunReachAndAssert("reach-normal", reach)));
    }

    /// <summary>
    /// Mock gate negative control for the reachability error filter: a scripted listing error that is neither an IO
    /// nor an access error escapes the scripted tree, and a check with a catch-all filter hides it; the production
    /// binding requires the check to let it propagate.
    /// </summary>
    [Fact]
    public void UnexpectedReachListingError_EscapesTheScriptedTree()
    {
        var tree = new ScriptedDirectoryTree()
            .Lists("/p", DirEntry("/p/.codex"))
            .Throws("/p/.codex", () => new InvalidOperationException("not an IO error"));

        Assert.Throws<InvalidOperationException>(() => PropagatesListingErrorsReach(tree.List, "/p", "/p/.codex/plugins"));
        Assert.Null(Record.Exception(() => SwallowsEverythingReach(tree.List, "/p", "/p/.codex/plugins")));
    }

    // Wrong reachability checks used only as negative controls.

    private static readonly ReachFunction UncheckedReach = (_, _, _) => true;

    private static bool FollowsLinksReach(Func<string, IReadOnlyList<DirectoryListingEntry>> list, string boundary, string root) =>
        ConfigurableReach(list, boundary, root, checkLinks: false, checkAncestors: true, checkBoundary: false, requireSeparator: true, catchListingErrors: true, catchAll: false, listRoot: false);

    private static bool RootOnlyReach(Func<string, IReadOnlyList<DirectoryListingEntry>> list, string boundary, string root) =>
        ConfigurableReach(list, boundary, root, checkLinks: true, checkAncestors: false, checkBoundary: false, requireSeparator: true, catchListingErrors: true, catchAll: false, listRoot: false);

    private static bool ChecksBoundaryReach(Func<string, IReadOnlyList<DirectoryListingEntry>> list, string boundary, string root) =>
        ConfigurableReach(list, boundary, root, checkLinks: true, checkAncestors: true, checkBoundary: true, requireSeparator: true, catchListingErrors: true, catchAll: false, listRoot: false);

    private static bool PrefixMatchReach(Func<string, IReadOnlyList<DirectoryListingEntry>> list, string boundary, string root) =>
        ConfigurableReach(list, boundary, root, checkLinks: true, checkAncestors: true, checkBoundary: false, requireSeparator: false, catchListingErrors: true, catchAll: false, listRoot: false);

    private static bool PropagatesListingErrorsReach(Func<string, IReadOnlyList<DirectoryListingEntry>> list, string boundary, string root) =>
        ConfigurableReach(list, boundary, root, checkLinks: true, checkAncestors: true, checkBoundary: false, requireSeparator: true, catchListingErrors: false, catchAll: false, listRoot: false);

    private static bool ListsRootReach(Func<string, IReadOnlyList<DirectoryListingEntry>> list, string boundary, string root) =>
        ConfigurableReach(list, boundary, root, checkLinks: true, checkAncestors: true, checkBoundary: false, requireSeparator: true, catchListingErrors: true, catchAll: false, listRoot: true);

    private static bool SwallowsEverythingReach(Func<string, IReadOnlyList<DirectoryListingEntry>> list, string boundary, string root) =>
        ConfigurableReach(list, boundary, root, checkLinks: true, checkAncestors: true, checkBoundary: false, requireSeparator: true, catchListingErrors: true, catchAll: true, listRoot: false);

    private static bool ConfigurableReach(
        Func<string, IReadOnlyList<DirectoryListingEntry>> list,
        string boundary,
        string root,
        bool checkLinks,
        bool checkAncestors,
        bool checkBoundary,
        bool requireSeparator,
        bool catchListingErrors,
        bool catchAll,
        bool listRoot)
    {
        var target = root.Length > 1 ? root.TrimEnd('/') : root;
        try
        {
            if (checkBoundary && boundary.Length > 1)
            {
                var parent = boundary[..Math.Max(1, boundary.LastIndexOf('/'))];
                var self = list(parent)
                    .Where(entry => entry.Path == boundary)
                    .Select(entry => (DirectoryListingEntry?)entry)
                    .FirstOrDefault();
                if (self is null || self.Value.IsLink)
                    return false;
            }

            var current = boundary;
            while (current != target)
            {
                var found = list(current)
                    .Where(entry => target.StartsWith(entry.Path, StringComparison.Ordinal)
                        && (!requireSeparator || target.Length == entry.Path.Length || target[entry.Path.Length] == '/'))
                    .Select(entry => (DirectoryListingEntry?)entry)
                    .FirstOrDefault();
                if (found is not { } next || !next.IsDirectory || next.Path.Length <= current.Length)
                    return false;
                if (next.IsLink && checkLinks && (checkAncestors || next.Path == target))
                    return false;
                current = next.Path;
            }

            if (listRoot)
                _ = list(target);
            return true;
        }
        catch (Exception ex) when (catchAll || (catchListingErrors && ex is IOException or UnauthorizedAccessException))
        {
            return false;
        }
    }

    /// <summary>
    /// Scripted in-memory directory tree: each directory lists fixed entries or throws a fixed exception, every
    /// listing is recorded in order, and an unscripted directory throws <see cref="InvalidOperationException"/>.
    /// </summary>
    private sealed class ScriptedDirectoryTree
    {
        private readonly Dictionary<string, Func<IReadOnlyList<DirectoryListingEntry>>> _listings = new(StringComparer.Ordinal);

        private readonly HashSet<string> _directoryLinks = new(StringComparer.Ordinal);

        /// <summary>Directories listed so far, in order.</summary>
        public List<string> Listed { get; } = [];

        /// <summary>True when some scripted listing reports <paramref name="directory"/> as a directory link.</summary>
        public bool IsLink(string directory) => _directoryLinks.Contains(directory);

        /// <summary>Scripts <paramref name="directory"/> to list <paramref name="entries"/>.</summary>
        public ScriptedDirectoryTree Lists(string directory, params DirectoryListingEntry[] entries)
        {
            _listings[directory] = () => entries;
            foreach (var entry in entries.Where(entry => entry.IsDirectory && entry.IsLink))
                _directoryLinks.Add(entry.Path);
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
