using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Models;

/// <summary>
/// Byrd execution status for an active TODO.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TodoExecutionStatus>))]
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

/// <summary>
/// Byrd execution priority for an active TODO.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TodoExecutionPriority>))]
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

/// <summary>
/// Byrd iteration phase status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TodoIterationPhaseStatus>))]
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

/// <summary>
/// Kinds of checkpoints that can be recorded against a Byrd TODO.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TodoCheckpointKind>))]
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

/// <summary>
/// Safe ADB actions supported by the MCP server.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AdbStepAction>))]
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

/// <summary>
/// Acceptance criterion attached to an execution TODO.
/// </summary>
public sealed record AcceptanceCriterion
{
    /// <summary>Unique criterion identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Criterion text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    /// <summary>Whether the criterion is currently satisfied.</summary>
    [JsonPropertyName("isSatisfied")]
    public bool IsSatisfied { get; init; }

    /// <summary>Optional evidence for the criterion.</summary>
    [JsonPropertyName("evidence")]
    public string? Evidence { get; init; }
}

/// <summary>
/// Constraint attached to an execution TODO.
/// </summary>
public sealed record TodoConstraint
{
    /// <summary>Unique constraint identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Constraint text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    /// <summary>Optional source of the constraint.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }
}

/// <summary>
/// Dependency relationship between execution TODOs.
/// </summary>
public sealed record TodoDependency
{
    /// <summary>Dependent TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Reason for the dependency.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Test plan attached to an execution TODO.
/// </summary>
public sealed record TodoTestPlan
{
    /// <summary>Whether unit tests are defined.</summary>
    [JsonPropertyName("unitTestsDefined")]
    public bool UnitTestsDefined { get; init; }

    /// <summary>Whether unit tests are passing.</summary>
    [JsonPropertyName("unitTestsPassing")]
    public bool UnitTestsPassing { get; init; }

    /// <summary>Whether integration tests are defined.</summary>
    [JsonPropertyName("integrationTestsDefined")]
    public bool IntegrationTestsDefined { get; init; }

    /// <summary>Whether integration tests are passing.</summary>
    [JsonPropertyName("integrationTestsPassing")]
    public bool IntegrationTestsPassing { get; init; }

    /// <summary>Test file paths.</summary>
    [JsonPropertyName("testFilePaths")]
    public IReadOnlyList<string> TestFilePaths { get; init; } = [];

    /// <summary>Test commands.</summary>
    [JsonPropertyName("testCommands")]
    public IReadOnlyList<string> TestCommands { get; init; } = [];
}

/// <summary>
/// Validation state attached to an execution TODO.
/// </summary>
public sealed record TodoValidationState
{
    /// <summary>Last validation result.</summary>
    [JsonPropertyName("lastResult")]
    public string LastResult { get; init; } = "not_run";

    /// <summary>UTC timestamp of the last validation.</summary>
    [JsonPropertyName("lastValidatedAtUtc")]
    public string? LastValidatedAtUtc { get; init; }

    /// <summary>Validation artifact identifiers.</summary>
    [JsonPropertyName("validationArtifactIds")]
    public IReadOnlyList<string> ValidationArtifactIds { get; init; } = [];

    /// <summary>Validation summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }
}

/// <summary>
/// Pointers that help the agent resume an execution TODO.
/// </summary>
public sealed record TodoExecutionPointers
{
    /// <summary>Last relevant session turn identifier.</summary>
    [JsonPropertyName("lastRelevantTurnId")]
    public string? LastRelevantTurnId { get; init; }

    /// <summary>Last successful session turn identifier.</summary>
    [JsonPropertyName("lastSuccessfulTurnId")]
    public string? LastSuccessfulTurnId { get; init; }

    /// <summary>Last failed session turn identifier.</summary>
    [JsonPropertyName("lastFailedTurnId")]
    public string? LastFailedTurnId { get; init; }

    /// <summary>Last checkpoint identifier.</summary>
    [JsonPropertyName("lastCheckpointId")]
    public string? LastCheckpointId { get; init; }

    /// <summary>Last commit SHA.</summary>
    [JsonPropertyName("lastCommitSha")]
    public string? LastCommitSha { get; init; }

    /// <summary>Last screenshot artifact identifier.</summary>
    [JsonPropertyName("lastScreenshotArtifactId")]
    public string? LastScreenshotArtifactId { get; init; }
}

/// <summary>
/// Stored iteration phase record.
/// </summary>
public sealed record TodoIterationPhase
{
    /// <summary>Phase identifier.</summary>
    [JsonPropertyName("phaseId")]
    public string PhaseId { get; init; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; init; } = string.Empty;

    /// <summary>Phase name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Phase summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    /// <summary>Phase status.</summary>
    [JsonPropertyName("status")]
    public TodoIterationPhaseStatus Status { get; init; } = TodoIterationPhaseStatus.Planning;

    /// <summary>Linked requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; init; } = [];

    /// <summary>Execution TODO identifiers in the phase.</summary>
    [JsonPropertyName("todoIds")]
    public IReadOnlyList<string> TodoIds { get; init; } = [];

    /// <summary>Entry criteria.</summary>
    [JsonPropertyName("entryCriteria")]
    public IReadOnlyList<string> EntryCriteria { get; init; } = [];

    /// <summary>Exit criteria.</summary>
    [JsonPropertyName("exitCriteria")]
    public IReadOnlyList<string> ExitCriteria { get; init; } = [];

    /// <summary>Originating plan identifier.</summary>
    [JsonPropertyName("createdFromPlanId")]
    public string? CreatedFromPlanId { get; init; }

    /// <summary>Branch name associated with the phase.</summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; init; }

    /// <summary>UTC creation time.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    /// <summary>UTC update time.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow.ToString("O");
}

/// <summary>
/// Stored checkpoint record for an execution TODO.
/// </summary>
public sealed record TodoCheckpoint
{
    /// <summary>Checkpoint identifier.</summary>
    [JsonPropertyName("checkpointId")]
    public string CheckpointId { get; init; } = string.Empty;

    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; init; } = string.Empty;

    /// <summary>Checkpoint kind.</summary>
    [JsonPropertyName("kind")]
    public TodoCheckpointKind Kind { get; init; }

    /// <summary>Checkpoint summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; init; }

    /// <summary>Requirement identifiers linked to the checkpoint.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; init; } = [];

    /// <summary>Session turn identifiers linked to the checkpoint.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string> SessionTurnIds { get; init; } = [];

    /// <summary>Artifact identifiers linked to the checkpoint.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string> ArtifactIds { get; init; } = [];

    /// <summary>Commit SHAs linked to the checkpoint.</summary>
    [JsonPropertyName("commitShas")]
    public IReadOnlyList<string> CommitShas { get; init; } = [];

    /// <summary>UTC creation time.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow.ToString("O");
}

/// <summary>
/// Stored execution TODO record.
/// </summary>
public sealed record TodoExecutionRecord
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; init; } = string.Empty;

    /// <summary>TODO title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>Execution goal.</summary>
    [JsonPropertyName("goal")]
    public string Goal { get; init; } = string.Empty;

    /// <summary>TODO summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; init; } = TodoExecutionStatus.Draft;

    /// <summary>Execution priority.</summary>
    [JsonPropertyName("priority")]
    public TodoExecutionPriority Priority { get; init; } = TodoExecutionPriority.Medium;

    /// <summary>Iteration phase identifier.</summary>
    [JsonPropertyName("iterationPhaseId")]
    public string? IterationPhaseId { get; init; }

    /// <summary>Parent TODO identifier.</summary>
    [JsonPropertyName("parentTodoId")]
    public string? ParentTodoId { get; init; }

    /// <summary>Child TODO identifiers.</summary>
    [JsonPropertyName("childTodoIds")]
    public IReadOnlyList<string> ChildTodoIds { get; init; } = [];

    /// <summary>Dependency list.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<TodoDependency> DependsOn { get; init; } = [];

    /// <summary>Blocking dependency list.</summary>
    [JsonPropertyName("blockedBy")]
    public IReadOnlyList<TodoDependency> BlockedBy { get; init; } = [];

    /// <summary>Acceptance criteria.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria { get; init; } = [];

    /// <summary>Constraints.</summary>
    [JsonPropertyName("constraints")]
    public IReadOnlyList<TodoConstraint> Constraints { get; init; } = [];

    /// <summary>Linked requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; init; } = [];

    /// <summary>Relevant file paths.</summary>
    [JsonPropertyName("relevantFiles")]
    public IReadOnlyList<string> RelevantFiles { get; init; } = [];

    /// <summary>Linked artifact identifiers.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string> ArtifactIds { get; init; } = [];

    /// <summary>Linked session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string> SessionTurnIds { get; init; } = [];

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; init; }

    /// <summary>Current test plan.</summary>
    [JsonPropertyName("testPlan")]
    public TodoTestPlan TestPlan { get; init; } = new();

    /// <summary>Current validation state.</summary>
    [JsonPropertyName("validation")]
    public TodoValidationState Validation { get; init; } = new();

    /// <summary>Execution pointers.</summary>
    [JsonPropertyName("pointers")]
    public TodoExecutionPointers Pointers { get; init; } = new();

    /// <summary>UTC creation time.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow.ToString("O");

    /// <summary>UTC update time.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow.ToString("O");
}

/// <summary>
/// Bounded execution context for the active Byrd TODO.
/// </summary>
public sealed record ActiveTodoContext
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Workspace path.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; init; } = string.Empty;

    /// <summary>TODO title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>Execution goal.</summary>
    [JsonPropertyName("goal")]
    public string Goal { get; init; } = string.Empty;

    /// <summary>TODO summary.</summary>
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; init; }

    /// <summary>Iteration phase identifier.</summary>
    [JsonPropertyName("iterationPhaseId")]
    public string? IterationPhaseId { get; init; }

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; init; }

    /// <summary>Linked requirement identifiers.</summary>
    [JsonPropertyName("requirementIds")]
    public IReadOnlyList<string> RequirementIds { get; init; } = [];

    /// <summary>Recent requirement snippets.</summary>
    [JsonPropertyName("recentRequirementSnippets")]
    public IReadOnlyList<string> RecentRequirementSnippets { get; init; } = [];

    /// <summary>Recent turn summaries.</summary>
    [JsonPropertyName("recentTurnSummaries")]
    public IReadOnlyList<string> RecentTurnSummaries { get; init; } = [];

    /// <summary>Relevant file paths.</summary>
    [JsonPropertyName("relevantFiles")]
    public IReadOnlyList<string> RelevantFiles { get; init; } = [];

    /// <summary>Artifact identifiers.</summary>
    [JsonPropertyName("artifactIds")]
    public IReadOnlyList<string> ArtifactIds { get; init; } = [];

    /// <summary>Acceptance criteria text.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<string> AcceptanceCriteria { get; init; } = [];

    /// <summary>Constraint text.</summary>
    [JsonPropertyName("constraints")]
    public IReadOnlyList<string> Constraints { get; init; } = [];

    /// <summary>Current test plan.</summary>
    [JsonPropertyName("testPlan")]
    public TodoTestPlan TestPlan { get; init; } = new();

    /// <summary>Current validation state.</summary>
    [JsonPropertyName("validation")]
    public TodoValidationState Validation { get; init; } = new();

    /// <summary>Execution pointers.</summary>
    [JsonPropertyName("pointers")]
    public TodoExecutionPointers Pointers { get; init; } = new();
}

/// <summary>
/// Delta context since a checkpoint.
/// </summary>
public sealed record TodoDeltaContext
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Checkpoint identifier used as the delta baseline.</summary>
    [JsonPropertyName("sinceCheckpointId")]
    public string? SinceCheckpointId { get; init; }

    /// <summary>New session turn identifiers.</summary>
    [JsonPropertyName("newTurnIds")]
    public IReadOnlyList<string> NewTurnIds { get; init; } = [];

    /// <summary>New session turn summaries.</summary>
    [JsonPropertyName("newTurnSummaries")]
    public IReadOnlyList<string> NewTurnSummaries { get; init; } = [];

    /// <summary>New artifact identifiers.</summary>
    [JsonPropertyName("newArtifactIds")]
    public IReadOnlyList<string> NewArtifactIds { get; init; } = [];

    /// <summary>New commit SHAs.</summary>
    [JsonPropertyName("newCommitShas")]
    public IReadOnlyList<string> NewCommitShas { get; init; } = [];

    /// <summary>Updated next action.</summary>
    [JsonPropertyName("updatedNextAction")]
    public string? UpdatedNextAction { get; init; }
}

/// <summary>
/// Request to create a Byrd iteration phase.
/// </summary>
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

/// <summary>
/// Result of creating a Byrd iteration phase.
/// </summary>
public sealed record CreateIterationPhaseResult
{
    /// <summary>Phase identifier.</summary>
    [JsonPropertyName("phaseId")]
    public string PhaseId { get; init; } = string.Empty;

    /// <summary>Phase status.</summary>
    [JsonPropertyName("status")]
    public TodoIterationPhaseStatus Status { get; init; }
}

/// <summary>
/// Plan step input used to create execution TODOs from a plan.
/// </summary>
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

/// <summary>
/// Request to create execution TODOs from a plan.
/// </summary>
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

/// <summary>
/// Result of creating execution TODOs from a plan.
/// </summary>
public sealed record CreateTodosFromPlanResult
{
    /// <summary>Iteration phase identifier.</summary>
    [JsonPropertyName("phaseId")]
    public string PhaseId { get; init; } = string.Empty;

    /// <summary>Created TODO identifiers.</summary>
    [JsonPropertyName("todoIds")]
    public IReadOnlyList<string> TodoIds { get; init; } = [];
}

/// <summary>
/// Compact result describing the current active TODO.
/// </summary>
public sealed record ActiveTodoResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>TODO title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>Current execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; init; }

    /// <summary>Suggested next action.</summary>
    [JsonPropertyName("nextAction")]
    public string? NextAction { get; init; }
}

/// <summary>
/// Request to store the test plan for a TODO.
/// </summary>
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

/// <summary>
/// Result of updating a TODO test plan.
/// </summary>
public sealed record SetTodoTestPlanResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Current execution status.</summary>
    [JsonPropertyName("status")]
    public TodoExecutionStatus Status { get; init; }
}

/// <summary>
/// Request to update a TODO status.
/// </summary>
public sealed class UpdateTodoStatusRequest
{
    /// <summary>Target execution status.</summary>
    [JsonPropertyName("targetStatus")]
    public TodoExecutionStatus TargetStatus { get; set; }

    /// <summary>Optional transition reason.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Result of updating a TODO status.
/// </summary>
public sealed record UpdateTodoStatusResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Previous execution status.</summary>
    [JsonPropertyName("previousStatus")]
    public TodoExecutionStatus PreviousStatus { get; init; }

    /// <summary>Current execution status.</summary>
    [JsonPropertyName("currentStatus")]
    public TodoExecutionStatus CurrentStatus { get; init; }
}

/// <summary>
/// Request to append a checkpoint to a TODO.
/// </summary>
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

/// <summary>
/// Result of appending a checkpoint.
/// </summary>
public sealed record AppendTodoCheckpointResult
{
    /// <summary>Checkpoint identifier.</summary>
    [JsonPropertyName("checkpointId")]
    public string CheckpointId { get; init; } = string.Empty;

    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;
}

/// <summary>
/// Request to record a validation result.
/// </summary>
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

/// <summary>
/// Result of recording a validation result.
/// </summary>
public sealed record RecordTodoValidationResultResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Updated validation state.</summary>
    [JsonPropertyName("validationState")]
    public TodoValidationState ValidationState { get; init; } = new();
}

/// <summary>
/// Request to link session turns to a TODO.
/// </summary>
public sealed class LinkTodoToSessionTurnsRequest
{
    /// <summary>Session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string>? SessionTurnIds { get; set; }
}

/// <summary>
/// Result of linking session turns to a TODO.
/// </summary>
public sealed record LinkTodoToSessionTurnsResult
{
    /// <summary>TODO identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; init; } = string.Empty;

    /// <summary>Linked session turn identifiers.</summary>
    [JsonPropertyName("sessionTurnIds")]
    public IReadOnlyList<string> SessionTurnIds { get; init; } = [];
}

/// <summary>
/// Request to perform a safe ADB step.
/// </summary>
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

/// <summary>
/// Result of performing a safe ADB step.
/// </summary>
public sealed record AdbStepResult
{
    /// <summary>Whether the action succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>ADB action performed.</summary>
    [JsonPropertyName("action")]
    public AdbStepAction Action { get; init; }

    /// <summary>Resolved device serial.</summary>
    [JsonPropertyName("deviceSerial")]
    public string? DeviceSerial { get; init; }

    /// <summary>Command summary.</summary>
    [JsonPropertyName("commandSummary")]
    public string? CommandSummary { get; init; }

    /// <summary>Captured screenshot path, when available.</summary>
    [JsonPropertyName("screenshotPath")]
    public string? ScreenshotPath { get; init; }

    /// <summary>Optional screenshot base64 payload.</summary>
    [JsonPropertyName("screenshotBase64")]
    public string? ScreenshotBase64 { get; init; }

    /// <summary>Current Android focus string.</summary>
    [JsonPropertyName("currentFocus")]
    public string? CurrentFocus { get; init; }

    /// <summary>Observation hints gathered from the step.</summary>
    [JsonPropertyName("observationHints")]
    public IReadOnlyList<string> ObservationHints { get; init; } = [];

    /// <summary>Error message when the action fails.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>UTC timestamp of the step result.</summary>
    [JsonPropertyName("timestampUtc")]
    public string TimestampUtc { get; init; } = DateTimeOffset.UtcNow.ToString("O");
}
