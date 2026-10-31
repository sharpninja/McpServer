using QBrainAi.Repl.Host;
using QBrainAi.Client;

namespace QBrainAi.Repl.Core.Tests.Host;

/// <summary>
/// FR-MCP-REPL-007 (ac-1 to ac-4): Contract cases and assertions for the marker walk and its diagnostics, one named
/// case per criterion. The expected values come from the criteria: the walk goes from the start folder up to the file
/// system root, the nearest marker on the way up wins, walking up to a parent workspace is intended, and a walk that
/// finds nothing lists every folder it searched in walk order. Every case runs over an in-memory file system rooted at
/// its own virtual temporary folder, with explicit parent relationships and no TMPDIR or host path grammar, so no case depends
/// on what the host holds. The same assertions run against a scripted walk (BDPv4 mock gate), wrong walks (negative
/// controls), and the production resolver.
/// </summary>
internal static class MarkerWalkContract
{
    /// <summary>The marker file name the walk looks for.</summary>
    internal const string MarkerFileName = "AGENTS-README-FIRST.yaml";

    /// <summary>The file system root of the scripted tree.</summary>
    internal const string Root = "/marker-walk-owned-temporary-root";

    /// <summary>The scripted workspace folder under the root.</summary>
    internal const string Workspace = Root + "/w";

    /// <summary>The scripted source folder under the workspace.</summary>
    internal const string Source = Workspace + "/src";

    /// <summary>The folder every case starts its walk from.</summary>
    internal const string Start = Source + "/nested";

    /// <summary>A marker walk: the marker found from <paramref name="startPath"/>, or null, and the folders searched.</summary>
    internal delegate string? Walk(string startPath, IMarkerFileSystem fileSystem, out IReadOnlyList<string> searched);

    /// <summary>A diagnosed resolution: whether it succeeded and, when it did not, its error.</summary>
    internal delegate bool Diagnose(string startPath, IMarkerFileSystem fileSystem, out string error);

    /// <summary>
    /// AC-1 FindMarkerFile_NoMarkerOnTheWayUp_ListsEveryFolderToTheRoot: no marker anywhere, so no marker is returned
    /// and the searched folders are the start, its parents, and the root, in that order.
    /// </summary>
    internal static void AssertNoMarkerOnTheWayUp(Walk walk)
    {
        var marker = walk(Start, new InMemoryMarkerFileSystem(), out var searched);

        Assert.Null(marker);
        Assert.Equal([Start, Source, Workspace, Root], searched);
    }

    /// <summary>
    /// AC-2 FindMarkerFile_MarkerInParent_IsFound: a marker only in the workspace folder is found, as intended, and the
    /// walk stops there.
    /// </summary>
    internal static void AssertMarkerInParentIsFound(Walk walk)
    {
        var marker = walk(Start, new InMemoryMarkerFileSystem(Workspace), out var searched);

        Assert.Equal(Workspace + "/" + MarkerFileName, marker);
        Assert.Equal([Start, Source, Workspace], searched);
    }

    /// <summary>
    /// AC-3 FindMarkerFile_MarkersInStartAndParent_NearestWins: with markers in the start folder and the workspace
    /// folder, the start folder's marker is returned and only the start folder is searched.
    /// </summary>
    internal static void AssertNearestMarkerWins(Walk walk)
    {
        var marker = walk(Start, new InMemoryMarkerFileSystem(Start, Workspace), out var searched);

        Assert.Equal(Start + "/" + MarkerFileName, marker);
        Assert.Equal([Start], searched);
    }

    /// <summary>
    /// AC-4 TryResolveWithDiagnostics_NoMarkerOnTheWayUp_NamesEveryFolderSearched: with no marker anywhere the
    /// resolution fails, and its error names the marker file and every folder searched.
    /// </summary>
    internal static void AssertDiagnosticsNameEveryFolder(Diagnose diagnose)
    {
        var ok = diagnose(Start, new InMemoryMarkerFileSystem(), out var error);

        Assert.False(ok);
        Assert.Contains(MarkerFileName, error, StringComparison.Ordinal);
        foreach (var folder in new[] { Start, Source, Workspace, Root })
            Assert.Contains(folder, error, StringComparison.Ordinal);
    }

    /// <summary>Asserts the three walk cases (AC-1 to AC-3) against <paramref name="walk"/>.</summary>
    internal static void AssertWalk(Walk walk)
    {
        AssertNoMarkerOnTheWayUp(walk);
        AssertMarkerInParentIsFound(walk);
        AssertNearestMarkerWins(walk);
    }

    /// <summary>An in-memory file system holding a marker in each named folder and nothing else.</summary>
    internal sealed class InMemoryMarkerFileSystem(params string[] markerFolders) : IMarkerFileSystem
    {
        /// <inheritdoc />
        public bool FileExists(string path)
            => markerFolders.Any(folder => string.Equals(GetMarkerPath(folder), path, StringComparison.Ordinal));

        /// <inheritdoc />
        public string[] ReadAllLines(string path)
            => throw new FileNotFoundException("The in-memory file system holds marker presence only; no case reads a marker.", path);

        /// <summary>Preserves explicit virtual path spelling without a native path query.</summary>
        public string GetFullPath(string path) => path;

        /// <summary>Returns an explicit parent, stopping at the owned virtual temporary root.</summary>
        public string? GetParentPath(string path) => path switch
        {
            Start => Source, Source => Workspace, Workspace => Root, Root => null,
            _ => throw new InvalidOperationException("Unscripted virtual directory: " + path),
        };

        /// <summary>Combines virtual separator syntax.</summary>
        public string GetMarkerPath(string directory) => directory + "/" + MarkerFileName;

        /// <summary>No trust-cache operation belongs to a marker-presence-only case.</summary>
        public bool TryUseCachedVerification(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent,
            out QBrainAiClientOptions? options) => throw new InvalidOperationException("Presence-only fixture must not read a verification cache.");

        /// <summary>No trust-cache operation belongs to a marker-presence-only case.</summary>
        public void SaveVerifiedCacheEntry(MarkerFileClientOptionsResolver.MarkerSettings marker, string agent) =>
            throw new InvalidOperationException("Presence-only fixture must not write a verification cache.");
    }
}
