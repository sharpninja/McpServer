using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TODO-005 (provider-agnostic): Behavioral tests for <see cref="EfTodoService"/>.
/// Tests run against an in-memory Sqlite provider so the same <see cref="McpDbContext"/>
/// configuration exercises the production relational code path.
/// </summary>
/// <remarks>
/// Byrd Development Process: these tests define the contract EfTodoService must satisfy.
/// The test list covers the acceptance criteria from
/// <c>plan-todo-provider-agnostic-v1.0.md</c> phase 3, plus the markdown-preservation,
/// id-validation, and query cases retired from the legacy provider-specific store.
/// </remarks>
public sealed class EfTodoServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;
    private readonly string _tempRoot;
    private readonly string _tempYamlPath;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly IChangeEventBus _eventBus = Substitute.For<IChangeEventBus>();
    private readonly EfTodoService _sut;

    /// <summary>Builds an isolated in-memory Sqlite EF stack per test instance.</summary>
    public EfTodoServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ef_todo_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempRoot, "docs", "Project"));
        _tempYamlPath = Path.Combine(_tempRoot, "docs", "Project", "TODO.yaml");

        // Keep a single open connection for the fixture lifetime so the in-memory database
        // survives across EF scopes (SQLite drops ":memory:" when the last connection closes).
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        // TR-MCP-TODO-008: WorkspaceContext pinned to the test repo root so the
        // global query filter installed in McpDbContext lets the fixture see its
        // own inserts. Without this, `!IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId`
        // is `false && ...` = false, suppressing every row.
        services.AddScoped(_ => new McpServer.Support.Mcp.Services.WorkspaceContext
        {
            WorkspacePath = _tempRoot,
        });
        services.AddDbContext<McpDbContext>(opts => opts.UseSqlite(_connection));

        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            ctx.Database.EnsureCreated();
        }

        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempRoot,
            TodoFilePath = _tempYamlPath,
        });
        var storageOptions = Microsoft.Extensions.Options.Options.Create(new TodoStorageOptions
        {
            Provider = TodoStorageOptions.DatabaseProvider,
        });

        _sut = new EfTodoService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            ingestionOptions,
            storageOptions,
            _auditLog,
            NullLogger<EfTodoService>.Instance,
            _eventBus);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sut.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.CreateAsync"/> persists a row
    /// through <see cref="McpDbContext"/>, round-trips via
    /// <see cref="EfTodoService.GetByIdAsync"/>, and publishes a <c>todo.created</c>
    /// change event.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PersistsAndRoundTrips()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TODO-001",
            Title = "EF TODO",
            Section = "mvp-support",
            Priority = "high",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        var item = await _sut.GetByIdAsync("EF-TODO-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("EF TODO", item!.Title);
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.UpdateAsync"/> is append-only
    /// in the audit table - one history row per version, monotonic
    /// <c>(TodoId, Version)</c>.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_AppendsAuditRow_MonotonicVersion()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TODO-002",
            Title = "Initial",
            Section = "mvp-support",
            Priority = "low",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sut.UpdateAsync("EF-TODO-002", new TodoUpdateRequest { Title = "Second" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var audit = await _sut.GetAuditAsync("EF-TODO-002", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(audit.TotalCount >= 2);
        Assert.Contains(audit.Entries, e => e.Action == "updated");
    }

    /// <summary>
    /// TEST-MCP-161: Transaction rollback compensation restores EF TODO state
    /// exactly enough to clear values that public update DTOs cannot clear.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_AfterUpdate_RestoresCapturedEntityState()
    {
        var create = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TXN-001",
            Title = "Before",
            Section = "Backlog",
            Priority = "high",
            FunctionalRequirements = ["FR-BEFORE-001"],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(create.Success, create.Error);

        var snapshot = await _sut.CaptureForRestoreAsync("EF-TXN-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(snapshot);

        var update = await _sut.UpdateAsync("EF-TXN-001", new TodoUpdateRequest
        {
            Title = "After",
            Note = "note after",
            Remaining = "remaining after",
            FunctionalRequirements = ["FR-AFTER-001"],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(update.Success, update.Error);

        var restore = await _sut.RestoreAsync(snapshot!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(restore.Success, restore.Error);

        var restored = await _sut.GetByIdAsync("EF-TXN-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("Before", restored!.Title);
        Assert.Null(restored.Note);
        Assert.Null(restored.Remaining);
        Assert.Equal(["FR-BEFORE-001"], restored.FunctionalRequirements);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var links = await db.TodoRequirementLinks
            .OrderBy(row => row.RequirementId)
            .Select(row => row.RequirementId)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(["FR-BEFORE-001"], links);
    }

    /// <summary>
    /// TEST-MCP-161: Store-level compensation restores a soft-deleted EF TODO row.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_AfterDelete_RestoresSoftDeletedTodo()
    {
        var create = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TXN-002",
            Title = "Delete rollback",
            Section = "Backlog",
            Priority = "medium",
            FunctionalRequirements = ["FR-DELETE-BEFORE-001"],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(create.Success, create.Error);

        var snapshot = await _sut.CaptureForRestoreAsync("EF-TXN-002", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(snapshot);

        var delete = await _sut.DeleteAsync("EF-TXN-002", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(delete.Success, delete.Error);
        Assert.Null(await _sut.GetByIdAsync("EF-TXN-002", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var restore = await _sut.RestoreAsync(snapshot!, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(restore.Success, restore.Error);

        var restored = await _sut.GetByIdAsync("EF-TXN-002", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("Delete rollback", restored!.Title);
        Assert.Equal(["FR-DELETE-BEFORE-001"], restored.FunctionalRequirements);
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.QueryAsync"/> honors priority
    /// and keyword filters applied through the relational layer.
    /// </summary>
    [Fact]
    public async Task QueryAsync_FiltersByPriorityAndKeyword()
    {
        var createA = await _sut.CreateAsync(new TodoCreateRequest { Id = "EF-ALPHA-001", Title = "alpha widget", Section = "s", Priority = "high" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(createA.Success, createA.Error);
        var createB = await _sut.CreateAsync(new TodoCreateRequest { Id = "EF-BETA-002", Title = "beta widget", Section = "s", Priority = "low" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(createB.Success, createB.Error);

        var highs = await _sut.QueryAsync(new TodoQueryRequest { Priority = "high" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Single(highs.Items);
        Assert.Equal("EF-ALPHA-001", highs.Items[0].Id);

        var betas = await _sut.QueryAsync(new TodoQueryRequest { Keyword = "beta" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Single(betas.Items);
        Assert.Equal("EF-BETA-002", betas.Items[0].Id);
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.DeleteAsync"/> removes the row
    /// and appends a <c>deleted</c> audit entry, making the TODO unfindable.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_RemovesAndAppendsAudit()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest { Id = "EF-DEL-001", Title = "doomed", Section = "s", Priority = "low" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        var result = await _sut.DeleteAsync("EF-DEL-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(result.Success, result.Error);
        Assert.Null(await _sut.GetByIdAsync("EF-DEL-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var audit = await _sut.GetAuditAsync("EF-DEL-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains(audit.Entries, e => e.Action == "deleted");
    }

    /// <summary>
    /// TR-MCP-TODO-005 / TR-MCP-TODO-006: EF mutations project deterministic TODO.yaml content
    /// and report a consistent projection after create, update, and delete.
    /// </summary>
    [Fact]
    public async Task CreateUpdateDelete_ProjectsYamlAndReportsConsistentStatus()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-PROJ-001",
            Title = "Before projection",
            Section = "mvp-support",
            Priority = "high",
            Description = ["Preserve this line"],
            ImplementationTasks = [new TodoFlatTask("write projection", false)],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(created.Success, created.Error);
        Assert.True(File.Exists(_tempYamlPath));
        Assert.Contains("EF-PROJ-001", File.ReadAllText(_tempYamlPath));

        var createdStatus = await _sut.GetProjectionStatusAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(createdStatus.ProjectionConsistent, createdStatus.Message);
        Assert.False(createdStatus.RepairRequired);

        var updated = await _sut.UpdateAsync("EF-PROJ-001", new TodoUpdateRequest
        {
            Title = "After projection",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(updated.Success, updated.Error);
        Assert.Contains("After projection", File.ReadAllText(_tempYamlPath));

        var deleted = await _sut.DeleteAsync("EF-PROJ-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(deleted.Success, deleted.Error);
        Assert.DoesNotContain("EF-PROJ-001", File.ReadAllText(_tempYamlPath));

        var finalStatus = await _sut.GetProjectionStatusAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(finalStatus.ProjectionConsistent, finalStatus.Message);
        Assert.False(finalStatus.RepairRequired);
    }

    /// <summary>
    /// TR-MCP-TODO-006: EF projection status detects missing projected YAML and asks for repair.
    /// </summary>
    [Fact]
    public async Task GetProjectionStatusAsync_WhenProjectedYamlIsMissing_RequiresRepair()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-MISS-001",
            Title = "Missing projection",
            Section = "mvp-support",
            Priority = "medium",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        File.Delete(_tempYamlPath);

        var status = await _sut.GetProjectionStatusAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(status.ProjectionTargetExists);
        Assert.False(status.ProjectionConsistent);
        Assert.True(status.RepairRequired);
        Assert.Contains("does not exist", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TR-MCP-TODO-006: EF repair rebuilds a drifted TODO.yaml projection from authoritative DB rows.
    /// </summary>
    [Fact]
    public async Task RepairProjectionAsync_AfterProjectionDrift_RebuildsYaml()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-REPAIR-001",
            Title = "Repair target",
            Section = "mvp-support",
            Priority = "low",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        File.WriteAllText(_tempYamlPath, "mvp-support:\n  high-priority: []\n");
        var drifted = await _sut.GetProjectionStatusAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(drifted.ProjectionConsistent);
        Assert.True(drifted.RepairRequired);

        var repair = await _sut.RepairProjectionAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(repair.Success, repair.Error);
        Assert.True(repair.Status.ProjectionConsistent, repair.Status.Message);
        Assert.False(repair.Status.RepairRequired);
        Assert.Contains("EF-REPAIR-001", File.ReadAllText(_tempYamlPath));
    }

    /// <summary>
    /// TEST-MCP-097: EF create returns a projection-failure classification when the DB commit succeeds
    /// but TODO.yaml cannot be written, and operator repair later rebuilds the projection.
    /// </summary>
    [Fact]
    public async Task Create_WhenYamlProjectionFails_ReturnsProjectionFailureButKeepsAuthoritativeState()
    {
        Directory.CreateDirectory(_tempYamlPath);

        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-FAIL-001",
            Title = "Projection failure",
            Section = "mvp-support",
            Priority = "high",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.ProjectionFailed, result.FailureKind);
        Assert.NotNull(await _sut.GetByIdAsync("EF-FAIL-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var failedStatus = await _sut.GetProjectionStatusAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(failedStatus.RepairRequired);
        Assert.NotNull(failedStatus.LastProjectionFailure);

        Directory.Delete(_tempYamlPath);
        var repair = await _sut.RepairProjectionAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(repair.Success, repair.Error);
        Assert.True(repair.Status.ProjectionConsistent, repair.Status.Message);
        Assert.Contains("EF-FAIL-001", File.ReadAllText(_tempYamlPath));
    }

    /// <summary>
    /// TR-MCP-TODO-005: EF projection stores code-review remediation references on the document section
    /// metadata row so the projected YAML preserves the source TODO shape.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CodeReviewPhaseReference_ProjectsCodeReviewSectionReference()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-REVIEW-001",
            Title = "Review phase",
            Section = "code-review-remediation",
            Priority = "high",
            Phase = "pass-1",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var updated = await _sut.UpdateAsync("EF-REVIEW-001", new TodoUpdateRequest
        {
            Reference = "docs/reviews/example.md",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(updated.Success, updated.Error);
        var yaml = File.ReadAllText(_tempYamlPath);
        Assert.Contains("code-review-remediation", yaml);
        Assert.Contains("reference: docs/reviews/example.md", yaml);
        Assert.Contains("phase: pass-1", yaml);
    }

    /// <summary>
    /// A Markdown description (canonical list-of-lines model) exercising every ISS-TODO-001
    /// preservation concern: heading, blank separator lines, a trailing-whitespace line, nested
    /// list indentation, a fenced code block with indented content, and a final line with no
    /// trailing newline. Ported from TodoMarkdownPreservationTests to the EF store.
    /// </summary>
    private static readonly string[] MarkdownLines =
    [
        "# Heading",
        "",
        "Paragraph with **bold** and trailing spaces here:   ",
        "",
        "- list item 1",
        "  - nested indented item",
        "",
        "```csharp",
        "var x = 1;   // keep the indented line below intact",
        "    int y = 2;",
        "```",
        "",
        "Final line without trailing newline",
    ];

    /// <summary>
    /// FR-MCP-108, TR-MCP-TODO-009, TEST-MCP-144: Creating a TODO and reading it back from the
    /// authoritative EF store returns the Markdown description line-for-line, including blank lines,
    /// indentation, and trailing whitespace. JSON storage of the description must not normalize it.
    /// </summary>
    [Fact]
    public async Task CreateThenGetById_PreservesMarkdownDescriptionExactly()
    {
        var create = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MD-PRESERVE-001",
            Title = "Markdown create round-trip",
            Section = "Backlog",
            Priority = "low",
            Description = MarkdownLines,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(create.Success, create.Error);

        var item = await _sut.GetByIdAsync("MD-PRESERVE-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.NotNull(item!.Description);
        Assert.Equal(MarkdownLines, item.Description!);
    }

    /// <summary>
    /// FR-MCP-108, TEST-MCP-144: The append-only audit snapshot captured on create preserves the
    /// Markdown description exactly, so audit history remains a faithful record of formatted content.
    /// </summary>
    [Fact]
    public async Task Audit_PreservesMarkdownDescriptionSnapshot()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MD-PRESERVE-002",
            Title = "Markdown audit snapshot",
            Section = "Backlog",
            Priority = "low",
            Description = MarkdownLines,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var audit = await _sut.GetAuditAsync("MD-PRESERVE-002", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, audit.TotalCount);
        var snapshot = audit.Entries[0].Snapshot;
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot!.Description);
        Assert.Equal(MarkdownLines, snapshot.Description!);
    }

    /// <summary>
    /// FR-MCP-108, TR-MCP-TODO-009, TEST-MCP-144: The Markdown description survives the EF store's
    /// deterministic YAML projection. After a create, the projected TODO.yaml is re-read through
    /// <see cref="TodoYamlFileSerializer"/> and the deserialized description must equal the original
    /// line-for-line, proving projection + deserialization strip no blank lines, indentation, or
    /// trailing content. (Import-from-YAML is TodoBootstrapImporter's responsibility, covered by its
    /// own tests.)
    /// </summary>
    [Fact]
    public async Task YamlProjection_PreservesMarkdownBlankLinesAndIndentation()
    {
        var create = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MD-PRESERVE-003",
            Title = "Markdown projection round-trip",
            Section = "Backlog",
            Priority = "low",
            Description = MarkdownLines,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(create.Success, create.Error);

        var projected = await TodoYamlFileSerializer.ReadIfExistsAsync(_tempYamlPath, CancellationToken.None).ConfigureAwait(true);
        Assert.NotNull(projected);
        Assert.True(projected!.Sections.TryGetValue("Backlog", out var section));
        var item = section!.LowPriority?.FirstOrDefault(i => i.Id == "MD-PRESERVE-003");
        Assert.NotNull(item);
        Assert.NotNull(item!.Description);
        Assert.Equal(MarkdownLines, item.Description!);
    }

    /// <summary>
    /// TEST-MCP-096: Section and done filters are applied against authoritative EF state. One open and
    /// one completed item share a section so the done filter isolates only the completed row.
    /// </summary>
    [Fact]
    public async Task Query_FiltersBySectionAndDone()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-QUERY-003",
            Title = "Open item",
            Section = "mvp-app",
            Priority = "high",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-QUERY-004",
            Title = "Done item",
            Section = "mvp-app",
            Priority = "high",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpdateAsync("EF-QUERY-004", new TodoUpdateRequest { Done = true }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new TodoQueryRequest
        {
            Section = "mvp-app",
            Done = true,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Single(result.Items);
        Assert.Equal("EF-QUERY-004", result.Items[0].Id);
    }

    /// <summary>
    /// TEST-MCP-096: A boolean keyword query matches across an item's searchable fields in the EF store.
    /// </summary>
    [Fact]
    public async Task Query_WithBooleanKeyword_CanMatchAcrossFields()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-QUERY-001",
            Title = "Alpha release",
            Section = "mvp-app",
            Priority = "high",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new TodoQueryRequest
        {
            Keyword = "ef && query",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal("EF-QUERY-001", item.Id);
    }

    /// <summary>
    /// TEST-MCP-096: An invalid persisted TODO id is rejected before any authoritative DB or YAML
    /// mutation occurs. The lowercase id violates the canonical TODO identifier convention.
    /// </summary>
    [Fact]
    public async Task Create_InvalidTodoId_ReturnsValidationError()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "ef-001",
            Title = "Invalid TODO ID",
            Section = "mvp-app",
            Priority = "high",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Validation, result.FailureKind);
        Assert.Contains("Todo id must match", result.Error ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-096: Canonical `ISSUE-{number}` identifiers remain valid in the EF store.
    /// </summary>
    [Fact]
    public async Task Create_ValidIssueNumberId_ReturnsSuccess()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "ISSUE-28",
            Title = "GitHub todo",
            Section = "issues",
            Priority = "medium",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success, result.Error);
        var stored = await _sut.GetByIdAsync("ISSUE-28", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Equal("GitHub todo", stored!.Title);
    }

    /// <summary>
    /// TEST-MCP-096: The persisted TODO ID contract accepts uppercase/digit kebab identifiers with more
    /// than two semantic segments before the numeric suffix.
    /// </summary>
    [Theory]
    [InlineData("PHASE0-REMOTE-001")]
    [InlineData("MCP-TODO-CREATE-001")]
    public async Task Create_ValidUppercaseKebabIdWithDigitsAndExtraSegments_ReturnsSuccess(string id)
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = id,
            Title = "Import-compatible TODO",
            Section = "mvp-app",
            Priority = "medium",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success, result.Error);
        var stored = await _sut.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Equal(id, stored!.Id);
    }

    /// <summary>
    /// TEST-MCP-096: Dependency validation runs in the EF-authoritative path before any authoritative
    /// mutation commits. An invalid dependency id fails the update with a validation classification.
    /// </summary>
    [Fact]
    public async Task Update_InvalidDependsOnId_ReturnsValidationError()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MCP-EF-001",
            Title = "Base",
            Section = "mvp-app",
            Priority = "high",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.UpdateAsync("MCP-EF-001", new TodoUpdateRequest
        {
            DependsOn = ["not-valid"],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Validation, result.FailureKind);
        Assert.Contains("dependsOn contains invalid TODO id", result.Error ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>P1-3: same idempotency key and exact normalized payload heals.</summary>
    [Fact]
    public async Task CreateAsync_SameKeyExactPayload_Heals()
    {
        var request = new TodoCreateRequest
        {
            Id = "MCP-HEAL-001",
            Title = "Heal me",
            Section = "mcp-server",
            Priority = "high",
            Estimate = "2h",
            Description = ["Do the work"],
            TechnicalDetails = ["Use the service"],
            ImplementationTasks = [new TodoFlatTask("Write tests", false)],
            DependsOn = [],
            FunctionalRequirements = [],
            TechnicalRequirements = [],
            IdempotencyKey = "handoff-todo:heal",
        };
        var first = await _sut.CreateAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _sut.CreateAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(first.Success, first.Error);
        Assert.True(second.Success, second.Error);
        Assert.Equal(first.Item!.Id, second.Item!.Id);
    }

    /// <summary>P1-3: same idempotency key with a changed payload is a conflict.</summary>
    [Fact]
    public async Task CreateAsync_SameKeyChangedPayload_Conflicts()
    {
        var first = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MCP-HEAL-002",
            Title = "Original",
            Section = "mcp-server",
            Priority = "high",
            Description = ["one"],
            TechnicalDetails = ["tech"],
            ImplementationTasks = [new TodoFlatTask("task", false)],
            IdempotencyKey = "handoff-todo:collide",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(first.Success, first.Error);

        var second = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MCP-HEAL-002",
            Title = "Changed",
            Section = "mcp-server",
            Priority = "high",
            Description = ["one"],
            TechnicalDetails = ["tech"],
            ImplementationTasks = [new TodoFlatTask("task", false)],
            IdempotencyKey = "handoff-todo:collide",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(second.Success);
        Assert.Equal(TodoMutationFailureKind.Conflict, second.FailureKind);
    }

}
