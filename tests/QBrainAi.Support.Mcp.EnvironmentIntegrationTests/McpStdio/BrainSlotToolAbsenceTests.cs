using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Server;
using QBrainAi.Tests.Shared;
using static QBrainAi.Support.Mcp.Tests.McpStdio.BrainSlotToolAbsenceTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.McpStdio;

/// <summary>
/// FR-MCP-129 and FR-MCP-130 agent-surface removal: QuadBrain brain-slot capabilities are
/// reachable only through the workspace-token REST controller and the OpenAI-compatible
/// chat-completions endpoint. No MCP tool on the Support.Mcp assembly may expose them,
/// and the durable STDIO tool contract must not advertise them.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-129; FR-MCP-130; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (McpStdio/BrainSlotToolAbsenceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (FindRepoRoot) moved unchanged with the class attributes; the helpers both halves use come from McpStdio/BrainSlotToolAbsenceTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class BrainSlotToolAbsenceTests
{
    /// <summary>
    /// Asserts the durable STDIO tool contract artifact advertises no brain-slot tool,
    /// so agent tooling generated from the manifest cannot discover the surface.
    /// </summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void StdioToolContract_ContainsNoBrainSlotTools()
    {
        var contractPath = Path.Combine(FindRepoRoot(), "docs", "stdio-tool-contract.json");
        using var document = JsonDocument.Parse(File.ReadAllText(contractPath));

        var offenders = document.RootElement
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty)
            .Where(name => name.StartsWith(BrainSlotToolPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "docs/stdio-tool-contract.json must not advertise brain-slot tools. Found: " + string.Join("; ", offenders));
    }

    private static string FindRepoRoot() => TestRepositoryRoot.Resolve();
}
