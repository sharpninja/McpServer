namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripts and deliberate corruptions validate Task006 supplementary assertions.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-006 B6-B9; FR-MCP-180/186/187/189; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237. Mock evidence only. Existing detailed fixtures remain mandatory.</remarks>
public sealed class Phase0Task006SupplementMockGateTests
{
    private static readonly string[] Cases = [
        "b6-default-transaction", "b6-private-conflict-transaction", "b6-unique", "b6-foreign-key", "b6-not-null", "b6-check", "b6-unrelated-history",
        "b7-hub-expand", "b7-contract-ready", "b7-contract-inventory-race", "b7-worker-compatible",
        "b8-seed", "b8-join-review-required", "b8-join-reviewed", "b8-join-cutover-unresolved", "b8-recovery", "b8-recovery-missing-memory", "b8-recovery-missing-branch", "b8-audit-receipt-missing",
        "b9-worker-fast-forward", "b9-worker-merge", "b9-worker-resolve", "b9-worker-schema", "b9-worker-recovery", "b9-stale-fence", "b9-stale-preview", "b9-dirty-takeover", "b9-exclusive-owner-crash", "b9-commit-before-history", "b9-commit-after-history", "b9-commit-after-receipt", "b9-request-conflict"];

    /// <summary>Gets independently listed cases with both row enumeration orders.</summary>
    public static IEnumerable<object[]> CorrectCases => from name in Cases from reverse in new[] { false, true } select new object[] { name, reverse };

    /// <summary>Gets every observed member's type/value/omission controls and structural boundary controls.</summary>
    public static IEnumerable<object[]> WrongCases
    {
        get
        {
            foreach (var name in Cases)
            {
                for (var boundary = 0; boundary < 4; boundary++)
                {
                    foreach (var fact in Script(name, Math.Min(boundary, 2)))
                        foreach (var mutation in new[] { "value", "type", "omit" })
                            yield return [name, boundary, fact.Path, mutation];
                    foreach (var mutation in new[] { "missing-facts", "extra", "duplicate", "receipt", "foreign-receipt", "boundary", "null-frame" })
                        yield return [name, boundary, "", mutation];
                }
                foreach (var mutation in new[] { "null-frames", "short-frames", "extra-frames", "reorder-frames" }) yield return [name, -1, "", mutation];
            }
        }
    }

    /// <summary>Independent correct scripts satisfy the fixed contracts and the complete call sequence.</summary><param name="name">Fixture.</param><param name="reverse">Enumeration permutation.</param>
    [Theory, MemberData(nameof(CorrectCases))]
    public void CorrectIndependentScriptsPass(string name, bool reverse)
    {
        var probe = new Probe(name, reverse);
        Phase0Task006SupplementContract.AssertScenario(name, probe);
        Assert.Equal(new[] { "prepare", "execute", "read", "reopen" }, probe.Calls);
        Assert.False(probe.FaultInjected);
    }

    /// <summary>Each injected wrong fact must reach and fail the unchanged assertion path.</summary><param name="name">Fixture.</param><param name="boundary">Observation boundary.</param><param name="path">Fact.</param><param name="mutation">Corruption.</param>
    [Theory, MemberData(nameof(WrongCases))]
    public void WrongIndependentScriptsAreRejected(string name, int boundary, string path, string mutation)
    {
        var probe = new Probe(name, false, boundary, path, mutation);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0Task006SupplementContract.AssertScenario(name, probe));
        Assert.True(probe.FaultInjected, $"The requested control did not execute: {name}/{boundary}/{path}/{mutation}");
    }

    /// <summary>The assertion inventory and the independently authored script inventory cannot silently diverge.</summary>
    [Fact]
    public void CurrentExperimentInventoryMatchesIndependentScripts()
        => Assert.Equal(Cases.Order(StringComparer.Ordinal), Phase0Task006SupplementContract.Scenarios.Order(StringComparer.Ordinal));

    /// <summary>Separate literal fixture oracle, never reading or calling the assertion contract's expected values.</summary>
    private static Phase0Task006Fact[] Script(string name, int stage)
    {
        var text = new List<string>
        {
            "project|id|Atlas", "branch|id|main", "other.rows|hash|other-rows-0", "other.refs|hash|other-refs-0", "source.original|hash|source-0",
            "source.retained|bool|true", "source.readOnly|bool|true", "external.dispatches|int|0"
        };
        void Add(string path, string type, string value) => text.Add(path + "|" + type + "|" + value);
        void Rows(string rows) => text.AddRange(rows.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        var final = stage == 2;
        if (name.StartsWith("b6-", StringComparison.Ordinal))
        {
            Rows("""
                live.head|hash|B1
                public.head|hash|A1
                live.rows|hash|valid-B1
                global.automaticCommit|int|0
                global.allowConflicts|int|0
                global.forceTransaction|int|0
                live.automaticCommit|int|0
                live.allowConflicts|int|0
                live.forceTransaction|int|0
                fresh.allowConflicts|int|0
                fresh.automaticCommit|int|0
                fresh.forceTransaction|int|0
                public.forceUpdates|int|0
                live.newCommits|int|0
                """);
            Add("experiment", "id", name[3..]);
            if (stage == 1)
            {
                Add("private.allowConflicts", "int", name == "b6-private-conflict-transaction" ? "1" : "0");
                Rows("private.forceTransaction|int|0\nprivate.automaticCommit|int|0\nmerge.noCommit|bool|true\nlive.gateHeld|bool|false");
            }
            else
            {
                Add("review.pins", "vector", name.EndsWith("unrelated-history", StringComparison.Ordinal) ? "B1,U1" : "C0,B1,A1");
                Add("private.sqlCommitted", "bool", final && name == "b6-private-conflict-transaction" ? "true" : "false");
                if (final)
                {
                    Rows("review.sourcesLocalRemoteReadable|bool|true\nprivate.closed|bool|true\nprivate.historyCommits|int|0");
                    switch (name)
                    {
                        case "b6-private-conflict-transaction": Rows("artifact.kind|id|cell\nartifact.values|vector|low,medium,high\nartifact.deleted|vector|false,false,false"); break;
                        case "b6-unrelated-history": Rows("commonAncestors|vector|\nincoming.head|hash|U1\nnative.error|diagnostic|no-common-ancestor"); break;
                        default:
                            Add("artifact.kind", "id", name == "b6-default-transaction" ? "cell" : name[3..]);
                            Add("native.error", "diagnostic", name == "b6-default-transaction" ? "unresolved-conflict" : name[3..] + "-violation");
                            Rows("rolledBack.rows|hash|private-before"); break;
                    }
                }
            }
        }
        else if (name.StartsWith("b7-", StringComparison.Ordinal))
        {
            Rows("""
                schema.author|id|hub-maintenance
                schema.ref|id|maintenance/schema-2
                project.efMigrations|int|0
                local.branchInventory|vector|main,feature/unpublished
                feature.head|hash|F1
                feature.rows|hash|F1-rows
                draft.disposition|id|explicitly-committed-before-preflight
                worker.contract|int|1
                plugin.contract|int|1
                epoch|id|maintenance-E2
                attestation.inventory|hash|I2
                attestation.branches|vector|main,feature/unpublished
                """);
            Add("public.schemaHead", "hash", final && name == "b7-hub-expand" ? "S1" : "C0");
            Add("contract.allBranchesFenced", "bool", stage == 0 ? "false" : "true");
            switch (name)
            {
                case "b7-hub-expand": Add("schema.newCommits", "int", final ? "1" : "0"); Add("schema.parents", "vector", final ? "C0" : ""); Rows("node.authoredSchemaCommits|int|0"); break;
                case "b7-contract-ready": Rows("allBranches.expandAncestry|bool|true"); Add("contract.operations", "int", final ? "1" : "0"); Add("contract.revalidatedUnderGate", "bool", final ? "true" : "false"); break;
                case "b7-contract-inventory-race": Add("current.inventory", "hash", stage == 0 ? "I2" : "I3"); Add("restoredFeature.head", "hash", stage == 0 ? "absent" : "Old1"); Rows("restoredFeature.writeOpen|bool|false\ncontract.operations|int|0"); Add("reply", "http", final ? "409 maintenance_not_ready" : "none"); break;
                default: Add("worker.negotiation", "id", final ? "compatible" : "pending"); Rows("domain.mutations|int|0\nhistory.commits|int|0"); break;
            }
        }
        else if (name.StartsWith("b8-", StringComparison.Ordinal))
        {
            var recovery = name.Contains("recovery", StringComparison.Ordinal);
            var join = name.Contains("join", StringComparison.Ordinal);
            var invalid = name is "b8-join-review-required" or "b8-join-cutover-unresolved" or "b8-recovery-missing-memory" or "b8-recovery-missing-branch" or "b8-audit-receipt-missing";
            Rows("""
                manifest.migration|id|migration-1
                manifest.project|id|Atlas
                manifest.source|hash|source-0
                manifest.sourceSchema|int|1
                manifest.targetSchema|int|1
                manifest.domainInventory|vector|todo,requirements,usecases,memories,portableConfig,promotedSessions,aliases
                manifest.schemaInventory|hash|B2-projection-0
                manifest.typedCanonicalHash|hash|typed-source-0
                manifest.identityAccounting|vector|preserved=19,aliased=2,deleted=1,unaccounted=0
                manifest.historicalText|hash|historical-text-0
                manifest.alias.LA.TODO-7|id|canonical-A
                manifest.alias.LG.TODO-7|id|canonical-B
                manifest.duplicateDecision|id|explicit-keep-both
                manifest.divergenceRules|vector|row-T1-title-take-reviewed,row-T2-title-keep-reviewed
                manifest.wildcardDefault|bool|false
                audit.todoRows|int|100
                audit.genericRows|int|20
                audit.payloadHashes|hash|audit-120

                target.writeOpen|bool|false
                public.forceUpdates|int|0
                external.commandsReplayed|int|0
                derivedIndex|id|rebuild-required
                """);
            Add("manifest.mode", "id", recovery ? "export-project" : join ? "join" : "seed");
            Add("manifest.provider", "id", recovery ? "dolt" : "sqlite");
            Add("source.activeRouting", "id", recovery ? "dolt" : "sqlite");
            Add("audit.receipts", "int", name == "b8-audit-receipt-missing" ? "119" : "120");
            if (join)
            {
                var imported = final && name != "b8-join-review-required";
                Rows("seed.commonRoot|hash|C0"); Add("join.import", "hash", imported ? "J1" : "absent"); Add("join.ancestors", "vector", imported ? "C0" : "");
                Add("join.sourceAccounting", "hash", imported ? "J1-source-complete" : "pending"); Add("review.decision", "id", name == "b8-join-reviewed" ? "authorized-take" : "pending");
                Add("final.main", "hash", final && name == "b8-join-reviewed" ? "M1" : "C0"); Add("final.parents", "vector", final && name == "b8-join-reviewed" ? "C0,J1" : "");
                Add("final.domainValidation", "id", final && name == "b8-join-reviewed" ? "valid" : "pending");
            }
            else if (recovery)
            {
                Rows("""
                    owner.quiesced|bool|true
                    export.branches|vector|main,feature/unpublished
                    export.main.head|hash|A1
                    export.feature.head|hash|F1
                    export.feature.workingSet|hash|W-draft
                    export.feature.T2.title|string|draft
                    export.historyCommits|int|0
                    export.doltArchive|hash|archive-A1-F1
                    backup.control|hash|control-Q1
                    backup.auditOutbox|hash|audit-Q1
                    backup.identity|hash|identity-Q1
                    backup.quiescence|id|Q1
                    """);
                Add("backup.restoreVerified", "bool", final && !invalid ? "true" : "false"); Add("audit.resend", "id", final && !invalid ? "same-hash-receipts" : "pending");
                foreach (var branch in new[] { "main", "feature" })
                    foreach (var domain in new[] { "todo", "requirements", "usecases", "memories", "portableConfig", "promotedSessions", "aliases" })
                        Add($"export.{branch}.{domain}.rowsAndEdges", "hash", $"{branch}-{domain}-typed-identities-links-tombstones-versions");
                Add("export.observedBranches", "vector", final && name == "b8-recovery-missing-branch" ? "main" : "main,feature/unpublished");
                Add("export.memory.W1", "id", final && name == "b8-recovery-missing-memory" ? "absent" : "present");
            }
            else
            {
                Add("seed.root", "hash", final && !invalid ? "C0" : "absent"); Add("seed.roots", "int", final && !invalid ? "1" : "0"); Add("seed.verified", "bool", final && !invalid ? "true" : "false");
            }
            Add("verification.outcome", "id", !final ? "pending" : invalid ? "blocked" : "verified"); Add("verification.error", "code", final && invalid ? "migration_verification_failed" : "none");
        }
        else
        {
            var worker = name.StartsWith("b9-worker-", StringComparison.Ordinal);
            var commit = name.StartsWith("b9-commit-", StringComparison.Ordinal);
            var hasHistory = commit && (final || stage == 1 && name != "b9-commit-before-history");
            var hasReceipt = commit && (final || stage == 1 && name == "b9-commit-after-receipt");
            Rows("""
                request.id|uuid|733477c8-974b-4877-9356-3a8b2ddcb948
                public.head|hash|C0
                feature.head|hash|F1
                owner.session|id|agent1

                live.forceReset|int|0
                otherAgent.captured|int|0
                """);
            Add("owner.osIdentity", "id", commit && stage > 0 ? "owner-process-2" : "owner-process-1");
            Add("live.head", "hash", hasHistory ? "A1" : "C0"); Add("history.newCommits", "int", hasHistory ? "1" : "0");
            if (worker)
            {
                Add("worker.operation", "id", name[10..]); Add("gate.admittedEdit", "bool", stage == 1 ? "true" : "false"); Add("worker.waiting", "bool", stage == 1 ? "true" : "false");
                Rows("gate.ownerFence|int64|4\nworker.liveMutations|int|0\nworker.directSqlLiveWrites|int|0"); Add("owner.rows", "hash", final ? "draft-W2" : "rows-W1");
                Add("worker.revalidatedWorkingSet", "hash", final ? "W2" : "W1"); Add("worker.outcome", "id", final ? "waiting_local_work" : "pending");
            }
            else if (commit)
            {
                Add("crash.boundary", "id", name[10..]); Add("crash.oldOwnerStopped", "bool", stage == 0 ? "false" : "true"); Add("restart.owner", "id", stage == 0 ? "owner-process-1" : "owner-process-2");
                Add("history.trailer.requestId", hasHistory ? "uuid" : "null", hasHistory ? "733477c8-974b-4877-9356-3a8b2ddcb948" : "");
                Add("history.parent", hasHistory ? "hash" : "null", hasHistory ? "C0" : ""); Add("history.result", hasHistory ? "hash" : "null", hasHistory ? "A1" : "");
                Add("history.trailer.branch", hasHistory ? "id" : "null", hasHistory ? "main" : ""); Add("history.trailer.payloadHash", hasHistory ? "hash" : "null", hasHistory ? "payload-1" : "");
                Add("retry.result", final ? "hash" : "null", final ? "A1" : ""); Add("receipt.count", "int", hasReceipt ? "1" : "0"); Add("intent.count", "int", hasReceipt ? "1" : "0");
                Add("reply.acknowledged", "bool", final ? "true" : "false"); Add("history.scannedBeforeRetry", "bool", final ? "true" : "false");
            }
            else if (name == "b9-exclusive-owner-crash")
            {
                Add("secondOwner.admitted", "bool", final ? "true" : "false"); Add("firstOwner.osLockHeld", "bool", final ? "false" : "true"); Add("firstOwner.exited", "bool", final ? "true" : "false");
                Rows("owner.unpublished|hash|A-local\nowner.draft|hash|W-draft\nowner.concurrentCount|int|1");
            }
            else
            {
                Rows("owner.draft|hash|W-draft\ngate.ownerFence|int64|5\npreview.current|hash|W2\nreceipt.payloadHash|hash|payload-1\nreceipt.original|hash|receipt-1");
                Add("request.session", "id", name == "b9-dirty-takeover" ? "agent2" : "agent1");
                Add("preview.expected", "hash", name == "b9-stale-preview" ? "W1" : "W2");
                Add("request.suppliedFence", "int64", name == "b9-stale-fence" ? "4" : "5");
                Add("request.payloadHash", "hash", name == "b9-request-conflict" ? "payload-2" : "payload-1");
                var error = name switch { "b9-stale-fence" => "lease_lost", "b9-stale-preview" => "working_set_changed", "b9-dirty-takeover" => "branch_busy", _ => "request_id_conflict" };
                Add("reply", "http", final ? "409 " + error : "none");
            }
        }
        return text.Select(row => row.Split('|', 3)).Select(parts => new Phase0Task006Fact(parts[0], parts[1], parts[2])).ToArray();
    }

    /// <summary>Scripted external observation source with independent counters and one precisely targeted corruption.</summary>
    private sealed class Probe(string name, bool reverse, int wrongBoundary = -2, string wrongPath = "", string mutation = "") : IPhase0Task006SupplementProbe
    {
        /// <summary>Gets actual ordered seam interactions.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Gets whether the requested wrong observation was returned.</summary>
        internal bool FaultInjected { get; private set; }
        /// <inheritdoc />
        public void Prepare(Phase0Task006Command command) { Address(command); Calls.Add("prepare"); }
        /// <inheritdoc />
        public void Execute(Phase0Task006Command command) { Address(command); Assert.Equal(new[] { "prepare" }, Calls); Calls.Add("execute"); }
        /// <inheritdoc />
        public Phase0Task006Frame[]? ReadFrames()
        {
            Assert.Equal(new[] { "prepare", "execute" }, Calls); Calls.Add("read");
            var frames = new[] { Frame(0), Frame(1), Frame(2) };
            if (wrongBoundary != -1) return frames;
            FaultInjected = true;
            return mutation switch { "null-frames" => null, "short-frames" => frames[..2], "extra-frames" => [..frames, frames[2]], "reorder-frames" => [frames[1], frames[0], frames[2]], _ => throw new InvalidOperationException(mutation) };
        }
        /// <inheritdoc />
        public Phase0Task006Frame? ReopenAndRead() { Assert.Equal(new[] { "prepare", "execute", "read" }, Calls); Calls.Add("reopen"); return Frame(3); }
        private void Address(Phase0Task006Command command)
        {
            Assert.Equal(name, command.Scenario); Assert.Equal("Atlas", command.Project); Assert.Equal("main", command.Branch);
            Assert.Equal(Guid.Parse("733477c8-974b-4877-9356-3a8b2ddcb948"), command.Request);
        }
        private Phase0Task006Frame Frame(int stage)
        {
            var boundary = new[] { "before", "held", "after", "reopened" }[stage];
            var facts = Script(name, Math.Min(stage, 2));
            if (reverse) facts = facts.Reverse().ToArray();
            var frame = new Phase0Task006Frame(boundary, facts, $"task006/{name}/{boundary}");
            if (stage != wrongBoundary) return frame;
            FaultInjected = true;
            var target = Array.FindIndex(facts, fact => fact.Path == wrongPath);
            switch (mutation)
            {
                case "value": Assert.True(target >= 0); facts[target] = facts[target] with { Value = facts[target].Value + "-corrupt" }; break;
                case "type": Assert.True(target >= 0); facts[target] = facts[target] with { Type = "wrong-type" }; break;
                case "omit": Assert.True(target >= 0); return frame with { Facts = facts.Where(fact => fact.Path != wrongPath).ToArray() };
                case "missing-facts": return frame with { Facts = null };
                case "extra": return frame with { Facts = [..facts, new("unaccounted", "id", "foreign")] };
                case "duplicate": return frame with { Facts = [facts[0], ..facts.Skip(2), facts[0]] };
                case "receipt": return frame with { Receipt = "" };
                case "foreign-receipt": return frame with { Receipt = "task006/other/after" };
                case "boundary": return frame with { Boundary = "wrong-boundary" };
                case "null-frame": return null!;
                default: throw new InvalidOperationException(mutation);
            }
            return frame;
        }
    }
}
