using System.Collections;
using System.Reflection;
using System.Text;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-020: deterministic consumer-gate failure tests kept separate from the fixed
/// seven-class external inventory.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-020; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration class this file held (RequirementsTwelfthAdversarialReviewTests, 7 Category=Integration cases) moved to the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"), so this unit-owned project holds only unit cases; the gate helper this class and those cases both call (AssertRaceHadSuccessfulOperation) moved once, unchanged, into Services/RequirementsTwelfthAdversarialReviewTestsSupport.cs beside this file, which the integration project links (DRY), and this class now calls it by that support class name (see the entry on its case).</remarks>
public sealed class RequirementsTwelfthContainmentGateTests
{
    /// <summary>Proves that rejecting every attempted contained operation fails the gate.</summary>
    /// <remarks>2026-11-07, TEST-MCP-USECASE-020; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: calls the gate helper through RequirementsTwelfthAdversarialReviewTestsSupport, its one definition shared with the integration cases, since RequirementsTwelfthAdversarialReviewTests moved to the integration-owned project (slice S8, hostile validation finding R14-F4); the assertion is unchanged.</remarks>
    [Fact]
    public void AllRejectedAttempts_CannotPassConsumerGate()
    {
        _ = Assert.Throws<Xunit.Sdk.InRangeException>(
            () => RequirementsTwelfthAdversarialReviewTestsSupport.AssertRaceHadSuccessfulOperation(
                successfulAttempts: 0,
                totalAttempts: 500));
    }
}
