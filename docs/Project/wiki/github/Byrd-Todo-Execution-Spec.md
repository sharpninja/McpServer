# TODO-Centered Byrd Development Process Implementation Spec

This document provides:

1. exact C# models for `McpServer`
2. exact MCP tool contracts
3. exact plugin updates
4. a Codex-ready implementation prompt

It is designed to align the MCP Server and Codex plugin with the Byrd Development Process, where planning is rich, TDD is mandatory, implementation is bounded by iteration phase, and validation is explicit and traceable.

---

## 1. Design Goals

The implementation must satisfy these goals:

- preserve rich planning fidelity without keeping the whole plan in Codex conversation state
- make TODOs the primary execution unit
- tie every TODO to requirements, acceptance criteria, constraints, and historical session turns
- enforce Byrd SDLC phases:
  - Planning
  - TDD-first Implementation
  - Validation
  - Deployment / Iteration progression
- let Codex resume from MCP state instead of chat memory
- minimize repeated context hydration by hydrating only the active TODO plus relevant deltas
- support device validation via `adb_step`

### Byrd Development Process implications

This implementation assumes:

- the MCP Server is the trusted persistence and coordination layer
- planning produces requirements, testing requirements, and iterative phases before implementation
- unit tests must be defined before implementation begins
- validation must prove acceptance criteria and preserve prior iterations
- strong process guidance is required so agents behave like disciplined software developers instead of generic assistants

---

## 2. Recommended Domain Model

## 2.1 New / Updated Aggregate Structure

Recommended aggregates:

- `Requirement`
- `IterationPhase` (new)
- `TodoItem` (extended)
- `TodoCheckpoint` (new)
- `PlanArtifact` (new or optional if plan storage already exists)
- `SessionLogTurn` (existing, linked more deeply)

### Relationship overview

- one `IterationPhase` has many `TodoItem`
- one `TodoItem` can link to many `Requirement`
- one `TodoItem` can link to many `SessionLogTurn`
- one `TodoItem` has many `TodoCheckpoint`
- one `PlanArtifact` decomposes into many `TodoItem`

---

## 2.2 C# Enums

````csharp
namespace McpServer.Domain.Todos;

public enum TodoStatus
{
    Draft = 0,
    Planned = 1,
    TestDesign = 2,
    TestReady = 3,
    Implementing = 4,
    Validating = 5,
    Blocked = 6,
    Complete = 7,
    Cancelled = 8
}

public enum TodoPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum IterationPhaseStatus
{
    Planning = 0,
    Implementing = 1,
    Validating = 2,
    Complete = 3,
    Blocked = 4,
    Cancelled = 5
}

public enum TodoCheckpointKind
{
    PlanningDecision = 0,
    TestDefined = 1,
    TestPassing = 2,
    ImplementationProgress = 3,
    ValidationPassed = 4,
    ValidationFailed = 5,
    Blocker = 6,
    DeviceValidation = 7,
    CommitCreated = 8,
    RequirementRefined = 9
}
````

---

## 2.3 Value Objects

````csharp
namespace McpServer.Domain.Todos;

public sealed record AcceptanceCriterion(
    string Id,
    string Text,
    bool IsSatisfied = false,
    string? Evidence = null
);

public sealed record TodoConstraint(
    string Id,
    string Text,
    string? Source = null
);

public sealed record TodoFileReference(
    string Path,
    string? Reason = null
);

public sealed record TodoArtifactReference(
    string Id,
    string Type,
    string? Path = null,
    string? Description = null
);

public sealed record TodoDependency(
    string TodoId,
    string Reason
);

public sealed record TodoTestPlan(
    bool UnitTestsDefined,
    bool UnitTestsPassing,
    bool IntegrationTestsDefined,
    bool IntegrationTestsPassing,
    IReadOnlyList<string> TestFilePaths,
    IReadOnlyList<string> TestCommands
);

public sealed record TodoValidationState(
    string LastResult,
    DateTimeOffset? LastValidatedAtUtc,
    IReadOnlyList<string> ValidationArtifactIds,
    string? Summary
);

public sealed record TodoExecutionPointers(
    string? LastRelevantTurnId,
    string? LastSuccessfulTurnId,
    string? LastFailedTurnId,
    string? LastCheckpointId,
    string? LastCommitSha,
    string? LastScreenshotArtifactId
);
````

---

## 2.4 `IterationPhase` Entity

````csharp
namespace McpServer.Domain.Todos;

public sealed class IterationPhase
{
    public string Id { get; init; } = default!;
    public string WorkspacePath { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public IterationPhaseStatus Status { get; set; } = IterationPhaseStatus.Planning;

    public List<string> RequirementIds { get; set; } = [];
    public List<string> TodoIds { get; set; } = [];

    public List<string> EntryCriteria { get; set; } = [];
    public List<string> ExitCriteria { get; set; } = [];

    public string? CreatedFromPlanId { get; set; }
    public string? Branch { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
````

---

## 2.5 `TodoCheckpoint` Entity

````csharp
namespace McpServer.Domain.Todos;

public sealed class TodoCheckpoint
{
    public string Id { get; init; } = default!;
    public string TodoId { get; init; } = default!;
    public string WorkspacePath { get; init; } = default!;

    public TodoCheckpointKind Kind { get; set; }
    public string Summary { get; set; } = default!;
    public string? NextAction { get; set; }

    public List<string> RequirementIds { get; set; } = [];
    public List<string> SessionTurnIds { get; set; } = [];
    public List<string> ArtifactIds { get; set; } = [];
    public List<string> CommitShas { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
````

---

## 2.6 `TodoItem` Entity

````csharp
namespace McpServer.Domain.Todos;

public sealed class TodoItem
{
    public string Id { get; init; } = default!;
    public string WorkspacePath { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Goal { get; set; } = default!;
    public string Summary { get; set; } = default!;

    public TodoStatus Status { get; set; } = TodoStatus.Draft;
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;

    public string? IterationPhaseId { get; set; }
    public string? ParentTodoId { get; set; }
    public List<string> ChildTodoIds { get; set; } = [];

    public List<TodoDependency> DependsOn { get; set; } = [];
    public List<TodoDependency> BlockedBy { get; set; } = [];

    public List<AcceptanceCriterion> AcceptanceCriteria { get; set; } = [];
    public List<TodoConstraint> Constraints { get; set; } = [];

    public List<string> RequirementIds { get; set; } = [];
    public List<string> SessionTurnIds { get; set; } = [];
    public List<string> CheckpointIds { get; set; } = [];

    public List<TodoFileReference> RelevantFiles { get; set; } = [];
    public List<TodoArtifactReference> Artifacts { get; set; } = [];

    public TodoTestPlan TestPlan { get; set; } = new(
        UnitTestsDefined: false,
        UnitTestsPassing: false,
        IntegrationTestsDefined: false,
        IntegrationTestsPassing: false,
        TestFilePaths: [],
        TestCommands: []
    );

    public TodoValidationState Validation { get; set; } = new(
        LastResult: "not_run",
        LastValidatedAtUtc: null,
        ValidationArtifactIds: [],
        Summary: null
    );

    public TodoExecutionPointers Pointers { get; set; } = new(
        LastRelevantTurnId: null,
        LastSuccessfulTurnId: null,
        LastFailedTurnId: null,
        LastCheckpointId: null,
        LastCommitSha: null,
        LastScreenshotArtifactId: null
    );

    public string? NextAction { get; set; }
    public string? CreatedFromPlanId { get; set; }
    public string? Branch { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
````

---

## 2.7 Optional `PlanArtifact` Entity

Use this if you want first-class plan persistence instead of embedding plan text only in session log.

````csharp
namespace McpServer.Domain.Planning;

public sealed class PlanArtifact
{
    public string Id { get; init; } = default!;
    public string WorkspacePath { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public string FullMarkdown { get; set; } = default!;

    public List<string> RequirementIds { get; set; } = [];
    public List<string> IterationPhaseIds { get; set; } = [];
    public List<string> TodoIds { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
````

---

## 2.8 Read Models

These are the most important surfaces for Codex.

### `ActiveTodoContext`

````csharp
namespace McpServer.Application.Todos.Models;

public sealed record ActiveTodoContext(
    string TodoId,
    string WorkspacePath,
    string Title,
    string Goal,
    string Summary,
    string Status,
    string? IterationPhaseId,
    string? NextAction,
    IReadOnlyList<string> RequirementIds,
    IReadOnlyList<string> RecentRequirementSnippets,
    IReadOnlyList<string> RecentTurnSummaries,
    IReadOnlyList<string> RelevantFiles,
    IReadOnlyList<string> ArtifactIds,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> Constraints,
    TodoTestPlan TestPlan,
    TodoValidationState Validation,
    TodoExecutionPointers Pointers
);
````

### `TodoDeltaContext`

````csharp
namespace McpServer.Application.Todos.Models;

public sealed record TodoDeltaContext(
    string TodoId,
    string? SinceCheckpointId,
    IReadOnlyList<string> NewTurnIds,
    IReadOnlyList<string> NewTurnSummaries,
    IReadOnlyList<string> NewArtifactIds,
    IReadOnlyList<string> NewCommitShas,
    string? UpdatedNextAction
);
````

---

## 3. Repository / Persistence Interfaces

````csharp
namespace McpServer.Domain.Todos;

public interface ITodoRepository
{
    Task<TodoItem?> GetByIdAsync(string workspacePath, string todoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TodoItem>> GetByIdsAsync(string workspacePath, IEnumerable<string> todoIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TodoItem>> GetActiveAsync(string workspacePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TodoItem>> GetReadyAsync(string workspacePath, CancellationToken cancellationToken = default);
    Task UpsertAsync(TodoItem todo, CancellationToken cancellationToken = default);
    Task DeleteAsync(string workspacePath, string todoId, CancellationToken cancellationToken = default);
}

public interface IIterationPhaseRepository
{
    Task<IterationPhase?> GetByIdAsync(string workspacePath, string phaseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IterationPhase>> GetActiveAsync(string workspacePath, CancellationToken cancellationToken = default);
    Task UpsertAsync(IterationPhase phase, CancellationToken cancellationToken = default);
}

public interface ITodoCheckpointRepository
{
    Task<TodoCheckpoint?> GetByIdAsync(string workspacePath, string checkpointId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TodoCheckpoint>> GetByTodoAsync(string workspacePath, string todoId, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TodoCheckpoint>> GetSinceAsync(string workspacePath, string todoId, string checkpointId, CancellationToken cancellationToken = default);
    Task UpsertAsync(TodoCheckpoint checkpoint, CancellationToken cancellationToken = default);
}
````

---

## 4. Application Services

## 4.1 Planning Service

````csharp
namespace McpServer.Application.Planning;

public interface IPlanDecompositionService
{
    Task<PlanToTodoResult> CreateTodosFromPlanAsync(
        string workspacePath,
        string planId,
        string phaseName,
        IReadOnlyList<PlanStepInput> steps,
        CancellationToken cancellationToken = default);
}

public sealed record PlanStepInput(
    string Title,
    string Goal,
    string Summary,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<string> RequirementIds,
    IReadOnlyList<string> RelevantFiles,
    IReadOnlyList<string> DependsOnTodoIds
);

public sealed record PlanToTodoResult(
    string IterationPhaseId,
    IReadOnlyList<string> TodoIds
);
````

## 4.2 TODO Context Hydration Service

````csharp
namespace McpServer.Application.Todos;

public interface ITodoContextHydrationService
{
    Task<ActiveTodoContext?> GetExecutionContextAsync(
        string workspacePath,
        string todoId,
        int requirementSnippetLimit = 5,
        int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default);

    Task<TodoDeltaContext> GetDeltaContextAsync(
        string workspacePath,
        string todoId,
        string? sinceCheckpointId,
        CancellationToken cancellationToken = default);
}
````

## 4.3 TODO Progression Rules Service

````csharp
namespace McpServer.Application.Todos;

public interface ITodoProgressionService
{
    Task ValidateTransitionAsync(
        TodoItem todo,
        TodoStatus targetStatus,
        CancellationToken cancellationToken = default);

    Task<TodoItem> SetStatusAsync(
        string workspacePath,
        string todoId,
        TodoStatus targetStatus,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<TodoItem?> GetNextReadyTodoAsync(
        string workspacePath,
        CancellationToken cancellationToken = default);
}
````

### Transition rules to enforce

Recommended transition rules:

- `Draft -> Planned`
- `Planned -> TestDesign`
- `TestDesign -> TestReady` only when `UnitTestsDefined == true`
- `TestReady -> Implementing`
- `Implementing -> Validating` only when code changes/checkpoints exist
- `Validating -> Complete` only when:
  - `UnitTestsPassing == true`
  - if integration tests are required, `IntegrationTestsPassing == true`
  - all acceptance criteria are satisfied or explicitly waived
- any state can go to `Blocked`
- `Blocked -> Planned | TestDesign | Implementing | Validating` with explicit reason
- `Complete` is terminal unless reopened by requirement refinement

This preserves the Byrd process requirement that implementation starts with tests and validation proves the iteration before completion.

---

## 5. Exact MCP Tool Contracts

The contracts below use stable JSON-friendly request/response shapes.

## 5.1 `create_iteration_phase`

Purpose:
Create a bounded Byrd iteration phase aligned to requirements and scope.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "name": "TODO-Centered Execution Model",
  "summary": "Add TODO execution context, phase tracking, and plugin steering for Codex.",
  "requirementIds": ["REQ-101", "REQ-102"],
  "entryCriteria": [
    "Requirements linked",
    "Scope approved"
  ],
  "exitCriteria": [
    "All phase TODOs complete",
    "Validation artifacts recorded"
  ],
  "createdFromPlanId": "PLAN-77",
  "branch": "main"
}
````

### Response

````json
{
  "phaseId": "PHASE-12",
  "status": "Planning"
}
````

---

## 5.2 `create_todos_from_plan`

Purpose:
Decompose an approved plan into executable TODO items inside an iteration phase.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "phaseId": "PHASE-12",
  "planId": "PLAN-77",
  "todos": [
    {
      "title": "Extend TodoItem domain model",
      "goal": "Support requirement links, phase membership, checkpoints, and test gating.",
      "summary": "Add new fields and supporting value objects to the todo domain.",
      "acceptanceCriteria": [
        "Todo model stores requirement IDs",
        "Todo model stores session turn IDs",
        "Todo model stores test plan and validation state"
      ],
      "constraints": [
        "Preserve backward compatibility where practical",
        "Do not break existing MCP TODO workflows"
      ],
      "requirementIds": ["REQ-101"],
      "relevantFiles": [
        "src/McpServer.Domain/Todos/TodoItem.cs"
      ],
      "dependsOnTodoIds": []
    }
  ]
}
````

### Response

````json
{
  "phaseId": "PHASE-12",
  "todoIds": ["TODO-201"]
}
````

---

## 5.3 `get_active_todo`

Purpose:
Return the single TODO Codex should work on next.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver"
}
````

### Response

````json
{
  "todoId": "TODO-201",
  "title": "Extend TodoItem domain model",
  "status": "TestDesign",
  "nextAction": "Define unit tests for new fields and transition rules"
}
````

---

## 5.4 `get_todo_execution_context`

Purpose:
Hydrate a single bounded working set for Codex.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "todoId": "TODO-201",
  "requirementSnippetLimit": 5,
  "sessionTurnSummaryLimit": 5
}
````

### Response

````json
{
  "todoId": "TODO-201",
  "workspacePath": "F:\\GitHub\\mcpserver",
  "title": "Extend TodoItem domain model",
  "goal": "Support requirement links, phase membership, checkpoints, and test gating.",
  "summary": "Add new fields and supporting value objects to the todo domain.",
  "status": "TestDesign",
  "iterationPhaseId": "PHASE-12",
  "nextAction": "Define unit tests for new fields and transition rules",
  "requirementIds": ["REQ-101"],
  "recentRequirementSnippets": [
    "Planning results in artifacts that capture requirements, testing requirements, and iterative phases."
  ],
  "recentTurnSummaries": [
    "Earlier design decision: TODOs are the bounded execution unit."
  ],
  "relevantFiles": [
    "src/McpServer.Domain/Todos/TodoItem.cs",
    "src/McpServer.Application/Todos/TodoProgressionService.cs"
  ],
  "artifactIds": [],
  "acceptanceCriteria": [
    "Todo model stores requirement IDs",
    "Todo model stores session turn IDs",
    "Todo model stores test plan and validation state"
  ],
  "constraints": [
    "Preserve backward compatibility where practical",
    "Do not break existing MCP TODO workflows"
  ],
  "testPlan": {
    "unitTestsDefined": false,
    "unitTestsPassing": false,
    "integrationTestsDefined": false,
    "integrationTestsPassing": false,
    "testFilePaths": [],
    "testCommands": []
  },
  "validation": {
    "lastResult": "not_run",
    "lastValidatedAtUtc": null,
    "validationArtifactIds": [],
    "summary": null
  },
  "pointers": {
    "lastRelevantTurnId": null,
    "lastSuccessfulTurnId": null,
    "lastFailedTurnId": null,
    "lastCheckpointId": null,
    "lastCommitSha": null,
    "lastScreenshotArtifactId": null
  }
}
````

---

## 5.5 `get_todo_delta_context`

Purpose:
Fetch only what changed since the last checkpoint instead of rereading history.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "todoId": "TODO-201",
  "sinceCheckpointId": "CHK-44"
}
````

### Response

````json
{
  "todoId": "TODO-201",
  "sinceCheckpointId": "CHK-44",
  "newTurnIds": ["TURN-912"],
  "newTurnSummaries": [
    "Unit tests for TodoProgressionService are now defined but failing on Complete transition."
  ],
  "newArtifactIds": [],
  "newCommitShas": ["abc1234"],
  "updatedNextAction": "Fix Complete transition guard in TodoProgressionService"
}
````

---

## 5.6 `set_todo_test_plan`

Purpose:
Store test files and commands before implementation begins.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "todoId": "TODO-201",
  "unitTestsDefined": true,
  "integrationTestsDefined": false,
  "testFilePaths": [
    "tests/McpServer.Application.Tests/Todos/TodoProgressionServiceTests.cs"
  ],
  "testCommands": [
    "dotnet test tests/McpServer.Application.Tests"
  ]
}
````

### Response

````json
{
  "todoId": "TODO-201",
  "status": "TestReady"
}
````

---

## 5.7 `update_todo_status`

Purpose:
Move TODO through Byrd process states with enforcement.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "todoId": "TODO-201",
  "targetStatus": "Implementing",
  "reason": "Unit tests are defined and reviewed"
}
````

### Response

````json
{
  "todoId": "TODO-201",
  "previousStatus": "TestReady",
  "currentStatus": "Implementing"
}
````

---

## 5.8 `append_todo_checkpoint`

Purpose:
Record progress, decisions, failures, or validation results.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "todoId": "TODO-201",
  "kind": "TestDefined",
  "summary": "Defined tests for TodoItem model additions and progression rules.",
  "nextAction": "Implement new domain fields and update persistence mapping.",
  "requirementIds": ["REQ-101"],
  "sessionTurnIds": ["TURN-912"],
  "artifactIds": [],
  "commitShas": []
}
````

### Response

````json
{
  "checkpointId": "CHK-45",
  "todoId": "TODO-201"
}
````

---

## 5.9 `record_todo_validation_result`

Purpose:
Persist validation state, including device validation via `adb_step`.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "todoId": "TODO-201",
  "result": "pass",
  "summary": "Unit tests pass and acceptance criteria for extended TODO model are satisfied.",
  "artifactIds": ["ART-991"],
  "sessionTurnIds": ["TURN-919"]
}
````

### Response

````json
{
  "todoId": "TODO-201",
  "validationState": {
    "lastResult": "pass",
    "lastValidatedAtUtc": "2026-04-23T22:00:00Z",
    "validationArtifactIds": ["ART-991"],
    "summary": "Unit tests pass and acceptance criteria for extended TODO model are satisfied."
  }
}
````

---

## 5.10 `get_next_ready_todo`

Purpose:
Advance work without rereading the whole plan.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver"
}
````

### Response

````json
{
  "todoId": "TODO-202",
  "title": "Add TODO execution context MCP tool",
  "status": "Planned",
  "nextAction": "Define unit tests for get_todo_execution_context"
}
````

---

## 5.11 `link_todo_to_session_turns`

Purpose:
Attach historical evidence to a TODO without duplicating log content.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "todoId": "TODO-202",
  "sessionTurnIds": ["TURN-880", "TURN-889"]
}
````

### Response

````json
{
  "todoId": "TODO-202",
  "sessionTurnIds": ["TURN-880", "TURN-889"]
}
````

---

## 5.12 `adb_step`

Purpose:
Mechanical Android validation action surface for Codex.

### Request

````json
{
  "workspacePath": "F:\\GitHub\\mcpserver",
  "deviceSerial": null,
  "action": "screenshot",
  "captureScreenshot": true,
  "instruction": "Capture current Android UI state before login flow validation."
}
````

### Response

````json
{
  "success": true,
  "action": "screenshot",
  "deviceSerial": "emulator-5554",
  "commandSummary": "adb -s emulator-5554 exec-out screencap -p",
  "screenshotPath": "artifacts/device/20260423-220101.png",
  "screenshotBase64": null,
  "currentFocus": "com.example/.MainActivity",
  "observationHints": [],
  "error": null,
  "timestampUtc": "2026-04-23T22:01:01Z"
}
````

---

## 6. Service Implementation Notes

## 6.1 Hydration strategy

`get_todo_execution_context` should return:

- TODO core fields
- concise requirement snippets
- concise recent turn summaries
- relevant file paths
- current test and validation state
- latest pointers

It should not return:

- full plan markdown
- full session log entries
- large artifact payloads
- full requirement documents

## 6.2 Delta strategy

On every checkpoint write:
- update `TodoItem.Pointers.LastCheckpointId`
- optionally update `LastRelevantTurnId`, `LastSuccessfulTurnId`, `LastFailedTurnId`, `LastCommitSha`, `LastScreenshotArtifactId`

Then `get_todo_delta_context` can cheaply return only what changed.

## 6.3 Byrd process enforcement

The progression service should enforce:

- no implementation before tests are defined
- no completion before validation passes with 100% test success for the executed gate
- no transition to the next iteration when the executed gate has failed tests, skipped tests, or required tests that were not run
- tests directly track progress; do not use skipped tests as placeholders for deferred work
- deferred work is tracked in MCP TODO/requirements state until its slice begins, then tests are added and made to pass before progression
- integration testing can be scheduled as a later gate, but any integration tests included in the executed gate must pass without skips
- reopened TODOs are expected when requirements are refined

That mirrors the Byrd Development Process emphasis on strong requirements, TDD, iterative refinement, and proof-driven completion.

---

## 7. Exact Plugin Updates

These changes apply to `mcpserver-codex-plugin`.

## 7.1 Add `AGENTS.md` at plugin root

Create:

`AGENTS.md`

Contents:

````md
# McpServer Codex Usage

Use the MCP Server as the default source of task continuity and execution state.

Follow the Byrd Development Process:

1. Planning
2. Test design
3. Implementation
4. Validation
5. Iteration progression

Rules:
- For multi-step work, persist approved plans as iteration phases and TODO items.
- Execute from the active TODO context, not from chat history.
- Prefer `get_active_todo`, `get_todo_execution_context`, and `get_todo_delta_context` over broad history reads.
- Do not implement a TODO until tests are defined.
- Do not mark a TODO complete until validation passes.
- Use MCP for continuity, requirements, session turns, TODO orchestration, and device actions.
- Keep reasoning, planning, and code decisions in Codex.
- For attached Android validation, use `adb_step` for screenshot -> inspect -> act -> screenshot loops.
- Record checkpoints after meaningful progress, validation cycles, and failures.
- Do not ask the user to restate work that should exist in MCP.
````

---

## 7.2 Add `skills/workflow/SKILL.md`

Create:

`skills/workflow/SKILL.md`

Contents:

````md
# Workflow Skill

Use this skill for multi-step development work in repositories that use McpServer.

## Goal

Execute substantial work through bounded TODO contexts instead of reconstructing task state from conversation history.

## Preferred workflow

1. Ensure MCP trust bootstrap is complete.
2. If the task is new and substantial, create or update an iteration phase.
3. Persist the approved plan as TODO items.
4. Select the active TODO.
5. Retrieve the TODO execution context.
6. Define tests before implementation.
7. Implement only after tests are defined.
8. Validate the result.
9. Record a checkpoint.
10. Move to the next ready TODO.

## Rules

- Prefer active TODO and delta context over full session history.
- Use requirements as the source of truth for completion criteria.
- Use linked session log turns as historical evidence.
- Keep TODOs bounded and resumable.
- Reopen or refine TODOs when requirements change.
````

---

## 7.3 Add `skills/device/SKILL.md`

Create:

`skills/device/SKILL.md`

Contents:

````md
# Device Validation Skill

Use this skill when an attached Android device is part of validation.

## Default loop

1. Capture a screenshot with `adb_step`.
2. Inspect the visible state.
3. Perform the next mechanical action with `adb_step`.
4. Capture another screenshot.
5. Determine whether validation passed, failed, or requires code changes.
6. Record a TODO checkpoint and session turn.

## Rules

- Always capture a screenshot before assuming device state.
- Do not assume navigation succeeded without visual confirmation.
- Use `adb_step` for mechanical actions only.
- Keep reasoning and decision-making in Codex.
- Ask the user for help only when device access is ambiguous, unavailable, blocked by credentials, or destructive confirmation is required.
````

---

## 7.4 Update `skills/session/SKILL.md`

Add near the top:

````md
## Preferred workflow

For ongoing work:
1. Resume from MCP trust bootstrap and current session state.
2. Prefer active TODO and TODO delta context over broad session history.
3. Use session turns as historical evidence linked to TODOs.
4. Record a new session turn after meaningful progress, validation, or failure.
5. Keep the active execution context bounded to the current TODO.
````

---

## 7.5 Update `skills/todo/SKILL.md`

Add near the top:

````md
## Usage guidance

Treat TODOs as bounded execution units aligned to the Byrd Development Process.

Use TODOs to:
- store goal, acceptance criteria, constraints, and next action
- link requirements and relevant session turns
- gate implementation on test definition
- drive progression through planning, implementation, and validation

Do not use TODOs as loose reminders only.
````

---

## 7.6 Update `lib/user-prompt-submit.sh`

Replace the reminder text with a workflow-first message:

````text
A session turn is active. Use McpServer as the default source of task continuity and execution state:
1. Prefer active TODO and TODO delta context over asking the user for context.
2. For multi-step work, persist approved plans as TODO items in an iteration phase.
3. Do not implement before tests are defined.
4. For attached Android validation, use adb_step for screenshot -> inspect -> act -> screenshot loops.
5. Record a checkpoint after meaningful progress, validation, or failure.
6. Run code-verify.sh after source edits and stop-gate.sh before the final response.
````

---

## 7.7 Update `.codex-plugin/plugin.json`

Suggested description update:

````json
{
  "name": "mcpserver",
  "description": "Primary workspace context, Byrd-process TODO execution, requirements, session logging, and Android device validation via adb_step.",
  "skillsPath": "skills"
}
````

---

## 8. Suggested Tests

## 8.1 Domain / progression tests

Create tests for:

- cannot transition to `Implementing` unless `UnitTestsDefined == true`
- cannot transition to `Complete` unless validation passed
- blocked TODO can resume only with explicit reason
- acceptance criteria persist correctly
- requirement links persist correctly
- session turn links persist correctly

## 8.2 Hydration tests

Create tests for:

- `get_todo_execution_context` returns bounded output
- requirement snippet limits are respected
- session turn summary limits are respected
- full plan is not returned
- delta context returns only changes since checkpoint

## 8.3 MCP tool tests

Create tests for:

- `create_todos_from_plan`
- `get_active_todo`
- `get_todo_execution_context`
- `set_todo_test_plan`
- `update_todo_status`
- `append_todo_checkpoint`
- `record_todo_validation_result`
- `get_next_ready_todo`
- `adb_step`

## 8.4 Plugin tests

Add or update bats tests for:

- AGENTS.md presence
- workflow/device skills existence
- prompt hook includes TODO-first guidance
- plugin description still valid JSON
- bootstrap behavior still functions

---

## 9. Suggested Implementation Order

1. Extend domain models
2. Add repositories and persistence mapping
3. Add progression service with Byrd gating rules
4. Add context hydration service
5. Add MCP tools for TODO lifecycle
6. Add `adb_step`
7. Update plugin files
8. Add tests
9. Update docs

---

## 10. Codex-Ready Implementation Prompt

Use this prompt with Codex.

````text
You are working in the `sharpninja/mcpserver` and `sharpninja/mcpserver-codex-plugin` repos.

Implement a TODO-centered execution model aligned to the Byrd Development Process.

Context:
- Planning must remain rich, but execution must not depend on carrying the full plan in chat context.
- The MCP Server is the trusted persistence and coordination layer.
- TODOs should become the primary bounded execution unit.
- Requirements remain the source of truth for why work exists and what completion means.
- Session Log Turns provide historical evidence and recent execution context.
- Implementation must enforce Byrd SDLC expectations:
  - Planning
  - Test-first implementation
  - Validation
  - Iterative progression
- Device validation should be supported through adb_step.

Main deliverables:
1. Add or extend domain models for:
   - IterationPhase
   - TodoItem
   - TodoCheckpoint
   - optional PlanArtifact if needed
2. Add repository interfaces and persistence for the new/extended models.
3. Add application services for:
   - plan decomposition to TODOs
   - TODO execution context hydration
   - TODO delta context
   - TODO status progression with Byrd process gating
4. Add MCP tools:
   - create_iteration_phase
   - create_todos_from_plan
   - get_active_todo
   - get_todo_execution_context
   - get_todo_delta_context
   - set_todo_test_plan
   - update_todo_status
   - append_todo_checkpoint
   - record_todo_validation_result
   - get_next_ready_todo
   - link_todo_to_session_turns
   - adb_step
5. Update the Codex plugin:
   - add AGENTS.md
   - add workflow skill
   - add device skill
   - update session and todo skills
   - update user-prompt-submit.sh reminder text
   - update plugin.json description if appropriate
6. Add tests for domain rules, hydration, MCP tools, and plugin changes.

Critical rules:
- Do not let implementation start before tests are defined for the active TODO.
- Do not let a TODO complete before validation passes.
- Do not rely on chat memory for continuity when MCP state is available.
- Keep TODO execution context bounded:
  - concise requirement snippets
  - concise recent turn summaries
  - relevant files
  - test/validation state
  - pointers
- Do not return full plan markdown or broad session history from TODO context tools.
- Link session turns to TODOs as evidence instead of duplicating raw logs.
- Use existing repo conventions for DI, registration, logging, configuration, and tests.
- Keep the implementation minimal, coherent, and production-appropriate.

Implementation guidance:
- Inspect the existing TODO, session log, requirements, and MCP tool patterns first.
- Prefer additive changes over invasive rewrites.
- Reuse existing storage and service patterns where possible.
- Keep tool outputs structured and bounded.
- If a plan artifact already exists in session log or another store, avoid creating redundant storage unless a dedicated PlanArtifact is clearly justified.
- For adb_step:
  - allow only fixed safe ADB operations
  - no arbitrary shell passthrough
  - return structured JSON
  - support screenshot, tap, swipe, text, keyevent, wait, launch_app, get_focus

Acceptance criteria:
- Codex can persist an approved plan as TODOs in an iteration phase.
- Codex can resume from a single active TODO context instead of reconstructing the full task from conversation history.
- TODOs carry local goal, acceptance criteria, constraints, requirement links, session turn links, relevant files, next action, test plan, and validation state.
- Transition rules enforce Byrd process gating.
- Plugin guidance teaches Codex to use active TODO and TODO delta context first.
- Plugin guidance teaches Codex to use adb_step for attached Android validation.
- Tests pass.
- Commit all changes and leave the repo clean.

Deliverables in final response:
1. concise summary of architecture changes
2. files changed
3. tests run
4. any follow-up recommendations
````

---

## 11. Recommended File/Namespace Layout

Example only; adapt to repo conventions.

````text
src/
  McpServer.Domain/
    Todos/
      TodoItem.cs
      TodoCheckpoint.cs
      IterationPhase.cs
      AcceptanceCriterion.cs
      TodoConstraint.cs
      TodoDependency.cs
      TodoFileReference.cs
      TodoArtifactReference.cs
      TodoTestPlan.cs
      TodoValidationState.cs
      TodoExecutionPointers.cs
      TodoStatus.cs
      TodoPriority.cs
      IterationPhaseStatus.cs
      TodoCheckpointKind.cs
      ITodoRepository.cs
      ITodoCheckpointRepository.cs
      IIterationPhaseRepository.cs

  McpServer.Application/
    Todos/
      ITodoContextHydrationService.cs
      ITodoProgressionService.cs
      TodoContextHydrationService.cs
      TodoProgressionService.cs
      Models/
        ActiveTodoContext.cs
        TodoDeltaContext.cs
    Planning/
      IPlanDecompositionService.cs
      PlanDecompositionService.cs
      Models/
        PlanStepInput.cs
        PlanToTodoResult.cs

  McpServer.Mcp/
    Tools/
      Todos/
        CreateIterationPhaseTool.cs
        CreateTodosFromPlanTool.cs
        GetActiveTodoTool.cs
        GetTodoExecutionContextTool.cs
        GetTodoDeltaContextTool.cs
        SetTodoTestPlanTool.cs
        UpdateTodoStatusTool.cs
        AppendTodoCheckpointTool.cs
        RecordTodoValidationResultTool.cs
        GetNextReadyTodoTool.cs
        LinkTodoToSessionTurnsTool.cs
      Device/
        AdbStepTool.cs
````

---

## 12. Final Notes

This design preserves planning fidelity by moving the full plan into persistent structured state and hydrating only the current execution slice for Codex. It aligns the MCP Server and plugin with the Byrd Development Process by making TODOs phase-aware, requirement-linked, TDD-gated, and validation-driven instead of acting as loose reminders.

Use the TODO graph as the execution memory.
Use requirements as the source of truth.
Use session turns as historical evidence.
Use Codex for reasoning and implementation.
Use MCP for persistence, coordination, and mechanical execution support.
