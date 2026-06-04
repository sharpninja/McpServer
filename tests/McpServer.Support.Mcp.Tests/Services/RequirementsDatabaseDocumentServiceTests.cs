using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Acceptance tests for DB-backed, workspace-scoped requirements storage.</summary>
public sealed class RequirementsDatabaseDocumentServiceTests
{
    /// <summary>Overlapping requirement ids do not leak between workspaces.</summary>
    [Fact]
    public async Task ListsAndExports_AreScopedToActiveWorkspace()
    {
        using var fixture = new RequirementsDbFixture();
        var workspaceA = fixture.CreateWorkspace("a");
        var workspaceB = fixture.CreateWorkspace("b");

        var service = fixture.CreateService();
        fixture.SetWorkspace(workspaceA);
        await service.AddFrAsync(new FrEntry("FR-MCP-900", "Workspace A", "A body"));
        await service.AddTrAsync(new TrEntry("TR-MCP-900", "A TR", "A TR body"));
        await service.AddTestAsync(new TestEntry("TEST-MCP-900", "A test"));
        Assert.Contains(await fixture.GetRequirementRowsAsync(), x => x.Id == "FR-MCP-900");
        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-900", ["TR-MCP-900"], ["TEST-MCP-900"]));

        fixture.SetWorkspace(workspaceB);
        await service.AddFrAsync(new FrEntry("FR-MCP-900", "Workspace B", "B body"));
        await service.AddTrAsync(new TrEntry("TR-MCP-900", "B TR", "B TR body"));
        await service.AddTestAsync(new TestEntry("TEST-MCP-900", "B test"));
        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-900", ["TR-MCP-900"], ["TEST-MCP-900"]));

        fixture.SetWorkspace(workspaceA);
        var fr = Assert.Single(await service.GetAllFrAsync());
        Assert.Equal("Workspace A", fr.Title);

        var (mappingMarkdown, _) = await service.GenerateDocumentAsync(RequirementsDocType.Mapping);
        Assert.Contains("TEST-MCP-900", mappingMarkdown);
        Assert.DoesNotContain("Workspace B", mappingMarkdown);

        var (matrixMarkdown, _) = await service.GenerateDocumentAsync(RequirementsDocType.Matrix);
        Assert.Contains("| FR-MCP-900 | Tracked | Functional-Requirements.md |", matrixMarkdown);
        Assert.Contains("| TR-MCP-900 | Tracked | Technical-Requirements.md |", matrixMarkdown);
        Assert.Contains("| TEST-MCP-900 | Tracked | Testing-Requirements.md |", matrixMarkdown);

        var outputRoot = Path.Combine(workspaceA, "docs", "Project", "export");
        var export = await service.GenerateAllAsync(outputRoot);
        var functional = await File.ReadAllTextAsync(Path.Combine(outputRoot, "Functional-Requirements.md"));
        Assert.Contains("Workspace A", functional);
        Assert.DoesNotContain("Workspace B", functional);
        Assert.Contains(export.Files, file => file.RelativePath == "Functional-Requirements.md");
        Assert.Contains(export.Files, file => file.RelativePath == "Requirements-Matrix.md");
    }

    /// <summary>Mapping validation rejects missing FR/TR/TEST ids before storing links.</summary>
    [Fact]
    public async Task UpsertMapping_ValidatesFrTrAndTestIds()
    {
        using var fixture = new RequirementsDbFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("validate"));
        var service = fixture.CreateService();

        await service.AddFrAsync(new FrEntry("FR-MCP-901", "FR", "FR body"));
        await service.AddTrAsync(new TrEntry("TR-MCP-901", "TR", "TR body"));
        await service.AddTestAsync(new TestEntry("TEST-MCP-901", "Test body"));
        Assert.Contains(await fixture.GetRequirementRowsAsync(), x => x.Id == "FR-MCP-901");

        await service.UpsertMappingAsync(new FrTrMapping("FR-MCP-901", ["TR-MCP-901"], ["TEST-MCP-901"]));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpsertMappingAsync(new FrTrMapping("FR-MCP-901", ["TR-MCP-MISSING"], [])));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpsertMappingAsync(new FrTrMapping("FR-MCP-901", [], ["TEST-MCP-MISSING"])));
    }

    /// <summary>Requirement metadata survives create, update, list, and get through DB storage.</summary>
    [Fact]
    public async Task RequirementMetadata_RoundTripsThroughDatabaseStorage()
    {
        using var fixture = new RequirementsDbFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("metadata"));
        var service = fixture.CreateService();

        await service.AddFrAsync(new FrEntry("FR-MCP-902", "FR", "FR body", Priority: "medium", Status: "pending", Notes: "draft"));
        await service.AddTrAsync(new TrEntry("TR-MCP-REQ-902", "TR", "TR body", Priority: "medium", Status: "pending", Notes: "draft"));
        await service.AddTestAsync(new TestEntry("TEST-MCP-902", "Test body", Title: "Metadata test", Priority: "medium", Status: "pending", Notes: "draft"));

        await service.UpdateFrAsync(new FrEntry("FR-MCP-902", "FR", "FR body", Priority: "high", Status: "in_progress", Notes: "reviewed"));
        await service.UpdateTrAsync(new TrEntry("TR-MCP-REQ-902", "TR", "TR body", Priority: "high", Status: "completed", Notes: "reviewed"));
        await service.UpdateTestAsync(new TestEntry("TEST-MCP-902", "Test body", Title: "Metadata test", Priority: "high", Status: "completed", Notes: "reviewed"));

        var fr = Assert.Single(await service.GetAllFrAsync());
        Assert.Equal("high", fr.Priority);
        Assert.Equal("in_progress", fr.Status);
        Assert.Equal("reviewed", fr.Notes);

        var tr = Assert.Single(await service.GetAllTrAsync());
        Assert.Equal("high", tr.Priority);
        Assert.Equal("completed", tr.Status);
        Assert.Equal("reviewed", tr.Notes);

        var test = await service.GetTestAsync("TEST-MCP-902");
        Assert.NotNull(test);
        Assert.Equal("Metadata test", test.Title);
        Assert.Equal("high", test.Priority);
        Assert.Equal("completed", test.Status);
        Assert.Equal("reviewed", test.Notes);
    }

    /// <summary>Atomic batch creation persists all FR/TR/TEST rows when every record is valid.</summary>
    [Fact]
    public async Task AddBatchAsync_ValidMixedRecords_PersistsAllRows()
    {
        using var fixture = new RequirementsDbFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("batch-create"));
        var service = fixture.CreateService();

        var result = await service.AddBatchAsync(new RequirementsBatchEntries(
            [new FrEntry("FR-MCP-903", "Batch FR", "FR body", Priority: "high")],
            [new TrEntry("TR-MCP-BATCH-903", "Batch TR", "TR body", Priority: "high")],
            [new TestEntry("TEST-MCP-903", "TEST body", Title: "Batch TEST", Priority: "high")])).ConfigureAwait(true);

        Assert.Equal(3, result.Count);
        Assert.Equal("high", Assert.Single(result.Functional).Priority);
        Assert.Equal("FR-MCP-903", (await service.GetFrAsync("FR-MCP-903").ConfigureAwait(true))?.Id);
        Assert.Equal("TR-MCP-BATCH-903", (await service.GetTrAsync("TR-MCP-BATCH-903").ConfigureAwait(true))?.Id);
        Assert.Equal("TEST-MCP-903", (await service.GetTestAsync("TEST-MCP-903").ConfigureAwait(true))?.Id);
    }

    /// <summary>Atomic batch updates preserve structured acceptance criteria through database storage.</summary>
    [Fact]
    public async Task UpdateBatchAsync_PreservesAcceptanceCriteria()
    {
        using var fixture = new RequirementsDbFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("batch-update-ac"));
        var service = fixture.CreateService();
        await service.AddFrAsync(new FrEntry("FR-MCP-906", "Existing", "Existing body")).ConfigureAwait(true);

        var result = await service.UpdateBatchAsync(new RequirementsBatchEntries(
            [
                new FrEntry(
                    "FR-MCP-906",
                    "Updated",
                    "Updated body",
                    AcceptanceCriteria:
                    [
                        new AcceptanceCriterion
                        {
                            Id = "FR-MCP-906-AC001",
                            Text = "Batch update preserves criteria.",
                            IsSatisfied = false
                        }
                    ])
            ],
            [],
            [])).ConfigureAwait(true);

        var resultCriteria = Assert.Single(Assert.Single(result.Functional).AcceptanceCriteria!);
        Assert.Equal("FR-MCP-906-AC001", resultCriteria.Id);
        Assert.Equal("Batch update preserves criteria.", resultCriteria.Text);
        Assert.False(resultCriteria.IsSatisfied);

        var reloaded = await service.GetFrAsync("FR-MCP-906").ConfigureAwait(true);
        var persistedCriteria = Assert.Single(reloaded!.AcceptanceCriteria!);
        Assert.Equal("FR-MCP-906-AC001", persistedCriteria.Id);
        Assert.Equal("Batch update preserves criteria.", persistedCriteria.Text);
        Assert.False(persistedCriteria.IsSatisfied);
    }

    /// <summary>Atomic batch creation rejects conflicts before committing any later record.</summary>
    [Fact]
    public async Task AddBatchAsync_ExistingConflict_RollsBackWholeBatch()
    {
        using var fixture = new RequirementsDbFixture();
        fixture.SetWorkspace(fixture.CreateWorkspace("batch-conflict"));
        var service = fixture.CreateService();
        await service.AddFrAsync(new FrEntry("FR-MCP-904", "Existing", "Existing body")).ConfigureAwait(true);

        await Assert.ThrowsAsync<RequirementsConflictException>(() =>
            service.AddBatchAsync(new RequirementsBatchEntries(
                [
                    new FrEntry("FR-MCP-904", "Conflict", "Conflict body"),
                    new FrEntry("FR-MCP-905", "Should not persist", "Should not persist body")
                ],
                [],
                []))).ConfigureAwait(true);

        Assert.Null(await service.GetFrAsync("FR-MCP-905").ConfigureAwait(true));
    }

    /// <summary>Bootstrap accepts bold legacy headings and does not treat notes columns as TEST links.</summary>
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
            """);
        await File.WriteAllTextAsync(
            Path.Combine(project, "Technical-Requirements.md"),
            """
            # Technical Requirements

            ## **TR-1 — Target frameworks.**

            **TR-1 — Target frameworks.** SDK targets net10.0.
            """);
        await File.WriteAllTextAsync(
            Path.Combine(project, "TR-per-FR-Mapping.md"),
            """
            # TR per FR Mapping

            | Functional Requirement | Technical Requirements | Notes |
            | --- | --- | --- |
            | FR-1 | TR-1 | Notes are prose, not TEST ids. |
            """);

        fixture.SetWorkspace(workspace);
        var service = fixture.CreateService();
        var export = await service.GenerateWikiAsync(Path.Combine(project, "wiki")).ConfigureAwait(true);
        var rows = await fixture.GetRequirementRowsAsync().ConfigureAwait(true);

        Assert.True(export.Success);
        Assert.Contains(rows, row => row.Kind == "fr" && row.Id == "FR-1");
        Assert.Contains(rows, row => row.Kind == "tr" && row.Id == "TR-1");
        Assert.DoesNotContain(rows, row => row.Kind == "test" && row.Id.Contains("Notes", StringComparison.OrdinalIgnoreCase));

        var (mappingMarkdown, _) = await service.GenerateDocumentAsync(RequirementsDocType.Mapping).ConfigureAwait(true);
        Assert.Contains("TR-1", mappingMarkdown);
        Assert.DoesNotContain("Notes are prose", mappingMarkdown);
    }

    /// <summary>Bootstrap rebuilds traceability when orphan links were left by a failed import.</summary>
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
            """);
        await File.WriteAllTextAsync(
            Path.Combine(project, "Technical-Requirements.md"),
            """
            # Technical Requirements

            ## TR-1

            The importer shall tolerate existing orphan links.
            """);
        await File.WriteAllTextAsync(
            Path.Combine(project, "TR-per-FR-Mapping.md"),
            """
            # TR per FR Mapping

            | Functional Requirement | Technical Requirements |
            | --- | --- |
            | FR-1 | TR-1 |
            """);

        fixture.SetWorkspace(workspace);
        await fixture.SeedTraceabilityLinkAsync(workspace, "FR-1", "tr", "TR-1").ConfigureAwait(true);

        var service = fixture.CreateService();
        var export = await service.GenerateWikiAsync(Path.Combine(project, "wiki")).ConfigureAwait(true);
        var links = await fixture.GetTraceabilityRowsAsync().ConfigureAwait(true);

        Assert.True(export.Success);
        var link = Assert.Single(links);
        Assert.Equal("FR-1", link.FrId);
        Assert.Equal("tr", link.TargetKind);
        Assert.Equal("TR-1", link.TargetId);
    }

    private sealed class RequirementsDbFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _requestScope;
        private readonly DefaultHttpContext _httpContext;
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly string _root = Path.Combine(Path.GetTempPath(), "mcp-reqdb-tests-" + Guid.NewGuid().ToString("N"));

        public RequirementsDbFixture()
        {
            var services = new ServiceCollection();
            _connection.Open();
            services.AddDbContext<McpDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<WorkspaceContext>();
            services.AddHttpContextAccessor();
            services.AddSingleton<IOptions<RequirementsOptions>>(Microsoft.Extensions.Options.Options.Create(new RequirementsOptions()));
            services.AddSingleton(NullLogger<RequirementsDatabaseDocumentService>.Instance);
            _provider = services.BuildServiceProvider();
            using (var schemaScope = _provider.CreateScope())
            {
                schemaScope.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();
            }
            _requestScope = _provider.CreateScope();
            _httpContext = new DefaultHttpContext { RequestServices = _requestScope.ServiceProvider };
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
        }

        public string CreateWorkspace(string name)
        {
            var path = Path.Combine(_root, name);
            Directory.CreateDirectory(Path.Combine(path, "docs", "Project"));
            return path;
        }

        public RequirementsDatabaseDocumentService CreateService() =>
            new(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                _provider.GetRequiredService<IOptions<RequirementsOptions>>(),
                NullLogger<RequirementsDatabaseDocumentService>.Instance,
                _provider.GetRequiredService<IHttpContextAccessor>());

        public async Task<IReadOnlyList<RequirementEntity>> GetRequirementRowsAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<McpDbContext>()
                .Requirements
                .IgnoreQueryFilters()
                .OrderBy(x => x.WorkspaceId)
                .ThenBy(x => x.Kind)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<RequirementTraceabilityLinkEntity>> GetTraceabilityRowsAsync()
        {
            await using var scope = _provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<McpDbContext>()
                .RequirementTraceabilityLinks
                .IgnoreQueryFilters()
                .OrderBy(x => x.WorkspaceId)
                .ThenBy(x => x.FrId)
                .ThenBy(x => x.TargetKind)
                .ThenBy(x => x.TargetId)
                .ToListAsync();
        }

        public async Task SeedTraceabilityLinkAsync(string workspacePath, string frId, string targetKind, string targetId)
        {
            await using var scope = _provider.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            await EnsureRequirementAsync(ctx, workspacePath, "fr", frId);
            await EnsureRequirementAsync(ctx, workspacePath, targetKind, targetId);
            ctx.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity
            {
                WorkspaceId = workspacePath,
                FrId = frId,
                TargetKind = targetKind,
                TargetId = targetId,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task EnsureRequirementAsync(McpDbContext ctx, string workspacePath, string kind, string id)
        {
            if (await ctx.Requirements.IgnoreQueryFilters()
                    .AnyAsync(x => x.WorkspaceId == workspacePath && x.Kind == kind && x.Id == id))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToString("O");
            ctx.Requirements.Add(new RequirementEntity
            {
                WorkspaceId = workspacePath,
                Kind = kind,
                Id = id,
                Title = id,
                Body = "Seeded requirement for traceability FK coverage.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
        }

        public void SetWorkspace(string workspacePath)
        {
            _provider.GetRequiredService<IHttpContextAccessor>().HttpContext = _httpContext;
            var ctx = _httpContext.RequestServices.GetRequiredService<WorkspaceContext>();
            ctx.WorkspacePath = workspacePath;
            ctx.WorkspaceName = Path.GetFileName(workspacePath);
        }

        public void Dispose()
        {
            _requestScope.Dispose();
            _provider.Dispose();
            _connection.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }
}
