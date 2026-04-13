using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-082: Abstraction for querying and pushing TODO and session log data
/// to a remote federated MCP server target. Implementations use HTTP to call
/// the remote server's REST API. Methods return <c>null</c> on remote failure
/// so decorators can gracefully fall back to local-only results.
/// </summary>
public interface IFederationDataClient
{
    /// <summary>FR-MCP-082: Query TODO items from a remote federation target.</summary>
    /// <param name="target">Resolved federation target with URL and optional API key.</param>
    /// <param name="request">Query parameters to forward to the remote server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Remote query result, or <c>null</c> if the remote call failed.</returns>
    Task<TodoQueryResult?> QueryTodosAsync(FederationTarget target, TodoQueryRequest request, CancellationToken ct = default);

    /// <summary>FR-MCP-082: Get a single TODO item by ID from a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Remote TODO item, or <c>null</c> if not found or remote call failed.</returns>
    Task<TodoFlatItem?> GetTodoByIdAsync(FederationTarget target, string id, CancellationToken ct = default);

    /// <summary>FR-MCP-083: Query session logs from a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="request">Query parameters to forward.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Remote query result, or <c>null</c> if the remote call failed.</returns>
    Task<SessionLogQueryResult?> QuerySessionLogsAsync(FederationTarget target, SessionLogQueryRequest request, CancellationToken ct = default);

    /// <summary>FR-MCP-085: Push TODO items to a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="items">TODO items to push.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating how many items succeeded and any errors.</returns>
    Task<FederationPushResult> PushTodosAsync(FederationTarget target, IReadOnlyList<TodoFlatItem> items, CancellationToken ct = default);

    /// <summary>FR-MCP-085: Push session logs to a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="items">Session log DTOs to push.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating how many items succeeded and any errors.</returns>
    Task<FederationPushResult> PushSessionLogsAsync(FederationTarget target, IReadOnlyList<UnifiedSessionLogDto> items, CancellationToken ct = default);
}
