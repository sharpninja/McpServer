namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 AC-2: contract scenarios and assertions for <see cref="ISvgIdRenumberer"/>. The fixtures mirror the
/// ids of the Structurizr export (JointJS j_N group ids, v-N view ids, v_N ids) and the references SVG allows
/// (url(#id), href and xlink:href); the expected documents are written by hand from the rule (ids defined by an id
/// attribute numbered from 1 per prefix in definition order, references follow, nothing else changes). The same
/// assertions run against the scripted mock (BDPv4 mock gate), wrong renumberers, and the production renumberer.
/// </summary>
internal static class SvgIdRenumberContract
{
    /// <summary>Every scenario name, in the order the contract checks them.</summary>
    internal static readonly string[] ScenarioNames =
    [
        "sequential", "prefixes-independent", "offset-invariant", "references-follow", "definition-order", "text-untouched",
        "undefined-reference", "swap", "other-ids-untouched", "idempotent", "no-ids", "not-svg",
    ];

    /// <summary>A view as exported, ids from one point of the page-wide counter.</summary>
    internal static readonly string SequentialInput = Svg(
        "<g id=\"j_4144\" transform=\"matrix(1,0,0,1,0,0)\"><g id=\"v-46524\"><rect id=\"v-46530\" fill=\"#85bbf0\"/></g></g>"
        + "<g id=\"j_4150\"><text id=\"v-46601\" font-size=\"24px\">Api</text></g>");

    /// <summary>The same view after the counter moved by +12 (j_) and +151 (v-), by a workspace change or by timing.</summary>
    internal static readonly string SequentialOffsetInput = Svg(
        "<g id=\"j_4156\" transform=\"matrix(1,0,0,1,0,0)\"><g id=\"v-46675\"><rect id=\"v-46681\" fill=\"#85bbf0\"/></g></g>"
        + "<g id=\"j_4162\"><text id=\"v-46752\" font-size=\"24px\">Api</text></g>");

    /// <summary>Both views renumbered: identical bytes.</summary>
    internal static readonly string SequentialExpected = Svg(
        "<g id=\"j_1\" transform=\"matrix(1,0,0,1,0,0)\"><g id=\"v-1\"><rect id=\"v-2\" fill=\"#85bbf0\"/></g></g>"
        + "<g id=\"j_2\"><text id=\"v-3\" font-size=\"24px\">Api</text></g>");

    /// <summary>Input and expected output of every scenario that is a single document comparison.</summary>
    internal static readonly IReadOnlyDictionary<string, (string Input, string Expected)> Pairs =
        new Dictionary<string, (string Input, string Expected)>(StringComparer.Ordinal)
        {
            ["sequential"] = (SequentialInput, SequentialExpected),
            ["prefixes-independent"] = (
                Svg("<g id=\"v_7\"/><g id=\"v-9\"/><g id=\"v_3\"/><g id=\"j_5\"/>"),
                Svg("<g id=\"v_1\"/><g id=\"v-1\"/><g id=\"v_2\"/><g id=\"j_1\"/>")),
            ["references-follow"] = (
                Svg("<defs><marker id=\"v-80\"><path d=\"M0,0\"/></marker></defs><path id=\"v-81\" marker-end=\"url(#v-80)\"/><use id=\"j_9\" xlink:href=\"#v-81\" href=\"#v-81\"/>"),
                Svg("<defs><marker id=\"v-1\"><path d=\"M0,0\"/></marker></defs><path id=\"v-2\" marker-end=\"url(#v-1)\"/><use id=\"j_1\" xlink:href=\"#v-2\" href=\"#v-2\"/>")),
            ["definition-order"] = (
                Svg("<use href=\"#v-90\"/><g id=\"v-5\"/><g id=\"v-90\"/>"),
                Svg("<use href=\"#v-2\"/><g id=\"v-1\"/><g id=\"v-2\"/>")),
            ["text-untouched"] = (
                Svg("<g id=\"j_40\"><text>Release v-12 and j_3</text><title>v_9</title></g><g class=\"j_40 v-12\" data-ref=\"v-12\"/>"),
                Svg("<g id=\"j_1\"><text>Release v-12 and j_3</text><title>v_9</title></g><g class=\"j_40 v-12\" data-ref=\"v-12\"/>")),
            ["undefined-reference"] = (
                Svg("<g id=\"j_5\"/><use href=\"#j_99\"/><path marker-end=\"url(#v-7)\"/>"),
                Svg("<g id=\"j_1\"/><use href=\"#j_99\"/><path marker-end=\"url(#v-7)\"/>")),
            ["swap"] = (
                Svg("<g id=\"j_2\"/><g id=\"j_1\"/><use href=\"#j_1\"/>"),
                Svg("<g id=\"j_1\"/><g id=\"j_2\"/><use href=\"#j_2\"/>")),
            ["other-ids-untouched"] = (
                Svg("<marker id=\"arrowhead\"/><g data-id=\"j_9\" id=\"j_3\"/><g id=\"v-12a\"/><g id=\"xj_4\"/><g id=\"J_5\"/>"),
                Svg("<marker id=\"arrowhead\"/><g data-id=\"j_9\" id=\"j_1\"/><g id=\"v-12a\"/><g id=\"xj_4\"/><g id=\"J_5\"/>")),
            ["no-ids"] = (Svg("<rect x=\"1\" y=\"2\"/>"), Svg("<rect x=\"1\" y=\"2\"/>")),
        };

    /// <summary>A document that is not an SVG.</summary>
    internal const string NotSvg = "<html id=\"j_1\"></html>";

    /// <summary>Wraps a body in the root element the Structurizr export writes.</summary>
    internal static string Svg(string body) =>
        "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\" viewBox=\"0 0 1180 638\" style=\"background: #ffffff\">"
        + body
        + "</svg>";

    /// <summary>Runs one scenario against the renumberer and asserts its contract.</summary>
    internal static void AssertScenario(string scenario, ISvgIdRenumberer renumberer)
    {
        switch (scenario)
        {
            case "offset-invariant":
                var before = renumberer.Renumber(SequentialInput);
                var after = renumberer.Renumber(SequentialOffsetInput);
                Assert.Equal(before, after);
                Assert.Equal(SequentialExpected, after);
                break;
            case "idempotent":
                Assert.Equal(SequentialExpected, renumberer.Renumber(SequentialExpected));
                break;
            case "not-svg":
                Assert.Throws<ArgumentException>(() => renumberer.Renumber(NotSvg));
                break;
            default:
                if (!Pairs.TryGetValue(scenario, out var pair))
                {
                    throw new InvalidOperationException("Unknown scenario " + scenario);
                }

                Assert.Equal(pair.Expected, renumberer.Renumber(pair.Input));
                break;
        }
    }
}
