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

    /// <summary>TR-PLANNED-013: Submit a session log payload.</summary>
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

    /// <summary>TR-PLANNED-013: Query session logs.</summary>
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

    /// <summary>TR-PLANNED-013: Append processing dialog items to an existing session log entry.</summary>
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

    /// <summary>FR-SUPPORT-010E: Stateless idempotent ensure-session.</summary>
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

    /// <summary>FR-SUPPORT-010E: Stateless begin-turn keyed by (agent, sessionId, requestId).</summary>
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

    /// <summary>FR-SUPPORT-010E: Stateless complete-turn with additive merge.</summary>
    [McpServerTool(Name = "sessionlog_complete_turn"), Description("Complete a session turn. Merges turnJson (UnifiedRequestEntryDto) additively onto the existing turn; requires at least one design decision, action, or commit.")]
    public Task<string> SessionLogCompleteTurn(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional JSON turn payload (UnifiedRequestEntryDto) merged additively")] string? turnJson = null,
        CancellationToken cancellationToken = default)
        => FinalizeLifecycleTurnToolAsync(agent, sessionId, requestId, workspacePath, "completed", turnJson, cancellationToken);

    /// <summary>FR-SUPPORT-010E: Stateless fail-turn with additive merge.</summary>
    [McpServerTool(Name = "sessionlog_fail_turn"), Description("Fail a session turn, recording the failure note. Merges turnJson additively; subject to the same compliance gate as complete.")]
    public Task<string> SessionLogFailTurn(
        [Description("Agent source type")] string agent,
        [Description("Session id")] string sessionId,
        [Description("Request id")] string requestId,
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional JSON turn payload (UnifiedRequestEntryDto) merged additively")] string? turnJson = null,
        CancellationToken cancellationToken = default)
        => FinalizeLifecycleTurnToolAsync(agent, sessionId, requestId, workspacePath, "failed", turnJson, cancellationToken);

    private Task<string> FinalizeLifecycleTurnToolAsync(
        string agent, string sessionId, string requestId, string workspacePath,
        string status, string? turnJson, CancellationToken cancellationToken)
    {
        UnifiedRequestEntryDto? payload = null;
        if (!string.IsNullOrWhiteSpace(turnJson))
        {
            payload = JsonSerializer.Deserialize<UnifiedRequestEntryDto>(turnJson, s_caseInsensitiveOptions);
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
        ApplyWorkspaceOverride(workspacePath);
        try
        {
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
