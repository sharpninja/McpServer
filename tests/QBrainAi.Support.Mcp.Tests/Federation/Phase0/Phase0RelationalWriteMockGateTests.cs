using System.Globalization;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent correct scripts and wrong-state controls for the representative relational write contract.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// No database or production code is used. Assertions, fixtures and faults remain distinct.
/// </remarks>
public sealed class Phase0RelationalWriteMockGateTests
{
    /// <summary>Gets each prescribed write scenario.</summary>
    private static IReadOnlyList<string> Scenarios => ["concurrency", "primary-key", "unique-index", "foreign-key", "not-null", "restrict-delete"];

    /// <summary>Gets correct observations with both inventory orders, without changing rule semantics.</summary>
    public static IEnumerable<object[]> CorrectCases => Scenarios.SelectMany(scenario => new[] { false, true }
        .Select(reverse => new object[] { scenario, reverse }));

    /// <summary>Gets faults for every independent checkpoint, row field and physical rule field.</summary>
    public static IEnumerable<object[]> WrongCases
    {
        get
        {
            foreach (var scenario in Scenarios)
            for (var stage = 0; stage < (scenario == "concurrency" ? 4 : 3); stage++)
            {
                foreach (var fault in new[] { "Missing", "ParentsNull", "ParentMissing", "ParentExtra", "ParentOrder",
                    "RowsNull", "RowMissing", "RowExtra", "RowOrder", "RulesNull", "RuleMissing", "RuleExtra", "RuleDuplicate",
                    "TokensNull", "ExtraToken", "Count", "Exception", "NativeError" })
                    yield return [scenario, stage, fault];
                for (var row = 0; row < 2; row++)
                foreach (var corruptField in new[] { "Null", "Id", "ParentId", "Title", "Code", "Revision" })
                    yield return [scenario, stage, $"Row:{row}:{corruptField}"];
                for (var rule = 0; rule < 5; rule++)
                foreach (var corruptField in new[] { "Null", "Table", "Kind", "ColumnsNull", "Columns", "PrincipalTable", "PrincipalColumns", "OnDelete" })
                    yield return [scenario, stage, $"Rule:{rule}:{corruptField}"];
                if (scenario == "concurrency" && stage < 3)
                    foreach (var fault in new[] { "ReaderA", "ReaderB", "MissingReaderA", "MissingReaderB" })
                        yield return [scenario, stage, fault];
                if (scenario != "concurrency" && stage == 1)
                    foreach (var fault in new[] { "MissingNativeError", "BlankNativeError" })
                        yield return [scenario, stage, fault];
            }
        }
    }

    /// <summary>All correct scripts pass the unchanged assertions and complete the required operation sequence.</summary>
    /// <param name="scenario">Fixed constraint or concurrency fixture.</param>
    /// <param name="reverse">A valid unordered physical-rule inventory permutation.</param>
    [Theory]
    [MemberData(nameof(CorrectCases))]
    public void CorrectWriteObservationsPass(string scenario, bool reverse)
    {
        var probe = new ScriptedProbe(scenario, reverse: reverse);
        Phase0RelationalWriteContract.AssertWrites(probe, scenario);
        Assert.Equal(scenario == "concurrency" ? new[] { "Open", "winner-A", "stale-B", "Reopen" } : ["Open", scenario, "Reopen"], probe.Calls);
    }

    /// <summary>Requires the intended wrong observation to trigger an assertion, never an arbitrary exception.</summary>
    /// <param name="scenario">Fixed constraint or concurrency fixture.</param>
    /// <param name="stage">Only this checkpoint is corrupted.</param>
    /// <param name="fault">One independently wrong field or omission.</param>
    [Theory]
    [MemberData(nameof(WrongCases))]
    public void WrongWriteObservationIsRejected(string scenario, int stage, string fault)
    {
        var probe = new ScriptedProbe(scenario, stage, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RelationalWriteContract.AssertWrites(probe, scenario));
        Assert.True(probe.CorruptionApplied);
    }

    /// <summary>Returns independently pinned physical fixture rules, not contract-generated expectations.</summary>
    private static Phase0PhysicalRule[] Rules() =>
    [
        new("compat_parents", "PrimaryKey", ["Id"], null, [], null),
        new("compat_children", "PrimaryKey", ["Id"], null, [], null),
        new("compat_children", "Unique", ["Code"], null, [], null),
        new("compat_children", "NotNull", ["Title"], null, [], null),
        new("compat_children", "ForeignKey", ["ParentId"], "compat_parents", ["Id"], "Restrict")
    ];

    /// <summary>Scripts observed EF results, actual physical metadata and durable rereads.</summary>
    /// <param name="scenario">The requested experiment.</param>
    /// <param name="faultStage">A single corrupted checkpoint or minus one.</param>
    /// <param name="fault">One independent wrong result.</param>
    /// <param name="reverse">Valid inventory-order variation.</param>
    private sealed class ScriptedProbe(string scenario, int faultStage = -1, string fault = "", bool reverse = false) : IPhase0RelationalWriteProbe
    {
        /// <summary>Records every operation requested by the assertions.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Confirms the intended negative injection occurred.</summary>
        internal bool CorruptionApplied { get; private set; }
        /// <inheritdoc />
        public Phase0RelationalWriteState? Open(string requestedScenario)
        {
            Assert.Equal(scenario, requestedScenario);
            return Observe(0, "Open");
        }
        /// <inheritdoc />
        public Phase0RelationalWriteState? Write(string operation)
        {
            var expected = scenario == "concurrency" ? (Calls.Count == 1 ? "winner-A" : "stale-B") : scenario;
            Assert.Equal(expected, operation);
            return Observe(Calls.Count, operation);
        }
        /// <inheritdoc />
        public Phase0RelationalWriteState? Reopen() => Observe(scenario == "concurrency" ? 3 : 2, "Reopen");

        /// <summary>Constructs a detached observation and corrupts only the named boundary.</summary>
        /// <param name="stage">Initial, successful/stale write or reopened checkpoint.</param>
        /// <param name="operation">Requested seam operation.</param>
        /// <returns>The independent readback or deliberate null.</returns>
        private Phase0RelationalWriteState? Observe(int stage, string operation)
        {
            Assert.Equal(stage, Calls.Count);
            Calls.Add(operation);
            var concurrent = scenario == "concurrency";
            var saved = concurrent && stage > 0;
            var tokenReaders = new Dictionary<string, int>();
            if (concurrent && stage < 3) { tokenReaders["A"] = stage == 0 ? 7 : 8; tokenReaders["B"] = 7; }
            var rules = Rules();
            if (reverse) Array.Reverse(rules);
            string? exception = concurrent && stage == 2 ? "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"
                : !concurrent && stage == 1 ? "Microsoft.EntityFrameworkCore.DbUpdateException" : null;
            string? native = !concurrent && stage == 1 ? scenario switch
                { "primary-key" or "unique-index" => "1062", "foreign-key" => "1452", "not-null" => "1048", _ => "1451" } : null;
            var result = new Phase0RelationalWriteState(["P1", "P2"],
                [new("C1", "P1", saved ? "winner" : "seed", "alpha", saved ? 8 : 7), new("C2", "P2", "unrelated", "beta", 11)],
                rules, tokenReaders, concurrent && stage == 1 ? 1 : null, exception, native);
            if (stage != faultStage) return result;
            CorruptionApplied = true;
            if (fault.StartsWith("Row:", StringComparison.Ordinal))
            {
                var parts = fault.Split(':');
                var rows = result.Rows.ToArray();
                var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                var row = rows[index];
                rows[index] = parts[2] switch
                {
                    "Null" => null!, "Id" => row with { Id = "wrong" }, "ParentId" => row with { ParentId = "wrong" },
                    "Title" => row with { Title = null }, "Code" => row with { Code = "wrong" }, "Revision" => row with { Revision = row.Revision + 1 },
                    _ => throw new ArgumentOutOfRangeException(nameof(fault))
                };
                return result with { Rows = rows };
            }
            if (fault.StartsWith("Rule:", StringComparison.Ordinal))
            {
                var parts = fault.Split(':');
                var observed = result.Rules.ToArray();
                var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                var rule = observed[index];
                observed[index] = parts[2] switch
                {
                    "Null" => null!, "Table" => rule with { Table = "wrong" }, "Kind" => rule with { Kind = "wrong" },
                    "ColumnsNull" => rule with { Columns = null! }, "Columns" => rule with { Columns = ["wrong"] },
                    "PrincipalTable" => rule with { PrincipalTable = rule.PrincipalTable is null ? "unexpected" : null },
                    "PrincipalColumns" => rule with { PrincipalColumns = ["wrong"] }, "OnDelete" => rule with { OnDelete = "Cascade" },
                    _ => throw new ArgumentOutOfRangeException(nameof(fault))
                };
                return result with { Rules = observed };
            }
            return fault switch
            {
                "Missing" => null, "ParentsNull" => result with { ParentIds = null! },
                "ParentMissing" => result with { ParentIds = ["P1"] }, "ParentExtra" => result with { ParentIds = ["P1", "P2", "extra"] },
                "ParentOrder" => result with { ParentIds = ["P2", "P1"] }, "RowsNull" => result with { Rows = null! },
                "RowMissing" => result with { Rows = result.Rows.Skip(1).ToArray() },
                "RowExtra" => result with { Rows = result.Rows.Append(result.Rows[0]).ToArray() },
                "RowOrder" => result with { Rows = result.Rows.Reverse().ToArray() }, "RulesNull" => result with { Rules = null! },
                "RuleMissing" => result with { Rules = result.Rules.Skip(1).ToArray() },
                "RuleExtra" => result with { Rules = [.. result.Rules, result.Rules[0]] },
                "RuleDuplicate" => result with { Rules = [result.Rules[0], result.Rules[0], .. result.Rules.Skip(2)] },
                "TokensNull" => result with { ReaderTokens = null! },
                "ExtraToken" => result with { ReaderTokens = result.ReaderTokens.Append(new("extra", 7)).ToDictionary(pair => pair.Key, pair => pair.Value) },
                "ReaderA" => result with { ReaderTokens = new Dictionary<string, int> { ["A"] = 999, ["B"] = 7 } },
                "ReaderB" => result with { ReaderTokens = new Dictionary<string, int> { ["A"] = stage == 0 ? 7 : 8, ["B"] = 999 } },
                "MissingReaderA" => result with { ReaderTokens = new Dictionary<string, int> { ["B"] = 7 } },
                "MissingReaderB" => result with { ReaderTokens = new Dictionary<string, int> { ["A"] = stage == 0 ? 7 : 8 } },
                "Count" => result with { AffectedRows = result.AffectedRows is null ? 0 : 99 },
                "Exception" => result with { ExceptionType = "wrong-exception" }, "NativeError" => result with { NativeErrorCode = "1064" },
                "MissingNativeError" => result with { NativeErrorCode = null }, "BlankNativeError" => result with { NativeErrorCode = " " },
                _ => throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown write fault.")
            };
        }
    }
}
