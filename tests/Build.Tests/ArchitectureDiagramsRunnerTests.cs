namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 (AC-1, AC-2, AC-6 and the wiring of AC-3 to AC-5): Binds the
/// <see cref="ArchitectureDiagramsRunnerContract"/> scenarios, unchanged from the BDPv4 mock gate, to the production
/// <see cref="ArchitectureDiagramsRunner"/> with the production rules and the recording tooling fake.
/// </summary>
public sealed class ArchitectureDiagramsRunnerTests
{
    /// <summary>TR-MCP-NUKE-017: every contract scenario passes against the production runner.</summary>
    /// <param name="scenario">Scenario name from <see cref="ArchitectureDiagramsRunnerContract.ScenarioNames"/>.</param>
    [Theory]
    [InlineData("validation-fails")]
    [InlineData("inspection-errors")]
    [InlineData("render")]
    [InlineData("deterministic")]
    [InlineData("whatif")]
    [InlineData("render-failure")]
    [InlineData("page-and-mermaid")]
    public void ProductionRunner_MeetsScenario(string scenario)
        => ArchitectureDiagramsRunnerContract.AssertScenario(scenario, tooling => new ArchitectureDiagramsRunner(tooling, new ArchitectureDiagramRules()));
}
