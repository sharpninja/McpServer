namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Observed same-cell review and protected refs after integration.</summary>
/// <param name="State">Visible review state.</param><param name="Base">Base cell value.</param>
/// <param name="Local">Local cell value.</param><param name="Incoming">Incoming cell value.</param>
/// <param name="LiveHead">B/main head.</param><param name="PublicRefs">All public refs in the isolated fixture.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-SameCellConflictNeedsReview; TEST-MCP-228.</remarks>
internal sealed record Phase0ConflictObservation(string State, string Base, string Local, string Incoming,
    string LiveHead, IReadOnlyDictionary<string, string> PublicRefs);

/// <summary>Disposable same-cell merge boundary; no live conflict setting is selected here.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006; FR-MCP-180; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal interface IPhase0ConflictProbe
{
    /// <summary>Prepares base low, local B1 medium, incoming A1 high and the named public refs.</summary>
    /// <param name="publicRefs">Initial protected refs, copied from independent fixture expectations.</param>
    void Prepare(IReadOnlyDictionary<string, string> publicRefs);
    /// <summary>Attempts B's integration of A1 and reads persisted review evidence and all refs.</summary>
    /// <returns>Actual review and ref observations.</returns>
    Phase0ConflictObservation? Integrate();
}

/// <summary>Observed refused maintenance request and all application/swap effects.</summary>
/// <param name="Status">HTTP result.</param><param name="Error">Contract error.</param>
/// <param name="MigrationsApplied">Schema operations actually applied.</param><param name="BinarySwaps">Binary swaps actually attempted.</param>
/// <remarks>2026-11-17: FR-MCP-186 ac-ContractBlockedUntilLocalBranchesAttested; TEST-MCP-234.</remarks>
internal sealed record Phase0MaintenanceObservation(int Status, string Error, int MigrationsApplied, int BinarySwaps);

/// <summary>Observed seed-only capability denial and target/routing effects.</summary>
/// <param name="Status">HTTP result.</param><param name="Error">Contract error.</param>
/// <param name="TargetCommits">New target commits.</param><param name="RoutingProvider">Observed active routing.</param>
/// <remarks>2026-11-17: FR-MCP-187 ac-SeedPhaseRejectsJoin; TEST-MCP-235.</remarks>
internal sealed record Phase0SeedObservation(int Status, string Error, int TargetCommits, string RoutingProvider);

/// <summary>Disposable schema-readiness and seed-capability boundaries.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006; FR-MCP-186/187; TR-MCP-FEDERATION-013/014; TEST-MCP-234/235.</remarks>
internal interface IPhase0SchemaSeedProbe
{
    /// <summary>Requests contract migration when public branches are current but one unpublished branch is unattested.</summary>
    /// <param name="allPublicBranchesCurrent">True for this isolated fixture.</param>
    /// <param name="unpublishedBranchAttested">False for this isolated fixture.</param>
    /// <returns>Observed refusal and effects.</returns>
    Phase0MaintenanceObservation? RequestContract(bool allPublicBranchesCurrent, bool unpublishedBranchAttested);
    /// <summary>Requests join in a Ph3-only fixture whose active source routing is sqlite.</summary>
    /// <param name="mode">join.</param><param name="implementedPhase">Ph3.</param>
    /// <returns>Observed capability denial and routing/commit effects.</returns>
    Phase0SeedObservation? RequestMigration(string mode, string implementedPhase);
}

/// <summary>Independent live-gate state at a deterministic race checkpoint.</summary>
/// <param name="Fence">Current owner fence.</param><param name="WriteInFlight">An admitted write still holds the gate.</param>
/// <param name="TakeoverPending">Fence5 takeover is waiting.</param><param name="Title">Persisted row value.</param>
/// <remarks>2026-11-17: FR-MCP-189 ac-LeaseLostDuringWriteRejected; TEST-MCP-237.</remarks>
internal sealed record Phase0WriteGateState(long Fence, bool WriteInFlight, bool TakeoverPending, string Title);

/// <summary>Stepwise disposable gate race, later implemented using real concurrency barriers.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006; FR-MCP-189; TR-MCP-FEDERATION-016; TEST-MCP-237.</remarks>
internal interface IPhase0WriteGateProbe
{
    /// <summary>Prepares clean fence4 ownership and title original.</summary>
    void Prepare();
    /// <summary>Admits the fence4 write, holding its transaction before completion.</summary>
    void AdmitFenceFourWrite();
    /// <summary>Expires the lease while the admitted transaction still holds the write gate.</summary>
    void ExpireLease();
    /// <summary>Starts fence5 takeover without waiting on its completion.</summary>
    void RequestFenceFiveTakeover();
    /// <summary>Releases the admitted transaction to persist title admitted and finish.</summary>
    void FinishAdmittedWrite();
    /// <summary>Waits for the pending takeover to complete after the write finishes.</summary>
    void FinishTakeover();
    /// <summary>Attempts a later fence4 write of title stale.</summary>
    /// <returns>rejected when admission fails; no numeric error is invented.</returns>
    string? AttemptStaleWrite();
    /// <summary>Reads current ownership, gate state and persisted row independently.</summary>
    /// <returns>Actual state at the checkpoint.</returns>
    Phase0WriteGateState? ReadState();
}

/// <summary>Reusable partial Task006 safety scenarios derived from canonical given/when/then outcomes.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-006; FR-MCP-180 ac-SameCellConflictNeedsReview;
/// FR-MCP-186 ac-ContractBlockedUntilLocalBranchesAttested; FR-MCP-187 ac-SeedPhaseRejectsJoin;
/// FR-MCP-189 ac-LeaseLostDuringWriteRejected; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237.
/// Real merge/constraint/conflict transactions, schema distribution, manifests and host-owner crash recovery remain unproven.
/// </remarks>
internal static class Phase0SafetyBoundaryContract
{
    /// <summary>Checks review cell values and preservation of both local and public heads.</summary>
    /// <param name="probe">Scripted or later real disposable merge boundary.</param>
    internal static void AssertConflictReview(IPhase0ConflictProbe probe)
    {
        var refs = new Dictionary<string, string>(StringComparer.Ordinal) { ["remote/main"] = "A1", ["remote/feature"] = "F1" };
        probe.Prepare(new Dictionary<string, string>(refs, StringComparer.Ordinal));
        var result = probe.Integrate(); Assert.NotNull(result);
        Assert.Equal("needs_review", result.State); Assert.Equal("low", result.Base);
        Assert.Equal("medium", result.Local); Assert.Equal("high", result.Incoming); Assert.Equal("B1", result.LiveHead);
        Assert.Equal(refs.OrderBy(pair => pair.Key), result.PublicRefs.OrderBy(pair => pair.Key));
    }

    /// <summary>Checks that current public branches cannot hide an unattested local unpublished branch.</summary>
    /// <param name="probe">Scripted or later real maintenance boundary.</param>
    internal static void AssertUnattestedContractRefused(IPhase0SchemaSeedProbe probe)
    {
        var result = probe.RequestContract(allPublicBranchesCurrent: true, unpublishedBranchAttested: false);
        Assert.NotNull(result); Assert.Equal(409, result.Status); Assert.Equal("maintenance_not_ready", result.Error);
        Assert.Equal(0, result.MigrationsApplied); Assert.Equal(0, result.BinarySwaps);
    }

    /// <summary>Checks join is unavailable in the seed-only phase without target commits or routing changes.</summary>
    /// <param name="probe">Scripted or later real seed-only migration boundary.</param>
    internal static void AssertSeedRejectsJoin(IPhase0SchemaSeedProbe probe)
    {
        var result = probe.RequestMigration("join", "Ph3");
        Assert.NotNull(result); Assert.Equal(501, result.Status); Assert.Equal("unsupported_capability", result.Error);
        Assert.Equal(0, result.TargetCommits); Assert.Equal("sqlite", result.RoutingProvider);
    }

    /// <summary>Checks expiry blocks new ownership until the admitted transaction completes, then rejects stale admission.</summary>
    /// <param name="probe">Scripted or later real gate race with deterministic barriers.</param>
    internal static void AssertAdmittedWriteFinishesBeforeTakeover(IPhase0WriteGateProbe probe)
    {
        probe.Prepare(); Assert.Equal(new(4, false, false, "original"), probe.ReadState());
        probe.AdmitFenceFourWrite(); Assert.Equal(new(4, true, false, "original"), probe.ReadState());
        probe.ExpireLease(); probe.RequestFenceFiveTakeover(); Assert.Equal(new(4, true, true, "original"), probe.ReadState());
        probe.FinishAdmittedWrite();
        var completed = probe.ReadState(); Assert.NotNull(completed);
        Assert.False(completed.WriteInFlight); Assert.Equal("admitted", completed.Title);
        // A waiting takeover may complete immediately after the admitted write releases the gate.
        Assert.Contains(completed.Fence, new long[] { 4, 5 });
        Assert.Equal(completed.Fence == 4, completed.TakeoverPending);
        probe.FinishTakeover(); Assert.Equal(new(5, false, false, "admitted"), probe.ReadState());
        Assert.Equal("rejected", probe.AttemptStaleWrite());
        Assert.Equal(new(5, false, false, "admitted"), probe.ReadState());
    }
}
