using QBrainAi.Support.Mcp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 (historical) / FR-MCP-MEMORY-017 (historical):
/// New memory verbs appear in tools search, descriptions are agent-oriented,
/// compat verbs remain, and STDIO equals HTTP.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-016; FR-MCP-MEMORY-017; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Memory/MemoryToolsDiscoveryTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class MemoryToolsDiscoveryTests
{
    /// <summary>AC-FR-MCP-MEMORY-017-09: STDIO and Streamable HTTP advertise the same memory verb set.</summary>
    /// <remarks>Integration: loads the committed tool JSON and stdio contracts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void StdioAndHttp_SameVerbSet()
    {
        var httpNames = MemoryS5Catalog.DiscoverMcpToolNames()
            .Where(name => name.StartsWith("memory_", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var stdioNames = MemoryS5Catalog.DiscoverStdioContractMemoryNames()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var jsonNames = MemoryS5Catalog.DiscoverCommittedToolJsonNames()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var verb in MemoryS5Catalog.NewVerbs.Concat(MemoryS5Catalog.CompatVerbs))
        {
            Assert.Contains(verb, httpNames);
            Assert.Contains(verb, stdioNames);
            Assert.Contains(verb, jsonNames);
        }

        Assert.Equal(httpNames, stdioNames);
    }
}
