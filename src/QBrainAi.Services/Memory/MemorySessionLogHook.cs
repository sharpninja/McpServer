using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010-37: Appends a session-log action when a turn is open for the workspace.
/// FR-MCP-MEMORY-007 criterion 6 (PLAN-SESSIONTRIAGEFIX-001 D6): the action uses type edit, as
/// docs/context/memory.md specifies, and takes the next free order on its turn.
/// </summary>
public static class MemorySessionLogHook
{
    /// <summary>
    /// Best-effort append of a memory mutation action on the latest open turn. The action takes one more than
    /// the highest order the turn has recorded, deleted actions included, so it repeats no order; 1 on a turn
    /// without actions.
    /// </summary>
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

        var highestOrder = await db.SessionLogActions
            .IgnoreQueryFilters()
            .Where(action => action.SessionLogTurnId == turn.Id)
            .MaxAsync(action => (int?)action.Order, cancellationToken)
            .ConfigureAwait(false);

        db.SessionLogActions.Add(new SessionLogActionEntity
        {
            WorkspaceId = string.IsNullOrEmpty(turn.WorkspaceId) ? workspaceId : turn.WorkspaceId,
            SessionLogTurnId = turn.Id,
            Order = (highestOrder ?? 0) + 1,
            Type = "edit",
            Status = "completed",
            Description = $"{actionType} {memoryId}",
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
