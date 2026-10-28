using QBrainAi.Repl.Host;

namespace QBrainAi.Repl.Core.Tests.Host;

/// <summary>
/// TR-MCP-REPL-008 (AC-1 to AC-4): Binds the <see cref="MarkerWalkContract"/> cases and assertions, unchanged from the
/// BDPv4 mock gate, to the production <see cref="MarkerFileClientOptionsResolver"/>, one test per named case, over an
/// in-memory file system. No host folder is read, so the cases hold on every host whatever folders sit above its
/// temporary folder.
/// </summary>
public sealed class MarkerWalkTests
{
    /// <summary>TR-MCP-REPL-008 AC-1: no marker on the way up; every folder to the root is listed in walk order.</summary>
    [Fact]
    public void FindMarkerFile_NoMarkerOnTheWayUp_ListsEveryFolderToTheRoot()
        => MarkerWalkContract.AssertNoMarkerOnTheWayUp(ProductionWalk);

    /// <summary>TR-MCP-REPL-008 AC-2: a marker only in a parent folder is found, as intended.</summary>
    [Fact]
    public void FindMarkerFile_MarkerInParent_IsFound()
        => MarkerWalkContract.AssertMarkerInParentIsFound(ProductionWalk);

    /// <summary>TR-MCP-REPL-008 AC-3: with markers in the start folder and a parent, the nearest wins.</summary>
    [Fact]
    public void FindMarkerFile_MarkersInStartAndParent_NearestWins()
        => MarkerWalkContract.AssertNearestMarkerWins(ProductionWalk);

    /// <summary>TR-MCP-REPL-008 AC-4: with no marker on the way up, the error names the marker file and every folder.</summary>
    [Fact]
    public void TryResolveWithDiagnostics_NoMarkerOnTheWayUp_NamesEveryFolderSearched()
        => MarkerWalkContract.AssertDiagnosticsNameEveryFolder(ProductionDiagnosis);

    private static string? ProductionWalk(string startPath, IMarkerFileSystem fileSystem, out IReadOnlyList<string> searched)
        => MarkerFileClientOptionsResolver.FindMarkerFile(startPath, out searched, fileSystem);

    private static bool ProductionDiagnosis(string startPath, IMarkerFileSystem fileSystem, out string error)
        => MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(startPath, null, out _, out error, agent: null, fileSystem);
}
