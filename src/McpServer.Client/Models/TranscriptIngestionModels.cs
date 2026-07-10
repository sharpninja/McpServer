using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Supported transcript source formats for ingestion.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TranscriptSourceKind
{
    /// <summary>Automatically detect the transcript source kind.</summary>
    Auto = 0,

    /// <summary>Claude Code JSONL transcript format.</summary>
    Claude = 1,

    /// <summary>OpenAI Codex JSONL transcript format.</summary>
    Codex = 2,

    /// <summary>Grok chat or event transcript format.</summary>
    Grok = 3,

    /// <summary>Cline paired session/messages JSON or JSONL export format.</summary>
    Cline = 4,

    /// <summary>GitHub Copilot event-stream session folder format.</summary>
    Copilot = 5,

    /// <summary>OpenCode JSONL export or snapshot format.</summary>
    OpenCode = 6,
}

/// <summary>Optional provider profile emitted beside canonical Session Log YAML.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TranscriptCompatibilityProfile
{
    /// <summary>No compatibility JSONL output.</summary>
    None = 0,

    /// <summary>Claude-compatible JSONL output.</summary>
    Claude = 1,

    /// <summary>Codex-compatible JSONL output.</summary>
    Codex = 2,

    /// <summary>Grok-compatible JSONL output.</summary>
    Grok = 3,
}

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
    public TranscriptCompatibilityProfile CompatibilityProfile { get; set; } = TranscriptCompatibilityProfile.None;

    /// <summary>Whether to emit the selected compatibility profile artifact.</summary>
    [JsonPropertyName("emitNormalizedProfile")]
    public bool EmitNormalizedProfile { get; set; }
}


/// <summary>One transcript upload file for multipart ingestion.</summary>
public sealed class TranscriptUploadFile
{
    /// <summary>Uploaded file name, optionally including a safe relative path.</summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>Uploaded file content type.</summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>Uploaded file bytes.</summary>
    [JsonPropertyName("content")]
    public byte[] Content { get; set; } = [];
}

/// <summary>Request body for multipart transcript ingestion.</summary>
public sealed class TranscriptIngestUploadRequest
{
    /// <summary>Agent that owns workspace transcript artifacts.</summary>
    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    /// <summary>Requested source kind, or Auto to detect.</summary>
    [JsonPropertyName("source")]
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
    public TranscriptCompatibilityProfile CompatibilityProfile { get; set; } = TranscriptCompatibilityProfile.None;

    /// <summary>Whether to emit the selected compatibility profile artifact.</summary>
    [JsonPropertyName("emitNormalizedProfile")]
    public bool EmitNormalizedProfile { get; set; }

    /// <summary>Files to upload.</summary>
    [JsonPropertyName("files")]
    public IReadOnlyList<TranscriptUploadFile> Files { get; set; } = [];
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


    /// <summary>Receipt returned by the primary session-log persistence path.</summary>
    [JsonPropertyName("persistenceReceipt")]
    public string? PersistenceReceipt { get; set; }

    /// <summary>Per-session diagnostics.</summary>
    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<TranscriptDiagnosticResponse> Diagnostics { get; set; } = [];
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
}