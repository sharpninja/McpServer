namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Small immutable source-accounting fixture, not the full B2 migration manifest.</summary>
/// <param name="Migration">Migration identity.</param><param name="Mode">Seed mode.</param><param name="Project">Project.</param><param name="Provider">Original provider.</param>
/// <param name="Source">Frozen source byte hash.</param><param name="SourceSchema">Original schema.</param><param name="TargetSchema">Target schema.</param>
/// <param name="TypedHash">Canonical source rows hash.</param><param name="Destination">Verified immutable destination when available.</param>
/// <param name="Tables">Complete small-fixture table counts.</param><param name="Aliases">Explicit empty alias decisions for this sample.</param><param name="AuditCount">Zero audit rows in this sample.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B8; FR-MCP-187/TR-MCP-FEDERATION-014/TEST-MCP-235. Whole-project/audit/join/export accounting remains pending.</remarks>
internal sealed record Phase0MigrationSafetyManifest(string Migration, string Mode, string Project, string Provider, string Source, int SourceSchema, int TargetSchema,
    string TypedHash, string? Destination, IReadOnlyDictionary<string, int>? Tables, string[]? Aliases, int AuditCount);

/// <summary>Independent durable import-journal row.</summary>
/// <param name="Batch">Stable keyset batch.</param><param name="Source">Bound source hash.</param><param name="Rows">Committed rows.</param><param name="Applications">Actual durable applications.</param>
/// <remarks>2026-11-17: FR-MCP-187 ac-ResumeAfterBatchCommit/ac-ChangedSnapshotRejected; TR-MCP-FEDERATION-014; TEST-MCP-235.</remarks>
internal sealed record Phase0MigrationSafetyBatch(string Batch, string Source, int Rows, int Applications);

/// <summary>Independent full small-fixture readback and protected source/routing state.</summary>
/// <param name="Manifest">Retained manifest.</param><param name="CurrentSource">Currently selected source hash.</param>
/// <param name="RetainedSource">Original retained snapshot byte hash.</param><param name="SourcePresent">Original exists.</param><param name="SourceReadOnly">Original is read-only.</param>
/// <param name="Routing">Current provider routing.</param><param name="WritesOpen">Target write admission.</param><param name="Head">Immutable destination head or absent.</param>
/// <param name="TargetHash">Canonical observed target row hash.</param><param name="Rows">Complete typed fixture rows.</param><param name="Journal">Complete durable batches.</param>
/// <param name="Protected">Other-project and operational fingerprints.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B8; FR-MCP-187/TR-MCP-FEDERATION-014/TEST-MCP-235 migration resume/verification/retention criteria.</remarks>
internal sealed record Phase0MigrationSafetyState(Phase0MigrationSafetyManifest? Manifest, string CurrentSource, string RetainedSource, bool SourcePresent, bool SourceReadOnly,
    string Routing, bool WritesOpen, string? Head, string TargetHash, Phase0FixtureRow[]? Rows, Phase0MigrationSafetyBatch[]? Journal, string? Protected);

/// <summary>Actual response fields; empty code denotes the fixture's ordinary successful response.</summary>
/// <param name="Status">HTTP status.</param><param name="Code">Canonical refusal code, or empty on success.</param><param name="Retryable">Retry flag.</param>
/// <remarks>2026-11-17: FR-MCP-187/TR-MCP-FEDERATION-014/TEST-MCP-235. This internal observation is not a public DTO.</remarks>
internal sealed record Phase0MigrationSafetyReply(int Status, string Code, bool Retryable);

/// <summary>Completed operations independently derived from retained native evidence at real binding.</summary>
/// <param name="Crashes">Crash/reopen checkpoints.</param><param name="Resumes">Resume requests.</param><param name="Cutovers">Fixture cutover requests.</param>
/// <param name="Writes">Post-cutover SQL saves.</param><param name="Reopens">Reopens including crash recovery.</param><param name="Receipts">Native/HTTP receipt binding.</param>
/// <remarks>2026-11-17: FR-MCP-187/TR-MCP-FEDERATION-014/TEST-MCP-235; method-entry counters are insufficient for real proof.</remarks>
internal sealed record Phase0MigrationSafetyTrace(int Crashes, int Resumes, int Cutovers, int Writes, int Reopens, string? Receipts);

/// <summary>Internal disposable migration safety boundary; no production implementation.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B8; FR-MCP-187/TR-MCP-FEDERATION-014/TEST-MCP-235.</remarks>
internal interface IPhase0MigrationSafetyProbe
{
    /// <summary>Creates the selected small fixture and pins real source/hash aliases.</summary><param name="provider">Source provider.</param><param name="kind">resume, changed, verify or retention.</param>
    void Prepare(string provider, string kind);
    /// <summary>Kills/reopens after batch1 commit but before acknowledgement.</summary>
    void CrashAfterBatchCommit();
    /// <summary>Resumes or retries using the original immutable manifest.</summary><returns>Actual result.</returns>
    Phase0MigrationSafetyReply? Resume();
    /// <summary>Requests routing switch only in the disposable fixture.</summary><returns>Actual result.</returns>
    Phase0MigrationSafetyReply? Cutover();
    /// <summary>Saves new T3 to the disposable Dolt working set without making a history commit.</summary>
    void WritePostCutover();
    /// <summary>Reopens the actual disposable process/stores.</summary>
    void Reopen();
    /// <summary>Reads state independently of responses.</summary><returns>Actual snapshot.</returns>
    Phase0MigrationSafetyState? ReadState();
    /// <summary>Reads completed operations independently.</summary><returns>Actual execution receipts.</returns>
    Phase0MigrationSafetyTrace? ReadTrace();
}

/// <summary>Mock-proven assertions later reused unchanged by native migration safety adapters.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B8; FR-MCP-187/TR-MCP-FEDERATION-014/TEST-MCP-235 ac-ResumeAfterBatchCommit/ac-ChangedSnapshotRejected/ac-FailedVerifyCannotCutover/ac-SourceRetainedAfterCutover. Partial sample only.</remarks>
internal static class Phase0MigrationSafetyContract
{
    /// <summary>Checks each observable boundary, including crash recovery and retry.</summary>
    /// <param name="provider">Supported original source provider.</param><param name="kind">Fixture key.</param><param name="probe">Scripted or later native adapter.</param>
    internal static void AssertSafety(string provider, string kind, IPhase0MigrationSafetyProbe probe)
    {
        Assert.Contains(provider, new[] { "sqlite", "postgresql", "sqlserver" });
        Assert.Contains(kind, new[] { "resume", "changed", "verify", "retention" });
        probe.Prepare(provider, kind); var baseline = Check(provider, kind, 0, probe);
        if (kind == "resume")
        {
            probe.CrashAfterBatchCommit(); Check(provider, kind, 1, probe, baseline);
            Reply(probe.Resume(), 200, ""); Check(provider, kind, 2, probe, baseline);
            Reply(probe.Resume(), 200, ""); Check(provider, kind, 3, probe, baseline);
            probe.Reopen(); Check(provider, kind, 4, probe, baseline); return;
        }
        if (kind == "retention")
        {
            Reply(probe.Cutover(), 200, ""); Check(provider, kind, 1, probe, baseline);
            probe.WritePostCutover(); Check(provider, kind, 2, probe, baseline);
            probe.Reopen(); Check(provider, kind, 3, probe, baseline); return;
        }
        var code = kind == "changed" ? "migration_source_changed" : "migration_verification_failed";
        Reply(kind == "changed" ? probe.Resume() : probe.Cutover(), 409, code); Check(provider, kind, 1, probe, baseline);
        probe.Reopen(); Check(provider, kind, 2, probe, baseline);
        Reply(kind == "changed" ? probe.Resume() : probe.Cutover(), 409, code); Check(provider, kind, 3, probe, baseline);
    }

    private static string Check(string provider, string kind, int stage, IPhase0MigrationSafetyProbe probe, string? baseline = null)
    {
        var state = probe.ReadState(); Assert.NotNull(state); var manifest = state.Manifest; Assert.NotNull(manifest);
        Assert.Equal("migration-1", manifest.Migration); Assert.Equal("seed", manifest.Mode); Assert.Equal("Atlas", manifest.Project);
        Assert.Equal(provider, manifest.Provider); Assert.Equal("S0", manifest.Source); Assert.Equal(1, manifest.SourceSchema); Assert.Equal(1, manifest.TargetSchema);
        Assert.Equal("typed-S0", manifest.TypedHash); Assert.Equal(kind == "retention" ? "C0" : null, manifest.Destination);
        Assert.NotNull(manifest.Tables); Assert.Single(manifest.Tables); Assert.Contains("TypedRows", manifest.Tables.Keys); Assert.Equal(2, manifest.Tables["TypedRows"]);
        Assert.NotNull(manifest.Aliases); Assert.Empty(manifest.Aliases); Assert.Equal(0, manifest.AuditCount);
        Assert.Equal(kind == "changed" ? "S1" : "S0", state.CurrentSource); Assert.Equal("S0", state.RetainedSource); Assert.True(state.SourcePresent); Assert.True(state.SourceReadOnly);
        Assert.Equal(kind == "retention" && stage > 0 ? "dolt" : provider, state.Routing); Assert.Equal(kind == "retention" && stage > 0, state.WritesOpen);
        Assert.Equal(kind == "retention" ? "C0" : null, state.Head);
        var complete = kind is "verify" or "retention" || (kind == "resume" && stage >= 2);
        var postWrite = kind == "retention" && stage >= 2;
        Assert.Equal(kind == "verify" ? "typed-corrupt" : postWrite ? "typed-T3" : complete ? "typed-S0" : "typed-batch1", state.TargetHash);
        var moment = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(1234567);
        var expected = new List<Phase0FixtureRow> { new("T1", kind == "verify" ? "wrong-κόσμος" : "seed-κόσμος", 12.34m, moment, null) };
        if (complete) expected.Add(new("T2", "retained", 56.78m, moment, "résumé"));
        if (postWrite) expected.Add(new("T3", "post-cutover", 90.12m, moment, ""));
        Assert.NotNull(state.Rows); Assert.Equal(expected.OrderBy(row => row.Id, StringComparer.Ordinal), state.Rows.OrderBy(row => row.Id, StringComparer.Ordinal));
        foreach (var row in expected)
        {
            var actual = Assert.Single(state.Rows, item => item.Id == row.Id);
            Assert.Equal(decimal.GetBits(row.Amount), decimal.GetBits(actual.Amount)); Assert.Equal(TimeSpan.Zero, actual.RecordedAt.Offset);
        }
        Assert.NotNull(state.Journal);
        Phase0MigrationSafetyBatch[] batches = complete ? [new("batch1", "S0", 1, 1), new("batch2", "S0", 1, 1)] : [new("batch1", "S0", 1, 1)];
        Assert.Equal(batches.OrderBy(batch => batch.Batch, StringComparer.Ordinal), state.Journal.OrderBy(batch => batch.Batch, StringComparer.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(state.Protected)); if (baseline is not null) Assert.Equal(baseline, state.Protected);
        var trace = probe.ReadTrace(); Assert.NotNull(trace); Assert.False(string.IsNullOrWhiteSpace(trace.Receipts));
        Assert.Equal(kind == "resume" && stage > 0 ? 1 : 0, trace.Crashes);
        Assert.Equal(kind == "resume" ? Math.Clamp(stage - 1, 0, 2) : kind == "changed" ? stage == 0 ? 0 : stage == 3 ? 2 : 1 : 0, trace.Resumes);
        Assert.Equal(kind == "retention" ? stage > 0 ? 1 : 0 : kind == "verify" ? stage == 0 ? 0 : stage == 3 ? 2 : 1 : 0, trace.Cutovers);
        Assert.Equal(postWrite ? 1 : 0, trace.Writes);
        Assert.Equal(kind == "resume" ? stage == 4 ? 2 : stage > 0 ? 1 : 0 : stage >= (kind == "retention" ? 3 : 2) ? 1 : 0, trace.Reopens);
        return state.Protected;
    }
    private static void Reply(Phase0MigrationSafetyReply? result, int status, string code)
    { Assert.NotNull(result); Assert.Equal(status, result.Status); Assert.Equal(code, result.Code); Assert.False(result.Retryable); }
}
