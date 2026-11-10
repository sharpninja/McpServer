using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.RequirementsDatabaseDocumentServiceTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>Acceptance tests for DB-backed, workspace-scoped requirements storage.</summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/RequirementsDatabaseDocumentServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 3 Category=Integration cases moved unchanged with the class attributes; the helpers both halves use come from Services/RequirementsDatabaseDocumentServiceTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class RequirementsDatabaseDocumentServiceTests
{
    /// <summary>Overlapping requirement ids do not leak between workspaces.</summary>
    /// <remarks>Integration: creates physical workspace documents and exercises bootstrap or export against them (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ListsAndExports_AreScopedToActiveWorkspace()
    {
        using var fixture = new RequirementsDbFixture();
        var workspaceA = fixture.CreateWorkspace("a");
        var workspaceB = fixture.CreateWorkspace("b");

        var service = fixture.CreateService();
        fixture.SetWorkspace(workspaceA);
        await service.AddFrAsync(new FrEntry("FR-MCP-900", "Workspace A", "A body"), ct: TestContext.Current.CancellationToken);
        await service.AddTrAsync(new TrEntry("TR-MCP-900", "A TR", "A TR body"), ct: TestContext.Current.CancellationToken);
        await service.AddTestAsync(new TestEntry("TEST-MCP-900", "A test"), ct: TestContext.Current.CancellationToken);
        Assert.Contains(await fixture.GetRequirementRowsAsync(), x => x.Id == "FR-MCP-900");
        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-900", ["TR-MCP-900"], ["TEST-MCP-900"]), ct: TestContext.Current.CancellationToken);

        fixture.SetWorkspace(workspaceB);
        await service.AddFrAsync(new FrEntry("FR-MCP-900", "Workspace B", "B body"), ct: TestContext.Current.CancellationToken);
        await service.AddTrAsync(new TrEntry("TR-MCP-900", "B TR", "B TR body"), ct: TestContext.Current.CancellationToken);
        await service.AddTestAsync(new TestEntry("TEST-MCP-900", "B test"), ct: TestContext.Current.CancellationToken);
        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-900", ["TR-MCP-900"], ["TEST-MCP-900"]), ct: TestContext.Current.CancellationToken);

        fixture.SetWorkspace(workspaceA);
        var fr = Assert.Single(await service.GetAllFrAsync(ct: TestContext.Current.CancellationToken));
        Assert.Equal("Workspace A", fr.Title);

        var (mappingMarkdown, _) = await service.GenerateDocumentAsync(RequirementsDocType.Mapping, ct: TestContext.Current.CancellationToken);
        Assert.Contains("TEST-MCP-900", mappingMarkdown);
        Assert.DoesNotContain("Workspace B", mappingMarkdown);

        var (matrixMarkdown, _) = await service.GenerateDocumentAsync(RequirementsDocType.Matrix, ct: TestContext.Current.CancellationToken);
        Assert.Contains("| FR-MCP-900 | Tracked | Functional-Requirements.md |", matrixMarkdown);
        Assert.Contains("| TR-MCP-900 | Tracked | Technical-Requirements.md |", matrixMarkdown);
        Assert.Contains("| TEST-MCP-900 | Tracked | Testing-Requirements.md |", matrixMarkdown);

        var outputRoot = Path.Combine(workspaceA, "docs", "Project", "export");
        var export = await service.GenerateAllAsync(outputRoot, ct: TestContext.Current.CancellationToken);
        var functional = await File.ReadAllTextAsync(Path.Combine(outputRoot, "Functional-Requirements.md"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("Workspace A", functional);
        Assert.DoesNotContain("Workspace B", functional);
        Assert.Contains(export.Files, file => file.RelativePath == "Functional-Requirements.md");
        Assert.Contains(export.Files, file => file.RelativePath == "Requirements-Matrix.md");
    }

    /// <summary>Bootstrap accepts bold legacy headings and does not treat notes columns as TEST links.</summary>
    /// <remarks>Integration: creates physical workspace documents and exercises bootstrap or export against them (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task Bootstrap_LegacyBoldHeadingsAndNotesMapping_GeneratesWikiWithoutDbErrors()
    {
        using var fixture = new RequirementsDbFixture();
        var workspace = fixture.CreateWorkspace("legacy-bold");
        var project = Path.Combine(workspace, "docs", "Project");
        await File.WriteAllTextAsync(
            Path.Combine(project, "Functional-Requirements.md"),
            """
            # Functional Requirements

            ## **FR-1 — Compile-time product identity.**

            **FR-1 — Compile-time product identity.** The build system shall stamp each binary.
            """, cancellationToken: TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(project, "Technical-Requirements.md"),
            """
            # Technical Requirements

            ## **TR-1 — Target frameworks.**

            **TR-1 — Target frameworks.** SDK targets net10.0.
            """, cancellationToken: TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(project, "TR-per-FR-Mapping.md"),
            """
            # TR per FR Mapping

            | Functional Requirement | Technical Requirements | Notes |
            | --- | --- | --- |
            | FR-1 | TR-1 | Notes are prose, not TEST ids. |
            """, cancellationToken: TestContext.Current.CancellationToken);

        fixture.SetWorkspace(workspace);
        var service = fixture.CreateService();
        var export = await service.GenerateWikiAsync(Path.Combine(project, "wiki"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var rows = await fixture.GetRequirementRowsAsync().ConfigureAwait(true);

        Assert.True(export.Success);
        Assert.Contains(rows, row => row.Kind == "fr" && row.Id == "FR-1");
        Assert.Contains(rows, row => row.Kind == "tr" && row.Id == "TR-1");
        Assert.DoesNotContain(rows, row => row.Kind == "test" && row.Id.Contains("Notes", StringComparison.OrdinalIgnoreCase));

        var (mappingMarkdown, _) = await service.GenerateDocumentAsync(RequirementsDocType.Mapping, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("TR-1", mappingMarkdown);
        Assert.DoesNotContain("Notes are prose", mappingMarkdown);
    }

    /// <summary>Bootstrap rebuilds traceability when orphan links were left by a failed import.</summary>
    /// <remarks>Integration: creates physical workspace documents and exercises bootstrap or export against them (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task Bootstrap_StaleTraceabilityLinks_RebuildsWithoutUniqueConstraintFailure()
    {
        using var fixture = new RequirementsDbFixture();
        var workspace = fixture.CreateWorkspace("stale-link");
        var project = Path.Combine(workspace, "docs", "Project");
        await File.WriteAllTextAsync(
            Path.Combine(project, "Functional-Requirements.md"),
            """
            # Functional Requirements

            ## FR-1 Stale link repair

            The system shall rebuild requirements storage from checked-in documents.
            """, cancellationToken: TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(project, "Technical-Requirements.md"),
            """
            # Technical Requirements

            ## TR-1

            The importer shall tolerate existing orphan links.
            """, cancellationToken: TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(project, "TR-per-FR-Mapping.md"),
            """
            # TR per FR Mapping

            | Functional Requirement | Technical Requirements |
            | --- | --- |
            | FR-1 | TR-1 |
            """, cancellationToken: TestContext.Current.CancellationToken);

        fixture.SetWorkspace(workspace);
        await fixture.SeedTraceabilityLinkAsync(workspace, "FR-1", "tr", "TR-1").ConfigureAwait(true);

        var service = fixture.CreateService();
        var export = await service.GenerateWikiAsync(Path.Combine(project, "wiki"), ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var links = await fixture.GetTraceabilityRowsAsync().ConfigureAwait(true);

        Assert.True(export.Success);
        var link = Assert.Single(links);
        Assert.Equal("FR-1", link.FrId);
        Assert.Equal("tr", link.TargetKind);
        Assert.Equal("TR-1", link.TargetId);
    }
}
