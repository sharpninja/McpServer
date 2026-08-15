namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-SESSIONLOGCTX-001 / AC-TR-MCP-SESSIONLOG-006-005:
/// One-shot backfill that upgrades <c>None</c> planFile/todoId columns from turn contents.
/// </summary>
public interface ISessionLogTurnContextBackfill
{
    /// <summary>Upgrades only columns that are still <c>None</c>.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="userProfilePath">Optional fake or real user profile used as <c>~</c> for history scans.</param>
    /// <returns>Number of turns whose columns changed.</returns>
    Task<int> RunAsync(CancellationToken cancellationToken = default, string? userProfilePath = null);
}
