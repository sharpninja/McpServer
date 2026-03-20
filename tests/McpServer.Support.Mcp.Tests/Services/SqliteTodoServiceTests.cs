using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-096 and TEST-MCP-097: Validates the SQLite-backed TODO store using isolated temp
/// database/YAML fixtures so the tests can verify authoritative DB writes, deterministic YAML
/// projection, YAML bootstrap import, projection-failure surfacing, and append-only audit history.
/// </summary>
public sealed class SqliteTodoServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _tempDbPath;
    private readonly string _tempYamlPath;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly IChangeEventBus _eventBus = Substitute.For<IChangeEventBus>();
    private readonly SqliteTodoService _sut;

    /// <summary>
    /// TEST-MCP-096 and TEST-MCP-097: Creates an isolated database and projected TODO.yaml path per
    /// test instance so storage-side mutations can be asserted without interference from other tests.
    /// </summary>
    public SqliteTodoServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"todo_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempRoot, "docs", "Project"));
        _tempDbPath = Path.Combine(_tempRoot, "todo.db");
        _tempYamlPath = Path.Combine(_tempRoot, "docs", "Project", "TODO.yaml");
        _sut = new SqliteTodoService(_tempDbPath, _tempYamlPath, _auditLog, NullLogger<SqliteTodoService>.Instance, _eventBus);
    }

    /// <summary>
    /// TEST-MCP-096 and TEST-MCP-097: Disposes the store and removes temp artifacts so each test starts
    /// from a clean authoritative DB/YAML pair.
    /// </summary>
    public void Dispose()
    {
        _sut.Dispose();
        TryDelete(_tempRoot);
    }

    /// <summary>
    /// TEST-MCP-096: Verifies that creating a TODO stores the authoritative row in SQLite and projects
    /// the same state into TODO.yaml. The fixture uses a temp DB + YAML path so the test can assert both
    /// stored fields and projected file content without relying on repository state.
    /// </summary>
    [Fact]
    public async Task CreateAndGetById_WorksAndProjectsYaml()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-001",
            Title = "SQLite TODO",
            Section = "mvp-support",
            Priority = "high",
            Note = "sqlite note",
            Remaining = "sqlite remaining",
            Description = ["stored in sqlite"],
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Created
                                     && e.EntityId == "SQL-TODO-001"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);

        var item = await _sut.GetByIdAsync("SQL-TODO-001").ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("SQLite TODO", item.Title);
        Assert.Equal("mvp-support", item.Section);
        Assert.Equal("high", item.Priority);
        Assert.Equal("sqlite note", item.Note);
        Assert.Equal("sqlite remaining", item.Remaining);
        Assert.Equal("stored in sqlite", item.Description![0]);

        var yaml = await File.ReadAllTextAsync(_tempYamlPath).ConfigureAwait(true);
        Assert.Contains("mvp-support:", yaml, StringComparison.Ordinal);
        Assert.Contains("SQL-TODO-001", yaml, StringComparison.Ordinal);
        Assert.Contains("SQLite TODO", yaml, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-097: Verifies that create, update, and delete append ordered audit-history snapshots and
    /// that the projected YAML removes the deleted item after the final mutation. The test uses one TODO id
    /// across all three mutations so versioning and previous-snapshot behavior can be asserted directly.
    /// </summary>
    [Fact]
    public async Task CreateUpdateDelete_RecordsAuditHistoryAndProjectsYaml()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-002",
            Title = "Before",
            Section = "mvp-support",
            Priority = "medium",
        }).ConfigureAwait(true);
        Assert.True(created.Success);

        var updated = await _sut.UpdateAsync("SQL-TODO-002", new TodoUpdateRequest
        {
            Title = "After",
            Done = true,
            Priority = "low",
        }).ConfigureAwait(true);

        Assert.True(updated.Success);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Updated
                                     && e.EntityId == "SQL-TODO-002"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);

        var deleted = await _sut.DeleteAsync("SQL-TODO-002").ConfigureAwait(true);
        Assert.True(deleted.Success);
        Assert.Null(await _sut.GetByIdAsync("SQL-TODO-002").ConfigureAwait(true));
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ChangeEvent>(e => e != null
                                     && e.Category == ChangeEventCategories.Todo
                                     && e.Action == ChangeEventActions.Deleted
                                     && e.EntityId == "SQL-TODO-002"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);

        var audit = await _sut.GetAuditAsync("SQL-TODO-002").ConfigureAwait(true);
        Assert.Equal(3, audit.TotalCount);
        Assert.Collection(
            audit.Entries,
            entry =>
            {
                Assert.Equal(1, entry.Version);
                Assert.Equal("created", entry.Action);
                Assert.Equal("Before", entry.Snapshot?.Title);
                Assert.Null(entry.PreviousSnapshot);
            },
            entry =>
            {
                Assert.Equal(2, entry.Version);
                Assert.Equal("updated", entry.Action);
                Assert.Equal("After", entry.Snapshot?.Title);
                Assert.Equal("Before", entry.PreviousSnapshot?.Title);
            },
            entry =>
            {
                Assert.Equal(3, entry.Version);
                Assert.Equal("deleted", entry.Action);
                Assert.Equal("After", entry.Snapshot?.Title);
                Assert.Equal("After", entry.PreviousSnapshot?.Title);
            });

        var yaml = await File.ReadAllTextAsync(_tempYamlPath).ConfigureAwait(true);
        Assert.DoesNotContain("SQL-TODO-002", yaml, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-096: Verifies that section and done filters are applied against authoritative SQLite state
    /// after projection-aware mutations. The fixture creates one open and one completed item in the same
    /// section so the done filter can isolate only the completed row.
    /// </summary>
    [Fact]
    public async Task Query_Filters_BySectionAndDone()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-003",
            Title = "Open item",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-004",
            Title = "Done item",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);
        await _sut.UpdateAsync("SQL-TODO-004", new TodoUpdateRequest { Done = true }).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new TodoQueryRequest
        {
            Section = "mvp-app",
            Done = true,
        }).ConfigureAwait(true);

        Assert.Single(result.Items);
        Assert.Equal("SQL-TODO-004", result.Items[0].Id);
    }

    /// <summary>
    /// TEST-MCP-096: Verifies that invalid persisted TODO ids are rejected before any authoritative DB or
    /// YAML mutation occurs. The fixture uses a lowercase id specifically because it violates the canonical
    /// TODO identifier convention introduced for server-managed TODOs.
    /// </summary>
    [Fact]
    public async Task Query_WithBooleanKeyword_CanMatchAcrossFields()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-TODO-001",
            Title = "Alpha release",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        var result = await _sut.QueryAsync(new TodoQueryRequest
        {
            Keyword = "sql && todo",
        }).ConfigureAwait(true);

        var item = Assert.Single(result.Items);
        Assert.Equal("SQL-TODO-001", item.Id);
    }

    [Fact]
    public async Task Create_InvalidTodoId_ReturnsValidationError()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "sql-001",
            Title = "Invalid TODO ID",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Validation, result.FailureKind);
        Assert.Contains("Todo id must match", result.Error ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-096: Verifies that canonical `ISSUE-{number}` identifiers remain valid under the new
    /// SQLite-authoritative store. The fixture uses `ISSUE-28` because GitHub-backed TODO ids are an
    /// explicitly supported alternate identifier shape.
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
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        var stored = await _sut.GetByIdAsync("ISSUE-28").ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Equal("GitHub todo", stored.Title);
    }

    /// <summary>
    /// TEST-MCP-096: Verifies that dependency validation still runs in the SQLite-authoritative path before
    /// any authoritative mutation is committed. The fixture uses an invalid dependency id to ensure the
    /// update fails with a validation classification instead of writing inconsistent state.
    /// </summary>
    [Fact]
    public async Task Update_InvalidDependsOnId_ReturnsValidationError()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "MCP-SQL-001",
            Title = "Base",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        var result = await _sut.UpdateAsync("MCP-SQL-001", new TodoUpdateRequest
        {
            DependsOn = ["not-valid"]
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.Validation, result.FailureKind);
        Assert.Contains("dependsOn contains invalid TODO id", result.Error ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-096: Verifies that an empty SQLite store bootstraps from an existing TODO.yaml file, carries
    /// forward document metadata, and reprojects that metadata on the next mutation. The fixture seeds one
    /// normal item, one code-review phase, notes, and a remediation reference so import plus projection can
    /// be verified in a single isolated temp workspace.
    /// </summary>
    [Fact]
    public async Task Initialize_WhenDatabaseEmpty_ImportsYamlAndPreservesMetadataDuringProjection()
    {
        var root = Path.Combine(Path.GetTempPath(), $"todo_import_{Guid.NewGuid():N}");
        var yamlPath = Path.Combine(root, "docs", "Project", "TODO.yaml");
        var dbPath = Path.Combine(root, "mcp.db");
        Directory.CreateDirectory(Path.GetDirectoryName(yamlPath)!);
        await File.WriteAllTextAsync(
            yamlPath,
            """
            mvp-app:
              high-priority:
                - id: BOOT-001
                  title: Bootstrapped item
                  done: false
            notes:
              - bootstrap note
            code-review-remediation:
              reference: docs/reviews/ref.md
              phases:
                - id: REMED-001
                  phase: pass-1
                  title: Fix findings
                  done: false
            """).ConfigureAwait(true);

        var auditLog = Substitute.For<IWriteAuditLog>();
        using var importedStore = new SqliteTodoService(dbPath, yamlPath, auditLog, NullLogger<SqliteTodoService>.Instance);

        var imported = await importedStore.QueryAsync(new TodoQueryRequest()).ConfigureAwait(true);
        Assert.Contains(imported.Items, static item => item.Id == "BOOT-001");
        Assert.Contains(imported.Items, static item => item.Id == "REMED-001" && item.Phase == "pass-1");

        var audit = await importedStore.GetAuditAsync("BOOT-001").ConfigureAwait(true);
        Assert.Equal(1, audit.TotalCount);
        Assert.Equal("imported", audit.Entries[0].Action);
        Assert.Equal("yaml-bootstrap", audit.Entries[0].Source);

        var createResult = await importedStore.CreateAsync(new TodoCreateRequest
        {
            Id = "BOOT-APP-002",
            Title = "Second item",
            Section = "mvp-app",
            Priority = "medium",
        }).ConfigureAwait(true);
        Assert.True(createResult.Success);

        var yaml = await File.ReadAllTextAsync(yamlPath).ConfigureAwait(true);
        Assert.Contains("notes:", yaml, StringComparison.Ordinal);
        Assert.Contains("bootstrap note", yaml, StringComparison.Ordinal);
        Assert.Contains("reference: docs/reviews/ref.md", yaml, StringComparison.Ordinal);
        Assert.Contains("REMED-001", yaml, StringComparison.Ordinal);

        TryDelete(root);
    }

    /// <summary>
    /// TEST-MCP-096 and TEST-MCP-097: Verifies that projection failures are surfaced explicitly while the
    /// authoritative SQLite mutation still commits. The fixture points the projected TODO path at an existing
    /// directory so YAML writing fails deterministically and the test can confirm both failure classification
    /// and retained authoritative state.
    /// </summary>
    [Fact]
    public async Task Create_WhenYamlProjectionFails_ReturnsProjectionFailureButKeepsAuthoritativeState()
    {
        var root = Path.Combine(Path.GetTempPath(), $"todo_projection_fail_{Guid.NewGuid():N}");
        var dbPath = Path.Combine(root, "mcp.db");
        var yamlDirectoryPath = Path.Combine(root, "docs", "Project", "TODO.yaml");
        Directory.CreateDirectory(yamlDirectoryPath);

        var auditLog = Substitute.For<IWriteAuditLog>();
        using var failingStore = new SqliteTodoService(dbPath, yamlDirectoryPath, auditLog, NullLogger<SqliteTodoService>.Instance);

        var result = await failingStore.CreateAsync(new TodoCreateRequest
        {
            Id = "SQL-FAIL-001",
            Title = "Projection failure",
            Section = "mvp-app",
            Priority = "high",
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.ProjectionFailed, result.FailureKind);
        Assert.Contains("authoritative SQLite storage", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var stored = await failingStore.GetByIdAsync("SQL-FAIL-001").ConfigureAwait(true);
        Assert.NotNull(stored);
        Assert.Equal("Projection failure", stored!.Title);

        var audit = await failingStore.GetAuditAsync("SQL-FAIL-001").ConfigureAwait(true);
        Assert.Equal(1, audit.TotalCount);
        Assert.Equal("created", audit.Entries[0].Action);

        TryDelete(root);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
            else if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup for temp test artifacts.
        }
    }
}
