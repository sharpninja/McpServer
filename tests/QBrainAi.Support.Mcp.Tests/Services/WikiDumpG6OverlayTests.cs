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

namespace QBrainAi.Support.Mcp.Tests.Services;

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
/// <item><term>2026-11-07</term><description>TEST-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-003; FR-MCP-WIKIEXPORT-004; FR-MCP-WIKIEXPORT-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 3 Category=Integration cases (WikiExport_WithoutDumpFlag_UnchangedBehavior, WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace and WikiExport_ShippedDi_ResolvesOrchestratorWithoutCaptiveScope) and the helpers only they use (CreateDocService and CreateWikiRequest) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_workspace, _connection, _options, .ctor, Dispose, CreateDumpService, CreateDb and Camel) moved once into the abstract base class WikiDumpG6OverlayTestsBase in Services/WikiDumpG6OverlayTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class WikiDumpG6OverlayTests : WikiDumpG6OverlayTestsBase, IDisposable
{
    /// <summary>TEST-MCP-WIKIEXPORT-003: dump TODOs and requirement links match the store.</summary>
    [Fact]
    public async Task Dump_ContainsTodoRowsAndRequirementLinksMatchingStore()
    {
        SeedTodo("TODO-DUMP-001");
        SeedLink("TODO-DUMP-001", "fr", "FR-DUMP-001");
        var output = Path.Combine(_workspace, "dump-match");
        var exported = await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = output, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        Assert.True(exported.Success);
        var dump = exported.Dump!;
        Assert.Contains(dump.Todos, item => item.Id == "TODO-DUMP-001");
        Assert.Contains(dump.TodoRequirementLinks, item => item.TodoId == "TODO-DUMP-001" && item.RequirementId == "FR-DUMP-001");
        using var verify = CreateDb();
        Assert.Equal(verify.TodoItems.Count(), dump.Todos.Count);
        Assert.Equal(verify.TodoRequirementLinks.Count(), dump.TodoRequirementLinks.Count);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: sha256 matches canonical UTF-8 JSON without the hash field.</summary>
    [Fact]
    public async Task Dump_Sha256_MatchesCanonicalUtf8Json()
    {
        var output = Path.Combine(_workspace, "dump-hash");
        var exported = await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = output, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        var dump = exported.Dump!;
        var copy = JsonSerializer.Deserialize<WikiDumpDocument>(JsonSerializer.Serialize(dump, Camel()), Camel())!;
        copy.Sha256 = string.Empty;
        var canonical = JsonSerializer.Serialize(copy, Camel());
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        Assert.Equal(expected, dump.Sha256);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-004: add-workspace dump hydrates TODOs from dump, not todo.yaml.</summary>
    [Fact]
    public async Task AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml()
    {
        SeedTodo("TODO-FROM-DUMP");
        var dumpRoot = Path.Combine(_workspace, "hydrate-dump");
        await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = dumpRoot, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        File.WriteAllText(Path.Combine(dumpRoot, "todo.yaml"), "id: TODO-FROM-YAML\n");

        var dest = Path.Combine(Path.GetTempPath(), "wiki-g6-dest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dest);
        var destDb = CreateDestDb(dest);
        destDb.Database.EnsureCreated();
        var dump = new WikiDumpService(destDb);
        var created = await CreateWorkspaceService(destDb, dump).CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = dest,
            Name = "g6-dump",
            DumpPath = dumpRoot,
        }, TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);
        Assert.Contains(destDb.TodoItems, item => item.Id == "TODO-FROM-DUMP");
        Assert.DoesNotContain(destDb.TodoItems, item => item.Id == "TODO-FROM-YAML");
    }

    /// <summary>TEST-MCP-WIKIEXPORT-004: import remaps workspace id; old id is absent.</summary>
    [Fact]
    public async Task Import_RemapsWorkspaceIdAndPaths_OldIdAbsent()
    {
        SeedTodo("TODO-REMAP");
        var dumpRoot = Path.Combine(_workspace, "remap-dump");
        await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = dumpRoot, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        var dest = Path.Combine(Path.GetTempPath(), "wiki-g6-remap", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dest);
        var destDb = CreateDestDb(dest);
        destDb.Database.EnsureCreated();
        var imported = await new WikiDumpService(destDb).ImportAsync(new WikiDumpImportRequest
        {
            DumpPath = dumpRoot,
            DestinationWorkspacePath = dest,
        }, TestContext.Current.CancellationToken);
        Assert.True(imported.Success, imported.Error);
        var todo = Assert.Single(destDb.TodoItems);
        Assert.Equal(ExpectedWorkspaceKey.Of(dest), todo.WorkspaceId);
        Assert.DoesNotContain(ExpectedWorkspaceKey.Of(_workspace), todo.WorkspaceId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-004: malformed dump, version mismatch, missing tables, unsafe path are rejected.</summary>
    [Fact]
    public async Task Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected()
    {
        var sut = CreateDumpService();
        var dest = Path.Combine(_workspace, "dest");
        Directory.CreateDirectory(dest);

        var malformed = Path.Combine(_workspace, "bad.json");
        await File.WriteAllTextAsync(malformed, "{not-json", TestContext.Current.CancellationToken);
        var badJson = await sut.ImportAsync(new WikiDumpImportRequest { DumpPath = malformed, DestinationWorkspacePath = dest }, TestContext.Current.CancellationToken);
        Assert.False(badJson.Success);
        Assert.Equal("malformed_dump", badJson.ErrorCode);

        var version = Path.Combine(_workspace, "version.json");
        await File.WriteAllTextAsync(version, """{"schemaVersion":"v0","tables":[{"name":"TodoItems"}]}""", TestContext.Current.CancellationToken);
        var badVersion = await sut.ImportAsync(new WikiDumpImportRequest { DumpPath = version, DestinationWorkspacePath = dest }, TestContext.Current.CancellationToken);
        Assert.False(badVersion.Success);
        Assert.Equal("version_mismatch", badVersion.ErrorCode);

        var missing = Path.Combine(_workspace, "missing.json");
        await File.WriteAllTextAsync(missing, """{"schemaVersion":"mcp-wiki-dump/v1","tables":[]}""", TestContext.Current.CancellationToken);
        var badTables = await sut.ImportAsync(new WikiDumpImportRequest { DumpPath = missing, DestinationWorkspacePath = dest }, TestContext.Current.CancellationToken);
        Assert.False(badTables.Success);
        Assert.Equal("missing_tables", badTables.ErrorCode);

        var unsafePath = await sut.ImportAsync(new WikiDumpImportRequest { DumpPath = "..\\windows\\system32", DestinationWorkspacePath = dest }, TestContext.Current.CancellationToken);
        Assert.False(unsafePath.Success);
        Assert.Equal("unsafe_path", unsafePath.ErrorCode);
        using var verify = CreateDb();
        Assert.Empty(verify.TodoItems);

        var createDest = Path.Combine(Path.GetTempPath(), "wiki-g6-reject", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(createDest);
        var createDb = CreateDestDb(createDest);
        createDb.Database.EnsureCreated();
        var rejected = await CreateWorkspaceService(createDb, new WikiDumpService(createDb)).CreateAsync(new WorkspaceCreateRequest
        {
            WorkspacePath = createDest,
            Name = "g6-reject",
            DumpPath = malformed,
        }, TestContext.Current.CancellationToken);
        Assert.False(rejected.Success);
        Assert.Empty(createDb.Workspaces.IgnoreQueryFilters().Where(row => row.WorkspacePath == createDest));
        Assert.Empty(createDb.TodoItems);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-004: re-import does not duplicate TODOs.</summary>
    [Fact]
    public async Task Import_IdempotentReimport_NoDuplicateTodos()
    {
        SeedTodo("TODO-IDEM");
        var dumpRoot = Path.Combine(_workspace, "idem-dump");
        await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = dumpRoot, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        var dest = Path.Combine(Path.GetTempPath(), "wiki-g6-idem", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dest);
        var destDb = CreateDestDb(dest);
        destDb.Database.EnsureCreated();
        var first = await new WikiDumpService(destDb).ImportAsync(new WikiDumpImportRequest { DumpPath = dumpRoot, DestinationWorkspacePath = dest }, TestContext.Current.CancellationToken);
        var second = await new WikiDumpService(destDb).ImportAsync(new WikiDumpImportRequest { DumpPath = dumpRoot, DestinationWorkspacePath = dest }, TestContext.Current.CancellationToken);
        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Single(destDb.TodoItems);
        Assert.Empty(second.CreatedTodoIds);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-005: todo.yaml is not the TODO source when dump is present.</summary>
    [Fact]
    public async Task TodoYaml_NotSourceOfTruth_WhenDumpPresent()
    {
        SeedTodo("TODO-DUMP-WINS");
        var dumpRoot = Path.Combine(_workspace, "yaml-vs-dump");
        await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = dumpRoot, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        var yaml = Path.Combine(dumpRoot, "todo.yaml");
        await File.WriteAllTextAsync(yaml, "id: TODO-YAML-ONLY\n", TestContext.Current.CancellationToken);
        var dest = Path.Combine(Path.GetTempPath(), "wiki-g6-yaml", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dest);
        var destDb = CreateDestDb(dest);
        destDb.Database.EnsureCreated();
        var imported = await new WikiDumpService(destDb).ImportAsync(new WikiDumpImportRequest
        {
            DumpPath = dumpRoot,
            DestinationWorkspacePath = dest,
            TodoYamlPath = yaml,
        }, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(destDb.TodoItems, item => item.Id == "TODO-YAML-ONLY");
        Assert.Contains(destDb.TodoItems, item => item.Id == "TODO-DUMP-WINS");
        Assert.Contains(imported.Diagnostics, item => item.Contains("dump_wins_over_todo_yaml", StringComparison.Ordinal));
    }

    /// <summary>TEST-MCP-WIKIEXPORT-005: cleanup archives todo.yaml with evidence and does not silent-delete.</summary>
    [Fact]
    public async Task TodoYaml_Cleanup_ArchivesWithEvidence_NoSilentDelete()
    {
        SeedTodo("TODO-ARCHIVE");
        var dumpRoot = Path.Combine(_workspace, "archive-dump");
        await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = dumpRoot, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        var yaml = Path.Combine(dumpRoot, "todo.yaml");
        await File.WriteAllTextAsync(yaml, "id: leftover\n", TestContext.Current.CancellationToken);
        var dest = Path.Combine(Path.GetTempPath(), "wiki-g6-archive", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dest);
        var destDb = CreateDestDb(dest);
        destDb.Database.EnsureCreated();
        var imported = await new WikiDumpService(destDb).ImportAsync(new WikiDumpImportRequest
        {
            DumpPath = dumpRoot,
            DestinationWorkspacePath = dest,
            TodoYamlPath = yaml,
        }, TestContext.Current.CancellationToken);
        Assert.True(File.Exists(yaml));
        Assert.False(string.IsNullOrWhiteSpace(imported.TodoYamlArchivePath));
        Assert.True(File.Exists(imported.TodoYamlArchivePath));
        Assert.Contains(".mcpServer", imported.TodoYamlArchivePath, StringComparison.Ordinal);
        Assert.Contains("archive", imported.TodoYamlArchivePath, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-WIKIEXPORT-005: dump and todo.yaml conflict names both paths.</summary>
    [Fact]
    public async Task DumpAndTodoYaml_Conflict_DumpWins_DiagnosticNamesBoth()
    {
        SeedTodo("TODO-BOTH");
        var dumpRoot = Path.Combine(_workspace, "conflict-dump");
        var exported = await CreateDumpService().ExportAsync(new WikiDumpExportRequest { OutputRoot = dumpRoot, IncludeDump = true, WorkspacePath = _workspace }, TestContext.Current.CancellationToken);
        var yaml = Path.Combine(dumpRoot, "todo.yaml");
        await File.WriteAllTextAsync(yaml, "id: conflict\n", TestContext.Current.CancellationToken);
        var dest = Path.Combine(Path.GetTempPath(), "wiki-g6-conflict", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dest);
        var destDb = CreateDestDb(dest);
        destDb.Database.EnsureCreated();
        var imported = await new WikiDumpService(destDb).ImportAsync(new WikiDumpImportRequest
        {
            DumpPath = dumpRoot,
            DestinationWorkspacePath = dest,
            TodoYamlPath = yaml,
        }, TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(imported.Diagnostics);
        Assert.Contains(exported.DumpFilePath!, diagnostic, StringComparison.Ordinal);
        Assert.Contains(yaml, diagnostic, StringComparison.Ordinal);
    }

    private static WorkspaceService CreateWorkspaceService(McpDbContext db, IWikiDumpService dump)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Mcp:Workspaces"] = "[]" })
            .Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(Path.GetTempPath());
        var projection = Substitute.For<IWorkspaceProjectionWriter>();
        projection.WriteProjectionAsync(Arg.Any<IReadOnlyList<WorkspaceConfigEntry>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return new WorkspaceService(
            configuration,
            environment,
            Substitute.For<IProcessRunner>(),
            db,
            projection,
            NullLogger<WorkspaceService>.Instance,
            eventBus: null,
            wikiDumpService: dump);
    }

    private static McpDbContext CreateDestDb(string dest)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        return new McpDbContext(options, new WorkspaceContext { WorkspacePath = dest });
    }

    private void SeedTodo(string id)
    {
        using var db = CreateDb();
        db.TodoItems.Add(new TodoItemEntity
        {
            Id = id,
            Title = id,
            Section = "overlay",
            Priority = "high",
            WorkspaceId = _workspace,
        });
        db.SaveChanges();
    }

    private void SeedLink(string todoId, string kind, string requirementId)
    {
        using var db = CreateDb();
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspace,
            Kind = kind,
            Id = requirementId,
            Title = requirementId,
            Body = "body",
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = "2026-09-10T00:00:00Z",
            UpdatedAtUtc = "2026-09-10T00:00:00Z",
        });
        db.TodoRequirementLinks.Add(new TodoRequirementLinkEntity
        {
            WorkspaceId = _workspace,
            TodoId = todoId,
            RequirementKind = kind,
            RequirementId = requirementId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
    }
}
