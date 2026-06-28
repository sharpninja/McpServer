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

/// <summary>FR-TRIAGE-003: Selected reports or groups to move into a new or existing triage group.</summary>
public sealed record TriageGroupSelectionRequest
{
    /// <summary>Selected triage group ids. All reports in each group are moved.</summary>
    [JsonPropertyName("groupIds")]
    public IReadOnlyList<string>? GroupIds { get; init; }

    /// <summary>Selected triage report ids.</summary>
    [JsonPropertyName("reportIds")]
    public IReadOnlyList<string>? ReportIds { get; init; }

    /// <summary>Optional representative title for a newly created group.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Optional representative summary for a newly created group.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }
}

/// <summary>FR-TRIAGE-003: Result returned after moving or merging triage reports.</summary>
public sealed record TriageGroupEditResult
{
    /// <summary>Target group after the edit.</summary>
    [JsonPropertyName("group")]
    public required TriageGroupDetail Group { get; init; }

    /// <summary>Source group ids deleted because all reports were moved out.</summary>
    [JsonPropertyName("removedGroupIds")]
    public IReadOnlyList<string> RemovedGroupIds { get; init; } = [];

    /// <summary>Number of reports moved into the target group.</summary>
    [JsonPropertyName("movedReportCount")]
    public int MovedReportCount { get; init; }
}

/// <summary>FR-TRIAGE-001: AI triage research run detail for dashboard consumers.</summary>
public sealed record TriageResearchRunDetail
{
    /// <summary>Durable research run id.</summary>
    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    /// <summary>Triage group id researched by this run.</summary>
    [JsonPropertyName("groupId")]
    public required string GroupId { get; init; }

    /// <summary>Current run status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Effective workspace path that owns the run.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    /// <summary>Current group status when the group is available.</summary>
    [JsonPropertyName("groupStatus")]
    public string? GroupStatus { get; init; }

    /// <summary>Representative group title when the group is available.</summary>
    [JsonPropertyName("groupTitle")]
    public string? GroupTitle { get; init; }

    /// <summary>Representative group summary when the group is available.</summary>
    [JsonPropertyName("groupSummary")]
    public string? GroupSummary { get; init; }

    /// <summary>Number of reports in the researched group.</summary>
    [JsonPropertyName("reportCount")]
    public int ReportCount { get; init; }

    /// <summary>Prompt template id used for this run.</summary>
    [JsonPropertyName("promptTemplateId")]
    public string? PromptTemplateId { get; init; }

    /// <summary>Rendered prompt sent to the triage agent.</summary>
    [JsonPropertyName("prompt")]
    public string? Prompt { get; init; }

    /// <summary>Serialized group JSON supplied to the triage agent.</summary>
    [JsonPropertyName("groupJson")]
    public string? GroupJson { get; init; }

    /// <summary>Raw agent output.</summary>
    [JsonPropertyName("rawOutput")]
    public string? RawOutput { get; init; }

    /// <summary>Raw stdout stream captured from the launched triage agent process.</summary>
    [JsonPropertyName("agentStdout")]
    public string? AgentStdout { get; init; }

    /// <summary>Raw stderr stream captured from the launched triage agent process.</summary>
    [JsonPropertyName("agentStderr")]
    public string? AgentStderr { get; init; }

    /// <summary>Launched triage agent process exit code, when available.</summary>
    [JsonPropertyName("agentExitCode")]
    public int? AgentExitCode { get; init; }

    /// <summary>Schema-valid agent JSON after validation.</summary>
    [JsonPropertyName("responseJson")]
    public string? ResponseJson { get; init; }

    /// <summary>Failure text when the run failed.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>Created TODO id, if any.</summary>
    [JsonPropertyName("createdTodoId")]
    public string? CreatedTodoId { get; init; }

    /// <summary>UTC timestamp when the run started.</summary>
    [JsonPropertyName("startedUtc")]
    public DateTimeOffset StartedUtc { get; init; }

    /// <summary>UTC timestamp when the run completed.</summary>
    [JsonPropertyName("completedUtc")]
    public DateTimeOffset? CompletedUtc { get; init; }
}

/// <summary>FR-TRIAGE-001: Query result for AI triage run history.</summary>
public sealed record TriageRunQueryResult
{
    /// <summary>Matching triage research runs.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<TriageResearchRunDetail> Items { get; init; } = [];

    /// <summary>Total matching runs.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
}

/// <summary>FR-TRIAGE-002: TODO created from a triage group or research run.</summary>
public sealed record TriageCreatedTodoDetail
{
    /// <summary>Canonical TODO identifier created by triage.</summary>
    [JsonPropertyName("todoId")]
    public required string TodoId { get; init; }

    /// <summary>Persisted UTC timestamp when the TODO anchor was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Workspace path that owns the triage-created TODO.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    /// <summary>Triage group that produced the TODO, when available.</summary>
    [JsonPropertyName("groupId")]
    public string? GroupId { get; init; }

    /// <summary>Research run that produced the TODO, when available.</summary>
    [JsonPropertyName("runId")]
    public string? RunId { get; init; }

    /// <summary>Current triage group status, when available.</summary>
    [JsonPropertyName("groupStatus")]
    public string? GroupStatus { get; init; }

    /// <summary>Current research run status, when available.</summary>
    [JsonPropertyName("runStatus")]
    public string? RunStatus { get; init; }

    /// <summary>Representative group title, when available.</summary>
    [JsonPropertyName("groupTitle")]
    public string? GroupTitle { get; init; }

    /// <summary>Representative group summary, when available.</summary>
    [JsonPropertyName("groupSummary")]
    public string? GroupSummary { get; init; }

    /// <summary>Number of reports attached to the group, when available.</summary>
    [JsonPropertyName("reportCount")]
    public int ReportCount { get; init; }

    /// <summary>Current quiet deadline for the group, when available.</summary>
    [JsonPropertyName("quietDeadlineUtc")]
    public DateTimeOffset? QuietDeadlineUtc { get; init; }
}

/// <summary>FR-TRIAGE-002: Query result for TODOs created by triage.</summary>
public sealed record TriageCreatedTodoQueryResult
{
    /// <summary>Matching triage-created TODOs with creation timestamps.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<TriageCreatedTodoDetail> Items { get; init; } = [];

    /// <summary>Total matching triage-created TODOs.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
}

/// <summary>FR-TRIAGE-001: Read-only dashboard state for triage queue, report-group queue, and run history.</summary>
public sealed record TriageDashboardResult
{
    /// <summary>Groups still collecting or waiting for their quiet window.</summary>
    [JsonPropertyName("triageQueue")]
    public IReadOnlyList<TriageGroupDetail> TriageQueue { get; init; } = [];

    /// <summary>Groups ready for or currently in report-group processing.</summary>
    [JsonPropertyName("reportGroupQueue")]
    public IReadOnlyList<TriageGroupDetail> ReportGroupQueue { get; init; } = [];

    /// <summary>AI triage run history with results and current statuses.</summary>
    [JsonPropertyName("runHistory")]
    public IReadOnlyList<TriageResearchRunDetail> RunHistory { get; init; } = [];

    /// <summary>Total groups visible to the dashboard query.</summary>
    [JsonPropertyName("totalGroupCount")]
    public int TotalGroupCount { get; init; }

    /// <summary>Total runs visible to the dashboard query.</summary>
    [JsonPropertyName("totalRunCount")]
    public int TotalRunCount { get; init; }
}
