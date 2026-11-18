namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent literal readiness scripts and field corruptions validate the shared B7 assertions.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 ac-DirtyDraftBlocksBinaryUpgrade/ac-ContractBlockedUntilLocalBranchesAttested/ac-DeploymentPreflightBlocksDirtyOrUnattestedBranch. No real host, migration, deployment or negotiated worker is implemented.</remarks>
public sealed class Phase0SchemaReadinessMockGateTests
{
    private static readonly string[] Kinds = ["dirty", "unattested", "offline", "inventory", "epoch", "protocol"];

    /// <summary>Gets the six literal fixtures in normal and reversed inventory order.</summary>
    public static IEnumerable<object[]> CorrectCases => Kinds.SelectMany(kind => new[] { new object[] { kind, false }, [kind, true] });

    /// <summary>All approved refusals preserve independently observed local state through reopen.</summary>
    /// <param name="kind">Fixture key.</param><param name="reverse">Enumeration order variation.</param>
    [Theory, MemberData(nameof(CorrectCases))]
    public void CorrectSchemaReadinessPasses(string kind, bool reverse)
    {
        var probe = new ReadinessProbe(kind, reverse);
        Phase0SchemaReadinessContract.AssertRefusal(kind, probe);
        Assert.Equal(3, probe.StateReads); Assert.Equal(3, probe.TraceReads);
        Assert.Equal(1, probe.Attempts); Assert.Equal(1, probe.Reopens);
    }

    /// <summary>Gets missing/misbound state, branch, attestation, reply and execution observations at each boundary.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var kind in Kinds)
            {
                for (var checkpoint = 0; checkpoint < 3; checkpoint++)
                {
                    foreach (var member in new[] { "Missing", "Project", "Epoch", "Fingerprint", "Binary", "Schema", "Public", "Worker", "Plugin", "Swaps", "Services", "Migrations", "Commits", "Transfers", "ProtectedEmpty", "BranchesMissing", "BranchesExtra", "BranchesDuplicate", "AttestationsMissing", "AttestationsExtra", "AttestationsDuplicate" })
                        yield return [kind, $"state:{checkpoint}:0:{member}"];
                    if (checkpoint > 0) yield return [kind, $"state:{checkpoint}:0:ProtectedChanged"];
                    for (var row = 0; row < 3; row++)
                        foreach (var member in new[] { "Node", "Name", "Head", "Schema", "Working", "Rows", "Dirty", "Open", "Ancestor" })
                            yield return [kind, $"branch:{checkpoint}:{row}:{member}"];
                    for (var row = 0; row < 2; row++)
                        foreach (var member in new[] { "Node", "Online", "Epoch", "Fingerprint", "BranchesMissing", "BranchesExtra", "BranchesWrong" })
                            yield return [kind, $"attestation:{checkpoint}:{row}:{member}"];
                    foreach (var member in new[] { "Missing", "Requests", "Reopens", "Operation", "Receipt", "Process", "ReopenReceipt" })
                        yield return [kind, $"trace:{checkpoint}:0:{member}"];
                }
                foreach (var member in new[] { "Missing", "Status", "Code", "Retryable" }) yield return [kind, "reply:0:0:" + member];
            }
        }
    }

    /// <summary>Each wrong observation must fail through the same assertions intended for real binding.</summary>
    /// <param name="kind">Fixture key.</param><param name="fault">Single deliberate corruption.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongSchemaReadinessIsRejected(string kind, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0SchemaReadinessContract.AssertRefusal(kind, new ReadinessProbe(kind, false, fault)));

    /// <summary>Literal independent response script; never calls the assertion code's expected-value factories.</summary>
    private sealed class ReadinessProbe(string kind, bool reverse, string fault = "") : IPhase0SchemaReadinessProbe
    {
        /// <summary>Gets independent state reads.</summary>
        internal int StateReads { get; private set; }
        /// <summary>Gets execution trace reads.</summary>
        internal int TraceReads { get; private set; }
        /// <summary>Gets attempted guarded commands.</summary>
        internal int Attempts { get; private set; }
        /// <summary>Gets completed scripted reopens.</summary>
        internal int Reopens { get; private set; }
        /// <inheritdoc />
        public void Prepare(string fixture) { Assert.Equal(kind, fixture); Assert.Equal(0, StateReads + TraceReads + Attempts + Reopens); }
        /// <inheritdoc />
        public Phase0SchemaReadinessReply? Attempt(string operation)
        {
            Assert.Equal(1, StateReads); Assert.Equal(1, TraceReads); Attempts++;
            Assert.Equal(kind is "dirty" or "inventory" ? "deployment.preflight" : kind == "protocol" ? "worker.negotiate" : "maintenance.contract", operation);
            return Active("reply", 0, 0) switch { "Missing" => null, "Status" => new(200, ExpectedCode, false), "Code" => new(409, "other", false), "Retryable" => new(409, ExpectedCode, true), _ => new(409, ExpectedCode, false) };
        }
        private string ExpectedCode => kind == "protocol" ? "unsupported_contract" : "maintenance_not_ready";
        /// <inheritdoc />
        public void Reopen() { Assert.Equal(2, StateReads); Assert.Equal(2, TraceReads); Assert.Equal(1, Attempts); Reopens++; }
        /// <inheritdoc />
        public Phase0SchemaReadinessState? ReadState()
        {
            var checkpoint = StateReads++;
            Phase0SchemaLocalBranch[] branches = [new("A", "main", "S1", 2, "clean-S1", "main-rows", false, true, true),
                new("B", "main", "S1", 2, "clean-S1", "main-rows", false, true, true),
                new(kind == "offline" ? "B" : "A", "feature/unpublished", kind == "protocol" ? "F2" : "F1", kind == "protocol" ? 2 : 1,
                    kind == "dirty" ? "draft-42" : "clean-feature", "offline-feature-rows", kind == "dirty", kind == "protocol", kind == "protocol")];
            for (var row = 0; row < branches.Length; row++)
            {
                var original = branches[row];
                branches[row] = Active("branch", checkpoint, row) switch
                {
                    "Node" => original with { Node = "foreign" }, "Name" => original with { Name = "wrong" }, "Head" => original with { Head = "C0" },
                    "Schema" => original with { Schema = 99 }, "Working" => original with { WorkingHash = "lost" }, "Rows" => original with { RowHash = "lost" },
                    "Dirty" => original with { Dirty = !original.Dirty }, "Open" => original with { WritesOpen = !original.WritesOpen }, "Ancestor" => original with { ExpandAncestor = !original.ExpandAncestor }, _ => original
                };
            }
            Phase0SchemaAttestation[] attestations = [new("A", true, kind == "epoch" ? 11 : 12, kind == "inventory" ? "inventory-prior" : "inventory-current",
                kind == "protocol" ? ["main", "feature/unpublished"] : ["main"]),
                new("B", kind != "offline", kind == "epoch" ? 11 : 12, kind == "inventory" ? "inventory-prior" : "inventory-current", ["main"])];
            for (var row = 0; row < attestations.Length; row++)
            {
                var original = attestations[row];
                attestations[row] = Active("attestation", checkpoint, row) switch
                {
                    "Node" => original with { Node = "foreign" }, "Online" => original with { Online = !original.Online }, "Epoch" => original with { Epoch = 99 },
                    "Fingerprint" => original with { Fingerprint = "wrong" }, "BranchesMissing" => original with { Branches = [] },
                    "BranchesExtra" => original with { Branches = [.. original.Branches, "extra"] }, "BranchesWrong" => original with { Branches = ["other"] }, _ => original
                };
            }
            var result = new Phase0SchemaReadinessState("Atlas", 12, "inventory-current", "binary-2", 2, "S1", kind == "protocol" ? 2 : 1, 1,
                reverse ? branches.Reverse().ToArray() : branches, reverse ? attestations.Reverse().ToArray() : attestations, 0, 0, 0, 0, 0, "protected-before");
            return Active("state", checkpoint, 0) switch
            {
                "Missing" => null, "Project" => result with { Project = "Other" }, "Epoch" => result with { Epoch = 13 },
                "Fingerprint" => result with { Fingerprint = "wrong" }, "Binary" => result with { Binary = "binary-3" }, "Schema" => result with { SupportedSchema = 3 },
                "Public" => result with { PublicHead = "K1" }, "Worker" => result with { WorkerContract = 99 }, "Plugin" => result with { PluginContract = 2 },
                "Swaps" => result with { Swaps = 1 }, "Services" => result with { Services = 1 }, "Migrations" => result with { Migrations = 1 },
                "Commits" => result with { Commits = 1 }, "Transfers" => result with { Transfers = 1 }, "ProtectedEmpty" => result with { Protected = null },
                "ProtectedChanged" => result with { Protected = "changed" }, "BranchesMissing" => result with { Branches = null },
                "BranchesExtra" => result with { Branches = [.. branches, branches[0]] }, "BranchesDuplicate" => result with { Branches = [branches[0], branches[0], branches[2]] },
                "AttestationsMissing" => result with { Attestations = null }, "AttestationsExtra" => result with { Attestations = [.. attestations, attestations[0]] },
                "AttestationsDuplicate" => result with { Attestations = [attestations[0], attestations[0]] }, _ => result
            };
        }
        /// <inheritdoc />
        public Phase0SchemaReadinessTrace? ReadTrace()
        {
            var checkpoint = TraceReads++;
            var operation = kind is "dirty" or "inventory" ? "deployment.preflight" : kind == "protocol" ? "worker.negotiate" : "maintenance.contract";
            var result = checkpoint == 0 ? new Phase0SchemaReadinessTrace(0, 0, "", null, "process-before", null)
                : new(1, checkpoint == 2 ? 1 : 0, operation, "request-native-receipt", checkpoint == 2 ? "process-after" : "process-before", checkpoint == 2 ? "reopen-native-receipt" : null);
            return Active("trace", checkpoint, 0) switch
            {
                "Missing" => null, "Requests" => result with { Requests = 99 }, "Reopens" => result with { Reopens = 99 }, "Operation" => result with { Operation = "wrong" },
                "Receipt" => result with { Receipt = checkpoint == 0 ? "unexpected" : null }, "Process" => result with { Process = "unchanged-or-wrong-process" },
                "ReopenReceipt" => result with { ReopenReceipt = checkpoint == 2 ? null : "unexpected" }, _ => result
            };
        }
        private string Active(string channel, int checkpoint, int row)
            => fault.StartsWith($"{channel}:{checkpoint}:{row}:", StringComparison.Ordinal) ? fault[(fault.LastIndexOf(':') + 1)..] : "";
    }
}
