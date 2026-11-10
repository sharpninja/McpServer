namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-8: contract cases and assertions for the Test target's selection filter and the filter line Test
/// logs for each project it runs. The expected filter comes from the criterion (Test runs no quarantined case, and the
/// filter it logs shows the exclusion) and from the categories Test already leaves out (Category=AiReview and
/// Category=Integration); the trait evaluator of <see cref="IntegrationLaneFilterContract.Selects"/> checks what the
/// filter selects for an untagged unit case, an integration case, an AI review case and a quarantined case. The project
/// names are generic and name no real project. The same assertions run against the scripted filter of the BDPv4 mock
/// gate (UnitTestFilterMockGateTests), its wrong filters, and the production filter in the binding.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added (operator 2026-11-07: "Find the AC matching the test behavior.  If none match, quarantine them (keep, but descope them) for later manual investigation."); no contract covered the Test target's filter before, which was a literal in build/Build.Test.cs that the target did not log; written first for mock gate ug-qr-mockgate-1 against the unchanged production code.</remarks>
internal static class UnitTestFilterContract
{
    /// <summary>TR-MCP-NUKE-019 AC-8: the filter Test must pass to the runner for every project it runs.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added; the categories are the ones Test excluded before, and the quarantine exclusion is added last.</remarks>
    internal const string ExpectedFilter = "Category!=AiReview&Category!=Integration&Quarantine!=NoMatchingCriterion";

    /// <summary>Generic project names the log-line assertion is checked with.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    internal static readonly string[] ProjectNames = ["Unit.Alpha.Tests", "Unit.Beta.Tests"];

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8: asserts the whole contract: the exact filter, what it selects, and the logged line for each
    /// project.
    /// </summary>
    /// <param name="filter">The filter under test.</param>
    /// <param name="logLine">The log line under test, for a project name.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    internal static void AssertContract(string filter, Func<string, string> logLine)
    {
        Assert.Equal(ExpectedFilter, filter);
        AssertSelection(filter);
        AssertLogLine(logLine);
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8: asserts what <paramref name="filter"/> selects: an untagged unit case runs; a
    /// Category=Integration case, a Category=AiReview case and a quarantined case (Quarantine=NoMatchingCriterion) do
    /// not. Independent of the exact text, so the negative controls show each assertion rejects a wrong filter alone.
    /// </summary>
    /// <param name="filter">The filter under test.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    internal static void AssertSelection(string filter)
    {
        Assert.True(IntegrationLaneFilterContract.Selects(filter, category: null, platform: null), "An untagged unit case must run in Test.");
        Assert.False(IntegrationLaneFilterContract.Selects(filter, category: "Integration", platform: null), "A Category=Integration case must not run in Test.");
        Assert.False(IntegrationLaneFilterContract.Selects(filter, category: "AiReview", platform: null), "A Category=AiReview case must not run in Test.");
        Assert.False(
            IntegrationLaneFilterContract.Selects(filter, category: null, platform: null, quarantine: IntegrationLaneFilterContract.NoMatchingCriterion),
            "A quarantined unit case (Quarantine=NoMatchingCriterion) must not run in Test (TR-MCP-NUKE-019 AC-8).");
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8: asserts that the line Test logs for each project names the project and the whole filter,
    /// so the log shows the quarantine exclusion.
    /// </summary>
    /// <param name="logLine">The log line under test, for a project name.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    internal static void AssertLogLine(Func<string, string> logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine);
        foreach (var project in ProjectNames)
        {
            var line = logLine(project);
            Assert.Equal($"Unit test filter for {project}: {ExpectedFilter}", line);
            Assert.Contains(IntegrationLaneFilterContract.QuarantineExclusion, line, StringComparison.Ordinal);
        }
    }
}
