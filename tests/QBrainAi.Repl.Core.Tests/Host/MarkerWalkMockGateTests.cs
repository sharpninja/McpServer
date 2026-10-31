using QBrainAi.Repl.Host;
using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.Host;

/// <summary>
/// FR-MCP-REPL-007 (ac-1 to ac-4), BDPv4 mock gate: proves the <see cref="MarkerWalkContract"/> assertions accept a
/// scripted walk and a scripted diagnosis that follow the criteria, and reject deliberately wrong ones (negative
/// controls), before the resolver reads through <see cref="IMarkerFileSystem"/>. Every case runs over an in-memory
/// file system; no host folder is read.
/// </summary>
public sealed class MarkerWalkMockGateTests
{
    /// <summary>FR-MCP-REPL-007 ac-1 to AC-3: the scripted walk passes every walk case.</summary>
    [Fact]
    public void ScriptedWalk_PassesContract()
        => MarkerWalkContract.AssertWalk(ScriptedWalk(WalkFault.None));

    /// <summary>FR-MCP-REPL-007 ac-4: the scripted diagnosis passes the diagnostics case.</summary>
    [Fact]
    public void ScriptedDiagnosis_PassesContract()
        => MarkerWalkContract.AssertDiagnosticsNameEveryFolder(ScriptedDiagnosis(DiagnosisFault.None));

    /// <summary>
    /// FR-MCP-REPL-007 ac-1 to AC-3 negative controls, each rejected inside this passing test: a walk that never
    /// leaves the start folder, one that skips the start folder, one where the farthest marker wins, one that stops
    /// before the root, and one that reports the searched folders in reverse.
    /// </summary>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData(WalkFault.StartOnly)]
    [InlineData(WalkFault.SkipsStart)]
    [InlineData(WalkFault.FarthestWins)]
    [InlineData(WalkFault.StopsBeforeRoot)]
    [InlineData(WalkFault.ReversedOrder)]
    public void WrongWalk_IsRejected(WalkFault fault)
        => Assert.ThrowsAny<XunitException>(() => MarkerWalkContract.AssertWalk(ScriptedWalk(fault)));

    /// <summary>
    /// FR-MCP-REPL-007 ac-4 negative controls, each rejected inside this passing test: an error that omits the
    /// searched folders, one that omits the marker file name, and a diagnosis that reports success.
    /// </summary>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData(DiagnosisFault.OmitsSearchedFolders)]
    [InlineData(DiagnosisFault.OmitsMarkerName)]
    [InlineData(DiagnosisFault.ReportsSuccess)]
    public void WrongDiagnosis_IsRejected(DiagnosisFault fault)
        => Assert.ThrowsAny<XunitException>(
            () => MarkerWalkContract.AssertDiagnosticsNameEveryFolder(ScriptedDiagnosis(fault)));

    /// <summary>Faults injected into the scripted walk.</summary>
    public enum WalkFault
    {
        /// <summary>No fault: the walk the criteria describe.</summary>
        None,

        /// <summary>Checks only the start folder.</summary>
        StartOnly,

        /// <summary>Starts at the start folder's parent.</summary>
        SkipsStart,

        /// <summary>Keeps walking after a marker and returns the farthest one.</summary>
        FarthestWins,

        /// <summary>Stops one folder below the root.</summary>
        StopsBeforeRoot,

        /// <summary>Reports the searched folders root first.</summary>
        ReversedOrder,
    }

    /// <summary>Faults injected into the scripted diagnosis.</summary>
    public enum DiagnosisFault
    {
        /// <summary>No fault: the diagnosis the criteria describe.</summary>
        None,

        /// <summary>The error does not list the searched folders.</summary>
        OmitsSearchedFolders,

        /// <summary>The error does not name the marker file.</summary>
        OmitsMarkerName,

        /// <summary>Reports success with no marker.</summary>
        ReportsSuccess,
    }

    private static MarkerWalkContract.Walk ScriptedWalk(WalkFault fault)
        => (string startPath, IMarkerFileSystem fileSystem, out IReadOnlyList<string> searched) =>
        {
            var folders = new List<string>();
            string? found = null;
            var current = fileSystem.GetFullPath(startPath);
            if (fault == WalkFault.SkipsStart)
                current = fileSystem.GetParentPath(current);
            while (current is not null)
            {
                if (fault == WalkFault.StopsBeforeRoot && fileSystem.GetParentPath(current) is null)
                    break;
                folders.Add(current);
                var candidate = fileSystem.GetMarkerPath(current);
                if (fileSystem.FileExists(candidate))
                {
                    found = candidate;
                    if (fault != WalkFault.FarthestWins)
                        break;
                }

                if (fault == WalkFault.StartOnly)
                    break;
                current = fileSystem.GetParentPath(current);
            }

            if (fault == WalkFault.ReversedOrder)
                folders.Reverse();
            searched = folders;
            return found;
        };

    private static MarkerWalkContract.Diagnose ScriptedDiagnosis(DiagnosisFault fault)
        => (string startPath, IMarkerFileSystem fileSystem, out string error) =>
        {
            var marker = ScriptedWalk(WalkFault.None)(startPath, fileSystem, out var searched);
            if (marker is not null || fault == DiagnosisFault.ReportsSuccess)
            {
                error = string.Empty;
                return true;
            }

            var name = fault == DiagnosisFault.OmitsMarkerName ? "the marker file" : $"'{MarkerWalkContract.MarkerFileName}'";
            var list = fault == DiagnosisFault.OmitsSearchedFolders ? "the workspace folders" : string.Join("; ", searched);
            error = $"Marker file {name} not found. Searched: {list}.";
            return false;
        };
}
