using QBrainAi.Support.Mcp;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Options;

/// <summary>
/// TR-MCP-QBRAIN-004 and TR-MCP-QBRAIN-005: product route and configuration aliases. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because one of its tests writes the process environment
/// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-QBRAIN-004; TR-MCP-QBRAIN-005; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Options/ProductAliasCompatibilityTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class ProductAliasCompatibilityTests
{
    /// <summary>
    /// TR-MCP-QBRAIN-005: operator Mcp environment and command-line values beat file and image QBrainAi values.
    /// </summary>
    /// <remarks>
    /// The test imports the whole process environment (<c>AddEnvironmentVariables</c>), so it is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008; hostile validation round 5 finding R5-F2, where an ambient
    /// <c>QBrainAi__ProjectionProbe</c> won over the legacy value and the first assertion failed). It sets the two
    /// variables its assertions read, clears the canonical and legacy forms of every other variable they depend on
    /// (both probe names and both port names), and restores all six; the assertions are unchanged.
    /// </remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public void ProjectCanonicalSection_EnvironmentAndCommandLineBeatFileQBrainAi()
    {
        var envName = "Mcp__ProjectionProbe";
        var cliName = "QBrainAi__ProjectionCliProbe";
        string[] cleared = ["QBrainAi__ProjectionProbe", "Mcp__ProjectionCliProbe", "Mcp__Port", "QBrainAi__Port"];
        var previous = new[] { envName, cliName }.Concat(cleared)
            .ToDictionary(name => name, Environment.GetEnvironmentVariable, StringComparer.Ordinal);
        try
        {
            foreach (var name in cleared)
                Environment.SetEnvironmentVariable(name, null);
            Environment.SetEnvironmentVariable(envName, "/myrepo");
            Environment.SetEnvironmentVariable(cliName, "/fromenv");
            using var configuration = new ConfigurationManager();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QBrainAi:ProjectionProbe"] = "/workspace",
                ["Mcp:ProjectionProbe"] = "/legacy",
                ["QBrainAi:ProjectionCliProbe"] = "/workspace",
                ["Mcp:Port"] = "7000",
                ["QBrainAi:Port"] = "7147",
            });
            configuration.AddEnvironmentVariables();
            configuration.AddCommandLine(["--Mcp:ProjectionCliProbe=/fromcli", "--Mcp:Port=9"]);
            McpInstanceResolver.ProjectCanonicalSectionOverLegacy(configuration);

            Assert.Equal("/myrepo", configuration["Mcp:ProjectionProbe"]);
            Assert.Equal("/fromcli", configuration["Mcp:ProjectionCliProbe"]);
            Assert.Equal("9", configuration["Mcp:Port"]);
            Assert.Equal("/myrepo", McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName: null, "ProjectionProbe"));
        }
        finally
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
