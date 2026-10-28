namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2), BDPv4 mock gate: proves the <see cref="IntegrationLaneProjectsContract"/> assertions accept a
/// scripted selector that produces the contract-derived selection, and reject deliberately wrong selectors (negative
/// controls), before any production selector code is written.
/// </summary>
public sealed class IntegrationLaneProjectsMockGateTests
{
    /// <summary>TR-MCP-NUKE-019 AC-2: the scripted selector satisfies every contract row and both refusals.</summary>
    [Fact]
    public void ScriptedSelector_SatisfiesTheContract()
        => IntegrationLaneProjectsContract.AssertContract(new ScriptedSelector(Fault.None));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2 negative controls, each rejected inside a passing harness:
    /// - the five projects without QBrainAi.Repl.IntegrationTests (the lane before slice A);
    /// - every .IntegrationTests project (the superseded rule);
    /// - QBrainAi.Support.Mcp.IntegrationTests added;
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
    [InlineData(Fault.TakesSupportMcpIntegration)]
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

        /// <summary>Selects the five unit and build projects only, without QBrainAi.Repl.IntegrationTests.</summary>
        FixedFiveOnly,

        /// <summary>Selects the five projects and then every .IntegrationTests project in ordinal order.</summary>
        EveryIntegrationProject,

        /// <summary>Also selects QBrainAi.Support.Mcp.IntegrationTests.</summary>
        TakesSupportMcpIntegration,

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

    /// <summary>Scripted selector: the contract-derived selection, with an optional injected fault.</summary>
    private sealed class ScriptedSelector(Fault fault) : IIntegrationLaneProjects
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Select(IReadOnlyList<string> solutionProjectNames)
        {
            var lane = IntegrationLaneProjectsContract.LaneProjects
                .Where(project => fault != Fault.DropsBuildTests || project != "Build.Tests")
                .Where(project => fault is not (Fault.FixedFiveOnly or Fault.EveryIntegrationProject) || project != "QBrainAi.Repl.IntegrationTests")
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
                Fault.TakesSupportMcpIntegration => [.. lane, "QBrainAi.Support.Mcp.IntegrationTests"],
                Fault.TakesPluginIntegration => [.. lane, "QBrainAi.PluginIntegration.Tests"],
                Fault.TakesValidation => [.. lane, .. solutionProjectNames.Where(name => name.EndsWith(".Validation", StringComparison.Ordinal))],
                Fault.ReplIntegrationFirst => ["QBrainAi.Repl.IntegrationTests", .. lane.Where(project => project != "QBrainAi.Repl.IntegrationTests")],
                Fault.KeepsSolutionOrder => [.. solutionProjectNames.Where(lane.Contains)],
                _ => lane,
            };
        }
    }
}
