using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// FR-MCP-TRIAGE-001: Triage report contract shared by REST, client, REPL, and plugin paths.
/// </summary>
public sealed record TriageReportRequest
{
    /// <summary>Human-readable issue title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>Concise bug summary.</summary>
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    /// <summary>Observed behavior, if known.</summary>
    [JsonPropertyName("observedBehavior")]
    public string? ObservedBehavior { get; init; }

    /// <summary>Expected behavior, if known.</summary>
    [JsonPropertyName("expectedBehavior")]
    public string? ExpectedBehavior { get; init; }

    /// <summary>Severity label.</summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; init; }

    /// <summary>Component, plugin, or subsystem.</summary>
    [JsonPropertyName("component")]
    public string? Component { get; init; }

    /// <summary>Stable caller-provided dedupe key.</summary>
    [JsonPropertyName("dedupeKey")]
    public string? DedupeKey { get; init; }

    /// <summary>Error signature or invariant text.</summary>
    [JsonPropertyName("errorSignature")]
    public string? ErrorSignature { get; init; }

    /// <summary>Affected paths.</summary>
    [JsonPropertyName("affectedPaths")]
    public IReadOnlyList<string>? AffectedPaths { get; init; }

    /// <summary>Affected symbols.</summary>
    [JsonPropertyName("affectedSymbols")]
    public IReadOnlyList<string>? AffectedSymbols { get; init; }

    /// <summary>Evidence map.</summary>
    [JsonPropertyName("evidence")]
    public IReadOnlyDictionary<string, string>? Evidence { get; init; }

    /// <summary>Reproduction hints.</summary>
    [JsonPropertyName("reproductionHints")]
    public IReadOnlyList<string>? ReproductionHints { get; init; }

    /// <summary>Tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Reporting agent identity.</summary>
    [JsonPropertyName("reporterAgent")]
    public string? ReporterAgent { get; init; }

    /// <summary>Session id active when the report was created.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    /// <summary>Turn id active when the report was created.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; init; }

    /// <summary>Current TODO id for context only.</summary>
    [JsonPropertyName("currentTodoId")]
    public string? CurrentTodoId { get; init; }

    /// <summary>Optional submitting workspace override.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    /// <summary>Optional idempotency key.</summary>
    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; init; }
}

/// <summary>FR-MCP-TRIAGE-001: Accepted triage intake result.</summary>
public sealed record TriageReportSubmitResult
{
    /// <summary>Whether intake accepted the report.</summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>Validation or persistence error.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>Durable report id.</summary>
    [JsonPropertyName("reportId")]
    public string ReportId { get; init; } = string.Empty;

    /// <summary>Durable group id.</summary>
    [JsonPropertyName("groupId")]
    public string GroupId { get; init; } = string.Empty;

    /// <summary>Current group status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>UTC quiet deadline.</summary>
    [JsonPropertyName("quietDeadlineUtc")]
    public DateTimeOffset QuietDeadlineUtc { get; init; }

    /// <summary>Effective workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; init; } = string.Empty;
}

/// <summary>FR-MCP-TRIAGE-001: Submitted triage report detail.</summary>
public sealed record TriageReportDetail
{
    /// <summary>Durable report id.</summary>
    [JsonPropertyName("reportId")]
    public required string ReportId { get; init; }

    /// <summary>Group id that owns this report.</summary>
    [JsonPropertyName("groupId")]
    public required string GroupId { get; init; }

    /// <summary>Report status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Report title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Report summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    /// <summary>Original submitting workspace path.</summary>
    [JsonPropertyName("originalWorkspacePath")]
    public string? OriginalWorkspacePath { get; init; }

    /// <summary>Effective workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdUtc")]
    public DateTimeOffset CreatedUtc { get; init; }
}

/// <summary>FR-MCP-TRIAGE-002: Triage group detail.</summary>
public sealed record TriageGroupDetail
{
    /// <summary>Durable group id.</summary>
    [JsonPropertyName("groupId")]
    public required string GroupId { get; init; }

    /// <summary>Group status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Number of reports in the group.</summary>
    [JsonPropertyName("reportCount")]
    public required int ReportCount { get; init; }

    /// <summary>Effective workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    /// <summary>Representative title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Representative summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    /// <summary>UTC quiet deadline.</summary>
    [JsonPropertyName("quietDeadlineUtc")]
    public DateTimeOffset QuietDeadlineUtc { get; init; }

    /// <summary>Created backlog TODO id.</summary>
    [JsonPropertyName("createdTodoId")]
    public string? CreatedTodoId { get; init; }

    /// <summary>Inspectable failure text.</summary>
    [JsonPropertyName("lastError")]
    public string? LastError { get; init; }

    /// <summary>Reports in this group.</summary>
    [JsonPropertyName("reports")]
    public IReadOnlyList<TriageReportDetail> Reports { get; init; } = [];
}

/// <summary>FR-MCP-TRIAGE-002: Triage group query result.</summary>
public sealed record TriageGroupQueryResult
{
    /// <summary>Matching groups.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<TriageGroupDetail> Items { get; init; } = [];

    /// <summary>Total matching groups.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
}
