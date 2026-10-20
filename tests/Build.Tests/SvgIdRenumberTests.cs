namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 AC-2: Binds the <see cref="SvgIdRenumberContract"/> assertions, unchanged from the BDPv4 mock gate,
/// to the production <see cref="SvgIdRenumberer"/>.
/// </summary>
public sealed class SvgIdRenumberTests
{
    private readonly SvgIdRenumberer renumberer = new();

    /// <summary>TR-MCP-NUKE-017 AC-2: the production renumberer meets every contract scenario.</summary>
    /// <param name="scenario">Scenario name from <see cref="SvgIdRenumberContract.ScenarioNames"/>.</param>
    [Theory]
    [InlineData("sequential")]
    [InlineData("prefixes-independent")]
    [InlineData("offset-invariant")]
    [InlineData("references-follow")]
    [InlineData("definition-order")]
    [InlineData("text-untouched")]
    [InlineData("undefined-reference")]
    [InlineData("swap")]
    [InlineData("other-ids-untouched")]
    [InlineData("idempotent")]
    [InlineData("no-ids")]
    [InlineData("not-svg")]
    public void Renumber_MeetsContract(string scenario) => SvgIdRenumberContract.AssertScenario(scenario, renumberer);

    /// <summary>TR-MCP-NUKE-017 AC-2: the theory above covers every contract scenario.</summary>
    [Fact]
    public void RenumberTheory_CoversEveryScenario() => Assert.Equal(12, SvgIdRenumberContract.ScenarioNames.Length);
}
