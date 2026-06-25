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
    public Task<TriageGroupDetail> GetGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group id is required.", nameof(groupId));
        return _client.GetGroupAsync(groupId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageGroupDetail> FlushGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group id is required.", nameof(groupId));
        return _client.FlushGroupAsync(groupId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TriageGroupDetail> RetryGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            throw new ArgumentException("Group id is required.", nameof(groupId));
        return _client.RetryGroupAsync(groupId, cancellationToken);
    }
}
