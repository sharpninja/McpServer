namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-5): binds the <see cref="IntegrationLanePreconditionContract"/> scenarios and assertions,
/// unchanged from the BDPv4 mock gate, to the production <see cref="InteractiveSessionPrecondition"/>. Pure
/// decision; the host and session flag are inputs, so no real session is read.
/// </summary>
public sealed class IntegrationLanePreconditionTests
{
    private readonly InteractiveSessionPrecondition precondition = new();

    /// <summary>TR-MCP-NUKE-019 AC-5: the production precondition meets every contract scenario.</summary>
    /// <param name="scenario">Scenario name from <see cref="IntegrationLanePreconditionContract.ScenarioNames"/>.</param>
    [Theory]
    [InlineData("windows-interactive")]
    [InlineData("windows-noninteractive")]
    [InlineData("linux-interactive")]
    [InlineData("linux-noninteractive")]
    public void Evaluate_MeetsContract(string scenario)
        => IntegrationLanePreconditionContract.AssertScenario(scenario, precondition);

    /// <summary>TR-MCP-NUKE-019 AC-5: the theory above covers every contract scenario.</summary>
    [Fact]
    public void EvaluateTheory_CoversEveryScenario()
        => Assert.Equal(4, IntegrationLanePreconditionContract.ScenarioNames.Length);
}
