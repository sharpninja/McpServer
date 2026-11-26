using System.Text.Json;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

public sealed partial class WikiExportEnhancementsContractTests
{
    /// <summary>Checks the default attribution in configured and legacy exports without a custom template.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiFooterCoverage.</remarks>
    [Fact]
    public void ProductionDefaultFooterCreditsQBrainAndLinksToItsRepository()
    {
        var legacy = RequirementsWikiDocumentRenderer.RenderWikiFiles([], [], [], [],
            new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero));
        CheckDefaultFooter(Assert.Single(legacy, static file => file.RelativePath == "github/_Footer.md").Content);
        var configured = RenderFixture([], [], [], enabled: false, footer: null);
        CheckDefaultFooter(configured["github/_Footer.md"]);
    }

    /// <summary>Runs the mock-validated group assertions against the shared renderer for both platform variants.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiGroupDetection, ac-WikiGroupNavigation,
    /// ac-WikiGroupInventory, ac-WikiGroupRoundTrip, and ac-WikiFooterCoverage.</remarks>
    [Fact]
    public void ProductionRendererPreservesGroupLinksMembersAndCriteria()
    {
        FrEntry[] functional = [
            new(Second, "Second", "Second body"),
            new(First, "First", "First body", AcceptanceCriteria: [new() { Text = MemberEvidence, IsSatisfied = true }]),
            new(Unrelated, "Singleton", "Keep on parent"),
            new("FR-MCP--WIKI-003", "Invalid", "Keep on parent"),
        ];
        TrEntry[] technical = [new("TR-MCP-RULE-002", "Second", "Second body"), new("TR-MCP-RULE-001", "First", "First body")];
        TestEntry[] testing = [new("TEST-MCP-RULE-002", "Second"), new("test-mcp-rule-001", "First")];
        var files = RenderFixture(functional, technical, testing, enabled: true);
        CheckGroup(files, "github/");
        CheckGroup(files, "azure/");
        Assert.Equal(Footer, files["github/_Footer.md"].Trim());
        Assert.Contains(InlineFooter.Trim(), files["azure/" + Child + ".md"], StringComparison.Ordinal);
        Assert.Contains("  " + ParentLink, files["github/_Sidebar.md"], StringComparison.Ordinal);
        Assert.Contains(Child + "\n", files["azure/.order"], StringComparison.Ordinal);
        Assert.Contains("TR-MCP-RULE-001", files["github/Technical-Requirements--TR-MCP-RULE.md"], StringComparison.Ordinal);
        Assert.Contains("test-mcp-rule-001", files["github/Testing-Requirements--TEST-MCP-RULE.md"], StringComparison.Ordinal);
        Assert.DoesNotContain("FR-MCP--WIKI-003", files["github/" + Child + ".md"], StringComparison.Ordinal);
        var reversed = RenderFixture([.. functional.Reverse()], [.. technical.Reverse()], [.. testing.Reverse()], enabled: true);
        Assert.Equal(files["github/" + Child + ".md"], reversed["github/" + Child + ".md"]);
        Assert.DoesNotContain("github/" + Child + ".md", RenderFixture(functional, technical, testing, enabled: false).Keys);
    }

    /// <summary>Runs canonical wiki selection with derived pages carrying newer dates and footer-lookalike records.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiFooterRoundTrip and ac-WikiGroupRoundTrip.</remarks>
    [Fact]
    public void ProductionImportDoesNotUseDerivedPagesAsCanonicalDataOrTimestamp()
    {
        var input = new FrEntry[] { new(First, "First", "Stored first"), new(Second, "Second", "Stored second") };
        const string fakeRequirementFooter = "## FR-MCP-FAKE-001 Bogus\n\nFooter body.\n- [x] Footer checklist";
        var rendered = RenderFixture(input, [], [], enabled: true, footer: fakeRequirementFooter);
        var stamp = new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);
        var documents = rendered.ToDictionary(static pair => pair.Key, pair =>
            new RequirementsIngestDocument
            {
                Content = pair.Value,
                LastModifiedUtc = string.Equals(pair.Key, "github/" + Child + ".md", StringComparison.Ordinal)
                    ? stamp.AddDays(1)
                    : stamp,
            }, StringComparer.Ordinal);
        var selected = RequirementsWikiDocumentSelector.Select(documents, "azure");
        Assert.Equal("azure", selected.Platform);
        Assert.Equal(stamp, selected.LatestFileModifiedUtc);
        var expected = RequirementsDocumentParser.ParseFunctional(RequirementsDocumentRenderer.RenderFunctional(input));
        var actual = RequirementsDocumentParser.ParseFunctional(selected.FunctionalMarkdown);
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    }

    /// <summary>Runs the mock-validated nested-route expectations against the shared renderer.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiGroupNavigation.</remarks>
    [Fact]
    public void ProductionNestedGroupNavigationUsesTheCorrectLinkContext()
    {
        var artifacts = RenderFixture([new(First, "First", "Body"), new(Second, "Second", "Body")], [], [],
            enabled: true, parentTarget: "Reference/FR-Catalog.md");
        CheckNestedLinks(artifacts["github/Reference/FR-Catalog.md"],
            artifacts["github/Reference/FR-Catalog--FR-MCP-WIKI.md"], artifacts["github/_Sidebar.md"]);
    }

    private static Dictionary<string, string> RenderFixture(
        FrEntry[] functional, TrEntry[] technical, TestEntry[] testing, bool enabled, string? footer = Footer, string? parentTarget = null)
    {
        string[] ids = ["home", "functional", "technical", "testing"];
        string[] titles = ["Home", "Functional Requirements", "Technical Requirements", "Testing Requirements"];
        string[] targets = ["Home.md", parentTarget ?? Parent + ".md", "Technical-Requirements.md", "Testing-Requirements.md"];
        var documents = ids.Select((id, index) => new RequirementsWikiExportDocument
        {
            Id = id,
            Title = titles[index],
            Source = "generated:" + id,
            Target = targets[index],
            Platforms = new HashSet<string>(["azure", "github"], StringComparer.Ordinal),
        }).ToArray();
        var config = new RequirementsWikiExportConfig
        {
            ConfigPath = "docs/wiki.yaml",
            WorkspaceRoot = ".",
            Documents = documents,
            DocumentsById = documents.ToDictionary(static document => document.Id, StringComparer.Ordinal),
            Navigation = ids.Select(static id => new RequirementsWikiExportNavigationItem { Document = id }).ToArray(),
            FooterContent = footer,
            RequirementGroupsEnabled = enabled,
        };

        return RequirementsWikiDocumentRenderer.RenderWikiFiles(functional, technical, testing, [],
            new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero), config: config)
            .ToDictionary(static file => file.RelativePath, static file => file.Content, StringComparer.Ordinal);
    }
}
