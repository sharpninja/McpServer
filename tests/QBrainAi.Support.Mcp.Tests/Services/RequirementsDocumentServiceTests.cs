using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Unit tests for <see cref="RequirementsDocumentService"/> document parsing, rendering, and persistence behavior.</summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; TEST-MCP-106; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 6 Category=Integration cases and the helpers only they use (SeedGroupedTestingDocs) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_tempRoot, .ctor, Dispose, CreateService and SeedCanonicalDocs) moved once into the abstract base class RequirementsDocumentServiceTestsBase in Services/RequirementsDocumentServiceTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class RequirementsDocumentServiceTests : RequirementsDocumentServiceTestsBase, IDisposable
{
    /// <summary>
    /// FR-MCP-040; FR-MCP-041; TR-MCP-REQ-002: the service parses the seeded canonical documents into 2 FR, 3 TR and 2
    /// TEST entries and one mapping row (one FR to two TRs and one TEST), and renders the Functional, Technical and
    /// Matrix documents as text/markdown with the expected headings and Tracked matrix rows. Data: the canonical seed
    /// documents written by SeedCanonicalDocs (RequirementsDocumentServiceTestsBase) in a per-test temporary folder.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-040; FR-MCP-041; TR-MCP-REQ-002: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task Constructor_ParsesCanonicalDocs_AndGeneratesMarkdown()
    {
        SeedCanonicalDocs();
        var service = CreateService();

        var fr = await service.GetAllFrAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var tr = await service.GetAllTrAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var test = await service.GetAllTestAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var mapping = await service.GetAllMappingsAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, fr.Count);
        Assert.Equal("FR-MCP-001", fr[0].Id);
        Assert.Equal("Configurable workspace root and paths", fr[0].Title);

        Assert.Equal(3, tr.Count);
        Assert.Equal("Workspace Controller", tr[2].Title);
        Assert.Contains("REST API", tr[2].Body);

        Assert.Equal(2, test.Count);
        Assert.Single(mapping);
        Assert.Equal("FR-MCP-001", mapping[0].FrId);
        Assert.Equal(2, mapping[0].TrIds.Count);
        Assert.Single(mapping[0].TestIds);
        Assert.Equal("TEST-MCP-001", mapping[0].TestIds[0]);

        var functionalDoc = await service.GenerateDocumentAsync(RequirementsDocType.Functional, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("text/markdown", functionalDoc.MimeType);
        Assert.Contains("# Functional Requirements (QBrain.AI)", functionalDoc.Content);
        Assert.Contains("## FR-MCP-001 Configurable workspace root and paths", functionalDoc.Content);

        var technicalDoc = await service.GenerateDocumentAsync(RequirementsDocType.Technical, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("## TR-MCP-WS-004", technicalDoc.Content);
        Assert.Contains("**Workspace Controller** — REST API", technicalDoc.Content);

        var matrixDoc = await service.GenerateDocumentAsync(RequirementsDocType.Matrix, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("# Requirements Matrix (QBrain.AI)", matrixDoc.Content);
        Assert.Contains("| FR-MCP-001 | Tracked | Functional-Requirements.md |", matrixDoc.Content);
        Assert.Contains("| TR-MCP-CFG-001 | Tracked | Technical-Requirements.md |", matrixDoc.Content);
        Assert.Contains("| TEST-MCP-001 | Tracked | Testing-Requirements.md |", matrixDoc.Content);
    }

    /// <summary>
    /// FR-MCP-040; TR-MCP-REQ-002: 20 concurrent AddTestAsync calls leave a Testing-Requirements.md that parses to
    /// exactly 20 entries, with no leftover .tmp file in its folder. Data: the empty seed documents written by
    /// SeedEmptyDocs in a per-test temporary folder; 20 generated TEST entries.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-040; TR-MCP-REQ-002: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task ConcurrentMutations_ProduceValidTestingDocument()
    {
        SeedEmptyDocs();
        var service = CreateService();

        var tasks = Enumerable.Range(1, 20)
            .Select(i => service.AddTestAsync(new TestEntry($"TEST-MCP-{100 + i:000}", $"Condition {i}")))
            .ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(true);

        var testingPath = Path.Combine(_tempRoot, "docs", "Project", "Testing-Requirements.md");
        var content = await File.ReadAllTextAsync(testingPath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var parsed = RequirementsDocumentParser.ParseTesting(content);

        Assert.Equal(20, parsed.Count);
        Assert.DoesNotContain(Directory.GetFiles(Path.GetDirectoryName(testingPath)!, "*.tmp"), _ => true);
    }

    /// <summary>
    /// FR-MCP-040; TR-MCP-REQ-002: deleting a TR removes it from the mapping row that named it, so the row keeps only
    /// its other TR. Data: the canonical seed documents written by SeedCanonicalDocs in a per-test temporary folder
    /// (one mapping row naming two TRs).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-040; TR-MCP-REQ-002: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task DeleteTrAsync_RemovesTrFromMappingRows()
    {
        SeedCanonicalDocs();
        var service = CreateService();

        await service.DeleteTrAsync("TR-MCP-CFG-002", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var mapping = await service.GetMappingAsync("FR-MCP-001", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(mapping);
        Assert.Single(mapping!.TrIds);
        Assert.Equal("TR-MCP-CFG-001", mapping.TrIds[0]);
    }

    /// <summary>
    /// FR-MCP-040; TR-MCP-REQ-002: the mapping parser accepts a legacy two-column table (FR and Primary TRs, no Tests
    /// column) and returns one row with its two TRs and no tests. Data: an inline mapping document; no service and no
    /// files.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-040; TR-MCP-REQ-002: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ParseMapping_AcceptsLegacyTwoColumnRows()
    {
        var parsed = RequirementsDocumentParser.ParseMapping("""
            # TR per FR Mapping (QBrain.AI)

            | FR | Primary TRs |
            | --- | --- |
            | FR-MCP-001 | TR-MCP-CFG-001, TR-MCP-CFG-002 |
            """);

        var mapping = Assert.Single(parsed);
        Assert.Equal("FR-MCP-001", mapping.FrId);
        Assert.Equal(2, mapping.TrIds.Count);
        Assert.Empty(mapping.TestIds);
    }

    /// <summary>
    /// FR-MCP-040; TR-MCP-REQ-002; FR-MCP-REQAC-001; TR-MCP-REQAC-002: the technical parser takes the first generated
    /// Status line as the status (completed), strips every Status line from the body, keeps the Covered by line, and
    /// reads the one checked acceptance criterion as satisfied. Data: an inline technical document with one TR entry,
    /// two Status lines, a Covered by line and one checked criterion; no service and no files.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-040; TR-MCP-REQ-002; FR-MCP-REQAC-001; TR-MCP-REQAC-002: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ParseTechnical_StripsGeneratedStatusMetadataFromBody()
    {
        var parsed = RequirementsDocumentParser.ParseTechnical("""
            # Technical Requirements (QBrain.AI)

            ## TR-MCP-QUAD-001

            **Brain-slot storage** — Persist brain-slot definitions.
            Scope: layer-1+
            **Acceptance Criteria:**
            - [x] Existing criterion

            **Status:** completed
            **Status:** pending
            **Covered by:** FR: FR-MCP-123; TEST: TEST-MCP-163
            """);

        var entry = Assert.Single(parsed);
        Assert.Equal("TR-MCP-QUAD-001", entry.Id);
        Assert.Equal("completed", entry.Status);
        Assert.DoesNotContain("**Status:**", entry.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("**Covered by:** FR: FR-MCP-123", entry.Body, StringComparison.Ordinal);
        var criterion = Assert.Single(entry.AcceptanceCriteria!);
        Assert.Equal("Existing criterion", criterion.Text);
        Assert.True(criterion.IsSatisfied);
    }

    /// <summary>
    /// FR-MCP-084; TR-MCP-REQ-005: the testing parser accepts the wiki-grouped layout (one table under each TEST group
    /// heading) and returns both rows, unescaping an escaped pipe character in a condition. Data: an inline testing
    /// document with two group sections; no service and no files.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-084; TR-MCP-REQ-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ParseTesting_AcceptsWikiGroupedTableRows()
    {
        var parsed = RequirementsDocumentParser.ParseTesting("""
            # Testing Requirements (QBrain.AI)

            ## TEST-MCP

            | ID | Requirement |
            | --- | --- |
            | TEST-MCP-001 | Given A \| B, when C, then D. |

            ## TEST-MCP-REPL

            | ID | Requirement |
            | --- | --- |
            | TEST-MCP-REPL-021 | Given marker diagnostics, when no marker exists, then searched paths are listed. |
            """);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("TEST-MCP-001", parsed[0].Id);
        Assert.Equal("Given A | B, when C, then D.", parsed[0].Condition);
        Assert.Equal("TEST-MCP-REPL-021", parsed[1].Id);
    }

    /// <summary>
    /// FR-MCP-084; TR-MCP-REQ-005; FR-MCP-112; FR-MCP-REQAC-001; TR-MCP-REQAC-002: the testing parser reads back the
    /// wiki section layout (a TEST heading, its description paragraph and an Acceptance Criteria checklist) as one
    /// entry with its condition and two criteria, the first unchecked and the second checked with its evidence text
    /// (parser-test). Data: an inline testing document with one grouped TEST section; no service and no files.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-084; TR-MCP-REQ-005; FR-MCP-112; FR-MCP-REQAC-001; TR-MCP-REQAC-002: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void ParseTesting_AcceptsWikiGroupedSectionsWithAcceptanceCriteria()
    {
        var parsed = RequirementsDocumentParser.ParseTesting("""
            # Testing Requirements (QBrain.AI)

            ## TEST-MCP

            ### TEST-MCP-AC-001

            Given exported requirements, when the wiki is generated, then descriptions are readable.

            **Acceptance Criteria:**
            - [ ] First criterion
            - [x] Second criterion (evidence: parser-test)
            """);

        var entry = Assert.Single(parsed);
        Assert.Equal("TEST-MCP-AC-001", entry.Id);
        Assert.Equal("Given exported requirements, when the wiki is generated, then descriptions are readable.", entry.Condition);
        Assert.Equal(2, entry.AcceptanceCriteria?.Count);
        Assert.False(entry.AcceptanceCriteria![0].IsSatisfied);
        Assert.Equal("First criterion", entry.AcceptanceCriteria[0].Text);
        Assert.True(entry.AcceptanceCriteria[1].IsSatisfied);
        Assert.Equal("Second criterion", entry.AcceptanceCriteria[1].Text);
        Assert.Equal("parser-test", entry.AcceptanceCriteria[1].Evidence);
    }

    private void SeedEmptyDocs()
    {
        var projectDir = Path.Combine(_tempRoot, "docs", "Project");
        File.WriteAllText(Path.Combine(projectDir, "Functional-Requirements.md"), "# Functional Requirements (QBrain.AI)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "Technical-Requirements.md"), "# Technical Requirements (QBrain.AI)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "Testing-Requirements.md"), "# Testing Requirements (QBrain.AI)\n\n");
        File.WriteAllText(Path.Combine(projectDir, "TR-per-FR-Mapping.md"), "# TR per FR Mapping (QBrain.AI)\n\n| FR | Primary TRs | Tests |\n| --- | --- | --- |\n");
    }
}
