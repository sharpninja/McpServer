using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// BDP v4 step 3: discriminator contract against WorkspaceIdentity and a real McpDbContext on
/// in-memory SQLite (relational, so query parameters go through the same value conversion as the
/// production providers).
/// </summary>
public sealed class WorkspaceDiscriminatorRealTests : WorkspaceDiscriminatorContractTests
{
    /// <inheritdoc />
    protected override string Canonicalize(string? workspaceId) =>
        WorkspaceIdentity.CanonicalizeWorkspaceId(workspaceId);

    /// <inheritdoc />
    protected override IWorkspaceDiscriminatorProbe CreateProbe(string? contextWorkspacePath)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var db = new McpDbContext(
            new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options,
            new WorkspaceContext { WorkspacePath = contextWorkspacePath });
        db.Database.EnsureCreated();
        return new DbProbe(db);
    }

    private sealed class DbProbe(McpDbContext db) : IWorkspaceDiscriminatorProbe
    {
        public string CurrentWorkspaceId => db.CurrentWorkspaceId;

        public void Override(string workspaceId) => db.OverrideWorkspaceId(workspaceId);

        public IDisposable Push(string workspaceId) => db.PushWorkspaceId(workspaceId);

        public async Task<string> SaveTodoAsync(string id, string explicitWorkspaceId)
        {
            var todo = new TodoItemEntity
            {
                Id = id,
                Title = id,
                Section = "Backlog",
                Priority = "low",
                WorkspaceId = explicitWorkspaceId,
            };
            db.TodoItems.Add(todo);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (await db.TodoItems.IgnoreQueryFilters().AsNoTracking()
                .SingleAsync(row => row.Id == id, TestContext.Current.CancellationToken)).WorkspaceId;
        }

        public async Task RegisterWorkspaceAsync(string workspacePath)
        {
            var now = DateTimeOffset.UtcNow;
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = workspacePath,
                WorkspacePath = workspacePath,
                Name = "registered",
                TodoPath = "docs/todo.yaml",
                IsEnabled = true,
                DateTimeCreated = now,
                DateTimeModified = now,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public Task<bool> WorkspaceExistsByIdAsync(string workspaceId) =>
            db.Workspaces.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(w => w.WorkspaceId == workspaceId, TestContext.Current.CancellationToken);

        public Task<bool> WorkspaceExistsByPathAsync(string workspacePath) =>
            db.Workspaces.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(w => w.WorkspacePath == workspacePath, TestContext.Current.CancellationToken);

        public Task<int> CountTodosAsync(string workspaceId) =>
            db.TodoItems.IgnoreQueryFilters().AsNoTracking()
                .CountAsync(t => t.WorkspaceId == workspaceId, TestContext.Current.CancellationToken);

        public async Task<string> SaveTodoTrackedAsync(string id, string explicitWorkspaceId)
        {
            var todo = new TodoItemEntity
            {
                Id = id,
                Title = id,
                Section = "Backlog",
                Priority = "low",
                WorkspaceId = explicitWorkspaceId,
            };
            db.TodoItems.Add(todo);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (await db.TodoItems.IgnoreQueryFilters()
                .SingleAsync(row => row.Id == id, TestContext.Current.CancellationToken)).WorkspaceId;
        }
    }
}
