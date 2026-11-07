namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2), BDPv4 mock gate: proves the <see cref="IntegrationLaneProjectsContract"/> assertions accept a
/// scripted selector that produces the contract-derived selection and class names, and reject deliberately wrong
/// selectors (negative controls), before any production selector code is written.
/// </summary>
public sealed class IntegrationLaneProjectsMockGateTests
{
    /// <summary>TR-MCP-NUKE-019 AC-2: the scripted selector satisfies every row, class selection and every missing-project refusal.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: unchanged case; the documentation no longer counts the lane projects, which slice S3 changed from seven to six.</remarks>
    [Fact]
    public void ScriptedSelector_SatisfiesTheContract()
        => IntegrationLaneProjectsContract.AssertContract(new ScriptedSelector(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2 negative controls, each rejected inside a passing harness:
    /// - the five projects without QBrainAi.Repl.IntegrationTests and QBrainAi.Support.Mcp.IntegrationTests (the lane before slice A);
    /// - every .IntegrationTests project (the superseded rule);
    /// - the six projects without QBrainAi.Support.Mcp.IntegrationTests (the lane of round 8b);
    /// - every class of QBrainAi.Support.Mcp.IntegrationTests;
    /// - a third class of QBrainAi.Support.Mcp.IntegrationTests;
    /// - the two classes named for every lane project;
    /// - QBrainAi.Acid.IntegrationTests added;
    /// - QBrainAi.PluginIntegration.Tests added;
    /// - the .Validation projects added;
    /// - Build.IntegrationTests dropped;
    /// - QBrainAi.Repl.IntegrationTests run first;
    /// - the lane projects returned in solution order;
    /// - a missing lane project ignored;
    /// - the lane of da11faa2, which still ran the three unit projects whose integration cases slice S3 moved;
    /// - the lane of slice S3, which still ran the infrastructure-owned Build.Tests;
    /// - the lane of slice S4, which did not yet run QBrainAi.Support.Mcp.Environment.IntegrationTests.
    /// </summary>
    /// <param name="fault">Injected fault.</param>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: added the negative control KeepsUnitProjects for slice S3 of hostile validation finding R14-F4, so the contract rejects the lane that still selects QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests; the earlier controls are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S4: DropsBuildTests became DropsBuildIntegrationTests, because Build.Tests left the lane and dropping it would no longer be a fault, and KeepsBuildTests was added, so the contract rejects the lane that still selects the infrastructure-owned Build.Tests; the other controls are unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S5: added the negative control OmitsSupportMcpEnvironmentIntegration, so the contract rejects the lane of slice S4, which does not run the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests (operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the other controls are unchanged.</para>
    /// </remarks>
    [Theory]
    [InlineData(Fault.FixedFiveOnly)]
    [InlineData(Fault.EveryIntegrationProject)]
    [InlineData(Fault.OmitsSupportMcpIntegration)]
    [InlineData(Fault.WholeSupportMcpIntegration)]
    [InlineData(Fault.ExtraSupportClass)]
    [InlineData(Fault.ClassesOnEveryProject)]
    [InlineData(Fault.TakesAcidIntegration)]
    [InlineData(Fault.TakesPluginIntegration)]
    [InlineData(Fault.TakesValidation)]
    [InlineData(Fault.DropsBuildIntegrationTests)]
    [InlineData(Fault.ReplIntegrationFirst)]
    [InlineData(Fault.KeepsSolutionOrder)]
    [InlineData(Fault.IgnoresMissingProject)]
    [InlineData(Fault.KeepsUnitProjects)]
    [InlineData(Fault.KeepsBuildTests)]
    [InlineData(Fault.OmitsSupportMcpEnvironmentIntegration)]
    public void WrongSelector_IsRejected(Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => IntegrationLaneProjectsContract.AssertContract(new ScriptedSelector(fault)));

    /// <summary>Faults injected into the scripted selector for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract-derived selection.</summary>
        None,

        /// <summary>Selects the five unit and build projects only, without the two .IntegrationTests lane projects.</summary>
        FixedFiveOnly,

        /// <summary>Selects the five projects and then every .IntegrationTests project in ordinal order.</summary>
        EveryIntegrationProject,

        /// <summary>Selects the six projects of round 8b, without QBrainAi.Support.Mcp.IntegrationTests.</summary>
        OmitsSupportMcpIntegration,

        /// <summary>Names no class for QBrainAi.Support.Mcp.IntegrationTests, so every class runs.</summary>
        WholeSupportMcpIntegration,

        /// <summary>Names a third class for QBrainAi.Support.Mcp.IntegrationTests.</summary>
        ExtraSupportClass,

        /// <summary>Names the two classes for every lane project.</summary>
        ClassesOnEveryProject,

        /// <summary>Also selects QBrainAi.Acid.IntegrationTests.</summary>
        TakesAcidIntegration,

        /// <summary>Also selects QBrainAi.PluginIntegration.Tests.</summary>
        TakesPluginIntegration,

        /// <summary>Also selects the .Validation projects.</summary>
        TakesValidation,

        /// <summary>Leaves out Build.IntegrationTests.</summary>
        /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: was DropsBuildTests (leaves out Build.Tests) until slice S4 took Build.Tests out of the lane.</remarks>
        DropsBuildIntegrationTests,

        /// <summary>Runs QBrainAi.Repl.IntegrationTests before the other lane projects.</summary>
        ReplIntegrationFirst,

        /// <summary>Returns the lane projects in the solution's order instead of the run order.</summary>
        KeepsSolutionOrder,

        /// <summary>Selects what is present instead of refusing a solution that lacks a lane project.</summary>
        IgnoresMissingProject,

        /// <summary>
        /// Selects the lane of da11faa2: QBrainAi.McpAgent.Tests and QBrainAi.Repl.Core.Tests instead of
        /// QBrainAi.McpAgent.IntegrationTests, and QBrainAi.QBAgent.Tests instead of QBrainAi.QBAgent.IntegrationTests.
        /// </summary>
        /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: added (slice S3).</remarks>
        KeepsUnitProjects,

        /// <summary>Selects the lane of slice S3: the infrastructure-owned Build.Tests instead of Build.IntegrationTests.</summary>
        /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: added (slice S4).</remarks>
        KeepsBuildTests,

        /// <summary>
        /// Selects the lane of slice S4: the six projects without the integration-owned
        /// QBrainAi.Support.Mcp.Environment.IntegrationTests.
        /// </summary>
        /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: added (slice S5).</remarks>
        OmitsSupportMcpEnvironmentIntegration,
    }

    /// <summary>Scripted selector: the contract-derived selection and class names, with an optional injected fault.</summary>
    private sealed class ScriptedSelector(Fault fault) : IIntegrationLaneProjects
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Select(IReadOnlyList<string> solutionProjectNames)
        {
            var lane = IntegrationLaneProjectsContract.LaneProjects
                .Where(project => fault != Fault.DropsBuildIntegrationTests || project != "Build.IntegrationTests")
                .Where(project => fault is not (Fault.FixedFiveOnly or Fault.EveryIntegrationProject) || !project.EndsWith(".IntegrationTests", StringComparison.Ordinal))
                .Where(project => fault != Fault.OmitsSupportMcpIntegration || project != IntegrationLaneProjectsContract.SupportIntegrationProject)
                .Where(project => fault != Fault.OmitsSupportMcpEnvironmentIntegration || project != "QBrainAi.Support.Mcp.Environment.IntegrationTests")
                .ToList();
            var missing = lane.Where(project => !solutionProjectNames.Contains(project)).ToList();
            if (missing.Count > 0)
            {
                if (fault != Fault.IgnoresMissingProject)
                    throw new InvalidOperationException("The solution lacks lane projects: " + string.Join(", ", missing));
                lane = lane.Except(missing).ToList();
            }

            return fault switch
            {
                Fault.EveryIntegrationProject => [.. lane, .. solutionProjectNames.Where(name => name.EndsWith(".IntegrationTests", StringComparison.Ordinal)).OrderBy(name => name, StringComparer.Ordinal)],
                Fault.TakesAcidIntegration => [.. lane, "QBrainAi.Acid.IntegrationTests"],
                Fault.TakesPluginIntegration => [.. lane, "QBrainAi.PluginIntegration.Tests"],
                Fault.TakesValidation => [.. lane, .. solutionProjectNames.Where(name => name.EndsWith(".Validation", StringComparison.Ordinal))],
                Fault.ReplIntegrationFirst => ["QBrainAi.Repl.IntegrationTests", .. lane.Where(project => project != "QBrainAi.Repl.IntegrationTests")],
                Fault.KeepsSolutionOrder => [.. solutionProjectNames.Where(lane.Contains)],
                Fault.KeepsUnitProjects => [.. lane.SelectMany(project => project switch
                {
                    "QBrainAi.McpAgent.IntegrationTests" => new[] { "QBrainAi.McpAgent.Tests", "QBrainAi.Repl.Core.Tests" },
                    "QBrainAi.QBAgent.IntegrationTests" => new[] { "QBrainAi.QBAgent.Tests" },
                    _ => new[] { project },
                })],
                Fault.KeepsBuildTests => [.. lane.Select(project => project == "Build.IntegrationTests" ? "Build.Tests" : project)],
                _ => lane,
            };
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ClassesOf(string projectName)
        {
            if (fault == Fault.ClassesOnEveryProject)
                return IntegrationLaneProjectsContract.SupportIntegrationClasses;
            if (projectName != IntegrationLaneProjectsContract.SupportIntegrationProject || fault == Fault.WholeSupportMcpIntegration)
                return [];

            return fault == Fault.ExtraSupportClass
                ? [.. IntegrationLaneProjectsContract.SupportIntegrationClasses, "CustomWebApplicationFactoryTests"]
                : IntegrationLaneProjectsContract.SupportIntegrationClasses;
        }
    }
}
