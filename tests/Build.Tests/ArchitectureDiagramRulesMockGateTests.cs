namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 (AC-1 to AC-5), BDPv4 mock gate: proves the <see cref="ArchitectureDiagramRulesContract"/>
/// assertions accept the contract-derived expected outputs from a scripted mock and reject deliberately wrong
/// outputs (negative controls), before any production rule code is written.
/// </summary>
public sealed class ArchitectureDiagramRulesMockGateTests
{
    /// <summary>TR-MCP-NUKE-017 AC-3: the scripted mock passes the Mermaid assertions.</summary>
    [Fact]
    public void ScriptedRules_PassMermaidContract() => ArchitectureDiagramRulesContract.AssertMermaid(new ScriptedRules());

    /// <summary>TR-MCP-NUKE-017 AC-2 and AC-4: the scripted mock passes the link assertions.</summary>
    [Fact]
    public void ScriptedRules_PassLinkContract() => ArchitectureDiagramRulesContract.AssertLinks(new ScriptedRules());

    /// <summary>TR-MCP-NUKE-017 AC-5: the scripted mock passes the page assertions.</summary>
    [Fact]
    public void ScriptedRules_PassPageContract() => ArchitectureDiagramRulesContract.AssertPages(new ScriptedRules());

    /// <summary>TR-MCP-NUKE-017 AC-1 and AC-2: the scripted mock passes the workspace reader assertions.</summary>
    [Fact]
    public void ScriptedRules_PassWorkspaceReaderContract() => ArchitectureDiagramRulesContract.AssertWorkspaceReaders(new ScriptedRules());

    /// <summary>
    /// TR-MCP-NUKE-017 AC-3 negative controls: a mock that misses a fence, reports a receipts file, reports a
    /// wrong line, or reports a finding without evidence is rejected.
    /// </summary>
    [Fact]
    public void WrongMermaidOutputs_AreRejected()
    {
        var expected = Findings(ArchitectureDiagramRulesContract.MermaidExpected);
        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertMermaid(rules), new ScriptedRules { Mermaid = expected.Skip(1).ToArray() });
        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertMermaid(rules), new ScriptedRules { Mermaid = [.. expected, new(ArchitectureDiagramFindingCodes.MermaidFile, "docs/receipts/hv/old.mmd", "receipt")] });
        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertMermaid(rules), new ScriptedRules { Mermaid = [.. expected.Skip(1), new(ArchitectureDiagramFindingCodes.MermaidFence, "docs/fenced.md:6", "off by one")] });
        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertMermaid(rules), new ScriptedRules { Mermaid = [.. expected.Skip(1), new(expected[0].Code, expected[0].Location, " ")] });
    }

    /// <summary>
    /// TR-MCP-NUKE-017 AC-2 and AC-4 negative controls: a mock that misses the dangling link, the missing render,
    /// or the orphan, or reports a foreign image link, is rejected.
    /// </summary>
    [Fact]
    public void WrongLinkOutputs_AreRejected()
    {
        var expected = Findings(ArchitectureDiagramRulesContract.LinkExpected);
        foreach (var index in Enumerable.Range(0, expected.Length))
        {
            AssertRejected(rules => ArchitectureDiagramRulesContract.AssertLinks(rules), new ScriptedRules { Links = expected.Where((_, i) => i != index).ToArray() });
        }

        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertLinks(rules), new ScriptedRules { Links = [.. expected, new(ArchitectureDiagramFindingCodes.DanglingViewLink, "docs/architecture/overview.md:4", "logo")] });
    }

    /// <summary>
    /// TR-MCP-NUKE-017 AC-5 negative controls: a mock that passes the spread, overflow or invalid fixtures, or
    /// flags a compliant or portrait-fitting render, is rejected.
    /// </summary>
    [Fact]
    public void WrongPageOutputs_AreRejected()
    {
        foreach (var (viewKey, _, expectedCodes) in ArchitectureDiagramRulesContract.PageFixtures)
        {
            var wrong = expectedCodes.Length == 0
                ? new[] { new ArchitectureDiagramFinding(ArchitectureDiagramFindingCodes.PageOverflow, viewKey, "false alarm") }
                : [];
            AssertRejected(rules => ArchitectureDiagramRulesContract.AssertPages(rules), new ScriptedRules { PageOverride = (viewKey, wrong) });
        }

        AssertRejected(
            rules => ArchitectureDiagramRulesContract.AssertPages(rules),
            new ScriptedRules { PageOverride = ("Spread", [new(ArchitectureDiagramFindingCodes.PageOverflow, "Spread", "wrong code")]) });
    }

    /// <summary>
    /// TR-MCP-NUKE-017 AC-1 and AC-2 negative controls: a mock that drops or reorders view keys, or misreads the
    /// inspection count, is rejected.
    /// </summary>
    [Fact]
    public void WrongWorkspaceReaderOutputs_AreRejected()
    {
        var keys = ArchitectureDiagramRulesContract.WorkspaceViewKeysExpected;
        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertWorkspaceReaders(rules), new ScriptedRules { ViewKeys = keys.Skip(1).ToArray() });
        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertWorkspaceReaders(rules), new ScriptedRules { ViewKeys = Enumerable.Reverse(keys).ToArray() });
        AssertRejected(rules => ArchitectureDiagramRulesContract.AssertWorkspaceReaders(rules), new ScriptedRules { InspectionErrors = 0 });
    }

    private static ArchitectureDiagramFinding[] Findings(IEnumerable<(string Code, string Location)> items)
        => items.Select(item => new ArchitectureDiagramFinding(item.Code, item.Location, "evidence for " + item.Location)).ToArray();

    private static void AssertRejected(Action<IArchitectureDiagramRules> contract, IArchitectureDiagramRules wrong)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => contract(wrong));

    /// <summary>Scripted mock: returns the contract-derived expected outputs unless a field overrides them.</summary>
    private sealed class ScriptedRules : IArchitectureDiagramRules
    {
        public ArchitectureDiagramFinding[] Mermaid { get; init; } = Findings(ArchitectureDiagramRulesContract.MermaidExpected);

        public ArchitectureDiagramFinding[] Links { get; init; } = Findings(ArchitectureDiagramRulesContract.LinkExpected);

        public (string ViewKey, ArchitectureDiagramFinding[] Findings)? PageOverride { get; init; }

        public string[] ViewKeys { get; init; } = ArchitectureDiagramRulesContract.WorkspaceViewKeysExpected;

        public int InspectionErrors { get; init; } = 18;

        public IReadOnlyList<ArchitectureDiagramFinding> FindMermaid(IEnumerable<TrackedTextFile> files) => Mermaid;

        public IReadOnlyList<ArchitectureDiagramFinding> FindLinkProblems(
            IReadOnlyCollection<string> viewKeys,
            IEnumerable<TrackedTextFile> markdownFiles,
            IEnumerable<string> renderedRelativePaths) => Links;

        public IReadOnlyList<ArchitectureDiagramFinding> FindPageProblems(string viewKey, string svg)
        {
            if (PageOverride is { } overridden && overridden.ViewKey == viewKey)
            {
                return overridden.Findings;
            }

            var fixture = ArchitectureDiagramRulesContract.PageFixtures.Single(item => item.ViewKey == viewKey);
            return fixture.ExpectedCodes.Select(code => new ArchitectureDiagramFinding(code, viewKey, "evidence for " + viewKey)).ToArray();
        }

        public IReadOnlyList<string> ReadViewKeys(string workspaceJson)
            => workspaceJson.Contains("\"Only\"", StringComparison.Ordinal) ? ["Only"] : ViewKeys;

        public int ReadInspectionErrorCount(string workspaceJson)
            => workspaceJson.Contains("structurizr.inspection.error", StringComparison.Ordinal) ? InspectionErrors : 0;
    }
}
