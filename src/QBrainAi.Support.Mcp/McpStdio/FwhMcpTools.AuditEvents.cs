// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using QBrainAi.Support.Mcp.Models;

namespace QBrainAi.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>FR-QB-003/005: Persists one captured event without minting another turn or lifecycle.</summary>
    [McpServerTool(Name = "sessionlog_event_submit"), Description("Commit an immutable captured event to its existing session-log turn; returns the actual event receipt.")]
    public async Task<string> SessionLogEventSubmit(
        [Description("Exact JSON SessionAuditEventRequest")] string eventJson,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
            var auditEvent = JsonSerializer.Deserialize<SessionAuditEventRequest>(eventJson, s_caseInsensitiveOptions)
                ?? throw new ArgumentException("Event body is required.", nameof(eventJson));
            return JsonSerializer.Serialize(await _sessionLogService.AppendAuditEventAsync(auditEvent, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception error)
        {
            _logger.LogError("{ExceptionDetail}", error.ToString());
            return McpToolErrors.Serialize(error);
        }
    }

    /// <summary>TR-QB-AUDIT-001: Independently reads the exact addressed event receipt.</summary>
    [McpServerTool(Name = "sessionlog_event_receipt"), Description("Read the immutable event receipt for an exact existing agent/session/request/event; absent evidence returns null.")]
    public async Task<string> SessionLogEventReceipt(
        [Description("Actual owning agent identity")] string agent,
        [Description("Existing MCP session identifier")] string sessionId,
        [Description("Existing request identifier")] string requestId,
        [Description("Captured event UUID")] string eventId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
            return JsonSerializer.Serialize(await _sessionLogService.ReadAuditEventAsync(
                agent, sessionId, requestId, eventId, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception error)
        {
            _logger.LogError("{ExceptionDetail}", error.ToString());
            return McpToolErrors.Serialize(error);
        }
    }

    /// <summary>AC-QB-009/069: Retained event rejections prevent successful completion.</summary>
    [McpServerTool(Name = "sessionlog_audit_completion"), Description("Check retained rejection evidence before completing the existing turn; does not complete or create a turn.")]
    public async Task<string> SessionLogAuditCompletion(
        [Description("Actual owning agent identity")] string agent,
        [Description("Existing MCP session identifier")] string sessionId,
        [Description("Existing request identifier")] string requestId,
        [Description("Workspace path (required)")] string workspacePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
            return JsonSerializer.Serialize(await _sessionLogService.CheckAuditCompletionAsync(
                agent, sessionId, requestId, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception error)
        {
            _logger.LogError("{ExceptionDetail}", error.ToString());
            return McpToolErrors.Serialize(error);
        }
    }
}
