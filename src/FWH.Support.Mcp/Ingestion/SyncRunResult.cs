namespace FWH.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Result of a single sync (ingestion) run.
/// FR-SUPPORT-010: Status for sync.status endpoint.
/// </summary>
public sealed class SyncRunResult
{
    /// <summary>Unique run identifier.</summary>
    public required string RunId { get; init; }

    /// <summary>When the run started (UTC).</summary>
    public DateTime StartedAt { get; init; }

    /// <summary>When the run completed (UTC); null if still running or failed before completion.</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Status: Idle, Running, Completed, Failed.</summary>
    public required string Status { get; init; }

    /// <summary>Error message if Status == Failed.</summary>
    public string? Error { get; init; }

    /// <summary>Documents ingested in this run.</summary>
    public int DocumentsIngested { get; init; }

    /// <summary>Chunks written in this run.</summary>
    public int ChunksWritten { get; init; }

    /// <summary>TR-PLANNED-013: Session logs imported into 4NF tables (MVP-SUPPORT-011).</summary>
    public int SessionLogsImported { get; init; }

    /// <summary>TR-GH-013-004: Issues synced during ingestion.</summary>
    public int IssuesSynced { get; init; }
}
