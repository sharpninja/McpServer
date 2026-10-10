using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010-37: Appends a session-log action when a turn is open for the workspace.
/// </summary>
public static class MemorySessionLogHook
{
    /// <summary>Best-effort append of a memory mutation action on the latest open turn.</summary>
    public static async Task TryAppendAsync(
        McpDbContext db,
        string memoryId,
        string actionType,
        CancellationToken cancellationToken)
    {
        var workspaceId = db.CurrentWorkspaceId;
        var turn = await (
                from entry in db.SessionLogTurns.IgnoreQueryFilters()
                join session in db.SessionLogs.IgnoreQueryFilters() on entry.SessionLogId equals session.Id
                where session.WorkspaceId == workspaceId || entry.WorkspaceId == workspaceId
                orderby entry.Id descending
                select entry)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (turn is null)
            return;

        var order = await db.SessionLogActions
            .IgnoreQueryFilters()
            .CountAsync(action => action.SessionLogTurnId == turn.Id, cancellationToken)
            .ConfigureAwait(false);

        db.SessionLogActions.Add(new SessionLogActionEntity
        {
            WorkspaceId = string.IsNullOrEmpty(turn.WorkspaceId) ? workspaceId : turn.WorkspaceId,
            SessionLogTurnId = turn.Id,
            Order = order + 1,
            Type = "memory",
            Status = "completed",
            Description = $"{actionType} {memoryId}",
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
