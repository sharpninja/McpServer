using Xunit.Sdk;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 AC-8, BDPv4 mock gate: proves the <see cref="UnitTestFilterContract"/> assertions accept a scripted
/// Test filter and log line that state the criterion (Test runs no quarantined case and logs the exclusion), and reject
/// deliberately wrong ones (negative controls), before the production Test filter changes. No checkout, process or
/// environment is read.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added for mock gate ug-qr-mockgate-1 (operator 2026-11-07: "Find the AC matching the test behavior.  If none match, quarantine them (keep, but descope them) for later manual investigation.").</remarks>
public sealed class UnitTestFilterMockGateTests
{
    /// <summary>TR-MCP-NUKE-019 AC-8 positive control: the scripted filter and log line satisfy the whole contract.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Fact]
    public void ScriptedFilter_SatisfiesTheContract()
        => UnitTestFilterContract.AssertContract(ScriptedFilter(Fault.None), project => ScriptedLogLine(Fault.None, project));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8 negative controls, each rejected by the whole contract inside this passing harness: the
    /// quarantine exclusion omitted (the Test filter before AC-8), integration cases admitted, AI review cases
    /// admitted, the quarantine value matched as a category instead of the Quarantine trait, a log line that prints the
    /// filter without the exclusion, and a log line without the project.
    /// </summary>
    /// <param name="fault">The injected fault.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Theory]
    [InlineData(Fault.OmitsQuarantineExclusion)]
    [InlineData(Fault.AdmitsIntegration)]
    [InlineData(Fault.AdmitsAiReview)]
    [InlineData(Fault.QuarantineAsCategory)]
    [InlineData(Fault.LogOmitsQuarantineExclusion)]
    [InlineData(Fault.LogOmitsProject)]
    public void WrongFilter_IsRejected(Fault fault)
        => Assert.ThrowsAny<XunitException>(() => UnitTestFilterContract.AssertContract(ScriptedFilter(fault), project => ScriptedLogLine(fault, project)));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8 negative controls: the selection assertions alone, without the exact-text check, reject each
    /// wrong filter, so each selection assertion is shown to discriminate (quarantined case admitted, integration case
    /// admitted, AI review case admitted, quarantined case admitted through a category term).
    /// </summary>
    /// <param name="fault">The injected fault.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Theory]
    [InlineData(Fault.OmitsQuarantineExclusion)]
    [InlineData(Fault.AdmitsIntegration)]
    [InlineData(Fault.AdmitsAiReview)]
    [InlineData(Fault.QuarantineAsCategory)]
    public void WrongFilter_FailsSelection(Fault fault)
        => Assert.ThrowsAny<XunitException>(() => UnitTestFilterContract.AssertSelection(ScriptedFilter(fault)));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-8 negative controls: the log-line assertion alone rejects a line that prints the filter without
    /// the exclusion and a line without the project.
    /// </summary>
    /// <param name="fault">The injected fault.</param>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    [Theory]
    [InlineData(Fault.LogOmitsQuarantineExclusion)]
    [InlineData(Fault.LogOmitsProject)]
    public void WrongLogLine_IsRejected(Fault fault)
        => Assert.ThrowsAny<XunitException>(() => UnitTestFilterContract.AssertLogLine(project => ScriptedLogLine(fault, project)));

    /// <summary>TR-MCP-NUKE-019 AC-8: faults injected into the scripted Test filter and log line.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    public enum Fault
    {
        /// <summary>No fault: the criterion's filter and log line.</summary>
        None,

        /// <summary>Omits the quarantine exclusion (the Test filter before AC-8).</summary>
        OmitsQuarantineExclusion,

        /// <summary>Drops the Category!=Integration term.</summary>
        AdmitsIntegration,

        /// <summary>Drops the Category!=AiReview term.</summary>
        AdmitsAiReview,

        /// <summary>Excludes the quarantine value as a category (Category!=NoMatchingCriterion) instead of the Quarantine trait.</summary>
        QuarantineAsCategory,

        /// <summary>Logs the filter without the quarantine exclusion while running with it.</summary>
        LogOmitsQuarantineExclusion,

        /// <summary>Logs the filter without naming the project.</summary>
        LogOmitsProject,
    }

    /// <summary>TR-MCP-NUKE-019 AC-8 scripted reference: the Test filter, with an optional injected fault.</summary>
    /// <param name="fault">The injected fault.</param>
    /// <returns>The filter.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static string ScriptedFilter(Fault fault) => fault switch
    {
        Fault.OmitsQuarantineExclusion => "Category!=AiReview&Category!=Integration",
        Fault.AdmitsIntegration => "Category!=AiReview&" + IntegrationLaneFilterContract.QuarantineExclusion,
        Fault.AdmitsAiReview => "Category!=Integration&" + IntegrationLaneFilterContract.QuarantineExclusion,
        Fault.QuarantineAsCategory => "Category!=AiReview&Category!=Integration&Category!=" + IntegrationLaneFilterContract.NoMatchingCriterion,
        _ => "Category!=AiReview&Category!=Integration&" + IntegrationLaneFilterContract.QuarantineExclusion,
    };

    /// <summary>TR-MCP-NUKE-019 AC-8 scripted reference: the line Test logs for one project, with an optional injected fault.</summary>
    /// <param name="fault">The injected fault.</param>
    /// <param name="project">The project name.</param>
    /// <returns>The log line.</returns>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-8: added.</remarks>
    private static string ScriptedLogLine(Fault fault, string project) => fault switch
    {
        Fault.LogOmitsQuarantineExclusion => $"Unit test filter for {project}: Category!=AiReview&Category!=Integration",
        Fault.LogOmitsProject => $"Unit test filter: {ScriptedFilter(Fault.None)}",
        _ => $"Unit test filter for {project}: {ScriptedFilter(fault)}",
    };
}
