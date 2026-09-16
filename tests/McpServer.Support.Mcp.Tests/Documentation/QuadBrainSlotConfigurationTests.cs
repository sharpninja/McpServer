using YamlDotNet.Serialization;

namespace McpServer.Support.Mcp.Tests.Documentation;

/// <summary>Verifies the prepared Quad-Brain slot assignment artifact.</summary>
public sealed class QuadBrainSlotConfigurationTests
{
    /// <summary>The prepared assignment file contains all four requested brain-slot mappings.</summary>
    [Fact]
    public void QuadBrainSlotAssignments_ContainRequestedRuntimeAndModelMappings()
    {
        var document = LoadAssignmentDocument();
        var slots = GetSequence(document, "slots")
            .Cast<Dictionary<object, object?>>()
            .ToArray();

        Assert.Equal(4, slots.Length);
        AssertRuntimeCompatibility(document);
        AssertApplyOrder(document);

        AssertSlot(
            slots,
            role: "ArbiterOfTruth",
            modelId: "grok-4.6",
            slotId: "brain-slot-arbiter-of-truth-grok-build",
            endpoint: "cli://grok-cli",
            credentialReference: "cli:interactive",
            partyId: "brain-slot:arbiter-of-truth");

        AssertSlot(
            slots,
            role: "CuriosityEngine",
            modelId: "gpt-5.6-sol",
            slotId: "brain-slot-curiosity-engine-codex-cli",
            endpoint: "cli://codex-cli",
            credentialReference: "cli:interactive",
            partyId: "brain-slot:curiosity-engine");

        AssertSlot(
            slots,
            role: "Creativity",
            modelId: "grok-4.6",
            slotId: "brain-slot-creativity-grok-build",
            endpoint: "cli://grok-cli",
            credentialReference: "cli:interactive",
            partyId: "brain-slot:creativity");

        AssertSlot(
            slots,
            role: "Logic",
            modelId: "gpt-5.6-sol",
            slotId: "brain-slot-logic-codex-cli",
            endpoint: "cli://codex-cli",
            credentialReference: "cli:interactive",
            partyId: "brain-slot:logic");
    }

    /// <summary>The generator script builds the YAML artifact through PowerShell object serialization.</summary>
    [Fact]
    public void QuadBrainSlotGenerator_UsesConvertToYaml()
    {
        var scriptPath = Path.Combine(FindRepoRoot(), "scripts", "New-QuadBrainSlotConfiguration.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("ConvertTo-Yaml $InputObject", script, StringComparison.Ordinal);
        Assert.Contains("New-BrainSlotAssignment", script, StringComparison.Ordinal);
    }

    private static void AssertRuntimeCompatibility(Dictionary<object, object?> document)
    {
        var compatibility = GetMap(document, "runtimeCompatibility");
        Assert.Equal("Cli", GetString(compatibility, "acceptedProviderKind"));
        Assert.Equal("Mcp:BrainSlots:ExecutionEnabled=true", GetString(compatibility, "requiresExecutionGate"));
        Assert.Equal("Mcp:TurnTransactions:Enabled=true", GetString(compatibility, "requiresTurnTransactions"));

        var appSettingsPatch = GetMap(document, "appSettingsPatch");
        var mcp = GetMap(appSettingsPatch, "Mcp");
        var brainSlots = GetMap(mcp, "BrainSlots");
        Assert.True(GetBool(brainSlots, "ExecutionEnabled"));
        Assert.False(GetBool(brainSlots, "AllowLoopbackEndpoints"));
        Assert.Equal(180, GetInt(brainSlots, "DefaultTimeoutSeconds"));
        Assert.Equal(300, GetInt(brainSlots, "MaxTimeoutSeconds"));
        Assert.Equal("kingd", GetString(brainSlots, "CliRunAs"));
    }

    private static void AssertApplyOrder(Dictionary<object, object?> document)
    {
        var applyOrder = GetSequence(document, "applyOrder")
            .Select(ToInvariantString)
            .ToArray();

        Assert.Equal(
            [
                "brain-slot-arbiter-of-truth-grok-build",
                "brain-slot-creativity-grok-build",
                "brain-slot-curiosity-engine-codex-cli",
                "brain-slot-logic-codex-cli",
            ],
            applyOrder);
    }

    private static void AssertSlot(
        IReadOnlyList<Dictionary<object, object?>> slots,
        string role,
        string modelId,
        string slotId,
        string endpoint,
        string credentialReference,
        string partyId)
    {
        var slot = Assert.Single(slots, item => GetString(item, "Role") == role);

        Assert.Equal(slotId, GetString(slot, "SlotId"));
        Assert.Equal("Cli", GetString(slot, "ProviderKind"));
        Assert.Equal(modelId, GetString(slot, "ModelId"));
        Assert.Equal(endpoint, GetString(slot, "Endpoint"));
        Assert.Equal(credentialReference, GetString(slot, "CredentialReference"));
        Assert.Equal(partyId, GetString(slot, "PartyId"));
        Assert.True(GetBool(slot, "Enabled"));
        Assert.True(GetBool(slot, "ReplaceExisting"));
        Assert.Equal(180, GetInt(slot, "TimeoutSeconds"));
        Assert.Equal(4096, GetInt(slot, "MaxOutputTokens"));
    }

    private static Dictionary<object, object?> LoadAssignmentDocument()
    {
        var path = Path.Combine(FindRepoRoot(), "config", "brain-slots", "quad-brain-slot-assignments.yaml");
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize<Dictionary<object, object?>>(File.ReadAllText(path));
    }

    private static Dictionary<object, object?> GetMap(Dictionary<object, object?> map, string key)
        => Assert.IsType<Dictionary<object, object?>>(GetValue(map, key));

    private static List<object> GetSequence(Dictionary<object, object?> map, string key)
        => Assert.IsType<List<object>>(GetValue(map, key));

    private static object? GetValue(Dictionary<object, object?> map, string key)
    {
        if (map.TryGetValue(key, out var exact))
            return exact;

        var match = map.Keys.OfType<string>().FirstOrDefault(
            candidate => string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return map[match];

        throw new InvalidOperationException($"Expected key '{key}'.");
    }

    private static string GetString(Dictionary<object, object?> map, string key)
        => Assert.IsType<string>(GetValue(map, key));

    private static bool GetBool(Dictionary<object, object?> map, string key)
        => GetValue(map, key) switch
        {
            bool value => value,
            string value when bool.TryParse(value, out var parsed) => parsed,
            var value => throw new InvalidOperationException($"Expected '{key}' to be a boolean but found '{value?.GetType().FullName ?? "<null>"}'."),
        };

    private static int GetInt(Dictionary<object, object?> map, string key)
        => GetValue(map, key) switch
        {
            int value => value,
            long value => checked((int)value),
            string value when int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed) => parsed,
            var value => throw new InvalidOperationException($"Expected '{key}' to be an integer but found '{value?.GetType().FullName ?? "<null>"}'."),
        };

    private static string ToInvariantString(object item)
        => Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
