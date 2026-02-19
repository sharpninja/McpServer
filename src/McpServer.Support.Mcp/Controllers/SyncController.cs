using McpServer.Support.Mcp.Ingestion;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// TR-PLANNED-013: Sync (ingestion) trigger and status.
/// FR-SUPPORT-010: sync.run, sync.status.
/// </summary>
[ApiController]
[Route("mcp/sync")]
public sealed class SyncController : ControllerBase
{
    private readonly IngestionCoordinator _coordinator;
    private readonly ISyncStatusStore _syncStatusStore;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="coordinator">Ingestion coordinator for running sync.</param>
    /// <param name="syncStatusStore">Store for tracking sync run status.</param>
    public SyncController(IngestionCoordinator coordinator, ISyncStatusStore syncStatusStore)
    {
        _coordinator = coordinator;
        _syncStatusStore = syncStatusStore;
    }

    /// <summary>TR-PLANNED-013: Trigger ingestion (sync.run).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sync run result with documents ingested and chunks written.</returns>
    [HttpPost("run")]
    public async Task<ActionResult<object>> RunSyncAsync(CancellationToken cancellationToken)
    {
        var result = await _coordinator.RunAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            result.RunId,
            result.StartedAt,
            result.CompletedAt,
            result.Status,
            result.Error,
            result.DocumentsIngested,
            result.ChunksWritten
        });
    }

    /// <summary>TR-PLANNED-013: Last sync status (sync.status).</summary>
    /// <returns>Last sync run status including timestamps, counts, and error info.</returns>
    [HttpGet("status")]
    public ActionResult<object> GetSyncStatus()
    {
        var last = _syncStatusStore.GetLast();
        if (last == null)
        {
            return Ok(new { lastRun = (DateTime?)null, status = "idle", error = (string?)null });
        }
        return Ok(new
        {
            lastRun = last.StartedAt,
            completedAt = last.CompletedAt,
            status = last.Status,
            error = last.Error,
            documentsIngested = last.DocumentsIngested,
            chunksWritten = last.ChunksWritten
        });
    }
}
