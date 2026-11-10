namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2): Binds the <see cref="IntegrationLaneProjectsContract"/> cases and assertions, unchanged
/// from the BDPv4 mock gate, to the production <see cref="IntegrationLaneProjects"/>, and checks the contract's
/// solution row against the real QBrainAi.sln (read only), so the row cannot drift from the solution the lane selects
/// from. Build-infrastructure test, accountable through TR only.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-NUKE-019 AC-2, AC-7; FR-MCP-BUILD-001: the infrastructure half of this class; its three Category=Integration cases, which read QBrainAi.sln and the QBrainAi.Support.Mcp.IntegrationTests sources, and the helpers only they used (ProjectLine, ReadSolutionProjectNames, FindRepositoryRoot) moved unchanged to the class of the same name in the integration-owned Build.IntegrationTests, which links IntegrationLaneProjectsContract, so this infrastructure-owned project holds no integration case (slice S4, hostile validation finding R14-F4); the contract binding below is unchanged and the solution-file checks named in the summary run there.</remarks>
public sealed class IntegrationLaneProjectsTests
{
    private readonly IntegrationLaneProjects selector = new();

    /// <summary>TR-MCP-NUKE-019 AC-2: the production selector satisfies every row and every missing-project refusal.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: unchanged case; the documentation no longer counts the lane projects, which slice S3 changed from seven to six.</remarks>
    [Fact]
    public void ProductionSelector_SatisfiesTheContract()
        => IntegrationLaneProjectsContract.AssertContract(selector);
}
