using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-005 / AC-FR-MCP-SESSIONLOGCTX-001-006:
/// Backfills None planFile/todoId from turn contents and ~ history.
/// </summary>
public sealed class SessionLogTurnContextBackfill : ISessionLogTurnContextBackfill
{
    private readonly McpDbContext _db;
    private readonly SessionLogTurnContextExtractor _extractor;
    private readonly ILogger<SessionLogTurnContextBackfill> _logger;

    /// <summary>Creates a backfill runner.</summary>
    public SessionLogTurnContextBackfill(
        McpDbContext db,
        SessionLogTurnContextExtractor extractor,
        ILogger<SessionLogTurnContextBackfill> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> RunAsync(CancellationToken cancellationToken = default, string? userProfilePath = null)
    {
        var turns = await _db.SessionLogTurns
            .Include(t => t.Actions)
            .Include(t => t.Tags)
            .Include(t => t.ContextItems)
            .Include(t => t.ProcessingDialog)
            .Include(t => t.StringListItems)
            .Include(t => t.SessionLog)
            .Where(t => t.PlanFile == SessionLogTurnContextValidator.NoneSentinel
                || t.TodoId == SessionLogTurnContextValidator.NoneSentinel)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var changed = 0;
        foreach (var turn in turns)
        {
            var workspace = string.IsNullOrWhiteSpace(turn.WorkspaceId) ? null : turn.WorkspaceId;
            var extracted = _extractor.Extract(
                turn,
                workspace,
                userProfilePath,
                agentSessionId: turn.SessionLog?.AgentSessionId,
                agentSessionTranscriptFile: turn.SessionLog?.AgentSessionTranscriptFile);

            var updated = false;
            if (turn.PlanFile == SessionLogTurnContextValidator.NoneSentinel
                && extracted.PlanFile != SessionLogTurnContextValidator.NoneSentinel)
            {
                turn.PlanFile = extracted.PlanFile;
                updated = true;
            }

            if (turn.TodoId == SessionLogTurnContextValidator.NoneSentinel
                && extracted.TodoId != SessionLogTurnContextValidator.NoneSentinel)
            {
                turn.TodoId = extracted.TodoId;
                updated = true;
            }

            if (updated)
                changed++;
        }

        if (changed > 0)
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Session-log planFile/todoId backfill updated {Count} turns.", changed);
        return changed;
    }
}
