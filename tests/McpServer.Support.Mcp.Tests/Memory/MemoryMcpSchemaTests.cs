using System.Text.Json;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// MCP tool input schemas document additionalProperties policy for new verbs.
/// </summary>
public sealed class MemoryMcpSchemaTests
{
    /// <summary>AC-TR-MCP-MEMORY-API-002-06: Unknown required-breaking fields follow the documented additionalProperties policy.</summary>
    [Fact]
    public void UnknownFields_Policy()
    {
        var root = MemoryS5Catalog.FindRepoRoot();
        var policyType = typeof(McpServer.Support.Mcp.Services.MemoryLimits).Assembly.GetTypes()
            .FirstOrDefault(type => type.Name == "MemoryMcpSchemaPolicy");
        Assert.NotNull(policyType);
        var additional = policyType!.GetField("AdditionalPropertiesAllowed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(additional);
        Assert.False((bool)additional!.GetValue(null)!);

        var contract = File.ReadAllText(Path.Combine(root, "docs", "stdio-tool-contract.json"));
        Assert.Contains("additionalProperties", contract, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(contract);
        foreach (var tool in document.RootElement.GetProperty("tools").EnumerateArray())
        {
            var name = tool.GetProperty("name").GetString();
            if (name is null || !MemoryS5Catalog.NewVerbs.Contains(name))
                continue;

            Assert.True(tool.TryGetProperty("additionalProperties", out var flag) ||
                        (tool.TryGetProperty("parameters", out var parameters) &&
                         parameters.TryGetProperty("additionalProperties", out flag)));
            Assert.False(flag.GetBoolean());
        }
    }
}
