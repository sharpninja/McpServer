using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Proves complete baseline metadata assertions accept valid scripts and reject independently corrupted facts.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002/004/005 B1/B2/B3/B5; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234
/// ac-DoltBaselineAndUpgrade/ac-OriginalProviderMigrationsUnchanged support only. No provider, database or live service is opened.
/// </remarks>
public sealed partial class Phase0RelationalModelMockGateTests
{
    private static readonly (string Name, string Hash, int Entities, int Properties, int ForeignKeys, int Indexes)[] Pins =
    [
        ("configuration-sqlite", "D93708CEA9C959CABE343D39D422F8AB937D64C64B4497AFB5EED3AC1B0BA635", 22, 162, 17, 22),
        ("identity-sqlite", "79FC2D6B3F5A9B3F606D1C1FE4FDC98BF1D25A6D59EB9BE5A450FA1208FE24EA", 7, 38, 6, 7),
        ("mcp-postgresql", "F53ACFDA6D9C6382BD0F7C3CFC5EAE2358C3E817EC9BBACD22A9E3F6B02059C2", 71, 968, 119, 193),
        ("mcp-sqlite", "1AB40F02B1A18271443870253C61BC1116AE6F5334F6D46E1B265E38B205B51B", 71, 968, 119, 193),
        ("mcp-sqlserver", "1DBD1AC14EAA96F113DBCFBAD8392042A5BFDB4C1AFD505229A5A16209D8B229", 71, 968, 119, 193),
        ("operational-sqlite", "5B3B9FF25D1486C768E907910827231B1E19E73D3B169C6BAC485FF557650B2A", 5, 42, 0, 15),
        ("transaction-sqlite", "15E6E6D659A6452540EAE72ABC27A1D061DA5DBD0BEAF7F23EC42CE340C5B6A2", 10, 73, 1, 0)
    ];

    private static readonly Lazy<IReadOnlyDictionary<string, JsonObject>> Baselines = new(() => Pins.ToDictionary(
        pin => pin.Name, pin => Load(pin.Name, pin.Hash), StringComparer.Ordinal));

    /// <summary>Gets every complete baseline with ordinary, reordered or extra-provenance observations.</summary>
    public static IEnumerable<object[]> ValidModels => Pins.SelectMany(pin => Enumerable.Range(0, 3).Select(variation => new object[] { pin.Name, variation }));

    /// <summary>Checks fixture pins, full inventories, correct probe addressing and all metadata assertions.</summary>
    /// <param name="model">Pinned fixture identity.</param>
    /// <param name="variation">Unchanged, reordered inventories/object members, or changed non-model provenance.</param>
    [Theory, MemberData(nameof(ValidModels))]
    public void CorrectCompleteModelPasses(string model, int variation)
    {
        var expected = Fresh(model);
        var observed = variation == 1 ? Reorder(Fresh(model)).AsObject() : Fresh(model);
        if (variation == 2) observed["timestampLocal"] = "a different capture, not a model attribute";
        var probe = new ScriptedProbe(observed);
        Phase0RelationalModelContract.AssertModel(probe, expected);
        Assert.Equal(new[] { expected["provider"]!.GetValue<string>() + "|" + expected["contextType"]!.GetValue<string>() }, probe.Calls);
        var pin = Pins.Single(item => item.Name == model);
        var entities = expected["entities"]!.AsArray();
        Assert.Equal(pin.Entities, entities.Count);
        Assert.Equal(pin.Properties, entities.Sum(entity => entity!["properties"]!.AsArray().Count));
        Assert.Equal(pin.ForeignKeys, entities.Sum(entity => entity!["foreignKeys"]!.AsArray().Count));
        Assert.Equal(pin.Indexes, entities.Sum(entity => entity!["indexes"]!.AsArray().Count));
    }

    /// <summary>Loads and pins the independently captured original-provider receipt before exposing expectations.</summary>
    private static JsonObject Load(string name, string hash)
    {
        using var stream = typeof(Phase0RelationalModelMockGateTests).Assembly.GetManifestResourceStream(
            "Federation.Phase0.relational-" + name + "-model-r2.json");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))));
        return JsonNode.Parse(text)!.AsObject();
    }

    /// <summary>Detaches a response/expectation object from all retained immutable fixture templates.</summary>
    private static JsonObject Fresh(string model) => Baselines.Value[model].DeepClone().AsObject();

    /// <summary>Reverses unordered observations and object members without changing ordered key or column vectors.</summary>
    private static JsonNode Reorder(JsonNode node, string memberName = "")
    {
        if (node is JsonObject map)
        {
            var result = new JsonObject();
            foreach (var member in map.Reverse()) result[member.Key] = member.Value is null ? null : Reorder(member.Value, member.Key);
            return result;
        }
        if (node is JsonArray array)
        {
            var items = array.Select(item => item is null ? null : Reorder(item)).ToArray();
            if (new[] { "entities", "properties", "indexes", "foreignKeys", "annotations" }.Contains(memberName)
                && array.All(item => item is JsonObject or null)) Array.Reverse(items);
            return new JsonArray(items);
        }
        return node.DeepClone();
    }

    /// <summary>Returns independently prepared metadata and records all requests, with optional callback-time corruption.</summary>
    private sealed class ScriptedProbe(JsonObject? response, Action? duringRead = null) : IPhase0RelationalModelProbe
    {
        /// <summary>Gets the exact requested provider/context identities.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public JsonObject? ReadModel(string provider, string contextType)
        {
            Calls.Add(provider + "|" + contextType);
            duringRead?.Invoke();
            return response;
        }
    }
}

/// <summary>Model-envelope omission, identity, duplicate and aliasing controls for the same baseline contract.</summary>
/// <remarks>2026-11-18: FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 baseline/upgrade and original-provider regression support.</remarks>
public sealed partial class Phase0RelationalModelMockGateTests
{
    /// <summary>Gets each independent envelope corruption for every baseline.</summary>
    public static IEnumerable<object[]> ModelFaults => Pins.SelectMany(pin => new[]
        { "Unavailable", "DropEntity", "DuplicateEntity", "ExtraEntity", "NullEntity", "BlankName", "MissingName", "NullName",
          "Wrong:modelKind", "Wrong:contextType", "Wrong:provider", "Missing:modelKind", "Missing:contextType", "Missing:provider", "Missing:entities",
          "Null:modelKind", "Null:contextType", "Null:provider", "Null:entities", "SharedBaselineMutation" }
        .Select(fault => new object[] { pin.Name, fault }));

    /// <summary>Rejects wrong addressing, missing/duplicated membership and probe-time corruption of shared expectations.</summary>
    /// <param name="model">Independent pinned model.</param>
    /// <param name="fault">Single deliberately invalid observation.</param>
    [Theory, MemberData(nameof(ModelFaults))]
    public void WrongModelEvidenceIsRejected(string model, string fault)
    {
        var expected = Fresh(model);
        var actual = Fresh(model);
        var entities = actual["entities"]!.AsArray();
        if (fault.StartsWith("Wrong:", StringComparison.Ordinal)) actual[fault[6..]] = "WRONG";
        if (fault.StartsWith("Missing:", StringComparison.Ordinal)) actual.Remove(fault[8..]);
        if (fault.StartsWith("Null:", StringComparison.Ordinal)) actual[fault[5..]] = null;
        switch (fault)
        {
            case "DropEntity": entities.RemoveAt(0); break;
            case "DuplicateEntity": entities.Add(entities[0]!.DeepClone()); break;
            case "ExtraEntity":
                var extra = entities[0]!.DeepClone(); extra["name"] = "UnexpectedEntity"; entities.Add(extra); break;
            case "NullEntity": entities[0] = null; break;
            case "BlankName": entities[0]!["name"] = " "; break;
            case "MissingName": entities[0]!.AsObject().Remove("name"); break;
            case "NullName": entities[0]!["name"] = null; break;
        }
        Action? mutate = null;
        if (fault == "SharedBaselineMutation")
        {
            actual = expected;
            mutate = () => expected["entities"]![0]!["table"] = "ALTERED-BY-PROBE";
        }
        var probe = new ScriptedProbe(fault == "Unavailable" ? null : actual, mutate);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RelationalModelContract.AssertModel(probe, expected));
    }
}


/// <summary>Traverses fixture facts to enumerate independent assertion-discrimination controls.</summary>
/// <remarks>2026-11-18: FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234; tests are generated from pinned original facts, not candidate output.</remarks>
public sealed partial class Phase0RelationalModelMockGateTests
{
    /// <summary>Gets a corruption for each scalar, object member and collection, including ordered composite column vectors.</summary>
    private static IEnumerable<(string Path, string Mutation)> FactFaults(JsonNode? node, string path = "", string memberName = "")
    {
        if (node is JsonObject map)
        {
            yield return (path, "ExtraField");
            foreach (var member in map)
            {
                var child = path + "/" + member.Key;
                yield return (child, "Remove");
                if (member.Value is JsonObject or JsonArray) yield return (child, "Null");
                foreach (var fault in FactFaults(member.Value, child, member.Key)) yield return fault;
            }
        }
        else if (node is JsonArray array)
        {
            yield return (path, "ExtraItem");
            if (array.Count > 0)
            {
                yield return (path, "DropItem");
                yield return (path, "DuplicateItem");
            }
            if (array.Count > 1 && array.All(item => item is JsonValue)
                && array.Select(item => item!.ToJsonString()).Distinct(StringComparer.Ordinal).Count() > 1)
                yield return (path, "ReverseVector");
            for (var index = 0; index < array.Count; index++)
                foreach (var fault in FactFaults(array[index], path + "/" + index, memberName)) yield return fault;
        }
        else yield return (path, "Value");
    }

    /// <summary>Finds an object field or array position using traversal tokens, without interpreting entity/property names as paths.</summary>
    private static JsonNode? Locate(JsonNode node, IEnumerable<string> tokens)
    {
        JsonNode? current = node;
        foreach (var token in tokens)
            current = current is JsonArray array ? array[int.Parse(token, System.Globalization.CultureInfo.InvariantCulture)] : current![token];
        return current;
    }

    /// <summary>Changes exactly one pinned metadata observation while leaving the independent expected entity untouched.</summary>
    private static void Corrupt(JsonObject entity, string path, string mutation)
    {
        var tokens = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var target = Locate(entity, tokens);
        if (mutation == "ExtraField") { target!.AsObject()["unexpectedMetadata"] = "WRONG"; return; }
        if (mutation is "ExtraItem" or "DropItem" or "DuplicateItem" or "ReverseVector")
        {
            var array = target!.AsArray();
            if (mutation == "ExtraItem") array.Add("WRONG");
            if (mutation == "DropItem") array.RemoveAt(0);
            if (mutation == "DuplicateItem") array.Add(array[0]?.DeepClone());
            if (mutation == "ReverseVector")
            {
                var reversed = array.Reverse().Select(item => item?.DeepClone()).ToArray();
                array.Clear(); foreach (var item in reversed) array.Add(item);
            }
            return;
        }
        var parent = Locate(entity, tokens.SkipLast(1));
        var key = tokens[^1];
        if (mutation == "Remove") { parent!.AsObject().Remove(key); return; }
        JsonNode? wrong = mutation == "Null" ? null : target is JsonValue scalar && scalar.TryGetValue<bool>(out var boolean)
            ? JsonValue.Create(!boolean) : JsonValue.Create("WRONG:" + target?.ToJsonString());
        if (parent is JsonArray values) values[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)] = wrong;
        else parent![key] = wrong;
    }
}


/// <summary>Exercises a separate expected rejection for every nested fact in all seven complete baseline models.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002/004/005 B1/B2/B3/B5; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234 supporting mock gate.</remarks>
public sealed partial class Phase0RelationalModelMockGateTests
{
    /// <summary>Gets every original entity; each reporting case retains every nested fault and assertion.</summary>
    /// <remarks>2026-11-18: FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234: group reporting to bound discovery/TRX overhead without dropping controls.</remarks>
    public static IEnumerable<object[]> EntityFaults => Pins.SelectMany(pin =>
        Baselines.Value[pin.Name]["entities"]!.AsArray().Select(entity =>
            new object[] { pin.Name, entity!["name"]!.GetValue<string>() }));

    /// <summary>Runs the same complete entity comparison separately against every original nested corruption.</summary>
    /// <param name="model">Pinned original context/provider baseline.</param>
    /// <param name="entityName">Exact baseline entity identity.</param>
    [Theory, MemberData(nameof(EntityFaults))]
    public void WrongEntityEvidenceIsRejected(string model, string entityName)
    {
        var expected = Baselines.Value[model]["entities"]!.AsArray()
            .Single(entity => entity!["name"]!.GetValue<string>() == entityName)!.AsObject();
        foreach (var fault in FactFaults(expected))
        {
            var actual = expected.DeepClone().AsObject();
            Corrupt(actual, fault.Path, fault.Mutation);
            try
            {
                Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RelationalModelContract.AssertEntity(expected, actual));
            }
            catch (Xunit.Sdk.XunitException exception)
            {
                throw new Xunit.Sdk.XunitException($"{model}|{entityName}|{fault.Path}|{fault.Mutation}: {exception.Message}");
            }
        }
    }

    /// <summary>Gets fault totals independently pinned from the original 102,801-case successful Windows TRX.</summary>
    public static IEnumerable<object[]> FaultInventoryPins =>
    [
        ["configuration-sqlite", 4928], ["identity-sqlite", 1230], ["mcp-postgresql", 31140],
        ["mcp-sqlite", 30846], ["mcp-sqlserver", 31140], ["operational-sqlite", 1328], ["transaction-sqlite", 2028]
    ];

    /// <summary>Rejects loss or duplication in the nested control inventory relative to the original run.</summary>
    /// <remarks>2026-11-18: FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234: preserve all 102,640 original negative controls.</remarks>
    /// <param name="model">Original model fixture.</param>
    /// <param name="expectedFaults">Independent original-run fault count, never derived from the current generator.</param>
    [Theory, MemberData(nameof(FaultInventoryPins))]
    public void EveryOriginalNestedFaultIsRetained(string model, int expectedFaults)
    {
        var count = Baselines.Value[model]["entities"]!.AsArray().Sum(entity => FactFaults(entity).Count());
        Assert.Equal(expectedFaults, count);
    }
}
