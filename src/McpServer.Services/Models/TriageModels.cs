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
/// FR-TRIAGE-003: Selected reports or groups to move into a new or existing triage group.
/// </summary>
public sealed record TriageGroupSelectionRequest
{
    /// <summary>Selected triage group ids. All reports in each group are moved.</summary>
    public IReadOnlyList<string>? GroupIds { get; init; }

    /// <summary>Selected triage report ids.</summary>
    public IReadOnlyList<string>? ReportIds { get; init; }

    /// <summary>Optional representative title for a newly created group.</summary>
    public string? Title { get; init; }

    /// <summary>Optional representative summary for a newly created group.</summary>
    public string? Summary { get; init; }
}

/// <summary>
/// FR-TRIAGE-003: Result returned after moving or merging triage reports.
/// </summary>
public sealed record TriageGroupEditResult
{
    /// <summary>Target group after the edit.</summary>
    public required TriageGroupDetail Group { get; init; }

    /// <summary>Source group ids deleted because all reports were moved out.</summary>
    public IReadOnlyList<string> RemovedGroupIds { get; init; } = [];

    /// <summary>Number of reports moved into the target group.</summary>
    public int MovedReportCount { get; init; }
}

/// <summary>
/// FR-TRIAGE-001: AI triage research run detail for Director and MCP Web dashboards.
/// </summary>
public sealed record TriageResearchRunDetail
{
    /// <summary>Durable research run id.</summary>
    public required string RunId { get; init; }

    /// <summary>Triage group id researched by this run.</summary>
    public required string GroupId { get; init; }

    /// <summary>Current run status.</summary>
    public required string Status { get; init; }

    /// <summary>Effective workspace path that owns the run.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Current group status when the group is available.</summary>
    public string? GroupStatus { get; init; }

    /// <summary>Representative group title when the group is available.</summary>
    public string? GroupTitle { get; init; }

    /// <summary>Representative group summary when the group is available.</summary>
    public string? GroupSummary { get; init; }

    /// <summary>Number of reports in the researched group.</summary>
    public int ReportCount { get; init; }

    /// <summary>Prompt template id used for this run.</summary>
    public string? PromptTemplateId { get; init; }

    /// <summary>Rendered prompt sent to the triage agent.</summary>
    public string? Prompt { get; init; }

    /// <summary>Serialized group JSON supplied to the triage agent.</summary>
    public string? GroupJson { get; init; }

    /// <summary>Raw agent output.</summary>
    public string? RawOutput { get; init; }

    /// <summary>Raw stdout stream captured from the launched triage agent process.</summary>
    public string? AgentStdout { get; init; }

    /// <summary>Raw stderr stream captured from the launched triage agent process.</summary>
    public string? AgentStderr { get; init; }

    /// <summary>Launched triage agent process exit code, when available.</summary>
    public int? AgentExitCode { get; init; }

    /// <summary>Schema-valid agent JSON after validation.</summary>
    public string? ResponseJson { get; init; }

    /// <summary>Failure text when the run failed.</summary>
    public string? Error { get; init; }

    /// <summary>Created TODO id, if any.</summary>
    public string? CreatedTodoId { get; init; }

    /// <summary>UTC timestamp when the run started.</summary>
    public DateTimeOffset StartedUtc { get; init; }

    /// <summary>UTC timestamp when the run completed.</summary>
    public DateTimeOffset? CompletedUtc { get; init; }
}

/// <summary>
/// FR-TRIAGE-001: Query result for AI triage run history.
/// </summary>
public sealed record TriageRunQueryResult
{
    /// <summary>Matching triage research runs.</summary>
    public IReadOnlyList<TriageResearchRunDetail> Items { get; init; } = [];

    /// <summary>Total matching runs.</summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// FR-TRIAGE-002: TODO created from a triage group or research run.
/// </summary>
public sealed record TriageCreatedTodoDetail
{
    /// <summary>Canonical TODO identifier created by triage.</summary>
    public required string TodoId { get; init; }

    /// <summary>Persisted UTC timestamp for the triage run that created the TODO.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Workspace path that owns the triage-created TODO.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Triage group that produced the TODO, when available.</summary>
    public string? GroupId { get; init; }

    /// <summary>Research run that produced the TODO, when available.</summary>
    public string? RunId { get; init; }

    /// <summary>Current triage group status, when available.</summary>
    public string? GroupStatus { get; init; }

    /// <summary>Current research run status, when available.</summary>
    public string? RunStatus { get; init; }

    /// <summary>Representative group title, when available.</summary>
    public string? GroupTitle { get; init; }

    /// <summary>Representative group summary, when available.</summary>
    public string? GroupSummary { get; init; }

    /// <summary>Number of reports attached to the group, when available.</summary>
    public int ReportCount { get; init; }

    /// <summary>Current quiet deadline for the group, when available.</summary>
    public DateTimeOffset? QuietDeadlineUtc { get; init; }
}

/// <summary>
/// FR-TRIAGE-002: Query result for TODOs created by triage.
/// </summary>
public sealed record TriageCreatedTodoQueryResult
{
    /// <summary>Matching triage-created TODOs with creation timestamps.</summary>
    public IReadOnlyList<TriageCreatedTodoDetail> Items { get; init; } = [];

    /// <summary>Total matching triage-created TODOs.</summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// FR-TRIAGE-001: Read-only dashboard state for triage queue, report-group queue, and run history.
/// </summary>
public sealed record TriageDashboardResult
{
    /// <summary>Groups still collecting or waiting for their quiet window.</summary>
    public IReadOnlyList<TriageGroupDetail> TriageQueue { get; init; } = [];

    /// <summary>Groups ready for or currently in report-group processing.</summary>
    public IReadOnlyList<TriageGroupDetail> ReportGroupQueue { get; init; } = [];

    /// <summary>AI triage run history with results and current statuses.</summary>
    public IReadOnlyList<TriageResearchRunDetail> RunHistory { get; init; } = [];

    /// <summary>Total groups visible to the dashboard query.</summary>
    public int TotalGroupCount { get; init; }

    /// <summary>Total runs visible to the dashboard query.</summary>
    public int TotalRunCount { get; init; }
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
    public TimeSpan MaxRunTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Prompt template id used to render research prompts.</summary>
    public string PromptTemplateId { get; set; } = "triage-research-bug-report";

    /// <summary>Direct triage agent name.</summary>
    public string? AgentName { get; set; } = "triage";

    /// <summary>Direct triage agent executable path.</summary>
    public string? AgentPath { get; set; } = "grok";

    /// <summary>Direct triage agent model id.</summary>
    public string AgentModel { get; set; } = "auto";

    /// <summary>Primary triage agent execution strategy.</summary>
    public string ExecutionStrategy { get; set; } = AgentExecutionStrategyNames.GrokCli;

    /// <summary>Additional environment variables passed to the primary agent.</summary>
    public Dictionary<string, string> AgentParameters { get; set; } = [];

    /// <summary>
    /// TR-MCP-TRIAGE-006: Secondary triage strategy tried when the primary agent fails with a
    /// retryable API error (4xx/rate-limit/unavailable) or times out. Default: claude.
    /// </summary>
    public TriageFallbackAgent? Secondary { get; set; } = new() { AgentName = "triage-claude", AgentPath = "claude" };

    /// <summary>
    /// TR-MCP-TRIAGE-006: Tertiary triage strategy tried when the secondary agent also fails with a
    /// retryable API error or times out. Default: none.
    /// </summary>
    public TriageFallbackAgent? Tertiary { get; set; }

    /// <summary>
    /// TR-MCP-TRIAGE-006: Case-insensitive substrings in agent stderr/body that mark a retryable API
    /// failure and advance the primary -&gt; secondary -&gt; tertiary chain.
    /// </summary>
    public List<string> FallbackTriggerSignals { get; set; } =
    [
        "429", "rate limit", "rate-limit", "too many requests", "quota", "insufficient_quota",
        "overloaded", "unavailable", "503", "529", "capacity", "401", "403",
    ];

    /// <summary>TR-MCP-TRIAGE-006: When true, a run that times out also advances to the next strategy.</summary>
    public bool FallbackOnTimeout { get; set; } = true;
}

/// <summary>
/// TR-MCP-TRIAGE-006: A single fallback triage strategy tier (secondary or tertiary) in the retry chain.
/// </summary>
public sealed class TriageFallbackAgent
{
    /// <summary>Execution strategy name. Default one-shot CLI, which natively supports grok and claude.</summary>
    public string ExecutionStrategy { get; set; } = AgentExecutionStrategyNames.OneShotCli;

    /// <summary>Fallback agent executable path (for example grok or claude). Blank disables this tier.</summary>
    public string? AgentPath { get; set; }

    /// <summary>Fallback agent model id.</summary>
    public string AgentModel { get; set; } = "auto";

    /// <summary>Optional fallback agent identity name.</summary>
    public string? AgentName { get; set; }

    /// <summary>Additional environment variables passed to the fallback agent.</summary>
    public Dictionary<string, string> AgentParameters { get; set; } = [];
}

/// <summary>
/// TR-MCP-TRIAGE-003: Incremental output emitted by the configured direct triage agent.
/// </summary>
public sealed record TriageResearchOutputUpdate(string StreamName, string Text);

/// <summary>
/// TR-MCP-TRIAGE-003: Request passed to the configured direct triage agent.
/// </summary>
public sealed record TriageResearchRequest(
    TriageGroupDetail Group,
    string GroupJson,
    string Prompt,
    string WorkspacePath,
    Func<TriageResearchOutputUpdate, Task>? OutputReceivedAsync = null);

/// <summary>
/// TR-MCP-TRIAGE-003: Raw direct-agent research result.
/// </summary>
public sealed record TriageResearchRunResult(
    bool Success,
    string? OutputJson,
    string? Error,
    string? AgentStdout = null,
    string? AgentStderr = null,
    int? AgentExitCode = null);

/// <summary>
/// TR-MCP-TRIAGE-003: Executes background research for a triage group.
/// </summary>
public interface ITriageResearchRunner
{
    /// <summary>Runs the configured triage agent and returns raw JSON output.</summary>
    Task<TriageResearchRunResult> RunAsync(TriageResearchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Result of soft-deleting a triage group and its reports.</summary>
public sealed class TriageGroupDeleteResult
{
    /// <summary>The soft-deleted group id.</summary>
    public required string GroupId { get; init; }

    /// <summary>Number of reports soft-deleted with the group.</summary>
    public int DeletedReportCount { get; init; }

    /// <summary>UTC timestamp when the group was soft-deleted.</summary>
    public required DateTimeOffset DeletedAtUtc { get; init; }
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

    /// <summary>Gets read-only dashboard state for triage queue, report-group queue, and AI run history.</summary>
    Task<TriageDashboardResult> GetDashboardAsync(string? workspacePath = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a triage group by id.</summary>
    Task<TriageGroupDetail> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Queries AI triage research runs by optional status, group, and workspace filters.</summary>
    Task<TriageRunQueryResult> QueryRunsAsync(
        string? status = null,
        string? groupId = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets an AI triage research run by id.</summary>
    Task<TriageResearchRunDetail> GetRunAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>Queries TODO ids created by triage with persisted TODO creation timestamps.</summary>
    Task<TriageCreatedTodoQueryResult> QueryCreatedTodosAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>Forces a group to be ready for research immediately.</summary>
    Task<TriageGroupDetail> FlushGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>Retries a failed group by resetting it to collecting state.</summary>
    Task<TriageGroupDetail> RetryGroupAsync(string groupId, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a triage group and its reports so they no longer appear in queries.</summary>
    Task<TriageGroupDeleteResult> DeleteGroupAsync(string groupId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>Creates a new group from selected triage reports and groups.</summary>
    Task<TriageGroupEditResult> CreateGroupFromSelectionAsync(
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Moves selected triage reports and groups into an existing target group.</summary>
    Task<TriageGroupEditResult> ConsolidateIntoGroupAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Merges selected source groups into an existing target group.</summary>
    Task<TriageGroupEditResult> MergeGroupsAsync(
        string targetGroupId,
        TriageGroupSelectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Processes due groups whose quiet window has expired.</summary>
    Task<TriageSweepResult> ProcessDueGroupsAsync(CancellationToken cancellationToken = default);
}
