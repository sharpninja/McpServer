// TR-MCP-REPL-005 / Phase 1d: Session Log MCP tools partial of FwhMcpTools.

using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    // ── GROUP B: Session Log tools ───────────────────────────────────────

    /// <summary>TR-PLANNED-CORE-013: Submit a session log payload.</summary>
    [McpServerTool(Name = "sessionlog_submit"), Description("Submit (upsert) a session log. Body is JSON string conforming to UnifiedSessionLogDto.")]
    public async Task<string> SessionLogSubmit(
        [Description("JSON string of the session log payload")] string json,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var dto = JsonSerializer.Deserialize<UnifiedSessionLogDto>(json, s_caseInsensitiveOptions);
            if (dto == null) return JsonSerializer.Serialize(new { error = "Invalid JSON" });
            var id = await _sessionLogService.SubmitAsync(dto, cancellationToken: cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, id });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Query session logs.</summary>
    [McpServerTool(Name = "sessionlog_query"), Description("Query session logs with optional filters: agent, model, text, from, to, limit.")]
    public async Task<string> SessionLogQuery(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Agent filter (e.g. cursor, copilot)")] string? agent = null,
        [Description("Model filter")] string? model = null,
        [Description("Text search")] string? text = null,
        [Description("From date (ISO 8601)")] string? from = null,
        [Description("To date (ISO 8601)")] string? to = null,
        [Description("Max results (default 100)")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var req = new SessionLogQueryRequest
            {
                Agent = agent,
                Model = model,
                Text = text,
                From = from != null ? DateTimeOffset.Parse(from, System.Globalization.CultureInfo.InvariantCulture) : null,
                To = to != null ? DateTimeOffset.Parse(to, System.Globalization.CultureInfo.InvariantCulture) : null,
                Limit = limit ?? 100
            };
            var result = await _sessionLogService.QueryAsync(req, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { totalCount = result.TotalCount, items = result.Items });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>TR-PLANNED-CORE-013: Append processing dialog items to an existing session log entry.</summary>
    [McpServerTool(Name = "sessionlog_dialog"), Description("Append processing dialog items to a session log entry.")]
    public async Task<string> SessionLogDialog(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("JSON array of dialog items")] string itemsJson,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var items = JsonSerializer.Deserialize<List<ProcessingDialogItemDto>>(itemsJson, s_caseInsensitiveOptions);
            if (items == null || items.Count == 0) return JsonSerializer.Serialize(new { error = "items required" });
            var count = await _sessionLogService.AppendProcessingDialogAsync(agent, sessionId, requestId, items, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, totalDialogItems = count });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>FR-SUPPORT-014: Stateless idempotent ensure-session.</summary>
    [McpServerTool(Name = "sessionlog_open"), Description("Idempotently open (ensure) a session keyed by agent + sessionId. Stateless; safe to call from any process.")]
    public async Task<string> SessionLogOpen(
        [Description("Agent source type (e.g. ClaudeCode)")] string agent,
        [Description("Session id (Agent-yyyyMMddTHHmmssZ-suffix)")] string sessionId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Session title (used only on create)")] string? title = null,
        [Description("Model id (used only on create)")] string? model = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var created = await _sessionLogService.OpenSessionAsync(agent, sessionId, title, model, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, agent, sessionId, created });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>FR-SUPPORT-014: Stateless begin-turn keyed by (agent, sessionId, requestId).</summary>
    [McpServerTool(Name = "sessionlog_begin_turn"), Description("Begin (or re-open) a session turn with status in_progress. Stateless; keyed by agent + sessionId + requestId.")]
    public Task<string> SessionLogBeginTurn(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id (req-yyyyMMddTHHmmssZ-slug)")] string requestId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Short turn title")] string? queryTitle = null,
        [Description("Full user query text")] string? queryText = null,
        CancellationToken cancellationToken = default)
        => UpsertLifecycleTurnToolAsync(agent, sessionId, requestId, workspacePath, "in_progress", turn =>
        {
            turn.QueryTitle = queryTitle;
            turn.QueryText = queryText;
        }, cancellationToken);

    /// <summary>FR-SUPPORT-014: Stateless complete-turn with additive merge.</summary>
    [McpServerTool(Name = "sessionlog_complete_turn"), Description("Complete a session turn. Merges turnJson (UnifiedRequestEntryDto) additively onto the existing turn. The at-least-one design-decision/action/commit compliance gate applies ONLY to the QBAgent ACID source type; standard agents (ClaudeCode, Cursor, Copilot, ...) may complete a turn with no items.")]
    public Task<string> SessionLogCompleteTurn(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional JSON turn payload (UnifiedRequestEntryDto) merged additively")] string? turnJson = null,
        CancellationToken cancellationToken = default)
        => FinalizeLifecycleTurnToolAsync(agent, sessionId, requestId, workspacePath, "completed", turnJson, cancellationToken);

    /// <summary>FR-SUPPORT-014: Stateless fail-turn with additive merge.</summary>
    [McpServerTool(Name = "sessionlog_fail_turn"), Description("Fail a session turn, recording the failure note. Merges turnJson additively; subject to the same QBAgent-only compliance gate as complete (standard agents are not gated).")]
    public Task<string> SessionLogFailTurn(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional JSON turn payload (UnifiedRequestEntryDto) merged additively")] string? turnJson = null,
        CancellationToken cancellationToken = default)
        => FinalizeLifecycleTurnToolAsync(agent, sessionId, requestId, workspacePath, "failed", turnJson, cancellationToken);

    /// <summary>FR-SUPPORT-010G: REPLACE a whole turn (PUT semantics). Omitted fields reset, sections cleared.</summary>
    [McpServerTool(Name = "sessionlog_replace_turn"), Description("REPLACE a session turn (PUT). Omitted scalar fields are reset and every section becomes exactly what turnJson carries (omitted/empty sections are cleared). Use to remove data by re-stating the turn. turnJson is a UnifiedRequestEntryDto.")]
    public async Task<string> SessionLogReplaceTurn(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("JSON turn payload (UnifiedRequestEntryDto) - the complete new turn state")] string turnJson,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var turn = JsonSerializer.Deserialize<UnifiedRequestEntryDto>(turnJson, s_caseInsensitiveOptions) ?? new UnifiedRequestEntryDto();
            turn.RequestId = requestId;
            var turnId = await _sessionLogService.ReplaceTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, turnId, agent, sessionId, requestId, replaced = true });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>FR-SUPPORT-010G: REPLACE one section of a turn (PUT semantics).</summary>
    [McpServerTool(Name = "sessionlog_replace_section"), Description("REPLACE one section of a turn (PUT). section in: actions, tags, context, dialog, commits, designDecisions, requirementsDiscovered, filesModified, blockers. sectionJson is a UnifiedRequestEntryDto whose matching property holds the new contents; an empty/omitted property clears the section. Other sections untouched.")]
    public async Task<string> SessionLogReplaceSection(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Section name (e.g. tags, actions, commits, designDecisions)")] string section,
        [Description("JSON UnifiedRequestEntryDto carrying the section's new contents")] string sectionJson,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var payload = JsonSerializer.Deserialize<UnifiedRequestEntryDto>(sectionJson, s_caseInsensitiveOptions) ?? new UnifiedRequestEntryDto();
            payload.RequestId = requestId;
            var found = await _sessionLogService.ReplaceTurnSectionAsync(agent, sessionId, requestId, section, payload, cancellationToken).ConfigureAwait(false);
            if (found)
                return JsonSerializer.Serialize(new { success = true, agent, sessionId, requestId, section, replaced = true });
            return JsonSerializer.Serialize(new { success = false, error = $"Turn not found: {agent}/{sessionId}/{requestId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>FR-SUPPORT-010G: Clear all items in a turn section (DELETE semantics).</summary>
    [McpServerTool(Name = "sessionlog_clear_section"), Description("Remove ALL items in a turn section. section in: actions, tags, context, dialog, commits, designDecisions, requirementsDiscovered, filesModified, blockers.")]
    public async Task<string> SessionLogClearSection(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Section name")] string section,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var found = await _sessionLogService.ClearTurnSectionAsync(agent, sessionId, requestId, section, cancellationToken).ConfigureAwait(false);
            if (found)
                return JsonSerializer.Serialize(new { success = true, agent, sessionId, requestId, section, cleared = true });
            return JsonSerializer.Serialize(new { success = false, error = $"Turn not found: {agent}/{sessionId}/{requestId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>FR-SUPPORT-010G: Remove a single item from a turn section (DELETE semantics).</summary>
    [McpServerTool(Name = "sessionlog_delete_item"), Description("Remove a single item from a turn section. itemKey is the value for string sections (tags/context/string-lists), the SHA for commits, the Order for actions, and the ordinal for dialog.")]
    public async Task<string> SessionLogDeleteItem(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Section name")] string section,
        [Description("Natural key of the item to remove")] string itemKey,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var found = await _sessionLogService.DeleteTurnItemAsync(agent, sessionId, requestId, section, itemKey, cancellationToken).ConfigureAwait(false);
            if (found)
                return JsonSerializer.Serialize(new { success = true, agent, sessionId, requestId, section, itemKey, deleted = true });
            return JsonSerializer.Serialize(new { success = false, error = $"Item '{itemKey}' not found in section '{section}'." });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>FR-SUPPORT-010G: Delete a single turn and all its children.</summary>
    [McpServerTool(Name = "sessionlog_delete_turn"), Description("Delete a single turn (and all of its child rows). The parent session is preserved.")]
    public async Task<string> SessionLogDeleteTurn(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var found = await _sessionLogService.DeleteTurnAsync(agent, sessionId, requestId, cancellationToken).ConfigureAwait(false);
            if (found)
                return JsonSerializer.Serialize(new { success = true, agent, sessionId, requestId, deleted = true });
            return JsonSerializer.Serialize(new { success = false, error = $"Turn not found: {agent}/{sessionId}/{requestId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>FR-SUPPORT-010G: Delete an entire session and everything beneath it.</summary>
    [McpServerTool(Name = "sessionlog_delete_session"), Description("Delete an entire session and every turn and child row beneath it. Irreversible.")]
    public async Task<string> SessionLogDeleteSession(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var found = await _sessionLogService.DeleteSessionAsync(agent, sessionId, cancellationToken).ConfigureAwait(false);
            if (found)
                return JsonSerializer.Serialize(new { success = true, agent, sessionId, deleted = true });
            return JsonSerializer.Serialize(new { success = false, error = $"Session not found: {agent}/{sessionId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private Task<string> FinalizeLifecycleTurnToolAsync(
        string agent, string sessionId, string requestId, string workspacePath,
        string status, string? turnJson, CancellationToken cancellationToken)
    {
        // TR-MCP-SESSIONLOG-001: parse the optional payload inside a guard so a malformed turnJson
        // returns a structured {error} instead of an uncaught JsonException that the MCP SDK
        // surfaces as the opaque "An error occurred invoking sessionlog_complete_turn".
        UnifiedRequestEntryDto? payload = null;
        if (!string.IsNullOrWhiteSpace(turnJson))
        {
            try
            {
                payload = JsonSerializer.Deserialize<UnifiedRequestEntryDto>(turnJson, s_caseInsensitiveOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError("{ExceptionDetail}", ex.ToString());
                return Task.FromResult(JsonSerializer.Serialize(new { error = ex.Message }));
            }
        }

        return UpsertLifecycleTurnToolAsync(agent, sessionId, requestId, workspacePath, status, turn =>
        {
            if (payload is null)
                return;
            turn.QueryTitle = payload.QueryTitle;
            turn.QueryText = payload.QueryText;
            turn.Response = payload.Response;
            turn.Interpretation = payload.Interpretation;
            turn.FailureNote = payload.FailureNote;
            turn.TokenCount = payload.TokenCount;
            turn.Model = payload.Model;
            turn.Actions = payload.Actions;
            turn.Commits = payload.Commits;
            turn.DesignDecisions = payload.DesignDecisions;
            turn.RequirementsDiscovered = payload.RequirementsDiscovered;
            turn.FilesModified = payload.FilesModified;
            turn.Blockers = payload.Blockers;
            turn.Tags = payload.Tags;
            turn.ContextList = payload.ContextList;
            turn.ProcessingDialog = payload.ProcessingDialog;
        }, cancellationToken);
    }

    private async Task<string> UpsertLifecycleTurnToolAsync(
        string agent, string sessionId, string requestId, string workspacePath,
        string status, Action<UnifiedRequestEntryDto> populate, CancellationToken cancellationToken)
    {
        // TR-MCP-SESSIONLOG-001: ApplyWorkspaceOverride is inside the try so any workspace-resolution
        // failure also returns a structured {error} rather than escaping to the opaque SDK message.
        try
        {
            ApplyWorkspaceOverride(workspacePath);
            var turn = new UnifiedRequestEntryDto
            {
                RequestId = requestId,
                Timestamp = DateTimeOffset.UtcNow.ToString("o"),
                Status = status,
            };
            populate(turn);
            var turnId = await _sessionLogService.UpsertTurnAsync(agent, sessionId, turn, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(new { success = true, turnId, agent, sessionId, requestId, status });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
