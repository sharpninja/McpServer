using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Services.AgentHelp;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>FR-MCP-HELP-006: Create an Agent Help session for MCP Server issue diagnosis.</summary>
    [McpServerTool(Name = "agent_help_create_session"), Description("Create an Agent Help session when stuck on MCP Server surfaces (marker trust, plugins, session log, TODO, triage, etc.).")]
    public async Task<string> AgentHelpCreateSession(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Optional topic label for corpus bootstrap and outcome triage")] string? topic = null,
        [Description("Optional caller agent identity for linkage")] string? callerAgent = null,
        [Description("Optional caller session id for linkage")] string? callerSessionId = null,
        [Description("Optional caller request/turn id for linkage")] string? callerRequestId = null,
        [Description("Optional factual issue summary (observation vs inference separated)")] string? issueSummary = null,
        [Description("Optional device id for session affinity")] string? deviceId = null,
        [Description("Optional execution strategy override")] string? executionStrategy = null,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var request = new AgentHelpSessionCreateRequest
            {
                WorkspacePath = workspacePath,
                Topic = topic,
                DeviceId = deviceId,
                ExecutionStrategy = executionStrategy,
                CallerAgent = callerAgent,
                CallerSessionId = callerSessionId,
                CallerRequestId = callerRequestId,
                IssueSummary = issueSummary,
            };

            var result = await _agentHelpService
                .CreateSessionAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return JsonSerializer.Serialize(result, s_camelCaseOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message }, s_camelCaseOptions);
        }
    }

    /// <summary>FR-MCP-HELP-006: Submit a synchronous Agent Help turn.</summary>
    [McpServerTool(Name = "agent_help_submit_turn"), Description("Submit one Agent Help turn for synchronous processing.")]
    public async Task<string> AgentHelpSubmitTurn(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Agent Help session id returned by agent_help_create_session")] string sessionId,
        [Description("User message for this help turn")] string userMessage,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _agentHelpService
                .SubmitTurnAsync(sessionId, new AgentHelpTurnRequest { UserMessage = userMessage }, cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
                return JsonSerializer.Serialize(new { error = $"Agent Help session '{sessionId}' not found." }, s_camelCaseOptions);

            return JsonSerializer.Serialize(result, s_camelCaseOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message }, s_camelCaseOptions);
        }
    }

    /// <summary>FR-MCP-HELP-006: Get Agent Help session status.</summary>
    [McpServerTool(Name = "agent_help_get_status"), Description("Get current status for an Agent Help session.")]
    public async Task<string> AgentHelpGetStatus(
        [Description("Workspace path (required)")] string workspacePath,
        [Description("Agent Help session id")] string sessionId,
        CancellationToken cancellationToken = default)
    {
        ApplyWorkspaceOverride(workspacePath);
        try
        {
            var result = await _agentHelpService.GetStatusAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (result is null)
                return JsonSerializer.Serialize(new { error = $"Agent Help session '{sessionId}' not found." }, s_camelCaseOptions);

            return JsonSerializer.Serialize(result, s_camelCaseOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return JsonSerializer.Serialize(new { error = ex.Message }, s_camelCaseOptions);
        }
    }
}