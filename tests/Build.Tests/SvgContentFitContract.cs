using System.Globalization;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 AC-2 ("the content fitted"): contract scenarios and assertions for <see cref="ISvgContentFitter"/>.
/// The fixtures mirror the Structurizr export (root viewBox "0 0 W H", background style, nested groups); the expected
/// viewBox values are computed by hand from the rule (content grown by 50, rounded outward, clamped to the paper).
/// The same assertions run against the scripted mock (BDPv4 mock gate), wrong fitters, and the production fitter.
/// </summary>
internal static class SvgContentFitContract
{
    /// <summary>Every scenario name, in the order the contract checks them.</summary>
    internal static readonly string[] ScenarioNames =
        ["inner", "fractional", "clamp-to-paper", "offset-paper", "culture", "preserves-bytes", "no-viewbox", "not-svg", "empty-content", "infinite-content"];

    /// <summary>Builds an export-shaped SVG with the given root viewBox value.</summary>
    internal static string Export(string viewBox) =>
        "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\" viewBox=\""
        + viewBox
        + "\" style=\"background: #ffffff\"><g transform=\"matrix(1,0,0,1,0,0)\"><g><rect x=\"393\" y=\"393\" width=\"450\" height=\"300\" fill=\"#85bbf0\"/>"
        + "<text font-size=\"24px\"><tspan x=\"618\" dy=\"34px\">viewBox=\"0 0 9 9\"</tspan></text></g></g></svg>";

    /// <summary>Runs one scenario against the fitter and asserts its contract.</summary>
    internal static void AssertScenario(string scenario, ISvgContentFitter fitter)
    {
        switch (scenario)
        {
            case "inner":
                AssertFitted(fitter, "0 0 1866 1166", new SvgContentBox(393, 393, 1473, 931), "343 343 1180 638");
                break;
            case "fractional":
                AssertFitted(fitter, "0 0 2008 1166", new SvgContentBox(309, 393, 1642.9, 931), "259 343 1434 638");
                break;
            case "clamp-to-paper":
                AssertFitted(fitter, "0 0 1866 1166", new SvgContentBox(20, 10, 1850, 1160), "0 0 1866 1166");
                break;
            case "offset-paper":
                AssertFitted(fitter, "100 50 1000 800", new SvgContentBox(110, 60, 600, 500), "100 50 550 500");
                break;
            case "culture":
                var previous = CultureInfo.CurrentCulture;
                try
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                    AssertFitted(fitter, "0 0 2008 1166", new SvgContentBox(309.25, 393.5, 1642.9, 931.2), "259 343 1434 639");
                }
                finally
                {
                    CultureInfo.CurrentCulture = previous;
                }

                break;
            case "preserves-bytes":
                var source = Export("0 0 1866 1166");
                var fitted = fitter.Fit(source, new SvgContentBox(393, 393, 1473, 931));
                Assert.Equal(Export("343 343 1180 638"), fitted);
                break;
            case "no-viewbox":
                Assert.Throws<ArgumentException>(() => fitter.Fit("<svg xmlns=\"http://www.w3.org/2000/svg\"><g/></svg>", new SvgContentBox(1, 1, 2, 2)));
                break;
            case "not-svg":
                Assert.Throws<ArgumentException>(() => fitter.Fit("<html viewBox=\"0 0 10 10\"></html>", new SvgContentBox(1, 1, 2, 2)));
                break;
            case "empty-content":
                Assert.Throws<ArgumentException>(() => fitter.Fit(Export("0 0 1866 1166"), new SvgContentBox(500, 500, 500, 900)));
                break;
            case "infinite-content":
                Assert.Throws<ArgumentException>(() => fitter.Fit(
                    Export("0 0 1866 1166"),
                    new SvgContentBox(double.PositiveInfinity, double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity)));
                break;
            default:
                throw new InvalidOperationException("Unknown scenario " + scenario);
        }
    }

    private static void AssertFitted(ISvgContentFitter fitter, string paper, SvgContentBox content, string expectedViewBox)
    {
        var fitted = fitter.Fit(Export(paper), content);
        Assert.Equal(Export(expectedViewBox), fitted);
    }
}
