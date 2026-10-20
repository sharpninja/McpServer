namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 AC-2, BDPv4 mock gate: proves the <see cref="SvgIdRenumberContract"/> assertions accept a scripted
/// renumberer (a lookup table of the contract's hand-written documents) and reject deliberately wrong renumberers,
/// before any production renumbering code is written.
/// </summary>
public sealed class SvgIdRenumberMockGateTests
{
    /// <summary>TR-MCP-NUKE-017 AC-2: every scenario passes against the scripted renumberer.</summary>
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
    public void ScriptedRenumberer_PassesScenario(string scenario)
        => SvgIdRenumberContract.AssertScenario(scenario, new ScriptedRenumberer(Fault.None));

    /// <summary>TR-MCP-NUKE-017 AC-2: the theory above covers every contract scenario.</summary>
    [Fact]
    public void ScriptedRenumbererTheory_CoversEveryScenario()
        => Assert.Equal(12, SvgIdRenumberContract.ScenarioNames.Length);

    /// <summary>
    /// TR-MCP-NUKE-017 AC-2 negative controls: each wrong renumberer is rejected by the scenario it violates (ids left
    /// as exported, one counter shared by all prefixes, a numbering that keeps the page offset, references left
    /// behind, numbering by first occurrence instead of definition, tokens in text rewritten, ids replaced one at a
    /// time, undefined references renamed, a data-id attribute treated as a definition, a non-SVG document accepted).
    /// </summary>
    /// <param name="scenario">Scenario the fault violates.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("sequential", Fault.NoRenumber)]
    [InlineData("prefixes-independent", Fault.SharedCounter)]
    [InlineData("offset-invariant", Fault.KeepsPageOffset)]
    [InlineData("references-follow", Fault.ReferencesLeftBehind)]
    [InlineData("definition-order", Fault.FirstOccurrenceOrder)]
    [InlineData("text-untouched", Fault.RewritesText)]
    [InlineData("swap", Fault.ReplacesOneAtATime)]
    [InlineData("undefined-reference", Fault.RenamesUndefined)]
    [InlineData("other-ids-untouched", Fault.TreatsDataIdAsId)]
    [InlineData("not-svg", Fault.AcceptsNotSvg)]
    public void WrongRenumberer_IsRejected(string scenario, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SvgIdRenumberContract.AssertScenario(scenario, new ScriptedRenumberer(fault)));

    /// <summary>Faults injected into the scripted renumberer for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract's hand-written documents.</summary>
        None,

        /// <summary>Returns the document as exported.</summary>
        NoRenumber,

        /// <summary>Numbers j_, v- and v_ ids from one shared counter.</summary>
        SharedCounter,

        /// <summary>Numbers relative to the smallest id of the page instead of per document.</summary>
        KeepsPageOffset,

        /// <summary>Renames the definitions but not the url(#id) and href references.</summary>
        ReferencesLeftBehind,

        /// <summary>Numbers ids in order of first occurrence, references included.</summary>
        FirstOccurrenceOrder,

        /// <summary>Rewrites matching tokens everywhere, text and other attributes included.</summary>
        RewritesText,

        /// <summary>Applies the renames one string replacement at a time, so earlier results are renamed again.</summary>
        ReplacesOneAtATime,

        /// <summary>Renames references to ids the document does not define.</summary>
        RenamesUndefined,

        /// <summary>Treats a data-id attribute as an id definition.</summary>
        TreatsDataIdAsId,

        /// <summary>Returns a document that is not an SVG instead of rejecting it.</summary>
        AcceptsNotSvg,
    }

    /// <summary>Scripted mock: the contract's documents per input, with one optional fault giving a wrong document.</summary>
    private sealed class ScriptedRenumberer(Fault fault) : ISvgIdRenumberer
    {
        public string Renumber(string svg)
        {
            if (svg == SvgIdRenumberContract.NotSvg)
            {
                return fault == Fault.AcceptsNotSvg ? svg : throw new ArgumentException("Not an SVG.", nameof(svg));
            }

            if (fault == Fault.NoRenumber)
            {
                return svg;
            }

            var wrong = fault switch
            {
                Fault.SharedCounter when svg == Pair("prefixes-independent").Input
                    => SvgIdRenumberContract.Svg("<g id=\"v_1\"/><g id=\"v-2\"/><g id=\"v_3\"/><g id=\"j_4\"/>"),
                Fault.KeepsPageOffset when svg == SvgIdRenumberContract.SequentialOffsetInput
                    => SvgIdRenumberContract.Svg(
                        "<g id=\"j_13\" transform=\"matrix(1,0,0,1,0,0)\"><g id=\"v-152\"><rect id=\"v-158\" fill=\"#85bbf0\"/></g></g>"
                        + "<g id=\"j_19\"><text id=\"v-229\" font-size=\"24px\">Api</text></g>"),
                Fault.ReferencesLeftBehind when svg == Pair("references-follow").Input
                    => SvgIdRenumberContract.Svg("<defs><marker id=\"v-1\"><path d=\"M0,0\"/></marker></defs><path id=\"v-2\" marker-end=\"url(#v-80)\"/><use id=\"j_1\" xlink:href=\"#v-81\" href=\"#v-81\"/>"),
                Fault.FirstOccurrenceOrder when svg == Pair("definition-order").Input
                    => SvgIdRenumberContract.Svg("<use href=\"#v-1\"/><g id=\"v-2\"/><g id=\"v-1\"/>"),
                Fault.RewritesText when svg == Pair("text-untouched").Input
                    => SvgIdRenumberContract.Svg("<g id=\"j_1\"><text>Release v-1 and j_2</text><title>v_1</title></g><g class=\"j_1 v-1\" data-ref=\"v-1\"/>"),
                Fault.ReplacesOneAtATime when svg == Pair("swap").Input
                    => SvgIdRenumberContract.Svg("<g id=\"j_2\"/><g id=\"j_2\"/><use href=\"#j_2\"/>"),
                Fault.RenamesUndefined when svg == Pair("undefined-reference").Input
                    => SvgIdRenumberContract.Svg("<g id=\"j_1\"/><use href=\"#j_2\"/><path marker-end=\"url(#v-1)\"/>"),
                Fault.TreatsDataIdAsId when svg == Pair("other-ids-untouched").Input
                    => SvgIdRenumberContract.Svg("<marker id=\"arrowhead\"/><g data-id=\"j_1\" id=\"j_2\"/><g id=\"v-12a\"/><g id=\"xj_4\"/><g id=\"J_5\"/>"),
                _ => null,
            };
            if (wrong is not null)
            {
                return wrong;
            }

            if (svg == SvgIdRenumberContract.SequentialOffsetInput || svg == SvgIdRenumberContract.SequentialExpected)
            {
                return SvgIdRenumberContract.SequentialExpected;
            }

            foreach (var pair in SvgIdRenumberContract.Pairs.Values)
            {
                if (svg == pair.Input)
                {
                    return pair.Expected;
                }
            }

            throw new InvalidOperationException("Input outside the contract.");
        }

        private static (string Input, string Expected) Pair(string scenario) => SvgIdRenumberContract.Pairs[scenario];
    }
}
