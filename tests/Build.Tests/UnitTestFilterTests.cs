namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-8 binding: runs the <see cref="UnitTestFilterContract"/> assertions, unchanged from the BDPv4 mock
/// gate ug-qr-mockgate-1, against the production <see cref="UnitTestFilter"/> the Test target uses: the exact filter
/// Category!=AiReview&amp;Category!=Integration&amp;Quarantine!=NoMatchingCriterion, what it selects (no integration, AI
/// review or quarantined case), and the line Test logs for each project. Pure string composition; no host, process or
/// test runner is touched.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added with the production filter.</remarks>
public sealed class UnitTestFilterTests
{
    /// <summary>TR-MCP-NUKE-019 AC-8: the production Test filter and log line satisfy the whole contract.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Fact]
    public void ProductionFilter_SatisfiesTheContract()
        => UnitTestFilterContract.AssertContract(UnitTestFilter.Filter, UnitTestFilter.LogLine);

    /// <summary>TR-MCP-NUKE-019 AC-8: the production Test filter's quarantine clause is the shared production constant the lane uses.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added, so the Test and lane exclusions cannot drift apart.</remarks>
    [Fact]
    public void ProductionFilter_UsesTheSharedQuarantineExclusion()
    {
        Assert.Equal(IntegrationLaneFilterContract.QuarantineExclusion, TestQuarantine.Exclusion);
        Assert.EndsWith("&" + TestQuarantine.Exclusion, UnitTestFilter.Filter, StringComparison.Ordinal);
        Assert.EndsWith("&" + TestQuarantine.Exclusion, new IntegrationLaneFilter().Compose(IntegrationLaneHost.Linux), StringComparison.Ordinal);
        Assert.EndsWith("&" + TestQuarantine.Exclusion, new IntegrationLaneFilter().Compose(IntegrationLaneHost.Windows), StringComparison.Ordinal);
    }
}
