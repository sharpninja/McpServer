namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Proves that the disposable-storage assertion scenarios discriminate wrong outcomes.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-002; FR-MCP-175 ac-SaveDoesNotPublish;
/// FR-MCP-177 ac-PooledConnectionDoesNotLeakBranch; FR-MCP-186 ac-DoltBaselineAndUpgrade;
/// TR-MCP-FEDERATION-002/004/013; TEST-MCP-223/225/234.
/// No environment, database or production implementation is used. Mock fixtures and faults
/// are independent of the assertion implementation. Real binding is still pending.
/// </remarks>
public sealed class Phase0StorageBoundaryMockGateTests
{
    /// <summary>Correct empty/baseline/upgrade/reapply readbacks pass the reusable scenario.</summary>
    [Fact]
    public void CorrectMigrationAndCrudPass()
    {
        var probe = new ScriptedMigrationProbe();
        Phase0StorageBoundaryContract.AssertBaselineAndUpgrade(probe);
        Assert.Equal(6, probe.CallCount);
    }

    /// <summary>The assertion scenario actually requests and validates 100 alternating reads.</summary>
    [Fact]
    public void CorrectPooledReadsPass()
    {
        var probe = new ScriptedPooledReadProbe();
        Phase0StorageBoundaryContract.AssertPooledBranchIsolation(probe);
        Assert.Equal(100, probe.CallCount);
    }

    /// <summary>A persisted SQL draft without history or publication changes passes.</summary>
    [Fact]
    public void CorrectSaveBoundaryPass()
        => Phase0StorageBoundaryContract.AssertSaveDoesNotPublish(new ScriptedSaveProbe());

    /// <summary>Gets faults for every checkpoint and every independent typed CRUD readback.</summary>
    public static IEnumerable<object[]> MigrationFaultCases
    {
        get
        {
            foreach (var call in new[] { 0, 1, 3, 5 })
            {
                foreach (var fault in new[] { "Missing", "Schema", "History", "Changes", "ExtraRow" })
                    yield return [call, fault];
                if (call < 3) continue;
                foreach (var fault in new[] { "MissingRow", "MissingHistory", "ReorderedHistory", "DuplicateHistory" })
                    yield return [call, fault];
                foreach (var corruptField in RowFields)
                    yield return [call, "Seed:" + corruptField];
            }
            foreach (var call in new[] { 2, 4 })
            {
                yield return [call, "Missing"];
                yield return [call, "DeletedStillPresent"];
                foreach (var row in new[] { "Created", "Updated", "Seed" })
                foreach (var corruptField in RowFields)
                    yield return [call, row + ":" + corruptField];
            }
        }
    }

    /// <summary>Gets a distinct fault at every read position, including the last read.</summary>
    public static IEnumerable<object[]> PooledFaultCases
    {
        get
        {
            for (var position = 0; position < 100; position++)
            foreach (var corruptField in new[] { "Missing", "Project", "Branch", "RowId", "Title" })
                yield return [position, corruptField];
        }
    }

    /// <summary>Gets each SQL-save boundary violation independently.</summary>
    public static IEnumerable<object[]> SaveFaultCases
        => new[] { "Missing", "Main", "Remote", "RowId", "Title", "Intent", "Commit" }
            .Select(fault => new object[] { fault });

    /// <summary>Every wrong migration or CRUD observation must fail the unchanged assertions.</summary>
    /// <param name="call">Only this checkpoint is corrupted.</param>
    /// <param name="fault">One independent missing or wrong value.</param>
    [Theory]
    [MemberData(nameof(MigrationFaultCases))]
    public void WrongMigrationOutcomeIsRejected(int call, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0StorageBoundaryContract.AssertBaselineAndUpgrade(new ScriptedMigrationProbe(call, fault)));

    /// <summary>Every wrong read must fail even when all earlier reads were correct.</summary>
    /// <param name="position">The single corrupted read, zero-based.</param>
    /// <param name="field">Identity or value corrupted independently.</param>
    [Theory]
    [MemberData(nameof(PooledFaultCases))]
    public void WrongPooledReadIsRejected(int position, string field)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0StorageBoundaryContract.AssertPooledBranchIsolation(new ScriptedPooledReadProbe(position, field)));

    /// <summary>Every wrong draft-save state or publication side effect must fail.</summary>
    /// <param name="fault">The sole corrupted boundary.</param>
    [Theory]
    [MemberData(nameof(SaveFaultCases))]
    public void WrongSaveOutcomeIsRejected(string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0StorageBoundaryContract.AssertSaveDoesNotPublish(new ScriptedSaveProbe(fault)));

    /// <summary>Gets each independently asserted fixture row field and its missing-row control.</summary>
    private static IReadOnlyList<string> RowFields
        => ["Missing", "Id", "Title", "Amount", "Scale", "Timestamp", "Offset", "Notes"];

    /// <summary>Builds an independently scripted typed fixture, never production-derived output.</summary>
    /// <param name="id">Fixture row identity.</param>
    /// <param name="title">Scripted text.</param>
    /// <returns>Independent canonical fixture value.</returns>
    private static Phase0FixtureRow Row(string id, string title)
        => new(id, title, 12.3400m,
            new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567),
            id == "T1" ? null : "Ångström 文");

    /// <summary>Mutates exactly one row observation to prove the corresponding assertion.</summary>
    /// <param name="row">Correct independent fixture.</param>
    /// <param name="field">Single field or omission to corrupt.</param>
    /// <returns>Wrong fixture or null.</returns>
    private static Phase0FixtureRow? CorruptRow(Phase0FixtureRow row, string field)
        => field switch
        {
            "Missing" => null,
            "Id" => row with { Id = "other" },
            "Title" => row with { Title = "wrong" },
            "Amount" => row with { Amount = 99.9900m },
            "Scale" => row with { Amount = 12.34m },
            "Timestamp" => row with { RecordedAt = row.RecordedAt.AddTicks(1) },
            "Offset" => row with { RecordedAt = new DateTimeOffset(row.RecordedAt.DateTime, TimeSpan.FromHours(1)) },
            "Notes" => row with { Notes = row.Id == "T1" ? "unexpected" : "Angstrom" },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

    /// <summary>Six-operation independent migration script with one optional corrupted response.</summary>
    /// <param name="faultCall">Only this operation is corrupted; -1 means correct throughout.</param>
    /// <param name="fault">Selected corruption.</param>
    private sealed class ScriptedMigrationProbe(int faultCall = -1, string fault = "") : IPhase0MigrationProbe
    {
        /// <summary>Gets the number of operations actually invoked by the scenario.</summary>
        internal int CallCount { get; private set; }

        /// <inheritdoc />
        public Phase0MigrationStage? InspectEmpty()
        {
            var call = CallCount++;
            Assert.Equal(0, call);
            return Stage(call, new(0, [], 0, []));
        }

        /// <inheritdoc />
        public Phase0MigrationStage? ApplySchema(int schemaVersion)
        {
            var call = CallCount++;
            Assert.Contains(call, new[] { 1, 3, 5 });
            Assert.Equal(call == 1 ? 1 : 2, schemaVersion);
            var correct = call switch
            {
                1 => new Phase0MigrationStage(1, ["schema1"], 1, []),
                3 => new Phase0MigrationStage(2, ["schema1", "schema2"], 1, [Row("T1", "seed")]),
                5 => new Phase0MigrationStage(2, ["schema1", "schema2"], 0, [Row("T1", "seed")]),
                _ => throw new InvalidOperationException("Unexpected migration call"),
            };
            return Stage(call, correct);
        }

        /// <inheritdoc />
        public Phase0CrudObservation? ExerciseCrud(bool seedIfMissing)
        {
            var call = CallCount++;
            Assert.Contains(call, new[] { 2, 4 });
            Assert.Equal(call == 2, seedIfMissing);
            var correct = new Phase0CrudObservation(Row("T2", "created"), Row("T2", "updated"), null, Row("T1", "seed"));
            if (call != faultCall) return correct;
            if (fault == "Missing") return null;
            if (fault == "DeletedStillPresent") return correct with { Deleted = Row("T2", "updated") };
            var parts = fault.Split(':');
            return parts[0] switch
            {
                "Created" => correct with { Created = CorruptRow(correct.Created!, parts[1]) },
                "Updated" => correct with { Updated = CorruptRow(correct.Updated!, parts[1]) },
                "Seed" => correct with { RetainedSeed = CorruptRow(correct.RetainedSeed!, parts[1]) },
                _ => throw new InvalidOperationException("Unexpected CRUD fault"),
            };
        }

        /// <summary>Injects exactly one schema/history/row fault at its named checkpoint.</summary>
        /// <param name="call">Current operation.</param>
        /// <param name="correct">Independently scripted correct state.</param>
        /// <returns>Correct state or one corrupted observation.</returns>
        private Phase0MigrationStage? Stage(int call, Phase0MigrationStage correct)
        {
            if (call != faultCall) return correct;
            if (fault.StartsWith("Seed:", StringComparison.Ordinal))
            {
                var row = CorruptRow(Row("T1", "seed"), fault[5..]);
                return correct with { Rows = row is null ? [] : [row] };
            }
            return fault switch
            {
                "Missing" => null,
                "Schema" => correct with { SchemaVersion = 99 },
                "History" => correct with { AppliedMigrations = ["other"] },
                "Changes" => correct with { ChangesApplied = correct.ChangesApplied + 1 },
                "ExtraRow" => correct with { Rows = [.. correct.Rows, Row("unexpected", "extra")] },
                "MissingRow" => correct with { Rows = [] },
                "MissingHistory" => correct with { AppliedMigrations = ["schema1"] },
                "ReorderedHistory" => correct with { AppliedMigrations = ["schema2", "schema1"] },
                "DuplicateHistory" => correct with { AppliedMigrations = ["schema1", "schema2", "schema2"] },
                _ => throw new InvalidOperationException("Unexpected checkpoint fault"),
            };
        }
    }

    /// <summary>Independent pool read script; one position and one field may be corrupted.</summary>
    /// <param name="faultPosition">Only this read is corrupted.</param>
    /// <param name="field">Field to corrupt.</param>
    private sealed class ScriptedPooledReadProbe(int faultPosition = -1, string field = "") : IPhase0PooledReadProbe
    {
        /// <summary>Gets the number of actual read invocations.</summary>
        internal int CallCount { get; private set; }

        /// <inheritdoc />
        public Phase0BranchRead? Read(string projectId, string branch, string rowId)
        {
            var position = CallCount++;
            Assert.Equal("Atlas", projectId);
            Assert.Equal(position % 2 == 0 ? "main" : "feature/search", branch);
            Assert.Equal("T1", rowId);
            var correct = position % 2 == 0
                ? new Phase0BranchRead("Atlas", "main", "T1", "main")
                : new Phase0BranchRead("Atlas", "feature/search", "T1", "feature");
            if (position != faultPosition) return correct;
            return field switch
            {
                "Missing" => null,
                "Project" => correct with { ProjectId = "OtherProject" },
                "Branch" => correct with { Branch = branch == "main" ? "feature/search" : "main" },
                "RowId" => correct with { RowId = "T2" },
                "Title" => correct with { Title = correct.Title == "main" ? "feature" : "main" },
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            };
        }
    }

    /// <summary>Independent saved-draft/ref/side-effect script.</summary>
    /// <param name="fault">Optional single corrupted boundary.</param>
    private sealed class ScriptedSaveProbe(string fault = "") : IPhase0SaveProbe
    {
        /// <inheritdoc />
        public Phase0SaveObservation? SaveDraft(string rowId, string title)
        {
            Assert.Equal("T1", rowId);
            Assert.Equal("draft", title);
            var correct = new Phase0SaveObservation("C0", "C0", "T1", "draft", [], 0);
            return fault switch
            {
                "" => correct,
                "Missing" => null,
                "Main" => correct with { MainHead = "A1" },
                "Remote" => correct with { RemoteHead = "A1" },
                "RowId" => correct with { StoredRowId = "T2" },
                "Title" => correct with { StoredTitle = "old" },
                "Intent" => correct with { PublicationIntents = ["intent-A1"] },
                "Commit" => correct with { HistoryCommitsCreated = 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(fault)),
            };
        }
    }
}
