using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

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
    public HandoffSourceKind SourceKind { get; set; }

    /// <summary>Workspace-relative or workspace-contained absolute path when source kind is Path.</summary>
    public string? Path { get; set; }

    /// <summary>Caller-supplied document text when source kind is Content.</summary>
    public string? Content { get; set; }

    /// <summary>MCP artifact identifier when source kind is Artifact.</summary>
    public string? ArtifactId { get; set; }

    /// <summary>Ingestion mode. Defaults to DraftOnly.</summary>
    public HandoffIngestionMode Mode { get; set; } = HandoffIngestionMode.DraftOnly;

    /// <summary>When true, skip deterministic replay and create a new run.</summary>
    public bool Force { get; set; }

    /// <summary>Optional explicit pooled agent name for extraction.</summary>
    public string? AgentName { get; set; }

    /// <summary>Optional prompt template identifier.</summary>
    public string? PromptTemplateId { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Structured TODO draft extracted from a handoff document.</summary>
public sealed class HandoffTodoDraft
{
    /// <summary>Proposed TODO identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Proposed TODO title.</summary>
    public string? Title { get; set; }

    /// <summary>Proposed TODO section.</summary>
    public string? Section { get; set; }

    /// <summary>Proposed priority.</summary>
    public string? Priority { get; set; }

    /// <summary>Proposed estimate string.</summary>
    public string? Estimate { get; set; }

    /// <summary>Proposed description lines.</summary>
    public IReadOnlyList<string> Description { get; set; } = [];

    /// <summary>Proposed technical detail lines.</summary>
    public IReadOnlyList<string> TechnicalDetails { get; set; } = [];

    /// <summary>Proposed implementation tasks.</summary>
    public IReadOnlyList<HandoffTodoDraftTask> ImplementationTasks { get; set; } = [];

    /// <summary>Proposed dependency TODO identifiers.</summary>
    public IReadOnlyList<string> DependsOn { get; set; } = [];

    /// <summary>Proposed functional requirement identifiers.</summary>
    public IReadOnlyList<string> FunctionalRequirements { get; set; } = [];

    /// <summary>Proposed technical requirement identifiers.</summary>
    public IReadOnlyList<string> TechnicalRequirements { get; set; } = [];

    /// <summary>Extractor-reported confidence in the range 0.0 to 1.0.</summary>
    public double? Confidence { get; set; }

    /// <summary>Unknown or missing source notes that must not be discarded.</summary>
    public IReadOnlyList<string> UnknownSourceNotes { get; set; } = [];
}

/// <summary>TR-HANDOFF-CONTRACT-001: Implementation task inside a handoff TODO draft.</summary>
public sealed class HandoffTodoDraftTask
{
    /// <summary>Task text.</summary>
    public string Task { get; set; } = string.Empty;

    /// <summary>Whether the task is already done.</summary>
    public bool Done { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Field-specific or run-level diagnostic.</summary>
public sealed class HandoffDiagnostic
{
    /// <summary>Stable diagnostic code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Diagnostic severity.</summary>
    public HandoffDiagnosticSeverity Severity { get; set; }

    /// <summary>Draft field name when the diagnostic is field-specific.</summary>
    public string? Field { get; set; }

    /// <summary>Human-readable message. Must not include raw source content or credentials.</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>TR-HANDOFF-CONTRACT-001: Auditable provenance for a handoff run.</summary>
public sealed class HandoffProvenance
{
    /// <summary>Durable run identifier.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Source kind used for this run.</summary>
    public HandoffSourceKind SourceKind { get; set; }

    /// <summary>Path or artifact locator. Never raw source content.</summary>
    public string SourceLocator { get; set; } = string.Empty;

    /// <summary>SHA-256 of the decoded source bytes, lowercase hex.</summary>
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>UTC extraction timestamp.</summary>
    public DateTimeOffset ExtractedAtUtc { get; set; }

    /// <summary>Versioned prompt identifier.</summary>
    public string PromptVersion { get; set; } = string.Empty;

    /// <summary>Prompt template identifier when a stored template was used.</summary>
    public string? TemplateVersion { get; set; }

    /// <summary>Pooled agent name used for extraction.</summary>
    public string? Agent { get; set; }

    /// <summary>Model identifier when the extractor reported one.</summary>
    public string? Model { get; set; }

    /// <summary>Final confidence used for mode decisions.</summary>
    public double? Confidence { get; set; }

    /// <summary>Mode requested for this run.</summary>
    public HandoffIngestionMode Mode { get; set; }

    /// <summary>Review or creation state.</summary>
    public HandoffReviewState ReviewState { get; set; }

    /// <summary>Created TODO identifier when a TODO was persisted.</summary>
    public string? CreatedTodoId { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Result of ingesting or inspecting a handoff run.</summary>
public sealed class HandoffIngestionResult
{
    /// <summary>Whether the operation completed without a transport-level failure.</summary>
    public bool Success { get; set; }

    /// <summary>Whether a new TODO was created by this call.</summary>
    public bool Created { get; set; }

    /// <summary>Whether this result is a deterministic replay of a prior run.</summary>
    public bool Replayed { get; set; }

    /// <summary>Whether the run is waiting for operator approval.</summary>
    public bool RequiresReview { get; set; }

    /// <summary>Normalized draft when extraction produced one.</summary>
    public HandoffTodoDraft? Draft { get; set; }

    /// <summary>Auditable provenance. Never includes raw source content or credentials.</summary>
    public HandoffProvenance? Provenance { get; set; }

    /// <summary>Diagnostics collected for this run.</summary>
    public IReadOnlyList<HandoffDiagnostic> Diagnostics { get; set; } = [];

    /// <summary>Created TODO identifier when a TODO was persisted.</summary>
    public string? CreatedTodoId { get; set; }

    /// <summary>Top-level error message when Success is false.</summary>
    public string? Error { get; set; }

    /// <summary>Stable outcome code used by HTTP mapping. Never localized English matching.</summary>
    public string? ErrorCode { get; set; }
}

/// <summary>TR-HANDOFF-CONTRACT-001: Request to approve or reject a stored handoff run.</summary>
public sealed class HandoffApprovalRequest
{
    /// <summary>True to approve and create the TODO after revalidation. False to reject.</summary>
    public bool Approved { get; set; }

    /// <summary>Reviewer identity recorded on the run. Must not contain secrets.</summary>
    public string? Reviewer { get; set; }

    /// <summary>Optional review notes. Must not contain raw source content or credentials.</summary>
    public string? Notes { get; set; }
}

/// <summary>TR-HANDOFF-SECURITY-001: Resolved source bytes and locator without leaking content into logs.</summary>
public sealed class HandoffResolvedSource
{
    /// <summary>Whether resolution succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Source kind actually resolved.</summary>
    public HandoffSourceKind SourceKind { get; init; }

    /// <summary>Safe locator used for provenance.</summary>
    public string Locator { get; init; } = string.Empty;

    /// <summary>Decoded UTF-8 text. Not copied into logs or provenance.</summary>
    public string? Text { get; init; }

    /// <summary>SHA-256 of the decoded UTF-8 bytes, lowercase hex.</summary>
    public string? ContentSha256 { get; init; }

    /// <summary>Diagnostics produced during resolution.</summary>
    public IReadOnlyList<HandoffDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>TR-HANDOFF-AGENT-001: Result of one-shot extraction.</summary>
public sealed class HandoffExtractionResult
{
    /// <summary>Whether the extractor returned a terminal response.</summary>
    public bool Success { get; init; }

    /// <summary>Raw extractor text. Must be strict JSON to parse.</summary>
    public string? ResponseText { get; init; }

    /// <summary>Resolved agent name.</summary>
    public string? AgentName { get; init; }

    /// <summary>Resolved model name when known.</summary>
    public string? Model { get; init; }

    /// <summary>Prompt or template version actually used.</summary>
    public string PromptVersion { get; init; } = HandoffPromptDefaults.PromptVersion;

    /// <summary>Template identifier when a stored template was used.</summary>
    public string? TemplateVersion { get; init; }

    /// <summary>Extractor error text. Must not include source content.</summary>
    public string? Error { get; init; }
}
