using System.Net;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent provisioning readback for one disposable logical peer.</summary>
/// <param name="Name">Exact logical peer name.</param><param name="DatabaseIdentity">Isolated database identity.</param>
/// <param name="Root">Disposable peer root.</param><param name="Address">Literal listening address.</param>
/// <param name="Port">Allocated listening port.</param><param name="ProcessId">Observed application process ID.</param>
/// <param name="IsRunning">Observed application liveness.</param><param name="ProductionEnrolled">Whether production enrollment occurred.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-008; FR-MCP-186 ac-RealHostAcceptanceMatrix; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0FixturePeer(string Name, string DatabaseIdentity, string Root, string Address,
    int Port, int ProcessId, bool IsRunning, bool ProductionEnrolled);

/// <summary>Disposable three-peer provisioning seam, later bound to the Nuke-owned native fixture processes.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-008; FR-MCP-186; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal interface IPhase0FixtureProvisioningProbe
{
    /// <summary>Reads every unrelated project's provider mapping and data fingerprint without modifying it.</summary>
    /// <returns>Independent stable fingerprints keyed by project identity.</returns>
    IReadOnlyDictionary<string, string>? ReadUnrelatedProjects();
    /// <summary>Provisions isolated H/A/B peers under the supplied TestResults run root using allocated loopback ports.</summary>
    /// <param name="runRoot">Absolute disposable run root.</param>
    /// <returns>Observed provisioned process, database and endpoint identities.</returns>
    IReadOnlyList<Phase0FixturePeer>? Provision(string runRoot);
    /// <summary>Stops only processes owned by this fixture, retaining all original diagnostic files and databases.</summary>
    /// <returns>Independent readback for each previously provisioned peer.</returns>
    IReadOnlyList<Phase0FixturePeer>? StopAndRead();
}

/// <summary>Reusable Ph0 provisioning assertions for the Ph8 acceptance fixture prerequisite.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-008; FR-MCP-186 ac-RealHostAcceptanceMatrix; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// This proves only the provisioning boundary. The complete Ph8 provider/migration/recovery fault matrix remains unproven.
/// </remarks>
internal static class Phase0FixtureProvisioningContract
{
    /// <summary>Checks three isolated running peers, loopback endpoints, unrelated data preservation and fixture shutdown.</summary>
    /// <param name="probe">Scripted or later real Nuke fixture adapter.</param>
    /// <param name="runRoot">Absolute isolated TestResults run root.</param>
    internal static void AssertIsolatedThreePeerLifecycle(IPhase0FixtureProvisioningProbe probe, string runRoot)
    {
        var before = probe.ReadUnrelatedProjects(); Assert.NotNull(before);
        var baseline = before.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        var peers = probe.Provision(runRoot); Assert.NotNull(peers);
        Assert.Equal(new[] { "FED-ATLAS-A", "FED-ATLAS-B", "FED-ATLAS-H" }, peers.Select(peer => peer.Name).Order(StringComparer.Ordinal));
        Assert.Equal(3, peers.Select(peer => peer.DatabaseIdentity).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3, peers.Select(peer => peer.ProcessId).Distinct().Count());
        Assert.Equal(3, peers.Select(peer => peer.Port).Distinct().Count());
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var root = Path.GetFullPath(runRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var peerRoots = peers.Select(peer => Path.GetFullPath(peer.Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).ToArray();
        Assert.Equal(3, peerRoots.Distinct(pathComparer).Count());
        for (var left = 0; left < peerRoots.Length; left++)
        for (var right = 0; right < peerRoots.Length; right++)
        {
            if (left == right) continue;
            var overlaps = peerRoots[right].StartsWith(peerRoots[left], pathComparison);
            Assert.False(overlaps, "Disposable peer roots overlap.");
        }
        foreach (var peer in peers)
        {
            Assert.False(string.IsNullOrWhiteSpace(peer.DatabaseIdentity));
            Assert.True(Path.IsPathFullyQualified(peer.Root));
            Assert.StartsWith(root, Path.GetFullPath(peer.Root), pathComparison);
            Assert.True(IPAddress.TryParse(peer.Address, out var address)); Assert.True(IPAddress.IsLoopback(address));
            Assert.InRange(peer.Port, 1, 65535); Assert.True(peer.ProcessId > 0); Assert.True(peer.IsRunning);
            Assert.False(peer.ProductionEnrolled);
        }
        Assert.Equal(baseline, probe.ReadUnrelatedProjects()?.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray());
        var stopped = probe.StopAndRead(); Assert.NotNull(stopped);
        Assert.Equal(peers.OrderBy(peer => peer.Name, StringComparer.Ordinal).Select(peer => peer with { IsRunning = false }),
            stopped.OrderBy(peer => peer.Name, StringComparer.Ordinal));
        Assert.Equal(baseline, probe.ReadUnrelatedProjects()?.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray());
    }
}
