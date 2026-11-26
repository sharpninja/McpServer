using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Checks the observable wiki inventory, navigation, and round-trip contract.</summary>
/// <remarks>2026-11-26, FR-MCP-WIKIEXPORT-001, TR-MCP-WIKIEXPORT-001, TEST-MCP-WIKIEXPORT-001:
/// ac-WikiFooterCoverage, ac-WikiFooterValidation, ac-WikiFooterRoundTrip, ac-WikiGroupDetection,
/// ac-WikiGroupNavigation, ac-WikiGroupInventory, ac-WikiGroupRoundTrip.
/// Establishes independent expected artifacts before the production binding.</remarks>
public sealed partial class WikiExportEnhancementsContractTests
{
    private const string Footer = "Shared footer text.";
    private const string FooterStart = "<!-- mcp-wiki-footer:begin -->";
    private const string FooterEnd = "<!-- mcp-wiki-footer:end -->";
    private const string Child = "Functional-Requirements--FR-MCP-WIKI";
    private const string Parent = "Functional-Requirements";
    private const string First = "FR-MCP-WIKI-001";
    private const string Second = "FR-MCP-WIKI-002";
    private const string Unrelated = "FR-MCP-WIKIEXPORT-001";
    private const string MemberEvidence = "Retain the original checklist evidence.";
    private const string ParentLink = "- [FR-MCP-WIKI](" + Child + ")";
    private const string BackLink = "[Back to Functional Requirements](" + Parent + ")";
    private static readonly string[] s_contentPages =
        ["Home.md", Parent + ".md", "Technical-Requirements.md", "Testing-Requirements.md",
         "TR-per-FR-Mapping.md", "Requirements-Matrix.md", "Guide.md", "API.md", Child + ".md"];

    /// <summary>Validates exact-once footer coverage and its negative controls.</summary>
    [Fact]
    public void MockFooterCoversContentPagesAndExcludesSidecars()
    {
        var files = MockInventory();
        CheckFooters(files);
        files["azure/Guide.md"] = "# Guide";
        Assert.NotNull(Record.Exception(() => CheckFooters(files)));
        files = MockInventory();
        files["azure/Home.md"] += InlineFooter;
        Assert.NotNull(Record.Exception(() => CheckFooters(files)));
        files = MockInventory();
        files["azure/.order"] += InlineFooter;
        Assert.NotNull(Record.Exception(() => CheckFooters(files)));
    }

    /// <summary>Validates configured footer rejection before any filesystem mutation.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("extension")]
    [InlineData("escape")]
    [InlineData("symlink")]
    [InlineData("delimiter")]
    public void MockInvalidConfigRejectsBeforeWriting(string reason)
    {
        CheckInvalidConfig(new InvalidOperationException("Invalid footer.template: " + reason), "before", "before");
        Assert.NotNull(Record.Exception(() => CheckInvalidConfig(null, "before", "before")));
        Assert.NotNull(Record.Exception(() => CheckInvalidConfig(new InvalidOperationException("footer.template"), "before", "after")));
    }

    /// <summary>Validates the group, the stable canonical entries, and navigation.</summary>
    [Fact]
    public void MockGroupLinksPreserveCanonicalEntriesAndExactMembership()
    {
        var files = MockInventory();
        CheckGroup(files, "github/");
        CheckGroup(files, "azure/");
        files["github/" + Child + ".md"] += "\n## " + Unrelated;
        Assert.NotNull(Record.Exception(() => CheckGroup(files, "github/")));
        files = MockInventory();
        files["github/" + Parent + ".md"] = files["github/" + Parent + ".md"].Replace(ParentLink, "", StringComparison.Ordinal);
        Assert.NotNull(Record.Exception(() => CheckGroup(files, "github/")));
        files = MockInventory();
        files["github/" + Child + ".md"] = files["github/" + Child + ".md"].Replace(MemberEvidence, "", StringComparison.Ordinal);
        Assert.NotNull(Record.Exception(() => CheckGroup(files, "github/")));
    }

    /// <summary>Validates matching artifact inventories and removal of an obsolete group.</summary>
    [Fact]
    public void MockGroupManifestZipAndCleanupAgree()
    {
        CheckInventory([Child + ".md"], [Child + ".md"], [Child + ".md"], []);
        Assert.NotNull(Record.Exception(() => CheckInventory([Child + ".md"], [], [Child + ".md"], [])));
        Assert.NotNull(Record.Exception(() => CheckInventory([Child + ".md"], [Child + ".md"], [], [])));
        Assert.NotNull(Record.Exception(() => CheckInventory([Child + ".md"], [Child + ".md"], [Child + ".md"], [Child + ".md"])));
    }

    /// <summary>Validates exact round-trip fingerprints and published Home footer placement.</summary>
    [Fact]
    public void MockRoundTripPreservesRecordsAndFooterOrdering()
    {
        string[] baseline = [First + "|" + MemberEvidence, Second + "|complete", Unrelated + "|pending"];
        var home = "# Home\n\n## User Documentation\n\n- [User Guide](Guide)\n" + InlineFooter;
        CheckRoundTrip(baseline, [.. baseline], home);
        Assert.NotNull(Record.Exception(() => CheckRoundTrip(baseline, [Second + "|complete"], home)));
        Assert.NotNull(Record.Exception(() => CheckRoundTrip(baseline, [.. baseline, "FR-MCP-FAKE-001|footer"], home)));
        Assert.NotNull(Record.Exception(() => CheckRoundTrip(baseline, [.. baseline], home + "\n## Unexpected tail")));
    }

    /// <summary>Checks that nested pages use a sibling link in content and a root-relative route in the sidebar.</summary>
    /// <remarks>2026-11-26, TEST-MCP-WIKIEXPORT-001 ac-WikiGroupNavigation.</remarks>
    [Fact]
    public void MockNestedGroupNavigationUsesTheCorrectLinkContext()
    {
        const string parent = "# Parent\n\n- [FR-MCP-WIKI](FR-Catalog--FR-MCP-WIKI)";
        const string child = "# Group\n\n[Back to Functional Requirements](FR-Catalog)";
        const string sidebar = "- [Functional Requirements](Reference/FR-Catalog)\n  - [FR-MCP-WIKI](Reference/FR-Catalog--FR-MCP-WIKI)";
        CheckNestedLinks(parent, child, sidebar);
        Assert.NotNull(Record.Exception(() => CheckNestedLinks(parent, child,
            sidebar.Replace("(Reference/FR-Catalog--FR-MCP-WIKI)", "(FR-Catalog--FR-MCP-WIKI)", StringComparison.Ordinal))));
        Assert.NotNull(Record.Exception(() => CheckNestedLinks(parent,
            child.Replace("(FR-Catalog)", "(Functional-Requirements)", StringComparison.Ordinal), sidebar)));
    }

    private static void CheckNestedLinks(string parent, string child, string sidebar)
    {
        Assert.Contains("[FR-MCP-WIKI](FR-Catalog--FR-MCP-WIKI)", parent, StringComparison.Ordinal);
        Assert.Contains("[Back to Functional Requirements](FR-Catalog)", child, StringComparison.Ordinal);
        Assert.Contains("  - [FR-MCP-WIKI](Reference/FR-Catalog--FR-MCP-WIKI)", sidebar, StringComparison.Ordinal);
    }

    private static string InlineFooter => "\n\n" + FooterStart + "\n" + Footer + "\n" + FooterEnd + "\n";

    private static Dictionary<string, string> MockInventory()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var page in s_contentPages)
        {
            files["azure/" + page] = "# Fixture" + InlineFooter;
            files["github/" + page] = "# Fixture";
        }

        foreach (var prefix in new[] { "azure/", "github/" })
        {
            var suffix = prefix == "azure/" ? InlineFooter : "";
            files[prefix + Parent + ".md"] =
                "# Functional Requirements\n\n" + ParentLink + "\n\n## " + First + "\n## " + Second + "\n## " + Unrelated + suffix;
            files[prefix + Child + ".md"] =
                "# FR-MCP-WIKI\n\n" + BackLink + "\n\n## " + First + "\n" + MemberEvidence + "\n## " + Second + suffix;
        }

        files["github/_Footer.md"] = Footer + "\n";
        files["github/_Sidebar.md"] = "- [Functional Requirements](Functional-Requirements)\n  " + ParentLink;
        files["azure/.order"] = "Home\nFunctional-Requirements\n" + Child + "\n";
        return files;
    }

    private static void CheckFooters(IReadOnlyDictionary<string, string> files)
    {
        Assert.Equal(Footer, files["github/_Footer.md"].Trim());
        foreach (var page in s_contentPages)
        {
            var azure = files["azure/" + page];
            Assert.EndsWith(InlineFooter.Trim(), azure.TrimEnd(), StringComparison.Ordinal);
            Assert.Equal(azure.IndexOf(FooterStart, StringComparison.Ordinal), azure.LastIndexOf(FooterStart, StringComparison.Ordinal));
            Assert.DoesNotContain(Footer, files["github/" + page], StringComparison.Ordinal);
        }

        Assert.DoesNotContain(Footer, files["azure/.order"], StringComparison.Ordinal);
        Assert.DoesNotContain(Footer, files["github/_Sidebar.md"], StringComparison.Ordinal);
        Assert.DoesNotContain(files.Keys, path => path.EndsWith("wiki-footer.md", StringComparison.OrdinalIgnoreCase));
    }

    private static void CheckInvalidConfig(Exception? error, string before, string after)
    {
        Assert.Contains("footer.template", Assert.IsType<InvalidOperationException>(error).Message, StringComparison.Ordinal);
        Assert.Equal(before, after);
    }

    private static void CheckGroup(IReadOnlyDictionary<string, string> files, string prefix)
    {
        var parent = files[prefix + Parent + ".md"];
        var child = files[prefix + Child + ".md"];
        Assert.Contains(ParentLink, parent, StringComparison.Ordinal);
        Assert.Contains(BackLink, child, StringComparison.Ordinal);
        Assert.Contains(First, parent, StringComparison.Ordinal);
        Assert.Contains(Second, parent, StringComparison.Ordinal);
        Assert.Contains(Unrelated, parent, StringComparison.Ordinal);
        Assert.Contains(First, child, StringComparison.Ordinal);
        Assert.Contains(Second, child, StringComparison.Ordinal);
        Assert.Contains(MemberEvidence, child, StringComparison.Ordinal);
        Assert.DoesNotContain(Unrelated, child, StringComparison.Ordinal);
    }

    private static void CheckInventory(string[] disk, string[] manifest, string[] zip, string[] afterRemoval)
    {
        Assert.Equal(new[] { Child + ".md" }, disk);
        Assert.Equal(disk, manifest);
        Assert.Equal(disk, zip);
        Assert.Empty(afterRemoval);
    }

    private static void CheckRoundTrip(string[] baseline, string[] imported, string home)
    {
        Assert.Equal(baseline, imported);
        Assert.Contains("## User Documentation", home, StringComparison.Ordinal);
        Assert.True(home.IndexOf("## User Documentation", StringComparison.Ordinal) < home.IndexOf(FooterStart, StringComparison.Ordinal));
        Assert.EndsWith(FooterEnd, home.TrimEnd(), StringComparison.Ordinal);
    }
}
