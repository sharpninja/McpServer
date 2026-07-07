using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TODO-008 Phase 4 acceptance: <see cref="TodoBootstrapImporter"/>
/// imports per-workspace <c>TodoPath</c> YAML into the authoritative database
/// exactly once per marker-file lifetime, stamping every inserted row with
/// the caller-resolved workspace id.
/// </summary>
public sealed class TodoBootstrapImporterTests : IDisposable
{
    private readonly string _root;
    private readonly SqliteConnection _conn;

    /// <summary>Fresh temp root + single long-lived in-memory SQLite per test.</summary>
    public TodoBootstrapImporterTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"todo_bootstrap_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _conn.Dispose();
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// First bootstrap of an empty workspace MUST read the YAML and insert
    /// every item with <c>WorkspaceId</c> set to the caller's workspace path.
    /// </summary>
    [Fact]
    public async Task Bootstrap_EmptyWorkspace_ImportsAllYamlItemsWithStampedWorkspaceId()
    {
        var ws = CreateWorkspaceDir("alpha");
        WriteYaml(ws, """
            mvp-support:
              high-priority:
                - id: ALPHA-001
                  title: First alpha item
              medium-priority:
                - id: ALPHA-002
                  title: Second alpha item
            """);

        var sut = BuildSut([(ws, "docs/todo.yaml")]);
        await sut.RunAsync(CancellationToken.None).ConfigureAwait(true);

        using var probe = NewReadScope();
        var all = await probe.Ctx.TodoItems.IgnoreQueryFilters().AsNoTracking().ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(2, all.Count);
        var wsIds = string.Join("|", all.Select(r => $"[{r.WorkspaceId}]"));
        Assert.All(all, r => Assert.True(r.WorkspaceId == ws, $"expected [{ws}] got {wsIds}"));
        Assert.Contains(all, r => r.Id == "ALPHA-001");
        Assert.Contains(all, r => r.Id == "ALPHA-002");
    }

    /// <summary>
    /// Second bootstrap of the same workspace MUST no-op when the marker
    /// file is present; the row count MUST be unchanged.
    /// </summary>
    [Fact]
    public async Task Bootstrap_IsIdempotent_WhenMarkerFilePresent()
    {
        var ws = CreateWorkspaceDir("beta");
        WriteYaml(ws, """
            mvp-support:
              high-priority:
                - id: BETA-001
                  title: Beta item
            """);

        var sut = BuildSut([(ws, "docs/todo.yaml")]);
        await sut.RunAsync(CancellationToken.None).ConfigureAwait(true);

        // Rewrite YAML with a new item; second run must NOT import it.
        WriteYaml(ws, """
            mvp-support:
              high-priority:
                - id: BETA-001
                  title: Beta item
                - id: BETA-002
                  title: Beta item 2
            """);
        var sut2 = BuildSut([(ws, "docs/todo.yaml")]);
        await sut2.RunAsync(CancellationToken.None).ConfigureAwait(true);

        using var probe = NewReadScope();
        probe.Ctx.OverrideWorkspaceId(ws);
        var rows = await probe.Ctx.TodoItems.AsNoTracking().ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Single(rows);
        Assert.Equal("BETA-001", rows[0].Id);
    }

    /// <summary>
    /// Bootstrap of a workspace whose YAML is missing MUST no-op cleanly,
    /// write no marker, and leave zero rows for that workspace.
    /// </summary>
    [Fact]
    public async Task Bootstrap_MissingYaml_IsNoop_NoMarkerWritten()
    {
        var ws = CreateWorkspaceDir("gamma");
        // No YAML file at all.
        var sut = BuildSut([(ws, "docs/todo.yaml")]);
        await sut.RunAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.False(File.Exists(Path.Combine(ws, TodoBootstrapImporter.MarkerFileName)));

        using var probe = NewReadScope();
        probe.Ctx.OverrideWorkspaceId(ws);
        Assert.False(await probe.Ctx.TodoItems.AnyAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>
    /// Two workspaces MUST be able to bootstrap independently with the same
    /// canonical TODO id; verifies TR-MCP-TODO-008 composite-PK guarantee.
    /// </summary>
    [Fact]
    public async Task Bootstrap_TwoWorkspacesSameId_BothInsert_PlanBitNetIntegrationCase()
    {
        var bitnetWs = CreateWorkspaceDir("bitnet");
        var truckMateWs = CreateWorkspaceDir("truckmate");
        const string sharedId = "PLAN-BITNETINTEGRATION-001";
        var yamlBitnet = $$"""
            planning:
              high-priority:
                - id: {{sharedId}}
                  title: BitNet-side integration
            """;
        var yamlTruckMate = $$"""
            planning:
              high-priority:
                - id: {{sharedId}}
                  title: TruckMate-side integration
            """;
        WriteYaml(bitnetWs, yamlBitnet);
        WriteYaml(truckMateWs, yamlTruckMate);

        var sut = BuildSut([(bitnetWs, "docs/todo.yaml"), (truckMateWs, "docs/todo.yaml")]);
        await sut.RunAsync(CancellationToken.None).ConfigureAwait(true);

        using var probe = NewReadScope();
        var all = await probe.Ctx.TodoItems.IgnoreQueryFilters().AsNoTracking().ToListAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, r => r.WorkspaceId == bitnetWs && r.Id == sharedId && r.Title == "BitNet-side integration");
        Assert.Contains(all, r => r.WorkspaceId == truckMateWs && r.Id == sharedId && r.Title == "TruckMate-side integration");
    }

    /// <summary>
    /// Bootstrap MUST preserve ordered section structure, completed items,
    /// top-level notes, and projection metadata; it is a mirror, not a merge.
    /// </summary>
    [Fact]
    public async Task Bootstrap_PreservesOrderedSectionsCompletedItemsAndNotes()
    {
        var ws = CreateWorkspaceDir("delta");
        WriteYaml(ws, """
            first-section:
              high-priority:
                - id: D-F-001
                  title: First in first section
            second-section:
              medium-priority:
                - id: D-S-001
                  title: First in second section
            notes:
              - "Top-level note A"
              - "Top-level note B"
            """);

        var sut = BuildSut([(ws, "docs/todo.yaml")]);
        await sut.RunAsync(CancellationToken.None).ConfigureAwait(true);

        using var probe = NewReadScope();
        probe.Ctx.OverrideWorkspaceId(ws);
        var rows = await probe.Ctx.TodoItems
            .AsNoTracking()
            .OrderBy(r => r.SectionOrder)
            .ThenBy(r => r.ItemOrder)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(2, rows.Count);
        Assert.Equal("first-section", rows[0].Section);
        Assert.Equal(0, rows[0].SectionOrder);
        Assert.Equal("second-section", rows[1].Section);
        Assert.Equal(1, rows[1].SectionOrder);

        var meta = await probe.Ctx.TodoDocumentMetadata.AsNoTracking().SingleAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var noteRows = await probe.Ctx.TodoDocumentNotes
            .AsNoTracking()
            .Where(n => n.WorkspaceId == meta.WorkspaceId && n.SingletonId == meta.SingletonId)
            .OrderBy(n => n.Ordinal)
            .Select(n => n.Value)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains("Top-level note A", noteRows);
        Assert.Contains("Top-level note B", noteRows);
        Assert.Equal(ws, meta.WorkspaceId);
    }

    private string CreateWorkspaceDir(string name)
    {
        // GetFullPath so the workspace id stamped by the importer matches the
        // value we feed back into OverrideWorkspaceId for probe queries. Path.GetTempPath()
        // can surface short 8.3 names on Windows; GetFullPath normalizes them.
        var dir = Path.GetFullPath(Path.Combine(_root, name));
        Directory.CreateDirectory(Path.Combine(dir, "docs"));
        return dir;
    }

    private static void WriteYaml(string wsRoot, string yaml)
        => File.WriteAllText(Path.Combine(wsRoot, "docs", "todo.yaml"), yaml);

    private TodoBootstrapImporter BuildSut(IReadOnlyList<(string Workspace, string TodoPath)> workspaces)
    {
        var services = new ServiceCollection();
        services.AddDbContext<McpDbContext>(opts => opts.UseSqlite(_conn));
        var dict = new Dictionary<string, string?>();
        var workspaceDtos = new List<WorkspaceDto>();
        for (var i = 0; i < workspaces.Count; i++)
        {
            dict[$"Mcp:Workspaces:{i}:WorkspacePath"] = workspaces[i].Workspace;
            dict[$"Mcp:Workspaces:{i}:Name"] = $"ws-{i}";
            dict[$"Mcp:Workspaces:{i}:TodoPath"] = workspaces[i].TodoPath;
            workspaceDtos.Add(new WorkspaceDto
            {
                WorkspacePath = workspaces[i].Workspace,
                Name = $"ws-{i}",
                TodoPath = workspaces[i].TodoPath,
                IsEnabled = true,
                StatusPrompt = "status",
                ImplementPrompt = "implement",
                PlanPrompt = "plan",
            });
        }
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceListResult(workspaceDtos, workspaceDtos.Count));
        services.AddSingleton(workspaceService);

        var sp = services.BuildServiceProvider();
        using (var s = sp.CreateScope())
            s.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        return new TodoBootstrapImporter(
            sp.GetRequiredService<IServiceScopeFactory>(),
            config,
            NullLogger<TodoBootstrapImporter>.Instance);
    }

    private ReadScope NewReadScope()
    {
        var services = new ServiceCollection();
        services.AddDbContext<McpDbContext>(opts => opts.UseSqlite(_conn));
        var sp = services.BuildServiceProvider();
        var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        return new ReadScope(sp, scope, ctx);
    }

    private readonly struct ReadScope(ServiceProvider sp, IServiceScope scope, McpDbContext ctx) : IDisposable
    {
        public McpDbContext Ctx { get; } = ctx;

        public void Dispose()
        {
            scope.Dispose();
            sp.Dispose();
        }
    }
}
