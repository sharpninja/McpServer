namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-TRIAGE-001: Workspace-scoped incidental bug report submitted to the triage intake.
/// </summary>
public sealed record TriageReportRequest
{
    /// <summary>Human-readable issue title.</summary>
    public required string Title { get; init; }

    /// <summary>Concise bug summary. Required for every intake path.</summary>
    public required string Summary { get; init; }

    /// <summary>Observed behavior, if known.</summary>
    public string? ObservedBehavior { get; init; }

    /// <summary>Expected behavior, if known.</summary>
    public string? ExpectedBehavior { get; init; }

    /// <summary>Severity label such as low, medium, high, or critical.</summary>
    public string? Severity { get; init; }

    /// <summary>Component, package, plugin, or subsystem associated with the report.</summary>
    public string? Component { get; init; }

    /// <summary>Stable caller-provided dedupe key. When present it dominates grouping.</summary>
    public string? DedupeKey { get; init; }

    /// <summary>Error signature, exception message, or invariant failure text.</summary>
    public string? ErrorSignature { get; init; }

    /// <summary>Paths associated with the bug.</summary>
    public IReadOnlyList<string>? AffectedPaths { get; init; }

    /// <summary>Symbols, methods, endpoints, or commands associated with the bug.</summary>
    public IReadOnlyList<string>? AffectedSymbols { get; init; }

    /// <summary>Evidence snippets, log excerpts, command outputs, or artifact references.</summary>
    public IReadOnlyDictionary<string, string>? Evidence { get; init; }

    /// <summary>Reproduction hints that may help the background triage agent.</summary>
    public IReadOnlyList<string>? ReproductionHints { get; init; }

    /// <summary>Optional tags supplied by the reporting agent.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Agent identity that submitted the report.</summary>
    public string? ReporterAgent { get; init; }

    /// <summary>Session id active when the bug was discovered.</summary>
    public string? SessionId { get; init; }

    /// <summary>Turn id active when the bug was discovered.</summary>
    public string? TurnId { get; init; }

    /// <summary>Active TODO id for context only. Triage must not hijack the current task.</summary>
    public string? CurrentTodoId { get; init; }

    /// <summary>Optional submitting workspace override.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Optional idempotency key for safe duplicate submissions.</summary>
    public string? IdempotencyKey { get; init; }
}

/// <summary>
/// FR-MCP-TRIAGE-001: Immediate accepted queue state returned by triage intake.
/// </summary>
public sealed record TriageReportSubmitResult
{
    /// <summary>Whether intake accepted and persisted the report.</summary>
    public required bool Success { get; init; }

    /// <summary>Validation or persistence error when <see cref="Success"/> is false.</summary>
    public string? Error { get; init; }

    /// <summary>Durable report id.</summary>
    public string ReportId { get; init; } = string.Empty;

    /// <summary>Durable deterministic group id.</summary>
    public string GroupId { get; init; } = string.Empty;

    /// <summary>Current group status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>UTC quiet deadline after which asynchronous research may run.</summary>
    public DateTimeOffset QuietDeadlineUtc { get; init; }

    /// <summary>Effective workspace that owns the persisted triage group.</summary>
    public string WorkspacePath { get; init; } = string.Empty;
}

/// <summary>
/// FR-MCP-TRIAGE-001: Durable triage report detail returned by status endpoints.
/// </summary>
public sealed record TriageReportDetail
{
    /// <summary>Durable report id.</summary>
    public required string ReportId { get; init; }

    /// <summary>Group id that owns this report.</summary>
    public required string GroupId { get; init; }

    /// <summary>Report status.</summary>
    public required string Status { get; init; }

    /// <summary>Report title.</summary>
    public string? Title { get; init; }

    /// <summary>Report summary.</summary>
    public string? Summary { get; init; }

    /// <summary>Submitting workspace path.</summary>
    public string? OriginalWorkspacePath { get; init; }

    /// <summary>Effective workspace path used for grouping.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>UTC timestamp when the report was persisted.</summary>
    public DateTimeOffset CreatedUtc { get; init; }
}

/// <summary>
/// FR-MCP-TRIAGE-002: Triage group detail returned by group endpoints.
/// </summary>
public sealed record TriageGroupDetail
{
    /// <summary>Durable group id.</summary>
    public required string GroupId { get; init; }

    /// <summary>Group status.</summary>
    public required string Status { get; init; }

    /// <summary>Number of reports in the group.</summary>
    public required int ReportCount { get; init; }

    /// <summary>Effective workspace path that owns the group.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Representative group title.</summary>
    public string? Title { get; init; }

    /// <summary>Representative group summary.</summary>
    public string? Summary { get; init; }

    /// <summary>UTC quiet deadline after which asynchronous research may run.</summary>
    public DateTimeOffset QuietDeadlineUtc { get; init; }

    /// <summary>Created backlog TODO id when research succeeded.</summary>
    public string? CreatedTodoId { get; init; }

    /// <summary>Inspectable failure text from the latest failed research attempt.</summary>
    public string? LastError { get; init; }

    /// <summary>Reports attached to the group.</summary>
    public IReadOnlyList<TriageReportDetail> Reports { get; init; } = [];
}

/// <summary>
/// FR-MCP-TRIAGE-002: Query result for triage group listing.
/// </summary>
public sealed record TriageGroupQueryResult
{
    /// <summary>Matching triage groups.</summary>
    public IReadOnlyList<TriageGroupDetail> Items { get; init; } = [];

    /// <summary>Total matching groups.</summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// FR-MCP-TRIAGE-002: Result of a background triage sweep.
/// </summary>
public sealed record TriageSweepResult(int ProcessedGroups);

/// <summary>
/// TR-MCP-TRIAGE-003: Configurable triage worker and direct-agent settings.
/// </summary>
public sealed class TriageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Triage";

    /// <summary>Quiet period before a group is researched.</summary>
    public TimeSpan QuietPeriod { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Background sweep interval.</summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Maximum duration for a single research run.</summary>
    public TimeSpan MaxRunTime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Prompt template id used to render research prompts.</summary>
    public string PromptTemplateId { get; set; } = "triage-research-bug-report";

    /// <summary>Direct triage agent name.</summary>
    public string? AgentName { get; set; }

    /// <summary>Direct triage agent executable path.</summary>
    public string? AgentPath { get; set; }

    /// <summary>Direct triage agent model id.</summary>
    public string AgentModel { get; set; } = "gpt-5.3-codex";

    /// <summary>Direct triage agent execution strategy.</summary>
    public string ExecutionStrategy { get; set; } = "copilot-cli";

    /// <summary>Additional environment variables passed to the direct agent.</summary>
    public Dictionary<string, string> AgentParameters { get; set; } = [];
}

/// <summary>
/// TR-MCP-TRIAGE-003: Request passed to the configured direct triage agent.
/// </summary>
public sealed record TriageResearchRequest(
    TriageGroupDetail Group,
    string GroupJson,
    string Prompt,
    string WorkspacePath);

/// <summary>
/// TR-MCP-TRIAGE-003: Raw direct-agent research result.
/// </summary>
public sealed record TriageResearchRunResult(bool Success, string? OutputJson, string? Error);

/// <summary>
/// TR-MCP-TRIAGE-003: Executes background research for a triage group.
/// </summary>
public interface ITriageResearchRunner
{
    /// <summary>Runs the configured triage agent and returns raw JSON output.</summary>
    Task<TriageResearchRunResult> RunAsync(TriageResearchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// FR-MCP-TRIAGE-001..003: Service contract shared by REST, MCP tools, worker, and tests.
/// </summary>
public interface ITriageService
{
    /// <summary>Submits an incidental bug report and returns accepted queue state immediately.</summary>
    Task<TriageReportSubmitResult> SubmitReportAsync(TriageReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a persisted triage report by id.</summary>
    Task<TriageReportDetail> GetReportAsync(string reportId, CancellationToken cancellationToken = default);

    /// <summary>Queries triage groups.</summary>
    Task<TriageGroupQueryResult> QueryGroupsAsync(string? status = null, string? workspacePath = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a triage group by id.</summary>
    Task<TriageGroupDetail> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Forces a group to be ready for research immediately.</summary>
    Task<TriageGroupDetail> FlushGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Retries a failed group by resetting it to collecting state.</summary>
    Task<TriageGroupDetail> RetryGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Processes due groups whose quiet window has expired.</summary>
    Task<TriageSweepResult> ProcessDueGroupsAsync(CancellationToken cancellationToken = default);
}
