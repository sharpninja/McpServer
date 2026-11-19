using System.Text.Json.Nodes;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Proves complete desired-Dolt model assertions reject independent metadata, value and uniqueness faults.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002; FR-MCP-186 ac-DoltBaselineAndUpgrade; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// No provider, database, environment, production adapter or service is used. Grouped cases retain every nested rejection.
/// </remarks>
public sealed class Phase0DoltFullModelMockGateTests
{
    /// <summary>Gets the independently pinned immutable fixture; observations always receive detached clones.</summary>
    private static readonly Lazy<JsonObject> Baseline = new(Phase0DoltFullModelFixture.Load);
    /// <summary>Gets the two separately observed full-model surfaces.</summary>
    private static readonly string[] Models = ["logicalModel", "physicalSchema"];

    /// <summary>Gets every model/entity as a bounded reporting group.</summary>
    public static IEnumerable<object[]> EntityGroups => Models.SelectMany(model => Baseline.Value[model]!["entities"]!.AsArray()
        .Select((entity, index) => new object[] { model, index, Mutations(entity!).Count() }));
    /// <summary>Gets every complete-model checkpoint, model envelope and invalid envelope field.</summary>
    public static IEnumerable<object[]> EnvelopeFaults => new[] { "Created", "Reopened" }.SelectMany(stage => Models.SelectMany(model =>
        new[] { "Unavailable", "DropEntity", "DuplicateEntity", "ExtraEntity", "NullEntity", "Missing:modelKind", "Wrong:modelKind", "Missing:contextType", "Wrong:contextType", "Missing:provider", "Wrong:provider", "Missing:entities" }
        .Select(fault => new object[] { stage, model, fault })));
    /// <summary>Gets both literal scalar fields and inventory faults.</summary>
    public static IEnumerable<object[]> ScalarFaults => Enumerable.Range(0, 15).SelectMany(index => new[] { "Id", "Type", "Value" }.Select(fault => new object[] { index, fault }))
        .Concat(new[] { new object[] { -1, "Drop" }, new object[] { -1, "Duplicate" }, new object[] { -1, "Unavailable" } });
    /// <summary>Gets all physically meaningful conditional outcomes and readback corruptions.</summary>
    public static IEnumerable<object[]> ConditionalFaults => UniqueFixtures().SelectMany(pair =>
        new[] { "Accepted", "NativeError", "Drop:False", "Extra:False", "Unavailable:False", "Drop:True", "Extra:True", "Unavailable:True" }
        .Concat(new[] { false, true }.SelectMany(reopen => Enumerable.Range(0, pair.Value.Accepted ? 2 : 1)
            .SelectMany(index => new[] { "Id", "Workspace", "Key", "Enabled", "Deleted" }.Select(member => $"Field:{reopen}:{index}:{member}"))))
        .Select(fault => new object[] { pair.Key, fault }));

    /// <summary>Accepts full correct inventories, harmless inventory ordering and distinct capture provenance.</summary>
    /// <param name="variation">Independent inventory order variation.</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void CorrectFullModelsPass(int variation)
    {
        var probe = new ScriptedProbe { Variation = variation };
        Phase0DoltFullModelContract.AssertModels(probe, Fresh());
        Assert.Equal(new[] { "Created", "Reopened" }, probe.Calls);
        Assert.Equal(71, Fresh()["logicalModel"]!["entities"]!.AsArray().Count);
        Assert.Equal(968, Fresh()["logicalModel"]!["entities"]!.AsArray().Sum(entity => entity!["properties"]!.AsArray().Count));
        Assert.Equal(970, Fresh()["physicalSchema"]!["entities"]!.AsArray().Sum(entity => entity!["properties"]!.AsArray().Count));
    }

    /// <summary>Rejects missing or corrupted complete-model observations at each real adapter checkpoint.</summary>
    /// <param name="stage">Only this checkpoint is corrupted.</param>
    /// <param name="model">Only this independent observation is corrupted.</param>
    /// <param name="fault">Exact omission or wrong fact.</param>
    [Theory, MemberData(nameof(EnvelopeFaults))]
    public void WrongFullModelEnvelopeIsRejected(string stage, string model, string fault)
    {
        var injected = false;
        var probe = new ScriptedProbe { ModelFault = (current, logical, physical) =>
        {
            if (stage != current) return new(logical, physical);
            injected = true;
            var target = model == "logicalModel" ? logical : physical;
            if (fault == "Unavailable") return model == "logicalModel" ? new(null, physical) : new(logical, null);
            var entities = target["entities"]!.AsArray();
            switch (fault)
            {
                case "DropEntity": entities.RemoveAt(0); break;
                case "DuplicateEntity": entities.Add(entities[0]!.DeepClone()); break;
                case "ExtraEntity": var extra = entities[0]!.DeepClone(); extra["name"] = "extra"; entities.Add(extra); break;
                case "NullEntity": entities[0] = null; break;
                default:
                    var parts = fault.Split(':');
                    if (parts[0] == "Missing") target.Remove(parts[1]); else target[parts[1]] = "wrong";
                    break;
            }
            return new(logical, physical);
        }};
        Assert.ThrowsAny<XunitException>(() => Phase0DoltFullModelContract.AssertModels(probe, Fresh()));
        Assert.True(injected);
    }

    /// <summary>Rejects every scalar leaf, omitted member and array omission/duplication through the same entity assertions.</summary>
    /// <param name="model">Independently pinned logical or physical schema.</param>
    /// <param name="index">One entity reporting group.</param>
    /// <param name="negativeControls">Exact nested rejection denominator retained in the TRX case identity.</param>
    [Theory, MemberData(nameof(EntityGroups))]
    public void EveryWrongEntityFactIsRejected(string model, int index, int negativeControls)
    {
        var expected = Baseline.Value[model]!["entities"]![index]!.AsObject();
        var rejected = 0;
        foreach (var mutate in Mutations(expected))
        {
            var actual = expected.DeepClone().AsObject();
            var injected = false;
            mutate(actual); injected = true;
            Assert.ThrowsAny<XunitException>(() => Phase0RelationalModelContract.AssertEntity(expected, actual));
            Assert.True(injected);
            rejected++;
        }
        Assert.True(negativeControls > 0);
        Assert.Equal(negativeControls, rejected);
    }

    /// <summary>Proves callback mutation cannot rewrite the physical-model expectation after the logical model was read.</summary>
    [Fact]
    public void SharedExpectedPhysicalModelRewriteIsRejected()
    {
        var expected = Fresh();
        var injected = false;
        var probe = new ScriptedProbe { ModelFault = (_, logical, _) =>
        {
            expected["physicalSchema"]!["entities"]![0]!["properties"]![0]!["columnType"] = "tinyint";
            injected = true;
            return new(logical, expected["physicalSchema"]!.AsObject());
        }};
        Assert.ThrowsAny<XunitException>(() => Phase0DoltFullModelContract.AssertModels(probe, expected));
        Assert.True(injected);
    }

    /// <summary>Accepts independently scripted scalar values in either inventory order.</summary>
    /// <param name="reverse">Harmless inventory order.</param>
    [Theory, InlineData(false), InlineData(true)]
    public void CorrectScalarRoundtripsPass(bool reverse)
    {
        var probe = new ScriptedProbe { Variation = reverse ? 1 : 0 };
        Phase0DoltFullModelContract.AssertScalars(probe);
        Assert.Equal(new[] { "Roundtrip" }, probe.Calls);
    }

    /// <summary>Rejects each typed specimen field and missing/duplicated/absent scalar readbacks.</summary>
    /// <param name="index">Corrupted scalar index.</param>
    /// <param name="fault">Wrong field or inventory.</param>
    [Theory, MemberData(nameof(ScalarFaults))]
    public void WrongScalarRoundtripIsRejected(int index, string fault)
    {
        var injected = false;
        var probe = new ScriptedProbe { ScalarFault = rows =>
        {
            injected = true;
            if (fault == "Unavailable") return null;
            var result = rows.ToList();
            if (fault == "Drop") result.RemoveAt(0);
            else if (fault == "Duplicate") result.Add(rows[0]);
            else result[index] = fault switch { "Id" => rows[index] with { Id = "wrong" }, "Type" => rows[index] with { Type = "wrong" }, _ => rows[index] with { Value = "wrong" } };
            return result;
        }};
        Assert.ThrowsAny<XunitException>(() => Phase0DoltFullModelContract.AssertScalars(probe));
        Assert.True(injected);
    }

    /// <summary>Gets each physical duplicate-acceptance/rejection case with independent readback order.</summary>
    public static IEnumerable<object[]> CorrectConditionalCases => UniqueFixtures().Keys.SelectMany(scenario => new[] { new object[] { scenario, false }, new object[] { scenario, true } });
    /// <summary>Accepts independently specified SQL outcome and rows across reopen.</summary>
    /// <param name="scenario">Conditional unique-key case.</param>
    /// <param name="reverse">Inventory order.</param>
    [Theory, MemberData(nameof(CorrectConditionalCases))]
    public void CorrectConditionalUniquenessPasses(string scenario, bool reverse)
    {
        var probe = new ScriptedProbe { Variation = reverse ? 1 : 0 };
        Phase0DoltFullModelContract.AssertConditional(probe, scenario);
        Assert.Equal(new[] { "Insert:" + scenario, "Rows:False", "Rows:True" }, probe.Calls);
    }

    /// <summary>Rejects wrong SQL diagnostics, incorrect predicate inclusion and missing or mutated durable rows.</summary>
    /// <param name="scenario">Conditional unique-key case.</param>
    /// <param name="fault">Exact deliberately wrong observation.</param>
    [Theory, MemberData(nameof(ConditionalFaults))]
    public void WrongConditionalUniquenessIsRejected(string scenario, string fault)
    {
        var injected = false;
        var probe = new ScriptedProbe
        {
            OutcomeFault = outcome => { if (fault is not ("Accepted" or "NativeError")) return outcome; injected = true; return fault == "Accepted" ? outcome with { Accepted = !outcome.Accepted } : outcome with { NativeError = 1064 }; },
            RowsFault = (reopen, rows) =>
            {
                var parts = fault.Split(':');
                if (parts.Length < 2 || parts[1] != reopen.ToString()) return rows;
                injected = true;
                if (parts[0] == "Unavailable") return null;
                var result = rows.ToList();
                if (parts[0] == "Drop") result.RemoveAt(0);
                else if (parts[0] == "Extra") result.Add(rows[0] with { Id = "extra" });
                else
                {
                    var index = int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                    var row = rows[index];
                    result[index] = parts[3] switch { "Id" => row with { Id = "wrong" }, "Workspace" => row with { Workspace = "wrong" }, "Key" => row with { Key = "wrong" }, "Enabled" => row with { Enabled = !row.Enabled }, _ => row with { Deleted = !row.Deleted } };
                }
                return result;
            }
        };
        Assert.ThrowsAny<XunitException>(() => Phase0DoltFullModelContract.AssertConditional(probe, scenario));
        Assert.True(injected);
    }

    /// <summary>Gets detached expected metadata; neither the probe nor a fault can modify the pinned template.</summary>
    private static JsonObject Fresh() => Baseline.Value.DeepClone().AsObject();

    /// <summary>Enumerates deterministic single-fact omissions and corruptions, including null and empty inventories.</summary>
    private static IEnumerable<Action<JsonObject>> Mutations(JsonNode node, object[]? path = null)
    {
        path ??= [];
        if (node is JsonObject map)
        {
            foreach (var pair in map)
            {
                var location = path.ToArray(); var key = pair.Key;
                yield return root => At(root, location).AsObject().Remove(key);
                if (pair.Value is not null && pair.Value is JsonObject or JsonArray)
                    foreach (var mutation in Mutations(pair.Value, [..path, key])) yield return mutation;
                else yield return root => At(root, location).AsObject()[key] = "wrong";
            }
        }
        else if (node is JsonArray array)
        {
            var location = path.ToArray();
            yield return root => At(root, location).AsArray().Add(JsonValue.Create("extra"));
            if (array.Count > 0)
                yield return root => { var values = At(root, location).AsArray(); values.Add(values[0]?.DeepClone()); };
            if (array.Count > 1 && array.All(value => value is JsonValue or null)
                && array.Select(value => value?.ToJsonString()).Distinct(StringComparer.Ordinal).Count() > 1)
                yield return root => { var values = At(root, location).AsArray(); var reversed = values.Select(value => value?.DeepClone()).Reverse().ToArray(); values.Clear(); foreach (var value in reversed) values.Add(value); };
            for (var i = 0; i < array.Count; i++)
            {
                var index = i;
                yield return root => At(root, location).AsArray().RemoveAt(index);
                if (array[index] is JsonObject or JsonArray)
                    foreach (var mutation in Mutations(array[index]!, [..path, index])) yield return mutation;
                else yield return root => At(root, location).AsArray()[index] = "wrong";
            }
        }
    }

    /// <summary>Locates a clone's exact object/array member without evaluating production code.</summary>
    private static JsonNode At(JsonNode root, IEnumerable<object> path)
    {
        foreach (var part in path) root = part is int index ? root[index]! : root[(string)part]!;
        return root;
    }

    /// <summary>Builds independent literal fixture cases, not outcomes calculated by the assertion contract.</summary>
    private static Dictionary<string, UniqueFixture> UniqueFixtures()
    {
        var a = new Phase0ConditionalRow("1", "Atlas", "same", true, false);
        var b = new Phase0ConditionalRow("2", "Atlas", "same", true, false);
        return new(StringComparer.Ordinal)
        {
            ["Product.ActiveDuplicate"] = new(a, b, false),
            ["Product.DeletedDuplicate"] = new(a, b with { Deleted = true }, true),
            ["Product.DeletedThenActive"] = new(a with { Deleted = true }, b, true),
            ["Product.CaseDistinct"] = new(a, b with { Key = "Same" }, true),
            ["Brain.EnabledDuplicate"] = new(a, b, false),
            ["Brain.DisabledDuplicate"] = new(a, b with { Enabled = false }, true),
            ["Brain.DeletedDuplicate"] = new(a, b with { Deleted = true }, true),
            ["Brain.OtherWorkspace"] = new(a, b with { Workspace = "Other" }, true),
            ["Triage.NonNullDuplicate"] = new(a, b, false),
            ["Triage.NullDuplicate"] = new(a with { Key = null }, b with { Key = null }, true),
            ["Triage.OtherWorkspace"] = new(a, b with { Workspace = "Other" }, true)
        };
    }
    /// <summary>Independent known seed, attempted insert and expected SQL result.</summary>
    private sealed record UniqueFixture(Phase0ConditionalRow Seed, Phase0ConditionalRow Attempt, bool Accepted);

    /// <summary>Independent deterministic observations; required operations and input specimens are checked separately.</summary>
    private sealed class ScriptedProbe : IPhase0DoltFullModelProbe
    {
        /// <summary>Gets a harmless inventory/provenance variation.</summary>
        internal int Variation { get; init; }
        /// <summary>Gets the single model observation corruption.</summary>
        internal Func<string, JsonObject, JsonObject, Phase0DoltModelObservation>? ModelFault { get; init; }
        /// <summary>Gets the single reopened scalar corruption.</summary>
        internal Func<IReadOnlyList<Phase0DoltScalar>, IReadOnlyList<Phase0DoltScalar>?>? ScalarFault { get; init; }
        /// <summary>Gets the single SQL outcome corruption.</summary>
        internal Func<Phase0ConditionalOutcome, Phase0ConditionalOutcome>? OutcomeFault { get; init; }
        /// <summary>Gets the single durable row observation corruption.</summary>
        internal Func<bool, IReadOnlyList<Phase0ConditionalRow>, IReadOnlyList<Phase0ConditionalRow>?>? RowsFault { get; init; }
        /// <summary>Gets all requested operations in their exact order.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public Phase0DoltModelObservation ReadModels(string stage)
        {
            Calls.Add(stage);
            var independent = Fresh();
            var logical = independent["logicalModel"]!.AsObject(); var physical = independent["physicalSchema"]!.AsObject();
            if (Variation == 1) foreach (var model in new[] { logical, physical })
                foreach (var entity in model["entities"]!.AsArray())
                    foreach (var name in new[] { "properties", "indexes", "foreignKeys" })
                    {
                        var reversed = entity![name]!.AsArray().Select(item => item?.DeepClone()).Reverse().ToArray();
                        entity[name] = new JsonArray(reversed);
                    }
            if (Variation == 2) { logical["captureNote"] = "different provenance"; physical["captureNote"] = "different provenance"; }
            return ModelFault?.Invoke(stage, logical, physical) ?? new(logical, physical);
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0DoltScalar>? Roundtrip(IReadOnlyList<Phase0DoltScalar> specimens)
        {
            Calls.Add("Roundtrip");
            Phase0DoltScalar[] rows =
            [
                new("null", "System.String", null), new("empty", "System.String", ""), new("unicode", "System.String", "東京😀\0 trailing "),
                new("bytes", "System.Byte[]", "00017FFF00"), new("false", "System.Boolean", "False"), new("true", "System.Boolean", "True"),
                new("int-min", "System.Int32", "-2147483648"), new("int-max", "System.Int32", "2147483647"),
                new("long-min", "System.Int64", "-9223372036854775808"), new("long-max", "System.Int64", "9223372036854775807"),
                new("double", "System.Double.Bits", "3FB999999999999A"), new("utc", "System.DateTime.Utc", "2026-11-17T01:02:03.1234567Z"),
                new("unspecified", "System.DateTime.Unspecified", "2026-11-17T01:02:03.1234567"),
                new("offset-positive", "System.DateTimeOffset", "2026-11-17T01:02:03.1234567+05:45"), new("offset-negative", "System.DateTimeOffset", "2026-11-17T01:02:03.1234567-03:30")
            ];
            Assert.Equal(rows, specimens);
            if (Variation == 1) Array.Reverse(rows);
            return ScalarFault is null ? rows : ScalarFault(rows);
        }
        /// <inheritdoc />
        public Phase0ConditionalOutcome InsertConditional(string scenario, Phase0ConditionalRow seed, Phase0ConditionalRow attempt)
        {
            Calls.Add("Insert:" + scenario);
            var fixture = UniqueFixtures()[scenario];
            Assert.Equal(fixture.Seed, seed); Assert.Equal(fixture.Attempt, attempt);
            var outcome = new Phase0ConditionalOutcome(fixture.Accepted, fixture.Accepted ? null : 1062);
            return OutcomeFault?.Invoke(outcome) ?? outcome;
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0ConditionalRow>? ReadConditional(string scenario, bool reopen)
        {
            Calls.Add("Rows:" + reopen);
            var fixture = UniqueFixtures()[scenario];
            Phase0ConditionalRow[] rows = fixture.Accepted ? [fixture.Seed, fixture.Attempt] : [fixture.Seed];
            if (Variation == 1) Array.Reverse(rows);
            return RowsFault is null ? rows : RowsFault(reopen, rows);
        }
    }
}
