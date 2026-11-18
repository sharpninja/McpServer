namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent migration response scripts, with field-corruption controls added before execution.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B8; FR-MCP-187/TR-MCP-FEDERATION-014/TEST-MCP-235 ac-ResumeAfterBatchCommit/ac-ChangedSnapshotRejected/ac-FailedVerifyCannotCutover/ac-SourceRetainedAfterCutover. No actual provider migration or live cutover.</remarks>
public sealed class Phase0MigrationSafetyMockGateTests
{
    private static readonly string[] Providers = ["sqlite", "postgresql", "sqlserver"];
    private static readonly string[] Kinds = ["resume", "changed", "verify", "retention"];

    /// <summary>Gets all source-label/script/order combinations.</summary>
    public static IEnumerable<object[]> CorrectCases => Providers.SelectMany(provider => Kinds.SelectMany(kind =>
        new[] { new object[] { provider, kind, false }, [provider, kind, true] }));

    /// <summary>Validates correct scripts and exact probe interactions.</summary>
    /// <param name="provider">Provider label.</param><param name="kind">Script.</param><param name="reverse">Unordered row/journal variation.</param>
    [Theory, MemberData(nameof(CorrectCases))]
    public void CorrectMigrationSafetyPasses(string provider, string kind, bool reverse)
    {
        var probe = new MigrationProbe(provider, kind, reverse);
        Phase0MigrationSafetyContract.AssertSafety(provider, kind, probe);
        Assert.Equal(kind == "resume" ? 5 : 4, probe.StateReads); Assert.Equal(probe.StateReads, probe.TraceReads);
        Assert.Equal(kind switch {
            "resume" => "prepare,crash,resume,resume,reopen", "changed" => "prepare,resume,reopen,resume",
            "verify" => "prepare,cutover,reopen,cutover", _ => "prepare,cutover,write,reopen" }, string.Join(",", probe.Calls));
    }

    /// <summary>Gets one corrupted observation at each actual boundary, for every source label.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var provider in Providers)
            foreach (var kind in Kinds)
            {
                for (var checkpoint = 0; checkpoint < (kind == "resume" ? 5 : 4); checkpoint++)
                {
                    foreach (var member in new[] { "Missing", "ManifestMissing", "CurrentSource", "RetainedSource", "SourcePresent", "SourceReadOnly", "Routing", "Open", "Head", "TargetHash", "RowsMissing", "RowsExtra", "RowsDuplicate", "JournalMissing", "JournalExtra", "JournalDuplicate", "ProtectedEmpty" })
                        yield return [provider, kind, $"state:{checkpoint}:0:{member}"];
                    if (checkpoint > 0) yield return [provider, kind, $"state:{checkpoint}:0:ProtectedChanged"];
                    foreach (var member in new[] { "Migration", "Mode", "Project", "Provider", "Source", "SourceSchema", "TargetSchema", "TypedHash", "Destination", "TablesMissing", "TablesExtra", "TablesKey", "TablesCount", "AliasesMissing", "AliasesExtra", "AuditCount" })
                        yield return [provider, kind, $"manifest:{checkpoint}:0:{member}"];
                    var filled = kind is "verify" or "retention" || (kind == "resume" && checkpoint >= 2);
                    var rowCount = kind == "retention" && checkpoint >= 2 ? 3 : filled ? 2 : 1;
                    for (var row = 0; row < rowCount; row++)
                        foreach (var member in new[] { "Id", "Title", "Amount", "Scale", "Time", "Offset", "Notes" })
                            yield return [provider, kind, $"row:{checkpoint}:{row}:{member}"];
                    for (var row = 0; row < (filled ? 2 : 1); row++)
                        foreach (var member in new[] { "Batch", "Source", "Rows", "Applications" })
                            yield return [provider, kind, $"journal:{checkpoint}:{row}:{member}"];
                    foreach (var member in new[] { "Missing", "Crashes", "Resumes", "Cutovers", "Writes", "Reopens", "Receipts" })
                        yield return [provider, kind, $"trace:{checkpoint}:0:{member}"];
                }
                for (var response = 0; response < (kind == "retention" ? 1 : 2); response++)
                    foreach (var member in new[] { "Missing", "Status", "Code", "Retryable" })
                        yield return [provider, kind, $"reply:{response}:0:{member}"];
            }
        }
    }

    /// <summary>Requires rejection by the assertion code, never a null/key/format exception.</summary>
    /// <param name="provider">Source label.</param><param name="kind">Script.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongMigrationSafetyIsRejected(string provider, string kind, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0MigrationSafetyContract.AssertSafety(provider, kind, new MigrationProbe(provider, kind, false, fault)));


    /// <summary>Independent literal state script. No contract factories or production implementation are called.</summary>
    private sealed class MigrationProbe(string provider, string kind, bool reverse, string fault = "") : IPhase0MigrationSafetyProbe
    {
        /// <summary>Gets state read count.</summary>
        internal int StateReads { get; private set; }
        /// <summary>Gets execution receipt read count.</summary>
        internal int TraceReads { get; private set; }
        /// <summary>Gets ordered actual boundary calls from the shared test.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare(string source, string fixture) { Assert.Equal(provider, source); Assert.Equal(kind, fixture); Assert.Empty(Calls); Calls.Add("prepare"); }
        /// <inheritdoc />
        public void CrashAfterBatchCommit() { At(1); Calls.Add("crash"); }
        /// <inheritdoc />
        public Phase0MigrationSafetyReply? Resume() { At(kind == "resume" ? StateReads == 2 ? 2 : 3 : StateReads == 1 ? 1 : 3); Calls.Add("resume"); return Response(kind == "changed" ? 409 : 200, kind == "changed" ? "migration_source_changed" : ""); }
        /// <inheritdoc />
        public Phase0MigrationSafetyReply? Cutover() { At(StateReads == 1 ? 1 : 3); Calls.Add("cutover"); return Response(kind == "verify" ? 409 : 200, kind == "verify" ? "migration_verification_failed" : ""); }
        /// <inheritdoc />
        public void WritePostCutover() { At(2); Calls.Add("write"); }
        /// <inheritdoc />
        public void Reopen() { At(kind == "resume" ? 4 : kind == "retention" ? 3 : 2); Calls.Add("reopen"); }
        private void At(int reads) { Assert.Equal(reads, StateReads); Assert.Equal(reads, TraceReads); }
        private Phase0MigrationSafetyReply? Response(int status, string code)
        {
            var index = Calls.Count(call => call is "resume" or "cutover") - 1;
            return Active("reply", index, 0) switch { "Missing" => null, "Status" => new(status == 200 ? 409 : 200, code, false),
                "Code" => new(status, "wrong", false), "Retryable" => new(status, code, true), _ => new(status, code, false) };
        }

        /// <inheritdoc />
        public Phase0MigrationSafetyState? ReadState()
        {
            var checkpoint = StateReads++;
            var filled = kind is "verify" or "retention" || (kind == "resume" && checkpoint is 2 or 3 or 4);
            var added = kind == "retention" && checkpoint is 2 or 3;
            var moment = DateTimeOffset.Parse("2026-01-02T03:04:05.1234567+00:00", System.Globalization.CultureInfo.InvariantCulture);
            var rows = new List<Phase0FixtureRow> { new("T1", kind == "verify" ? "wrong-κόσμος" : "seed-κόσμος", 12.34m, moment, null) };
            if (filled) rows.Add(new("T2", "retained", 56.78m, moment, "résumé"));
            if (added) rows.Add(new("T3", "post-cutover", 90.12m, moment, ""));
            Phase0MigrationSafetyBatch[] journal = filled ? [new("batch1", "S0", 1, 1), new("batch2", "S0", 1, 1)] : [new("batch1", "S0", 1, 1)];
            var manifest = new Phase0MigrationSafetyManifest("migration-1", "seed", "Atlas", provider, "S0", 1, 1, "typed-S0",
                kind == "retention" ? "C0" : null, new Dictionary<string, int> { ["TypedRows"] = 2 }, [], 0);
            var state = new Phase0MigrationSafetyState(manifest, kind == "changed" ? "S1" : "S0", "S0", true, true,
                kind == "retention" && checkpoint is 1 or 2 or 3 ? "dolt" : provider,
                kind == "retention" && checkpoint is 1 or 2 or 3, kind == "retention" ? "C0" : null,
                kind == "verify" ? "typed-corrupt" : added ? "typed-T3" : filled ? "typed-S0" : "typed-batch1",
                rows.ToArray(), journal, "protected-other-project-and-control");
            state = CorruptState(state, checkpoint);
            return reverse && state is not null ? state with { Rows = state.Rows?.Reverse().ToArray(), Journal = state.Journal?.Reverse().ToArray() } : state;
        }

        /// <inheritdoc />
        public Phase0MigrationSafetyTrace? ReadTrace()
        {
            var checkpoint = TraceReads++;
            var trace = new Phase0MigrationSafetyTrace(Calls.Count(call => call == "crash"), Calls.Count(call => call == "resume"),
                Calls.Count(call => call == "cutover"), Calls.Count(call => call == "write"),
                Calls.Count(call => call is "crash" or "reopen"), "script-receipts-" + checkpoint);
            return Active("trace", checkpoint, 0) switch {
                "Missing" => null, "Crashes" => trace with { Crashes = trace.Crashes + 1 }, "Resumes" => trace with { Resumes = trace.Resumes + 1 },
                "Cutovers" => trace with { Cutovers = trace.Cutovers + 1 }, "Writes" => trace with { Writes = trace.Writes + 1 },
                "Reopens" => trace with { Reopens = trace.Reopens + 1 }, "Receipts" => trace with { Receipts = "" }, _ => trace };
        }

        /// <summary>Corrupts only freshly constructed independent script observations.</summary>
        private Phase0MigrationSafetyState? CorruptState(Phase0MigrationSafetyState state, int checkpoint)
        {
            var m = state.Manifest!;
            m = Active("manifest", checkpoint, 0) switch {
                "Migration" => m with { Migration = "other" }, "Mode" => m with { Mode = "join" }, "Project" => m with { Project = "Other" },
                "Provider" => m with { Provider = "wrong" }, "Source" => m with { Source = "S1" },
                "SourceSchema" => m with { SourceSchema = 2 }, "TargetSchema" => m with { TargetSchema = 2 },
                "TypedHash" => m with { TypedHash = "wrong" }, "Destination" => m with { Destination = "wrong" },
                "TablesMissing" => m with { Tables = null }, "TablesExtra" => m with { Tables = new Dictionary<string, int> { ["TypedRows"] = 2, ["extra"] = 1 } },
                "TablesKey" => m with { Tables = new Dictionary<string, int> { ["Other"] = 2 } },
                "TablesCount" => m with { Tables = new Dictionary<string, int> { ["TypedRows"] = 3 } },
                "AliasesMissing" => m with { Aliases = null }, "AliasesExtra" => m with { Aliases = ["unexpected-alias"] },
                "AuditCount" => m with { AuditCount = 1 }, _ => m };
            var rows = state.Rows!;
            for (var index = 0; index < rows.Length; index++)
            {
                var r = rows[index];
                rows[index] = Active("row", checkpoint, index) switch {
                    "Id" => r with { Id = "other" }, "Title" => r with { Title = "wrong" }, "Amount" => r with { Amount = r.Amount + 0.01m },
                    "Scale" => r with { Amount = r.Amount * 1.0m }, "Time" => r with { RecordedAt = r.RecordedAt.AddTicks(1) },
                    "Offset" => r with { RecordedAt = r.RecordedAt.ToOffset(TimeSpan.FromHours(1)) },
                    "Notes" => r with { Notes = r.Notes is null ? "" : null }, _ => r };
            }
            var journal = state.Journal!;
            for (var index = 0; index < journal.Length; index++)
            {
                var b = journal[index];
                journal[index] = Active("journal", checkpoint, index) switch {
                    "Batch" => b with { Batch = "wrong" }, "Source" => b with { Source = "S1" },
                    "Rows" => b with { Rows = 2 }, "Applications" => b with { Applications = 2 }, _ => b };
            }
            state = state with { Manifest = m };
            return Active("state", checkpoint, 0) switch {
                "Missing" => null, "ManifestMissing" => state with { Manifest = null }, "CurrentSource" => state with { CurrentSource = "wrong" },
                "RetainedSource" => state with { RetainedSource = "S1" }, "SourcePresent" => state with { SourcePresent = false },
                "SourceReadOnly" => state with { SourceReadOnly = false }, "Routing" => state with { Routing = "wrong" },
                "Open" => state with { WritesOpen = !state.WritesOpen }, "Head" => state with { Head = "wrong" },
                "TargetHash" => state with { TargetHash = "wrong" }, "RowsMissing" => state with { Rows = null },
                "RowsExtra" => state with { Rows = [.. rows, rows[0] with { Id = "extra" }] },
                "RowsDuplicate" => state with { Rows = [rows[0], rows[0]] },
                "JournalMissing" => state with { Journal = null }, "JournalExtra" => state with { Journal = [.. journal, new("extra", "S0", 0, 1)] },
                "JournalDuplicate" => state with { Journal = [journal[0], journal[0]] },
                "ProtectedEmpty" => state with { Protected = "" }, "ProtectedChanged" => state with { Protected = "changed" }, _ => state };
        }

        private string Active(string area, int checkpoint, int row)
        {
            var parts = fault.Split(':');
            return parts.Length == 4 && parts[0] == area && parts[1] == checkpoint.ToString(System.Globalization.CultureInfo.InvariantCulture)
                && parts[2] == row.ToString(System.Globalization.CultureInfo.InvariantCulture) ? parts[3] : "";
        }
    }
}
