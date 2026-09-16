using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>FR-MCP-HOSTILEREVIEW-001: Submit a bounded hostile-review request.</summary>
    [McpServerTool(Name = "hostile_review_submit"), Description("Submit a bounded hostile review request. Queue-and-record only.")]
    public async Task<string> HostileReviewSubmit(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Target type.")] string targetType,
        [Description("Review mode.")] string mode,
        [Description("Scope statement.")] string scopeStatement,
        [Description("Requester identity.")] string requestingAgent,
        CancellationToken cancellationToken = default)
    {
        if (_hostileReviewService is null)
            return JsonSerializer.Serialize(new { error = "Hostile review service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _hostileReviewService.SubmitAsync(new HostileReviewSubmitRequest
        {
            TargetType = targetType,
            Mode = mode,
            ScopeStatement = scopeStatement,
            RequestingAgent = requestingAgent,
            WorkspacePath = workspacePath,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }

    /// <summary>FR-MCP-HOSTILEREVIEW-006: Read-only status.</summary>
    [McpServerTool(Name = "hostile_review_status"), Description("Read-only hostile review status. Does not mutate state.")]
    public async Task<string> HostileReviewStatus(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Request id.")] string requestId,
        CancellationToken cancellationToken = default)
    {
        if (_hostileReviewService is null)
            return JsonSerializer.Serialize(new { error = "Hostile review service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _hostileReviewService.StatusAsync(requestId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }

    /// <summary>FR-MCP-HOSTILEREVIEW-004: Read-only get.</summary>
    [McpServerTool(Name = "hostile_review_get"), Description("Get normalized hostile review results. Read-only.")]
    public async Task<string> HostileReviewGet(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Request id.")] string requestId,
        CancellationToken cancellationToken = default)
    {
        if (_hostileReviewService is null)
            return JsonSerializer.Serialize(new { error = "Hostile review service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _hostileReviewService.GetAsync(requestId, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }

    /// <summary>FR-MCP-HOSTILEREVIEW-005: AND-filtered query.</summary>
    [McpServerTool(Name = "hostile_review_query"), Description("Query hostile review runs with AND filters.")]
    public async Task<string> HostileReviewQuery(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Optional model filter.")] string? model = null,
        [Description("Optional effort filter.")] string? effort = null,
        CancellationToken cancellationToken = default)
    {
        if (_hostileReviewService is null)
            return JsonSerializer.Serialize(new { error = "Hostile review service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _hostileReviewService.QueryAsync(new HostileReviewQueryRequest
        {
            Model = model,
            Effort = effort,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }
}
