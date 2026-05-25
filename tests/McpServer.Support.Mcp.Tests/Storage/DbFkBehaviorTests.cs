using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-139: Behavioral DB-FK-001 tests for generic audit ledger rows and
/// soft-delete persistence.
/// </summary>
public sealed class DbFkBehaviorTests
{
    /// <summary>
    /// TEST-MCP-139: Create, update, and delete operations on mutable durable
    /// entities write generic audit ledger rows and convert delete to soft delete.
    /// </summary>
    [Fact]
    public async Task DataAuditLog_SaveChanges_CreateUpdateSoftDelete_WritesRows()
    {
        await using var db = CreateContext();
        db.OverrideWorkspaceId("F:\\Workspaces\\DbFkAudit");
        var todo = new TodoItemEntity
        {
            Id = "DBFK-AUDIT-001",
            Title = "Audit me",
            Section = "Backlog",
            Priority = "high",
        };

        db.TodoItems.Add(todo);
        await db.SaveChangesAsync().ConfigureAwait(true);
        todo.Title = "Audit me again";
        await db.SaveChangesAsync().ConfigureAwait(true);
        db.TodoItems.Remove(todo);
        await db.SaveChangesAsync().ConfigureAwait(true);

        var actions = await db.DataAuditLogs
            .AsNoTracking()
            .Where(row => row.EntityKind == nameof(TodoItemEntity) && row.EntityKey.Contains("DBFK-AUDIT-001", StringComparison.Ordinal))
            .OrderBy(row => row.OccurredAtUtc)
            .Select(row => row.Action)
            .ToListAsync()
            .ConfigureAwait(true);

        Assert.Contains("create", actions);
        Assert.Contains("update", actions);
        Assert.Contains("delete", actions);

        var stored = await db.TodoItems
            .IgnoreQueryFilters()
            .SingleAsync(row => row.Id == "DBFK-AUDIT-001")
            .ConfigureAwait(true);
        Assert.True((bool)db.Entry(stored).Property("IsDeleted").CurrentValue!);
        Assert.NotNull(db.Entry(stored).Property("DeletedAtUtc").CurrentValue);
        Assert.Equal(nameof(McpDbContext), db.Entry(stored).Property("DeletedBy").CurrentValue);
    }

    /// <summary>
    /// TEST-MCP-139: Audit ledger rows are append-only audit data and must not
    /// recursively audit themselves.
    /// </summary>
    [Fact]
    public async Task DataAuditLog_SaveChanges_DoesNotAuditDataAuditLogRows()
    {
        await using var db = CreateContext();

        db.DataAuditLogs.Add(new DataAuditLogEntity
        {
            WorkspaceId = string.Empty,
            EntityKind = "manual",
            EntityKey = "manual-1",
            Action = "manual",
            Actor = "test",
            SourceType = "test",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync().ConfigureAwait(true);

        var count = await db.DataAuditLogs.CountAsync().ConfigureAwait(true);
        Assert.Equal(1, count);
    }

    /// <summary>
    /// TEST-MCP-139: Append-only audit ledger rows are durable records, so EF
    /// tracked delete calls against them must be rejected instead of physically
    /// removing audit evidence.
    /// </summary>
    [Fact]
    public async Task DataAuditLog_SaveChanges_DeleteAuditRow_Throws()
    {
        await using var db = CreateContext();

        var audit = new DataAuditLogEntity
        {
            WorkspaceId = string.Empty,
            EntityKind = "manual",
            EntityKey = "manual-2",
            Action = "manual",
            Actor = "test",
            SourceType = "test",
            OccurredAtUtc = DateTimeOffset.UtcNow,
        };
        db.DataAuditLogs.Add(audit);
        await db.SaveChangesAsync().ConfigureAwait(true);

        db.DataAuditLogs.Remove(audit);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync()).ConfigureAwait(true);
        Assert.Contains("Physical deletes are blocked", ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DataAuditLogEntity), ex.Message, StringComparison.Ordinal);
    }

    private static McpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"dbfk-behavior-{Guid.NewGuid():N}")
            .Options;

        return new McpDbContext(options);
    }
}
