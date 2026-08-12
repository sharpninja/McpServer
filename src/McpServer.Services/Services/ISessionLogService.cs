using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-CORE-013: Service for submitting and querying session logs (MVP-SUPPORT-011).
/// FR-SUPPORT-010: Agents POST session log payloads; clients GET with optional filters.
/// </summary>
public interface ISessionLogService
{
    /// <summary>
    /// TR-PLANNED-CORE-013: Submit (upsert) a session log. Inserts or replaces by (SourceType, SessionId).
    /// </summary>
    /// <param name="dto">Unified session log payload conforming to the schema.</param>
    /// <param name="sourceFilePath">Full path to the source JSON file, or null for API submissions.</param>
    /// <param name="contentHash">SHA-256 hash of the source file at time of import, stored for change detection during sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted session log id.</returns>
    Task<long> SubmitAsync(UnifiedSessionLogDto dto, string? sourceFilePath = null, string? contentHash = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-PLANNED-CORE-013: Checks whether a session with the given key already exists with the specified content hash.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="contentHash">SHA-256 content hash to compare.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session exists and its stored hash matches; false otherwise.</returns>
    Task<bool> IsUnchangedAsync(string sourceType, string sessionId, string contentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-PLANNED-CORE-013: Append one or more processing dialog items to an existing turn.
    /// The AI model calls this on the fly to record reasoning, tool calls, and execution trace.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Request turn identifier within the session.</param>
    /// <param name="items">Dialog items to append (added after existing items).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total dialog item count on the turn after appending.</returns>
    Task<int> AppendProcessingDialogAsync(
        string sourceType,
        string sessionId,
        string requestId,
        IReadOnlyList<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-PLANNED-CORE-013: Query session logs with optional filters and pagination.
    /// </summary>
    /// <param name="request">Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of session logs with turns.</returns>
    Task<SessionLogQueryResult> QueryAsync(SessionLogQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-013: Fetch a single session log by (sourceType, sessionId) within
    /// the current workspace context. Returns null when the session does not exist
    /// or is filtered out by tenancy.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped session log DTO, or null if not found.</returns>
    Task<UnifiedSessionLogDto?> GetAsync(string sourceType, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-013: Upsert a single turn on an existing session by RequestId.
    /// Does not delete sibling turns.
    /// </summary>
    /// <param name="sourceType">Agent source type of the parent session.</param>
    /// <param name="sessionId">Session identifier of the parent session.</param>
    /// <param name="turn">Turn payload to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted turn entity identifier.</returns>
    /// <exception cref="InvalidOperationException">When the parent session does not exist.</exception>
    Task<long> UpsertTurnAsync(string sourceType, string sessionId, UnifiedRequestEntryDto turn, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-010G: REPLACE a single turn (PUT semantics). Unlike the additive
    /// <see cref="UpsertTurnAsync"/>, omitted scalar fields are reset and every
    /// section collection becomes exactly what the payload carries (omitted or
    /// empty sections are cleared). Use this to remove data by re-stating the turn.
    /// </summary>
    /// <param name="sourceType">Agent source type of the parent session.</param>
    /// <param name="sessionId">Session identifier of the parent session.</param>
    /// <param name="turn">Complete turn representation to replace the existing one with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted turn entity identifier.</returns>
    /// <exception cref="InvalidOperationException">When the parent session does not exist.</exception>
    Task<long> ReplaceTurnAsync(string sourceType, string sessionId, UnifiedRequestEntryDto turn, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-010G: REPLACE a single named section of a turn (PUT semantics).
    /// Sections: actions, tags, context, dialog, commits, designDecisions,
    /// requirementsDiscovered, filesModified, blockers. The matching property on
    /// <paramref name="payload"/> becomes the section's complete new contents; a
    /// null/empty property clears the section. Other sections are untouched.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="section">Section name (case-insensitive).</param>
    /// <param name="payload">Carrier whose matching section property holds the new contents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the turn was found and replaced; false when the turn does not exist.</returns>
    /// <exception cref="ArgumentException">When <paramref name="section"/> is not a known section.</exception>
    Task<bool> ReplaceTurnSectionAsync(string sourceType, string sessionId, string requestId, string section, UnifiedRequestEntryDto payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-010G: Remove every item in a named section of a turn (DELETE
    /// semantics). Equivalent to replacing the section with an empty collection.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="section">Section name (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the turn was found and the section cleared; false when the turn does not exist.</returns>
    /// <exception cref="ArgumentException">When <paramref name="section"/> is not a known section.</exception>
    Task<bool> ClearTurnSectionAsync(string sourceType, string sessionId, string requestId, string section, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-010G: Remove a single item from a section of a turn (DELETE
    /// semantics). The <paramref name="itemKey"/> is matched against the item's
    /// natural identity: the value for string sections (tags/context/string-lists),
    /// the SHA for commits, the Order for actions, and the ordinal for dialog.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="section">Section name (case-insensitive).</param>
    /// <param name="itemKey">Natural key of the item to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when a matching item was found and removed; false otherwise.</returns>
    /// <exception cref="ArgumentException">When <paramref name="section"/> is not a known section.</exception>
    Task<bool> DeleteTurnItemAsync(string sourceType, string sessionId, string requestId, string section, string itemKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-010G: Delete a single turn and all of its child rows from a
    /// session. The parent session is left in place.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the turn existed and was deleted; false when not found.</returns>
    Task<bool> DeleteTurnAsync(string sourceType, string sessionId, string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-010G: Delete a session and every turn and child row beneath it.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the session existed and was deleted; false when not found.</returns>
    Task<bool> DeleteSessionAsync(string sourceType, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-SUPPORT-014: Idempotent ensure-session keyed by (sourceType, sessionId)
    /// within the current workspace. Creates the session with status in_progress
    /// when missing; otherwise leaves the existing session untouched.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="title">Optional title for a newly created session.</param>
    /// <param name="model">Optional model for a newly created session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the session was created; false when it already existed.</returns>
    Task<bool> OpenSessionAsync(string sourceType, string sessionId, string? title = null, string? model = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-MCP-SESSIONLOG-005: Explicitly set the title of an existing session.
    /// This is the dedicated session-retitle path; the whole-session additive
    /// submit (<see cref="SubmitAsync"/>) never re-titles a session it did not
    /// create when the plugin omits the title, so an agent uses this to durably
    /// rename a session without a stale re-submit clobbering it.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="title">New session title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted session id.</returns>
    /// <exception cref="InvalidOperationException">When the session does not exist.</exception>
    Task<long> SetSessionTitleAsync(string sourceType, string sessionId, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// TR-MCP-SESSIONLOG-005: Explicitly set the QueryTitle of an existing turn.
    /// This is the dedicated turn-retitle path; an agent uses it to durably refine
    /// a turn's title without a full <see cref="ReplaceTurnAsync"/> that would
    /// reset omitted scalar fields and clear collections.
    /// </summary>
    /// <param name="sourceType">Agent source type.</param>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="requestId">Turn request identifier.</param>
    /// <param name="title">New turn title.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted turn id.</returns>
    /// <exception cref="InvalidOperationException">When the session or turn does not exist.</exception>
    Task<long> SetTurnTitleAsync(string sourceType, string sessionId, string requestId, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// BUG-SESSIONLOG-WS-005: Re-stamps session-log child rows (turns and their
    /// children) whose WorkspaceId drifted away from their parent session's
    /// WorkspaceId. Idempotent data repair for stamping inconsistencies introduced
    /// before the parent-inheritance invariant was enforced.
    /// </summary>
    /// <param name="dryRun">When true, counts drifted rows without persisting changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of rows re-stamped (or that would be re-stamped when <paramref name="dryRun"/> is true).</returns>
    Task<int> RepairWorkspaceStampsAsync(bool dryRun = false, CancellationToken cancellationToken = default);
}

/// <summary>TR-PLANNED-CORE-013: Query parameters for session log search.</summary>
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

    /// <summary>FR-MCP-SESSIONLOGCTX-001: Exact filter on turn planFile after normalize/expand.</summary>
    public string? PlanFile { get; init; }

    /// <summary>FR-MCP-SESSIONLOGCTX-001: Exact filter on turn todoId.</summary>
    public string? TodoId { get; init; }
}

/// <summary>TR-PLANNED-CORE-013: Paginated result of a session log query.</summary>
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
