using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>A flattened TODO item.</summary>
public sealed class TodoFlatItem
{
    /// <summary>Unique item identifier (e.g. MVP-APP-001).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Item title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Section key (e.g. mvp-app).</summary>
    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    /// <summary>Priority: high, medium, or low.</summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    /// <summary>Whether the item is complete.</summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; }

    /// <summary>Time estimate.</summary>
    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    /// <summary>Free-text note.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Description lines.</summary>
    [JsonPropertyName("description")]
    public IReadOnlyList<string>? Description { get; set; }

    /// <summary>Technical detail lines.</summary>
    [JsonPropertyName("technicalDetails")]
    public IReadOnlyList<string>? TechnicalDetails { get; set; }

    /// <summary>Implementation task checklist.</summary>
    [JsonPropertyName("implementationTasks")]
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }

    /// <summary>Date completed (ISO 8601).</summary>
    [JsonPropertyName("completedDate")]
    public string? CompletedDate { get; set; }

    /// <summary>Summary written on completion.</summary>
    [JsonPropertyName("doneSummary")]
    public string? DoneSummary { get; set; }

    /// <summary>Remaining work description.</summary>
    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    /// <summary>Priority note / justification.</summary>
    [JsonPropertyName("priorityNote")]
    public string? PriorityNote { get; set; }

    /// <summary>External reference link.</summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    /// <summary>Code-review phase label for remediation items.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>IDs of items this depends on.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<string>? DependsOn { get; set; }

    /// <summary>Associated functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Associated technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
}

/// <summary>A sub-task within a TODO item.</summary>
public sealed class TodoFlatTask
{
    /// <summary>Task description.</summary>
    [JsonPropertyName("task")]
    public string Task { get; set; } = string.Empty;

    /// <summary>Whether the sub-task is done.</summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

/// <summary>Request to create a TODO item.</summary>
public sealed class TodoCreateRequest
{
    /// <summary>Unique item identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Item title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Section key.</summary>
    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    /// <summary>Priority: high, medium, or low.</summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    /// <summary>Time estimate.</summary>
    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    /// <summary>Description lines.</summary>
    [JsonPropertyName("description")]
    public IReadOnlyList<string>? Description { get; set; }

    /// <summary>Technical detail lines.</summary>
    [JsonPropertyName("technicalDetails")]
    public IReadOnlyList<string>? TechnicalDetails { get; set; }

    /// <summary>Implementation task checklist.</summary>
    [JsonPropertyName("implementationTasks")]
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }

    /// <summary>Free-text note.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Remaining work description.</summary>
    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    /// <summary>Code-review phase label.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>IDs of items this depends on.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<string>? DependsOn { get; set; }

    /// <summary>Associated functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Associated technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
}

/// <summary>Request to update a TODO item.</summary>
public sealed class TodoUpdateRequest
{
    /// <summary>Updated title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Updated priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Updated section.</summary>
    [JsonPropertyName("section")]
    public string? Section { get; set; }

    /// <summary>Mark done or not-done.</summary>
    [JsonPropertyName("done")]
    public bool? Done { get; set; }

    /// <summary>Updated estimate.</summary>
    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    /// <summary>Updated description lines.</summary>
    [JsonPropertyName("description")]
    public IReadOnlyList<string>? Description { get; set; }

    /// <summary>Updated technical details.</summary>
    [JsonPropertyName("technicalDetails")]
    public IReadOnlyList<string>? TechnicalDetails { get; set; }

    /// <summary>Updated implementation tasks.</summary>
    [JsonPropertyName("implementationTasks")]
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }

    /// <summary>Updated note.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Completion date.</summary>
    [JsonPropertyName("completedDate")]
    public string? CompletedDate { get; set; }

    /// <summary>Completion summary.</summary>
    [JsonPropertyName("doneSummary")]
    public string? DoneSummary { get; set; }

    /// <summary>Remaining work.</summary>
    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    /// <summary>Updated reference text.</summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    /// <summary>Updated code-review phase label.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>Dependency IDs.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<string>? DependsOn { get; set; }

    /// <summary>Associated functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Associated technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
}

/// <summary>Request to move a TODO item to another registered workspace.</summary>
public sealed class TodoMoveRequest
{
    /// <summary>Absolute path of the target workspace.</summary>
    [JsonPropertyName("targetWorkspacePath")]
    public string TargetWorkspacePath { get; set; } = string.Empty;
}

/// <summary>Result of a TODO query.</summary>
public sealed class TodoQueryResult
{
    /// <summary>Matching items.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<TodoFlatItem> Items { get; set; } = [];

    /// <summary>Total count of matching items.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Result of a TODO mutation (create/update/delete).</summary>
public sealed class TodoMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>The affected item (on success).</summary>
    [JsonPropertyName("item")]
    public TodoFlatItem? Item { get; set; }

    /// <summary>Structured failure classification for partial or failed mutations.</summary>
    [JsonPropertyName("failureKind")]
    public TodoMutationFailureKind FailureKind { get; set; }
}

/// <summary>Classifies the failure mode of a TODO mutation.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoMutationFailureKind
{
    /// <summary>No failure classification applies.</summary>
    None = 0,

    /// <summary>The request content was invalid.</summary>
    Validation = 1,

    /// <summary>The request conflicted with existing state.</summary>
    Conflict = 2,

    /// <summary>The target TODO item was not found.</summary>
    NotFound = 3,

    /// <summary>The authoritative database mutation succeeded but TODO.yaml projection failed.</summary>
    ProjectionFailed = 4,

    /// <summary>An external dependency failed after the local state changed.</summary>
    ExternalSyncFailed = 5,
}

/// <summary>Result of querying TODO audit history.</summary>
public sealed class TodoAuditQueryResult
{
    /// <summary>Audit entries ordered by TODO version.</summary>
    [JsonPropertyName("entries")]
    public IReadOnlyList<TodoAuditEntry> Entries { get; set; } = [];

    /// <summary>Total matching audit entry count before pagination.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Status of SQLite-authoritative TODO.yaml projection health and consistency.</summary>
public sealed class TodoProjectionStatusResult
{
    /// <summary>Authoritative storage provider name.</summary>
    [JsonPropertyName("authoritativeStore")]
    public string AuthoritativeStore { get; set; } = string.Empty;

    /// <summary>Absolute path to the authoritative SQLite data source.</summary>
    [JsonPropertyName("authoritativeDataSource")]
    public string AuthoritativeDataSource { get; set; } = string.Empty;

    /// <summary>Absolute path to the projected TODO.yaml file.</summary>
    [JsonPropertyName("projectionTargetPath")]
    public string ProjectionTargetPath { get; set; } = string.Empty;

    /// <summary>Whether the projection target currently exists as a file.</summary>
    [JsonPropertyName("projectionTargetExists")]
    public bool ProjectionTargetExists { get; set; }

    /// <summary>Whether the projected TODO.yaml content matches authoritative SQLite state.</summary>
    [JsonPropertyName("projectionConsistent")]
    public bool ProjectionConsistent { get; set; }

    /// <summary>Whether operator repair is currently required.</summary>
    [JsonPropertyName("repairRequired")]
    public bool RepairRequired { get; set; }

    /// <summary>UTC timestamp when the consistency check was performed.</summary>
    [JsonPropertyName("verifiedAtUtc")]
    public string VerifiedAtUtc { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the last YAML import into SQLite, when known.</summary>
    [JsonPropertyName("lastImportedFromYamlUtc")]
    public string? LastImportedFromYamlUtc { get; set; }

    /// <summary>UTC timestamp of the last successful projection from SQLite to YAML, when known.</summary>
    [JsonPropertyName("lastProjectedToYamlUtc")]
    public string? LastProjectedToYamlUtc { get; set; }

    /// <summary>UTC timestamp of the last recorded projection failure, when known.</summary>
    [JsonPropertyName("lastProjectionFailureUtc")]
    public string? LastProjectionFailureUtc { get; set; }

    /// <summary>Last recorded projection failure message, when known.</summary>
    [JsonPropertyName("lastProjectionFailure")]
    public string? LastProjectionFailure { get; set; }

    /// <summary>Human-readable projection status summary.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>Result of an operator-requested TODO.yaml projection repair attempt.</summary>
public sealed class TodoProjectionRepairResult
{
    /// <summary>Whether the repair attempt succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message on repair failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Status after the repair attempt completed.</summary>
    [JsonPropertyName("status")]
    public TodoProjectionStatusResult Status { get; set; } = new();
}

/// <summary>Append-only audit entry for a TODO item.</summary>
public sealed class TodoAuditEntry
{
    /// <summary>Monotonic audit row identifier.</summary>
    [JsonPropertyName("auditId")]
    public long AuditId { get; set; }

    /// <summary>TODO item identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Monotonic version for this TODO id.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>Recorded action.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the history row was recorded.</summary>
    [JsonPropertyName("recordedAtUtc")]
    public string RecordedAtUtc { get; set; } = string.Empty;

    /// <summary>Post-mutation snapshot.</summary>
    [JsonPropertyName("snapshot")]
    public TodoFlatItem? Snapshot { get; set; }

    /// <summary>Pre-mutation snapshot.</summary>
    [JsonPropertyName("previousSnapshot")]
    public TodoFlatItem? PreviousSnapshot { get; set; }

    /// <summary>Origin of the mutation or backfill operation.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>Result of a requirements analysis.</summary>
public sealed class RequirementsAnalysisResult
{
    /// <summary>Whether the analysis succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Discovered functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Discovered technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }

    /// <summary>Error message on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Raw Copilot response.</summary>
    [JsonPropertyName("copilotResponse")]
    public string? CopilotResponse { get; set; }
}

/// <summary>Byrd execution status for an active TODO.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoExecutionStatus
{
    /// <summary>The TODO exists but has not yet been planned for execution.</summary>
    Draft = 0,

    /// <summary>The TODO is planned and waiting for test design.</summary>
    Planned = 1,

    /// <summary>The TODO is in test-design work.</summary>
    TestDesign = 2,

    /// <summary>The TODO has a defined test plan and is ready for implementation.</summary>
    TestReady = 3,

    /// <summary>The TODO is being implemented.</summary>
    Implementing = 4,

    /// <summary>The TODO is in validation.</summary>
    Validating = 5,

    /// <summary>The TODO is blocked.</summary>
    Blocked = 6,

    /// <summary>The TODO is complete.</summary>
    Complete = 7,

    /// <summary>The TODO has been cancelled.</summary>
    Cancelled = 8,
}

/// <summary>Byrd execution priority for an active TODO.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoExecutionPriority
{
    /// <summary>Low execution priority.</summary>
    Low = 0,

    /// <summary>Normal execution priority.</summary>
    Medium = 1,

    /// <summary>High execution priority.</summary>
    High = 2,

    /// <summary>Critical execution priority.</summary>
    Critical = 3,
}

/// <summary>Byrd iteration phase status.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoIterationPhaseStatus
{
    /// <summary>The phase is in planning.</summary>
    Planning = 0,

    /// <summary>The phase is in implementation.</summary>
    Implementing = 1,

    /// <summary>The phase is in validation.</summary>
    Validating = 2,

    /// <summary>The phase is complete.</summary>
    Complete = 3,

    /// <summary>The phase is blocked.</summary>
    Blocked = 4,

    /// <summary>The phase is cancelled.</summary>
    Cancelled = 5,
}

/// <summary>Kinds of checkpoints that can be recorded against a Byrd TODO.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoCheckpointKind
{
    /// <summary>Planning decision checkpoint.</summary>
    PlanningDecision = 0,

    /// <summary>Test definition checkpoint.</summary>
    TestDefined = 1,

    /// <summary>Passing test checkpoint.</summary>
    TestPassing = 2,

    /// <summary>Implementation progress checkpoint.</summary>
    ImplementationProgress = 3,

    /// <summary>Successful validation checkpoint.</summary>
    ValidationPassed = 4,

    /// <summary>Failed validation checkpoint.</summary>
    ValidationFailed = 5,

    /// <summary>Blocker checkpoint.</summary>
    Blocker = 6,

    /// <summary>Device validation checkpoint.</summary>
    DeviceValidation = 7,

    /// <summary>Commit checkpoint.</summary>
    CommitCreated = 8,

    /// <summary>Requirement refinement checkpoint.</summary>
    RequirementRefined = 9,
}

/// <summary>Safe ADB actions supported by the MCP server.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdbStepAction
{
    /// <summary>Capture a screenshot.</summary>
    Screenshot = 0,

    /// <summary>Tap a point on the screen.</summary>
    Tap = 1,

    /// <summary>Swipe across the screen.</summary>
    Swipe = 2,

    /// <summary>Send text input.</summary>
    Text = 3,

    /// <summary>Send a key event.</summary>
    Keyevent = 4,

    /// <summary>Wait without interacting with the device.</summary>
    Wait = 5,

    /// <summary>Launch an Android application.</summary>
    LaunchApp = 6,

    /// <summary>Return the current focus information.</summary>
    GetFocus = 7,
}

/// <summary>Acceptance criterion attached to an execution TODO.</summary>
public sealed class AcceptanceCriterion
{
    /// <summary>Unique criterion identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Criterion text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Whether the criterion is currently satisfied.</summary>
    [JsonPropertyName("isSatisfied")]
    public bool IsSatisfied { get; set; }

    /// <summary>Optional evidence for the criterion.</summary>
    [JsonPropertyName("evidence")]
    public string? Evidence { get; set; }
}

/// <summary>Constraint attached to an execution TODO.</summary>
public sealed class TodoConstraint
{
    /// <summary>Unique constraint identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Constraint text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional source of the constraint.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>Dependency relationship between execution TODOs.</summary>
public sealed class TodoDependency
{
    /// <summary>Dependent TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Reason for the dependency.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Test plan attached to an execution TODO.</summary>
public sealed class TodoTestPlan
{
    /// <summary>Whether unit tests are defined.</summary>
    [JsonPropertyName("unitTestsDefined")]
    public bool UnitTestsDefined { get; set; }

    /// <summary>Whether unit tests are passing.</summary>
    [JsonPropertyName("unitTestsPassing")]
    public bool UnitTestsPassing { get; set; }

    /// <summary>Whether integration tests are defined.</summary>
    [JsonPropertyName("integrationTestsDefined")]
    public bool IntegrationTestsDefined { get; set; }

    /// <summary>Whether integration tests are passing.</summary>
    [JsonPropertyName("integrationTestsPassing")]
    public bool IntegrationTestsPassing { get; set; }

    /// <summary>Test file paths.</summary>
    [JsonPropertyName("testFilePaths")]
    public IReadOnlyList<string> TestFilePaths { get; set; } = [];

    /// <summary>Test commands.</summary>
    [JsonPropertyName("testCommands")]
    public IReadOnlyList<string> TestCommands { get; set; } = [];
}

/// <summary>Validation state attached to an execution TODO.</summary>
public sealed class TodoValidationState
{
    /// <summary>Last validation result.</summary>
    [JsonPropertyName("lastResult")]
    public string LastResult { get; set; } = "not_run";

    /// <summary>UTC timestamp of the last validation.</summary>
    [JsonPropertyName("lastValidatedAtUtc")]
    public string? LastValidatedAtUtc { get; set; }

    /// <summary>Validation artifact identifiers.</summary>
    [JsonPropertyName("validationArtifactIds")]
    public IReadOnlyList<string> ValidationArtifactIds { get; set; } = [];

    /// <summary>Validation summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

/// <summary>Pointers that help the agent resume an execution TODO.</summary>
public sealed class TodoExecutionPointers
{
    /// <summary>Last relevant session turn identifier.</summary>
    [JsonPropertyName("lastRelevantTurnId")]
    public string? LastRelevantTurnId { get; set; }

    /// <summary>Last successful session turn identifier.</summary>
    [JsonPropertyName("lastSuccessfulTurnId")]
    public string? LastSuccessfulTurnId { get; set; }

    /// <summary>Last failed session turn identifier.</summary>
    [JsonPropertyName("lastFailedTurnId")]
    public string? LastFailedTurnId { get; set; }

    /// <summary>Last checkpoint identifier.</summary>
    [JsonPropertyName("lastCheckpointId")]
    public string? LastCheckpointId { get; set; }

    /// <summary>Last commit SHA.</summary>
    [JsonPropertyName("lastCommitSha")]
    public string? LastCommitSha { get; set; }

    /// <summary>Last screenshot artifact identifier.</summary>
    [JsonPropertyName("lastScreenshotArtifactId")]
    public string? LastScreenshotArtifactId { get; set; }
}

/// <summary>Stored iteration phase record.</summary>
public sealed class TodoIterationPhase
{
    /// <summary>Phase identifier.</summary>
    [JsonPropertyName("phaseId")]
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Phase name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Phase summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Phase status.</summary>
    [JsonPropertyName("status")]
    public TodoIterationPhaseStatus Status { get; set; }

    /// <summary>Linked requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; set; } = [];

    /// <summary>Execution TODO identifiers in the phase.</summary>
    [JsonPropertyName("todoIds")]
    public IReadOnlyList<string> TodoIds { get; set; } = [];

    /// <summary>Entry criteria.</summary>
    [JsonPropertyName("entryCriteria")]
    public IReadOnlyList<string> EntryCriteria { get; set; } = [];

    /// <summary>Exit criteria.</summary>
    [JsonPropertyName("exitCriteria")]
    public IReadOnlyList<string> ExitCriteria { get; set; } = [];

    /// <summary>Originating plan identifier.</summary>
    [JsonPropertyName("createdFromPlanId")]
    public string? CreatedFromPlanId { get; set; }

    /// <summary>Branch name associated with the phase.</summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    /// <summary>UTC creation time.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>UTC update time.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; set; } = string.Empty;
}

/// <summary>Stored checkpoint record for an execution TODO.</summary>
public sealed class TodoCheckpoint
{
    /// <summary>Checkpoint identifier.</summary>
    [JsonPropertyName("checkpointId")]
    public string CheckpointId { get; set; } = string.Empty;

    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Checkpoint kind.</summary>
    [JsonPropertyName("kind")]
    public TodoCheckpointKind Kind { get; set; }

    /// <summary>Checkpoint summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; set; }

    /// <summary>Requirement identifiers linked to the checkpoint.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; set; } = [];

    /// <summary>Session turn identifiers linked to the checkpoint.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string> SessionTurnIds { get; set; } = [];

    /// <summary>Artifact identifiers linked to the checkpoint.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string> ArtifactIds { get; set; } = [];

    /// <summary>Commit SHAs linked to the checkpoint.</summary>
    [JsonPropertyName("commitShas")]
    public IReadOnlyList<string> CommitShas { get; set; } = [];

    /// <summary>UTC creation time.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string CreatedAtUtc { get; set; } = string.Empty;
}

/// <summary>Stored execution TODO record.</summary>
public sealed class TodoExecutionRecord
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>TODO title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Execution goal.</summary>
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    /// <summary>TODO summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; set; }

    /// <summary>Execution priority.</summary>
    [JsonPropertyName("priority")]
    public TodoExecutionPriority Priority { get; set; }

    /// <summary>Iteration phase identifier.</summary>
    [JsonPropertyName("iterationPhaseId")]
    public string? IterationPhaseId { get; set; }

    /// <summary>Parent TODO identifier.</summary>
    [JsonPropertyName("parentTodoId")]
    public string? ParentTodoId { get; set; }

    /// <summary>Child TODO identifiers.</summary>
    [JsonPropertyName("childTodoIds")]
    public IReadOnlyList<string> ChildTodoIds { get; set; } = [];

    /// <summary>Dependency list.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<TodoDependency> DependsOn { get; set; } = [];

    /// <summary>Blocking dependency list.</summary>
    [JsonPropertyName("blockedBy")]
    public IReadOnlyList<TodoDependency> BlockedBy { get; set; } = [];

    /// <summary>Acceptance criteria.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria { get; set; } = [];

    /// <summary>Constraints.</summary>
    [JsonPropertyName("constraints")]
    public IReadOnlyList<TodoConstraint> Constraints { get; set; } = [];

    /// <summary>Linked requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; set; } = [];

    /// <summary>Relevant file paths.</summary>
    [JsonPropertyName("relevantFiles")]
    public IReadOnlyList<string> RelevantFiles { get; set; } = [];

    /// <summary>Linked artifact identifiers.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string> ArtifactIds { get; set; } = [];

    /// <summary>Linked session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string> SessionTurnIds { get; set; } = [];

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; set; }

    /// <summary>Current test plan.</summary>
    [JsonPropertyName("testPlan")]
    public TodoTestPlan TestPlan { get; set; } = new();

    /// <summary>Current validation state.</summary>
    [JsonPropertyName("validation")]
    public TodoValidationState Validation { get; set; } = new();

    /// <summary>Execution pointers.</summary>
    [JsonPropertyName("pointers")]
    public TodoExecutionPointers Pointers { get; set; } = new();

    /// <summary>UTC creation time.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>UTC update time.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; set; } = string.Empty;
}

/// <summary>Bounded execution context for the active Byrd TODO.</summary>
public sealed class ActiveTodoContext
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>TODO title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Execution goal.</summary>
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    /// <summary>TODO summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; set; }

    /// <summary>Iteration phase identifier.</summary>
    [JsonPropertyName("iterationPhaseId")]
    public string? IterationPhaseId { get; set; }

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; set; }

    /// <summary>Linked requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; set; } = [];

    /// <summary>Recent requirement snippets.</summary>
    [JsonPropertyName("recentRequirementSnippets")]
    public IReadOnlyList<string> RecentRequirementSnippets { get; set; } = [];

    /// <summary>Recent turn summaries.</summary>
    [JsonPropertyName("recentTurnSummaries")]
    public IReadOnlyList<string> RecentTurnSummaries { get; set; } = [];

    /// <summary>Relevant file paths.</summary>
    [JsonPropertyName("relevantFiles")]
    public IReadOnlyList<string> RelevantFiles { get; set; } = [];

    /// <summary>Artifact identifiers.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string> ArtifactIds { get; set; } = [];

    /// <summary>Acceptance criteria text.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<string> AcceptanceCriteria { get; set; } = [];

    /// <summary>Constraint text.</summary>
    [JsonPropertyName("constraints")]
    public IReadOnlyList<string> Constraints { get; set; } = [];

    /// <summary>Current test plan.</summary>
    [JsonPropertyName("testPlan")]
    public TodoTestPlan TestPlan { get; set; } = new();

    /// <summary>Current validation state.</summary>
    [JsonPropertyName("validation")]
    public TodoValidationState Validation { get; set; } = new();

    /// <summary>Execution pointers.</summary>
    [JsonPropertyName("pointers")]
    public TodoExecutionPointers Pointers { get; set; } = new();
}

/// <summary>Delta context since a checkpoint.</summary>
public sealed class TodoDeltaContext
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Checkpoint identifier used as the delta baseline.</summary>
    [JsonPropertyName("sinceCheckpointId")]
    public string? SinceCheckpointId { get; set; }

    /// <summary>New session turn identifiers.</summary>
    [JsonPropertyName("newTurnIds")]
    public IReadOnlyList<string> NewTurnIds { get; set; } = [];

    /// <summary>New session turn summaries.</summary>
    [JsonPropertyName("newTurnSummaries")]
    public IReadOnlyList<string> NewTurnSummaries { get; set; } = [];

    /// <summary>New artifact identifiers.</summary>
    [JsonPropertyName("newArtifactIds")]
    public IReadOnlyList<string> NewArtifactIds { get; set; } = [];

    /// <summary>New commit SHAs.</summary>
    [JsonPropertyName("newCommitShas")]
    public IReadOnlyList<string> NewCommitShas { get; set; } = [];

    /// <summary>Updated next action.</summary>
    [JsonPropertyName("updatedNextAction")]
    public string? UpdatedNextAction { get; set; }
}

/// <summary>Request to create a Byrd iteration phase.</summary>
public sealed class CreateIterationPhaseRequest
{
    /// <summary>Phase name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Phase summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Linked requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string>? RequirementIds { get; set; }

    /// <summary>Entry criteria.</summary>
    [JsonPropertyName("entryCriteria")]
    public IReadOnlyList<string>? EntryCriteria { get; set; }

    /// <summary>Exit criteria.</summary>
    [JsonPropertyName("exitCriteria")]
    public IReadOnlyList<string>? ExitCriteria { get; set; }

    /// <summary>Originating plan identifier.</summary>
    [JsonPropertyName("createdFromPlanId")]
    public string? CreatedFromPlanId { get; set; }

    /// <summary>Branch associated with the phase.</summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }
}

/// <summary>Result of creating a Byrd iteration phase.</summary>
public sealed class CreateIterationPhaseResult
{
    /// <summary>Phase identifier.</summary>
    [JsonPropertyName("phaseId")]
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>Phase status.</summary>
    [JsonPropertyName("status")]
    public TodoIterationPhaseStatus Status { get; set; }
}

/// <summary>Plan step input used to create execution TODOs from a plan.</summary>
public sealed class PlanTodoInput
{
    /// <summary>TODO title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Execution goal.</summary>
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    /// <summary>TODO summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Acceptance criteria text.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<string>? AcceptanceCriteria { get; set; }

    /// <summary>Constraint text.</summary>
    [JsonPropertyName("constraints")]
    public IReadOnlyList<string>? Constraints { get; set; }

    /// <summary>Requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string>? RequirementIds { get; set; }

    /// <summary>Relevant file paths.</summary>
    [JsonPropertyName("relevantFiles")]
    public IReadOnlyList<string>? RelevantFiles { get; set; }

    /// <summary>Dependency TODO identifiers.</summary>
    [JsonPropertyName("dependsOnTodoIds")]
    public IReadOnlyList<string>? DependsOnTodoIds { get; set; }
}

/// <summary>Request to create execution TODOs from a plan.</summary>
public sealed class CreateTodosFromPlanRequest
{
    /// <summary>Iteration phase identifier.</summary>
    [JsonPropertyName("phaseId")]
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>Plan identifier.</summary>
    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;

    /// <summary>Planned TODO inputs.</summary>
    [JsonPropertyName("todos")]
    public IReadOnlyList<PlanTodoInput>? Todos { get; set; }
}

/// <summary>Result of creating execution TODOs from a plan.</summary>
public sealed class CreateTodosFromPlanResult
{
    /// <summary>Iteration phase identifier.</summary>
    [JsonPropertyName("phaseId")]
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>Created TODO identifiers.</summary>
    [JsonPropertyName("todoIds")]
    public IReadOnlyList<string> TodoIds { get; set; } = [];
}

/// <summary>Compact result describing the current active TODO.</summary>
public sealed class ActiveTodoResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>TODO title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Current execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; set; }

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; set; }
}

/// <summary>Request to store the test plan for a TODO.</summary>
public sealed class SetTodoTestPlanRequest
{
    /// <summary>Whether unit tests are defined.</summary>
    [JsonPropertyName("unitTestsDefined")]
    public bool UnitTestsDefined { get; set; }

    /// <summary>Whether unit tests are already passing.</summary>
    [JsonPropertyName("unitTestsPassing")]
    public bool? UnitTestsPassing { get; set; }

    /// <summary>Whether integration tests are defined.</summary>
    [JsonPropertyName("integrationTestsDefined")]
    public bool IntegrationTestsDefined { get; set; }

    /// <summary>Whether integration tests are already passing.</summary>
    [JsonPropertyName("integrationTestsPassing")]
    public bool? IntegrationTestsPassing { get; set; }

    /// <summary>Test file paths.</summary>
    [JsonPropertyName("testFilePaths")]
    public IReadOnlyList<string>? TestFilePaths { get; set; }

    /// <summary>Test commands.</summary>
    [JsonPropertyName("testCommands")]
    public IReadOnlyList<string>? TestCommands { get; set; }
}

/// <summary>Result of updating a TODO test plan.</summary>
public sealed class SetTodoTestPlanResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Current execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; set; }
}

/// <summary>Request to update a TODO status.</summary>
public sealed class UpdateTodoStatusRequest
{
    /// <summary>Target execution status.</summary>
    [JsonPropertyName("targetStatus")]
    public TodoExecutionStatus TargetStatus { get; set; }

    /// <summary>Optional transition reason.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>Result of updating a TODO status.</summary>
public sealed class UpdateTodoStatusResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Previous execution status.</summary>
    [JsonPropertyName("previousStatus")]
    public TodoExecutionStatus PreviousStatus { get; set; }

    /// <summary>Current execution status.</summary>
    [JsonPropertyName("currentStatus")]
    public TodoExecutionStatus CurrentStatus { get; set; }
}

/// <summary>Request to append a checkpoint to a TODO.</summary>
public sealed class AppendTodoCheckpointRequest
{
    /// <summary>Checkpoint kind.</summary>
    [JsonPropertyName("kind")]
    public TodoCheckpointKind Kind { get; set; }

    /// <summary>Checkpoint summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; set; }

    /// <summary>Requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string>? RequirementIds { get; set; }

    /// <summary>Session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string>? SessionTurnIds { get; set; }

    /// <summary>Artifact identifiers.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string>? ArtifactIds { get; set; }

    /// <summary>Commit SHAs.</summary>
    [JsonPropertyName("commitShas")]
    public IReadOnlyList<string>? CommitShas { get; set; }
}

/// <summary>Result of appending a checkpoint.</summary>
public sealed class AppendTodoCheckpointResult
{
    /// <summary>Checkpoint identifier.</summary>
    [JsonPropertyName("checkpointId")]
    public string CheckpointId { get; set; } = string.Empty;

    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;
}

/// <summary>Request to record a validation result.</summary>
public sealed class RecordTodoValidationResultRequest
{
    /// <summary>Validation result string.</summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    /// <summary>Validation summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>Artifact identifiers.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string>? ArtifactIds { get; set; }

    /// <summary>Session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string>? SessionTurnIds { get; set; }

    /// <summary>Optional unit-test passing flag.</summary>
    [JsonPropertyName("unitTestsPassing")]
    public bool? UnitTestsPassing { get; set; }

    /// <summary>Optional integration-test passing flag.</summary>
    [JsonPropertyName("integrationTestsPassing")]
    public bool? IntegrationTestsPassing { get; set; }
}

/// <summary>Result of recording a validation result.</summary>
public sealed class RecordTodoValidationResultResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Updated validation state.</summary>
    [JsonPropertyName("validationState")]
    public TodoValidationState ValidationState { get; set; } = new();
}

/// <summary>Request to link session turns to a TODO.</summary>
public sealed class LinkTodoToSessionTurnsRequest
{
    /// <summary>Session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string>? SessionTurnIds { get; set; }
}

/// <summary>Result of linking session turns to a TODO.</summary>
public sealed class LinkTodoToSessionTurnsResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Linked session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string> SessionTurnIds { get; set; } = [];
}

/// <summary>Request to perform a safe ADB step.</summary>
public sealed class AdbStepRequest
{
    /// <summary>Optional device serial.</summary>
    [JsonPropertyName("deviceSerial")]
    public string? DeviceSerial { get; set; }

    /// <summary>ADB action to perform.</summary>
    [JsonPropertyName("action")]
    public AdbStepAction Action { get; set; }

    /// <summary>Whether to capture a screenshot after the action.</summary>
    [JsonPropertyName("captureScreenshot")]
    public bool CaptureScreenshot { get; set; }

    /// <summary>Optional user-facing instruction for the step.</summary>
    [JsonPropertyName("instruction")]
    public string? Instruction { get; set; }

    /// <summary>X coordinate for tap actions.</summary>
    [JsonPropertyName("x")]
    public int? X { get; set; }

    /// <summary>Y coordinate for tap actions.</summary>
    [JsonPropertyName("y")]
    public int? Y { get; set; }

    /// <summary>Swipe start X coordinate.</summary>
    [JsonPropertyName("startX")]
    public int? StartX { get; set; }

    /// <summary>Swipe start Y coordinate.</summary>
    [JsonPropertyName("startY")]
    public int? StartY { get; set; }

    /// <summary>Swipe end X coordinate.</summary>
    [JsonPropertyName("endX")]
    public int? EndX { get; set; }

    /// <summary>Swipe end Y coordinate.</summary>
    [JsonPropertyName("endY")]
    public int? EndY { get; set; }

    /// <summary>Optional duration in milliseconds.</summary>
    [JsonPropertyName("durationMs")]
    public int? DurationMs { get; set; }

    /// <summary>Text payload for text-input actions.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Key event for keyevent actions.</summary>
    [JsonPropertyName("keyEvent")]
    public string? KeyEvent { get; set; }

    /// <summary>Package name for app-launch actions.</summary>
    [JsonPropertyName("packageName")]
    public string? PackageName { get; set; }

    /// <summary>Activity name for explicit component launch actions.</summary>
    [JsonPropertyName("activityName")]
    public string? ActivityName { get; set; }

    /// <summary>Wait duration in milliseconds.</summary>
    [JsonPropertyName("waitMilliseconds")]
    public int? WaitMilliseconds { get; set; }
}

/// <summary>Result of performing a safe ADB step.</summary>
public sealed class AdbStepResult
{
    /// <summary>Whether the action succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>ADB action performed.</summary>
    [JsonPropertyName("action")]
    public AdbStepAction Action { get; set; }

    /// <summary>Resolved device serial.</summary>
    [JsonPropertyName("deviceSerial")]
    public string? DeviceSerial { get; set; }

    /// <summary>Command summary.</summary>
    [JsonPropertyName("commandSummary")]
    public string? CommandSummary { get; set; }

    /// <summary>Captured screenshot path, when available.</summary>
    [JsonPropertyName("screenshotPath")]
    public string? ScreenshotPath { get; set; }

    /// <summary>Optional screenshot base64 payload.</summary>
    [JsonPropertyName("screenshotBase64")]
    public string? ScreenshotBase64 { get; set; }

    /// <summary>Current Android focus string.</summary>
    [JsonPropertyName("currentFocus")]
    public string? CurrentFocus { get; set; }

    /// <summary>Observation hints gathered from the step.</summary>
    [JsonPropertyName("observationHints")]
    public IReadOnlyList<string> ObservationHints { get; set; } = [];

    /// <summary>Error message when the action fails.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>UTC timestamp of the step result.</summary>
    [JsonPropertyName("timestampUtc")]
    public string TimestampUtc { get; set; } = string.Empty;
}
