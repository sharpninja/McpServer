using System.Globalization;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Discriminating, environment-free mocks for the reusable query and transaction assertions.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade;
/// FR-MCP-175 ac-SaveDoesNotPublish; TR-MCP-FEDERATION-002/013; TEST-MCP-223/234.
/// Scripted values are independent literals, not results computed by the assertion contract or production code.
/// These tests do not establish real SQL, Dolt, provider compatibility or task completion.
/// </remarks>
public sealed class Phase0QueryTransactionMockGateTests
{
    /// <summary>Accepts the three correct query results and verifies their complete invocation sequence.</summary>
    [Fact]
    public void CorrectQueriesPass()
    {
        var probe = new ScriptedQueryProbe();
        Phase0QueryTransactionContract.AssertQueries(probe);
        Assert.Equal(new[] { "filter-order-page", "group-aggregate", "raw-parameter" }, probe.Calls);
    }

    /// <summary>Accepts each distinct SQL and history boundary, including history inventory reordering.</summary>
    /// <param name="outcome">The requested SQL/history boundary.</param>
    /// <param name="reverseHistory">Reorders inventory without changing any identity or parent.</param>
    [Theory]
    [InlineData("rollback", false)]
    [InlineData("sql-commit", false)]
    [InlineData("dolt-commit", false)]
    [InlineData("dolt-commit", true)]
    public void CorrectTransactionsPass(string outcome, bool reverseHistory)
    {
        var probe = new ScriptedTransactionProbe(outcome, reverseHistory: reverseHistory);
        Phase0QueryTransactionContract.AssertTransaction(probe, outcome);
        var expected = new List<string> { "Begin", "Mutate", outcome == "rollback" ? "Rollback" : "SqlCommit" };
        if (outcome == "dolt-commit") expected.Add("CommitHistory");
        Assert.Equal(expected, probe.Calls);
    }

    /// <summary>Gets every query shape, binding and typed result corruption at its own call.</summary>
    public static IEnumerable<object[]> QueryFaultCases
    {
        get
        {
            for (var call = 0; call < 3; call++)
            {
                foreach (var fault in new[] { "Missing", "RowsNull", "ExtraRow", "GroupsNull", "ExtraGroup", "Sql", "ParametersNull", "ExtraParameter" })
                    yield return [call, fault];
                if (call == 1)
                {
                    yield return [call, "MissingGroup"];
                    yield return [call, "ReverseGroups"];
                    for (var group = 0; group < 2; group++)
                    foreach (var corruptField in new[] { "Null", "Key", "Count", "Total", "Scale" })
                        yield return [call, $"Group:{group}:{corruptField}"];
                }
                else
                {
                    yield return [call, "MissingRow"];
                    if (call == 0) yield return [call, "ReverseRows"];
                    for (var row = 0; row < (call == 0 ? 2 : 1); row++)
                    foreach (var corruptField in RowFields)
                        yield return [call, $"Row:{row}:{corruptField}"];
                }
            }
            foreach (var fault in new[] { "MissingParameter", "ParameterName", "ParameterValue", "InterpolatedSql", "MutateInputParameter" })
                yield return [2, fault];
        }
    }

    /// <summary>Gets faults at every before/inside/after checkpoint and explicit history commit.</summary>
    public static IEnumerable<object[]> TransactionFaultCases
    {
        get
        {
            foreach (var outcome in new[] { "rollback", "sql-commit", "dolt-commit" })
            for (var stage = 0; stage < (outcome == "dolt-commit" ? 4 : 3); stage++)
            {
                foreach (var fault in new[] { "Missing", "RowsNull", "MissingRow", "ExtraRow", "ReverseRows", "MainBlank", "RemoteBlank",
                    "HistoryNull", "HistoryEmpty", "HistoryDuplicate", "HistoryNullPoint", "HistoryBlankHash", "HistoryNullParents", "MainAbsent", "IntentsNull", "Intent" })
                    yield return [outcome, stage, fault];
                for (var row = 0; row < 4; row++)
                foreach (var corruptField in RowFields)
                    yield return [outcome, stage, $"Row:{row}:{corruptField}"];
                if (stage == 0) yield return [outcome, stage, "RemoteChanged"];
                else
                {
                    foreach (var fault in new[] { "RemoteChanged", "RewriteParents", "MissingRoot" })
                        yield return [outcome, stage, fault];
                    if (stage < 3) yield return [outcome, stage, "ImplicitCommit"];
                    else
                        foreach (var fault in new[] { "MainUnchanged", "NoNewCommit", "ExtraCommit", "NewParentWrong", "NewParentExtra" })
                            yield return [outcome, stage, fault];
                }
            }
        }
    }

    /// <summary>Requires an assertion failure after the intended query fault was actually injected.</summary>
    /// <param name="call">Only this query observation is corrupted.</param>
    /// <param name="fault">One omitted or wrong observation.</param>
    [Theory]
    [MemberData(nameof(QueryFaultCases))]
    public void WrongQueryIsRejected(int call, string fault)
    {
        var probe = new ScriptedQueryProbe(call, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0QueryTransactionContract.AssertQueries(probe));
        Assert.True(probe.CorruptionApplied);
    }

    /// <summary>Requires an assertion failure after the intended transaction fault was injected.</summary>
    /// <param name="outcome">The SQL/history boundary being tested.</param>
    /// <param name="stage">The single corrupted checkpoint.</param>
    /// <param name="fault">The independent state defect.</param>
    [Theory]
    [MemberData(nameof(TransactionFaultCases))]
    public void WrongTransactionIsRejected(string outcome, int stage, string fault)
    {
        var probe = new ScriptedTransactionProbe(outcome, stage, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0QueryTransactionContract.AssertTransaction(probe, outcome));
        Assert.True(probe.CorruptionApplied);
    }

    /// <summary>Rejects a probe rewriting the same parent list it returned as the baseline.</summary>
    [Fact]
    public void AliasedBaselineRewriteIsRejected()
    {
        var probe = new ScriptedTransactionProbe("sql-commit", 1, "AliasedParents");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0QueryTransactionContract.AssertTransaction(probe, "sql-commit"));
        Assert.True(probe.CorruptionApplied);
    }

    /// <summary>Names each independently asserted row field, including shape and representation.</summary>
    private static IReadOnlyList<string> RowFields => ["Null", "Id", "Title", "Amount", "Scale", "Timestamp", "Offset", "Notes"];

    /// <summary>Builds four independent fixed rows without calling any contract expectation builder.</summary>
    private static Phase0FixtureRow[] Seed()
    {
        var time = new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567);
        return [new("T1", "seed", 12.3400m, time, null),
            new("T2", "Ångström 文' OR 1=1 --", -2.0000m, time, "Ångström 文"),
            new("T3", "repeat", 12.3400m, time, ""), new("T4", "high", 99.9900m, time, null)];
    }

    /// <summary>Checks that assertions supply the specified seed through the intended seam.</summary>
    /// <param name="actual">The input received by the scripted probe.</param>
    private static void CheckSeed(IReadOnlyList<Phase0FixtureRow> actual)
    {
        var expected = Seed();
        Assert.Equal(expected, actual);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(decimal.GetBits(expected[i].Amount), decimal.GetBits(actual[i].Amount));
            Assert.Equal(expected[i].RecordedAt.Ticks, actual[i].RecordedAt.Ticks);
            Assert.Equal(TimeSpan.Zero, actual[i].RecordedAt.Offset);
        }
    }

    /// <summary>Changes exactly one row field, including equal numeric value with wrong decimal scale.</summary>
    /// <param name="row">Independent correct row.</param>
    /// <param name="field">The sole field to corrupt.</param>
    /// <returns>Changed row or intentional missing row.</returns>
    private static Phase0FixtureRow CorruptRow(Phase0FixtureRow row, string field) => field switch
    {
        "Null" => null!,
        "Id" => row with { Id = "wrong-id" },
        "Title" => row with { Title = "wrong-title" },
        "Amount" => row with { Amount = row.Amount + 1 },
        "Scale" => row with { Amount = WrongScale(row.Amount) },
        "Timestamp" => row with { RecordedAt = row.RecordedAt.AddTicks(1) },
        "Offset" => row with { RecordedAt = new DateTimeOffset(row.RecordedAt.DateTime, TimeSpan.FromHours(1)) },
        "Notes" => row with { Notes = row.Notes is null ? "" : null },
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown row fault.")
    };

    /// <summary>Preserves the numeric value while dropping the independently required four-digit scale.</summary>
    /// <param name="value">A two-decimal numeric fixture stored with four scale digits.</param>
    private static decimal WrongScale(decimal value) => decimal.Parse(value.ToString("F2", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    /// <summary>Scripts actual query observations and verifies input bindings before injecting a fault.</summary>
    /// <param name="faultCall">The single corrupted query, or minus one for correct behavior.</param>
    /// <param name="fault">The independent defect.</param>
    private sealed class ScriptedQueryProbe(int faultCall = -1, string fault = "") : IPhase0QueryProbe
    {
        /// <summary>Records exact query order.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Proves the requested negative control reached its checkpoint.</summary>
        internal bool CorruptionApplied { get; private set; }

        /// <inheritdoc />
        public Phase0QueryObservation? Execute(string query, IReadOnlyList<Phase0FixtureRow> seed,
            string? rawSql, IReadOnlyDictionary<string, string> parameters)
        {
            var call = Calls.Count;
            Assert.Equal(new[] { "filter-order-page", "group-aggregate", "raw-parameter" }[call], query);
            CheckSeed(seed);
            var expectedSql = call == 2 ? "SELECT Id, Title, Amount, RecordedAt, Notes FROM compat_rows WHERE Title = @title ORDER BY Id" : null;
            Assert.Equal(expectedSql, rawSql);
            Assert.Equal(call == 2 ? 1 : 0, parameters.Count);
            if (call == 2) Assert.Equal("Ångström 文' OR 1=1 --", parameters["@title"]);
            Calls.Add(query);
            Phase0QueryObservation result = call switch
            {
                0 => new([Seed()[0], Seed()[2]], [], null, new Dictionary<string, string>()),
                1 => new([], [new(false, 2, 112.3300m), new(true, 2, 10.3400m)], null, new Dictionary<string, string>()),
                _ => new([Seed()[1]], [], expectedSql, new Dictionary<string, string> { ["@title"] = "Ångström 文' OR 1=1 --" })
            };
            if (call != faultCall) return result;
            CorruptionApplied = true;
            if (fault.StartsWith("Row:", StringComparison.Ordinal))
            {
                var parts = fault.Split(':');
                var rows = result.Rows.ToArray();
                var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                rows[index] = CorruptRow(rows[index], parts[2]);
                return result with { Rows = rows };
            }
            if (fault.StartsWith("Group:", StringComparison.Ordinal))
            {
                var parts = fault.Split(':');
                var groups = result.Groups.ToArray();
                var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                var group = groups[index];
                groups[index] = parts[2] switch
                {
                    "Null" => null!, "Key" => group with { HasNotes = !group.HasNotes },
                    "Count" => group with { Count = group.Count + 1 }, "Total" => group with { Total = group.Total + 1 },
                    "Scale" => group with { Total = WrongScale(group.Total) },
                    _ => throw new ArgumentOutOfRangeException(nameof(fault))
                };
                return result with { Groups = groups };
            }
            if (fault == "MutateInputParameter")
            {
                var mutable = Assert.IsType<Dictionary<string, string>>(parameters);
                mutable["@title"] = "rewritten";
                return result with { Parameters = mutable };
            }
            return fault switch
            {
                "Missing" => null, "RowsNull" => result with { Rows = null! },
                "ExtraRow" => result with { Rows = result.Rows.Append(Seed()[3]).ToArray() },
                "MissingRow" => result with { Rows = result.Rows.Skip(1).ToArray() },
                "ReverseRows" => result with { Rows = result.Rows.Reverse().ToArray() },
                "GroupsNull" => result with { Groups = null! },
                "ExtraGroup" => result with { Groups = result.Groups.Append(new(false, 99, 999m)).ToArray() },
                "MissingGroup" => result with { Groups = result.Groups.Skip(1).ToArray() },
                "ReverseGroups" => result with { Groups = result.Groups.Reverse().ToArray() },
                "Sql" => result with { RawSql = "wrong-command" },
                "InterpolatedSql" => result with { RawSql = "SELECT * FROM compat_rows WHERE Title = 'Ångström 文' OR 1=1 --'" },
                "ParametersNull" => result with { Parameters = null! },
                "ExtraParameter" => result with { Parameters = result.Parameters.Append(new("extra", "unexpected")).ToDictionary(pair => pair.Key, pair => pair.Value) },
                "MissingParameter" => result with { Parameters = new Dictionary<string, string>() },
                "ParameterName" => result with { Parameters = new Dictionary<string, string> { ["title"] = "Ångström 文' OR 1=1 --" } },
                "ParameterValue" => result with { Parameters = new Dictionary<string, string> { ["@title"] = "rewritten" } },
                _ => throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown query fault.")
            };
        }
    }

    /// <summary>Independent SQL/history observations, including shared baseline-list corruption.</summary>
    /// <param name="outcome">Requested rollback, SQL commit or explicit Dolt commit.</param>
    /// <param name="faultStage">Only this checkpoint is corrupted, or minus one for correct behavior.</param>
    /// <param name="fault">The independent state defect.</param>
    /// <param name="reverseHistory">Valid inventory-order variation.</param>
    private sealed class ScriptedTransactionProbe(string outcome, int faultStage = -1, string fault = "", bool reverseHistory = false) : IPhase0TransactionProbe
    {
        private const string Root = "native-root-4be";
        private const string Head = "native-head-20c";
        private const string Child = "native-child-93a";
        private readonly List<string> sharedRootParents = [];
        /// <summary>Records the complete invocation sequence.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Proves the intended corruption ran rather than an unrelated assertion failing.</summary>
        internal bool CorruptionApplied { get; private set; }

        /// <inheritdoc />
        public Phase0TransactionState? Begin(IReadOnlyList<Phase0FixtureRow> seed)
        {
            CheckSeed(seed);
            return Observe(0, "Begin");
        }
        /// <inheritdoc />
        public Phase0TransactionState? Mutate() => Observe(1, "Mutate");
        /// <inheritdoc />
        public Phase0TransactionState? CompleteSql(bool commit)
        {
            Assert.Equal(outcome != "rollback", commit);
            return Observe(2, commit ? "SqlCommit" : "Rollback");
        }
        /// <inheritdoc />
        public Phase0TransactionState? CommitHistory()
        {
            Assert.Equal("dolt-commit", outcome);
            return Observe(3, "CommitHistory");
        }

        /// <summary>Builds independent readbacks and applies exactly the requested wrong observation.</summary>
        /// <param name="stage">Before, inside, after SQL, or after explicit history commit.</param>
        /// <param name="operation">The called seam operation.</param>
        /// <returns>Scripted actual state or intentional missing observation.</returns>
        private Phase0TransactionState? Observe(int stage, string operation)
        {
            Assert.Equal(stage, Calls.Count);
            Calls.Add(operation);
            var initial = Seed();
            Phase0FixtureRow[] changed = [initial[0] with { Title = "edited" }, initial[1], initial[3], initial[2] with { Id = "TX1", Title = "inserted" }];
            Phase0HistoryPoint[] history = [new(Root, sharedRootParents), new(Head, [Root])];
            if (stage == 3) history = [.. history, new(Child, [Head])];
            if (reverseHistory) Array.Reverse(history);
            var result = new Phase0TransactionState(stage == 0 || (stage == 2 && outcome == "rollback") ? initial : changed,
                stage == 3 ? Child : Head, Head, history, []);
            if (stage != faultStage) return result;
            CorruptionApplied = true;
            if (fault.StartsWith("Row:", StringComparison.Ordinal))
            {
                var parts = fault.Split(':');
                var rows = result.Rows.ToArray();
                var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                rows[index] = CorruptRow(rows[index], parts[2]);
                return result with { Rows = rows };
            }
            if (fault == "AliasedParents")
            {
                sharedRootParents.Add("rewritten-baseline");
                return result;
            }
            return fault switch
            {
                "Missing" => null, "RowsNull" => result with { Rows = null! },
                "MissingRow" => result with { Rows = result.Rows.Skip(1).ToArray() },
                "ExtraRow" => result with { Rows = result.Rows.Append(Seed()[0]).ToArray() },
                "ReverseRows" => result with { Rows = result.Rows.Reverse().ToArray() },
                "MainBlank" => result with { MainHead = " " }, "RemoteBlank" => result with { RemoteHead = " " },
                "RemoteChanged" => result with { RemoteHead = "unexpected-published-head" },
                "HistoryNull" => result with { History = null! }, "HistoryEmpty" => result with { History = [] },
                "HistoryDuplicate" => result with { History = result.History.Append(result.History[0]).ToArray() },
                "HistoryNullPoint" => result with { History = [null!, .. result.History.Skip(1)] },
                "HistoryBlankHash" => result with { History = [result.History[0] with { Hash = " " }, .. result.History.Skip(1)] },
                "HistoryNullParents" => result with { History = [result.History[0] with { Parents = null! }, .. result.History.Skip(1)] },
                "MainAbsent" => result with { MainHead = "absent-from-history" },
                "IntentsNull" => result with { PublicationIntents = null! }, "Intent" => result with { PublicationIntents = ["unrequested-publication"] },
                "RewriteParents" => result with { History = result.History.Select(point => point.Hash == Root ? point with { Parents = ["rewritten-parent"] } : point).ToArray() },
                "MissingRoot" => result with { History = result.History.Where(point => point.Hash != Root).ToArray() },
                "ImplicitCommit" => result with { MainHead = Child, History = [.. result.History, new(Child, [Head])] },
                "MainUnchanged" => result with { MainHead = Head },
                "NoNewCommit" => result with { MainHead = Head, History = result.History.Where(point => point.Hash != Child).ToArray() },
                "ExtraCommit" => result with { History = [.. result.History, new("unexpected-extra", [Head])] },
                "NewParentWrong" => result with { History = result.History.Select(point => point.Hash == Child ? point with { Parents = [Root] } : point).ToArray() },
                "NewParentExtra" => result with { History = result.History.Select(point => point.Hash == Child ? point with { Parents = [Head, Root] } : point).ToArray() },
                _ => throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown transaction fault.")
            };
        }
    }
}
