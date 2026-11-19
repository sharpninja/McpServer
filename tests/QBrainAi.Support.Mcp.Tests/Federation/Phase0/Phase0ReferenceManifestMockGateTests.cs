using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Complete B2 definition and persisted-reference observation seam, separate from the small authored collision fixture.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B2/B3/B4; FR-MCP-182 ac-LegacyIdsRemainValid and
/// FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.
/// All inputs are test-owned design projections. A native adapter is implemented only after the group barrier.</remarks>
internal interface IPhase0ReferenceManifestProbe
{
    /// <summary>Seeds all pinned relationship cases and the explicit reviewed parent mappings before references.</summary>
    /// <param name="source">Full source definitions and original typed reference cases.</param>
    /// <param name="reviewedParents">Complete reviewed parent targets, not an inferred legacy-ID match.</param>
    void Prepare(JsonObject source, JsonArray reviewedParents);
    /// <summary>Reads the complete planned/observed entity/property/reference topology projection independently.</summary>
    /// <returns>Full manifest, with no missing/extra definition.</returns>
    JsonObject? ReadManifest();
    /// <summary>Applies only the supplied reviewed mapping; no source or historical bytes may change.</summary>
    void ApplyReviewedMapping();
    /// <summary>Reads every target reference, typed original key, scope, history and source fingerprint.</summary>
    /// <returns>Complete persisted snapshot, not a fixture-success flag.</returns>
    JsonObject? ReadReferences();
    /// <summary>Reopens independent readers without reseeding.</summary>
    void Reopen();
}

/// <summary>Immutable source-derived B2 projection and shared complete-graph assertions.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004; FR-MCP-182 ac-LegacyIdsRemainValid; FR-MCP-184
/// ac-BackgroundWritersDoNotDirtyVersionedBranch; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.
/// The proposed classification is not accepted native routing evidence. This seam pins its full denominator and later adapter inputs.</remarks>
internal static class Phase0ReferenceManifestContract
{
    private static readonly Lazy<JsonObject> Template = new(Build);

    /// <summary>Returns isolated pinned definitions and all origin/null reference cases.</summary>
    /// <returns>A deep copy; adapter mutation cannot alter the shared expected fixture.</returns>
    internal static JsonObject Fixture() => Template.Value.DeepClone().AsObject();

    /// <summary>Freezes expected complete state before invoking a script or later independent native adapter.</summary>
    /// <param name="probe">Replaceable observation boundary.</param>
    internal static void AssertComplete(IPhase0ReferenceManifestProbe probe)
    {
        var frozen = Fixture();
        var source = frozen["source"]!.DeepClone().AsObject();
        var parents = frozen["parents"]!.DeepClone().AsArray();
        probe.Prepare(source, parents);
        AssertObservation(frozen["manifest"]!, probe.ReadManifest());
        AssertObservation(frozen["before"]!, probe.ReadReferences());
        probe.ApplyReviewedMapping();
        AssertObservation(frozen["after"]!, probe.ReadReferences());
        probe.Reopen();
        AssertObservation(frozen["manifest"]!, probe.ReadManifest());
        AssertObservation(frozen["after"]!, probe.ReadReferences());
    }

    /// <summary>Checks exact values, types, explicit nulls, object membership and ordered column vectors.</summary>
    /// <param name="expected">Independent frozen projection.</param><param name="observed">Independent read.</param>
    internal static void AssertObservation(JsonNode expected, JsonNode? observed)
    {
        Assert.NotNull(observed);
        Assert.Equal(Canonical(expected), Canonical(observed));
    }

    /// <summary>Canonicalizes object order only; ordered inventories and composite vectors remain exact.</summary>
    /// <param name="node">Full or individual observed entry.</param><returns>Order-independent object representation.</returns>
    internal static string Canonical(JsonNode node) => Normalize(node).ToJsonString();

    /// <summary>Normalizes dictionary members while retaining every array's explicit order.</summary>
    private static JsonNode Normalize(JsonNode node) => node switch
    {
        JsonObject map => new JsonObject(map.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value is null ? null : Normalize(pair.Value)))),
        JsonArray array => new JsonArray(array.Select(value => value is null ? null : Normalize(value)).ToArray()),
        _ => node.DeepClone()
    };

    /// <summary>Joins every proposed relationship to corrected R2 nullability and key metadata without opening a database.</summary>
    private static JsonObject Build()
    {
        var classification = Load("proposed-storage-classification.json", "E60B28468F3C884EC7EB70BAA48C2F1457365EB64CB4279DDB146278301AA324");
        var models = new Dictionary<string, JsonObject>(StringComparer.Ordinal)
        {
            ["baseline-ef-model.json"] = Load("relational-mcp-sqlite-model-r2.json", "1AB40F02B1A18271443870253C61BC1116AE6F5334F6D46E1B265E38B205B51B"),
            ["identity-ef-model.json"] = Load("relational-identity-sqlite-model-r2.json", "79FC2D6B3F5A9B3F606D1C1FE4FDC98BF1D25A6D59EB9BE5A450FA1208FE24EA"),
            ["transaction-ef-model.json"] = Load("relational-transaction-sqlite-model-r2.json", "15E6E6D659A6452540EAE72ABC27A1D061DA5DBD0BEAF7F23EC42CE340C5B6A2")
        };
        var entities = classification["entities"]!.AsArray();
        Assert.Equal(88, entities.Count);
        Assert.Equal(1079, entities.Sum(entity => entity!["properties"]!.AsArray().Count));
        var relations = classification["relationships"]!.AsArray();
        Assert.Equal(126, relations.Count);
        var manifestRelations = new JsonArray();
        var originals = new JsonArray();
        var resolved = new JsonArray();
        var parents = new JsonArray();
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var relationNode in relations)
        {
            var relation = relationNode!.AsObject();
            var context = Text(relation, "inputModel");
            var model = models[context];
            var dependent = Assert.Single(model["entities"]!.AsArray(), entity => Text(entity!, "name") == Text(relation, "dependent"))!;
            var principal = Assert.Single(model["entities"]!.AsArray(), entity => Text(entity!, "name") == Text(relation, "principal"))!;
            var fk = Assert.Single(dependent["foreignKeys"]!.AsArray(), key =>
                Text(key!, "principal") == Text(relation, "principal") &&
                Canonical(key!["dependentProperties"]!) == Canonical(relation["dependentProperties"]!) &&
                Canonical(key["principalProperties"]!) == Canonical(relation["principalProperties"]!))!;
            var id = context + "/" + Text(relation, "dependent") + "/" + relation["dependentProperties"]!.ToJsonString() +
                "/" + Text(relation, "principal") + "/" + relation["principalProperties"]!.ToJsonString();
            Assert.True(unique.Add(id));
            var declared = relation.DeepClone().AsObject();
            declared["key"] = id;
            declared["required"] = fk["required"]!.DeepClone();
            declared["unique"] = fk["unique"]!.DeepClone();
            declared["dependentColumnMetadata"] = Columns(dependent, relation["dependentProperties"]!.AsArray());
            declared["principalColumnMetadata"] = Columns(principal, relation["principalProperties"]!.AsArray());
            Assert.Equal(Text(relation, "existingDeleteBehavior"), Text(fk, "deleteBehavior"));
            manifestRelations.Add(declared);
            foreach (var origin in new[] { "LA", "LG" })
            {
                AddCase(false);
                if (!fk["required"]!.GetValue<bool>()) AddCase(true);
                // A nullable composite relationship nulls an actually nullable dependent column only.
                void AddCase(bool absent)
                {
                    var edgeId = id + "/" + origin + (absent ? "/null" : "/linked");
                    var sourceValues = Values(declared["dependentColumnMetadata"]!.AsArray());
                    if (absent)
                    {
                        var columns = declared["dependentColumnMetadata"]!.AsArray();
                        var nullableIndex = Enumerable.Range(0, columns.Count).First(index => columns[index]!["nullable"]!.GetValue<bool>());
                        sourceValues[nullableIndex]!["value"] = null;
                    }
                    var parentValues = Values(declared["principalColumnMetadata"]!.AsArray());
                    var principalDefinition = Assert.Single(entities, entity => Text(entity!, "name") == Text(relation, "principal") && Text(entity!, "inputModel") == context)!;
                    var disposition = Text(principalDefinition, "destination");
                    var parentHandle = "reviewed/Atlas/" + origin + "/" + id;
                    // Handles name fixture rows; they are not invented public ID strings. Operational key values remain unchanged.
                    var target = new JsonObject { ["rowHandle"] = parentHandle, ["destination"] = disposition,
                        ["originalTypedKey"] = parentValues.DeepClone(), ["projectId"] = "Atlas", ["originNodeId"] = origin };
                    if (!absent) parents.Add(new JsonObject { ["edge"] = edgeId, ["parent"] = target.DeepClone() });
                    var original = new JsonObject
                    {
                        ["edge"] = edgeId, ["relationshipKey"] = id, ["sourceContext"] = context,
                        ["projectId"] = "Atlas", ["originNodeId"] = origin, ["dependent"] = Text(relation, "dependent"),
                        ["principal"] = Text(relation, "principal"), ["dependentColumns"] = relation["dependentProperties"]!.DeepClone(),
                        ["principalColumns"] = relation["principalProperties"]!.DeepClone(), ["required"] = fk["required"]!.DeepClone(),
                        ["sourceTypedValues"] = sourceValues, ["routing"] = Text(relation, "routing"),
                        ["target"] = absent ? null : new JsonObject { ["originalTypedKey"] = parentValues.DeepClone() }
                    };
                    originals.Add(original);
                    var translated = original.DeepClone().AsObject();
                    translated["target"] = absent ? null : target;
                    resolved.Add(translated);
                }
            }
        }
        var manifest = new JsonObject { ["entities"] = entities.DeepClone(), ["relationships"] = manifestRelations,
            ["topology"] = classification["topology"]!.DeepClone() };
        var history = new JsonObject();
        foreach (var kind in new[] { "commit", "pr", "session", "memory", "document", "todo-reference" })
            history[kind] = kind + ": TODO-7 / UC-7\r\ncafé 雪\n";
        var source = new JsonObject { ["manifest"] = manifest.DeepClone(), ["references"] = originals.DeepClone(), ["history"] = history.DeepClone() };
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(source))));
        JsonObject Snapshot(JsonArray edges) => new() { ["references"] = edges, ["history"] = history.DeepClone(),
            ["sourceFingerprint"] = sourceHash, ["unrelatedProjectFingerprint"] = "unrelated-project-unchanged",
            ["crossDatabaseForeignKeys"] = 0, ["crossDatabaseJoins"] = 0, ["distributedTransactions"] = 0 };
        return new JsonObject { ["manifest"] = manifest, ["source"] = source, ["parents"] = parents,
            ["before"] = Snapshot(originals), ["after"] = Snapshot(resolved) };
    }

    /// <summary>Retains complete corrected metadata in exact relationship-column order.</summary>
    private static JsonArray Columns(JsonNode entity, JsonArray names) => new(names.Select(name =>
        Assert.Single(entity["properties"]!.AsArray(), property => Text(property!, "name") == name!.GetValue<string>())!.DeepClone()).ToArray());

    /// <summary>Builds deterministic original typed keys; order, type and exact string representation are preserved.</summary>
    private static JsonArray Values(JsonArray columns) => new(columns.Select(column => (JsonNode)new JsonObject
    {
        ["column"] = Text(column!, "name"), ["clrType"] = Text(column!, "clrType"),
        ["value"] = Text(column!, "name") == "WorkspaceId" ? JsonValue.Create("original-workspace") :
            Text(column!, "clrType").Contains("Int", StringComparison.Ordinal) ? JsonValue.Create(7L) : JsonValue.Create("007")
    }).ToArray());

    /// <summary>Reads one required field from immutable source metadata.</summary>
    private static string Text(JsonNode node, string member) => node[member]!.GetValue<string>();

    /// <summary>Checks LF-normalized immutable source hashes before deserializing embedded design evidence.</summary>
    private static JsonObject Load(string name, string hash)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Federation.Phase0." + name);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))));
        return JsonNode.Parse(text)!.AsObject();
    }
}

/// <summary>Complete valid manifest gate plus single-field and whole-inventory rejection controls.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B2-B4; FR-MCP-182 ac-LegacyIdsRemainValid;
/// FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.
/// These are design/mock observations, not database routing, SQL enforcement or migration acceptance.</remarks>
public sealed class Phase0ReferenceManifestMockGateTests
{
    /// <summary>Runs the full 88-entity,1079-property,126-relationship fixture through all adapter checkpoints.</summary>
    [Fact]
    public void CompleteManifestAndReviewedGraphPass() => Phase0ReferenceManifestContract.AssertComplete(new ScriptedProbe());

    /// <summary>Enumerates independently discriminated entries, including each classified property and ordered relationship cell.</summary>
    /// <returns>Every entry field, with complete references and topology included.</returns>
    public static IEnumerable<object[]> EntryFields()
    {
        var fixture = Phase0ReferenceManifestContract.Fixture();
        foreach (var area in new[] { "entities", "relationships" })
        {
            var entries = fixture["manifest"]![area]!.AsArray();
            for (var index = 0; index < entries.Count; index++)
            {
                foreach (var key in entries[index]!.AsObject().Select(pair => pair.Key).Where(key => key != "properties"))
                    yield return [area, index, -1, key];
                if (area == "entities")
                {
                    var properties = entries[index]!["properties"]!.AsArray();
                    for (var property = 0; property < properties.Count; property++)
                        foreach (var key in properties[property]!.AsObject().Select(pair => pair.Key)) yield return [area, index, property, key];
                }
            }
        }
        var references = fixture["after"]!["references"]!.AsArray();
        for (var index = 0; index < references.Count; index++)
            foreach (var key in references[index]!.AsObject().Select(pair => pair.Key)) yield return ["references", index, -1, key];
        foreach (var key in fixture["manifest"]!["topology"]!.AsObject().Select(pair => pair.Key)) yield return ["topology", 0, -1, key];
    }

    /// <summary>Mutates one exact entry field and runs the same assertion called by the full observation contract.</summary>
    /// <param name="area">Pinned inventory.</param><param name="index">Exact entry.</param><param name="property">Optional property index.</param>
    /// <param name="field">Single deliberately wrong observation.</param>
    [Theory]
    [MemberData(nameof(EntryFields))]
    public void WrongEntryFieldIsRejected(string area, int index, int property, string field)
    {
        var fixture = Phase0ReferenceManifestContract.Fixture();
        var expected = area switch
        {
            "references" => fixture["after"]!["references"]![index]!,
            "topology" => fixture["manifest"]!["topology"]!,
            _ => property < 0 ? fixture["manifest"]![area]![index]! : fixture["manifest"]![area]![index]!["properties"]![property]!
        };
        var actual = expected.DeepClone().AsObject();
        actual[field] = actual[field] is null ? JsonValue.Create("wrong") : null;
        Assert.IsAssignableFrom<XunitException>(Record.Exception(() => Phase0ReferenceManifestContract.AssertObservation(expected, actual)));
    }

    /// <summary>Enumerates full-seam omissions, extras, duplicates, ordered vectors, history and source/effect failures.</summary>
    /// <returns>One full adapter-bound fault per checkpoint.</returns>
    public static IEnumerable<object[]> WrongSnapshots()
    {
        for (var checkpoint = 0; checkpoint < 5; checkpoint++)
        {
            yield return [checkpoint, "missing"];
            foreach (var fault in checkpoint is 0 or 3 ? new[] { "entity-missing", "property-missing", "relation-missing", "relation-extra", "relation-duplicate", "vector-order", "topology" } :
                new[] { "reference-missing", "reference-extra", "reference-duplicate", "typed-key", "target", "history", "source", "other", "fk", "join", "transaction" })
                yield return [checkpoint, fault];
        }
    }

    /// <summary>Requires the intended complete-read mutation to execute and fail a shared xUnit assertion.</summary>
    /// <param name="checkpoint">Actual adapter read position.</param><param name="fault">Single fault.</param>
    [Theory]
    [MemberData(nameof(WrongSnapshots))]
    public void WrongSnapshotIsRejected(int checkpoint, string fault)
    {
        var probe = new ScriptedProbe(checkpoint, fault);
        var failure = Record.Exception(() => Phase0ReferenceManifestContract.AssertComplete(probe));
        Assert.True(probe.Injected);
        Assert.IsAssignableFrom<XunitException>(failure);
    }

    /// <summary>Finite independent observation script, with no import algorithm, SQL, context or native implementation.</summary>
    private sealed class ScriptedProbe(int faultCheckpoint = -1, string fault = "") : IPhase0ReferenceManifestProbe
    {
        private JsonObject script = null!;
        private int checkpoint;
        private bool applied;
        private bool reopened;
        /// <summary>Whether the exact requested fault reached its intended read.</summary>
        internal bool Injected { get; private set; }
        /// <inheritdoc/>
        public void Prepare(JsonObject source, JsonArray reviewedParents)
        {
            script = Phase0ReferenceManifestContract.Fixture();
            Phase0ReferenceManifestContract.AssertObservation(script["source"]!, source);
            Phase0ReferenceManifestContract.AssertObservation(script["parents"]!, reviewedParents);
            checkpoint = 0; applied = false; reopened = false;
        }
        /// <inheritdoc/>
        public JsonObject? ReadManifest() => Observe(script["manifest"]!.DeepClone().AsObject());
        /// <inheritdoc/>
        public void ApplyReviewedMapping() { Assert.Equal(2, checkpoint); applied = true; }
        /// <inheritdoc/>
        public JsonObject? ReadReferences() => Observe(script[applied ? "after" : "before"]!.DeepClone().AsObject());
        /// <inheritdoc/>
        public void Reopen() { Assert.True(applied); Assert.Equal(3, checkpoint); reopened = true; }
        /// <summary>Mutates one complete observation while retaining all unrelated fields.</summary>
        private JsonObject? Observe(JsonObject actual)
        {
            if (checkpoint >= 3) Assert.True(reopened);
            var current = checkpoint++;
            if (current != faultCheckpoint) return actual;
            Injected = true;
            if (fault == "missing") return null;
            switch (fault)
            {
                case "entity-missing": actual["entities"]!.AsArray().RemoveAt(0); break;
                case "property-missing": actual["entities"]![0]!["properties"]!.AsArray().RemoveAt(0); break;
                case "relation-missing": actual["relationships"]!.AsArray().RemoveAt(0); break;
                case "relation-extra": actual["relationships"]!.AsArray().Add(new JsonObject { ["extra"] = true }); break;
                case "relation-duplicate": actual["relationships"]!.AsArray().Add(actual["relationships"]![0]!.DeepClone()); break;
                case "vector-order":
                    var composite = actual["relationships"]!.AsArray().First(item => item!["dependentProperties"]!.AsArray().Count > 1)!;
                    var vector = composite["dependentProperties"]!.AsArray();
                    var first = vector[0]!.DeepClone(); vector[0] = vector[1]!.DeepClone(); vector[1] = first; break;
                case "topology": actual["topology"]!["crossStore"] = "cross-database FK"; break;
                case "reference-missing": actual["references"]!.AsArray().RemoveAt(0); break;
                case "reference-extra": actual["references"]!.AsArray().Add(new JsonObject { ["extra"] = true }); break;
                case "reference-duplicate": actual["references"]!.AsArray().Add(actual["references"]![0]!.DeepClone()); break;
                case "typed-key": actual["references"]![0]!["sourceTypedValues"]![0]!["value"] = "7"; break;
                case "target": actual["references"]![0]!["target"] = null; break;
                case "history": actual["history"]!["commit"] = "rewritten"; break;
                case "source": actual["sourceFingerprint"] = "mutated"; break;
                case "other": actual["unrelatedProjectFingerprint"] = "mutated"; break;
                case "fk": actual["crossDatabaseForeignKeys"] = 1; break;
                case "join": actual["crossDatabaseJoins"] = 1; break;
                case "transaction": actual["distributedTransactions"] = 1; break;
                default: throw new InvalidOperationException(fault);
            }
            return actual;
        }
    }
}