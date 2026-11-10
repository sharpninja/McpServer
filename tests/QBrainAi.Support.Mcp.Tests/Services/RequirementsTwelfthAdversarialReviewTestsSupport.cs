namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-018; TEST-MCP-USECASE-020: the consumer-gate assertion that both the integration cases of RequirementsTwelfthAdversarialReviewTests (in QBrainAi.Support.Mcp.EnvironmentIntegrationTests) and the unit gate RequirementsTwelfthContainmentGateTests (in QBrainAi.Support.Mcp.Tests) use, defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-018; TEST-MCP-USECASE-020; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from RequirementsTwelfthAdversarialReviewTests (AssertRaceHadSuccessfulOperation) when that class moved to the integration-owned project (slice S8, hostile validation finding R14-F4), so both classes use one definition instead of a copy (DRY); its accessibility stays internal.</remarks>
internal static class RequirementsTwelfthAdversarialReviewTestsSupport
{
    internal static void AssertRaceHadSuccessfulOperation(
        int successfulAttempts,
        int totalAttempts)
    {
        Assert.InRange(
            successfulAttempts,
            low: 1,
            high: totalAttempts);
    }
}
