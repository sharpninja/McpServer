using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// Tests for safe brain-slot credential reference resolution. TEST-MCP-177. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because one of its tests writes the process environment
/// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-177; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/BrainSlotCredentialResolverTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; only the namespace changed.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class BrainSlotCredentialResolverTests
{
    /// <summary>
    /// Environment references resolve through the named variable and trim whitespace. The test sets and clears a
    /// uniquely named variable, so it writes the process environment and is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResolveAsync_WithEnvironmentReference_ReturnsTrimmedSecret()
    {
        var variableName = "MCP_BRAIN_SLOT_TEST_KEY_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variableName, "  test-secret  ");
        try
        {
            var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());

            var value = await resolver.ResolveAsync("env:" + variableName, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("test-secret", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
