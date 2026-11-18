namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One independently enumerated local branch, including unpublished work.</summary>
/// <param name="Node">Owning node.</param><param name="Name">Exact branch.</param><param name="Head">Commit identity.</param>
/// <param name="Schema">Branch schema.</param><param name="WorkingHash">Working-state identity.</param><param name="RowHash">Typed row identity.</param>
/// <param name="Dirty">Draft exists.</param><param name="WritesOpen">Domain writes are admitted.</param><param name="ExpandAncestor">Required expand head is an ancestor.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 ac-DirtyDraftBlocksBinaryUpgrade/ac-ContractBlockedUntilLocalBranchesAttested/ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch.</remarks>
internal sealed record Phase0SchemaLocalBranch(string Node, string Name, string Head, int Schema, string WorkingHash, string RowHash, bool Dirty, bool WritesOpen, bool ExpandAncestor);

/// <summary>Node attestation binding read separately from its local branch enumeration.</summary>
/// <param name="Node">Node identity.</param><param name="Online">Reachability observation.</param><param name="Epoch">Attested epoch.</param>
/// <param name="Fingerprint">Attested inventory.</param><param name="Branches">Explicitly covered local branches.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186 ac-ContractBlockedUntilLocalBranchesAttested; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0SchemaAttestation(string Node, bool Online, long Epoch, string Fingerprint, string[] Branches);

/// <summary>Independent before/after persistence and side-effect projection; not a new public DTO.</summary>
/// <param name="Project">Project identity.</param><param name="Epoch">Current maintenance epoch.</param><param name="Fingerprint">Current inventory identity.</param>
/// <param name="Binary">Current binary version.</param><param name="SupportedSchema">Its maximum supported schema.</param><param name="PublicHead">Published main.</param>
/// <param name="WorkerContract">Worker contract.</param><param name="PluginContract">Separately observed plugin contract.</param>
/// <param name="Branches">All local branches.</param><param name="Attestations">All enrolled node attestations.</param>
/// <param name="Swaps">Binary swaps.</param><param name="Services">Service configuration/lifecycle changes caused by the request.</param>
/// <param name="Migrations">Project EF migration executions.</param><param name="Commits">History commits.</param><param name="Transfers">Remote objects transferred.</param><param name="Protected">Full protected-state fingerprint.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 readiness criteria. Counts are independent native evidence, not the command's success flag.</remarks>
internal sealed record Phase0SchemaReadinessState(string Project, long Epoch, string Fingerprint, string Binary, int SupportedSchema, string PublicHead,
    int WorkerContract, int PluginContract, Phase0SchemaLocalBranch[]? Branches, Phase0SchemaAttestation[]? Attestations,
    int Swaps, int Services, int Migrations, int Commits, int Transfers, string? Protected);

/// <summary>Guarded command response, independent of persistence reads.</summary>
/// <param name="Status">HTTP outcome.</param><param name="Code">Stable code.</param><param name="Retryable">Transient retry hint.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234; canonical stable mapping.</remarks>
internal sealed record Phase0SchemaReadinessReply(int Status, string Code, bool Retryable);

/// <summary>Completed-operation receipts and process identity from an independent harness trace.</summary>
/// <param name="Requests">Completed guarded requests.</param><param name="Reopens">Completed close/reopen operations.</param>
/// <param name="Operation">Exact attempted operation.</param><param name="Receipt">Retained native/HTTP receipt identity.</param>
/// <param name="Process">Observed current process identity.</param><param name="ReopenReceipt">Retained process lifecycle receipt.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234. Method entry counters are not acceptable real-binding evidence.</remarks>
internal sealed record Phase0SchemaReadinessTrace(int Requests, int Reopens, string Operation, string? Receipt, string Process, string? ReopenReceipt);

/// <summary>Disposable readiness test seam; real operations remain behind the whole-group mock barrier.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 B7.</remarks>
internal interface IPhase0SchemaReadinessProbe
{
    /// <summary>Prepares the literal named fixture without using assertion expectations.</summary><param name="kind">Fixture key.</param>
    void Prepare(string kind);
    /// <summary>Enumerates persisted branches, attestations and protected effects independently.</summary><returns>Actual state.</returns>
    Phase0SchemaReadinessState? ReadState();
    /// <summary>Attempts the fixture's deployment, contract migration or worker negotiation.</summary><param name="operation">Exact operation.</param><returns>Actual response.</returns>
    Phase0SchemaReadinessReply? Attempt(string operation);
    /// <summary>Closes and reopens the disposable process/store without reconstructing missing state.</summary>
    void Reopen();
    /// <summary>Reads retained operation and process receipts, not method-call counters.</summary><returns>Independent trace.</returns>
    Phase0SchemaReadinessTrace? ReadTrace();
}

/// <summary>Mock-first assertions for all-branch readiness refusal and separate worker negotiation.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 ac-DirtyDraftBlocksBinaryUpgrade/ac-ContractBlockedUntilLocalBranchesAttested/ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch. Real preflight, authentication, filesystem/SCM safety and schema evolution remain unproven.</remarks>
internal static class Phase0SchemaReadinessContract
{
    /// <summary>Checks refusal, complete independent inventories, zero effects and durable preservation through reopen.</summary>
    /// <param name="kind">dirty, unattested, offline, inventory, epoch or protocol.</param><param name="probe">Scripted or later disposable boundary.</param>
    internal static void AssertRefusal(string kind, IPhase0SchemaReadinessProbe probe)
    {
        Assert.Contains(kind, new[] { "dirty", "unattested", "offline", "inventory", "epoch", "protocol" });
        var operation = kind is "dirty" or "inventory" ? "deployment.preflight" : kind == "protocol" ? "worker.negotiate" : "maintenance.contract";
        probe.Prepare(kind);
        var before = AssertState(kind, probe.ReadState());
        Assert.Equal(new(0, 0, "", null, "process-before", null), probe.ReadTrace());
        var reply = probe.Attempt(operation);
        Assert.NotNull(reply); Assert.Equal(409, reply.Status);
        Assert.Equal(kind == "protocol" ? "unsupported_contract" : "maintenance_not_ready", reply.Code); Assert.False(reply.Retryable);
        var after = AssertState(kind, probe.ReadState()); Assert.Equal(before.Protected, after.Protected);
        AssertTrace(probe.ReadTrace(), operation, reopened: false);
        probe.Reopen();
        var persisted = AssertState(kind, probe.ReadState()); Assert.Equal(before.Protected, persisted.Protected);
        AssertTrace(probe.ReadTrace(), operation, reopened: true);
    }

    private static Phase0SchemaReadinessState AssertState(string kind, Phase0SchemaReadinessState? state)
    {
        Assert.NotNull(state); Assert.Equal("Atlas", state.Project); Assert.Equal(12, state.Epoch);
        Assert.Equal("inventory-current", state.Fingerprint); Assert.Equal("binary-2", state.Binary); Assert.Equal(2, state.SupportedSchema);
        Assert.Equal("S1", state.PublicHead); Assert.Equal(kind == "protocol" ? 2 : 1, state.WorkerContract); Assert.Equal(1, state.PluginContract);
        Assert.Equal(0, state.Swaps); Assert.Equal(0, state.Services); Assert.Equal(0, state.Migrations); Assert.Equal(0, state.Commits); Assert.Equal(0, state.Transfers);
        Assert.False(string.IsNullOrWhiteSpace(state.Protected));
        Assert.NotNull(state.Branches); Assert.Equal(3, state.Branches.Length);
        var featureNode = kind == "offline" ? "B" : "A";
        Phase0SchemaLocalBranch[] expected = [new("A", "main", "S1", 2, "clean-S1", "main-rows", false, true, true),
            new("B", "main", "S1", 2, "clean-S1", "main-rows", false, true, true),
            new(featureNode, "feature/unpublished", kind == "protocol" ? "F2" : "F1", kind == "protocol" ? 2 : 1,
                kind == "dirty" ? "draft-42" : "clean-feature", "offline-feature-rows", kind == "dirty", kind == "protocol", kind == "protocol")];
        Assert.Equal(expected.OrderBy(row => row.Node, StringComparer.Ordinal).ThenBy(row => row.Name, StringComparer.Ordinal),
            state.Branches.OrderBy(row => row.Node, StringComparer.Ordinal).ThenBy(row => row.Name, StringComparer.Ordinal));
        Assert.NotNull(state.Attestations); Assert.Equal(2, state.Attestations.Length);
        foreach (var node in new[] { "A", "B" })
        {
            var match = Assert.Single(state.Attestations, item => item.Node == node);
            Assert.Equal(!(kind == "offline" && node == "B"), match.Online); Assert.Equal(kind == "epoch" ? 11 : 12, match.Epoch);
            Assert.Equal(kind == "inventory" ? "inventory-prior" : "inventory-current", match.Fingerprint);
            Assert.NotNull(match.Branches);
            string[] covered = node == "A" && kind == "protocol" ? ["main", "feature/unpublished"] : ["main"];
            Assert.Equal(covered.OrderBy(value => value, StringComparer.Ordinal), match.Branches.OrderBy(value => value, StringComparer.Ordinal));
        }
        return state;
    }

    private static void AssertTrace(Phase0SchemaReadinessTrace? trace, string operation, bool reopened)
    {
        Assert.NotNull(trace); Assert.Equal(1, trace.Requests); Assert.Equal(reopened ? 1 : 0, trace.Reopens); Assert.Equal(operation, trace.Operation);
        Assert.False(string.IsNullOrWhiteSpace(trace.Receipt));
        Assert.Equal(reopened ? "process-after" : "process-before", trace.Process);
        if (reopened) Assert.False(string.IsNullOrWhiteSpace(trace.ReopenReceipt));
        else Assert.Null(trace.ReopenReceipt);
    }
}
