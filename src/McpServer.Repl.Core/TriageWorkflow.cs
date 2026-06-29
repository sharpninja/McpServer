using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// TR-MCP-REPL-TRIAGE-001: Production workflow.triage wrapper that delegates to <see cref="TriageClient"/>.
/// </summary>
public sealed class TriageWorkflow : ITriageWorkflow
{
    private readonly TriageClient _client;

    /// <summary>Initializes a new instance of the <see cref="TriageWorkflow"/> class.</summary>
    public TriageWorkflow(TriageClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public Task<TriageReportSubmitResult> ReportAsync(TriageReportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _client.SubmitReportAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageReportDetail> GetReportAsync(string reportId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportId))
            throw new ArgumentException("Report id is required.", nameof(reportId));
        return _client.GetReportAsync(reportId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageGroupQueryResult> QueryGroupsAsync(string? status = null, string? workspacePath = null, CancellationToken cancellationToken = default)
        => _client.QueryGroupsAsync(status, workspacePath, cancellationToken);

    /// <inheritdoc />
    public Task<TriageDashboardResult> GetDashboardAsync(string? workspacePath = null, CancellationToken cancellationToken = default)
        => _client.GetDashboardAsync(workspacePath, cancellationToken);

    /// <inheritdoc />
    public Task<TriageGroupDetail> GetGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group id is required.", nameof(groupId));
        return _client.GetGroupAsync(groupId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageRunQueryResult> QueryRunsAsync(
        string? status = null,
        string? groupId = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
        => _client.QueryRunsAsync(status, groupId, workspacePath, cancellationToken);

    /// <inheritdoc />
    public Task<TriageResearchRunDetail> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run id is required.", nameof(runId));
        return _client.GetRunAsync(runId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageCreatedTodoQueryResult> QueryCreatedTodosAsync(string? workspacePath = null, CancellationToken cancellationToken = default)
        => _client.QueryCreatedTodosAsync(workspacePath, cancellationToken);

    /// <inheritdoc />
    public Task<TriageGroupDetail> FlushGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group id is required.", nameof(groupId));
        return _client.FlushGroupAsync(groupId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageGroupDetail> RetryGroupAsync(
        string groupId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group id is required.", nameof(groupId));
        return _client.RetryGroupAsync(groupId, force, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageGroupEditResult> CreateGroupFromSelectionAsync(
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _client.CreateGroupFromSelectionAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageGroupEditResult> ConsolidateIntoGroupAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetGroupId))
            throw new ArgumentException("Target group id is required.", nameof(targetGroupId));
        ArgumentNullException.ThrowIfNull(request);
        return _client.ConsolidateIntoGroupAsync(targetGroupId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageGroupEditResult> MergeGroupsAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetGroupId))
            throw new ArgumentException("Target group id is required.", nameof(targetGroupId));
        ArgumentNullException.ThrowIfNull(request);
        return _client.MergeGroupsAsync(targetGroupId, request, cancellationToken);
    }
}
