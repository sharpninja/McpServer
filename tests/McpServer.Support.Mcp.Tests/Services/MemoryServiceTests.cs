using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-MEMORY-003 and TR-MCP-MEMORY-007 acceptance: the memory service
/// exposes scope-aware CRUD behavior over the EF-backed memory store.
/// </summary>
public sealed class MemoryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly string _workspaceA = Path.Combine(Path.GetTempPath(), "mcp-memory-a");
    private readonly string _workspaceB = Path.Combine(Path.GetTempPath(), "mcp-memory-b");

    /// <summary>Creates an isolated relational schema for each test case.</summary>
    public MemoryServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext(_workspaceA);
        ctx.Database.EnsureCreated();
    }

    /// <summary>Releases the shared in-memory SQLite connection.</summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    /// <summary>
    /// Effective listing returns visible Global memories before memories scoped
    /// to the active workspace, sorts each scope by id, and does not leak another
    /// workspace's rows.
    /// </summary>
    [Fact]
    public async Task ListAsync_ReturnsGlobalThenWorkspace_WithoutCrossWorkspaceLeak()
    {
        await using (var db = CreateContext(_workspaceA))
        {
            var svc = CreateService(db);
            var laterGlobal = await svc.AddAsync(new MemoryAddRequest
            {
                Id = "MEMORY-ZETA-010",
                Category = "zeta",
                Scope = MemoryScope.Global,
                Text = "later global memory",
            }).ConfigureAwait(true);
            var global = await svc.AddAsync(new MemoryAddRequest
            {
                Id = "MEMORY-ALPHA-001",
                Category = "operator",
                Scope = MemoryScope.Global,
                Text = "global memory",
            }).ConfigureAwait(true);
            var laterWorkspace = await svc.AddAsync(new MemoryAddRequest
            {
                Id = "MEMORY-ZETA-011",
                Category = "zeta",
                Scope = MemoryScope.Workspace,
                Text = "later workspace A memory",
            }).ConfigureAwait(true);
            var workspace = await svc.AddAsync(new MemoryAddRequest
            {
                Id = "MEMORY-ALPHA-002",
                Category = "operator",
                Scope = MemoryScope.Workspace,
                Text = "workspace A memory",
            }).ConfigureAwait(true);

            Assert.True(laterGlobal.Success, laterGlobal.Error);
            Assert.True(global.Success, global.Error);
            Assert.True(laterWorkspace.Success, laterWorkspace.Error);
            Assert.True(workspace.Success, workspace.Error);
        }

        await using (var db = CreateContext(_workspaceB))
        {
            var svc = CreateService(db);
            var other = await svc.AddAsync(new MemoryAddRequest
            {
                Category = "operator",
                Scope = MemoryScope.Workspace,
                Text = "workspace B memory",
            }).ConfigureAwait(true);
            Assert.True(other.Success, other.Error);
        }

        await using (var db = CreateContext(_workspaceA))
        {
            var result = await CreateService(db).ListAsync(new MemoryListRequest()).ConfigureAwait(true);

            Assert.Equal(4, result.TotalCount);
            Assert.Equal(MemoryScope.Global, result.Items[0].Scope);
            Assert.Equal("MEMORY-ALPHA-001", result.Items[0].Id);
            Assert.Equal("global memory", result.Items[0].Text);
            Assert.Equal(MemoryScope.Global, result.Items[1].Scope);
            Assert.Equal("MEMORY-ZETA-010", result.Items[1].Id);
            Assert.Equal(MemoryScope.Workspace, result.Items[2].Scope);
            Assert.Equal("MEMORY-ALPHA-002", result.Items[2].Id);
            Assert.Equal("workspace A memory", result.Items[2].Text);
            Assert.Equal(MemoryScope.Workspace, result.Items[3].Scope);
            Assert.Equal("MEMORY-ZETA-011", result.Items[3].Id);
            Assert.DoesNotContain(result.Items, item => item.Text.Contains("workspace B", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Generated memory ids are stable, category-prefixed, and globally
    /// monotonic across Global and Workspace scopes.
    /// </summary>
    [Fact]
    public async Task AddAsync_GeneratesStableCategoryIdsAcrossScopes()
    {
        await using var db = CreateContext(_workspaceA);
        var svc = CreateService(db);

        var first = await svc.AddAsync(new MemoryAddRequest
        {
            Category = "operator notes",
            Scope = MemoryScope.Global,
            Text = "first",
        }).ConfigureAwait(true);
        var second = await svc.AddAsync(new MemoryAddRequest
        {
            Category = "operator notes",
            Scope = MemoryScope.Workspace,
            Text = "second",
        }).ConfigureAwait(true);

        Assert.True(first.Success, first.Error);
        Assert.True(second.Success, second.Error);
        Assert.Equal("MEMORY-OPERATOR-NOTES-001", first.Memory!.Id);
        Assert.Equal("MEMORY-OPERATOR-NOTES-002", second.Memory!.Id);
    }

    /// <summary>
    /// Updating memory text records a new version and preserves the stable id.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ChangesTextAndIncrementsVersion()
    {
        await using var db = CreateContext(_workspaceA);
        var svc = CreateService(db);
        var created = await svc.AddAsync(new MemoryAddRequest
        {
            Category = "operator",
            Scope = MemoryScope.Workspace,
            Text = "before",
        }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var updated = await svc.UpdateAsync(created.Memory!.Id, new MemoryUpdateRequest
        {
            Text = "after",
        }).ConfigureAwait(true);

        Assert.True(updated.Success, updated.Error);
        Assert.Equal(created.Memory.Id, updated.Memory!.Id);
        Assert.Equal("after", updated.Memory.Text);
        Assert.Equal(2, updated.Memory.Version);
    }

    /// <summary>
    /// Duplicate explicit ids are rejected globally, not only within the active
    /// workspace.
    /// </summary>
    [Fact]
    public async Task AddAsync_DuplicateExplicitId_ReturnsConflict()
    {
        await using (var db = CreateContext(_workspaceA))
        {
            var first = await CreateService(db).AddAsync(new MemoryAddRequest
            {
                Id = "MEMORY-CUSTOM-001",
                Category = "custom",
                Scope = MemoryScope.Workspace,
                Text = "first",
            }).ConfigureAwait(true);
            Assert.True(first.Success, first.Error);
        }

        await using (var db = CreateContext(_workspaceB))
        {
            var duplicate = await CreateService(db).AddAsync(new MemoryAddRequest
            {
                Id = "MEMORY-CUSTOM-001",
                Category = "custom",
                Scope = MemoryScope.Workspace,
                Text = "duplicate",
            }).ConfigureAwait(true);

            Assert.False(duplicate.Success);
            Assert.Equal(MemoryMutationFailureKind.Conflict, duplicate.FailureKind);
        }
    }

    /// <summary>
    /// Explicit memory ids must use the canonical MEMORY-{CATEGORY}-{NNN}
    /// shape so callers cannot persist arbitrary identifiers.
    /// </summary>
    [Fact]
    public async Task AddUpdateAndRemoveAsync_InvalidId_ReturnValidation()
    {
        await using var db = CreateContext(_workspaceA);
        var svc = CreateService(db);

        var add = await svc.AddAsync(new MemoryAddRequest
        {
            Id = "not-a-memory-id",
            Category = "operator",
            Scope = MemoryScope.Workspace,
            Text = "invalid",
        }).ConfigureAwait(true);
        var update = await svc.UpdateAsync("not-a-memory-id", new MemoryUpdateRequest { Text = "after" }).ConfigureAwait(true);
        var remove = await svc.RemoveAsync("not-a-memory-id").ConfigureAwait(true);

        Assert.False(add.Success);
        Assert.Equal(MemoryMutationFailureKind.Validation, add.FailureKind);
        Assert.False(update.Success);
        Assert.Equal(MemoryMutationFailureKind.Validation, update.FailureKind);
        Assert.False(remove.Success);
        Assert.Equal(MemoryMutationFailureKind.Validation, remove.FailureKind);
    }

    /// <summary>
    /// Removing a memory uses the shared soft-delete path and hides the row from
    /// subsequent effective reads.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_SoftDeletesMemory()
    {
        string id;
        await using (var db = CreateContext(_workspaceA))
        {
            var created = await CreateService(db).AddAsync(new MemoryAddRequest
            {
                Category = "operator",
                Scope = MemoryScope.Workspace,
                Text = "temporary",
            }).ConfigureAwait(true);
            Assert.True(created.Success, created.Error);
            id = created.Memory!.Id;

            var removed = await CreateService(db).RemoveAsync(id).ConfigureAwait(true);
            Assert.True(removed.Success, removed.Error);
        }

        await using (var db = CreateContext(_workspaceA))
        {
            var list = await CreateService(db).ListAsync(new MemoryListRequest()).ConfigureAwait(true);
            Assert.Empty(list.Items);

            var row = await db.Memories.IgnoreQueryFilters().SingleAsync(memory => memory.Id == id).ConfigureAwait(true);
            Assert.True(db.Entry(row).Property<bool>("IsDeleted").CurrentValue);
        }
    }

    private McpDbContext CreateContext(string? workspacePath)
    {
        return new McpDbContext(
            _options,
            new WorkspaceContext
            {
                WorkspacePath = workspacePath,
            });
    }

    private static MemoryService CreateService(McpDbContext db)
    {
        return new MemoryService(db, NullLogger<MemoryService>.Instance);
    }
}
