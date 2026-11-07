using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Options;

/// <summary>
/// Tests instance-name resolution and instance validation. The class runs in <see cref="ProcessEnvironmentCollection"/>
/// because two of its tests write the process environment; those two are integration tests (TR-MCP-USECASE-018
/// AC-006, TR-MCP-USECASE-019 AC-008; hostile validation round 5 finding R5-F2 and the sweep of plan section 19.3).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Options/McpInstanceResolverTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases moved unchanged with the class attributes; only the namespace changed.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class McpInstanceResolverTests
{
    /// <summary>
    /// TEST-MCP-QBRAIN-005: QBRAINAI_INSTANCE wins over MCP_INSTANCE when both are set.
    /// Restores both variables after the assertion. It writes the process environment, so it is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void GetRequestedInstanceName_PrefersQBrainAiEnvironmentVariable()
    {
        var previousModern = Environment.GetEnvironmentVariable("QBRAINAI_INSTANCE");
        var previousLegacy = Environment.GetEnvironmentVariable("MCP_INSTANCE");
        try
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", "from-new");
            Environment.SetEnvironmentVariable("MCP_INSTANCE", "from-old");

            var value = McpInstanceResolver.GetRequestedInstanceName([]);

            Assert.Equal("from-new", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", previousModern);
            Environment.SetEnvironmentVariable("MCP_INSTANCE", previousLegacy);
        }
    }

    /// <summary>
    /// TEST-MCP-QBRAIN-005: MCP_INSTANCE still selects the instance when QBRAINAI_INSTANCE is unset.
    /// Restores both variables after the assertion. It writes the process environment, so it is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void GetRequestedInstanceName_FallsBackToLegacyEnvironmentVariable()
    {
        var previousModern = Environment.GetEnvironmentVariable("QBRAINAI_INSTANCE");
        var previousLegacy = Environment.GetEnvironmentVariable("MCP_INSTANCE");
        try
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", null);
            Environment.SetEnvironmentVariable("MCP_INSTANCE", "from-old");

            var value = McpInstanceResolver.GetRequestedInstanceName([]);

            Assert.Equal("from-old", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("QBRAINAI_INSTANCE", previousModern);
            Environment.SetEnvironmentVariable("MCP_INSTANCE", previousLegacy);
        }
    }
}
