using System.Globalization;
using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 AC-2, BDPv4 mock gate: proves the <see cref="SvgContentFitContract"/> assertions accept a scripted
/// fitter (a lookup table of the contract's hand-computed viewBox values) and reject deliberately wrong fitters, before
/// any production fitting code is written.
/// </summary>
public sealed class SvgContentFitMockGateTests
{
    /// <summary>TR-MCP-NUKE-017 AC-2: every scenario passes against the scripted fitter.</summary>
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
    public void ScriptedFitter_PassesScenario(string scenario)
        => SvgContentFitContract.AssertScenario(scenario, new ScriptedFitter(Fault.None));

    /// <summary>TR-MCP-NUKE-017 AC-2: the theory above covers every contract scenario.</summary>
    [Fact]
    public void ScriptedFitterTheory_CoversEveryScenario()
        => Assert.Equal(10, SvgContentFitContract.ScenarioNames.Length);

    /// <summary>
    /// TR-MCP-NUKE-017 AC-2 negative controls: each wrong fitter is rejected by the scenario it violates (no margin,
    /// no clamp to the paper, inward rounding, every viewBox text replaced, empty content accepted, culture formatting).
    /// </summary>
    /// <param name="scenario">Scenario the fault violates.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("inner", Fault.NoMargin)]
    [InlineData("clamp-to-paper", Fault.NoClamp)]
    [InlineData("fractional", Fault.RoundsInward)]
    [InlineData("preserves-bytes", Fault.ReplacesEveryViewBox)]
    [InlineData("empty-content", Fault.AcceptsEmptyBox)]
    [InlineData("culture", Fault.CultureFormatting)]
    public void WrongFitter_IsRejected(string scenario, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SvgContentFitContract.AssertScenario(scenario, new ScriptedFitter(fault)));

    /// <summary>Faults injected into the scripted fitter for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract-derived values.</summary>
        None,

        /// <summary>Crops to the content without the 50-unit margin.</summary>
        NoMargin,

        /// <summary>Lets the grown box extend past the paper.</summary>
        NoClamp,

        /// <summary>Rounds the far edges down instead of up.</summary>
        RoundsInward,

        /// <summary>Replaces every viewBox text in the document, not only the root attribute.</summary>
        ReplacesEveryViewBox,

        /// <summary>Returns the document unchanged for an empty content box.</summary>
        AcceptsEmptyBox,

        /// <summary>Writes unrounded values with the current culture.</summary>
        CultureFormatting,
    }

    /// <summary>Scripted mock: hand-computed viewBox values per contract input, with one optional fault.</summary>
    private sealed class ScriptedFitter(Fault fault) : ISvgContentFitter
    {
        private static readonly Regex RootViewBox = new("^<svg\\b[^>]*?\\sviewBox=\"([^\"]*)\"", RegexOptions.CultureInvariant);

        public string Fit(string svg, SvgContentBox content)
        {
            var match = RootViewBox.Match(svg);
            if (!match.Success)
            {
                throw new ArgumentException("No root viewBox.", nameof(svg));
            }

            var finite = double.IsFinite(content.MinX) && double.IsFinite(content.MinY) && double.IsFinite(content.MaxX) && double.IsFinite(content.MaxY);
            if (!finite || content.MaxX <= content.MinX || content.MaxY <= content.MinY)
            {
                if (fault == Fault.AcceptsEmptyBox)
                {
                    return svg;
                }

                throw new ArgumentException("Empty content.", nameof(content));
            }

            var key = match.Groups[1].Value + "|" + string.Join(",", new[] { content.MinX, content.MinY, content.MaxX, content.MaxY }.Select(v => v.ToString(CultureInfo.InvariantCulture)));
            var value = key switch
            {
                "0 0 1866 1166|393,393,1473,931" => fault == Fault.NoMargin ? "393 393 1080 538" : "343 343 1180 638",
                "0 0 2008 1166|309,393,1642.9,931" => fault == Fault.RoundsInward ? "259 343 1433 638" : "259 343 1434 638",
                "0 0 1866 1166|20,10,1850,1160" => fault == Fault.NoClamp ? "-30 -40 1930 1250" : "0 0 1866 1166",
                "100 50 1000 800|110,60,600,500" => "100 50 550 500",
                "0 0 2008 1166|309.25,393.5,1642.9,931.2" => fault == Fault.CultureFormatting
                    ? string.Join(" ", new[] { 259.25, 343.5, 1433.65, 587.7 }.Select(v => v.ToString(CultureInfo.CurrentCulture)))
                    : "259 343 1434 639",
                _ => throw new InvalidOperationException("Input outside the contract: " + key),
            };

            if (fault == Fault.ReplacesEveryViewBox)
            {
                return Regex.Replace(svg, "viewBox=\"[^\"]*\"", "viewBox=\"" + value + "\"", RegexOptions.CultureInvariant);
            }

            var group = match.Groups[1];
            return string.Concat(svg.AsSpan(0, group.Index), value, svg.AsSpan(group.Index + group.Length));
        }
    }
}
