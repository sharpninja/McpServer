namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Complete named fixture projections, independently reread from project and operational stores.</summary>
/// <param name="Project">Observed project.</param><param name="Branch">Observed branch.</param>
/// <param name="Head">Project ref.</param><param name="Remote">Remote ref.</param><param name="WorkingSet">Actual working-set fingerprint alias.</param>
/// <param name="Lease">Existing owner or no owner.</param><param name="Protected">Other-project/protected-state fingerprint alias.</param>
/// <param name="Authored">All named authored fixture cells, including promoted snapshots and explicit links.</param>
/// <param name="Operational">All named operational fixture cells, including retained original audit.</param>
/// <param name="Effects">Observed fixture notifications; ordinary audit capture is observed separately in Operational.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-004 B2/B3; FR-MCP-184/TR-MCP-FEDERATION-011/TEST-MCP-232 ac-BackgroundWritersDoNotDirtyVersionedBranch/ac-SecondAgentLoggingNeedsNoLease. Projections do not claim the full EF model.</remarks>
internal sealed record Phase0AutomaticWriterState(string Project, string Branch, string Head, string Remote, string WorkingSet, string? Lease,
    string? Protected, IReadOnlyDictionary<string, string?>? Authored, IReadOnlyDictionary<string, string?>? Operational, string[]? Effects);

/// <summary>Completed dispatches and reopened stores derived from retained native receipts at real binding.</summary>
/// <param name="Completed">Ordered completed writer kinds.</param><param name="Reopens">Completed process/store reopens.</param><param name="ReceiptBinding">Nonempty retained execution-receipt alias.</param>
/// <remarks>2026-11-17: FR-MCP-184 operational-isolation and second-agent logging criteria. Method-entry counters alone are not real proof.</remarks>
internal sealed record Phase0AutomaticWriterTrace(string[]? Completed, int Reopens, string? ReceiptBinding);

/// <summary>Internal selected-writer fixture boundary; no product implementation or new public API.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004 B2/B3; FR-MCP-184/TR-MCP-FEDERATION-011/TEST-MCP-232.</remarks>
internal interface IPhase0AutomaticWriterProbe
{
    /// <summary>Seeds authored and operational fixture projections; optionally leases main to agent1.</summary><param name="owned">Whether agent1 owns the lease before automatic work.</param>
    void Prepare(bool owned);
    /// <summary>Executes the actual named automatic/indirect operation as agent2 where relevant.</summary><param name="kind">Fixed fixture operation.</param>
    void Run(string kind);
    /// <summary>Closes and reopens actual stores/processes; never reruns initialization side effects implicitly.</summary>
    void Reopen();
    /// <summary>Reads complete named projections and independent branch/lease/protected state.</summary><returns>Observed state.</returns>
    Phase0AutomaticWriterState? ReadState();
    /// <summary>Reads independently completed dispatch/reopen receipts.</summary><returns>Observed execution.</returns>
    Phase0AutomaticWriterTrace? ReadTrace();
}

/// <summary>Source-derived mixed-field operational routing assertions, proven first with independent scripts.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; FR-MCP-184/TR-MCP-FEDERATION-011/TEST-MCP-232 ac-BackgroundWritersDoNotDirtyVersionedBranch/ac-SecondAgentLoggingNeedsNoLease. Native routing, full graph/audit coverage and standalone comparison remain pending.</remarks>
internal static class Phase0AutomaticWriterContract
{
    /// <summary>Checks each operation before and after reopen with no automatic project edits or lease change.</summary>
    /// <param name="owned">Existing agent1 lease, or none.</param><param name="probe">Independent script or later native adapter.</param>
    internal static void AssertIsolation(bool owned, IPhase0AutomaticWriterProbe probe)
    {
        string[] operations = ["heartbeat", "audit", "automatic-session", "requirements-read-empty", "requirements-read-initialized", "session-backfill",
            "memory-index-main", "memory-index-failure", "co-retrieval-off", "co-retrieval-on", "workspace-bootstrap", "context-ingestion", "agent-seeding", "agent-seeding-repeat", "second-agent-session"];
        var authored = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Requirement/FR-1/Body"]="authored requirement", ["Layer/default/Name"]="Layer 1", ["Memory/M1/Content"]="café 雪",
            ["Memory/M1/Version"]="V1", ["MemoryVersion/V1/Content"]="original", ["MemoryEdge/M1/M2/explicit/Weight"]="0.5",
            ["PromotedSession/S1/PlanFile"]="docs/plans/original.md", ["PromotedSession/S1/TodoId"]="TODO-7"
        };
        var operational = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["OriginalAudit/E0/PayloadHash"]="original-audit-hash", ["MemoryIndex/main/M1/Head"]="C0", ["MemoryIndex/main/M1/Hash"]="old",
            ["MemoryStatus/main/M1"]="pending", ["MemoryStatus/feature/M1"]="pending", ["MemoryEdge/M1/M2/co-retrieved/Weight"]="0.1",
            ["MemoryEdge/M2/M1/co-retrieved/Weight"]="0.1", ["Session/auto/PlanFile"]=null, ["Session/auto/TodoId"]=null,
            ["HostWorkspace/Atlas/CurrentLayer"]="layer-1", ["RuntimeAgent/deleted/State"]="deleted"
        };
        probe.Prepare(owned); var completed = new List<string>(); var reopens = 0; Check();
        foreach (var kind in operations)
        {
            probe.Run(kind); completed.Add(kind);
            switch (kind)
            {
                case "heartbeat": operational["Node/H/LastSeen"]="fixture-time-2"; break;
                case "audit": operational["Audit/E1/PayloadHash"]="new-audit-hash"; break;
                case "automatic-session": operational["Session/auto/Dialog"]="automatic capture"; break;
                case "session-backfill": operational["Session/auto/PlanFile"]="docs/plans/active.md"; operational["Session/auto/TodoId"]="TODO-8"; break;
                case "memory-index-main": operational["MemoryIndex/main/M1/Hash"]="authored-M1-hash"; operational["MemoryStatus/main/M1"]="indexed"; break;
                case "memory-index-failure": operational["MemoryStatus/main/M1"]="failed"; break;
                case "co-retrieval-on": operational["MemoryEdge/M1/M2/co-retrieved/Weight"]="0.2"; operational["MemoryEdge/M2/M1/co-retrieved/Weight"]="0.2"; break;
                case "workspace-bootstrap": operational["HostWorkspace/Beta/Path"]="fixture-host-path"; break;
                case "context-ingestion": operational["RawDocument/D1/Text"]="raw source"; operational["RawChunk/K1/Text"]="raw chunk"; operational["Vector/K1/Value"]="fixture-vector"; break;
                case "agent-seeding": operational["RuntimeAgent/new/State"]="active"; break;
                case "second-agent-session": operational["Session/agent2/Action"]="agent2 action"; break;
            }
            Check(); probe.Reopen(); reopens++; Check();
        }
        void Check()
        {
            var state = probe.ReadState(); Assert.NotNull(state);
            Assert.Equal("Atlas", state.Project); Assert.Equal("main", state.Branch); Assert.Equal("C0", state.Head); Assert.Equal("C0", state.Remote);
            Assert.Equal("Atlas-main-clean-C0", state.WorkingSet); Assert.Equal(owned ? "agent1" : null, state.Lease);
            Assert.Equal("other-project-and-control-baseline", state.Protected);
            Cells(authored, state.Authored); Cells(operational, state.Operational);
            Assert.Equal(completed.Contains("agent-seeding") ? ["Created:RuntimeAgent/new"] : Array.Empty<string>(), Assert.IsType<string[]>(state.Effects));
            var trace = probe.ReadTrace(); Assert.NotNull(trace);
            Assert.Equal(completed, Assert.IsType<string[]>(trace.Completed)); Assert.Equal(reopens, trace.Reopens);
            Assert.Equal("native-completed-receipts", trace.ReceiptBinding);
        }
    }

    /// <summary>Checks exact fixture cell keys and values without inventing enumeration order.</summary>
    /// <param name="expected">Independently specified fixture projection.</param><param name="actual">Reread observed projection.</param>
    private static void Cells(IReadOnlyDictionary<string, string?> expected, IReadOnlyDictionary<string, string?>? actual)
    {
        Assert.NotNull(actual); Assert.Equal(expected.OrderBy(x=>x.Key, StringComparer.Ordinal), actual.OrderBy(x=>x.Key, StringComparer.Ordinal));
    }
}
