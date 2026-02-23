using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Result of running an ingestion sync.</summary>
public sealed class SyncRunResult
{
    /// <summary>Run identifier.</summary>
    [JsonPropertyName("runId")]
    public string? RunId { get; set; }

    /// <summary>Start time (ISO 8601).</summary>
    [JsonPropertyName("startedAt")]
    public string? StartedAt { get; set; }

    /// <summary>Completion time (ISO 8601).</summary>
    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; set; }

    /// <summary>Run status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Error message on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Number of documents ingested.</summary>
    [JsonPropertyName("documentsIngested")]
    public int DocumentsIngested { get; set; }

    /// <summary>Number of chunks written.</summary>
    [JsonPropertyName("chunksWritten")]
    public int ChunksWritten { get; set; }

    /// <summary>Number of session logs imported.</summary>
    [JsonPropertyName("sessionLogsImported")]
    public int SessionLogsImported { get; set; }

    /// <summary>Number of issues synced.</summary>
    [JsonPropertyName("issuesSynced")]
    public int IssuesSynced { get; set; }
}

/// <summary>Current sync status.</summary>
public sealed class SyncStatus
{
    /// <summary>Last run identifier.</summary>
    [JsonPropertyName("lastRun")]
    public string? LastRun { get; set; }

    /// <summary>Last completion time.</summary>
    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; set; }

    /// <summary>Current status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Documents ingested in last run.</summary>
    [JsonPropertyName("documentsIngested")]
    public int? DocumentsIngested { get; set; }

    /// <summary>Chunks written in last run.</summary>
    [JsonPropertyName("chunksWritten")]
    public int? ChunksWritten { get; set; }

    /// <summary>Session logs imported in last run.</summary>
    [JsonPropertyName("sessionLogsImported")]
    public int? SessionLogsImported { get; set; }

    /// <summary>Issues synced in last run.</summary>
    [JsonPropertyName("issuesSynced")]
    public int? IssuesSynced { get; set; }
}
