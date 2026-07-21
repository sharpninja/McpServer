using System.ComponentModel;
using System.Text.Json;
using McpServer.Support.Mcp.Services;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.McpStdio;

public sealed partial class FwhMcpTools
{
    /// <summary>FR-MCP-TRIAGE-001: Submit an incidental bug report and return accepted queue state.</summary>
    [McpServerTool(Name = "triage_report"), Description("Submit an incidental bug report for asynchronous triage. Do not use for the user's active requested fix.")]
    public async Task<string> TriageReport(
        [Description("Workspace path for the current task. MCP Server-related bugs may route to the registered McpServer workspace.")] string workspacePath,
        [Description("Bug title.")] string title,
        [Description("Bug summary.")] string summary,
        [Description("Optional component, plugin, command, or subsystem.")] string? component = null,
        [Description("Optional severity: low, medium, high, or critical.")] string? severity = null,
        [Description("Optional stable dedupe key.")] string? dedupeKey = null,
        [Description("Optional error signature.")] string? errorSignature = null,
        [Description("Optional comma-separated affected paths.")] string? affectedPaths = null,
        [Description("Optional reporting agent identity.")] string? reporterAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (_triageService is null)
            return JsonSerializer.Serialize(new { error = "Triage service is not registered." });

        ApplyWorkspaceOverride(workspacePath);
        var request = new TriageReportRequest
        {
            WorkspacePath = workspacePath,
            Title = title,
            Summary = summary,
            Component = component,
            Severity = severity,
            DedupeKey = dedupeKey,
            ErrorSignature = errorSignature,
            AffectedPaths = SplitCsv(affectedPaths),
            ReporterAgent = reporterAgent,
        };

        var result = await _triageService.SubmitReportAsync(request, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(result, s_camelCaseOptions);
    }

    /// <summary>FR-MCP-TRIAGE-002: Get triage report or group status.</summary>
    [McpServerTool(Name = "triage_status"), Description("Get asynchronous triage status by reportId or groupId.")]
    public async Task<string> TriageStatus(
        [Description("Workspace path for the status lookup.")] string workspacePath,
        [Description("Optional report id.")] string? reportId = null,
        [Description("Optional group id.")] string? groupId = null,
        CancellationToken cancellationToken = default)
    {
        if (_triageService is null)
            return JsonSerializer.Serialize(new { error = "Triage service is not registered." });

        ApplyWorkspaceOverride(workspacePath);
        try
        {
            if (!string.IsNullOrWhiteSpace(reportId))
            {
                var report = await _triageService.GetReportAsync(reportId, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(report, s_camelCaseOptions);
            }

            if (!string.IsNullOrWhiteSpace(groupId))
            {
                var group = await _triageService.GetGroupAsync(groupId, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(group, s_camelCaseOptions);
            }

            var groups = await _triageService.QueryGroupsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Serialize(groups, s_camelCaseOptions);
        }
        catch (Exception ex)
        {
            return McpToolErrors.Serialize(ex);
        }
    }

    private static IReadOnlyList<string> SplitCsv(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
