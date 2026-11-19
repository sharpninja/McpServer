namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Frozen disposable SQL target selected from the run manifest before probing.</summary>
/// <param name="Host">Actual target host.</param><param name="NetworkAddress">Its approved lab-link address.</param><param name="Port">Its run-owned SQL port.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-007 B12; FR-MCP-185 ac-SqlPortUnreachableAcrossLink; TR-MCP-FEDERATION-012; TEST-MCP-233. Fixture ports do not alter the production3306 policy.</remarks>
internal sealed record Phase0SqlNetworkEndpoint(string Host, string NetworkAddress, int Port);

/// <summary>Independent completed connection/query evidence, never a requested listener configuration.</summary>
/// <param name="SourceHost">Host that actually attempted the connection.</param><param name="TargetHost">Host owning the target.</param>
/// <param name="Address">Actual destination address.</param><param name="Port">Actual destination port.</param><param name="LocalHealth">Whether the attempt is local SQL health.</param>
/// <param name="Connected">Observed TCP connection result.</param><param name="HealthValue">Actual SELECT1 scalar, null when no query ran.</param>
/// <param name="Receipt">Retained completed-operation evidence key, with no credential material.</param>
/// <remarks>2026-11-19: B12/ac-SqlPortUnreachableAcrossLink; native adapters read actual socket and SQL results after the all-nine barrier.</remarks>
internal sealed record Phase0SqlNetworkAttempt(string SourceHost, string TargetHost, string Address, int Port, bool LocalHealth,
    bool Connected, int? HealthValue, string Receipt);

/// <summary>Test-only SQL isolation boundary, later supplied by the Nuke-owned disposable two-host adapter.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-007 B12; FR-MCP-185/TR-MCP-FEDERATION-012/TEST-MCP-233 ac-SqlPortUnreachableAcrossLink. Mocks open no connection.</remarks>
internal interface IPhase0SqlNetworkProbe
{
    /// <summary>Prepares both running, isolated SQL services using the already frozen run manifest.</summary>
    void Prepare();
    /// <summary>Completes a local SQL health query or a reciprocal cross-link TCP attempt.</summary>
    /// <param name="sourceHost">Host executing the attempt.</param><param name="targetHost">Owner of the addressed SQL service.</param><param name="localHealth">True for local SELECT1, false for cross-link TCP only.</param>
    void Probe(string sourceHost, string targetHost, bool localHealth);
    /// <summary>Reads the last actual completed attempt from independently retained socket/query evidence.</summary>
    /// <returns>Observed evidence, or null if incomplete. Never fabricate refusal from missing evidence or replay requested values.</returns>
    Phase0SqlNetworkAttempt? ReadAttempt();
}

/// <summary>Reusable local-health and reciprocal cross-link refusal assertions, independent of listener configuration.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-007 B12; FR-MCP-185 ac-SqlPortUnreachableAcrossLink; TR-MCP-FEDERATION-012; TEST-MCP-233. Green unit mocks establish assertion discrimination only, never actual network or B12 acceptance.</remarks>
internal static class Phase0SqlNetworkContract
{
    /// <summary>Checks both local services are healthy before both forbidden cross-link connections are refused.</summary>
    /// <param name="probe">Scripted observer or later native disposable adapter.</param><param name="endpoints">Independently frozen target manifest.</param>
    internal static void AssertIsolation(IPhase0SqlNetworkProbe probe, IReadOnlyList<Phase0SqlNetworkEndpoint> endpoints)
    {
        var targets = endpoints.ToArray();
        Assert.Equal(new[] { "LAB-OMARCHY", "PAYTON-LEGION2" }, targets.Select(target => target.Host).Order());
        foreach (var target in targets)
        {
            Assert.Equal(target.Host == "LAB-OMARCHY" ? "10.42.0.214" : "10.42.0.143", target.NetworkAddress);
            Assert.InRange(target.Port, 1, 65535);
        }
        probe.Prepare();
        foreach (var target in targets) Check(target.Host, target, localHealth: true);
        foreach (var target in targets) Check(targets.Single(other => other.Host != target.Host).Host, target, localHealth: false);

        void Check(string source, Phase0SqlNetworkEndpoint target, bool localHealth)
        {
            probe.Probe(source, target.Host, localHealth);
            var attempt = probe.ReadAttempt(); Assert.NotNull(attempt);
            Assert.Equal(source, attempt.SourceHost); Assert.Equal(target.Host, attempt.TargetHost);
            Assert.Equal(localHealth ? "127.0.0.1" : target.NetworkAddress, attempt.Address); Assert.Equal(target.Port, attempt.Port);
            Assert.Equal(localHealth, attempt.LocalHealth); Assert.Equal(localHealth, attempt.Connected);
            if (localHealth) Assert.Equal(1, attempt.HealthValue); else Assert.Null(attempt.HealthValue);
            Assert.False(string.IsNullOrWhiteSpace(attempt.Receipt));
        }
    }
}