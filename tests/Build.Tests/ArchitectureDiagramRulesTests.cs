namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-017 (AC-1 to AC-5): Binds the <see cref="ArchitectureDiagramRulesContract"/> assertions, unchanged
/// from the BDPv4 mock gate, to the production <see cref="ArchitectureDiagramRules"/>.
/// </summary>
public sealed class ArchitectureDiagramRulesTests
{
    private readonly ArchitectureDiagramRules rules = new();

    /// <summary>TR-MCP-NUKE-017 AC-3: Mermaid fences in markdown and .mmd files are found; receipts and non-markdown files are not.</summary>
    [Fact]
    public void FindMermaid_MeetsContract() => ArchitectureDiagramRulesContract.AssertMermaid(rules);

    /// <summary>TR-MCP-NUKE-017 AC-2 and AC-4: dangling links, missing renders and orphan renders are found.</summary>
    [Fact]
    public void FindLinkProblems_MeetsContract() => ArchitectureDiagramRulesContract.AssertLinks(rules);

    /// <summary>TR-MCP-NUKE-017 AC-5: the letter-page and 8 to 16 pt standard is enforced on rendered SVG.</summary>
    [Fact]
    public void FindPageProblems_MeetsContract() => ArchitectureDiagramRulesContract.AssertPages(rules);

    /// <summary>TR-MCP-NUKE-017 AC-1 and AC-2: view keys and the inspection error count are read from workspace JSON.</summary>
    [Fact]
    public void WorkspaceReaders_MeetContract() => ArchitectureDiagramRulesContract.AssertWorkspaceReaders(rules);
}
