namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent mock responses prove merge assertions accept valid outcomes and reject unsafe observations.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-006; FR-MCP-180 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview/ac-CleanMergeCanFailDomainValidation/ac-StaleReviewRejected/ac-SafeTodoMergeRules;
/// TR-MCP-FEDERATION-007; TEST-MCP-228. No Dolt merge, engine adapter, database or production behavior is implemented here.
/// </remarks>
public sealed class Phase0MergeValidationMockGateTests
{
    private static readonly string[] Scenarios = ["same_cell", "delete_update", "dependency_cycle", "different_cells", "priority", "completion"];
    /// <summary>Gets all approved three-way input scenarios.</summary>
    public static IEnumerable<object[]> CorrectScenarios => Scenarios.Select(scenario => new object[] { scenario });
    /// <summary>Each valid fixture passes the same reusable assertions before and after restart.</summary>
    /// <param name="scenario">Fixed fixture selection.</param>
    [Theory, MemberData(nameof(CorrectScenarios))]
    public void CorrectIntegrationPasses(string scenario)
    {
        var probe = new Probe(); Phase0MergeValidationContract.AssertIntegration(probe, scenario);
        Assert.Equal(new[] { "prepare:" + scenario, "integrate", "read", "restart", "read" }, probe.Calls);
    }
    /// <summary>Row enumeration order is not a merge requirement.</summary>
    [Fact]
    public void ReversedRowsRemainValid() => Phase0MergeValidationContract.AssertIntegration(new Probe(reverse: true), "different_cells");
    /// <summary>Stale incoming source is independently A2 and the old B1/A1 preview creates no merge commit.</summary>
    [Fact]
    public void CorrectStaleReviewPasses()
    {
        var probe = new Probe(); Phase0MergeValidationContract.AssertStaleReview(probe);
        Assert.Equal(new[] { "prepare:same_cell", "integrate", "read", "advance", "resolve", "read", "restart", "read" }, probe.Calls);
    }

    /// <summary>Gets independent result, row, review, conflict and physical-ref corruptions at both read checkpoints.</summary>
    public static IEnumerable<object[]> IntegrationFaults
    {
        get
        {
            foreach (var scenario in Scenarios)
            for (var checkpoint = 0; checkpoint < 2; checkpoint++)
            {
                foreach (var fault in new[] { "Missing", "State", "LiveHead", "LiveValue", "LiveMissing", "LiveExtra", "PublicValue", "PublicMissing", "PublicExtra", "Tracking", "EmptyCandidateHead", "Constraint", "DomainError", "CellCount" })
                    yield return [scenario, checkpoint, fault];
                if (scenario is "dependency_cycle" or "different_cells")
                    foreach (var fault in new[] { "CandidateValue", "CandidateMissing", "CandidateExtra" }) yield return [scenario, checkpoint, fault];
                if (scenario == "different_cells") { yield return [scenario, checkpoint, "UnexpectedReview"]; continue; }
                foreach (var fault in new[] { "MissingReview", "EmptyId", "Project", "Branch", "Base", "Local", "Incoming", "CandidateRef", "ReviewError" })
                    yield return [scenario, checkpoint, fault];
                if (scenario != "dependency_cycle")
                    foreach (var fault in new[] { "CellMissing", "CellEntity", "CellColumn", "CellBase", "CellLocal", "CellIncoming", "LocalDeleted", "IncomingDeleted" }) yield return [scenario, checkpoint, fault];
                else yield return [scenario, checkpoint, "UnexpectedCell"];
                foreach (var host in new[] { "LocalPin", "RemotePin" })
                foreach (var role in new[] { "base", "local", "incoming", "candidate" })
                foreach (var member in new[] { "Missing", "Target", "Readable", "Name" })
                    yield return [scenario, checkpoint, host + ":" + role + ":" + member];
                foreach (var fault in new[] { "LocalPinDuplicate", "RemotePinDuplicate", "LocalPinExtra", "RemotePinExtra" }) yield return [scenario, checkpoint, fault];
                if (checkpoint == 1)
                    foreach (var fault in new[] { "ReviewIdChanged", "CandidateChanged", "PinNameChanged" }) yield return [scenario, checkpoint, fault];
            }
        }
    }
    /// <summary>Every corrupt persisted observation must fail an assertion, never merely throw an arbitrary exception.</summary>
    /// <param name="scenario">Fixed fixture.</param><param name="checkpoint">Corrupted read.</param><param name="fault">One independent field corruption.</param>
    [Theory, MemberData(nameof(IntegrationFaults))]
    public void WrongIntegrationIsRejected(string scenario, int checkpoint, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0MergeValidationContract.AssertIntegration(new Probe(checkpoint, fault), scenario));

    /// <summary>Gets stale-resolution response/effect errors and independent tracking/restart evidence loss.</summary>
    public static IEnumerable<object[]> StaleFaults
    {
        get
        {
            foreach (var fault in new[] { "NoResolution", "ResolveStatus", "ResolveError", "ResolveCommit", "AdvanceNoOp" }) yield return [-1, fault];
            foreach (var checkpoint in new[] { 1, 2 })
            foreach (var fault in new[] { "Missing", "LiveHead", "LiveValue", "PublicValue", "MissingReview", "ReviewIdChanged", "Tracking", "CandidateChanged", "PinNameChanged", "RemotePin:incoming:Missing" }) yield return [checkpoint, fault];
        }
    }
    /// <summary>Accepted stale input, changed live data, lost pins or fabricated A2 must fail the reusable stale-review contract.</summary>
    /// <param name="checkpoint">Corrupted read, or minus one for an operation fault.</param><param name="fault">Independent stale outcome corruption.</param>
    [Theory, MemberData(nameof(StaleFaults))]
    public void WrongStaleReviewIsRejected(int checkpoint, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0MergeValidationContract.AssertStaleReview(new Probe(checkpoint, fault)));

    /// <summary>Scripted fixture responses and interaction capture, independent of the assertion implementation.</summary>
    private sealed class Probe(int checkpoint = -1, string fault = "", bool reverse = false) : IPhase0MergeValidationProbe
    {
        private const string ReviewText = "11111111-2222-3333-4444-555555555555";
        private static readonly Guid ReviewId = Guid.Parse(ReviewText);
        private string scenario = "";
        private bool integrated;
        private bool advanced;
        private int reads;
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare(string name) { Assert.Contains(name, Scenarios); scenario = name; Calls.Add("prepare:" + name); }
        /// <inheritdoc />
        public void Integrate() { Assert.NotEqual("", scenario); integrated = true; Calls.Add("integrate"); }
        /// <inheritdoc />
        public void Restart() => Calls.Add("restart");
        /// <inheritdoc />
        public void AdvanceIncoming() { Calls.Add("advance"); advanced = fault != "AdvanceNoOp"; }
        /// <inheritdoc />
        public Phase0MergeResolution? Resolve(Guid reviewId, string localHead, string incomingHead)
        {
            Calls.Add("resolve"); Assert.Equal(ReviewId, reviewId); Assert.Equal("B1", localHead); Assert.Equal("A1", incomingHead);
            return fault switch { "NoResolution" => null, "ResolveStatus" => new(200, "stale_review", 0), "ResolveError" => new(409, "wrong", 0), "ResolveCommit" => new(409, "stale_review", 1), _ => new(409, "stale_review", 0) };
        }
        /// <inheritdoc />
        public Phase0MergeSnapshot? Read()
        {
            Calls.Add("read"); Assert.True(integrated); var currentRead = reads++;
            var live = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["T1.title"] = scenario == "delete_update" ? "edited" : "original",
                ["T1.description"] = scenario == "different_cells" ? "local description" : "original description",
                ["T1.priority"] = scenario is "same_cell" or "priority" ? "medium" : "low",
                ["T1.completion"] = scenario == "completion" ? "doing" : "todo", ["T1.dependsOn"] = scenario == "dependency_cycle" ? "T2" : "",
                ["T2.title"] = "second", ["T2.dependsOn"] = ""
            };
            var candidate = new Dictionary<string, string>(live, StringComparer.Ordinal);
            if (scenario == "different_cells") candidate["T1.title"] = "incoming title";
            if (scenario == "dependency_cycle") candidate["T2.dependsOn"] = "T1";
            var publicRefs = new Dictionary<string, string> { ["Atlas/main"] = "A1", ["Atlas/feature"] = "F1", ["Other/main"] = "X1" };
            var cycle = scenario == "dependency_cycle"; var valid = scenario == "different_cells";
            Phase0MergeCell[] cells = cycle ? [] : scenario switch
            {
                "delete_update" => [new("T1", "title", "original", "edited", null, false, true)],
                "completion" => [new("T1", "completion", "todo", "doing", "done", false, false)],
                _ => [new("T1", "priority", "low", "medium", "high", false, false)]
            };
            string[] errors = cycle ? ["dependency_cycle"] : [];
            var candidateRef = "candidate/" + ReviewText;
            var review = new Phase0MergeReview(ReviewId, "Atlas", "main", ["C0"], "B1", "A1", candidateRef, cells, errors);
            Phase0MergePin[] localPins = [new("base", "review/" + ReviewText + "/base", "C0", true), new("local", "review/" + ReviewText + "/local", "B1", true), new("incoming", "review/" + ReviewText + "/incoming", "A1", true), new("candidate", candidateRef, "M1", true)];
            var remotePins = localPins.Select(pin => pin with { Name = "private/" + pin.Name }).ToArray();
            var result = new Phase0MergeSnapshot(valid ? "valid_candidate" : "needs_review", "B1", live, publicRefs, candidate,
                valid || cycle ? 0 : 1, 0, errors, valid ? null : review, valid ? [] : localPins, valid ? [] : remotePins, advanced ? "A2" : "A1", "M1");
            if (reverse) result = result with { LiveRows = live.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value), CandidateRows = candidate.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value) };
            if (currentRead != checkpoint) return result;
            switch (fault)
            {
                case "Missing": return null;
                case "LiveValue": live["T1.title"] = "wrong"; break;
                case "LiveMissing": live.Remove("T2.title"); break;
                case "LiveExtra": live["T3.title"] = "unexpected"; break;
                case "PublicValue": publicRefs["Atlas/main"] = "M1"; break;
                case "PublicMissing": publicRefs.Remove("Atlas/feature"); break;
                case "PublicExtra": publicRefs["Other/new"] = "bad"; break;
                case "CandidateValue": candidate["T1.title"] = "wrong"; break;
                case "CandidateMissing": candidate.Remove("T2.dependsOn"); break;
                case "CandidateExtra": candidate["T3.title"] = "unexpected"; break;
            }
            if (fault.StartsWith("LocalPin:", StringComparison.Ordinal)) result = result with { LocalPins = CorruptPins(localPins, fault) };
            if (fault.StartsWith("RemotePin:", StringComparison.Ordinal)) result = result with { RemotePins = CorruptPins(remotePins, fault) };
            if (fault.StartsWith("Cell", StringComparison.Ordinal) || fault is "LocalDeleted" or "IncomingDeleted")
            {
                if (fault == "CellMissing") review = review with { Cells = [] };
                else if (fault != "CellCount")
                {
                    var cell = cells[0]; cell = fault switch { "CellEntity" => cell with { Entity = "T2" }, "CellColumn" => cell with { Column = "wrong" }, "CellBase" => cell with { Base = "wrong" }, "CellLocal" => cell with { Local = "wrong" }, "CellIncoming" => cell with { Incoming = "wrong" }, "LocalDeleted" => cell with { LocalDeleted = !cell.LocalDeleted }, "IncomingDeleted" => cell with { IncomingDeleted = !cell.IncomingDeleted }, _ => cell };
                    review = review with { Cells = [cell] };
                }
                result = result with { Review = review };
            }
            result = fault switch
            {
                "State" => result with { State = "merged" }, "LiveHead" => result with { LiveHead = "M1" }, "Tracking" => result with { TrackingIncomingHead = "wrong" },
                "EmptyCandidateHead" => result with { CandidateHead = "" }, "Constraint" => result with { ConstraintViolations = 1 }, "DomainError" => result with { DomainErrors = ["wrong"] }, "CellCount" => result with { CellConflicts = result.CellConflicts + 1 },
                "UnexpectedReview" => result with { Review = review }, "MissingReview" => result with { Review = null }, "EmptyId" => result with { Review = review with { Id = Guid.Empty } },
                "Project" => result with { Review = review with { Project = "Other" } }, "Branch" => result with { Review = review with { Branch = "feature" } }, "Base" => result with { Review = review with { BaseHeads = ["wrong"] } },
                "Local" => result with { Review = review with { LocalHead = "wrong" } }, "Incoming" => result with { Review = review with { IncomingHead = "A2" } }, "CandidateRef" => result with { Review = review with { CandidateRef = "wrong" } },
                "ReviewError" => result with { Review = review with { Errors = ["wrong"] } }, "UnexpectedCell" => result with { Review = review with { Cells = [new("T1", "title", "x", "y", "z", false, false)] } },
                "ReviewIdChanged" => result with { Review = review with { Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") } },
                "CandidateChanged" => result with { CandidateHead = "M2", LocalPins = localPins.Select(pin => pin.Purpose == "candidate" ? pin with { Target = "M2" } : pin).ToArray(), RemotePins = remotePins.Select(pin => pin.Purpose == "candidate" ? pin with { Target = "M2" } : pin).ToArray() },
                "PinNameChanged" => result with { RemotePins = remotePins.Select(pin => pin with { Name = pin.Name + "-changed" }).ToArray() },
                "LocalPinDuplicate" => result with { LocalPins = localPins.Select(pin => pin with { Name = "duplicate" }).ToArray() }, "RemotePinDuplicate" => result with { RemotePins = remotePins.Select(pin => pin with { Name = "duplicate" }).ToArray() },
                "LocalPinExtra" => result with { LocalPins = [..localPins, new("extra", "extra", "extra", true)] }, "RemotePinExtra" => result with { RemotePins = [..remotePins, new("extra", "extra", "extra", true)] }, _ => result
            };
            return result;
        }
        /// <summary>Changes only the selected host/role field, leaving the correct fixture independent of assertions.</summary>
        /// <param name="pins">Scripted physical refs.</param><param name="fault">One host/role/field corruption.</param><returns>Single-fault array.</returns>
        private static Phase0MergePin[] CorruptPins(Phase0MergePin[] pins, string fault)
        {
            var parts = fault.Split(':'); var role = parts[1]; var field = parts[2];
            if (field == "Missing") return pins.Where(pin => pin.Purpose != role).ToArray();
            return pins.Select(pin => pin.Purpose != role ? pin : field switch { "Target" => pin with { Target = "wrong" }, "Readable" => pin with { Readable = false }, "Name" => pin with { Name = "" }, _ => pin }).ToArray();
        }
    }
}
