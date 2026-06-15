using System.Text.Json;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>TEST-MCP-174 and TEST-MCP-176: Verifies durable BrainSlot STDIO contract metadata.</summary>
public sealed class BrainSlotContractArtifactTests
{
    /// <summary>The STDIO contract manifest includes all BrainSlot tools and constrained safety parameters.</summary>
    [Fact]
    public void StdioToolContract_IncludesBrainSlotToolsAndSafetyParameters()
    {
        var contractPath = Path.Combine(FindRepoRoot(), "docs", "stdio-tool-contract.json");
        using var document = JsonDocument.Parse(File.ReadAllText(contractPath));
        var tools = document.RootElement.GetProperty("tools").EnumerateArray().ToArray();

        AssertBrainSlotToolExists(tools, "brain_slot_list");
        AssertBrainSlotToolExists(tools, "brain_slot_get");
        var upsert = Assert.Single(tools, tool => GetString(tool, "name") == "brain_slot_upsert");
        AssertBrainSlotToolExists(tools, "brain_slot_delete");
        AssertBrainSlotToolExists(tools, "brain_slot_enable");
        AssertBrainSlotToolExists(tools, "brain_slot_disable");
        AssertBrainSlotToolExists(tools, "brain_slot_status");
        var invoke = Assert.Single(tools, tool => GetString(tool, "name") == "brain_slot_invoke");

        var roleValues = upsert
            .GetProperty("parameters")
            .GetProperty("role")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(["LeftHemisphere", "RightHemisphere", "CuriosityEngine", "ArbiterOfTruth"], roleValues);

        var providerValues = upsert
            .GetProperty("parameters")
            .GetProperty("providerKind")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(["OpenAI", "OpenAICompatible"], providerValues);
        Assert.Equal("^(env|config|file):.+", GetString(upsert.GetProperty("parameters").GetProperty("credentialReference"), "pattern"));
        Assert.Equal("POST /mcpserver/brain-slots/{slotId}/invoke", GetString(invoke, "httpEquivalent"));
        Assert.False(invoke.GetProperty("parameters").GetProperty("admitToGraphRag").GetProperty("default").GetBoolean());
    }

    private static void AssertBrainSlotToolExists(JsonElement[] tools, string name)
    {
        Assert.Contains(tools, tool => GetString(tool, "name") == name);
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docs", "stdio-tool-contract.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
