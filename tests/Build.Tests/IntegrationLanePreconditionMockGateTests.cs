namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019, BDPv4 mock gate: proves the <see cref="IntegrationLanePreconditionContract"/> assertions
/// accept a scripted precondition that produces the criterion's outcomes, and reject deliberately wrong
/// preconditions (negative controls), before any production precondition code is written.
/// </summary>
public sealed class IntegrationLanePreconditionMockGateTests
{
    /// <summary>TR-MCP-NUKE-019: every scenario passes against the scripted precondition.</summary>
    /// <param name="scenario">Scenario name from <see cref="IntegrationLanePreconditionContract.ScenarioNames"/>.</param>
    [Theory]
    [InlineData("windows-interactive")]
    [InlineData("windows-noninteractive")]
    [InlineData("linux-interactive")]
    [InlineData("linux-noninteractive")]
    public void ScriptedPrecondition_PassesScenario(string scenario)
        => IntegrationLanePreconditionContract.AssertScenario(scenario, new ScriptedPrecondition(Fault.None));

    /// <summary>TR-MCP-NUKE-019: the theory above covers every contract scenario.</summary>
    [Fact]
    public void ScriptedTheory_CoversEveryScenario()
        => InlineTheoryCoverage.AssertRows<IntegrationLanePreconditionMockGateTests>(nameof(ScriptedPrecondition_PassesScenario), IntegrationLanePreconditionContract.ScenarioNames);

    /// <summary>
    /// TR-MCP-NUKE-019 negative controls: a precondition that runs a non-interactive Windows session, stops
    /// without a message (a silent skip), names no session in its message, or blocks a Linux host, is rejected.
    /// </summary>
    /// <param name="scenario">Scenario the fault breaks.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("windows-noninteractive", Fault.RunsWhenNonInteractive)]
    [InlineData("windows-noninteractive", Fault.StopsSilently)]
    [InlineData("windows-noninteractive", Fault.MessageOmitsSession)]
    [InlineData("linux-noninteractive", Fault.BlocksLinux)]
    public void WrongPrecondition_IsRejected(string scenario, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => IntegrationLanePreconditionContract.AssertScenario(scenario, new ScriptedPrecondition(fault)));

    /// <summary>Faults injected into the scripted precondition for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the criterion's outcomes.</summary>
        None,

        /// <summary>Lets a non-interactive Windows session run the lane.</summary>
        RunsWhenNonInteractive,

        /// <summary>Refuses the lane but gives no message, so the run would stop silently.</summary>
        StopsSilently,

        /// <summary>Refuses the lane with a message that does not name the missing session.</summary>
        MessageOmitsSession,

        /// <summary>Applies the Windows session requirement to a Linux host.</summary>
        BlocksLinux,
    }

    /// <summary>Scripted precondition: the criterion's outcomes, with an optional injected fault.</summary>
    private sealed class ScriptedPrecondition(Fault fault) : IIntegrationLanePrecondition
    {
        private const string Message = "MigrationIntegrationTests needs an interactive desktop session on Windows.";

        /// <inheritdoc />
        public IntegrationLaneReadiness Evaluate(IntegrationLaneHost host, bool userInteractive)
        {
            if (fault == Fault.BlocksLinux && !userInteractive)
                return new IntegrationLaneReadiness(false, Message);
            if (host != IntegrationLaneHost.Windows || userInteractive)
                return new IntegrationLaneReadiness(true, null);

            return fault switch
            {
                Fault.RunsWhenNonInteractive => new IntegrationLaneReadiness(true, null),
                Fault.StopsSilently => new IntegrationLaneReadiness(false, null),
                Fault.MessageOmitsSession => new IntegrationLaneReadiness(false, "MigrationIntegrationTests cannot run here."),
                _ => new IntegrationLaneReadiness(false, Message),
            };
        }
    }
}
