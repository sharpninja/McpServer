using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>FR-MCP-HYGIENE-001: Read-only workspace hygiene validation.</summary>
    [McpServerTool(Name = "workspace_validate"), Description("Run read-only workspace hygiene validation. Never repairs records.")]
    public async Task<string> WorkspaceValidate(
        [Description("Workspace path (required).")] string workspacePath,
        [Description("Optional stale-turn threshold hours.")] double? staleTurnThresholdHours = null,
        CancellationToken cancellationToken = default)
    {
        if (_workspaceValidationService is null)
            return JsonSerializer.Serialize(new { error = "Workspace validation service is not registered." }, s_camelCaseOptions);

        using var workspaceScope = ApplyWorkspaceOverride(workspacePath);
        var result = await _workspaceValidationService.ValidateAsync(new WorkspaceValidationRequest
        {
            Authenticated = true,
            StaleTurnThresholdHours = staleTurnThresholdHours,
        }, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }
}
