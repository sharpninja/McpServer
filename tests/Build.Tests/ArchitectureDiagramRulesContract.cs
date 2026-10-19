namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 (AC-1 to AC-5): Contract fixtures and assertions for <see cref="IArchitectureDiagramRules"/>.
/// Expected results come from the acceptance criteria, never from an implementation. The same assertions run
/// against the scripted mock (BDPv4 mock gate), against deliberately wrong mocks (negative controls), and
/// against the production rules.
/// </summary>
internal static class ArchitectureDiagramRulesContract
{
    /// <summary>AC-3 fixture: clean, fenced, tilde-fenced, indented, .mmd, receipts and non-markdown files.</summary>
    internal static readonly TrackedTextFile[] MermaidFixture =
    [
        new("docs/clean.md", "# Clean\n\nThe word mermaid in prose.\n\n```text\nmermaid inside a text fence\n```\n"),
        new("docs/fenced.md", "# Doc\n\nIntro\n\n```mermaid\nflowchart TD\n  A-->B\n```\n"),
        new("docs/tilde.md", "~~~ Mermaid\nsequenceDiagram\n~~~\n"),
        new("README.markdown", "  ```mermaid\nflowchart LR\n  ```\n"),
        new("docs/diagrams/legacy.mmd", null),
        new("docs/receipts/hv/old.md", "```mermaid\nflowchart TD\n```\n"),
        new("docs/receipts/hv/old.mmd", null),
        new("tests/Build.Tests/Fixture.cs", null),
    ];

    /// <summary>AC-3 expected findings for <see cref="MermaidFixture"/>.</summary>
    internal static readonly (string Code, string Location)[] MermaidExpected =
    [
        (ArchitectureDiagramFindingCodes.MermaidFence, "docs/fenced.md:5"),
        (ArchitectureDiagramFindingCodes.MermaidFence, "docs/tilde.md:1"),
        (ArchitectureDiagramFindingCodes.MermaidFence, "README.markdown:1"),
        (ArchitectureDiagramFindingCodes.MermaidFile, "docs/diagrams/legacy.mmd"),
    ];

    /// <summary>AC-4 fixture: workspace view keys.</summary>
    internal static readonly string[] LinkViewKeys = ["SystemContext", "AD-TXN-001"];

    /// <summary>AC-4 fixture: documents linking a defined view, an undefined view, a foreign image, and a view from another folder.</summary>
    internal static readonly TrackedTextFile[] LinkDocuments =
    [
        new(
            "docs/architecture/overview.md",
            "![Context](c4/rendered/SystemContext.svg)\nView: SystemContext\n![Ghost](c4/rendered/Ghost.svg \"missing view\")\n![Logo](../img/logo.png)\n"),
        new("docs/Project/plan.md", "Text\n\n![AD-TXN-001](../architecture/c4/rendered/AD-TXN-001.svg)\nView: AD-TXN-001\n"),
    ];

    /// <summary>AC-4 fixture: rendered files on disk (one view rendered, one stale file).</summary>
    internal static readonly string[] LinkRenderedPaths =
    [
        "docs/architecture/c4/rendered/SystemContext.svg",
        "docs/architecture/c4/rendered/Stale.svg",
    ];

    /// <summary>AC-2 and AC-4 expected findings for the link fixture.</summary>
    internal static readonly (string Code, string Location)[] LinkExpected =
    [
        (ArchitectureDiagramFindingCodes.DanglingViewLink, "docs/architecture/overview.md:3"),
        (ArchitectureDiagramFindingCodes.MissingRender, "AD-TXN-001"),
        (ArchitectureDiagramFindingCodes.OrphanRender, "docs/architecture/c4/rendered/Stale.svg"),
    ];

    /// <summary>
    /// AC-5 fixtures keyed by view key, with the expected finding codes for each. Band (amended 2026-10-19):
    /// 10.67 to 21.33 px (8 to 16 pt), spread at most 2:1; Structurizr text with the metadata line visible is
    /// 16.8, 24 and 33.6 px (exactly 2:1).
    /// </summary>
    internal static readonly (string ViewKey, string Svg, string[] ExpectedCodes)[] PageFixtures =
    [
        ("Compliant", Svg(1600, 1200, visible: [16.8, 24, 33.6]), []),
        ("PortraitFits", Svg(1250, 1600, visible: [16.8, 24, 33.6]), []),
        ("Spread", Svg(1600, 1200, visible: [10, 24, 33.6]), [ArchitectureDiagramFindingCodes.TextSizeSpread]),
        ("Overflow", Svg(4000, 3000, visible: [16.8, 24, 33.6]), [ArchitectureDiagramFindingCodes.PageOverflow]),
        ("NoViewBox", "<svg xmlns=\"http://www.w3.org/2000/svg\"><text font-size=\"24px\"><tspan>x</tspan></text></svg>", [ArchitectureDiagramFindingCodes.InvalidSvg]),
        ("NotXml", "not an svg", [ArchitectureDiagramFindingCodes.InvalidSvg]),
    ];

    /// <summary>AC-1 and AC-2 fixture: an exported workspace JSON with every view kind and an inspection count.</summary>
    internal const string WorkspaceJson = """
        {
          "name": "fixture",
          "model": {},
          "views": {
            "systemContextViews": [ { "key": "SystemContext" } ],
            "containerViews": [ { "key": "Containers" } ],
            "componentViews": [ { "key": "ServerComponents" } ],
            "dynamicViews": [ { "key": "SubmitSequence" } ],
            "deploymentViews": [ { "key": "DeploymentDocker" } ],
            "customViews": [ { "key": "AD-TXN-001" } ],
            "systemLandscapeViews": [ { "key": "Landscape" } ],
            "imageViews": [ { "key": "Image" } ],
            "filteredViews": [ { "key": "Filtered", "baseViewKey": "SystemContext" } ],
            "configuration": { "properties": { "structurizr.metadata": "false" } }
          },
          "properties": { "structurizr.inspection.error": "18", "structurizr.inspection.warning": "0" }
        }
        """;

    /// <summary>AC-2 expected view keys of <see cref="WorkspaceJson"/>, in document order.</summary>
    internal static readonly string[] WorkspaceViewKeysExpected =
        ["SystemContext", "Containers", "ServerComponents", "SubmitSequence", "DeploymentDocker", "AD-TXN-001", "Landscape", "Image", "Filtered"];

    /// <summary>AC-1 fixture without inspection properties.</summary>
    internal const string WorkspaceJsonWithoutInspection = """{ "name": "clean", "views": { "customViews": [ { "key": "Only" } ] } }""";

    /// <summary>Builds a fixture SVG shaped like the Structurizr export: a hidden title, empty label stubs, and visible text.</summary>
    internal static string Svg(int width, int height, double[] visible)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"0 0 ")
            .Append(width).Append(' ').Append(height).Append("\" style=\"background: #ffffff\">");
        builder.Append("<g><text font-size=\"36px\"><tspan display=\"none\">Hidden title</tspan></text></g>");
        builder.Append("<g><text font-size=\"14\"></text></g>");
        for (var index = 0; index < visible.Length; index++)
        {
            builder.Append("<g><text font-size=\"")
                .Append(visible[index].ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append("px\"><tspan>Text ").Append(index).Append("</tspan></text></g>");
        }

        builder.Append("</svg>");
        return builder.ToString();
    }

    /// <summary>Asserts AC-3 findings for <see cref="MermaidFixture"/>.</summary>
    internal static void AssertMermaid(IArchitectureDiagramRules rules)
        => AssertFindings(MermaidExpected, rules.FindMermaid(MermaidFixture));

    /// <summary>Asserts AC-2 and AC-4 findings for the link fixture.</summary>
    internal static void AssertLinks(IArchitectureDiagramRules rules)
        => AssertFindings(LinkExpected, rules.FindLinkProblems(LinkViewKeys, LinkDocuments, LinkRenderedPaths));

    /// <summary>Asserts AC-5 findings for every page fixture; each finding is located at its view key.</summary>
    internal static void AssertPages(IArchitectureDiagramRules rules)
    {
        foreach (var (viewKey, svg, expectedCodes) in PageFixtures)
        {
            var expected = expectedCodes.Select(code => (code, viewKey)).ToArray();
            AssertFindings(expected, rules.FindPageProblems(viewKey, svg));
        }
    }

    /// <summary>Asserts the AC-2 view-key reader and the AC-1 inspection reader.</summary>
    internal static void AssertWorkspaceReaders(IArchitectureDiagramRules rules)
    {
        Assert.Equal(WorkspaceViewKeysExpected, rules.ReadViewKeys(WorkspaceJson));
        Assert.Equal(18, rules.ReadInspectionErrorCount(WorkspaceJson));
        Assert.Equal(0, rules.ReadInspectionErrorCount(WorkspaceJsonWithoutInspection));
        Assert.Equal(new[] { "Only" }, rules.ReadViewKeys(WorkspaceJsonWithoutInspection));
    }

    /// <summary>Exact, order-insensitive comparison of (code, location) pairs; every message must carry evidence.</summary>
    internal static void AssertFindings(
        IReadOnlyCollection<(string Code, string Location)> expected,
        IReadOnlyList<ArchitectureDiagramFinding> actual)
    {
        var expectedSet = expected.Select(item => item.Code + " @ " + item.Location).OrderBy(item => item, StringComparer.Ordinal).ToArray();
        var actualSet = actual.Select(item => item.Code + " @ " + item.Location).OrderBy(item => item, StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedSet, actualSet);
        Assert.All(actual, finding => Assert.False(string.IsNullOrWhiteSpace(finding.Message), "Finding message is empty: " + finding.Code));
    }
}
