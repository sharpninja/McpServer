namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2), BDPv4 mock gate: proves the <see cref="IntegrationLaneProjectsContract"/> assertions accept a
/// scripted selector that produces the contract-derived selection and class names, and reject deliberately wrong
/// selectors (negative controls), before any production selector code is written.
/// </summary>
public sealed class IntegrationLaneProjectsMockGateTests
{
    /// <summary>TR-MCP-NUKE-019 AC-2: the scripted selector satisfies every row, class selection and all seven missing-project refusals.</summary>
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
    /// - Build.Tests dropped;
    /// - QBrainAi.Repl.IntegrationTests run first;
    /// - the lane projects returned in solution order;
    /// - a missing lane project ignored.
    /// </summary>
    /// <param name="fault">Injected fault.</param>
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
    [InlineData(Fault.DropsBuildTests)]
    [InlineData(Fault.ReplIntegrationFirst)]
    [InlineData(Fault.KeepsSolutionOrder)]
    [InlineData(Fault.IgnoresMissingProject)]
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

        /// <summary>Leaves out Build.Tests.</summary>
        DropsBuildTests,

        /// <summary>Runs QBrainAi.Repl.IntegrationTests before the other lane projects.</summary>
        ReplIntegrationFirst,

        /// <summary>Returns the lane projects in the solution's order instead of the run order.</summary>
        KeepsSolutionOrder,

        /// <summary>Selects what is present instead of refusing a solution that lacks a lane project.</summary>
        IgnoresMissingProject,
    }

    /// <summary>Scripted selector: the contract-derived selection and class names, with an optional injected fault.</summary>
    private sealed class ScriptedSelector(Fault fault) : IIntegrationLaneProjects
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Select(IReadOnlyList<string> solutionProjectNames)
        {
            var lane = IntegrationLaneProjectsContract.LaneProjects
                .Where(project => fault != Fault.DropsBuildTests || project != "Build.Tests")
                .Where(project => fault is not (Fault.FixedFiveOnly or Fault.EveryIntegrationProject) || !project.EndsWith(".IntegrationTests", StringComparison.Ordinal))
                .Where(project => fault != Fault.OmitsSupportMcpIntegration || project != IntegrationLaneProjectsContract.SupportIntegrationProject)
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
