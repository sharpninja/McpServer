using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// Overlay G6 wiki dump/import: named tests against shipped
/// <see cref="WikiDumpService"/>, <see cref="RequirementsWikiExportOrchestrator"/>,
/// and <see cref="WorkspaceService"/>. TEST-MCP-WIKIEXPORT-003 through 005.
/// </summary>
/// <remarks>
/// Requirement coverage named by requirement evidence: FR-MCP-WIKIEXPORT-003, FR-MCP-WIKIEXPORT-004 and
/// FR-MCP-WIKIEXPORT-005 (every criterion of each; they carry no criterion ids).
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>FR-MCP-WIKIEXPORT-003 through FR-MCP-WIKIEXPORT-005 (criteria without ids): cites the criteria whose evidence names this class, so the evidence no longer names the test (operator: "Must be fixed.").</description></item>
/// <item><term>2026-11-07</term><description>TEST-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-004; FR-MCP-WIKIEXPORT-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/WikiDumpG6OverlayTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 3 Category=Integration cases and the helpers only they use (CreateDocService and CreateWikiRequest) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class WikiDumpG6OverlayTestsBase in Services/WikiDumpG6OverlayTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</description></item>
/// </list>
/// </remarks>
public sealed class WikiDumpG6OverlayTests : WikiDumpG6OverlayTestsBase, IDisposable
{
    /// <summary>TEST-MCP-WIKIEXPORT-003: no dump file without --include-dump on GenerateWikiAsync.</summary>
    /// <remarks>Integration: writes physical wiki output or reads the shipped service-registration source (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task WikiExport_WithoutDumpFlag_UnchangedBehavior()
    {
        var first = Path.Combine(_workspace, "out-a");
        var second = Path.Combine(_workspace, "out-b");
        var service = CreateDocService();
        var a = await service.GenerateWikiAsync(first, ct: TestContext.Current.CancellationToken, includeDump: false);
        var b = await service.GenerateWikiAsync(second, ct: TestContext.Current.CancellationToken, includeDump: false);
        Assert.True(a.Success);
        Assert.True(b.Success);
        Assert.False(File.Exists(Path.Combine(first, WikiDumpDefaults.FileName)));
        Assert.False(File.Exists(Path.Combine(second, WikiDumpDefaults.FileName)));
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: GenerateWikiAsync includeDump writes versioned JSON keyed by workspace.</summary>
    /// <remarks>Integration: writes physical wiki output or reads the shipped service-registration source (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace()
    {
        var output = Path.Combine(_workspace, "wiki-dump");
        var service = CreateDocService();
        var exported = await service.GenerateWikiAsync(output, ct: TestContext.Current.CancellationToken, includeDump: true);
        Assert.True(exported.Success);
        var dumpPath = Path.Combine(output, WikiDumpDefaults.FileName);
        Assert.True(File.Exists(dumpPath));
        var dump = JsonSerializer.Deserialize<WikiDumpDocument>(await File.ReadAllTextAsync(dumpPath, TestContext.Current.CancellationToken), Camel())!;
        Assert.Equal(WikiDumpDefaults.SchemaVersion, dump.SchemaVersion);
        Assert.Equal(_workspace, dump.SourceWorkspacePath);
        Assert.False(string.IsNullOrWhiteSpace(dump.SourceWorkspaceKey));
        Assert.NotEmpty(dump.Tables);

        var requirements = Substitute.For<IRequirementsDocumentService>();
        requirements.GenerateWikiAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(new RequirementsDocumentExportResult { Success = true, Format = "wiki", Files = [] });
        var controller = new RequirementsController(
            requirements,
            MsOptions.Options.Create(new RequirementsOptions()),
            new WorkspaceContext { WorkspacePath = _workspace },
            Substitute.For<ITodoExecutionService>(),
            NullLogger<RequirementsController>.Instance);
        _ = await controller.GenerateAsync("all", "wiki", TestContext.Current.CancellationToken, includeDump: true);
        await requirements.Received().GenerateWikiAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>(), true);
        var generate = typeof(FwhMcpTools).GetMethod(nameof(FwhMcpTools.RequirementsGenerate));
        Assert.NotNull(generate);
        Assert.Contains(generate!.GetParameters(), parameter => parameter.Name == "includeDump");
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: shipped DI resolves the singleton orchestrator without a captive scoped dump.</summary>
    /// <remarks>Integration: writes physical wiki output or reads the shipped service-registration source (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task WikiExport_ShippedDi_ResolvesOrchestratorWithoutCaptiveScope()
    {
        var output = Path.Combine(_workspace, "wiki-di");
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var services = new ServiceCollection();
        services.AddSingleton<IRequirementsDocFxWorkflowRunner, DisabledRequirementsDocFxWorkflowRunner>();
        services.AddSingleton<IRequirementsWikiExportOrchestrator>(provider =>
            new RequirementsWikiExportOrchestrator(
                provider.GetRequiredService<IRequirementsDocFxWorkflowRunner>(),
                provider.GetRequiredService<IServiceScopeFactory>()));
        services.AddSingleton(connection);
        services.AddScoped<WorkspaceContext>();
        services.AddScoped(provider =>
        {
            var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(provider.GetRequiredService<SqliteConnection>()).Options;
            return new McpDbContext(options, provider.GetRequiredService<WorkspaceContext>());
        });
        services.AddScoped<IWikiDumpService, WikiDumpService>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using (var setup = provider.CreateScope())
        {
            var context = setup.ServiceProvider.GetRequiredService<WorkspaceContext>();
            context.WorkspacePath = _workspace;
            var db = setup.ServiceProvider.GetRequiredService<McpDbContext>();
            db.Database.EnsureCreated();
            db.TodoItems.Add(new TodoItemEntity
            {
                Id = "TODO-DI-KEEP",
                Title = "keep",
                Section = "overlay",
                Priority = "high",
                WorkspaceId = _workspace,
            });
            db.TodoItems.Add(new TodoItemEntity
            {
                Id = "TODO-DI-OTHER",
                Title = "other",
                Section = "overlay",
                Priority = "high",
                WorkspaceId = _workspace + "-other",
            });
            db.SaveChanges();
        }

        var orchestrator = provider.GetRequiredService<IRequirementsWikiExportOrchestrator>();
        await orchestrator.ExportAsync(CreateWikiRequest(output, includeDump: true), TestContext.Current.CancellationToken);
        var dumpPath = Path.Combine(output, WikiDumpDefaults.FileName);
        Assert.True(File.Exists(dumpPath));
        var dump = JsonSerializer.Deserialize<WikiDumpDocument>(await File.ReadAllTextAsync(dumpPath, TestContext.Current.CancellationToken), Camel())!;
        Assert.Contains(dump.Todos, item => item.Id == "TODO-DI-KEEP");
        Assert.DoesNotContain(dump.Todos, item => item.Id == "TODO-DI-OTHER");
        Assert.Equal(_workspace, dump.SourceWorkspacePath);
    }

    private RequirementsDocumentService CreateDocService()
    {
        var docs = Path.Combine(_workspace, "docs", "Project");
        Directory.CreateDirectory(docs);
        File.WriteAllText(Path.Combine(docs, "Functional-Requirements.md"), "# Functional Requirements (QBrain.AI)\n");
        File.WriteAllText(Path.Combine(docs, "Technical-Requirements.md"), "# Technical Requirements (QBrain.AI)\n");
        File.WriteAllText(Path.Combine(docs, "Testing-Requirements.md"), "# Testing Requirements (QBrain.AI)\n");
        File.WriteAllText(Path.Combine(docs, "TR-per-FR-Mapping.md"), "# TR per FR Mapping\n");
        File.WriteAllText(Path.Combine(docs, "Requirements-Matrix.md"), "# Requirements Matrix (QBrain.AI)\n");
        var options = new RequirementsOptions
        {
            FunctionalRequirementsPath = Path.Combine(docs, "Functional-Requirements.md"),
            TechnicalRequirementsPath = Path.Combine(docs, "Technical-Requirements.md"),
            TestingRequirementsPath = Path.Combine(docs, "Testing-Requirements.md"),
            MappingPath = Path.Combine(docs, "TR-per-FR-Mapping.md"),
            MatrixPath = Path.Combine(docs, "Requirements-Matrix.md"),
            WikiConfigPath = Path.Combine(_workspace, "docs", "wiki.yaml"),
        };
        var orchestrator = new RequirementsWikiExportOrchestrator(new DisabledRequirementsDocFxWorkflowRunner(), CreateDumpService());
        return new RequirementsDocumentService(MsOptions.Options.Create(options), NullLogger<RequirementsDocumentService>.Instance, wikiExportOrchestrator: orchestrator);
    }

    private RequirementsWikiExportRequest CreateWikiRequest(string outputRoot, bool includeDump)
        => new(
            outputRoot,
            DateTimeOffset.UtcNow,
            _workspace,
            new RequirementsOptions(),
            [],
            [],
            [],
            [],
            null,
            includeDump);
}
