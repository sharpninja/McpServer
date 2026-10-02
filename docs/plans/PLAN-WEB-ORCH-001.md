# PLAN-WEB-ORCH-001: mcp-web orchestration dashboard (Prompter Hawk goals)

Status: requirements captured; waiting for operator approval before implementation.
Date: 2026-09-02
Author: GrokCode
Sources: https://prompterhawk.dev/ and https://prompterhawk.dev/docs.html
Session: GrokCode-20260902T145530Z-start-new-session
Request: req-prompterhawk-mcpweb-reqs (failsafe-queued; MCP storage unreachable)

## Intent

Give mcp-web (`McpServer.Web`, FR-MCP-031) a mission-control UI that covers the same operator goals as Prompter Hawk: one dashboard, many agents, parallel fire-and-forget work, automatic task pickup, recurring jobs, live observability, retry with context, human feedback, permissions, and multi-provider backends.

This is not a clone of Prompter Hawk. Map their nouns onto MCP Server nouns:

- Mission -> MCP workspace
- Agent -> pooled / hosted agent (`/mcpserver/agent-pool`)
- Task -> MCP TODO (never `TODO.yaml` as source of truth)
- Task bank -> mcp-web TODO board
- Live peek -> session log + event stream
- CLI companion -> existing Director CLI (do not replace it)

Do not copy Prompter Hawk pricing, magic-link auth, Python runtime, or vendor telemetry. mcp-web uses existing pairing/OIDC and the workspace API key.

## Out of scope until a later amendment

- Team-plan features (shared mission feed, per-seat billing)
- Local-model offline inference
- Prompter Hawk file formats (`.prompter-hawk/`, `mission.json`)
- Replacing Director TUI

## Requirements (queued in failsafe; not yet in the live store)

Functional: FR-WEB-001 through FR-WEB-020
Technical: TR-WEB-UI-001, TR-WEB-UI-002, TR-WEB-ORCH-001, TR-WEB-TODO-001, TR-WEB-SESS-001, TR-WEB-SEC-001, TR-WEB-SEC-002, TR-WEB-SCHED-001, TR-WEB-CTX-001, TR-WEB-OBS-001, TR-WEB-GIT-001, TR-WEB-API-001, TR-WEB-PRIV-001, TR-WEB-BUDGET-001
Tests: TEST-WEB-001 through TEST-WEB-020

Replay after storage returns: drain `.mcpServer/failsafe/GrokCode/workspaces/*/pending/` records labeled `requirements_create_batch` and `requirements_create_mapping`.

## Locked product decisions

1. Host: Blazor dashboard in `src/McpServer.Web` (mcp-web). If that project is missing from this solution, restore or recreate it as the FR-MCP-031 host. Do not put this UI in Director.
2. Data: mcp-web is a client of existing MCP REST/SSE. New REST only when an AC cannot be met by Todo, AgentPool, SessionLog, Events, Workspace policy, Templates, or Memory APIs (TR-WEB-API-001).
3. Auth: existing pairing and OIDC (FR-MCP-014, FR-MCP-026). No Prompter Hawk magic links.
4. TODO lanes: tentative (needs operator approve), pending, in_progress, blocked (prereq or policy), feedback_required, recurring, completed. Map onto MCP TODO status/tags; do not invent a second task store.
5. Parallelism: agents share the workspace filesystem. Coordination is TODO dependencies plus pool queue, not git branch-per-agent.
6. Fire-and-forget: enqueue work, operator may leave; dashboard remains truthful on return via SSE + refresh.
7. Privacy: mcp-web must not send prompts, file contents, or diffs to any analytics host. Model traffic stays on the existing provider path the operator already configured.
8. Responsive: desktop (1280px) and mobile (390px) are both in AC.
9. Python is forbidden in lab automation and must not become a runtime for mcp-web.

## Byrd phases

Hostile AGREE is required at each phase gate before the next phase. Full unit suite for in-scope tests: Failed 0, Skipped 0. Do not mark PLAN or FR done without hostile AGREE.

### Phase 0 (this turn): requirements

Capture FR/TR/TEST with structured AC. Store via MCP requirements workflow; while storage is down, queue failsafe records. Write this plan. Stop. Wait for "Yes, I approve the change".

Red tests for this phase: none. No product code.

### Phase 1: dashboard shell and agent fleet (FR-WEB-001, FR-WEB-002, FR-WEB-003, FR-WEB-017)

TDD unit tests first (shown red):

- `McpServer.Web.Tests.OrchestrationDashboardTests.RendersCockpit_WithWorkspaceBinding`
- `McpServer.Web.Tests.AgentFleetPanelTests.ShowsOffIdleWorkingWaiting_FromAgentPoolStatus`
- `McpServer.Web.Tests.AgentFleetPanelTests.StartStopRecycleStartAllStopAll_CallAgentPoolClient`
- `McpServer.Web.Tests.OrchestrationDashboardTests.DesktopAndMobileLayout_DoNotClipFleetOrBank`

Green: Blazor pages bind `AgentPoolClient` and workspace marker path. No duplicate pool logic.

### Phase 2: task bank and dispatch (FR-WEB-004, FR-WEB-005, FR-WEB-018)

Tests first:

- `TaskBankTests.Lanes_MatchTodoStatusAndTags`
- `TaskBankTests.CreateAndAssign_UsesTodoClient_NotYamlFile`
- `TaskBankTests.Dependencies_MoveCardToBlockedUntilPrereqDone`
- `DispatchTests.EnqueueOneShot_LeavesOperatorFree_StatusSurvivesRefresh`

### Phase 3: context, personas, retry (FR-WEB-007, FR-WEB-008, FR-WEB-019)

Tests first:

- `ContextInheritanceTests.WorkspaceThenAgentThenTask_MergedForDispatch`
- `RetryTests.Retry_ReusesSessionContextAndOriginalTodoId`
- `AgentPersonaTests.PromptSurvivesAgentRestart`

### Phase 4: observability (FR-WEB-009, FR-WEB-010, FR-WEB-015)

Tests first:

- `LivePeekTests.StreamsSessionDialogAndToolCalls`
- `BurnChartTests.ColorCodesRate_AgainstWorkspaceBaseline`
- `ProgressMetricsTests.CountsCommitsLinesAddedRemovedFilesAnalyzedAndPeakParallelism_FromSessionLog`
- `CommitLinkTests.CompletedTodo_ShowsLinkedCommitSha`

### Phase 5: HITL and captain (FR-WEB-011, FR-WEB-012)

Tests first:

- `FeedbackQueueTests.AgentWaitDoesNotBlockOtherAgents`
- `TentativeApprovalTests.Unapproved_IsNotDispatched`
- `CaptainTests.IdleAgent_CreatesTentativeTodos_NotAutoRun`

### Phase 6: policy, providers, privacy, budget, schedule (FR-WEB-006, FR-WEB-013, FR-WEB-014, FR-WEB-016, FR-WEB-020)

Tests first:

- `RecurringTodoTests.CronFires_CreatesOrReopensTodo`
- `PermissionUiTests.AllowDeny_ToolsAndPaths_FailClosed`
- `ProviderTests.PerAgentModel_ClaudeOpenAiGeminiGrok`
- `PrivacyTests.NoPromptOrFileTelemetry`
- `BudgetTests.EnqueueFails_WhenHourlyOrDailyCapExceeded`

## Validation commands (after approval, per phase)

```
pwsh -NoProfile -NonInteractive -File ./build.ps1 Test --filter FullyQualifiedName~McpServer.Web.Tests
```

Exit gate for a phase: zero failed, zero skipped in that filter, then the full unit suite for current plus previous web-orch tests. Then hostile validator.

## Failure modes

- Storage 503: keep failsafe; do not treat markdown exports as the store.
- Missing `McpServer.Web` project: Phase 1 first slice is restore/create the host project with red tests, not a silent pivot to Director.
- Operator rejects captain or budgets: drop FR-WEB-012 / FR-WEB-020 by amendment; do not implement them.

## Stop condition

No product implementation until explicit approval of this plan.
