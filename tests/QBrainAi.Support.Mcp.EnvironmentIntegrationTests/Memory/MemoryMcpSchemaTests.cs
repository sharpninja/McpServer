using System.Text.Json;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 (historical) / TR-MCP-MEMORY-API-002 (historical):
/// MCP tool input schemas document additionalProperties policy for new verbs.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-016; TR-MCP-MEMORY-API-002; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryMcpSchemaTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryS5Catalog is linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryMcpSchemaTests
{
    /// <summary>AC-TR-MCP-MEMORY-API-002-06: Unknown required-breaking fields follow the documented additionalProperties policy.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void UnknownFields_Policy()
    {
        var root = MemoryS5Catalog.FindRepoRoot();
        var policyType = typeof(QBrainAi.Support.Mcp.Services.MemoryLimits).Assembly.GetTypes()
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
