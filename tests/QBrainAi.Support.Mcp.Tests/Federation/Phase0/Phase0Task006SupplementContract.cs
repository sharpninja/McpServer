namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One independently read fact, with an explicit canonical type and lossless value.</summary>
/// <param name="Path">Unique scope-qualified observation path.</param><param name="Type">Canonical scalar/vector encoding.</param>
/// <param name="Value">Exact value, with null represented by the null type rather than an empty string.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-006 B6-B9; FR-MCP-180/186/187/189; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237. Test-owned observations, not a product wire contract.</remarks>
internal sealed record Phase0Task006Fact(string Path, string Type, string Value);

/// <summary>Independent native-boundary snapshot and the retained operation evidence it addresses.</summary>
/// <param name="Boundary">Ordered fixture boundary.</param><param name="Facts">Complete exact fixture projection.</param>
/// <param name="Receipt">Binding to retained execution and independent query receipts, not a method-entry counter.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-006 B6-B9; FR-MCP-180/186/187/189; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237.</remarks>
internal sealed record Phase0Task006Frame(string Boundary, Phase0Task006Fact[]? Facts, string Receipt);

/// <summary>Command addressed to one disposable scenario; no production fault-injection API is proposed.</summary>
/// <param name="Scenario">Documented fixed fixture.</param><param name="Project">Logical fixture project.</param>
/// <param name="Branch">Selected branch.</param><param name="Request">Fixed idempotency UUID.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-006 B6-B9; FR-MCP-180/186/187/189; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237.</remarks>
internal sealed record Phase0Task006Command(string Scenario, string Project, string Branch, Guid Request);

/// <summary>Disposable experiment orchestration seam shared by mock and later native adapters.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-006 B6-B9; FR-MCP-180/186/187/189; TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237. Implementation remains behind the group barrier.</remarks>
internal interface IPhase0Task006SupplementProbe
{
    /// <summary>Creates the documented fixed fixture with separate original and target roots.</summary><param name="command">Immutable fixture address.</param>
    void Prepare(Phase0Task006Command command);
    /// <summary>Executes the fixed operation sequence and captures barriers from independent readers.</summary><param name="command">Same immutable fixture address.</param>
    void Execute(Phase0Task006Command command);
    /// <summary>Reads chronological retained evidence after the operation without deriving it from responses.</summary><returns>Independent snapshots.</returns>
    Phase0Task006Frame[]? ReadFrames();
    /// <summary>Closes all owning processes, reopens retained stores, then reads the final evidence again.</summary><returns>Reopened final snapshot.</returns>
    Phase0Task006Frame? ReopenAndRead();
}

/// <summary>Supplementary assertions for previously uncovered parts of the current Task006 definition.</summary>
/// <remarks>
/// 2026-11-19: FR-MCP-180 ac-SameCellConflictNeedsReview, ac-CleanMergeCanFailDomainValidation;
/// FR-MCP-186 ac-ContractBlockedUntilLocalBranchesAttested; FR-MCP-187 ac-SeedPhaseRejectsJoin,
/// ac-MigrationJoinPreservesCommonAncestor, ac-MigrationJoinDivergentRowRequiresReview,
/// ac-JoinCutoverRequiresReviewedMain, ac-FullProjectRecoveryPreservesAllDomains;
/// FR-MCP-189 ac-LeaseLostDuringWriteRejected, ac-CommitRetryReturnsSameHash, ac-IncomingMergeCannotRaceAdmittedEdit;
/// TR-MCP-FEDERATION-007/013/014/016; TEST-MCP-228/234/235/237. Broader body obligations are experiment cases,
/// not invented ACs. Existing detailed merge/schema/migration fixtures remain required.
/// </remarks>
internal static class Phase0Task006SupplementContract
{
    /// <summary>Gets the complete supplementary experiment inventory.</summary>
    internal static string[] Scenarios => [
        "b6-default-transaction", "b6-private-conflict-transaction", "b6-unique", "b6-foreign-key",
        "b6-not-null", "b6-check", "b6-unrelated-history",
        "b7-hub-expand", "b7-contract-ready", "b7-contract-inventory-race", "b7-worker-compatible",
        "b8-seed", "b8-join-review-required", "b8-join-reviewed", "b8-join-cutover-unresolved",
        "b8-recovery", "b8-recovery-missing-memory", "b8-recovery-missing-branch", "b8-audit-receipt-missing",
        "b9-worker-fast-forward", "b9-worker-merge", "b9-worker-resolve", "b9-worker-schema", "b9-worker-recovery",
        "b9-stale-fence", "b9-stale-preview", "b9-dirty-takeover", "b9-exclusive-owner-crash",
        "b9-commit-before-history", "b9-commit-after-history", "b9-commit-after-receipt", "b9-request-conflict"];

    /// <summary>Checks fixed inputs, every observation, chronological barriers, and independent reopen.</summary>
    /// <param name="scenario">Current approved experiment case.</param><param name="probe">Independent scripted or native observer.</param>
    internal static void AssertScenario(string scenario, IPhase0Task006SupplementProbe probe)
    {
        Assert.Contains(scenario, Scenarios);
        var command = new Phase0Task006Command(scenario, "Atlas", "main", Guid.Parse("733477c8-974b-4877-9356-3a8b2ddcb948"));
        probe.Prepare(command); probe.Execute(command);
        var frames = probe.ReadFrames(); Assert.NotNull(frames); Assert.Equal(3, frames.Length);
        var expected = Expected(scenario);
        for (var index = 0; index < frames.Length; index++)
        {
            Assert.NotNull(frames[index]);
            Assert.Equal(new[] { "before", "held", "after" }[index], frames[index].Boundary);
            Check(expected[index], frames[index], $"task006/{scenario}/{frames[index].Boundary}");
        }
        var reopened = probe.ReopenAndRead(); Assert.NotNull(reopened); Assert.Equal("reopened", reopened.Boundary);
        Check(expected[2], reopened, $"task006/{scenario}/reopened");
    }

    private static void Check(IReadOnlyDictionary<string, string> expected, Phase0Task006Frame frame, string receipt)
    {
        Assert.Equal(receipt, frame.Receipt); Assert.NotNull(frame.Facts);
        Assert.Equal(expected.Count, frame.Facts.Length);
        Assert.Equal(frame.Facts.Length, frame.Facts.Select(fact => fact.Path).Distinct(StringComparer.Ordinal).Count());
        foreach (var pair in expected)
        {
            var fact = Assert.Single(frame.Facts, fact => fact.Path == pair.Key);
            var separator = pair.Value.IndexOf(':');
            Assert.Equal(pair.Value[..separator], fact.Type); Assert.Equal(pair.Value[(separator + 1)..], fact.Value);
        }
    }

    private static Dictionary<string, string>[] Expected(string scenario)
    {
        var frames = Enumerable.Range(0, 3).Select(_ => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["project"] = "id:Atlas", ["branch"] = "id:main", ["other.rows"] = "hash:other-rows-0",
            ["other.refs"] = "hash:other-refs-0", ["source.original"] = "hash:source-0",
            ["source.retained"] = "bool:true", ["source.readOnly"] = "bool:true", ["external.dispatches"] = "int:0"
        }).ToArray();
        if (scenario.StartsWith("b6-", StringComparison.Ordinal)) Merge(scenario, frames);
        else if (scenario.StartsWith("b7-", StringComparison.Ordinal)) Schema(scenario, frames);
        else if (scenario.StartsWith("b8-", StringComparison.Ordinal)) Migration(scenario, frames);
        else Ownership(scenario, frames);
        return frames;
    }

    private static void Put(Dictionary<string, string> values, params (string Path, string Value)[] facts)
    {
        foreach (var fact in facts) values.Add(fact.Path, fact.Value);
    }

    private static void Merge(string scenario, Dictionary<string, string>[] frames)
    {
        foreach (var frame in frames)
        {
            Put(frame, ("live.head", "hash:B1"), ("public.head", "hash:A1"), ("live.rows", "hash:valid-B1"),
                ("global.automaticCommit", "int:0"), ("global.allowConflicts", "int:0"), ("global.forceTransaction", "int:0"),
                ("live.automaticCommit", "int:0"), ("live.allowConflicts", "int:0"), ("live.forceTransaction", "int:0"),
                ("fresh.allowConflicts", "int:0"), ("fresh.automaticCommit", "int:0"), ("fresh.forceTransaction", "int:0"),
                ("public.forceUpdates", "int:0"), ("live.newCommits", "int:0"));
        }
        var kind = scenario[3..];
        Put(frames[0], ("experiment", "id:" + kind), ("private.sqlCommitted", "bool:false"), ("review.pins", scenario == "b6-unrelated-history" ? "vector:B1,U1" : "vector:C0,B1,A1"));
        Put(frames[1], ("experiment", "id:" + kind), ("private.allowConflicts", scenario == "b6-private-conflict-transaction" ? "int:1" : "int:0"),
            ("private.forceTransaction", "int:0"), ("private.automaticCommit", "int:0"), ("merge.noCommit", "bool:true"), ("live.gateHeld", "bool:false"));
        Put(frames[2], ("experiment", "id:" + kind), ("review.pins", scenario == "b6-unrelated-history" ? "vector:B1,U1" : "vector:C0,B1,A1"),
            ("review.sourcesLocalRemoteReadable", "bool:true"), ("private.closed", "bool:true"), ("private.historyCommits", "int:0"));
        if (scenario == "b6-private-conflict-transaction")
            Put(frames[2], ("private.sqlCommitted", "bool:true"), ("artifact.kind", "id:cell"), ("artifact.values", "vector:low,medium,high"), ("artifact.deleted", "vector:false,false,false"));
        else if (scenario == "b6-unrelated-history")
            Put(frames[2], ("private.sqlCommitted", "bool:false"), ("commonAncestors", "vector:"), ("incoming.head", "hash:U1"), ("native.error", "diagnostic:no-common-ancestor"));
        else
            Put(frames[2], ("private.sqlCommitted", "bool:false"), ("artifact.kind", "id:" + (scenario == "b6-default-transaction" ? "cell" : kind)),
                ("native.error", "diagnostic:" + (scenario == "b6-default-transaction" ? "unresolved-conflict" : kind + "-violation")), ("rolledBack.rows", "hash:private-before"));
    }

    private static void Schema(string scenario, Dictionary<string, string>[] frames)
    {
        var ready = scenario == "b7-contract-ready"; var expand = scenario == "b7-hub-expand";
        for (var index = 0; index < 3; index++)
        {
            var frame = frames[index]; var after = index == 2;
            Put(frame, ("schema.author", "id:hub-maintenance"), ("schema.ref", "id:maintenance/schema-2"),
                ("project.efMigrations", "int:0"), ("public.schemaHead", "hash:" + (expand && after ? "S1" : "C0")),
                ("local.branchInventory", "vector:main,feature/unpublished"), ("feature.head", "hash:F1"), ("feature.rows", "hash:F1-rows"),
                ("draft.disposition", "id:explicitly-committed-before-preflight"), ("worker.contract", "int:1"), ("plugin.contract", "int:1"),
                ("epoch", "id:maintenance-E2"), ("attestation.inventory", "hash:I2"), ("attestation.branches", "vector:main,feature/unpublished"),
                ("contract.allBranchesFenced", "bool:" + (index > 0 ? "true" : "false")));
            if (expand) Put(frame, ("schema.newCommits", "int:" + (after ? "1" : "0")), ("schema.parents", after ? "vector:C0" : "vector:"), ("node.authoredSchemaCommits", "int:0"));
            else if (ready) Put(frame, ("allBranches.expandAncestry", "bool:true"), ("contract.operations", "int:" + (after ? "1" : "0")), ("contract.revalidatedUnderGate", "bool:" + (after ? "true" : "false")));
            else if (scenario == "b7-contract-inventory-race") Put(frame,
                ("current.inventory", "hash:" + (index > 0 ? "I3" : "I2")), ("restoredFeature.head", "hash:" + (index > 0 ? "Old1" : "absent")),
                ("restoredFeature.writeOpen", "bool:false"), ("contract.operations", "int:0"), ("reply", after ? "http:409 maintenance_not_ready" : "http:none"));
            else Put(frame, ("worker.negotiation", "id:" + (after ? "compatible" : "pending")), ("domain.mutations", "int:0"), ("history.commits", "int:0"));
        }
    }

    private static void Migration(string scenario, Dictionary<string, string>[] frames)
    {
        var recovery = scenario.StartsWith("b8-recovery", StringComparison.Ordinal);
        var join = scenario.StartsWith("b8-join", StringComparison.Ordinal);
        var blocked = scenario is "b8-join-review-required" or "b8-join-cutover-unresolved" or "b8-recovery-missing-memory" or "b8-recovery-missing-branch" or "b8-audit-receipt-missing";
        for (var index = 0; index < 3; index++)
        {
            var frame = frames[index]; var after = index == 2;
            Put(frame, ("manifest.migration", "id:migration-1"), ("manifest.project", "id:Atlas"), ("manifest.mode", "id:" + (recovery ? "export-project" : join ? "join" : "seed")),
                ("manifest.provider", "id:" + (recovery ? "dolt" : "sqlite")), ("manifest.source", "hash:source-0"), ("manifest.sourceSchema", "int:1"), ("manifest.targetSchema", "int:1"),
                ("manifest.domainInventory", "vector:todo,requirements,usecases,memories,portableConfig,promotedSessions,aliases"),
                ("manifest.schemaInventory", "hash:B2-projection-0"), ("manifest.typedCanonicalHash", "hash:typed-source-0"),
                ("manifest.identityAccounting", "vector:preserved=19,aliased=2,deleted=1,unaccounted=0"), ("manifest.historicalText", "hash:historical-text-0"),
                ("manifest.alias.LA.TODO-7", "id:canonical-A"), ("manifest.alias.LG.TODO-7", "id:canonical-B"),
                ("manifest.duplicateDecision", "id:explicit-keep-both"), ("manifest.divergenceRules", "vector:row-T1-title-take-reviewed,row-T2-title-keep-reviewed"),
                ("manifest.wildcardDefault", "bool:false"), ("audit.todoRows", "int:100"), ("audit.genericRows", "int:20"),
                ("audit.payloadHashes", "hash:audit-120"), ("audit.receipts", "int:" + (scenario == "b8-audit-receipt-missing" ? "119" : "120")),
                ("source.activeRouting", recovery ? "id:dolt" : "id:sqlite"), ("target.writeOpen", "bool:false"), ("public.forceUpdates", "int:0"),
                ("external.commandsReplayed", "int:0"), ("derivedIndex", "id:rebuild-required"));
            if (join)
                Put(frame, ("seed.commonRoot", "hash:C0"), ("join.import", "hash:" + (after && scenario != "b8-join-review-required" ? "J1" : "absent")),
                    ("join.ancestors", after && scenario != "b8-join-review-required" ? "vector:C0" : "vector:"),
                    ("join.sourceAccounting", "hash:" + (after && scenario != "b8-join-review-required" ? "J1-source-complete" : "pending")),
                    ("review.decision", "id:" + (scenario == "b8-join-reviewed" ? "authorized-take" : "pending")),
                    ("final.main", "hash:" + (after && scenario == "b8-join-reviewed" ? "M1" : "C0")),
                    ("final.parents", after && scenario == "b8-join-reviewed" ? "vector:C0,J1" : "vector:"),
                    ("final.domainValidation", "id:" + (after && scenario == "b8-join-reviewed" ? "valid" : "pending")));
            else if (recovery)
            {
                Put(frame, ("owner.quiesced", "bool:true"), ("export.branches", "vector:main,feature/unpublished"),
                    ("export.main.head", "hash:A1"), ("export.feature.head", "hash:F1"), ("export.feature.workingSet", "hash:W-draft"),
                    ("export.feature.T2.title", "string:draft"), ("export.historyCommits", "int:0"), ("export.doltArchive", "hash:archive-A1-F1"),
                    ("backup.control", "hash:control-Q1"), ("backup.auditOutbox", "hash:audit-Q1"), ("backup.identity", "hash:identity-Q1"),
                    ("backup.quiescence", "id:Q1"), ("backup.restoreVerified", "bool:" + (after && !blocked ? "true" : "false")),
                    ("audit.resend", "id:" + (after && !blocked ? "same-hash-receipts" : "pending")));
                foreach (var branch in new[] { "main", "feature" })
                    foreach (var domain in new[] { "todo", "requirements", "usecases", "memories", "portableConfig", "promotedSessions", "aliases" })
                        Put(frame, ($"export.{branch}.{domain}.rowsAndEdges", "hash:" + branch + "-" + domain + "-typed-identities-links-tombstones-versions"));
                Put(frame, ("export.observedBranches", "vector:" + (after && scenario == "b8-recovery-missing-branch" ? "main" : "main,feature/unpublished")),
                    ("export.memory.W1", "id:" + (after && scenario == "b8-recovery-missing-memory" ? "absent" : "present")));
            }
            else Put(frame, ("seed.root", "hash:" + (after && !blocked ? "C0" : "absent")), ("seed.roots", "int:" + (after && !blocked ? "1" : "0")),
                ("seed.verified", "bool:" + (after && !blocked ? "true" : "false")));
            Put(frame, ("verification.outcome", "id:" + (after ? blocked ? "blocked" : "verified" : "pending")),
                ("verification.error", after && blocked ? "code:migration_verification_failed" : "code:none"));
        }
    }

    private static void Ownership(string scenario, Dictionary<string, string>[] frames)
    {
        var worker = scenario.StartsWith("b9-worker-", StringComparison.Ordinal);
        var commit = scenario.StartsWith("b9-commit-", StringComparison.Ordinal);
        for (var index = 0; index < 3; index++)
        {
            var frame = frames[index]; var after = index == 2;
            var committed = commit && (after || index == 1 && scenario != "b9-commit-before-history");
            var receipted = commit && (after || index == 1 && scenario == "b9-commit-after-receipt");
            Put(frame, ("request.id", "uuid:733477c8-974b-4877-9356-3a8b2ddcb948"), ("public.head", "hash:C0"),
                ("feature.head", "hash:F1"), ("owner.session", "id:agent1"), ("owner.osIdentity", commit && index > 0 ? "id:owner-process-2" : "id:owner-process-1"),
                ("live.head", "hash:" + (committed ? "A1" : "C0")), ("history.newCommits", "int:" + (committed ? "1" : "0")),
                ("live.forceReset", "int:0"), ("otherAgent.captured", "int:0"));
            if (worker)
                Put(frame, ("worker.operation", "id:" + scenario[10..]), ("gate.admittedEdit", "bool:" + (index == 1 ? "true" : "false")),
                    ("worker.waiting", "bool:" + (index == 1 ? "true" : "false")), ("gate.ownerFence", "int64:4"),
                    ("worker.liveMutations", "int:0"), ("owner.rows", "hash:" + (after ? "draft-W2" : "rows-W1")),
                    ("worker.revalidatedWorkingSet", "hash:" + (after ? "W2" : "W1")), ("worker.outcome", "id:" + (after ? "waiting_local_work" : "pending")),
                    ("worker.directSqlLiveWrites", "int:0"));
            else if (commit)
                Put(frame, ("crash.boundary", "id:" + scenario[10..]), ("crash.oldOwnerStopped", "bool:" + (index > 0 ? "true" : "false")),
                    ("restart.owner", "id:" + (index > 0 ? "owner-process-2" : "owner-process-1")),
                    ("history.trailer.requestId", committed ? "uuid:733477c8-974b-4877-9356-3a8b2ddcb948" : "null:"),
                    ("history.parent", committed ? "hash:C0" : "null:"), ("history.result", committed ? "hash:A1" : "null:"),
                    ("history.trailer.branch", committed ? "id:main" : "null:"), ("history.trailer.payloadHash", committed ? "hash:payload-1" : "null:"),
                    ("retry.result", after ? "hash:A1" : "null:"), ("receipt.count", "int:" + (receipted ? "1" : "0")),
                    ("intent.count", "int:" + (receipted ? "1" : "0")), ("reply.acknowledged", "bool:" + (after ? "true" : "false")),
                    ("history.scannedBeforeRetry", "bool:" + (after ? "true" : "false")));
            else if (scenario == "b9-exclusive-owner-crash")
                Put(frame, ("secondOwner.admitted", "bool:" + (after ? "true" : "false")), ("firstOwner.osLockHeld", "bool:" + (after ? "false" : "true")),
                    ("firstOwner.exited", "bool:" + (after ? "true" : "false")), ("owner.unpublished", "hash:A-local"), ("owner.draft", "hash:W-draft"), ("owner.concurrentCount", "int:1"));
            else
                Put(frame, ("owner.draft", "hash:W-draft"), ("gate.ownerFence", "int64:5"),
                    ("request.session", scenario == "b9-dirty-takeover" ? "id:agent2" : "id:agent1"),
                    ("preview.expected", scenario == "b9-stale-preview" ? "hash:W1" : "hash:W2"),
                    ("preview.current", "hash:W2"), ("request.suppliedFence", scenario == "b9-stale-fence" ? "int64:4" : "int64:5"),
                    ("request.payloadHash", scenario == "b9-request-conflict" ? "hash:payload-2" : "hash:payload-1"),
                    ("receipt.payloadHash", "hash:payload-1"), ("receipt.original", "hash:receipt-1"),
                    ("reply", after ? "http:409 " + (scenario == "b9-stale-fence" ? "lease_lost" : scenario == "b9-stale-preview" ? "working_set_changed" : scenario == "b9-dirty-takeover" ? "branch_busy" : "request_id_conflict") : "http:none"));
        }
    }
}
