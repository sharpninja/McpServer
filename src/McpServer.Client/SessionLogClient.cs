using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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
        return await QueryAsync(
            new SessionLogQueryRequest
            {
                Agent = agent,
                Model = model,
                Text = text,
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
            },
            cancellationToken);
    }

    /// <summary>Query session logs with the full controller filter surface.</summary>
    public async Task<SessionLogQueryResult> QueryAsync(
        SessionLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parts = new List<string>();
        if (request.Agent is not null) parts.Add($"agent={Uri.EscapeDataString(request.Agent)}");
        if (request.AgentDefinitionId is not null) parts.Add($"agentDefinitionId={Uri.EscapeDataString(request.AgentDefinitionId)}");
        if (request.Model is not null) parts.Add($"model={Uri.EscapeDataString(request.Model)}");
        if (request.Text is not null) parts.Add($"text={Uri.EscapeDataString(request.Text)}");
        if (request.From.HasValue) parts.Add($"from={Uri.EscapeDataString(request.From.Value.ToString("o"))}");
        if (request.To.HasValue) parts.Add($"to={Uri.EscapeDataString(request.To.Value.ToString("o"))}");
        if (request.Limit != 100) parts.Add($"limit={request.Limit}");
        if (request.Offset != 0) parts.Add($"offset={request.Offset}");
        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return await GetAsync<SessionLogQueryResult>($"mcpserver/sessionlog{qs}", cancellationToken);
    }


    /// <summary>Ingests a server-local transcript file or folder through the session-log ingestion pipeline.</summary>
    public async Task<TranscriptIngestRunResponse> IngestTranscriptPathAsync(
        TranscriptIngestPathRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<TranscriptIngestRunResponse>("mcpserver/sessionlog/ingest/path", request, cancellationToken);
    }


    /// <summary>Uploads transcript files and ingests them through the session-log ingestion pipeline.</summary>
    public async Task<TranscriptIngestRunResponse> IngestTranscriptUploadAsync(
        TranscriptIngestUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var form = new MultipartFormDataContent();
        AddFormString(form, "agent", request.Agent);
        AddFormString(form, "source", request.Source.ToString());
        AddFormString(form, "recursive", request.Recursive.ToString().ToLowerInvariant());
        AddFormString(form, "strict", request.Strict.ToString().ToLowerInvariant());
        AddFormString(form, "persist", request.Persist.ToString().ToLowerInvariant());
        AddFormString(form, "compatibilityProfile", request.CompatibilityProfile.ToString());
        AddFormString(form, "emitNormalizedProfile", request.EmitNormalizedProfile.ToString().ToLowerInvariant());

        foreach (var file in request.Files)
        {
            if (file is null)
                continue;
            var content = new ByteArrayContent(file.Content ?? []);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType!);
            form.Add(content, "files", file.FileName);
        }

        return await PostContentAsync<TranscriptIngestRunResponse>("mcpserver/sessionlog/ingest/upload", form, cancellationToken);
    }

    private static void AddFormString(MultipartFormDataContent form, string name, string? value)
    {
        form.Add(new StringContent(value ?? string.Empty), name);
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
    /// Repairs session-log child rows whose workspace stamp drifted from their parent session.
    /// </summary>
    /// <param name="dryRun">When true, counts affected rows without persisting changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Repair count and dry-run flag.</returns>
    public async Task<SessionLogWorkspaceStampRepairResult> RepairWorkspaceStampsAsync(
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<SessionLogWorkspaceStampRepairResult>(
            $"mcpserver/sessionlog/repair-workspace-stamps?dryRun={dryRun.ToString().ToLowerInvariant()}",
            null,
            cancellationToken);
    }

    /// <summary>
    /// FR-SUPPORT-014: Idempotent ensure-session keyed by (agent, sessionId).
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
    /// FR-SUPPORT-014: Begins (or re-opens) a turn keyed by
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
    /// FR-SUPPORT-014: Completes the turn, merging <paramref name="payload"/>
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
    /// FR-SUPPORT-014: Fails the turn, recording the failure note. Subject to the
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
