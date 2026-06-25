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

    /// <summary>Gets a triage group by id.</summary>
    Task<TriageGroupDetail> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Flushes a triage group.</summary>
    Task<TriageGroupDetail> FlushGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Retries a triage group.</summary>
    Task<TriageGroupDetail> RetryGroupAsync(string groupId, CancellationToken cancellationToken = default);
}
