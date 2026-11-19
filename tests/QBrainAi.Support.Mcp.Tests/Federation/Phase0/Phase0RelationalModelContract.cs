using System.Text.Json.Nodes;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Reads an observed relational model through a replaceable mock or disposable integration adapter.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-002/004/005 B1/B2/B3/B5; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234.</remarks>
public interface IPhase0RelationalModelProbe
{
    /// <summary>Reads all entity metadata for the requested provider and context, without supplying success flags.</summary>
    /// <param name="provider">Exact EF provider name.</param>
    /// <param name="contextType">Exact context CLR full name.</param>
    /// <returns>Observed model envelope, or null if the requested model is unavailable.</returns>
    JsonObject? ReadModel(string provider, string contextType);
}

/// <summary>Checks every field of independently pinned original-provider relational metadata.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-002/004/005 B1/B2/B3/B5; FR-MCP-186/TR-MCP-FEDERATION-013/TEST-MCP-234
/// ac-DoltBaselineAndUpgrade and ac-OriginalProviderMigrationsUnchanged supporting assertions.
/// Metadata equality proves neither physical SQL constraints nor a new Dolt provider mapping, migration or runtime topology.
/// </remarks>
public static class Phase0RelationalModelContract
{
    private static readonly HashSet<string> Inventories = new(StringComparer.Ordinal)
        { "entities", "properties", "indexes", "foreignKeys", "annotations" };

    /// <summary>Reads and compares a complete model while freezing expectations before invoking the adapter.</summary>
    /// <param name="probe">Independent observation source.</param>
    /// <param name="expected">Pinned baseline envelope; metadata provenance outside the model is not compared.</param>
    public static void AssertModel(IPhase0RelationalModelProbe probe, JsonObject expected)
    {
        var frozen = ProjectModel(expected);
        var provider = frozen["provider"]!.GetValue<string>();
        var contextType = frozen["contextType"]!.GetValue<string>();
        var observed = probe.ReadModel(provider, contextType);
        Assert.NotNull(observed);
        var actual = ProjectModel(observed);
        foreach (var identity in new[] { "modelKind", "contextType", "provider" })
            Assert.Equal(frozen[identity]!.GetValue<string>(), actual[identity]!.GetValue<string>());
        var expectedEntities = frozen["entities"]!.AsArray();
        var actualEntities = actual["entities"]!.AsArray();
        Assert.Equal(expectedEntities.Count, actualEntities.Count);
        Assert.Equal(expectedEntities.Select(Name).Order(StringComparer.Ordinal),
            actualEntities.Select(Name).Order(StringComparer.Ordinal));
        Assert.Equal(actualEntities.Count, actualEntities.Select(Name).Distinct(StringComparer.Ordinal).Count());
        foreach (var entity in expectedEntities)
        {
            var match = Assert.Single(actualEntities, candidate => Name(candidate) == Name(entity));
            AssertEntity(entity!.AsObject(), match!.AsObject());
        }
    }

    /// <summary>Compares all entity fields using the same assertions called by the complete-model gate.</summary>
    /// <param name="expected">Independent baseline entity.</param>
    /// <param name="observed">Observed entity, including all scalar, index and relationship attributes.</param>
    public static void AssertEntity(JsonObject expected, JsonObject observed)
        => Assert.Equal(Canonicalize(expected).ToJsonString(), Canonicalize(observed).ToJsonString());

    /// <summary>Extracts the model identity and complete entities, retaining explicit missing/null distinctions.</summary>
    private static JsonObject ProjectModel(JsonObject envelope)
    {
        var projected = new JsonObject();
        foreach (var key in new[] { "modelKind", "contextType", "provider", "entities" })
        {
            Assert.True(envelope.ContainsKey(key), "Missing model field: " + key);
            Assert.NotNull(envelope[key]);
            projected[key] = envelope[key]!.DeepClone();
        }
        Assert.IsType<JsonArray>(projected["entities"]);
        return projected;
    }

    /// <summary>Gets a required nonempty entity identity from observed metadata.</summary>
    private static string Name(JsonNode? entity)
    {
        Assert.IsType<JsonObject>(entity);
        var name = entity["name"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(name), "Entity name is missing or blank.");
        return name;
    }

    /// <summary>Sorts object members and unordered inventories while preserving every ordered column vector.</summary>
    private static JsonNode Canonicalize(JsonNode node, string memberName = "")
    {
        if (node is JsonObject map)
        {
            var result = new JsonObject();
            foreach (var member in map.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                result[member.Key] = member.Value is null ? null : Canonicalize(member.Value, member.Key);
            return result;
        }
        if (node is JsonArray array)
        {
            var children = array.Select(item => item is null ? null : Canonicalize(item)).ToArray();
            if (Inventories.Contains(memberName) && array.All(item => item is JsonObject or null))
                children = children.OrderBy(item => item?.ToJsonString(), StringComparer.Ordinal).ToArray();
            return new JsonArray(children);
        }
        return node.DeepClone();
    }
}
