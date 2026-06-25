using System.Net.Http;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// FR-MCP-TRIAGE-001..003: Client for incidental bug triage endpoints.
/// </summary>
public sealed class TriageClient : McpClientBase
{
    /// <inheritdoc />
    public TriageClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal TriageClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Submits a triage report and returns accepted queue state immediately.</summary>
    public Task<TriageReportSubmitResult> SubmitReportAsync(
        TriageReportRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<TriageReportSubmitResult>("mcpserver/triage/reports", request, cancellationToken);

    /// <summary>Gets a triage report by id.</summary>
    public Task<TriageReportDetail> GetReportAsync(string id, CancellationToken cancellationToken = default)
        => GetAsync<TriageReportDetail>($"mcpserver/triage/reports/{Encode(id)}", cancellationToken);

    /// <summary>Queries triage groups by optional status and workspace path.</summary>
    public Task<TriageGroupQueryResult> QueryGroupsAsync(
        string? status = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
        => GetAsync<TriageGroupQueryResult>($"mcpserver/triage/groups{BuildQueryString(status, workspacePath)}", cancellationToken);

    /// <summary>Gets triage dashboard queue buckets and AI run history for an optional workspace.</summary>
    public Task<TriageDashboardResult> GetDashboardAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
        => GetAsync<TriageDashboardResult>($"mcpserver/triage/dashboard{BuildQueryString(workspacePath: workspacePath)}", cancellationToken);

    /// <summary>Gets a triage group by id.</summary>
    public Task<TriageGroupDetail> GetGroupAsync(string id, CancellationToken cancellationToken = default)
        => GetAsync<TriageGroupDetail>($"mcpserver/triage/groups/{Encode(id)}", cancellationToken);

    /// <summary>Queries AI triage research runs by optional status, group, and workspace filters.</summary>
    public Task<TriageRunQueryResult> QueryRunsAsync(
        string? status = null,
        string? groupId = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
        => GetAsync<TriageRunQueryResult>(
            $"mcpserver/triage/runs{BuildQueryString(status, workspacePath, groupId)}",
            cancellationToken);

    /// <summary>Gets an AI triage research run by id.</summary>
    public Task<TriageResearchRunDetail> GetRunAsync(string id, CancellationToken cancellationToken = default)
        => GetAsync<TriageResearchRunDetail>($"mcpserver/triage/runs/{Encode(id)}", cancellationToken);

    /// <summary>Queries TODO ids created by triage with persisted TODO creation timestamps.</summary>
    public Task<TriageCreatedTodoQueryResult> QueryCreatedTodosAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
        => GetAsync<TriageCreatedTodoQueryResult>(
            $"mcpserver/triage/todos{BuildQueryString(workspacePath: workspacePath)}",
            cancellationToken);

    /// <summary>Flushes a triage group for immediate research eligibility.</summary>
    public Task<TriageGroupDetail> FlushGroupAsync(string id, CancellationToken cancellationToken = default)
        => PostAsync<TriageGroupDetail>($"mcpserver/triage/groups/{Encode(id)}/flush", null, cancellationToken);

    /// <summary>Retries a failed triage group.</summary>
    public Task<TriageGroupDetail> RetryGroupAsync(string id, CancellationToken cancellationToken = default)
        => PostAsync<TriageGroupDetail>($"mcpserver/triage/groups/{Encode(id)}/retry", null, cancellationToken);

    private static string BuildQueryString(string? status = null, string? workspacePath = null, string? groupId = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            parts.Add($"status={Encode(status)}");
        if (!string.IsNullOrWhiteSpace(groupId))
            parts.Add($"groupId={Encode(groupId)}");
        if (!string.IsNullOrWhiteSpace(workspacePath))
            parts.Add($"workspacePath={Encode(workspacePath)}");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private static string Encode(string value) => Uri.EscapeDataString(value);
}
