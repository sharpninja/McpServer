namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Fixture-selected host policy; supplying it does not approve a host installation.</summary>
/// <param name="Owner">Run-scoped ownership identity.</param><param name="ServiceName">Isolated service identity.</param>
/// <param name="Account">Expected restricted account.</param><param name="StartMode">Expected start mode.</param>
/// <param name="BinaryHash">Pinned host binary hash.</param><param name="RecoveryDelaysMs">Three ordered recovery delays.</param>
/// <param name="StopTimeoutMs">Candidate graceful-stop budget.</param><param name="RequiredArtifacts">Original store/log artifact keys.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-001 B1; FR-MCP-186; TR-MCP-FEDERATION-013 native host lifecycle; TEST-MCP-234.</remarks>
internal sealed record Phase0HostPolicy(string Owner, string ServiceName, string Account, string StartMode,
    string BinaryHash, IReadOnlyList<int> RecoveryDelaysMs, int StopTimeoutMs, IReadOnlyList<string> RequiredArtifacts);

/// <summary>Independent native service configuration readback, not requested configuration.</summary>
/// <param name="ServiceName">Observed service name.</param><param name="Account">Observed account.</param>
/// <param name="StartMode">Observed start mode.</param><param name="BinaryHash">Observed executable hash.</param>
/// <param name="RecoveryDelaysMs">Observed ordered failure actions.</param><param name="RebootsMachine">Whether a failure action reboots.</param>
internal sealed record Phase0HostConfiguration(string ServiceName, string Account, string StartMode,
    string BinaryHash, IReadOnlyList<int>? RecoveryDelaysMs, bool RebootsMachine);

/// <summary>One observed owned process; identity includes process creation identity, not just a reusable PID.</summary>
/// <param name="Identity">Process creation identity.</param><param name="Parent">Parent creation identity, null for host root.</param>
/// <param name="Role">host, dolt or helper.</param><param name="Owner">Observed fixture attribution.</param>
/// <param name="Account">Observed process token account.</param>
internal sealed record Phase0HostProcess(string Identity, string? Parent, string Role, string Owner, string Account);

/// <summary>Independent state gathered after one requested lifecycle operation.</summary>
/// <param name="Registered">Service registration exists.</param><param name="State">Running, Stopped or Absent.</param>
/// <param name="Processes">Actual attributed process inventory.</param><param name="DataFingerprint">Seeded store fingerprint.</param>
/// <param name="UnpublishedHead">Retained local unpublished head.</param><param name="Artifacts">Retained original file hashes.</param>
/// <param name="Unrelated">Unrelated service/process/data fingerprints.</param><param name="StopElapsedMs">Observed graceful-stop elapsed time.</param>
/// <param name="ForcedTermination">Whether graceful stop used force.</param><param name="RecoveryElapsedMs">Observed crash-to-restart delay.</param>
/// <param name="FailedChild">Creation identity of the deliberately failed child.</param>
internal sealed record Phase0HostState(bool Registered, string State, IReadOnlyList<Phase0HostProcess>? Processes,
    string? DataFingerprint, string? UnpublishedHead, IReadOnlyDictionary<string, string>? Artifacts,
    IReadOnlyDictionary<string, string>? Unrelated, long? StopElapsedMs = null, bool ForcedTermination = false,
    long? RecoveryElapsedMs = null, string? FailedChild = null);

/// <summary>Mock-first seam later bound to Nuke-owned native disposable host operations.</summary>
/// <remarks>2026-11-18: B1/TR-MCP-FEDERATION-013; no environment-dependent implementation belongs in this unit project.</remarks>
internal interface IPhase0HostLifecycleProbe
{
    /// <summary>Prepares an isolated seeded fixture; never touches an enrolled project.</summary>
    /// <param name="policy">Candidate-specific fixture policy.</param>
    void Prepare(Phase0HostPolicy policy);
    /// <summary>Reads actual registered configuration independently of the requested values.</summary>
    /// <returns>Observed configuration.</returns>
    Phase0HostConfiguration? ReadConfiguration();
    /// <summary>Reads process, service and retained-resource state independently.</summary>
    /// <returns>Observed snapshot.</returns>
    Phase0HostState? ReadState();
    /// <summary>Starts only the owned service.</summary>
    void Start();
    /// <summary>Requests graceful stop and observes completion, retaining originals.</summary>
    void StopGracefully();
    /// <summary>Fails the current owned Dolt child and observes configured recovery.</summary>
    /// <param name="ordinal">Zero-based failure ordinal.</param>
    void FailChildAndWait(int ordinal);
    /// <summary>Unregisters only the stopped owned host, retaining store and diagnostics.</summary>
    void Unregister();
}

/// <summary>Reusable candidate host lifecycle assertions, without service installation or OS evidence.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-001 B1; FR-MCP-186; TR-MCP-FEDERATION-013 native lifecycle; TEST-MCP-234.
/// Supporting design evidence only, not full ac-FourProvidersResolve, ac-WindowsWriterLockAndRestart or ac-RealHostAcceptanceMatrix.
/// Native ACLs, locks, SQL readiness and failure timing must be measured by later integration adapters.
/// 2026-11-18: B1 lifecycle baseline dictionaries are copied before operations so mutable observer caches cannot alter expectations.
/// </remarks>
internal static class Phase0HostLifecycleContract
{
    /// <summary>Checks start, graceful stop, three crash recoveries and owned cleanup while preserving original state.</summary>
    /// <param name="probe">Independent scripted or future native observer.</param><param name="policy">Fixture-selected candidate policy.</param>
    internal static void AssertLifecycle(IPhase0HostLifecycleProbe probe, Phase0HostPolicy policy)
    {
        foreach (var text in new[] { policy.Owner, policy.ServiceName, policy.Account, policy.StartMode, policy.BinaryHash })
            Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Equal(3, policy.RecoveryDelaysMs.Count);
        Assert.All(policy.RecoveryDelaysMs, delay => Assert.True(delay > 0));
        Assert.True(policy.StopTimeoutMs > 0); Assert.NotEmpty(policy.RequiredArtifacts);
        probe.Prepare(policy);
        var config = probe.ReadConfiguration(); Assert.NotNull(config);
        Assert.Equal(policy.ServiceName, config.ServiceName); Assert.Equal(policy.Account, config.Account);
        Assert.Equal(policy.StartMode, config.StartMode); Assert.Equal(policy.BinaryHash, config.BinaryHash);
        Assert.NotNull(config.RecoveryDelaysMs); Assert.Equal(policy.RecoveryDelaysMs, config.RecoveryDelaysMs);
        Assert.False(config.RebootsMachine);
        var baseline = Required(probe.ReadState());
        AssertStopped(baseline, registered: true);
        Assert.False(string.IsNullOrWhiteSpace(baseline.DataFingerprint));
        Assert.False(string.IsNullOrWhiteSpace(baseline.UnpublishedHead));
        Assert.NotNull(baseline.Artifacts); Assert.NotNull(baseline.Unrelated); Assert.NotEmpty(baseline.Unrelated);
        foreach (var key in policy.RequiredArtifacts)
        {
            Assert.True(baseline.Artifacts.TryGetValue(key, out var hash)); Assert.False(string.IsNullOrWhiteSpace(hash));
        }
        foreach (var pair in baseline.Artifacts.Concat(baseline.Unrelated))
        {
            Assert.False(string.IsNullOrWhiteSpace(pair.Key)); Assert.False(string.IsNullOrWhiteSpace(pair.Value));
        }
        baseline = baseline with
        {
            Artifacts = baseline.Artifacts.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            Unrelated = baseline.Unrelated.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
        };
        probe.Start(); var running = CheckPreservation(probe, baseline); AssertRunning(running, policy);
        probe.StopGracefully(); var stopped = CheckPreservation(probe, baseline); AssertGraceful(stopped, policy);
        probe.Start(); running = CheckPreservation(probe, baseline); AssertRunning(running, policy);
        for (var ordinal = 0; ordinal < 3; ordinal++)
        {
            var failed = running.Processes!.Single(process => process.Role == "dolt").Identity;
            probe.FailChildAndWait(ordinal); running = CheckPreservation(probe, baseline); AssertRunning(running, policy);
            Assert.Equal(failed, running.FailedChild);
            Assert.DoesNotContain(running.Processes!, process => process.Identity == failed);
            Assert.NotNull(running.RecoveryElapsedMs);
            Assert.True(running.RecoveryElapsedMs.Value >= policy.RecoveryDelaysMs[ordinal]);
        }
        probe.StopGracefully(); stopped = CheckPreservation(probe, baseline); AssertGraceful(stopped, policy);
        probe.Unregister(); AssertStopped(CheckPreservation(probe, baseline), registered: false);
    }

    /// <summary>Requires a snapshot before reading its fields.</summary>
    private static Phase0HostState Required(Phase0HostState? state) { Assert.NotNull(state); return state; }

    /// <summary>Compares every original retained resource; additional diagnostic artifacts are allowed.</summary>
    private static Phase0HostState CheckPreservation(IPhase0HostLifecycleProbe probe, Phase0HostState baseline)
    {
        var state = Required(probe.ReadState());
        Assert.Equal(baseline.DataFingerprint, state.DataFingerprint); Assert.Equal(baseline.UnpublishedHead, state.UnpublishedHead);
        Assert.NotNull(state.Artifacts); Assert.NotNull(state.Unrelated);
        foreach (var pair in baseline.Artifacts!)
        {
            Assert.True(state.Artifacts.TryGetValue(pair.Key, out var hash)); Assert.Equal(pair.Value, hash);
        }
        Assert.Equal(baseline.Unrelated!.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            state.Unrelated.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        return state;
    }

    /// <summary>Requires the complete owned process inventory to be empty after stop or unregister.</summary>
    private static void AssertStopped(Phase0HostState state, bool registered)
    {
        Assert.Equal(registered, state.Registered); Assert.Equal(registered ? "Stopped" : "Absent", state.State);
        Assert.NotNull(state.Processes); Assert.Empty(state.Processes);
    }

    /// <summary>Rejects forced or late shutdown as evidence of the candidate's ordinary graceful-stop behavior.</summary>
    private static void AssertGraceful(Phase0HostState state, Phase0HostPolicy policy)
    {
        AssertStopped(state, registered: true); Assert.False(state.ForcedTermination); Assert.NotNull(state.StopElapsedMs);
        Assert.InRange(state.StopElapsedMs.Value, 0, policy.StopTimeoutMs);
    }

    /// <summary>Requires one host and Dolt child with a connected, acyclic, correctly attributed process tree.</summary>
    private static void AssertRunning(Phase0HostState state, Phase0HostPolicy policy)
    {
        Assert.True(state.Registered); Assert.Equal("Running", state.State); Assert.NotNull(state.Processes);
        Assert.Equal(state.Processes.Count, state.Processes.Select(process => process.Identity).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(state.Processes, process => process.Role == "host");
        Assert.Single(state.Processes, process => process.Role == "dolt");
        var root = state.Processes.Single(process => process.Role == "host"); Assert.Null(root.Parent);
        foreach (var process in state.Processes)
        {
            Assert.False(string.IsNullOrWhiteSpace(process.Identity)); Assert.Equal(policy.Owner, process.Owner);
            Assert.Equal(policy.Account, process.Account); Assert.Contains(process.Role, new[] { "host", "dolt", "helper" });
            var current = process;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (current != root)
            {
                Assert.True(visited.Add(current.Identity), "Owned process graph contains a cycle.");
                Assert.NotNull(current.Parent);
                var parents = state.Processes.Where(candidate => candidate.Identity == current.Parent).ToArray();
                Assert.Single(parents); current = parents[0];
            }
        }
    }
}