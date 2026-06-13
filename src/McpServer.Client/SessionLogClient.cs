using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for session log endpoints (<c>/mcpserver/sessionlog</c>). Supports submitting (upserting)
/// session logs, querying historical logs with filters, and appending processing dialog items
/// to existing log turns.
/// </summary>
/// <seealso cref="McpServerClient.SessionLog"/>
public sealed class SessionLogClient : McpClientBase
{
    /// <inheritdoc />
    public SessionLogClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal SessionLogClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Submit (upsert) a session log entry. Creates or updates based on session ID.</summary>
    public async Task<SessionLogSubmitResult> SubmitAsync(UnifiedSessionLogDto sessionLog, CancellationToken cancellationToken = default)
    {
        return await PostAsync<SessionLogSubmitResult>("mcpserver/sessionlog", sessionLog, cancellationToken);
    }

    /// <summary>Upsert a single turn on an existing session log.</summary>
    public async Task<SessionLogTurnSubmitResult> UpsertTurnAsync(
        string agent,
        string sessionId,
        UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/turn";
        return await PostAsync<SessionLogTurnSubmitResult>(path, turn, cancellationToken);
    }

    /// <summary>Query session logs with optional filters.</summary>
    public async Task<SessionLogQueryResult> QueryAsync(
        string? agent = null, string? model = null, string? text = null,
        DateTimeOffset? from = null, DateTimeOffset? to = null,
        int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();
        if (agent is not null) parts.Add($"agent={Uri.EscapeDataString(agent)}");
        if (model is not null) parts.Add($"model={Uri.EscapeDataString(model)}");
        if (text is not null) parts.Add($"text={Uri.EscapeDataString(text)}");
        if (from.HasValue) parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to.HasValue) parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        if (limit != 100) parts.Add($"limit={limit}");
        if (offset != 0) parts.Add($"offset={offset}");
        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return await GetAsync<SessionLogQueryResult>($"mcpserver/sessionlog{qs}", cancellationToken);
    }

    /// <summary>Append processing dialog items to a session log turn.</summary>
    public async Task<DialogAppendResult> AppendDialogAsync(
        string agent, string sessionId, string requestId,
        List<ProcessingDialogItemDto> items, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/dialog";
        return await PostAsync<DialogAppendResult>(path, items, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010E: Idempotent ensure-session keyed by (agent, sessionId).
    /// Stateless: callable from any process with no prior session state.
    /// </summary>
    public async Task<SessionLifecycleOpenResult> OpenSessionAsync(
        string agent, string sessionId, string? title = null, string? model = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/open";
        return await PostAsync<SessionLifecycleOpenResult>(path, new { title, model }, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010E: Begins (or re-opens) a turn keyed by
    /// (agent, sessionId, requestId) with status in_progress.
    /// </summary>
    public async Task<SessionLogTurnSubmitResult> BeginTurnAsync(
        string agent, string sessionId, string requestId,
        string? queryTitle = null, string? queryText = null, string? model = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/begin";
        return await PostAsync<SessionLogTurnSubmitResult>(path, new { queryTitle, queryText, model }, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010E: Completes the turn, merging <paramref name="payload"/>
    /// onto the existing turn (omitted fields preserved). Requires at least one
    /// design decision, action, or commit (terminal-turn compliance gate).
    /// </summary>
    public async Task<SessionLogTurnSubmitResult> CompleteTurnAsync(
        string agent, string sessionId, string requestId,
        UnifiedRequestEntryDto? payload = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/complete";
        return await PostAsync<SessionLogTurnSubmitResult>(path, payload ?? new UnifiedRequestEntryDto(), cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010E: Fails the turn, recording the failure note. Subject to the
    /// same terminal-turn compliance gate as complete.
    /// </summary>
    public async Task<SessionLogTurnSubmitResult> FailTurnAsync(
        string agent, string sessionId, string requestId,
        UnifiedRequestEntryDto? payload = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/fail";
        return await PostAsync<SessionLogTurnSubmitResult>(path, payload ?? new UnifiedRequestEntryDto(), cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010G: PATCH a turn - additive merge. Omitted fields preserved,
    /// collection items appended. Explicit verb for the additive submit behavior.
    /// </summary>
    public async Task<SessionLogTurnSubmitResult> PatchTurnAsync(
        string agent, string sessionId, string requestId,
        UnifiedRequestEntryDto turn, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}";
        return await PatchAsync<SessionLogTurnSubmitResult>(path, turn, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010G: PUT a turn - REPLACE. Omitted scalar fields are reset and
    /// every section becomes exactly what the payload carries (omitted/empty
    /// sections cleared). Use to remove data by re-stating the turn.
    /// </summary>
    public async Task<SessionLogMutationResult> ReplaceTurnAsync(
        string agent, string sessionId, string requestId,
        UnifiedRequestEntryDto turn, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}";
        return await PutAsync<SessionLogMutationResult>(path, turn, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010G: PUT a single turn section - REPLACE just that section.
    /// Sections: actions, tags, context, dialog, commits, designDecisions,
    /// requirementsDiscovered, filesModified, blockers. The matching property on
    /// <paramref name="payload"/> becomes the section's new contents; null/empty clears it.
    /// </summary>
    public async Task<SessionLogMutationResult> ReplaceTurnSectionAsync(
        string agent, string sessionId, string requestId, string section,
        UnifiedRequestEntryDto payload, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/sections/{Uri.EscapeDataString(section)}";
        return await PutAsync<SessionLogMutationResult>(path, payload, cancellationToken);
    }

    /// <summary>FR-SUPPORT-010G: DELETE all items in a turn section (clear the section).</summary>
    public async Task<SessionLogMutationResult> ClearTurnSectionAsync(
        string agent, string sessionId, string requestId, string section,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/sections/{Uri.EscapeDataString(section)}";
        return await DeleteAsync<SessionLogMutationResult>(path, cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-010G: DELETE a single item from a turn section. The item key is
    /// the value for string sections (tags/context/string-lists), the SHA for
    /// commits, the Order for actions, and the ordinal for dialog.
    /// </summary>
    public async Task<SessionLogMutationResult> DeleteTurnItemAsync(
        string agent, string sessionId, string requestId, string section, string itemKey,
        CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/sections/{Uri.EscapeDataString(section)}/items/{Uri.EscapeDataString(itemKey)}";
        return await DeleteAsync<SessionLogMutationResult>(path, cancellationToken);
    }

    /// <summary>FR-SUPPORT-010G: DELETE a single turn and all of its child rows.</summary>
    public async Task<SessionLogMutationResult> DeleteTurnAsync(
        string agent, string sessionId, string requestId, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}";
        return await DeleteAsync<SessionLogMutationResult>(path, cancellationToken);
    }

    /// <summary>FR-SUPPORT-010G: DELETE an entire session and every turn beneath it.</summary>
    public async Task<SessionLogMutationResult> DeleteSessionAsync(
        string agent, string sessionId, CancellationToken cancellationToken = default)
    {
        var path = $"mcpserver/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}";
        return await DeleteAsync<SessionLogMutationResult>(path, cancellationToken);
    }
}

