namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 AC-2: Binds the <see cref="SvgContentFitContract"/> assertions, unchanged from the BDPv4 mock gate,
/// to the production <see cref="SvgContentFitter"/>.
/// </summary>
public sealed class SvgContentFitTests
{
    private readonly SvgContentFitter fitter = new();

    /// <summary>TR-MCP-NUKE-017 AC-2: the production fitter meets every contract scenario.</summary>
    /// <param name="scenario">Scenario name from <see cref="SvgContentFitContract.ScenarioNames"/>.</param>
    [Theory]
    [InlineData("inner")]
    [InlineData("fractional")]
    [InlineData("clamp-to-paper")]
    [InlineData("offset-paper")]
    [InlineData("culture")]
    [InlineData("preserves-bytes")]
    [InlineData("no-viewbox")]
    [InlineData("not-svg")]
    [InlineData("empty-content")]
    [InlineData("infinite-content")]
    public void Fit_MeetsContract(string scenario) => SvgContentFitContract.AssertScenario(scenario, fitter);

    /// <summary>TR-MCP-NUKE-017 AC-2: the theory above covers every contract scenario.</summary>
    [Fact]
    public void FitTheory_CoversEveryScenario() => Assert.Equal(10, SvgContentFitContract.ScenarioNames.Length);
}
