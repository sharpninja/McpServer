using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>Director adapter for <see cref="ISyncApiClient"/> backed by <see cref="McpServer.Client.McpServerClient"/>.</summary>
internal sealed class SyncApiClientAdapter : ISyncApiClient
{
    private readonly DirectorMcpContext _context;

    public SyncApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    public async Task<SyncStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var status = await client.Sync.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return Map(status);
    }

    public async Task<SyncRunSummary> RunAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var run = await client.Sync.RunAsync(cancellationToken).ConfigureAwait(false);
        return Map(run);
    }

    private static SyncStatusSnapshot Map(SyncStatus status)
        => new(
            status.LastRun,
            status.CompletedAt,
            status.Status,
            status.Error,
            status.DocumentsIngested,
            status.ChunksWritten,
            status.SessionLogsImported,
            status.IssuesSynced,
            DateTimeOffset.UtcNow);

    private static SyncRunSummary Map(SyncRunResult run)
        => new(
            run.RunId,
            run.StartedAt,
            run.CompletedAt,
            run.Status,
            run.Error,
            run.DocumentsIngested,
            run.ChunksWritten,
            run.SessionLogsImported,
            run.IssuesSynced,
            DateTimeOffset.UtcNow);
}
