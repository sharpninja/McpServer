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
