using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// Tests the directory walk that both user-profile scans use (marker agent-plugin version resolution and the
/// session-log agent-history scan): a walk never follows a directory link or other reparse point, skips the
/// contents of any directory whose listing fails with an IO or access error, and keeps walking the rest.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-WS-005 / FR-MCP-018 ac-1 and TR-MCP-SESSIONLOG-006 /
/// AC-FR-MCP-SESSIONLOGCTX-001-008 (operator 2026-10-20: "Fix scans in PR 87 (Recommended)"; triage
/// report b0e41c36). Test data: scripted in-memory directory trees (<c>ScriptedDirectoryTree</c>) whose
/// listings return fixed entries or throw the exceptions observed on TEST-NODE-A as fixture-user (an untrusted mount
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
/// <para>2026-11-07, TR-MCP-WS-005; FR-MCP-018 ac-1; TR-MCP-SESSIONLOG-006; FR-MCP-SESSIONLOGCTX-001-008; TEST-NODE-A; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/DirectoryTreeWalkerTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases moved unchanged with the class attributes; only the namespace changed.</para>
/// </remarks>
public sealed class DirectoryTreeWalkerTests
{
    /// <summary>
    /// Integration: on the real file system, <see cref="DirectoryTreeWalker.FileSystem"/> reaches a real folder below
    /// a boundary, does not reach a scan root that is a directory symbolic link or one reached through a linked
    /// ancestor (each link's target lies outside the boundary), and still reaches a root below a boundary that is
    /// itself a directory symbolic link (FR-MCP-018 ac-1, AC-FR-MCP-SESSIONLOGCTX-001-008; hostile validation
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
}
