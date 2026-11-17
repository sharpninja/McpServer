namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripted fixtures prove partial Task006 assertions discriminate unsafe outcomes.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-006; FR-MCP-180 ac-SameCellConflictNeedsReview;
/// FR-MCP-186 ac-ContractBlockedUntilLocalBranchesAttested; FR-MCP-187 ac-SeedPhaseRejectsJoin;
/// FR-MCP-189 ac-LeaseLostDuringWriteRejected; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237.
/// These are unit mock controls, not evidence of real concurrency, Dolt semantics or migration safety.
/// </remarks>
public sealed class Phase0SafetyBoundaryMockGateTests
{
    /// <summary>Correct review evidence retains both public refs and B/main.</summary>
    [Fact]
    public void CorrectConflictReviewPasses() => Phase0SafetyBoundaryContract.AssertConflictReview(new ConflictProbe());
    /// <summary>Unattested unpublished branch refuses maintenance before schema or binary changes.</summary>
    [Fact]
    public void CorrectUnattestedRefusalPasses() => Phase0SafetyBoundaryContract.AssertUnattestedContractRefused(new SchemaSeedProbe());
    /// <summary>Ph3-only join produces the exact unsupported outcome and no target/routing effects.</summary>
    [Fact]
    public void CorrectSeedOnlyDenialPasses() => Phase0SafetyBoundaryContract.AssertSeedRejectsJoin(new SchemaSeedProbe());
    /// <summary>The admitted fence4 transaction finishes before takeover and the later stale write is rejected.</summary>
    [Fact]
    public void CorrectLeaseOrderingPasses()
    {
        var probe = new GateProbe();
        Phase0SafetyBoundaryContract.AssertAdmittedWriteFinishesBeforeTakeover(probe);
        Assert.Equal(new[] { "prepare", "read", "admit4", "read", "expire", "request5", "read", "finish4", "read", "finish5", "read", "stale4", "read" }, probe.Calls);
    }

    /// <summary>A waiting takeover may complete immediately after the admitted write has finished.</summary>
    [Fact]
    public void ImmediatePostWriteTakeoverRemainsValid()
        => Phase0SafetyBoundaryContract.AssertAdmittedWriteFinishesBeforeTakeover(new GateProbe(fault: "ImmediateTakeover"));

    /// <summary>Gets every independent review/ref outcome corruption.</summary>
    public static IEnumerable<object[]> ConflictFaults => new[] { "Missing", "State", "Base", "Local", "Incoming", "LiveHead", "MainRef", "FeatureRef", "RefDropped", "RefExtra" }.Select(fault => new object[] { fault });
    /// <summary>Gets every schema-refusal status or effect corruption.</summary>
    public static IEnumerable<object[]> MaintenanceFaults => new[] { "Missing", "Status", "Error", "Migration", "Swap" }.Select(fault => new object[] { fault });
    /// <summary>Gets every seed-denial status or effect corruption.</summary>
    public static IEnumerable<object[]> SeedFaults => new[] { "Missing", "Status", "Error", "Commit", "Routing" }.Select(fault => new object[] { fault });
    /// <summary>Gets every gate-state field corruption at each of six checkpoints plus stale admission.</summary>
    public static IEnumerable<object[]> GateFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 6; checkpoint++)
            foreach (var fault in new[] { "Missing", "Fence", "InFlight", "Pending", "Title" })
                yield return [checkpoint, fault];
            yield return [-1, "StaleAccepted"];
        }
    }

    /// <summary>Wrong review values or overwritten refs must fail the reusable contract.</summary>
    /// <param name="fault">Single review/ref corruption.</param>
    [Theory, MemberData(nameof(ConflictFaults))]
    public void WrongConflictOutcomeIsRejected(string fault) => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0SafetyBoundaryContract.AssertConflictReview(new ConflictProbe(fault)));
    /// <summary>Missing refusal or maintenance side effects must fail the reusable contract.</summary>
    /// <param name="fault">Single refusal/effect corruption.</param>
    [Theory, MemberData(nameof(MaintenanceFaults))]
    public void WrongMaintenanceOutcomeIsRejected(string fault) => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0SafetyBoundaryContract.AssertUnattestedContractRefused(new SchemaSeedProbe(fault)));
    /// <summary>Join acceptance, new commits or changed routing must fail the reusable contract.</summary>
    /// <param name="fault">Single denial/effect corruption.</param>
    [Theory, MemberData(nameof(SeedFaults))]
    public void WrongSeedOutcomeIsRejected(string fault) => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0SafetyBoundaryContract.AssertSeedRejectsJoin(new SchemaSeedProbe(fault)));
    /// <summary>Early takeover, lost admitted write and accepted/visible stale writes must fail.</summary>
    /// <param name="checkpoint">Single corrupted readback.</param><param name="fault">Single race outcome corruption.</param>
    [Theory, MemberData(nameof(GateFaults))]
    public void WrongGateOutcomeIsRejected(int checkpoint, string fault) => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0SafetyBoundaryContract.AssertAdmittedWriteFinishesBeforeTakeover(new GateProbe(checkpoint, fault)));

    /// <summary>Independent persisted review result with one optionally corrupted cell/ref.</summary>
    private sealed class ConflictProbe(string fault = "") : IPhase0ConflictProbe
    {
        private bool prepared;
        /// <inheritdoc />
        public void Prepare(IReadOnlyDictionary<string, string> publicRefs)
        {
            Assert.Equal(2, publicRefs.Count); Assert.Equal("A1", publicRefs["remote/main"]); Assert.Equal("F1", publicRefs["remote/feature"]); prepared = true;
        }
        /// <inheritdoc />
        public Phase0ConflictObservation? Integrate()
        {
            Assert.True(prepared);
            var refs = new Dictionary<string, string>(StringComparer.Ordinal) { ["remote/main"] = "A1", ["remote/feature"] = "F1" };
            if (fault == "MainRef") refs["remote/main"] = "merged";
            if (fault == "FeatureRef") refs["remote/feature"] = "merged";
            if (fault == "RefDropped") refs.Remove("remote/feature");
            if (fault == "RefExtra") refs.Add("remote/unexpected", "merged");
            var result = new Phase0ConflictObservation("needs_review", "low", "medium", "high", "B1", refs);
            return fault switch
            {
                "Missing" => null, "State" => result with { State = "merged" }, "Base" => result with { Base = "wrong" },
                "Local" => result with { Local = "wrong" }, "Incoming" => result with { Incoming = "wrong" },
                "LiveHead" => result with { LiveHead = "merged" }, _ => result
            };
        }
    }

    /// <summary>Independent maintenance and seed-only response/effect fixtures.</summary>
    private sealed class SchemaSeedProbe(string fault = "") : IPhase0SchemaSeedProbe
    {
        /// <inheritdoc />
        public Phase0MaintenanceObservation? RequestContract(bool allPublicBranchesCurrent, bool unpublishedBranchAttested)
        {
            Assert.True(allPublicBranchesCurrent); Assert.False(unpublishedBranchAttested);
            var result = new Phase0MaintenanceObservation(409, "maintenance_not_ready", 0, 0);
            return fault switch
            {
                "Missing" => null, "Status" => result with { Status = 200 }, "Error" => result with { Error = "wrong" },
                "Migration" => result with { MigrationsApplied = 1 }, "Swap" => result with { BinarySwaps = 1 }, _ => result
            };
        }
        /// <inheritdoc />
        public Phase0SeedObservation? RequestMigration(string mode, string implementedPhase)
        {
            Assert.Equal("join", mode); Assert.Equal("Ph3", implementedPhase);
            var result = new Phase0SeedObservation(501, "unsupported_capability", 0, "sqlite");
            return fault switch
            {
                "Missing" => null, "Status" => result with { Status = 200 }, "Error" => result with { Error = "wrong" },
                "Commit" => result with { TargetCommits = 1 }, "Routing" => result with { RoutingProvider = "dolt" }, _ => result
            };
        }
    }

    /// <summary>Independent six-stage gate snapshots and method-order evidence, never a gate implementation.</summary>
    private sealed class GateProbe(int checkpoint = -1, string fault = "") : IPhase0WriteGateProbe
    {
        internal List<string> Calls { get; } = [];
        private int reads;
        private static Phase0WriteGateState[] States =>
        [
            new(4, false, false, "original"), new(4, true, false, "original"), new(4, true, true, "original"),
            new(4, false, true, "admitted"), new(5, false, false, "admitted"), new(5, false, false, "admitted")
        ];
        /// <inheritdoc />
        public void Prepare() => Calls.Add("prepare");
        /// <inheritdoc />
        public void AdmitFenceFourWrite() => Calls.Add("admit4");
        /// <inheritdoc />
        public void ExpireLease() => Calls.Add("expire");
        /// <inheritdoc />
        public void RequestFenceFiveTakeover() => Calls.Add("request5");
        /// <inheritdoc />
        public void FinishAdmittedWrite() => Calls.Add("finish4");
        /// <inheritdoc />
        public void FinishTakeover() => Calls.Add("finish5");
        /// <inheritdoc />
        public string? AttemptStaleWrite() { Calls.Add("stale4"); return fault == "StaleAccepted" ? "admitted" : "rejected"; }
        /// <inheritdoc />
        public Phase0WriteGateState? ReadState()
        {
            Calls.Add("read"); var state = States[reads];
            if (reads == 3 && fault == "ImmediateTakeover") state = new(5, false, false, "admitted");
            if (reads++ != checkpoint) return state;
            return fault switch
            {
                "Missing" => null, "Fence" => state with { Fence = 99 }, "InFlight" => state with { WriteInFlight = !state.WriteInFlight },
                "Pending" => state with { TakeoverPending = !state.TakeoverPending }, "Title" => state with { Title = "wrong" }, _ => state
            };
        }
    }
}
