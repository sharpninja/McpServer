using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Service for submitting and querying session logs (MVP-SUPPORT-011).
/// FR-SUPPORT-010: Agents POST session log payloads; clients GET with optional filters.
/// </summary>
public interface ISessionLogService
{
    /// <summary>
    /// TR-PLANNED-013: Submit (upsert) a session log. Inserts or replaces by (SourceType, SessionId).
    /// </summary>
    /// <param name="dto">Unified session log payload conforming to the schema.</param>
    /// <param name="sourceFilePath">Full path to the source JSON file, or null for API submissions.</param>
    /// <param name="contentHash">SHA-256 hash of the source file at time of import, stored for change detection during sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted session log id.</returns>
    Task<long> SubmitAsync(UnifiedSessionLogDto dto, string? sourceFilePath = null, string? contentHash = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-PLANNED-013: Checks whether a session with the given key already exists with the specified content hash.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="contentHash">SHA-256 content hash to compare.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session exists and its stored hash matches; false otherwise.</returns>
    Task<bool> IsUnchangedAsync(string sourceType, string sessionId, string contentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-PLANNED-013: Append one or more processing dialog items to an existing entry.
    /// The AI model calls this on the fly to record reasoning, tool calls, and execution trace.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Request entry identifier within the session.</param>
    /// <param name="items">Dialog items to append (added after existing items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total dialog item count on the entry after appending.</returns>
    Task<int> AppendProcessingDialogAsync(
        string sourceType,
        string sessionId,
        string requestId,
        IReadOnlyList<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-PLANNED-013: Query session logs with optional filters and pagination.
    /// </summary>
    /// <param name="request">Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of session logs with entries.</returns>
    Task<SessionLogQueryResult> QueryAsync(SessionLogQueryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>TR-PLANNED-013: Query parameters for session log search.</summary>
public sealed record SessionLogQueryRequest
{
    /// <summary>Filter by agent source type (e.g. Cursor, Copilot).</summary>
    public string? Agent { get; init; }

    /// <summary>Filter by linked agent definition identifier.</summary>
    public string? AgentDefinitionId { get; init; }

    /// <summary>Filter by AI model (exact or contains match).</summary>
    public string? Model { get; init; }

    /// <summary>Full-text search over QueryText, QueryTitle, Response, Interpretation.</summary>
    public string? Text { get; init; }

    /// <summary>Sessions with Started >= From.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Sessions with LastUpdated &lt;= To.</summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>Page size (default 100, max 1000).</summary>
    public int Limit { get; init; } = 100;

    /// <summary>Number of sessions to skip (default 0).</summary>
    public int Offset { get; init; }
}

/// <summary>TR-PLANNED-013: Paginated result of a session log query.</summary>
public sealed record SessionLogQueryResult
{
    /// <summary>Total number of matching sessions (before pagination).</summary>
    public int TotalCount { get; init; }

    /// <summary>Page size used.</summary>
    public int Limit { get; init; }

    /// <summary>Offset used.</summary>
    public int Offset { get; init; }

    /// <summary>Session log DTOs for the current page.</summary>
    public required IReadOnlyList<UnifiedSessionLogDto> Items { get; init; }
}
