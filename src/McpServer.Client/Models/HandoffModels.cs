using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>TR-HANDOFF-CONTRACT-001: Source kinds accepted by handoff ingestion.</summary>
[JsonConverter(typeof(HandoffStrictStringEnumConverter<HandoffSourceKind>))]
public enum HandoffSourceKind
{
    /// <summary>Workspace-contained file path.</summary>
    Path = 0,

    /// <summary>Caller-supplied document content.</summary>
    Content = 1,

    /// <summary>MCP artifact identifier resolved inside the workspace.</summary>
    Artifact = 2,
}

/// <summary>TR-HANDOFF-CONTRACT-001: Handoff ingestion modes.</summary>
[JsonConverter(typeof(HandoffStrictStringEnumConverter<HandoffIngestionMode>))]
public enum HandoffIngestionMode
{
    /// <summary>Default. Extract and validate a draft without mutating TODO state.</summary>
    DraftOnly = 0,

    /// <summary>Persist an approvable run without creating a TODO.</summary>
    RequireReview = 1,

    /// <summary>Create a TODO only when confidence is at least 0.75 and no error diagnostic exists.</summary>
    CreateWhenConfident = 2,
}

/// <summary>TR-HANDOFF-CONTRACT-001: Review state recorded on a handoff run.</summary>
[JsonConverter(typeof(HandoffStrictStringEnumConverter<HandoffReviewState>))]
public enum HandoffReviewState
{
    /// <summary>No review is pending. Used for DraftOnly completions.</summary>
    None = 0,

    /// <summary>The run is stored and waiting for operator approval.</summary>
    PendingReview = 1,

    /// <summary>The stored draft was rejected.</summary>
    Rejected = 3,

    /// <summary>A TODO was created from this run.</summary>
    Created = 4,

    /// <summary>This result is a deterministic replay of a prior run.</summary>
    Replayed = 5,

    /// <summary>Extraction or validation stopped before a durable review state.</summary>
    Failed = 6,

    /// <summary>An approval claim is in flight. Used for durable compare-and-swap.</summary>
    Approving = 7,
}

/// <summary>TR-HANDOFF-CONTRACT-001: Diagnostic severity for a handoff run.</summary>
[JsonConverter(typeof(HandoffStrictStringEnumConverter<HandoffDiagnosticSeverity>))]
public enum HandoffDiagnosticSeverity
{
    /// <summary>Informational diagnostic.</summary>
    Info = 0,

    /// <summary>Non-fatal diagnostic that still requires attention.</summary>
    Warning = 1,

    /// <summary>Blocking diagnostic. TODO creation is forbidden.</summary>
    Error = 2,
}

/// <summary>TR-HANDOFF-CONTRACT-001: Request to ingest a handoff document.</summary>
public sealed class HandoffIngestionRequest
{
    /// <summary>How the source document is supplied.</summary>
    [JsonPropertyName("sourceKind")]
    public HandoffSourceKind SourceKind { get; set; }

    /// <summary>Workspace-relative or workspace-contained absolute path when <see cref="SourceKind"/> is Path.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Caller-supplied document text when <see cref="SourceKind"/> is Content.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>MCP artifact identifier when <see cref="SourceKind"/> is Artifact.</summary>
    [JsonPropertyName("artifactId")]
    public string? ArtifactId { get; set; }

    /// <summary>Ingestion mode. Defaults to DraftOnly.</summary>
    [JsonPropertyName("mode")]
    public HandoffIngestionMode Mode { get; set; } = HandoffIngestionMode.DraftOnly;

    /// <summary>When true, skip deterministic replay and create a new run.</summary>
    [JsonPropertyName("force")]
    public bool Force { get; set; }

    /// <summary>Optional explicit pooled agent name for extraction.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    /// <summary>Optional prompt template identifier. Defaults to the versioned handoff template.</summary>
    [JsonPropertyName("promptTemplateId")]
    public string? PromptTemplateId { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Structured TODO draft extracted from a handoff document.</summary>
public sealed class HandoffTodoDraft
{
    /// <summary>Proposed TODO identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Proposed TODO title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Proposed TODO section.</summary>
    [JsonPropertyName("section")]
    public string? Section { get; set; }

    /// <summary>Proposed priority: critical, high, medium, or low.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Proposed estimate string.</summary>
    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    /// <summary>Proposed description lines.</summary>
    [JsonPropertyName("description")]
    public IReadOnlyList<string> Description { get; set; } = [];

    /// <summary>Proposed technical detail lines.</summary>
    [JsonPropertyName("technicalDetails")]
    public IReadOnlyList<string> TechnicalDetails { get; set; } = [];

    /// <summary>Proposed implementation tasks.</summary>
    [JsonPropertyName("implementationTasks")]
    public IReadOnlyList<HandoffTodoDraftTask> ImplementationTasks { get; set; } = [];

    /// <summary>Proposed dependency TODO identifiers.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<string> DependsOn { get; set; } = [];

    /// <summary>Proposed functional requirement identifiers.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string> FunctionalRequirements { get; set; } = [];

    /// <summary>Proposed technical requirement identifiers.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string> TechnicalRequirements { get; set; } = [];

    /// <summary>Extractor-reported confidence in the range 0.0 to 1.0.</summary>
    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    /// <summary>Unknown or missing source notes that must not be discarded.</summary>
    [JsonPropertyName("unknownSourceNotes")]
    public IReadOnlyList<string> UnknownSourceNotes { get; set; } = [];
}

/// <summary>TR-HANDOFF-CONTRACT-001: Implementation task inside a handoff TODO draft.</summary>
public sealed class HandoffTodoDraftTask
{
    /// <summary>Task text.</summary>
    [JsonPropertyName("task")]
    public string Task { get; set; } = string.Empty;

    /// <summary>Whether the task is already done.</summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Field-specific or run-level diagnostic.</summary>
public sealed class HandoffDiagnostic
{
    /// <summary>Stable diagnostic code.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Diagnostic severity.</summary>
    [JsonPropertyName("severity")]
    public HandoffDiagnosticSeverity Severity { get; set; }

    /// <summary>Draft field name when the diagnostic is field-specific.</summary>
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    /// <summary>Human-readable message. Must not include raw source content or credentials.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>TR-HANDOFF-CONTRACT-001: Auditable provenance for a handoff run.</summary>
public sealed class HandoffProvenance
{
    /// <summary>Durable run identifier.</summary>
    [JsonPropertyName("runId")]
    public string RunId { get; set; } = string.Empty;

    /// <summary>Source kind used for this run.</summary>
    [JsonPropertyName("sourceKind")]
    public HandoffSourceKind SourceKind { get; set; }

    /// <summary>Path or artifact locator. Never raw source content.</summary>
    [JsonPropertyName("sourceLocator")]
    public string SourceLocator { get; set; } = string.Empty;

    /// <summary>SHA-256 of the decoded source bytes, lowercase hex.</summary>
    [JsonPropertyName("contentSha256")]
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>UTC extraction timestamp.</summary>
    [JsonPropertyName("extractedAtUtc")]
    public DateTimeOffset ExtractedAtUtc { get; set; }

    /// <summary>Versioned prompt identifier.</summary>
    [JsonPropertyName("promptVersion")]
    public string PromptVersion { get; set; } = string.Empty;

    /// <summary>Prompt template identifier when a stored template was used.</summary>
    [JsonPropertyName("templateVersion")]
    public string? TemplateVersion { get; set; }

    /// <summary>Pooled agent name used for extraction.</summary>
    [JsonPropertyName("agent")]
    public string? Agent { get; set; }

    /// <summary>Model identifier when the extractor reported one.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>Final confidence used for mode decisions.</summary>
    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    /// <summary>Mode requested for this run.</summary>
    [JsonPropertyName("mode")]
    public HandoffIngestionMode Mode { get; set; }

    /// <summary>Review or creation state.</summary>
    [JsonPropertyName("reviewState")]
    public HandoffReviewState ReviewState { get; set; }

    /// <summary>Created TODO identifier when a TODO was persisted.</summary>
    [JsonPropertyName("createdTodoId")]
    public string? CreatedTodoId { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Result of ingesting or inspecting a handoff run.</summary>
public sealed class HandoffIngestionResult
{
    /// <summary>Whether the operation completed without a transport-level failure.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Whether a new TODO was created by this call.</summary>
    [JsonPropertyName("created")]
    public bool Created { get; set; }

    /// <summary>Whether this result is a deterministic replay of a prior run.</summary>
    [JsonPropertyName("replayed")]
    public bool Replayed { get; set; }

    /// <summary>Whether the run is waiting for operator approval.</summary>
    [JsonPropertyName("requiresReview")]
    public bool RequiresReview { get; set; }

    /// <summary>Normalized draft when extraction produced one.</summary>
    [JsonPropertyName("draft")]
    public HandoffTodoDraft? Draft { get; set; }

    /// <summary>Auditable provenance. Never includes raw source content or credentials.</summary>
    [JsonPropertyName("provenance")]
    public HandoffProvenance? Provenance { get; set; }

    /// <summary>Diagnostics collected for this run.</summary>
    [JsonPropertyName("diagnostics")]
    public IReadOnlyList<HandoffDiagnostic> Diagnostics { get; set; } = [];

    /// <summary>Created TODO identifier when a TODO was persisted.</summary>
    [JsonPropertyName("createdTodoId")]
    public string? CreatedTodoId { get; set; }

    /// <summary>Top-level error message when <see cref="Success"/> is false.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Stable outcome code used by HTTP mapping.</summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Request to approve or reject a stored handoff run.</summary>
public sealed class HandoffApprovalRequest
{
    /// <summary>True to approve and create the TODO after revalidation. False to reject.</summary>
    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    /// <summary>Reviewer identity recorded on the run. Must not contain secrets.</summary>
    [JsonPropertyName("reviewer")]
    public string? Reviewer { get; set; }

    /// <summary>Optional review notes. Must not contain raw source content or credentials.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
