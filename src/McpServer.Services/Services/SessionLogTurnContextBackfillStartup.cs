using McpServer.Support.Mcp.Storage;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-005: host-startup runner for planFile/todoId backfill.
/// Uses the already-migrated <see cref="McpDbContext"/> and never aborts process startup.
/// </summary>
public static class SessionLogTurnContextBackfillStartup
{
    /// <summary>
    /// Runs one-shot backfill on <paramref name="db"/>. Failures are logged and return 0.
    /// </summary>
    /// <param name="db">The migrated database context used for the rest of host startup.</param>
    /// <param name="extractor">Turn-content extractor.</param>
    /// <param name="logger">Logger for success and swallowed failures.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="userProfilePath">Optional <c>~</c> override for history scans.</param>
    /// <returns>Turns updated, or 0 when backfill throws.</returns>
    public static async Task<int> TryRunAsync(
        McpDbContext db,
        SessionLogTurnContextExtractor extractor,
        ILogger<SessionLogTurnContextBackfill> logger,
        CancellationToken cancellationToken = default,
        string? userProfilePath = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(extractor);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            return await new SessionLogTurnContextBackfill(db, extractor, logger)
                .RunAsync(cancellationToken, userProfilePath)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Session-log planFile/todoId backfill failed; continuing startup.");
            return 0;
        }
    }
}
