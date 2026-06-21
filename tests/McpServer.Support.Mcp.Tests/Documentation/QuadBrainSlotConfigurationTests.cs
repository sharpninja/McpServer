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
            roleAlias: "AoT",
            assignedRuntime: "Grok Build",
            modelId: "grok-build",
            slotId: "brain-slot-arbiter-of-truth-grok-build",
            endpoint: "http://127.0.0.1:8311/v1",
            endpointEnvironmentVariable: "MCP_BRAIN_AOT_ENDPOINT",
            credentialReference: "env:MCP_BRAIN_AOT_API_KEY",
            partyId: "brain-slot:arbiter-of-truth");

        AssertSlot(
            slots,
            role: "CuriosityEngine",
            roleAlias: "Researcher",
            assignedRuntime: "Claude Code CLI",
            modelId: "claude-code-cli-opus-4.8",
            slotId: "brain-slot-curiosity-engine-claude-code-opus-4-8",
            endpoint: "http://127.0.0.1:8312/v1",
            endpointEnvironmentVariable: "MCP_BRAIN_CLAUDE_CODE_ENDPOINT",
            credentialReference: "env:MCP_BRAIN_CLAUDE_CODE_API_KEY",
            partyId: "brain-slot:curiosity-engine");

        AssertSlot(
            slots,
            role: "LeftHemisphere",
            roleAlias: "LeftBrain",
            assignedRuntime: "Claude Code CLI",
            modelId: "claude-code-cli-opus-4.8",
            slotId: "brain-slot-left-hemisphere-claude-code-opus-4-8",
            endpoint: "http://127.0.0.1:8312/v1",
            endpointEnvironmentVariable: "MCP_BRAIN_CLAUDE_CODE_ENDPOINT",
            credentialReference: "env:MCP_BRAIN_CLAUDE_CODE_API_KEY",
            partyId: "brain-slot:left-hemisphere");

        AssertSlot(
            slots,
            role: "RightHemisphere",
            roleAlias: "RightBrain",
            assignedRuntime: "Codex CLI",
            modelId: "codex-cli-gpt-5.5",
            slotId: "brain-slot-right-hemisphere-codex-cli-gpt-5-5",
            endpoint: "http://127.0.0.1:8313/v1",
            endpointEnvironmentVariable: "MCP_BRAIN_CODEX_ENDPOINT",
            credentialReference: "env:MCP_BRAIN_CODEX_API_KEY",
            partyId: "brain-slot:right-hemisphere");
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
        Assert.Equal("OpenAICompatible", GetString(compatibility, "acceptedProviderKind"));
        Assert.Equal("Mcp:BrainSlots:ExecutionEnabled=true", GetString(compatibility, "requiresExecutionGate"));
        Assert.Equal("Mcp:BrainSlots:AllowLoopbackEndpoints=true", GetString(compatibility, "requiresLoopbackGate"));

        var appSettingsPatch = GetMap(document, "appSettingsPatch");
        var mcp = GetMap(appSettingsPatch, "Mcp");
        var brainSlots = GetMap(mcp, "BrainSlots");
        Assert.True(GetBool(brainSlots, "ExecutionEnabled"));
        Assert.True(GetBool(brainSlots, "AllowLoopbackEndpoints"));
        Assert.Equal(180, GetInt(brainSlots, "DefaultTimeoutSeconds"));
        Assert.Equal(300, GetInt(brainSlots, "MaxTimeoutSeconds"));

        var allowedHosts = GetSequence(brainSlots, "AllowedEndpointHosts")
            .Select(ToInvariantString)
            .ToArray();
        Assert.Equal(["127.0.0.1", "localhost"], allowedHosts);

        var environment = GetMap(document, "environment");
        Assert.Equal("set outside source control", GetString(environment, "MCP_BRAIN_AOT_API_KEY"));
        Assert.Equal("set outside source control", GetString(environment, "MCP_BRAIN_CLAUDE_CODE_API_KEY"));
        Assert.Equal("set outside source control", GetString(environment, "MCP_BRAIN_CODEX_API_KEY"));
    }

    private static void AssertApplyOrder(Dictionary<object, object?> document)
    {
        var applyOrder = GetSequence(document, "applyOrder")
            .Select(ToInvariantString)
            .ToArray();

        Assert.Equal(
            [
                "brain-slot-arbiter-of-truth-grok-build",
                "brain-slot-curiosity-engine-claude-code-opus-4-8",
                "brain-slot-left-hemisphere-claude-code-opus-4-8",
                "brain-slot-right-hemisphere-codex-cli-gpt-5-5",
            ],
            applyOrder);
    }

    private static void AssertSlot(
        IReadOnlyList<Dictionary<object, object?>> slots,
        string role,
        string roleAlias,
        string assignedRuntime,
        string modelId,
        string slotId,
        string endpoint,
        string endpointEnvironmentVariable,
        string credentialReference,
        string partyId)
    {
        var slot = Assert.Single(slots, item => GetString(item, "role") == role);

        Assert.Equal(slotId, GetString(slot, "slotId"));
        Assert.Equal(roleAlias, GetString(slot, "roleAlias"));
        Assert.Equal(assignedRuntime, GetString(slot, "assignedRuntime"));
        Assert.Equal("OpenAICompatible", GetString(slot, "providerKind"));
        Assert.Equal(modelId, GetString(slot, "modelId"));
        Assert.Equal(endpoint, GetString(slot, "endpoint"));
        Assert.Equal(endpointEnvironmentVariable, GetString(slot, "endpointEnvironmentVariable"));
        Assert.Equal(credentialReference, GetString(slot, "credentialReference"));
        Assert.Equal(partyId, GetString(slot, "partyId"));
        Assert.StartsWith("env:", credentialReference, StringComparison.Ordinal);
        Assert.True(GetBool(slot, "enabled"));
        Assert.True(GetBool(slot, "replaceExisting"));

        var request = GetMap(slot, "upsertRequest");
        Assert.Equal(role, GetString(request, "role"));
        Assert.Equal("OpenAICompatible", GetString(request, "providerKind"));
        Assert.Equal(modelId, GetString(request, "modelId"));
        Assert.Equal(endpoint, GetString(request, "endpoint"));
        Assert.Equal(credentialReference, GetString(request, "credentialReference"));
        Assert.Equal(partyId, GetString(request, "partyId"));
        Assert.True(GetBool(request, "enabled"));
        Assert.True(GetBool(request, "replaceExisting"));
        Assert.Equal(180, GetInt(request, "timeoutSeconds"));
        Assert.Equal(4096, GetInt(request, "maxOutputTokens"));
    }

    private static Dictionary<object, object?> LoadAssignmentDocument()
    {
        var path = Path.Combine(FindRepoRoot(), "config", "brain-slots", "quad-brain-slot-assignments.yaml");
        var deserializer = new DeserializerBuilder().Build();
        return deserializer.Deserialize<Dictionary<object, object?>>(File.ReadAllText(path));
    }

    private static Dictionary<object, object?> GetMap(Dictionary<object, object?> map, string key)
        => Assert.IsType<Dictionary<object, object?>>(map[key]);

    private static List<object> GetSequence(Dictionary<object, object?> map, string key)
        => Assert.IsType<List<object>>(map[key]);

    private static string GetString(Dictionary<object, object?> map, string key)
        => Assert.IsType<string>(map[key]);

    private static bool GetBool(Dictionary<object, object?> map, string key)
        => map[key] switch
        {
            bool value => value,
            string value when bool.TryParse(value, out var parsed) => parsed,
            var value => throw new InvalidOperationException($"Expected '{key}' to be a boolean but found '{value?.GetType().FullName ?? "<null>"}'."),
        };

    private static int GetInt(Dictionary<object, object?> map, string key)
        => map[key] switch
        {
            int value => value,
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
