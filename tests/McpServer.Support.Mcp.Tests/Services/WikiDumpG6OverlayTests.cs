using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Overlay G6 wiki dump/import: named tests against shipped
/// <see cref="WikiDumpService"/>, <see cref="RequirementsWikiExportOrchestrator"/>,
/// and <see cref="WorkspaceService"/>. TEST-MCP-WIKIEXPORT-003 through 005.
/// </summary>
public sealed class WikiDumpG6OverlayTests : IDisposable
{
    private readonly string _workspace;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Isolated in-memory SQLite workspace.</summary>
    public WikiDumpG6OverlayTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "wiki-g6", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        try
        {
            if (Directory.Exists(_workspace))
                Directory.Delete(_workspace, recursive: true);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    /// <summary>TEST-MCP-WIKIEXPORT-003: no dump file without --include-dump on GenerateWikiAsync.</summary>
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
        Assert.Equal(dest, todo.WorkspaceId);
        Assert.DoesNotContain(_workspace, todo.WorkspaceId, StringComparison.OrdinalIgnoreCase);
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

    private WikiDumpService CreateDumpService()
        => new(CreateDb(), TimeProvider.System);

    private RequirementsDocumentService CreateDocService()
    {
        var docs = Path.Combine(_workspace, "docs", "Project");
        Directory.CreateDirectory(docs);
        File.WriteAllText(Path.Combine(docs, "Functional-Requirements.md"), "# Functional Requirements (MCP Server)\n");
        File.WriteAllText(Path.Combine(docs, "Technical-Requirements.md"), "# Technical Requirements (MCP Server)\n");
        File.WriteAllText(Path.Combine(docs, "Testing-Requirements.md"), "# Testing Requirements (MCP Server)\n");
        File.WriteAllText(Path.Combine(docs, "TR-per-FR-Mapping.md"), "# TR per FR Mapping\n");
        File.WriteAllText(Path.Combine(docs, "Requirements-Matrix.md"), "# Requirements Matrix (MCP Server)\n");
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

    private McpDbContext CreateDb()
        => new(_options, new WorkspaceContext { WorkspacePath = _workspace });

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

    private static JsonSerializerOptions Camel()
        => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
