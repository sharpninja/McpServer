using System.Text.Json.Serialization;
using McpServer.SessionLog.Transcripts;
using Microsoft.AspNetCore.Http;

namespace McpServer.Support.Mcp.Models;

/// <summary>Request body for path-based transcript ingestion.</summary>
public sealed class TranscriptIngestPathRequest
{
    /// <summary>Server-local file or directory path to ingest.</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>Agent that owns workspace transcript artifacts.</summary>
    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    /// <summary>Requested source kind, or Auto to detect.</summary>
    [JsonPropertyName("source")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TranscriptSourceKind Source { get; set; } = TranscriptSourceKind.Auto;

    /// <summary>Whether recursive folder discovery is enabled.</summary>
    [JsonPropertyName("recursive")]
    public bool Recursive { get; set; } = true;

    /// <summary>Whether malformed records should fail the bundle.</summary>
    [JsonPropertyName("strict")]
    public bool Strict { get; set; } = true;

    /// <summary>Whether to persist through the session-log path.</summary>
    [JsonPropertyName("persist")]
    public bool Persist { get; set; } = true;

    /// <summary>Optional provider compatibility profile.</summary>
    [JsonPropertyName("compatibilityProfile")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TranscriptCompatibilityProfile CompatibilityProfile { get; set; } = TranscriptCompatibilityProfile.None;

    /// <summary>Whether to emit the selected compatibility profile artifact.</summary>
    [JsonPropertyName("emitNormalizedProfile")]
    public bool EmitNormalizedProfile { get; set; }
}


/// <summary>Request body for multipart transcript ingestion.</summary>
public sealed class TranscriptIngestUploadRequest
{
    /// <summary>Agent that owns workspace transcript artifacts.</summary>
    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    /// <summary>Requested source kind, or Auto to detect.</summary>
    [JsonPropertyName("source")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TranscriptSourceKind Source { get; set; } = TranscriptSourceKind.Auto;

    /// <summary>Whether recursive folder discovery is enabled.</summary>
    [JsonPropertyName("recursive")]
    public bool Recursive { get; set; } = true;

    /// <summary>Whether malformed records should fail the bundle.</summary>
    [JsonPropertyName("strict")]
    public bool Strict { get; set; } = true;

    /// <summary>Whether to persist through the session-log path.</summary>
    [JsonPropertyName("persist")]
    public bool Persist { get; set; } = true;

    /// <summary>Optional provider compatibility profile.</summary>
    [JsonPropertyName("compatibilityProfile")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TranscriptCompatibilityProfile CompatibilityProfile { get; set; } = TranscriptCompatibilityProfile.None;

    /// <summary>Whether to emit the selected compatibility profile artifact.</summary>
    [JsonPropertyName("emitNormalizedProfile")]
    public bool EmitNormalizedProfile { get; set; }

    /// <summary>Uploaded transcript files.</summary>
    [JsonPropertyName("files")]
    public List<IFormFile> Files { get; set; } = [];
}

/// <summary>Run receipt returned by transcript ingestion endpoints.</summary>
public sealed class TranscriptIngestRunResponse
{
    /// <summary>Ingestion run identifier.</summary>
    [JsonPropertyName("runId")]
    public string? RunId { get; set; }

    /// <summary>Artifact root for the run.</summary>
    [JsonPropertyName("artifactRootPath")]
    public string? ArtifactRootPath { get; set; }

    /// <summary>Total normalized sessions discovered in the run.</summary>
    [JsonPropertyName("totalSessions")]
    public int TotalSessions { get; set; }

    /// <summary>Pending import recovery envelope paths.</summary>
    [JsonPropertyName("importRecoveryPaths")]
    public IReadOnlyList<string> ImportRecoveryPaths { get; set; } = [];

    /// <summary>Whether all sessions were persisted by the primary session-log path.</summary>
    [JsonPropertyName("persisted")]
    public bool Persisted { get; set; }

    /// <summary>Whether persistence is degraded and recovery files must be retained.</summary>
    [JsonPropertyName("degraded")]
    public bool Degraded { get; set; }

    /// <summary>Per-session ingestion receipts.</summary>
    [JsonPropertyName("receipts")]
    public IReadOnlyList<TranscriptSessionReceiptResponse> Receipts { get; set; } = [];

    /// <summary>Run-level diagnostics.</summary>
    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<TranscriptDiagnosticResponse> Diagnostics { get; set; } = [];

    /// <summary>Builds an HTTP response from a transcript ingestion result.</summary>
    /// <param name="result">Shared ingestion result.</param>
    /// <returns>Serializable run receipt.</returns>
    public static TranscriptIngestRunResponse FromResult(TranscriptIngestionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TranscriptIngestRunResponse
        {
            RunId = result.RunId,
            ArtifactRootPath = result.ArtifactRootPath,
            TotalSessions = result.Receipts.Count > 0 ? result.Receipts.Count : result.Sessions.Count,
            ImportRecoveryPaths = result.ImportRecoveryPaths,
            Persisted = result.Persisted,
            Degraded = result.Degraded,
            Receipts = result.Receipts.Select(TranscriptSessionReceiptResponse.FromReceipt).ToArray(),
            Diagnostics = result.Diagnostics.Select(TranscriptDiagnosticResponse.FromDiagnostic).ToArray(),
        };
    }
}

/// <summary>Receipt for one normalized transcript session.</summary>
public sealed class TranscriptSessionReceiptResponse
{
    /// <summary>Detected source kind.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>Root identifier of the source data unit.</summary>
    [JsonPropertyName("rootId")]
    public string RootId { get; set; } = string.Empty;

    /// <summary>Canonical session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Short source hash used for idempotent artifact names.</summary>
    [JsonPropertyName("sourceHash")]
    public string SourceHash { get; set; } = string.Empty;

    /// <summary>Import status for this session.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Canonical Session Log YAML artifact path.</summary>
    [JsonPropertyName("yamlArtifactPath")]
    public string YamlArtifactPath { get; set; } = string.Empty;

    /// <summary>Pending import recovery envelope path.</summary>
    [JsonPropertyName("importRecoveryPath")]
    public string ImportRecoveryPath { get; set; } = string.Empty;

    /// <summary>Optional compatibility JSONL artifact path.</summary>
    [JsonPropertyName("compatibilityArtifactPath")]
    public string? CompatibilityArtifactPath { get; set; }

    /// <summary>Per-session diagnostics.</summary>
    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<TranscriptDiagnosticResponse> Diagnostics { get; set; } = [];

    /// <summary>Builds a receipt response from a shared receipt.</summary>
    /// <param name="receipt">Shared session receipt.</param>
    /// <returns>Serializable session receipt.</returns>
    public static TranscriptSessionReceiptResponse FromReceipt(TranscriptSessionReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new TranscriptSessionReceiptResponse
        {
            Source = receipt.SourceKind.ToString(),
            RootId = receipt.RootId,
            SessionId = receipt.SessionId,
            SourceHash = receipt.SourceHash,
            Status = receipt.Status,
            YamlArtifactPath = receipt.YamlArtifactPath,
            ImportRecoveryPath = receipt.ImportRecoveryPath,
            CompatibilityArtifactPath = receipt.CompatibilityArtifactPath,
            Diagnostics = receipt.Diagnostics.Select(TranscriptDiagnosticResponse.FromDiagnostic).ToArray(),
        };
    }
}

/// <summary>Diagnostic emitted while ingesting transcripts.</summary>
public sealed class TranscriptDiagnosticResponse
{
    /// <summary>Stable diagnostic code.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Diagnostic message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Diagnostic severity.</summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    /// <summary>Optional source path.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Builds a diagnostic response from a shared diagnostic.</summary>
    /// <param name="diagnostic">Shared diagnostic.</param>
    /// <returns>Serializable diagnostic.</returns>
    public static TranscriptDiagnosticResponse FromDiagnostic(TranscriptDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new TranscriptDiagnosticResponse
        {
            Code = diagnostic.Code,
            Message = diagnostic.Message,
            Severity = diagnostic.Severity,
            Path = diagnostic.Path,
        };
    }
}