using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// TR-MCP-REPL-TRIAGE-001: Typed workflow wrapper for triage REPL operations.
/// </summary>
public interface ITriageWorkflow
{
    /// <summary>Submits an incidental bug report.</summary>
    Task<TriageReportSubmitResult> ReportAsync(TriageReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a triage report by id.</summary>
    Task<TriageReportDetail> GetReportAsync(string reportId, CancellationToken cancellationToken = default);

    /// <summary>Queries triage groups.</summary>
    Task<TriageGroupQueryResult> QueryGroupsAsync(string? status = null, string? workspacePath = null, CancellationToken cancellationToken = default);

    /// <summary>Gets triage dashboard state.</summary>
    Task<TriageDashboardResult> GetDashboardAsync(string? workspacePath = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a triage group by id.</summary>
    Task<TriageGroupDetail> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Queries triage research runs.</summary>
    Task<TriageRunQueryResult> QueryRunsAsync(
        string? status = null,
        string? groupId = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a triage research run by id.</summary>
    Task<TriageResearchRunDetail> GetRunAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>Queries TODOs created by triage.</summary>
    Task<TriageCreatedTodoQueryResult> QueryCreatedTodosAsync(string? workspacePath = null, CancellationToken cancellationToken = default);

    /// <summary>Flushes a triage group.</summary>
    Task<TriageGroupDetail> FlushGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Retries a triage group.</summary>
    Task<TriageGroupDetail> RetryGroupAsync(string groupId, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a triage group and its reports.</summary>
    Task<TriageGroupDeleteResult> DeleteGroupAsync(string groupId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a triage group from selected reports or groups.</summary>
    Task<TriageGroupEditResult> CreateGroupFromSelectionAsync(
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Moves selected reports or groups into an existing triage group.</summary>
    Task<TriageGroupEditResult> ConsolidateIntoGroupAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Merges source triage groups into an existing triage group.</summary>
    Task<TriageGroupEditResult> MergeGroupsAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default);
}
