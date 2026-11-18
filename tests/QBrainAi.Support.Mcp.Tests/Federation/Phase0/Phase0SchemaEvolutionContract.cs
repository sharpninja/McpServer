namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Typed fixture row used to detect value loss while schema is exchanged as history.</summary>
/// <param name="Id">Stable row key.</param><param name="Title">Authored Unicode title.</param><param name="Amount">Exact decimal value.</param>
/// <param name="Created">Precise UTC time.</param><param name="Note">Nullable Unicode value.</param><param name="Added">Additive schema field.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 ac-OldSchemaUnpublishedCommitMergesAfterUpgrade/ac-FeatureBranchSchemaCatchUp.</remarks>
internal sealed record Phase0SchemaEvolutionRow(string Id, string Title, decimal Amount, DateTimeOffset Created, string? Note, string? Added);

/// <summary>Complete small-fixture branch projection, with ancestry and migration-history rows.</summary>
/// <param name="Name">Branch.</param><param name="Head">Actual run-manifest commit.</param><param name="Schema">Schema version.</param><param name="WritesOpen">Domain write admission.</param>
/// <param name="WorkingHash">Independent working-state fingerprint.</param><param name="History">Migration-history rows in this commit.</param><param name="Ancestors">Observed ancestry.</param><param name="Rows">Complete typed fixture contents.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 schema startup/catch-up criteria.</remarks>
internal sealed record Phase0SchemaEvolutionBranch(string Name, string Head, int Schema, bool WritesOpen, string WorkingHash, string[]? History, string[]? Ancestors, Phase0SchemaEvolutionRow[]? Rows);

/// <summary>Retained data conflict during feature schema catch-up.</summary>
/// <param name="Branch">Affected branch.</param><param name="State">Review state.</param><param name="Key">Row key.</param><param name="Column">Conflicting column.</param>
/// <param name="Base">Base value.</param><param name="Local">Feature value.</param><param name="Incoming">Hub schema-commit value.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186 ac-FeatureBranchSchemaCatchUp; TR-MCP-FEDERATION-013; TEST-MCP-234. No independent node schema migration is invented.</remarks>
internal sealed record Phase0SchemaEvolutionReview(string Branch, string State, string Key, string Column, string Base, string Local, string Incoming);

/// <summary>Independent branch and protected-state snapshot.</summary>
/// <param name="Project">Project.</param><param name="Binary">Loaded binary fixture identity.</param><param name="PublicHead">Remote main.</param>
/// <param name="ProjectEfMigrations">Local EF project migration executions.</param><param name="HistoryReadable">Maintenance history remains readable.</param>
/// <param name="Branches">All local branches.</param><param name="Review">Pending feature review.</param><param name="Protected">Other-project/control fingerprint.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234. Real relational/history behavior remains unproven.</remarks>
internal sealed record Phase0SchemaEvolutionState(string Project, string Binary, string PublicHead, int ProjectEfMigrations, bool HistoryReadable, Phase0SchemaEvolutionBranch[]? Branches, Phase0SchemaEvolutionReview? Review, string? Protected);

/// <summary>Independently retained completed native/HTTP operations; not method entry counts.</summary>
/// <param name="Starts">Application starts.</param><param name="Preflights">Valid clean all-branch preflights.</param><param name="Swaps">Disposable fixture binary swaps.</param>
/// <param name="MainMerges">Main schema-history merge attempts.</param><param name="FeatureMerges">Feature schema-history merge attempts.</param><param name="RejectedWrites">Unsupported writes rejected.</param>
/// <param name="Reopens">Process/store reopens.</param><param name="Receipts">Nonblank aggregate binding to retained command/process receipts.</param>
/// <remarks>2026-11-17: B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234. Real adapter must derive this independently from command/process evidence.</remarks>
internal sealed record Phase0SchemaEvolutionTrace(int Starts, int Preflights, int Swaps, int MainMerges, int FeatureMerges, int RejectedWrites, int Reopens, string? Receipts);

/// <summary>Disposable versioned startup and history catch-up boundary, with no production implementation.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234.</remarks>
internal interface IPhase0SchemaEvolutionProbe
{
    /// <summary>Creates the named independent fixture and pins C0/B1/F1/S1 identities.</summary><param name="kind">startup, unsupported, clean or conflict.</param>
    void Prepare(string kind);
    /// <summary>Starts the current binary without applying project EF migrations.</summary>
    void Start();
    /// <summary>Completes evidenced clean preflight before installing fixture binary 2.</summary>
    void UpgradeAfterPreflight();
    /// <summary>History-merges the hub schema commit into the selected branch.</summary><param name="branch">main or feature.</param>
    void MergeSchema(string branch);
    /// <summary>Attempts a domain write while the selected schema is unsupported.</summary><param name="branch">Affected branch.</param><returns>Actual refusal.</returns>
    Phase0SchemaReadinessReply? TryWrite(string branch);
    /// <summary>Reopens the disposable processes/stores without discarding history or review.</summary>
    void Reopen();
    /// <summary>Reads actual state independently of command replies.</summary><returns>Actual snapshot.</returns>
    Phase0SchemaEvolutionState? ReadState();
    /// <summary>Reads completed native operation evidence independently.</summary><returns>Actual execution trace.</returns>
    Phase0SchemaEvolutionTrace? ReadTrace();
}

/// <summary>Same mock-proven assertions later bind to real versioned startup and schema history.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 ac-VersionedProjectNotMigratedAtStartup/ac-UnsupportedSchemaRejected/ac-OldSchemaUnpublishedCommitMergesAfterUpgrade/ac-FeatureBranchSchemaCatchUp. This is partial mock evidence, not B7 acceptance.</remarks>
internal static class Phase0SchemaEvolutionContract
{
    /// <summary>Checks typed data, exact ancestry/history, branch admission and completed operations at each boundary.</summary>
    /// <param name="kind">Independent fixture key.</param><param name="probe">Scripted or later real adapter.</param>
    internal static void AssertEvolution(string kind, IPhase0SchemaEvolutionProbe probe)
    {
        Assert.Contains(kind, new[] { "startup", "unsupported", "clean", "conflict" });
        probe.Prepare(kind); var baseline = Check(kind, 0, probe);
        probe.Start(); Check(kind, 1, probe, baseline);
        if (kind == "startup") { probe.Start(); Check(kind, 2, probe, baseline); return; }
        if (kind == "unsupported") { AssertDenied(probe.TryWrite("main")); Check(kind, 2, probe, baseline); return; }
        probe.UpgradeAfterPreflight(); Check(kind, 2, probe, baseline);
        probe.MergeSchema("main"); Check(kind, 3, probe, baseline);
        AssertDenied(probe.TryWrite("feature")); Check(kind, 4, probe, baseline);
        probe.MergeSchema("feature"); Check(kind, 5, probe, baseline);
        probe.Reopen(); Check(kind, 6, probe, baseline);
    }

    private static string Check(string kind, int stage, IPhase0SchemaEvolutionProbe probe, string? baseline = null)
    {
        var state = probe.ReadState(); Assert.NotNull(state);
        var upgrade = kind is "clean" or "conflict"; var mainMerged = upgrade && stage >= 3; var featureMerged = kind == "clean" && stage >= 5;
        Assert.Equal("Atlas", state.Project); Assert.Equal(upgrade && stage >= 2 ? "binary-2" : "binary-1", state.Binary);
        Assert.Equal("S1", state.PublicHead); Assert.Equal(0, state.ProjectEfMigrations); Assert.True(state.HistoryReadable);
        Assert.False(string.IsNullOrWhiteSpace(state.Protected)); if (baseline is not null) Assert.Equal(baseline, state.Protected);
        Assert.NotNull(state.Branches); Assert.Equal(2, state.Branches.Length);
        foreach (var name in new[] { "main", "feature" })
        {
            var branch = Assert.Single(state.Branches, value => value.Name == name);
            var merged = name == "main" ? mainMerged : featureMerged;
            var schema = kind == "unsupported" || merged ? 2 : 1;
            var head = kind == "unsupported" ? (name == "main" ? "S1" : "F2") : merged ? (name == "main" ? "M1" : "F2") : name == "main" ? "B1" : "F1";
            Assert.Equal(head, branch.Head); Assert.Equal(schema, branch.Schema); Assert.Equal("clean-" + head, branch.WorkingHash);
            Assert.Equal(kind != "unsupported" && (!upgrade || stage < 2 || merged), branch.WritesOpen);
            Assert.NotNull(branch.History); Assert.Equal(schema == 2 ? new[] { "schema1", "schema2" } : ["schema1"], branch.History);
            string[] ancestry = kind == "unsupported" ? (name == "main" ? ["C0", "S1"] : ["C0", "S1", "F2"])
                : merged ? (name == "main" ? ["C0", "B1", "S1", "M1"] : ["C0", "F1", "S1", "F2"]) : ["C0", head];
            Assert.NotNull(branch.Ancestors); Assert.Equal(ancestry.Order(StringComparer.Ordinal), branch.Ancestors.Order(StringComparer.Ordinal));
            Assert.NotNull(branch.Rows); Assert.Equal(2, branch.Rows.Length);
            var moment = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(1234567);
            var firstTitle = name == "main" && kind != "unsupported" ? "offline-κόσμος" : "base-title";
            var secondTitle = name == "feature" && kind != "unsupported" ? "feature-title" : kind == "conflict" && mainMerged ? "hub-title" : "legacy-feature";
            Phase0SchemaEvolutionRow[] expected = [new("TODO-007", firstTitle, 12.34m, moment, null, schema == 2 ? "expanded" : null),
                new("TODO-008", secondTitle, 56.78m, moment, "résumé", schema == 2 ? "expanded" : null)];
            Assert.Equal(expected.OrderBy(row => row.Id, StringComparer.Ordinal), branch.Rows.OrderBy(row => row.Id, StringComparer.Ordinal));
        }
        if (kind == "conflict" && stage >= 5) Assert.Equal(new("feature", "needs_review", "TODO-008", "Title", "legacy-feature", "feature-title", "hub-title"), state.Review);
        else Assert.Null(state.Review);
        var trace = probe.ReadTrace(); Assert.NotNull(trace); Assert.False(string.IsNullOrWhiteSpace(trace.Receipts));
        Assert.Equal(kind == "startup" ? stage : stage > 0 ? 1 : 0, trace.Starts);
        Assert.Equal(upgrade && stage >= 2 ? 1 : 0, trace.Preflights); Assert.Equal(upgrade && stage >= 2 ? 1 : 0, trace.Swaps);
        Assert.Equal(mainMerged ? 1 : 0, trace.MainMerges); Assert.Equal(upgrade && stage >= 5 ? 1 : 0, trace.FeatureMerges);
        Assert.Equal((kind == "unsupported" && stage == 2) || (upgrade && stage >= 4) ? 1 : 0, trace.RejectedWrites); Assert.Equal(upgrade && stage == 6 ? 1 : 0, trace.Reopens);
        return state.Protected;
    }

    private static void AssertDenied(Phase0SchemaReadinessReply? reply)
    { Assert.NotNull(reply); Assert.Equal(409, reply.Status); Assert.Equal("incompatible_schema", reply.Code); Assert.False(reply.Retryable); }
}
