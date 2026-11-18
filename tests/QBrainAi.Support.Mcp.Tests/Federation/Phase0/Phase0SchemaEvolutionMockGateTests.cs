namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripted schema-history fixtures validate each shared assertion before real binding.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006 B7; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 ac-VersionedProjectNotMigratedAtStartup/ac-UnsupportedSchemaRejected/ac-OldSchemaUnpublishedCommitMergesAfterUpgrade/ac-FeatureBranchSchemaCatchUp. No engine, migration or deployment is implemented.</remarks>
public sealed class Phase0SchemaEvolutionMockGateTests
{
    private static readonly string[] Kinds = ["startup", "unsupported", "clean", "conflict"];

    /// <summary>Gets four independent scripts with normal and reversed unordered inventories.</summary>
    public static IEnumerable<object[]> CorrectCases => Kinds.SelectMany(kind => new[] { new object[] { kind, false }, [kind, true] });

    /// <summary>Exercises the same assertions intended for disposable native binding.</summary>
    /// <param name="kind">Script.</param><param name="reverse">Reverse unordered inventories.</param>
    [Theory, MemberData(nameof(CorrectCases))]
    public void CorrectSchemaEvolutionPasses(string kind, bool reverse)
    {
        var probe = new EvolutionProbe(kind, reverse);
        Phase0SchemaEvolutionContract.AssertEvolution(kind, probe);
        Assert.Equal(kind is "startup" or "unsupported" ? 3 : 7, probe.StateReads);
        Assert.Equal(probe.StateReads, probe.TraceReads);
        Assert.Equal(kind == "startup" ? "prepare,start,start" : kind == "unsupported" ? "prepare,start,write:main"
            : "prepare,start,preflight+swap,merge:main,write:feature,merge:feature,reopen", string.Join(",", probe.Calls));
    }

    /// <summary>Gets one deliberate corruption of every independent observation at each checkpoint.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var kind in Kinds)
            {
                var count = kind is "startup" or "unsupported" ? 3 : 7;
                for (var checkpoint = 0; checkpoint < count; checkpoint++)
                {
                    foreach (var member in new[] { "Missing", "Project", "Binary", "Public", "Migrations", "HistoryReadable", "ProtectedEmpty", "BranchesMissing", "BranchesExtra", "BranchesDuplicate" })
                        yield return [kind, $"state:{checkpoint}:0:{member}"];
                    if (checkpoint > 0) yield return [kind, $"state:{checkpoint}:0:ProtectedChanged"];
                    for (var branch = 0; branch < 2; branch++)
                    {
                        foreach (var member in new[] { "Name", "Head", "Schema", "Open", "Working", "HistoryMissing", "HistoryExtra", "HistoryWrong", "AncestorsMissing", "AncestorsExtra", "AncestorsWrong", "RowsMissing", "RowsExtra", "RowsDuplicate" })
                            yield return [kind, $"branch:{checkpoint}:{branch}:{member}"];
                        for (var row = 0; row < 2; row++)
                            foreach (var member in new[] { "Id", "Title", "Amount", "Created", "Note", "Added" })
                                yield return [kind, $"row{branch}:{checkpoint}:{row}:{member}"];
                    }
                    foreach (var member in new[] { "Missing", "Starts", "Preflights", "Swaps", "MainMerges", "FeatureMerges", "RejectedWrites", "Reopens", "Receipts" })
                        yield return [kind, $"trace:{checkpoint}:0:{member}"];
                    if (kind == "conflict" && checkpoint >= 5)
                        foreach (var member in new[] { "Missing", "Branch", "State", "Key", "Column", "Base", "Local", "Incoming" })
                            yield return [kind, $"review:{checkpoint}:0:{member}"];
                    else yield return [kind, $"review:{checkpoint}:0:Extra"];
                }
                if (kind != "startup")
                    foreach (var member in new[] { "Missing", "Status", "Code", "Retryable" }) yield return [kind, "reply:0:0:" + member];
            }
        }
    }

    /// <summary>Rejects each wrong observation through actual contract assertions, not arbitrary exceptions.</summary>
    /// <param name="kind">Script.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongSchemaEvolutionIsRejected(string kind, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0SchemaEvolutionContract.AssertEvolution(kind, new EvolutionProbe(kind, false, fault)));

    /// <summary>Literal scripted data; never calls contract expected-value factories or production code.</summary>
    private sealed class EvolutionProbe(string kind, bool reverse, string fault = "") : IPhase0SchemaEvolutionProbe
    {
        /// <summary>Gets independently requested state snapshots.</summary>
        internal int StateReads { get; private set; }
        /// <summary>Gets requested native-evidence snapshots.</summary>
        internal int TraceReads { get; private set; }
        /// <summary>Gets the boundary calls made by the shared test.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare(string fixture) { Assert.Equal(kind, fixture); Assert.Empty(Calls); Calls.Add("prepare"); }
        /// <inheritdoc />
        public void Start() { Assert.Equal(kind == "startup" && StateReads == 2 ? 2 : 1, StateReads); Assert.Equal(StateReads, TraceReads); Calls.Add("start"); }
        /// <inheritdoc />
        public void UpgradeAfterPreflight() { At(2); Calls.Add("preflight+swap"); }
        /// <inheritdoc />
        public void MergeSchema(string branch) { At(branch == "main" ? 3 : 5); Calls.Add("merge:" + branch); }
        /// <inheritdoc />
        public Phase0SchemaReadinessReply? TryWrite(string branch)
        {
            At(kind == "unsupported" ? 2 : 4); Assert.Equal(kind == "unsupported" ? "main" : "feature", branch); Calls.Add("write:" + branch);
            return Active("reply", 0, 0) switch { "Missing" => null, "Status" => new(200, "incompatible_schema", false),
                "Code" => new(409, "other", false), "Retryable" => new(409, "incompatible_schema", true), _ => new(409, "incompatible_schema", false) };
        }
        /// <inheritdoc />
        public void Reopen() { At(6); Calls.Add("reopen"); }
        private void At(int reads) { Assert.Equal(reads, StateReads); Assert.Equal(reads, TraceReads); }

        /// <inheritdoc />
        public Phase0SchemaEvolutionState? ReadState()
        {
            var checkpoint = StateReads++;
            // These literal maps pin script states independently of the assertion's expected-state calculations.
            var mainHead = kind == "unsupported" ? "S1" : checkpoint is 3 or 4 or 5 or 6 ? "M1" : "B1";
            var featureHead = kind == "unsupported" || (kind == "clean" && checkpoint is 5 or 6) ? "F2" : "F1";
            var mainSchema = mainHead is "S1" or "M1" ? 2 : 1;
            var featureSchema = featureHead == "F2" ? 2 : 1;
            var mainOpen = kind != "unsupported" && (checkpoint != 2 || kind == "startup");
            var featureOpen = kind == "startup" || ((kind is "clean" or "conflict") && checkpoint < 2) || (kind == "clean" && checkpoint is 5 or 6);
            var mainTitle = kind == "unsupported" ? "base-title" : "offline-κόσμος";
            var mainSecond = kind == "conflict" && checkpoint is 3 or 4 or 5 or 6 ? "hub-title" : "legacy-feature";
            string[] mainAncestors = mainHead switch { "S1" => ["C0", "S1"], "M1" => ["C0", "B1", "S1", "M1"], _ => ["C0", "B1"] };
            string[] featureAncestors = featureHead == "F2" ? kind == "unsupported" ? ["C0", "S1", "F2"] : ["C0", "F1", "S1", "F2"] : ["C0", "F1"];
            Phase0SchemaEvolutionBranch[] branches = [
                new("main", mainHead, mainSchema, mainOpen, "clean-" + mainHead, mainSchema == 2 ? ["schema1", "schema2"] : ["schema1"], mainAncestors,
                    Rows(mainTitle, mainSecond, mainSchema)),
                new("feature", featureHead, featureSchema, featureOpen, "clean-" + featureHead, featureSchema == 2 ? ["schema1", "schema2"] : ["schema1"], featureAncestors,
                    Rows("base-title", kind == "unsupported" ? "legacy-feature" : "feature-title", featureSchema))];
            for (var branchIndex = 0; branchIndex < branches.Length; branchIndex++)
            {
                var b = branches[branchIndex];
                var rows = b.Rows!;
                for (var row = 0; row < rows.Length; row++)
                {
                    var r = rows[row];
                    rows[row] = Active("row" + branchIndex, checkpoint, row) switch {
                        "Id" => r with { Id = "other" }, "Title" => r with { Title = "wrong" }, "Amount" => r with { Amount = r.Amount + 0.01m },
                        "Created" => r with { Created = r.Created.AddTicks(1) }, "Note" => r with { Note = r.Note is null ? "" : null },
                        "Added" => r with { Added = r.Added is null ? "expanded" : null }, _ => r };
                }
                branches[branchIndex] = Active("branch", checkpoint, branchIndex) switch {
                    "Name" => b with { Name = "other" }, "Head" => b with { Head = "C0" }, "Schema" => b with { Schema = 99 }, "Open" => b with { WritesOpen = !b.WritesOpen },
                    "Working" => b with { WorkingHash = "dirty" }, "HistoryMissing" => b with { History = null },
                    "HistoryExtra" => b with { History = [.. b.History!, "unexpected"] }, "HistoryWrong" => b with { History = ["schema0"] },
                    "AncestorsMissing" => b with { Ancestors = null }, "AncestorsExtra" => b with { Ancestors = [.. b.Ancestors!, "unrelated"] },
                    "AncestorsWrong" => b with { Ancestors = ["C0"] }, "RowsMissing" => b with { Rows = null },
                    "RowsExtra" => b with { Rows = [.. rows, rows[0] with { Id = "extra" }] }, "RowsDuplicate" => b with { Rows = [rows[0], rows[0]] }, _ => b };
            }
            Phase0SchemaEvolutionReview? review = kind == "conflict" && checkpoint is 5 or 6
                ? new("feature", "needs_review", "TODO-008", "Title", "legacy-feature", "feature-title", "hub-title") : null;
            review = Active("review", checkpoint, 0) switch {
                "Missing" => null, "Extra" => new("main", "needs_review", "extra", "Title", "base", "local", "incoming"),
                "Branch" => review! with { Branch = "main" }, "State" => review! with { State = "resolved" }, "Key" => review! with { Key = "TODO-007" },
                "Column" => review! with { Column = "Note" }, "Base" => review! with { Base = "wrong" }, "Local" => review! with { Local = "wrong" },
                "Incoming" => review! with { Incoming = "wrong" }, _ => review };
            if (reverse)
            {
                branches = branches.Select(b => b with { Ancestors = b.Ancestors?.Reverse().ToArray(), Rows = b.Rows?.Reverse().ToArray() }).Reverse().ToArray();
            }
            var state = new Phase0SchemaEvolutionState("Atlas", (kind is "clean" or "conflict") && checkpoint is 2 or 3 or 4 or 5 or 6 ? "binary-2" : "binary-1", "S1", 0, true, branches, review, "protected-control-and-other-project");
            return Active("state", checkpoint, 0) switch {
                "Missing" => null, "Project" => state with { Project = "Other" }, "Binary" => state with { Binary = "wrong" },
                "Public" => state with { PublicHead = "M1" }, "Migrations" => state with { ProjectEfMigrations = 1 },
                "HistoryReadable" => state with { HistoryReadable = false }, "ProtectedEmpty" => state with { Protected = "" },
                "ProtectedChanged" => state with { Protected = "changed" }, "BranchesMissing" => state with { Branches = null },
                "BranchesExtra" => state with { Branches = [.. branches, branches[0] with { Name = "extra" }] },
                "BranchesDuplicate" => state with { Branches = [branches[0], branches[0]] }, _ => state };
        }

        /// <summary>Supplies typed literals independent of the assertion's expected record construction.</summary>
        private static Phase0SchemaEvolutionRow[] Rows(string first, string second, int schema) => [
            new("TODO-007", first, 12.34m, DateTimeOffset.Parse("2026-01-02T03:04:05.1234567+00:00", System.Globalization.CultureInfo.InvariantCulture), null, schema == 2 ? "expanded" : null),
            new("TODO-008", second, 56.78m, DateTimeOffset.Parse("2026-01-02T03:04:05.1234567+00:00", System.Globalization.CultureInfo.InvariantCulture), "résumé", schema == 2 ? "expanded" : null)];

        /// <inheritdoc />
        public Phase0SchemaEvolutionTrace? ReadTrace()
        {
            var checkpoint = TraceReads++;
            var trace = new Phase0SchemaEvolutionTrace(Calls.Count(c => c == "start"), Calls.Count(c => c == "preflight+swap"),
                Calls.Count(c => c == "preflight+swap"), Calls.Count(c => c == "merge:main"), Calls.Count(c => c == "merge:feature"),
                Calls.Count(c => c.StartsWith("write:", StringComparison.Ordinal)), Calls.Count(c => c == "reopen"), "script-receipts-" + checkpoint);
            return Active("trace", checkpoint, 0) switch {
                "Missing" => null, "Starts" => trace with { Starts = trace.Starts + 1 }, "Preflights" => trace with { Preflights = trace.Preflights + 1 },
                "Swaps" => trace with { Swaps = trace.Swaps + 1 }, "MainMerges" => trace with { MainMerges = trace.MainMerges + 1 },
                "FeatureMerges" => trace with { FeatureMerges = trace.FeatureMerges + 1 }, "RejectedWrites" => trace with { RejectedWrites = trace.RejectedWrites + 1 },
                "Reopens" => trace with { Reopens = trace.Reopens + 1 }, "Receipts" => trace with { Receipts = "" }, _ => trace };
        }
        private string Active(string area, int checkpoint, int row)
        {
            var parts = fault.Split(':');
            return parts.Length == 4 && parts[0] == area && parts[1] == checkpoint.ToString(System.Globalization.CultureInfo.InvariantCulture)
                && parts[2] == row.ToString(System.Globalization.CultureInfo.InvariantCulture) ? parts[3] : "";
        }
    }
}
