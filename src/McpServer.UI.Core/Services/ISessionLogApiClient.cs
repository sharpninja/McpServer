using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided API client abstraction for querying session logs.
/// </summary>
public interface ISessionLogApiClient
{
    /// <summary>
    /// Queries session logs using the supplied filter and paging parameters.
    /// </summary>
    /// <param name="query">Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session log summaries for the requested page.</returns>
    Task<ListSessionLogsResult> ListSessionLogsAsync(ListSessionLogsQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single session log detail by its session identifier.
    /// </summary>
    /// <param name="sessionId">Session identifier to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session log detail, or <see langword="null"/> when not found.</returns>
    Task<SessionLogDetail?> GetSessionLogAsync(string sessionId, CancellationToken cancellationToken = default);
}
